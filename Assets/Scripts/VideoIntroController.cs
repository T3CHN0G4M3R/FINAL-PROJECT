using System.Collections;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public sealed class VideoIntroController : MonoBehaviour
{
    public static VideoIntroController Instance { get; private set; }
    [SerializeField] private UnityEngine.UI.RawImage _display;
    [SerializeField] private UIAlphaFadeEffect _fadeEffect;
    [SerializeField] private UnityEngine.UI.Image _fadeImagePrefab;
    [SerializeField, Min(1f)] private float _prepareTimeout = 20f;
    private VideoPlayer _player;
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
    }
    private IEnumerator Start()
    {
        if (!NewGameSceneSequence.IntroActive)
            NewGameSceneSequence.BeginDirectIntro(_fadeEffect, _fadeImagePrefab);
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
        Mute(false);
        _player.Play();
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
        if (Instance == this) Instance = null;
        if (_player == null) return;
        _player.frameReady -= OnFrame;
        _player.loopPointReached -= OnFinished;
        _player.errorReceived -= OnError;
    }
}
