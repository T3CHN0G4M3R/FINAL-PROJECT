using UnityEngine;

[DisallowMultipleComponent]
public class DevCodeFlyMode : MonoBehaviour
{
    [Header("Configuración de Vuelo")]
    public float flySpeed = 15f;
    public float doubleTapThreshold = 0.3f;

    [Header("Estado del DevCode")]
    public bool isDevCodeUnlocked = false;
    public bool isFlyModeActive = false;

    private Rigidbody rb;
    private Collider[] playerColliders;
    private bool[] colliderStates;
    private StarterAssets.StarterAssetsInputs playerInputs;
    private UnityEngine.InputSystem.PlayerInput playerInput;
    private Transform viewTransform;
    private bool originalUseGravity;
    private bool originalIsKinematic;
    private bool physicsCaptured;
    private float lastSpaceTime = float.NegativeInfinity;
    private const string CheatSequence = ".-{";
    private int sequenceIndex;
    private UnityEngine.InputSystem.Keyboard keyboard;
    private UnityEngine.InputSystem.InputAction toggleAction;
    private UnityEngine.InputSystem.InputAction riseAction;
    private UnityEngine.InputSystem.InputAction descendAction;
    private static bool sessionUnlocked;
    private static int videoSequenceIndex;
    public static bool IsSessionUnlocked => sessionUnlocked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        sessionUnlocked = false;
        videoSequenceIndex = 0;
    }

    public static bool HandleVideoTextInput(char character)
    {
        if (character == '+') return sessionUnlocked;
        if (char.IsControl(character)) return false;
        if (character == CheatSequence[videoSequenceIndex]) videoSequenceIndex++;
        else videoSequenceIndex = character == CheatSequence[0] ? 1 : 0;
        if (videoSequenceIndex != CheatSequence.Length) return false;
        videoSequenceIndex = 0;
        sessionUnlocked = !sessionUnlocked;
        Debug.Log("DevCode Desbloqueado: " + sessionUnlocked);
        return false;
    }

    private void Awake()
    {
        isDevCodeUnlocked = isDevCodeUnlocked || sessionUnlocked;
        sessionUnlocked = isDevCodeUnlocked;
        rb = GetComponent<Rigidbody>();
        playerColliders = GetComponentsInChildren<Collider>(true);
        colliderStates = new bool[playerColliders.Length];
        playerInputs = GetComponent<StarterAssets.StarterAssetsInputs>();
        playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        var controller = GetComponent<StarterAssets.FirstPersonController>();
        if (controller != null && controller.CinemachineCameraTarget != null)
            viewTransform = controller.CinemachineCameraTarget.transform;
        else
        {
            var camera = Camera.main;
            viewTransform = camera != null ? camera.transform : transform;
        }
        toggleAction = new UnityEngine.InputSystem.InputAction("DevFlightToggle", UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/space");
        riseAction = new UnityEngine.InputSystem.InputAction("DevFlightRise", UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/e");
        descendAction = new UnityEngine.InputSystem.InputAction("DevFlightDescend", UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/q");
    }

    private void OnEnable()
    {
        BindKeyboard();
        UnityEngine.InputSystem.InputSystem.onDeviceChange += OnDeviceChange;
        toggleAction.performed += OnSpace;
        toggleAction.Enable();
        riseAction.Enable();
        descendAction.Enable();
    }

    private bool CanReadInput()
    {
        return Time.timeScale > 0f && (playerInput == null || (playerInput.enabled && playerInput.inputIsActive));
    }

    private void BindKeyboard()
    {
        if (keyboard != null) keyboard.onTextInput -= OnTextInput;
        keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null) keyboard.onTextInput += OnTextInput;
    }

    private void OnDeviceChange(UnityEngine.InputSystem.InputDevice device, UnityEngine.InputSystem.InputDeviceChange change)
    {
        if (device is UnityEngine.InputSystem.Keyboard) BindKeyboard();
    }

    private void OnTextInput(char character)
    {
        if (!CanReadInput()) return;
        // Text input respects the keyboard layout. Shift/AltGr do not reset the sequence.
        if (char.IsControl(character)) return;
        if (character == CheatSequence[sequenceIndex]) sequenceIndex++;
        else sequenceIndex = character == CheatSequence[0] ? 1 : 0;
        if (sequenceIndex != CheatSequence.Length) return;
        sequenceIndex = 0;
        lastSpaceTime = float.NegativeInfinity;
        isDevCodeUnlocked = !isDevCodeUnlocked;
        sessionUnlocked = isDevCodeUnlocked;
        Debug.Log("DevCode Desbloqueado: " + isDevCodeUnlocked, this);
        if (!isDevCodeUnlocked && isFlyModeActive) ToggleFlyMode();
    }

    private void OnSpace(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (!CanReadInput() || !isDevCodeUnlocked) return;
        float now = Time.unscaledTime;
        if (now - lastSpaceTime <= doubleTapThreshold)
        {
            ToggleFlyMode();
            lastSpaceTime = float.NegativeInfinity;
        }
        else lastSpaceTime = now;
    }

    private void ToggleFlyMode()
    {
        if (isFlyModeActive) RestorePhysics();
        else
        {
            for (int i = 0; i < playerColliders.Length; i++)
            {
                if (playerColliders[i] == null) continue;
                colliderStates[i] = playerColliders[i].enabled;
                playerColliders[i].enabled = false;
            }
            if (rb != null)
            {
                originalUseGravity = rb.useGravity;
                originalIsKinematic = rb.isKinematic;
                if (!rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                rb.useGravity = false;
                rb.isKinematic = true;
            }
            physicsCaptured = true;
            isFlyModeActive = true;
        }
        if (playerInputs != null) playerInputs.jump = false;
        Debug.Log("Modo Vuelo: " + isFlyModeActive, this);
    }

    private void RestorePhysics()
    {
        if (physicsCaptured)
        {
            for (int i = 0; i < playerColliders.Length; i++)
                if (playerColliders[i] != null) playerColliders[i].enabled = colliderStates[i];
            if (rb != null) { rb.isKinematic = originalIsKinematic; rb.useGravity = originalUseGravity; }
        }
        physicsCaptured = false;
        isFlyModeActive = false;
    }

    private void Update()
    {
        if (!isFlyModeActive || !CanReadInput()) return;
        Vector2 move = playerInputs != null ? playerInputs.move : Vector2.zero;
        float vertical = riseAction.ReadValue<float>() - descendAction.ReadValue<float>();
        Vector3 direction = viewTransform.right * move.x + viewTransform.forward * move.y + Vector3.up * vertical;
        transform.position += Vector3.ClampMagnitude(direction, 1f) * flySpeed * Time.deltaTime;
        if (playerInputs != null) playerInputs.jump = false;
    }

    private void OnDisable()
    {
        if (keyboard != null) keyboard.onTextInput -= OnTextInput;
        UnityEngine.InputSystem.InputSystem.onDeviceChange -= OnDeviceChange;
        toggleAction.performed -= OnSpace;
        toggleAction.Disable();
        riseAction.Disable();
        descendAction.Disable();
        RestorePhysics();
        lastSpaceTime = float.NegativeInfinity;
        sequenceIndex = 0;
    }

    private void OnDestroy()
    {
        toggleAction?.Dispose();
        riseAction?.Dispose();
        descendAction?.Dispose();
    }
}
