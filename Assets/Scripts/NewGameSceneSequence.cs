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
    public static bool BeginGameplayCinematic(UIAlphaFadeEffect effect, UnityEngine.UI.Image image,
        string cinematic, string destination, float fadeOut, float fadeIn, float blackHold, out string error)
    {
        error = null;
        var root = new GameObject("Gameplay Cinematic Sequence");
        var sequence = root.AddComponent<NewGameSceneSequence>();
        sequence._fadeEffect = effect;
        sequence._fadeImagePrefab = image;
        sequence._introScene = cinematic;
        sequence._gameplayScene = destination;
        sequence._fadeOutSeconds = Mathf.Max(0, fadeOut);
        sequence._fadeInSeconds = Mathf.Max(0, fadeIn);
        sequence._blackHoldSeconds = Mathf.Max(0, blackHold);
        if (!sequence.Validate()) { error = "Cinematic destinations, fade or operation state are invalid."; Destroy(root); return false; }
        if (!GameSaveManager.Instance.BeginGameplayCinematic(cinematic, destination, out error)) { Destroy(root); return false; }
        try { GameSettings.Save(); sequence.StartIntro(false); return true; }
        catch (System.Exception e)
        {
            error = e.Message;
            GameSaveManager.Instance.EndIntroTransition();
            Destroy(root);
            return false;
        }
    }
    public static void BeginDirectIntro(UIAlphaFadeEffect effect, UnityEngine.UI.Image image,
        string cinematic = "Assets/Scenes/VideoIntro.unity", string destination = "Assets/Scenes/Neighborhood Scene.unity")
    {
        if (IntroActive) return;
        var root = new GameObject("Direct Video Intro Sequence");
        var sequence = root.AddComponent<NewGameSceneSequence>();
        sequence._fadeEffect = effect;
        sequence._fadeImagePrefab = image;
        sequence._introScene = cinematic;
        sequence._gameplayScene = destination;
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
        _overlay.Initialize((UIAlphaFadeEffect)_fadeEffect, _fadeImagePrefab, _fadeOutSeconds, _fadeInSeconds, _blackHoldSeconds, direct);
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
        if (!direct)
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
        while (_failure == null && (_readyAdapter == null || !GameSaveManager.Instance.IsCinematicGameplayReady(_readyAdapter)) && Time.realtimeSinceStartup < deadline) yield return null;
        if (_readyAdapter == null || !GameSaveManager.Instance.IsCinematicGameplayReady(_readyAdapter))
            _failure = _failure ?? "Gameplay initialization or world restoration failed.";
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
