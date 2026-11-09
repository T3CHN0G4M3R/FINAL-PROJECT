using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class NewGameSceneSequence : MonoBehaviour
{
    [Header("Easy Transition")]
    [SerializeField] private PixeLadder.EasyTransition.SceneTransitioner _transitioner;
    [SerializeField] private PixeLadder.EasyTransition.TransitionEffect _fadeEffect;
    [SerializeField, Min(0f)] private float _fadeOutSeconds = 1f;
    [SerializeField, Min(0f)] private float _fadeInSeconds = 1f;
    [SerializeField, Min(0f)] private float _blackHoldSeconds = 0.2f;

    [Header("Intro Timing")]
    [Tooltip("Time each Dog scene stays fully visible AFTER fade-in, before the next fade-out. Uses real-time seconds.")]
    [SerializeField, Min(0f)] private float _dogSceneDurationSeconds = 3f;

    [Header("Scene Order")]
    [Tooltip("Scene names or full build scene paths, in playback order.")]
    [SerializeField] private string[] _dogScenes =
    {
        "Assets/Scenes/Dog1.unity", "Assets/Scenes/Dog2.unity",
        "Assets/Scenes/Dog3.unity", "Assets/Scenes/Dog4.unity",
        "Assets/Scenes/Dog5.unity", "Assets/Scenes/Dog6.unity",
        "Assets/Scenes/Dog7.unity", "Assets/Scenes/Dog8.unity"
    };
    [SerializeField] private string _gameplayScene = "Assets/Scenes/Neighborhood Scene.unity";

    private PixeLadder.EasyTransition.TransitionEffect _runtimeFade;
    public bool IsRunning { get; private set; }

    // Wired to New Game in MainMenu; this root survives only for the intro.
    public void BeginNewGame()
    {
        if (IsRunning) return;
        if (PixeLadder.EasyTransition.SceneTransitioner.Instance != null)
            _transitioner = PixeLadder.EasyTransition.SceneTransitioner.Instance;

        if (_transitioner == null || !_transitioner.isActiveAndEnabled || !(_fadeEffect is UIAlphaFadeEffect) || _fadeEffect.transitionMaterial == null)
        {
            Debug.LogWarning("New Game requires an active Easy Transition manager and a UI Alpha Fade with a material.", this);
            return;
        }
        if (_transitioner.IsTransitioning) return;
        if (_dogScenes == null || _dogScenes.Length == 0 || !CanLoad(_gameplayScene)) return;
        foreach (string scene in _dogScenes)
            if (!CanLoad(scene)) return;

        if (GameSaveManager.Instance == null || !GameSaveManager.Instance.BeginFreshRun(out _)) return;
        GameSettings.Save();
        IsRunning = true;
        DontDestroyOnLoad(gameObject);
        _runtimeFade = Instantiate(_fadeEffect);
        ((UIAlphaFadeEffect)_runtimeFade).Configure(_fadeOutSeconds, _fadeInSeconds, _blackHoldSeconds);
        StartCoroutine(PlaySequence());
    }

    private bool CanLoad(string scene)
    {
        if (!string.IsNullOrWhiteSpace(scene) && Application.CanStreamedLevelBeLoaded(scene)) return true;
        Debug.LogError($"New Game scene '{scene}' is missing or disabled in the active build scene list.", this);
        return false;
    }

    private IEnumerator PlaySequence()
    {
        foreach (string scene in _dogScenes)
        {
            _transitioner.LoadScene(scene, _runtimeFade);
            while (_transitioner != null && _transitioner.IsTransitioning) yield return null;
            if (_transitioner == null || !IsActiveScene(scene))
            {
                FinishSequence();
                yield break;
            }

            float visibleUntil = Time.realtimeSinceStartup + Mathf.Max(0f, _dogSceneDurationSeconds);
            while (Time.realtimeSinceStartup < visibleUntil)
            {
                if (_transitioner == null || !_transitioner.isActiveAndEnabled || !IsActiveScene(scene))
                {
                    FinishSequence();
                    yield break;
                }
                yield return null;
            }
        }

        if (_transitioner == null || !_transitioner.isActiveAndEnabled)
        {
            FinishSequence();
            yield break;
        }
        _transitioner.LoadScene(_gameplayScene, _runtimeFade);
        while (_transitioner != null && _transitioner.IsTransitioning) yield return null;
        if (IsActiveScene(_gameplayScene))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        FinishSequence();
    }

    private static bool IsActiveScene(string scene)
    {
        Scene active = SceneManager.GetActiveScene();
        return active.path == scene || active.name == scene;
    }

    private void FinishSequence()
    {
        IsRunning = false;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (_runtimeFade != null) Destroy(_runtimeFade);
    }
}
