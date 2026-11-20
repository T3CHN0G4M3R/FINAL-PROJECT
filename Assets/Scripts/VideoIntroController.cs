using System.Collections;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public sealed class VideoIntroController : MonoBehaviour
{
    public static VideoIntroController Instance { get; private set; }
    [SerializeField] private UnityEngine.UI.RawImage _display;
    [SerializeField, Tooltip("The visible pause UI panel controlled by PauseMenuManager.")]
    private GameObject _pauseMenuRoot;
    private bool _playbackRequested;
    private bool _pausedForMenu;
    [SerializeField] private UIAlphaFadeEffect _fadeEffect;
    [SerializeField] private UnityEngine.UI.Image _fadeImagePrefab;
    [SerializeField, Min(1f)] private float _prepareTimeout = 20f;
    [Header("Direct Playback Route")]
    [SerializeField] private string _gameplayScene = "Assets/Scenes/Neighborhood Scene.unity";
    private VideoPlayer _player;
    private UnityEngine.InputSystem.Keyboard _keyboard;
    private UnityEngine.InputSystem.InputAction _skipAction;
    public bool Ready { get; private set; }
    public bool Completed { get; private set; }
    public string Error { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => Instance = null;
    private void Awake()
    {
        Instance = this;
        _player = GetComponent<VideoPlayer>();
        _player.playOnAwake = false;
        _player.isLooping = false;
        _player.waitForFirstFrame = true;
        _player.sendFrameReadyEvents = true;
        _player.frameReady += OnFrame;
        _player.loopPointReached += OnFinished;
        _player.errorReceived += OnError;
        if (_display != null) _display.texture = _player.targetTexture;
        Mute(true);
        _skipAction = new UnityEngine.InputSystem.InputAction("DevVideoSkip", UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/numpadPlus");
    }
    private void OnEnable()
    {
        BindKeyboard();
        UnityEngine.InputSystem.InputSystem.onDeviceChange += OnDeviceChange;
        _skipAction.performed += OnSkip;
        _skipAction.Enable();
    }
    private void BindKeyboard()
    {
        if (_keyboard != null) _keyboard.onTextInput -= OnTextInput;
        _keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (_keyboard != null) _keyboard.onTextInput += OnTextInput;
    }
    private void OnDeviceChange(UnityEngine.InputSystem.InputDevice device, UnityEngine.InputSystem.InputDeviceChange change)
    {
        if (device is UnityEngine.InputSystem.Keyboard) BindKeyboard();
    }
    private void OnTextInput(char character)
    {
        if (Time.timeScale <= 0f) return;
        if (DevCodeFlyMode.HandleVideoTextInput(character)) SkipVideo();
    }
    private void OnSkip(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (Time.timeScale > 0f && DevCodeFlyMode.IsSessionUnlocked) SkipVideo();
    }
    private void SkipVideo()
    {
        if (!Ready || Completed || Error != null || !_player.isPlaying) return;
        OnFinished(_player);
    }
    private void OnDisable()
    {
        if (_keyboard != null) _keyboard.onTextInput -= OnTextInput;
        UnityEngine.InputSystem.InputSystem.onDeviceChange -= OnDeviceChange;
        _skipAction.performed -= OnSkip;
        _skipAction.Disable();
    }
    private IEnumerator Start()
    {
        if (!NewGameSceneSequence.IntroActive)
            NewGameSceneSequence.BeginDirectIntro(_fadeEffect, _fadeImagePrefab, gameObject.scene.path, _gameplayScene);
        if (_player.clip == null || _player.targetTexture == null || _display == null)
        { Error = "Intro video is missing its clip, texture or display."; yield break; }
        _player.Prepare();
        float deadline = Time.realtimeSinceStartup + _prepareTimeout;
        while (!_player.isPrepared && Error == null && Time.realtimeSinceStartup < deadline) yield return null;
        if (!_player.isPrepared) { Error = Error ?? "Video preparation timed out."; yield break; }
        _player.Play();
        while (!Ready && Error == null && Time.realtimeSinceStartup < deadline) yield return null;
        if (!Ready) Error = Error ?? "The video did not decode its first frame.";
    }
    private void OnFrame(VideoPlayer player, long frame)
    {
        if (Ready) return;
        player.Pause();
        Ready = true;
        player.sendFrameReadyEvents = false;
    }
    public void PlayVideo()
    {
        if (!Ready || Completed || Error != null) return;
        _playbackRequested = true;
        Mute(false);
        _pausedForMenu = _pauseMenuRoot != null && _pauseMenuRoot.activeInHierarchy;
        if (!_pausedForMenu) _player.Play();
    }
    private void LateUpdate()
    {
        // Leave preparation and the transition's first-frame hold untouched.
        if (!_playbackRequested || !Ready || Completed || Error != null) return;
        bool menuVisible = _pauseMenuRoot != null && _pauseMenuRoot.activeInHierarchy;
        if (menuVisible)
        {
            if (_player.isPlaying) _player.Pause();
            _pausedForMenu = true;
        }
        else if (_pausedForMenu)
        {
            _pausedForMenu = false;
            _player.Play();
        }
    }
    private void Mute(bool mute)
    {
        for (ushort track = 0; track < _player.audioTrackCount; track++)
            _player.SetDirectAudioMute(track, mute);
    }
    private void OnFinished(VideoPlayer player) { player.Pause(); Mute(true); Completed = true; }
    private void OnError(VideoPlayer player, string message) { Error = message; Debug.LogError("Intro video: " + message, this); }
    private void OnDestroy()
    {
        _skipAction?.Dispose();
        if (Instance == this) Instance = null;
        if (_player == null) return;
        _player.frameReady -= OnFrame;
        _player.loopPointReached -= OnFinished;
        _player.errorReceived -= OnError;
    }
}
