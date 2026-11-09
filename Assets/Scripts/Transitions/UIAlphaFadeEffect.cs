using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Transitions/UI Alpha Fade")]
public class UIAlphaFadeEffect : PixeLadder.EasyTransition.TransitionEffect
{
    [SerializeField, Min(0f)] private float _fadeOutSeconds = 1f;
    [SerializeField, Min(0f)] private float _fadeInSeconds = 1f;

    public void Configure(float fadeOutSeconds, float fadeInSeconds, float blackHoldSeconds)
    {
        _fadeOutSeconds = Mathf.Max(0f, fadeOutSeconds);
        _fadeInSeconds = Mathf.Max(0f, fadeInSeconds);
        MiddleDelay = blackHoldSeconds;
    }

    public override IEnumerator AnimateOut(UnityEngine.UI.Image image)
    {
        // Standard UI blending, not a Shader Graph cutoff: guarantees partial opacity.
        image.raycastTarget = true;
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.sizeDelta = Vector2.zero;
        image.rectTransform.anchoredPosition = Vector2.zero;
        yield return AnimateAlpha(image, 0f, 1f, _fadeOutSeconds);
    }

    public override IEnumerator AnimateIn(UnityEngine.UI.Image image)
    {
        image.rectTransform.sizeDelta = Vector2.zero;
        yield return AnimateAlpha(image, 1f, 0f, _fadeInSeconds);
    }

    private IEnumerator AnimateAlpha(UnityEngine.UI.Image image, float start, float end, float seconds)
    {
        image.color = new Color(0f, 0f, 0f, start);
        float elapsed = 0f;
        while (elapsed < seconds && image != null)
        {
            float progress = Mathf.Clamp01(elapsed / seconds);
            image.color = new Color(0f, 0f, 0f, Mathf.Lerp(start, end, easingCurve.Evaluate(progress)));
            yield return null;
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        }
        if (image != null) image.color = new Color(0f, 0f, 0f, end);
    }
}
