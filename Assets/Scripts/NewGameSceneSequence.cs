using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class NewGameSceneSequence : MonoBehaviour
{
    [Header("Intro Fade")]
    [SerializeField] private PixeLadder.EasyTransition.TransitionEffect _fadeEffect;
    [SerializeField] private UnityEngine.UI.Image _fadeImagePrefab;
    [SerializeField, Min(0f)] private float _fadeOutSeconds = 1f;
    [SerializeField, Min(0f)] private float _fadeInSeconds = 1f;
    [SerializeField, Min(0f)] private float _blackHoldSeconds = 0.2f;
    [Header("Scenes")]
    [SerializeField] private string _introScene = "Assets/Scenes/VideoIntro.unity";
    [SerializeField] private string _gameplayScene = "Assets/Scenes/Neighborhood Scene.unity";
    [SerializeField, Min(1f)] private float _readinessTimeout = 30f;
    private DeathRetryTransition _overlay;
    private GameplaySaveAdapter _readyAdapter;
    private string _failure;
    public bool IsRunning { get; private set; }
    public static bool IntroActive { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => IntroActive = false;

    public void BeginNewGame()
    {
        if (!Validate()) return;
        if (GameSaveManager.Instance == null || !GameSaveManager.Instance.BeginFreshRun(out _)) return;
        GameSettings.Save();
        StartIntro(false);
    }
    public static void BeginDirectIntro(UIAlphaFadeEffect effect, UnityEngine.UI.Image image)
    {
        if (IntroActive) return;
        var root = new GameObject("Direct Video Intro Sequence");
        var sequence = root.AddComponent<NewGameSceneSequence>();
        sequence._fadeEffect = effect;
        sequence._fadeImagePrefab = image;
        if (!sequence.Validate()) { Destroy(root); return; }
        sequence.StartIntro(true);
    }
    private bool Validate()
    {
        if (IsRunning || IntroActive) return false;
        var transitioner = PixeLadder.EasyTransition.SceneTransitioner.Instance;
        if (transitioner != null && transitioner.IsTransitioning) return false;
        if (!(_fadeEffect is UIAlphaFadeEffect) || _fadeEffect.transitionMaterial == null || _fadeImagePrefab == null)
        { Debug.LogError("Intro requires the UI fade effect, material and Image prefab.", this); return false; }
        if (!Application.CanStreamedLevelBeLoaded(_introScene) || !Application.CanStreamedLevelBeLoaded(_gameplayScene))
        { Debug.LogError("VideoIntro or Neighborhood is missing from the build scene list.", this); return false; }
        return GameSaveManager.Instance != null && !GameSaveManager.Instance.Busy;
    }
    private void StartIntro(bool direct)
    {
        IsRunning = true;
        IntroActive = true;
        DontDestroyOnLoad(gameObject);
        GameSaveManager.Instance.BeginIntroTransition();
        GameplaySaveAdapter.SceneReady += OnGameplayReady;
        var root = new GameObject("Intro Fade Overlay");
        _overlay = root.AddComponent<DeathRetryTransition>();
        _overlay.Initialize((UIAlphaFadeEffect)_fadeEffect, _fadeImagePrefab, _fadeOutSeconds, _fadeInSeconds, _blackHoldSeconds);
        StartCoroutine(PlaySequence(direct));
    }
    private void OnGameplayReady(GameplaySaveAdapter adapter)
    {
        if (adapter.ScenePath == _gameplayScene) { _readyAdapter = adapter; adapter.BlockInput(true); }
    }
    private IEnumerator Load(string path)
    {
        AsyncOperation op = null;
        try { op = SceneManager.LoadSceneAsync(path); }
        catch (System.Exception e) { _failure = e.Message; }
        if (op == null) { _failure = _failure ?? "Scene loading could not start."; yield break; }
        yield return op;
    }
    private IEnumerator PlaySequence(bool direct)
    {
        Time.timeScale = 1f;
        if (direct)
        {
            // Zero-length blackening hides preparation without consuming the opening.
            var image = _overlay.GetComponentInChildren<UnityEngine.UI.Image>();
            if (image != null) image.color = Color.black;
        }
        else
        {
            yield return _overlay.FadeOut();
            yield return Load(_introScene);
        }
        float deadline = Time.realtimeSinceStartup + _readinessTimeout;
        while (_failure == null && (VideoIntroController.Instance == null || !VideoIntroController.Instance.Ready) && Time.realtimeSinceStartup < deadline)
        {
            if (VideoIntroController.Instance != null && VideoIntroController.Instance.Error != null) _failure = VideoIntroController.Instance.Error;
            yield return null;
        }
        var video = VideoIntroController.Instance;
        if (_failure == null && (video == null || !video.Ready)) _failure = "Video did not become ready.";
        if (_failure != null) { yield return Recover(); yield break; }
        yield return _overlay.FadeIn();
        video.PlayVideo();
        // Hold the first frame during the reveal, then play the complete clip with audio.
        while (video != null && !video.Completed && video.Error == null && SceneManager.GetActiveScene().path == _introScene) yield return null;
        if (video == null || !video.Completed) { _failure = video != null ? video.Error : "Video playback was interrupted."; yield return Recover(); yield break; }
        yield return _overlay.FadeOut();
        yield return Load(_gameplayScene);
        deadline = Time.realtimeSinceStartup + _readinessTimeout;
        while (_failure == null && _readyAdapter == null && Time.realtimeSinceStartup < deadline) yield return null;
        if (_readyAdapter == null || !GameSaveManager.Instance.HasEntryCheckpoint(_gameplayScene))
            _failure = _failure ?? "Gameplay initialization failed.";
        if (_failure != null) { yield return Recover(); yield break; }
        yield return _overlay.FadeIn();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Finish();
    }
    private IEnumerator Recover()
    {
        Debug.LogError("Intro failed: " + _failure, this);
        GameSaveManager.Instance.Report("Intro failed: " + _failure);
        if (Application.CanStreamedLevelBeLoaded("MainMenu")) yield return Load("MainMenu");
        if (_overlay != null) yield return _overlay.FadeIn();
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        Finish();
    }
    private void Finish()
    {
        if (GameSaveManager.Instance != null) GameSaveManager.Instance.EndIntroTransition();
        IsRunning = false; IntroActive = false;
        if (_overlay != null) _overlay.Dispose();
        Destroy(gameObject);
    }
    private void OnDestroy()
    {
        GameplaySaveAdapter.SceneReady -= OnGameplayReady;
        if (IsRunning)
        {
            IntroActive = false;
            if (GameSaveManager.Instance != null) GameSaveManager.Instance.EndIntroTransition();
        }
        if (_overlay != null) _overlay.Dispose();
    }
}
