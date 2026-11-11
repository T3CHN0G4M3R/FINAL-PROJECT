using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class GameSaveManager : MonoBehaviour
{
    public static GameSaveManager Instance { get; private set; }
    public SaveFileRepository Repository { get; private set; }
    public bool Busy { get; private set; }
    public string Status { get; private set; }
    public event Action Changed;
    private GameplaySaveAdapter _adapter;
    private string _runId;
    private readonly Dictionary<string, SavedSceneState> _visited = new Dictionary<string, SavedSceneState>();
    private SaveGameData _pending;
    private Action<bool, string> _loadCompletion;
    private Coroutine _loadRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => Instance = null;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        new GameObject("GameSaveManager").AddComponent<GameSaveManager>();
    }
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Repository = new SaveFileRepository();
        _runId = Guid.NewGuid().ToString("N");
        GameplaySaveAdapter.SceneReady += OnSceneReady;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDestroy()
    {
        GameplaySaveAdapter.SceneReady -= OnSceneReady;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (_retryTransition != null) _retryTransition.Dispose();
        if (Instance == this) Instance = null;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            _adapter = null;
            _entryCheckpointJson = null; _entryScene = null;
            _retryPending = false; EncounterInProgress = false;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    public bool IntroTransitionInProgress { get; private set; }
    public void BeginIntroTransition() { IntroTransitionInProgress = true; Busy = true; }
    public void EndIntroTransition()
    {
        IntroTransitionInProgress = false;
        Busy = false;
        if (_adapter != null) _adapter.BlockInput(false);
        Changed?.Invoke();
    }
    public void Report(string status) { Status = status; Changed?.Invoke(); }
    private void OnSceneReady(GameplaySaveAdapter adapter)
    {
        adapter.HasCoordinator = true;
        _adapter = adapter;
        try
        {
            string error = adapter.ValidateObjects();
            if (error != null) throw new InvalidOperationException(error);
            bool loading = _pending != null;
            if (_retryPending)
            {
                if (!HasEntryCheckpoint(adapter.ScenePath)) throw new InvalidOperationException("Retry loaded the wrong level");
                var checkpoint = JsonUtility.FromJson<SavedSceneState>(_entryCheckpointJson);
                adapter.Restore(checkpoint);
                _visited[adapter.ScenePath] = JsonUtility.FromJson<SavedSceneState>(_entryCheckpointJson);
                _retryPending = false;
                _retryRestored = true;
                Report("Entry checkpoint restored; revealing level");
            }
            else if (loading)
            {
                var state = _pending.scenes.FirstOrDefault(s => s.path == adapter.ScenePath);
                if (_pending.currentScene != adapter.ScenePath || state == null) throw new InvalidOperationException("Wrong scene loaded");
                adapter.Restore(state);
                _runId = _pending.runId;
                _visited.Clear();
                foreach (var visited in _pending.scenes) _visited.Add(visited.path, visited);
                _pending = null;
                _entryScene = adapter.ScenePath;
                _entryCheckpointJson = JsonUtility.ToJson(adapter.Capture());
                Busy = false;
                Report("Game loaded");
                var complete = _loadCompletion; _loadCompletion = null;
                adapter.BlockInput(false);
                complete?.Invoke(true, null);
            }
            else
            {
                if (_visited.TryGetValue(adapter.ScenePath, out var previous)) adapter.Restore(previous);
                _entryScene = adapter.ScenePath;
                _entryCheckpointJson = JsonUtility.ToJson(adapter.Capture());
                if (!IntroTransitionInProgress) adapter.BlockInput(false);
                if (IntroTransitionInProgress) Busy = false;
                Save(0, out _);
                if (IntroTransitionInProgress) { Busy = true; adapter.BlockInput(true); }
            }
        }
        catch (Exception e)
        {
            if (_retryPending) { _retryError = e.Message; _retryPending = false; Report("Retry restore failed: " + e.Message); }
            else if (_pending != null) FailLoad(e.Message);
            else { adapter.BlockInput(false); Report("Save unavailable: " + e.Message); }
        }
    }
    public bool Save(int slot, out string error)
    {
        if (Busy || _adapter == null || !_adapter.Ready) { error = "Gameplay is not ready to save"; return false; }
        Busy = true;
        try
        {
            Report("Saving...");
            _visited[_adapter.ScenePath] = _adapter.Capture();
            var data = new SaveGameData { runId = _runId, slot = slot, currentScene = _adapter.ScenePath, scenes = _visited.Values.ToList() };
            bool success = Repository.Commit(data, out error);
            Report(success ? (slot == 0 ? "Autosaved" : "Game saved to slot " + slot) : "Save failed: " + error);
            return success;
        }
        catch (Exception e) { error = e.Message; Report("Save failed: " + error); return false; }
        finally { Busy = false; Changed?.Invoke(); }
    }
    public bool BeginFreshRun(out string error)
    {
        if (Busy) { error = "Another operation is in progress"; return false; }
        if (!Repository.ResetAutosave(out error)) { Report("Could not reset autosave: " + error); return false; }
        _runId = Guid.NewGuid().ToString("N");
        _visited.Clear(); _pending = null; _adapter = null;
        Report("Starting new game");
        return true;
    }
    public bool Load(int slot, Action<bool, string> completion = null)
    {
        if (Busy) return false;
        var info = Repository.Read(slot);
        if (!info.Valid) { Report(info.Error); completion?.Invoke(false, info.Error); return false; }
        if (PixeLadder.EasyTransition.SceneTransitioner.Instance != null && PixeLadder.EasyTransition.SceneTransitioner.Instance.IsTransitioning) return false;
        _pending = info.Data; _loadCompletion = completion;
        Busy = true;
        Report("Loading...");
        _loadRoutine = StartCoroutine(LoadRoutine(info.Data.currentScene));
        return true;
    }
    private IEnumerator LoadRoutine(string path)
    {
        AsyncOperation operation = null;
        string error = null;
        try { Time.timeScale = 1f; operation = SceneManager.LoadSceneAsync(path); }
        catch (Exception e) { error = e.Message; }
        if (operation == null) { FailLoad(error ?? "Could not start scene loading"); yield break; }
        while (!operation.isDone) yield return null;
        float deadline = Time.realtimeSinceStartup + 10f;
        while (_pending != null && Time.realtimeSinceStartup < deadline) yield return null;
        if (_pending != null) FailLoad("Gameplay save adapter did not become ready");
        _loadRoutine = null;
    }
    private void FailLoad(string error)
    {
        _pending = null; Busy = false;
        var completion = _loadCompletion; _loadCompletion = null;
        Report("Load failed: " + error);
        if (SceneManager.GetActiveScene().name != "MainMenu") SceneManager.LoadScene("MainMenu");
        completion?.Invoke(false, error);
    }
    public bool ExitToMenu(bool save, out string error)
    {
        if (Busy) { error = "Another operation is in progress"; return false; }
        if (!Application.CanStreamedLevelBeLoaded("MainMenu")) { error = "MainMenu is missing or disabled"; return false; }
        if (save && !Save(0, out error)) return false;
        try
        {
            Busy = true;
            GameSettings.Save();
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            SceneManager.LoadScene("MainMenu");
            error = null; return true;
        }
        catch (Exception e) { error = e.Message; Time.timeScale = 0f; return false; }
        finally { Busy = false; }
    }
    private string _entryCheckpointJson;
    private string _entryScene;
    private bool _retryPending;
    private bool _retryRestored;
    private string _retryError;
    private DeathRetryTransition _retryTransition;
    public bool EncounterInProgress { get; private set; }
    public bool HasEntryCheckpoint(string scenePath) => _entryScene == scenePath && !string.IsNullOrEmpty(_entryCheckpointJson);

    public bool BeginEncounter(string scenePath)
    {
        if (Busy || EncounterInProgress || _adapter == null || !_adapter.Ready || !HasEntryCheckpoint(scenePath)) return false;
        EncounterInProgress = true;
        Busy = true;
        return true;
    }
    public void CancelEncounter()
    {
        if (_retryPending) return;
        EncounterInProgress = false;
        Busy = false;
        Changed?.Invoke();
    }
    public bool RetrySceneEntry(out string error) => RetrySceneEntry(null, null, 0, 0, 0, out error);

    public bool RetrySceneEntry(UIAlphaFadeEffect effect, UnityEngine.UI.Image imagePrefab,
        float fadeOut, float fadeIn, float blackHold, out string error)
    {
        if (!EncounterInProgress || _retryPending || _retryTransition != null || _adapter == null || !HasEntryCheckpoint(_adapter.ScenePath))
        { error = "No current scene-entry checkpoint is available"; return false; }
        if (PixeLadder.EasyTransition.SceneTransitioner.Instance != null && PixeLadder.EasyTransition.SceneTransitioner.Instance.IsTransitioning)
        { error = "Another scene transition is already running"; return false; }
        if (!Application.CanStreamedLevelBeLoaded(_entryScene)) { error = "The current level is missing or disabled"; return false; }
        if (effect != null && (imagePrefab == null || effect.transitionMaterial == null))
        { error = "Death retry fade is missing its Image prefab or material"; return false; }
        var checkpoint = JsonUtility.FromJson<SavedSceneState>(_entryCheckpointJson);
        error = _adapter.ValidateObjects(checkpoint);
        if (error != null) return false;
        if (effect != null)
        {
            var overlay = new GameObject("DeathRetryTransition");
            _retryTransition = overlay.AddComponent<DeathRetryTransition>();
            try { _retryTransition.Initialize(effect, imagePrefab, fadeOut, fadeIn, blackHold); }
            catch (Exception e)
            {
                _retryTransition.Dispose(); _retryTransition = null;
                error = "Could not initialize death fade: " + e.Message; return false;
            }
        }
        _retryPending = true; _retryRestored = false; _retryError = null;
        StartCoroutine(RetryEntryRoutine(_entryScene));
        return true;
    }
    private IEnumerator RetryEntryRoutine(string path)
    {
        bool hasFade = _retryTransition != null;
        if (hasFade) yield return _retryTransition.FadeOut();
        string failure = hasFade && (_retryTransition == null || !_retryTransition.IsUsable) ? "Death fade was interrupted" : null;
        AsyncOperation operation = null;
        if (failure == null)
        {
            try { Time.timeScale = 1f; operation = SceneManager.LoadSceneAsync(path); }
            catch (Exception e) { failure = e.Message; }
            if (operation != null)
            {
                while (!operation.isDone) yield return null;
                float deadline = Time.realtimeSinceStartup + 15f;
                while (_retryPending && !_retryRestored && Time.realtimeSinceStartup < deadline) yield return null;
                if (!_retryRestored) failure = _retryError ?? "Level checkpoint did not become ready";
            }
            else failure = failure ?? "Could not reload the current level";
        }
        if (failure == null && hasFade)
        {
            if (_retryTransition == null || !_retryTransition.IsUsable) failure = "Death fade was interrupted before reveal";
            else
            {
                yield return _retryTransition.FadeIn();
                if (_retryTransition == null || !_retryTransition.IsUsable) failure = "Death fade was interrupted during reveal";
            }
        }
        if (_retryTransition != null) _retryTransition.Dispose();
        _retryTransition = null;
        _retryPending = false; EncounterInProgress = false; Busy = false;
        if (_adapter != null) _adapter.BlockInput(false);
        Report(failure == null ? "Level restarted at entry checkpoint" : "Retry failed: " + failure);
    }

    // Any future gameplay travel must use this boundary to capture before unloading.
    public bool TravelToGameplay(string path, out string error)
    {
        if (Busy || !SaveGameData.SupportedScene(path) || !Application.CanStreamedLevelBeLoaded(path)) { error = "Invalid gameplay transition"; return false; }
        try { if (_adapter != null) _visited[_adapter.ScenePath] = _adapter.Capture(); SceneManager.LoadScene(path); error = null; return true; }
        catch (Exception e) { error = e.Message; return false; }
    }
}
