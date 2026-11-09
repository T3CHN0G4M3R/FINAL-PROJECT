using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerCaptureSequence : MonoBehaviour
{
    [Header("Explicit Scene References")]
    [SerializeField] private SirenHeadAI _enemy;
    [SerializeField] private GameplaySaveAdapter _saveAdapter;
    [SerializeField] private StarterAssets.StarterAssetsInputs _playerInputs;
    [SerializeField] private Animator _playerAnimator;
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private Unity.Cinemachine.CinemachineBrain _cameraBrain;
    [SerializeField] private GameplaySettingsApplier _hud;
    [Header("Death Animation")]
    [Min(0.05f)] [SerializeField] private float _deathPlaybackSpeed = 1f;
    [Min(0)] [SerializeField] private float _deathBlendTime = 0.15f;
    [Min(0)] [SerializeField] private float _postAnimationDelay = 0.5f;
    [Tooltip("Diagnostic deadline only: if the Death state never completes, recover rather than falsely report playback.")]
    [Min(5)] [SerializeField] private float _completionTimeout = 30f;
    [Header("First-Person Framing")]
    [Min(0)] [SerializeField] private float _cameraBlendTime = 0.25f;
    [Range(30, 110)] [SerializeField] private float _captureFieldOfView = 85f;
    [Tooltip("Offset in the death camera's local space from the animated player head. Clearance checks prevent ground/wall penetration.")]
    [SerializeField] private Vector3 _eyeOffset = new Vector3(0, 0.05f, 0.15f);
    private Transform _playerEye;
    public bool IsCapturing { get; private set; }
    public bool DeathAnimationFinished { get; private set; }
    public string Failure { get; private set; }
    [Header("Player Death and Retry Fade")]
    [SerializeField] private AnimationClip _playerDeathClip;
    [SerializeField] private UIAlphaFadeEffect _retryFadeEffect;
    [SerializeField] private UnityEngine.UI.Image _retryImagePrefab;
    [Min(0)] [SerializeField] private float _fadeOutSeconds = 1f;
    [Min(0)] [SerializeField] private float _fadeInSeconds = 1f;
    [Min(0)] [SerializeField] private float _blackHoldSeconds = 0.2f;
    [Header("Death Camera Clearance")]
    [SerializeField] private InteractionSystem _interactions;
    [SerializeField] private LayerMask _cameraCollisionMask = ~0;
    [Min(0.01f)] [SerializeField] private float _cameraClearance = 0.1f;
    private Quaternion _headRotationAtCapture;
    private float _animatorSpeedAtCapture;
    private int _playerStateAtCapture;
    private float _playerStateTimeAtCapture;
    private readonly RaycastHit[] _cameraHits = new RaycastHit[128];
    private Coroutine _routine;
    private bool _brainEnabled, _cursorLook, _reloading;
    private Vector3 _cameraPosition;
    private Quaternion _cameraRotation;
    private float _cameraFov;
    private static readonly int PlayerDeath = Animator.StringToHash("Base Layer.PlayerDeath");
    private static readonly int DeathPlayback = Animator.StringToHash("DeathPlayback");
    private void OnEnable() { if (_enemy != null) _enemy.CaptureRequested += TryCapture; }
    private void Start()
    {
        if (_playerAnimator != null && _playerAnimator.isHuman) _playerEye = _playerAnimator.GetBoneTransform(HumanBodyBones.Head);
        if (_enemy != null) { _enemy.CaptureRequested -= TryCapture; _enemy.CaptureRequested += TryCapture; }
    }
    private void Update()
    {
        if (_enemy == null || _saveAdapter == null) return;
        var manager = GameSaveManager.Instance;
        _enemy.EncounterReady = !IsCapturing && _saveAdapter.Ready && manager != null &&
            manager.HasEntryCheckpoint(gameObject.scene.path) && !manager.Busy;
        if (_reloading && manager != null && !manager.EncounterInProgress && !_saveAdapter.gameObject.scene.isLoaded)
            _reloading = false;
    }
    private void TryCapture(SirenHeadAI enemy)
    {
        if (IsCapturing || _saveAdapter == null || _playerCamera == null || _cameraBrain == null || !enemy.CanCapturePlayer()) return;
        if (_playerAnimator == null || !_playerAnimator.isHuman || _playerDeathClip == null || !_playerDeathClip.humanMotion ||
            !_playerAnimator.HasState(0, PlayerDeath) || _retryFadeEffect == null || _retryImagePrefab == null || _retryFadeEffect.transitionMaterial == null)
        {
            Failure = "Player capture requires a compatible PlayerDeath state/clip and configured retry fade assets";
            Debug.LogError(Failure, this);
            enabled = false;
            return;
        }
        var manager = GameSaveManager.Instance;
        if (manager == null || !manager.BeginEncounter(gameObject.scene.path)) return;
        IsCapturing = true; DeathAnimationFinished = false; Failure = null;
        _cameraPosition = _playerCamera.transform.position;
        _cameraRotation = _playerCamera.transform.rotation;
        _cameraFov = _playerCamera.fieldOfView;
        _headRotationAtCapture = _playerEye != null ? _playerEye.rotation : Quaternion.identity;
        _animatorSpeedAtCapture = _playerAnimator.speed;
        var originalState = _playerAnimator.GetCurrentAnimatorStateInfo(0);
        _playerStateAtCapture = originalState.fullPathHash;
        _playerStateTimeAtCapture = originalState.normalizedTime;
        _brainEnabled = _cameraBrain.enabled;
        _cursorLook = _playerInputs != null && _playerInputs.cursorInputForLook;
        _cameraBrain.enabled = false;
        if (_playerInputs != null)
        {
            _playerInputs.move = Vector2.zero; _playerInputs.look = Vector2.zero;
            _playerInputs.jump = false; _playerInputs.sprint = false;
            _playerInputs.cursorInputForLook = false;
        }
        _saveAdapter.BlockInput(true);
        _playerAnimator.SetFloat("Speed", 0); _playerAnimator.SetFloat("MotionSpeed", 0);
        if (_hud != null) _hud.RefreshHUD();
        enemy.BeginCapture();
        _playerAnimator.speed = 1;
        _playerAnimator.SetFloat(DeathPlayback, Mathf.Max(0.05f, _deathPlaybackSpeed));
        _playerAnimator.CrossFadeInFixedTime(PlayerDeath, _deathBlendTime, 0, 0);
        _routine = StartCoroutine(CaptureRoutine());
    }
    private IEnumerator CaptureRoutine()
    {
        float elapsed = 0;
        bool entered = false;
        float deadline = Mathf.Max(_completionTimeout, _playerDeathClip.length / Mathf.Max(0.05f, _deathPlaybackSpeed) + _deathBlendTime + 5f);
        while (elapsed < deadline)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime;
            var state = _playerAnimator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash == PlayerDeath)
            {
                entered = true;
                if (state.normalizedTime >= 1f && !_playerAnimator.IsInTransition(0)) { DeathAnimationFinished = true; break; }
            }
            else if (entered) { Recover("Player Death exited before completion"); yield break; }
        }
        if (!DeathAnimationFinished) { Recover("Player Death did not complete before its diagnostic deadline"); yield break; }
        if (_postAnimationDelay > 0) yield return new WaitForSecondsRealtime(_postAnimationDelay);
        string error;
        if (!GameSaveManager.Instance.RetrySceneEntry(_retryFadeEffect, _retryImagePrefab, _fadeOutSeconds, _fadeInSeconds, _blackHoldSeconds, out error))
        { Recover(error); yield break; }
        _reloading = true;
        while (GameSaveManager.Instance != null && GameSaveManager.Instance.EncounterInProgress) yield return null;
        if (this != null && gameObject.scene.isLoaded)
            Recover(GameSaveManager.Instance != null ? GameSaveManager.Instance.Status : "Restart coordinator unavailable");
    }
    private void LateUpdate()
    {
        if (!IsCapturing || _playerCamera == null || _playerEye == null || _enemy == null) return;
        Quaternion viewRotation = (_playerEye.rotation * Quaternion.Inverse(_headRotationAtCapture)) * _cameraRotation;
        Vector3 desired = _playerEye.position + viewRotation * _eyeOffset;
        Vector3 origin = _enemy.Player.position + Vector3.up * 0.8f;
        Vector3 delta = desired - origin;
        float distance = delta.magnitude;
        int count = distance > 0.001f ? Physics.SphereCastNonAlloc(origin, _cameraClearance, delta / distance,
            _cameraHits, distance, _cameraCollisionMask, QueryTriggerInteraction.Ignore) : 0;
        float safeDistance = distance;
        var held = _interactions != null ? _interactions.HeldObject : null;
        for (int i = 0; i < count; i++)
        {
            Transform hit = _cameraHits[i].transform;
            if (hit.IsChildOf(_enemy.Player) || hit.IsChildOf(_enemy.transform) || (held != null && hit.IsChildOf(held.transform))) continue;
            safeDistance = Mathf.Min(safeDistance, Mathf.Max(0, _cameraHits[i].distance - _cameraClearance));
        }
        if (count == _cameraHits.Length) safeDistance = 0;
        desired = distance > 0.001f ? origin + delta / distance * safeDistance : desired;
        desired.y = Mathf.Max(desired.y, _enemy.Player.position.y + _cameraClearance);
        float t = _cameraBlendTime <= 0 ? 1 : 1f - Mathf.Exp(-Time.unscaledDeltaTime / _cameraBlendTime);
        _playerCamera.transform.position = desired;
        _playerCamera.transform.rotation = Quaternion.Slerp(_playerCamera.transform.rotation, viewRotation, t);
        _playerCamera.fieldOfView = Mathf.Lerp(_playerCamera.fieldOfView, _captureFieldOfView, t);
    }
    private void Recover(string error)
    {
        Failure = error; Debug.LogError("Capture aborted: " + error, this);
        RestoreOwnership();
        if (_enemy != null) _enemy.AbortCapture();
        if (GameSaveManager.Instance != null) GameSaveManager.Instance.Report("Capture failed: " + error);
    }
    private void RestoreOwnership()
    {
        if (!IsCapturing) return;
        IsCapturing = false; _reloading = false;
        if (GameSaveManager.Instance != null) GameSaveManager.Instance.CancelEncounter();
        if (_playerAnimator != null)
        {
            _playerAnimator.speed = _animatorSpeedAtCapture;
            _playerAnimator.Play(_playerStateAtCapture, 0, _playerStateTimeAtCapture);
        }
        if (_saveAdapter != null) _saveAdapter.BlockInput(false);
        if (_playerInputs != null) { _playerInputs.cursorInputForLook = _cursorLook; _playerInputs.move = Vector2.zero; _playerInputs.look = Vector2.zero; }
        if (_playerCamera != null) { _playerCamera.transform.SetPositionAndRotation(_cameraPosition, _cameraRotation); _playerCamera.fieldOfView = _cameraFov; }
        if (_cameraBrain != null) _cameraBrain.enabled = _brainEnabled;
        if (_hud != null) _hud.RefreshHUD();
    }
    private void OnDisable()
    {
        if (_enemy != null) _enemy.CaptureRequested -= TryCapture;
        if (_routine != null) StopCoroutine(_routine);
        if (!_reloading) RestoreOwnership();
    }
}
