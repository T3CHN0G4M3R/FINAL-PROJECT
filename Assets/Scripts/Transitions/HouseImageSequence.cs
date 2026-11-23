using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class HouseImageSequence : MonoBehaviour
{
    [Header("Image Fade")]
    [SerializeField] private CanvasGroup _imageGroup;
    [SerializeField, Min(0f)] private float _fadeInSeconds = 1f;
    [SerializeField, Min(0f)] private float _displaySeconds = 5f;
    [SerializeField, Min(0f)] private float _fadeOutSeconds = 1f;

    [Header("Destination")]
    [SerializeField] private string _mainMenuScene = "Assets/Scenes/MainMenu.unity";

    private void Awake()
    {
        // This is a full-screen ending card, not camera-space gameplay UI.
        // Camera-space canvases can be obscured by the incoming scene fade.
        var canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
        }
        if (_imageGroup != null) _imageGroup.alpha = 0f;
    }

    private IEnumerator Start()
    {
        if (_imageGroup == null || !Application.CanStreamedLevelBeLoaded(_mainMenuScene))
        {
            Debug.LogError("House image sequence requires an image group and a MainMenu scene included in the build.", this);
            yield break;
        }

        // The incoming cinematic owns a persistent black overlay until its handoff finishes.
        // Do not consume the ending card's display time underneath that overlay.
        while (NewGameSceneSequence.IntroActive) yield return null;
        yield return Fade(0f, 1f, _fadeInSeconds);
        yield return new WaitForSecondsRealtime(_displaySeconds);
        yield return Fade(1f, 0f, _fadeOutSeconds);
        // Keep the opaque black background visible until the destination is ready.
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        AsyncOperation operation = null;
        try { operation = SceneManager.LoadSceneAsync(_mainMenuScene, LoadSceneMode.Single); }
        catch (System.Exception exception) { Debug.LogError("Could not load MainMenu: " + exception.Message, this); }
        if (operation == null) yield break;
        yield return operation;
    }

    private IEnumerator Fade(float start, float end, float seconds)
    {
        _imageGroup.alpha = start;
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime;
            _imageGroup.alpha = Mathf.Lerp(start, end, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / seconds)));
        }
        _imageGroup.alpha = end;
    }
}
