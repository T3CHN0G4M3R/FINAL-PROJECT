using UnityEngine;

[DisallowMultipleComponent]
public class Flashlight : InteractableObject, IUsable
{
    [Header("Light Component")]
    [Tooltip("Reference to the light component. If empty, will search in children automatically.")]
    [SerializeField] private Light flashlightLight;

    [Header("Illumination Settings")]
    [Tooltip("Initial state of the flashlight (true = on, false = off)")]
    [SerializeField] private bool startsOn = true;

    [Tooltip("Range of the flashlight beam in meters")]
    [SerializeField] private float lightRange = 30f;

    [Tooltip("Outer spot angle in degrees")]
    [SerializeField] private float spotAngle = 65f;

    [Tooltip("Inner spot angle in degrees (controls hotspot falloff)")]
    [SerializeField] private float innerSpotAngle = 35f;

    [Tooltip("Light intensity")]
    [SerializeField] private float lightIntensity = 4.5f;

    [Tooltip("Light color")]
    [SerializeField] private Color lightColor = new Color(1f, 0.98f, 0.93f, 1f);

    [Tooltip("Enable shadows for realistic object lighting")]
    [SerializeField] private LightShadows shadows = LightShadows.Soft;

    [Header("Center-Screen Aiming")]
    [Tooltip("When held, dynamically aim the flashlight beam towards the center of the screen")]
    [SerializeField] private bool aimTowardsCrosshair = true;

    [Tooltip("Speed of aim smoothing")]
    [SerializeField] private float aimSmoothSpeed = 30f;

    [Tooltip("Max distance to raycast for aim point")]
    [SerializeField] private float maxAimDistance = 50f;

    [Tooltip("Layers to ignore during aim raycasting")]
    [SerializeField] private LayerMask aimIgnoreLayers;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip toggleSound;

    private bool isOn;
    private bool isHeld;
    private Camera playerCamera;
    private Transform playerTransform;

    public bool IsInUse => isOn;
    public Light FlashlightLight => flashlightLight;

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        if (flashlightLight == null)
        {
            flashlightLight = GetComponentInChildren<Light>(true);
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        ConfigureLightSettings();

        // Default rotation offset for standard flashlights
        HoldRotationOffset = new Vector3(90f, 0f, 0f);
        HoldPositionOffset = Vector3.zero;

        // Default ignore layers: Ignore raycast layer
        if (aimIgnoreLayers.value == 0)
        {
            aimIgnoreLayers = LayerMask.GetMask("Ignore Raycast", "UI");
        }
    }

    private void ConfigureLightSettings()
    {
        if (flashlightLight != null)
        {
            // Remove any 2D cookie texture that projects a flat image onto surfaces
            flashlightLight.cookie = null;
            flashlightLight.type = LightType.Spot;
            flashlightLight.range = lightRange;
            flashlightLight.spotAngle = spotAngle;
            flashlightLight.innerSpotAngle = innerSpotAngle;
            flashlightLight.intensity = lightIntensity;
            flashlightLight.color = lightColor;
            flashlightLight.shadows = shadows;
            flashlightLight.enabled = isOn || startsOn;
        }

        isOn = startsOn;
    }

    private void LateUpdate()
    {
        if (!isHeld || !aimTowardsCrosshair) return;

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null) return;
        }

        // Raycast from exact center of the screen (crosshair)
        Ray centerRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        LayerMask mask = ~aimIgnoreLayers;

        Vector3 targetPoint;
        if (Physics.Raycast(centerRay, out RaycastHit hit, maxAimDistance, mask, QueryTriggerInteraction.Ignore))
        {
            // If we hit player or flashlight itself, extend beyond
            if (playerTransform != null && (hit.transform == playerTransform || hit.transform.IsChildOf(playerTransform)))
            {
                targetPoint = centerRay.GetPoint(maxAimDistance);
            }
            else
            {
                targetPoint = hit.point;
            }
        }
        else
        {
            targetPoint = centerRay.GetPoint(maxAimDistance);
        }

        Vector3 aimDirection = (targetPoint - transform.position).normalized;
        if (aimDirection.sqrMagnitude > 0.001f)
        {
            // In the Flashlight model, the lens/light shines along local +Y.
            // LookRotation(aimDirection) * Euler(90, 0, 0) maps local +Y exactly to aimDirection in world space.
            Vector3 upVector = playerTransform != null ? playerTransform.up : Vector3.up;
            Quaternion desiredRotation = Quaternion.LookRotation(aimDirection, upVector) * Quaternion.Euler(90f, 0f, 0f);

            if (aimSmoothSpeed > 0f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * aimSmoothSpeed);
            }
            else
            {
                transform.rotation = desiredRotation;
            }
        }
    }

    public override void OnPickUp(GameObject player)
    {
        base.OnPickUp(player);
        isHeld = true;
        if (player != null)
        {
            playerTransform = player.transform;
        }
        playerCamera = Camera.main;
    }

    public override void OnDrop()
    {
        base.OnDrop();
        isHeld = false;
        playerTransform = null;
    }

    public void Use(GameObject user)
    {
        Toggle();
    }

    public void Toggle()
    {
        SetState(!isOn);
    }

    public void SetState(bool state)
    {
        isOn = state;
        if (flashlightLight != null)
        {
            flashlightLight.enabled = isOn;
        }

        PlayToggleSound();
    }

    private void PlayToggleSound()
    {
        if (audioSource != null && toggleSound != null)
        {
            audioSource.PlayOneShot(toggleSound);
        }
    }
}
