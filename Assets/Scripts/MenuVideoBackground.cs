using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public sealed class MenuVideoBackground : MonoBehaviour
{
    [SerializeField] private Animator _menuAnimator;
    [SerializeField] private GameObject _extrasMenu;
    [SerializeField] private VideoClip _mainClip;
    [SerializeField] private VideoClip _secondaryClip;
    [Header("Video Transitions")]
    [SerializeField, Min(0.01f)] private float _fadeDuration = 0.6f;

    private enum FadeState { Preparing, FadingIn, Playing, FadingOut }
    private FadeState _state;
    private VideoPlayer _player;
    private static readonly int Animate = Animator.StringToHash("Animate");

    private VideoClip DesiredClip
    {
        get
        {
            bool settings = _menuAnimator != null && _menuAnimator.GetFloat(Animate) > 0.5f;
            bool extras = _extrasMenu != null && _extrasMenu.activeInHierarchy;
            return settings && !extras ? _secondaryClip : _mainClip;
        }
    }

    private void Awake()
    {
        _player = GetComponent<VideoPlayer>();
        _player.isLooping = true;
        _player.playOnAwake = false;
        _player.audioOutputMode = VideoAudioOutputMode.None;
        _player.targetCameraAlpha = 0f;
        _player.prepareCompleted += OnPrepared;
        _player.errorReceived += OnVideoError;
    }

    private void Start()
    {
        PrepareClip(DesiredClip);
    }

    private void LateUpdate()
    {
        VideoClip desired = DesiredClip;
        if (desired == null) return;

        if (desired != _player.clip)
        {
            if (_state == FadeState.Preparing)
                PrepareClip(desired);
            else
                _state = FadeState.FadingOut;
        }
        else if (_state == FadeState.FadingOut)
        {
            // A quick return to the current menu cancels the pending clip change.
            _state = FadeState.FadingIn;
        }

        float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, _fadeDuration);
        switch (_state)
        {
            case FadeState.Preparing:
                // Keep the background black until the first decoded frame is ready.
                if (_player.isPlaying && _player.frame >= 0)
                    _state = FadeState.FadingIn;
                break;
            case FadeState.FadingIn:
                _player.targetCameraAlpha = Mathf.MoveTowards(_player.targetCameraAlpha, 1f, step);
                if (_player.targetCameraAlpha >= 1f)
                    _state = FadeState.Playing;
                break;
            case FadeState.FadingOut:
                _player.targetCameraAlpha = Mathf.MoveTowards(_player.targetCameraAlpha, 0f, step);
                if (_player.targetCameraAlpha <= 0f)
                    PrepareClip(desired);
                break;
        }
    }

    private void PrepareClip(VideoClip clip)
    {
        _player.targetCameraAlpha = 0f;
        _player.Stop();
        _state = FadeState.Preparing;
        _player.clip = clip;
        if (clip != null) _player.Prepare();
    }

    private void OnPrepared(VideoPlayer player)
    {
        player.Play();
    }

    private void OnVideoError(VideoPlayer player, string message)
    {
        Debug.LogError("Menu background video: " + message, this);
    }

    private void OnDestroy()
    {
        if (_player == null) return;
        _player.prepareCompleted -= OnPrepared;
        _player.errorReceived -= OnVideoError;
    }
}
