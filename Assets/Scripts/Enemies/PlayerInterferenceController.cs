using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class PlayerInterferenceController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _enemy;
    [SerializeField] private GameplaySaveAdapter _saveAdapter;
    [SerializeField] private InteractionSystem _interactions;
    [SerializeField] private GameplaySettingsApplier _hud;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _interferenceClip;
    [SerializeField] private UnityEngine.Rendering.Volume _originalVolume;
    [SerializeField] private UnityEngine.Rendering.Volume _dangerVolume;
    [SerializeField] private UnityEngine.UI.Image _blackoutImage;
    [Tooltip("Visible only if interference initialization/retry fails.")]
    [SerializeField] private TMPro.TextMeshProUGUI _errorText;

    [Header("Interference Audio")]
    [Tooltip("Quiet static always heard by the player, including after Radio use.")]
    [Range(0, 1)] [SerializeField] private float _baselineVolume = 0.08f;
    [Tooltip("Volume at which distortion and blackout begin. Must exceed baseline.")]
    [Range(0, 1)] [SerializeField] private float _maximumVolume = 0.8f;
    [Tooltip("3D distance from Player to Siren Head root. Walls do not block interference.")]
    [Min(0.01f)] [SerializeField] private float _enemyInfluenceRadius = 20f;
    [Tooltip("Accumulated seconds inside the radius to reach maximum. Leaving holds progress.")]
    [Min(0.01f)] [SerializeField] private float _timeToMaximum = 30f;

    [Header("Blackout and Checkpoint Retry")]
    [Tooltip("Seconds from maximum volume to full black. Radio cancels it before commitment.")]
    [Min(0.01f)] [SerializeField] private float _blackoutDuration = 5f;
    [SerializeField] private UIAlphaFadeEffect _retryFadeEffect;
    [SerializeField] private UnityEngine.UI.Image _retryImagePrefab;
    [Min(0)] [SerializeField] private float _fadeInSeconds = 1f;
    [Min(0)] [SerializeField] private float _blackHoldSeconds = 0.2f;

    public float Exposure => _exposure;
    public float BlackoutAlpha => _blackoutImage != null ? _blackoutImage.color.a : 0;
    public float CurrentVolume => _audioSource != null ? _audioSource.volume : 0;
    public bool RetryCommitted => _retryCommitted;
    public Transform Player => _player;
    public string Failure { get; private set; }
    public bool CanUseRadio => isActiveAndEnabled && _initialized && !_retryCommitted && Failure == null && GameplayAvailable;

    private DevCodeFlyMode _playerDevCode;
    private bool DevCodeProtected => _playerDevCode != null && _playerDevCode.isActiveAndEnabled && _playerDevCode.isDevCodeUnlocked;

    private void Awake()
    {
        if (_player != null) _playerDevCode = _player.GetComponent<DevCodeFlyMode>();
    }

    private float _exposure;
    private float _blackoutElapsed;
    private int _resetFrame = -1;
    private bool _initialized, _retryCommitted, _focused = true;
    private bool _originalEnabled, _dangerEnabled;
    private float _originalWeight, _dangerWeight;

    private bool GameplayAvailable
    {
        get
        {
            var manager = GameSaveManager.Instance;
            return _focused && !PauseMenuManager.IsGamePaused && Time.timeScale > 0 &&
                _player != null && _player.gameObject.activeInHierarchy && _enemy != null && _enemy.gameObject.activeInHierarchy &&
                _saveAdapter != null && _saveAdapter.Ready && _interactions != null && !_interactions.PersistenceInputBlocked &&
                manager != null && !manager.Busy && !manager.EncounterInProgress && !manager.IntroTransitionInProgress &&
                manager.HasEntryCheckpoint(gameObject.scene.path);
        }
    }

    private void OnEnable()
    {
        _focused = true;
        Failure = null;
        _retryCommitted = false;
        ValidateTuning();
        if (_player == null || _enemy == null || _saveAdapter == null || _interactions == null ||
            _audioSource == null || _interferenceClip == null || _originalVolume == null || _dangerVolume == null ||
            _originalVolume == _dangerVolume || _originalVolume.sharedProfile == null || _dangerVolume.sharedProfile == null ||
            _blackoutImage == null || _retryFadeEffect == null || _retryImagePrefab == null || _retryFadeEffect.transitionMaterial == null)
        {
            Fail("Interference needs its Player/enemy, audio, two distinct Volume profiles, blackout Image and retry references.");
            return;
        }
        _originalEnabled = _originalVolume.enabled;
        _dangerEnabled = _dangerVolume.enabled;
        _originalWeight = _originalVolume.weight;
        _dangerWeight = _dangerVolume.weight;
        _initialized = true;
        _audioSource.clip = _interferenceClip;
        _audioSource.loop = true;
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0;
        _audioSource.volume = _baselineVolume;
        _exposure = 0;
        _blackoutElapsed = 0;
        ApplyView(false, 0);
        if (_errorText != null) _errorText.text = "";
        _audioSource.Play();
    }

    private void LateUpdate()
    {
        if (!_initialized || Failure != null) return;
        var manager = GameSaveManager.Instance;
        if (manager != null && !manager.Busy && manager.Status != null && manager.Status.StartsWith("Retry failed:", System.StringComparison.Ordinal))
        {
            _retryCommitted = false;
            Fail(manager.Status);
            return;
        }
        if (_retryCommitted) return;
        if (DevCodeProtected)
        {
            _exposure = 0;
            _blackoutElapsed = 0;
            if (_audioSource != null) _audioSource.volume = _baselineVolume;
            ApplyView(false, 0);
            return;
        }
        if (manager != null && (manager.EncounterInProgress || manager.IntroTransitionInProgress))
        {
            // Another sequence owns the screen. Do not cancel its encounter or input lock.
            ApplyView(false, 0);
            return;
        }
        if (_player == null || _enemy == null || _audioSource == null || _blackoutImage == null ||
            _originalVolume == null || _dangerVolume == null)
        {
            Fail("An interference scene reference was removed during gameplay.");
            return;
        }
        if (!GameplayAvailable || Time.frameCount == _resetFrame) return;
        if ((_player.position - _enemy.position).sqrMagnitude <= _enemyInfluenceRadius * _enemyInfluenceRadius)
            _exposure = Mathf.Clamp01(_exposure + Time.deltaTime / _timeToMaximum);
        _audioSource.volume = Mathf.Lerp(_baselineVolume, _maximumVolume, _exposure);
        bool danger = _exposure >= 1;
        if (danger) _blackoutElapsed = Mathf.Min(_blackoutDuration, _blackoutElapsed + Time.deltaTime);
        ApplyView(danger, danger ? _blackoutElapsed / _blackoutDuration : 0);
        if (danger && _blackoutElapsed >= _blackoutDuration) CommitRetry();
    }

    private void CommitRetry()
    {
        if (DevCodeProtected) return;
        var manager = GameSaveManager.Instance;
        if (manager == null || !manager.BeginEncounter(gameObject.scene.path)) return;
        bool accepted = false;
        string error;
        try
        {
            accepted = manager.RetrySceneEntry(_retryFadeEffect, _retryImagePrefab, 0, _fadeInSeconds,
                _blackHoldSeconds, out error, true);
        }
        catch (System.Exception exception) { error = exception.Message; }
        if (!accepted)
        {
            // We own this newly claimed encounter; no other sequence's locks are touched.
            manager.CancelEncounter();
            Fail("Could not restart at the entry checkpoint: " + error);
            return;
        }
        _retryCommitted = true;
        // The persistent overlay is now already opaque and owns loading/reveal.
        _blackoutImage.color = new Color(0, 0, 0, 0);
        if (_hud != null) _hud.RefreshHUD();
    }

    public bool ResetInterference()
    {
        if (!CanUseRadio) return false;
        _exposure = 0;
        _blackoutElapsed = 0;
        _resetFrame = Time.frameCount;
        _audioSource.volume = _baselineVolume;
        ApplyView(false, 0);
        return true;
    }

    private void ApplyView(bool danger, float alpha)
    {
        if (_originalVolume != null) { _originalVolume.enabled = !danger; _originalVolume.weight = danger ? 0 : 1; }
        if (_dangerVolume != null) { _dangerVolume.enabled = danger; _dangerVolume.weight = danger ? 1 : 0; }
        if (_blackoutImage != null) { _blackoutImage.color = new Color(0, 0, 0, Mathf.Clamp01(alpha)); _blackoutImage.raycastTarget = false; }
    }

    private void Fail(string message)
    {
        Failure = message;
        ApplyView(false, 0);
        if (_audioSource != null) _audioSource.volume = _baselineVolume;
        if (_errorText != null) _errorText.text = "Interference unavailable: " + message;
        if (GameSaveManager.Instance != null) GameSaveManager.Instance.Report(message);
        Debug.LogError(message, this);
    }

    private void OnApplicationFocus(bool focused) => _focused = focused;
    private void OnDisable()
    {
        if (_audioSource != null) _audioSource.Stop();
        if (_blackoutImage != null) _blackoutImage.color = new Color(0, 0, 0, 0);
        if (_initialized)
        {
            if (_originalVolume != null) { _originalVolume.enabled = _originalEnabled; _originalVolume.weight = _originalWeight; }
            if (_dangerVolume != null) { _dangerVolume.enabled = _dangerEnabled; _dangerVolume.weight = _dangerWeight; }
        }
        _initialized = false;
    }

    private void OnValidate() => ValidateTuning();
    private void ValidateTuning()
    {
        _maximumVolume = Mathf.Clamp(FiniteOr(_maximumVolume, 0.8f), 0.001f, 1);
        _baselineVolume = Mathf.Clamp(FiniteOr(_baselineVolume, 0.08f), 0, _maximumVolume - 0.001f);
        _enemyInfluenceRadius = Mathf.Max(0.01f, FiniteOr(_enemyInfluenceRadius, 20));
        _timeToMaximum = Mathf.Max(0.01f, FiniteOr(_timeToMaximum, 30));
        _blackoutDuration = Mathf.Max(0.01f, FiniteOr(_blackoutDuration, 5));
        _fadeInSeconds = Mathf.Max(0, FiniteOr(_fadeInSeconds, 1));
        _blackHoldSeconds = Mathf.Max(0, FiniteOr(_blackHoldSeconds, 0.2f));
    }
    private static float FiniteOr(float value, float fallback) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
}
