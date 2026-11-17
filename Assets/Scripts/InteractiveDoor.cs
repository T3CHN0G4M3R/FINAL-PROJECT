using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InteractiveDoor : MonoBehaviour, IUsable
{
    [Header("Door Settings")]
    [SerializeField] private float openAngle = 100f;
    [SerializeField] private float speed = 3.5f;
    [SerializeField] private bool isOpen = false;
    [SerializeField] private float interactionDistance = 3.0f;
    [SerializeField] private string doorName = "Puerta";
    [Tooltip("This scene door requires the logical DoorKey collected in CarScene.")]
    [SerializeField] private bool requiresDoorKey;
    public bool IsLocked => requiresDoorKey && (GameSaveManager.Instance == null || !GameSaveManager.Instance.HasDoorKey);

    private Quaternion closedLocalRot;
    private Quaternion openLocalRot;
    private Transform playerTransform;
    private Camera mainCamera;

    public bool IsInUse => isOpen;
    public string DoorName => doorName;

    private void Awake()
    {
        float currentY = transform.localEulerAngles.y;
        if (currentY > 180f) currentY -= 360f;

        if (Mathf.Abs(currentY) > 30f)
        {
            isOpen = true;
            openLocalRot = transform.localRotation;
            closedLocalRot = Quaternion.Euler(transform.localEulerAngles.x, 0f, transform.localEulerAngles.z);
        }
        else
        {
            isOpen = false;
            closedLocalRot = transform.localRotation;
            openLocalRot = Quaternion.Euler(transform.localEulerAngles.x, openAngle, transform.localEulerAngles.z);
        }
        ReconcileLock();
    }

    private void OnEnable()
    {
        if (requiresDoorKey) GameSaveManager.DoorKeyChanged += ReconcileLock;
        ReconcileLock();
    }
    private void OnDisable() => GameSaveManager.DoorKeyChanged -= ReconcileLock;
    private void ReconcileLock()
    {
        if (!IsLocked) return;
        isOpen = false;
        transform.localRotation = closedLocalRot;
    }

    private void Start()
    {
        FindPlayer();
    }

    private void FindPlayer()
    {
        if (playerTransform == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p == null) p = GameObject.Find("Player");
            if (p != null) playerTransform = p.transform;
        }
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        Quaternion targetRot = isOpen ? openLocalRot : closedLocalRot;
        if (Quaternion.Angle(transform.localRotation, targetRot) > 0.1f)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * speed);
        }

        CheckDirectInput();
    }

    private void CheckDirectInput()
    {
        // Handled centrally by InteractionSystem to prevent double-toggle and ensure prompt synchronization
    }

    private bool IsPartOfThisDoor(Transform target)
    {
        if (target == null) return false;
        if (target.CompareTag("TagDoor")) return true;
        if (target == transform || target.IsChildOf(transform) || transform.IsChildOf(target)) return true;
        if (transform.parent != null && target.IsChildOf(transform.parent)) return true;
        return false;
    }

    private bool IsInteractPressed(bool isLookingAtThis)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
                return true;
        }
        if (isLookingAtThis && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.E))
            return true;
        if (isLookingAtThis && Input.GetMouseButtonDown(0))
            return true;
#endif
        return false;
    }

    public void RestoreSavedState(bool open)
    {
        isOpen = open && !IsLocked;
        transform.localRotation = isOpen ? openLocalRot : closedLocalRot;
    }

    public void ToggleDoor()
    {
        if (IsLocked) { ReconcileLock(); return; }
        isOpen = !isOpen;
    }

    public void Open()
    {
        if (IsLocked) { ReconcileLock(); return; }
        isOpen = true;
    }

    public void Close()
    {
        isOpen = false;
    }

    public void Use(GameObject user)
    {
        ToggleDoor();
    }
}
