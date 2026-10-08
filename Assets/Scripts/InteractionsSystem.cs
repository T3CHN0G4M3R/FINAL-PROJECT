using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using TMPro;

public class InteractionSystem : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Maximum distance to interact with objects")]
    [SerializeField] private float interactDistance = 4f;

    [Tooltip("Radius for spherecast detection (makes picking up small objects easier)")]
    [SerializeField] private float raycastRadius = 0.2f;

    [Tooltip("Layer mask for interactable objects and environment")]
    [SerializeField] private LayerMask interactableLayers = ~0;

    [Header("Player & Hand References")]
    [Tooltip("Transform of the hand or socket where held objects will be parented")]
    [SerializeField] private Transform handPoint;

    [Tooltip("Root Transform of the Player")]
    [SerializeField] private Transform playerTransform;

    [Tooltip("Animator reference on the Player (optional)")]
    [SerializeField] private Animator animator;

    [Tooltip("Camera used for raycasting. If empty, Camera.main is used automatically.")]
    [SerializeField] private Camera playerCamera;

    [Header("UI Prompt (Optional)")]
    [SerializeField] private TextMeshProUGUI promptText;

    private GameObject currentInteractable;
    private GameObject heldObject;

    // Cached physics state of held object
    private readonly List<Collider> heldColliders = new List<Collider>();
    private Rigidbody heldRigidbody;
    private bool cachedRbKinematic;
    private bool cachedRbDetectCollisions;
    private bool hasHoldingAnimParam;

    public GameObject HeldObject => heldObject;
    public GameObject CurrentInteractable => currentInteractable;

    private void Awake()
    {
        InitializeReferences();
    }

    private void Start()
    {
        InitializeReferences();
    }

    private void InitializeReferences()
    {
        if (playerTransform == null)
        {
            if (CompareTag("Player"))
            {
                playerTransform = transform;
            }
            else
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj == null) playerObj = GameObject.Find("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
            }
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (animator == null && playerTransform != null)
        {
            animator = playerTransform.GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            hasHoldingAnimParam = HasAnimatorParameter(animator, "IsHoldingObject");
        }

        if (handPoint == null && playerTransform != null)
        {
            Transform[] children = playerTransform.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name.Equals("HandPoint", System.StringComparison.OrdinalIgnoreCase))
                {
                    handPoint = child;
                    break;
                }
            }

            if (handPoint == null)
            {
                foreach (Transform child in children)
                {
                    if (child.name.Equals("hand_r", System.StringComparison.OrdinalIgnoreCase) ||
                        child.name.Equals("RightHand", System.StringComparison.OrdinalIgnoreCase))
                    {
                        handPoint = child;
                        break;
                    }
                }
            }
        }
    }

    private void Update()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        CheckForInteractable();
        HandleInput();
        UpdateUI();
    }

    private void CheckForInteractable()
    {
        Ray ray = playerCamera != null
            ? playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : new Ray(transform.position, transform.forward);

        Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.cyan);

        RaycastHit hit;
        bool hitSomething = false;

        if (Physics.SphereCast(ray, raycastRadius, out hit, interactDistance, interactableLayers, QueryTriggerInteraction.Collide))
        {
            hitSomething = true;
        }
        else if (Physics.Raycast(ray, out hit, interactDistance, interactableLayers, QueryTriggerInteraction.Collide))
        {
            hitSomething = true;
        }

        if (hitSomething && hit.collider != null)
        {
            if (playerTransform != null && (hit.collider.transform == playerTransform || hit.collider.transform.IsChildOf(playerTransform)))
            {
                currentInteractable = null;
                return;
            }

            if (heldObject != null && (hit.collider.gameObject == heldObject || hit.collider.transform.IsChildOf(heldObject.transform)))
            {
                currentInteractable = null;
                return;
            }

            GameObject candidate = GetInteractableRoot(hit.collider.gameObject);
            if (candidate != null)
            {
                currentInteractable = candidate;
                return;
            }
        }

        currentInteractable = null;
    }

    private GameObject GetInteractableRoot(GameObject hitObj)
    {
        if (hitObj == null) return null;

        if (hitObj.CompareTag("InteractiveObject"))
        {
            return hitObj;
        }

        Transform current = hitObj.transform;
        while (current != null)
        {
            if (current.CompareTag("InteractiveObject"))
            {
                return current.gameObject;
            }

            if (current.GetComponent<IInteractable>() != null || current.GetComponent<IUsable>() != null)
            {
                return current.gameObject;
            }

            if (playerTransform != null && current == playerTransform) break;
            current = current.parent;
        }

        return null;
    }

    private void HandleInput()
    {
        bool fPressed = IsFPressed();
        bool rightClickPressed = IsRightClickPressed();

        // [F] Key: Pick up or Drop
        if (fPressed)
        {
            if (heldObject != null)
            {
                if (currentInteractable != null && currentInteractable != heldObject)
                {
                    GameObject targetToPick = currentInteractable;
                    DropObject();
                    PickUpObject(targetToPick);
                }
                else
                {
                    DropObject();
                }
            }
            else if (currentInteractable != null)
            {
                PickUpObject(currentInteractable);
            }
        }

        // Right-Click: Use held object or world object
        if (rightClickPressed)
        {
            if (heldObject != null)
            {
                UseObject(heldObject);
            }
        }
    }

    private bool IsFPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.F))
        {
            return true;
        }
#endif
        return false;
    }

    private bool IsRightClickPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(1))
        {
            return true;
        }
#endif
        return false;
    }

    public void PickUpObject(GameObject obj)
    {
        if (obj == null) return;
        if (handPoint == null)
        {
            InitializeReferences();
            if (handPoint == null)
            {
                Debug.LogWarning("[InteractionSystem] Cannot pick up object: HandPoint transform is not assigned!");
                return;
            }
        }

        heldObject = obj;

        heldRigidbody = heldObject.GetComponent<Rigidbody>();
        if (heldRigidbody != null)
        {
            cachedRbKinematic = heldRigidbody.isKinematic;
            cachedRbDetectCollisions = heldRigidbody.detectCollisions;
            heldRigidbody.isKinematic = true;
            heldRigidbody.detectCollisions = false;
        }

        heldColliders.Clear();
        heldColliders.AddRange(heldObject.GetComponentsInChildren<Collider>(true));
        foreach (Collider col in heldColliders)
        {
            col.enabled = false;
        }

        heldObject.transform.SetParent(handPoint);

        IInteractable interactable = heldObject.GetComponent<IInteractable>();
        if (interactable != null)
        {
            heldObject.transform.localPosition = interactable.HoldPositionOffset;
            heldObject.transform.localRotation = Quaternion.Euler(interactable.HoldRotationOffset);
            interactable.OnPickUp(playerTransform != null ? playerTransform.gameObject : gameObject);
        }
        else
        {
            heldObject.transform.localPosition = Vector3.zero;
            heldObject.transform.localRotation = Quaternion.identity;
        }

        if (animator != null && hasHoldingAnimParam)
        {
            animator.SetBool("IsHoldingObject", true);
        }
    }

    public void DropObject()
    {
        if (heldObject == null) return;

        Light lightComp = heldObject.GetComponentInChildren<Light>(true);
        if (lightComp != null)
        {
            lightComp.enabled = false;
        }

        IInteractable interactable = heldObject.GetComponent<IInteractable>();
        if (interactable != null)
        {
            interactable.OnDrop();
        }

        heldObject.transform.SetParent(null);

        foreach (Collider col in heldColliders)
        {
            if (col != null) col.enabled = true;
        }
        heldColliders.Clear();

        if (heldRigidbody != null)
        {
            heldRigidbody.isKinematic = cachedRbKinematic;
            heldRigidbody.detectCollisions = cachedRbDetectCollisions;
            heldRigidbody.linearVelocity = Vector3.zero;
            heldRigidbody.angularVelocity = Vector3.zero;
            heldRigidbody = null;
        }

        if (animator != null && hasHoldingAnimParam)
        {
            animator.SetBool("IsHoldingObject", false);
        }

        heldObject = null;
    }

    public void UseObject(GameObject target)
    {
        if (target == null) return;

        IUsable usable = target.GetComponent<IUsable>();
        if (usable == null)
        {
            usable = target.GetComponentInChildren<IUsable>(true);
        }

        if (usable != null)
        {
            usable.Use(playerTransform != null ? playerTransform.gameObject : gameObject);
            return;
        }

        Light lightComp = target.GetComponentInChildren<Light>(true);
        if (lightComp != null)
        {
            lightComp.enabled = !lightComp.enabled;
        }
    }

    private void UpdateUI()
    {
        if (promptText == null) return;

        if (heldObject != null)
        {
            string name = GetItemDisplayName(heldObject);
            bool isUsable = heldObject.GetComponent<IUsable>() != null || heldObject.GetComponentInChildren<IUsable>() != null || heldObject.GetComponentInChildren<Light>() != null;

            if (isUsable)
            {
                promptText.text = $"[Right Click] Usar {name}";
            }
            else
            {
                promptText.text = $"[F] Soltar {name}";
            }
        }
        else if (currentInteractable != null)
        {
            string name = GetItemDisplayName(currentInteractable);
            promptText.text = $"[F] Tomar {name}";
        }
        else
        {
            promptText.text = "";
        }
    }

    private string GetItemDisplayName(GameObject obj)
    {
        if (obj == null) return "";
        IInteractable item = obj.GetComponent<IInteractable>();
        if (item != null && !string.IsNullOrEmpty(item.ItemName))
        {
            return item.ItemName;
        }
        return obj.name;
    }

    private bool HasAnimatorParameter(Animator anim, string paramName)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return false;
        foreach (AnimatorControllerParameter p in anim.parameters)
        {
            if (p.name == paramName) return true;
        }
        return false;
    }
}
