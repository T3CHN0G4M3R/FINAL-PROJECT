using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DeathRetryTransition : MonoBehaviour
{
    public enum TransitionPhase { FadeOut, LoadingBlack, FadeIn, Finished }
    public TransitionPhase Phase { get; private set; }
    public float Alpha => _image != null ? _image.color.a : 0;
    public bool IsUsable => isActiveAndEnabled && _image != null && _effect != null;
    private UIAlphaFadeEffect _effect;
    private UnityEngine.UI.Image _image;
    private Material _material;
    public void Initialize(UIAlphaFadeEffect effect, UnityEngine.UI.Image imagePrefab, float fadeOut, float fadeIn, float blackHold)
    {
        DontDestroyOnLoad(gameObject);
        _effect = Instantiate(effect);
        _effect.Configure(fadeOut, fadeIn, blackHold);
        var canvasObject = new GameObject("Death Retry Overlay", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        _image = Instantiate(imagePrefab, canvasObject.transform);
        _image.name = "Death Fade Image";
        _image.rectTransform.anchorMin = Vector2.zero;
        _image.rectTransform.anchorMax = Vector2.one;
        _image.rectTransform.sizeDelta = Vector2.zero;
        _image.rectTransform.anchoredPosition = Vector2.zero;
        _image.color = new Color(0, 0, 0, 0);
        _image.raycastTarget = true;
        _material = new Material(effect.transitionMaterial);
        _image.material = _material;
    }
    public IEnumerator FadeOut()
    {
        Phase = TransitionPhase.FadeOut;
        yield return _effect.AnimateOut(_image);
        if (_image != null) _image.color = Color.black;
        Phase = TransitionPhase.LoadingBlack;
    }
    public IEnumerator FadeIn()
    {
        if (_effect.MiddleDelay > 0) yield return new WaitForSecondsRealtime(_effect.MiddleDelay);
        Phase = TransitionPhase.FadeIn;
        yield return _effect.AnimateIn(_image);
        Phase = TransitionPhase.Finished;
        if (_image != null) { _image.color = new Color(0, 0, 0, 0); _image.raycastTarget = false; }
    }
    public void Dispose()
    {
        if (_image != null) _image.gameObject.SetActive(false);
        Destroy(gameObject);
    }
    private void OnDisable() { if (_image != null) _image.gameObject.SetActive(false); }
    private void OnDestroy()
    {
        if (_material != null) Destroy(_material);
        if (_effect != null) Destroy(_effect);
    }
}
