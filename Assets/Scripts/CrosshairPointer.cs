using UnityEngine;
using UnityEngine.UI;

public class CrosshairPointer : MonoBehaviour
{
    [Header("Pointer Appearance")]
    [Tooltip("The Image component of the crosshair / dot")]
    [SerializeField] private Image pointerImage;

    [Tooltip("Default color of the pointer")]
    [SerializeField] private Color defaultColor = new Color(1f, 1f, 1f, 0.85f);

    [Tooltip("Color of the pointer when looking at an interactable object")]
    [SerializeField] private Color interactableColor = new Color(0.2f, 0.9f, 1f, 1f);

    [Tooltip("Default size of the pointer (width and height)")]
    [SerializeField] private float defaultSize = 8f;

    [Tooltip("Size of the pointer when looking at an interactable object")]
    [SerializeField] private float interactableSize = 12f;

    [Tooltip("Animation speed when transitioning between states")]
    [SerializeField] private float transitionSpeed = 15f;

    [Header("Interaction System Reference (Optional)")]
    [SerializeField] private InteractionSystem interactionSystem;

    private RectTransform rectTransform;
    private float targetSize;
    private Color targetColor;

    private void Awake()
    {
        if (pointerImage == null)
        {
            pointerImage = GetComponent<Image>();
        }

        if (pointerImage != null)
        {
            rectTransform = pointerImage.rectTransform;
        }

        if (interactionSystem == null)
        {
            interactionSystem = Object.FindAnyObjectByType<InteractionSystem>();
        }

        targetSize = defaultSize;
        targetColor = defaultColor;
    }

    private void Update()
    {
        bool hasInteractable = false;
        if (interactionSystem != null)
        {
            hasInteractable = (interactionSystem.CurrentInteractable != null);
        }

        targetSize = hasInteractable ? interactableSize : defaultSize;
        targetColor = hasInteractable ? interactableColor : defaultColor;

        if (rectTransform != null)
        {
            float currentSize = rectTransform.sizeDelta.x;
            float newSize = Mathf.Lerp(currentSize, targetSize, Time.deltaTime * transitionSpeed);
            rectTransform.sizeDelta = new Vector2(newSize, newSize);
        }

        if (pointerImage != null)
        {
            pointerImage.color = Color.Lerp(pointerImage.color, targetColor, Time.deltaTime * transitionSpeed);
        }
    }

    public void SetState(bool isHoveringInteractable)
    {
        targetSize = isHoveringInteractable ? interactableSize : defaultSize;
        targetColor = isHoveringInteractable ? interactableColor : defaultColor;
    }
}
