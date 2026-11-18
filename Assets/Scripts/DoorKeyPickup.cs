using UnityEngine;

[DisallowMultipleComponent]
public sealed class DoorKeyPickup : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private GameObject _keyObject;
    [SerializeField] private Collider _keyCollider;
    [SerializeField] private Transform _player;
    [SerializeField] private Collider _playerBody;
    [SerializeField] private InteractionSystem _interactions;
    [Header("Interaction")]
    [Tooltip("Distance to the key collider. Aiming is not required.")]
    [Min(0.01f)] [SerializeField] private float _interactionRadius = 3f;
    [SerializeField] private LayerMask _obstructionMask = ~0;
    [Header("Return to Neighborhood")]
    [SerializeField] private UIAlphaFadeEffect _fadeEffect;
    [SerializeField] private UnityEngine.UI.Image _fadeImagePrefab;
    [SerializeField, Min(0f)] private float _fadeOutSeconds = 1f;
    [SerializeField, Min(0f)] private float _fadeInSeconds = 1f;
    [SerializeField, Min(0f)] private float _blackHoldSeconds = 0.2f;
    public string Error { get; private set; }

    private readonly RaycastHit[] _hits = new RaycastHit[64];
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private Vector3 _originalScale;
    private bool _initialized;

    private void Awake()
    {
        if (_keyObject == null) return;
        _originalPosition = _keyObject.transform.localPosition;
        _originalRotation = _keyObject.transform.localRotation;
        _originalScale = _keyObject.transform.localScale;
        _initialized = true;
    }
    private void OnEnable()
    {
        GameSaveManager.DoorKeyChanged += Reconcile;
        Reconcile();
    }
    private void Start() => Reconcile();
    private void OnDisable() => GameSaveManager.DoorKeyChanged -= Reconcile;

    private void Reconcile()
    {
        if (!_initialized || _keyObject == null) return;
        bool available = GameSaveManager.Instance == null || !GameSaveManager.Instance.HasDoorKey;
        if (available)
        {
            _keyObject.transform.localPosition = _originalPosition;
            _keyObject.transform.localRotation = _originalRotation;
            _keyObject.transform.localScale = _originalScale;
        }
        _keyObject.SetActive(available);
    }
    public bool Owns(GameObject obj) => obj != null && _keyObject != null &&
        (obj == _keyObject || obj.transform.IsChildOf(_keyObject.transform));

    public bool CanInteract(Transform player, Camera view)
    {
        var manager = GameSaveManager.Instance;
        if (!isActiveAndEnabled || manager == null || manager.HasDoorKey || manager.Busy || manager.EncounterInProgress ||
            PauseMenuManager.IsGamePaused || !Application.isFocused || player == null || player != _player || view == null ||
            _interactions == null || _interactions.PersistenceInputBlocked || _keyObject == null || !_keyObject.activeInHierarchy ||
            _keyCollider == null || !_keyCollider.enabled || _playerBody == null) return false;
        Vector3 body = _playerBody.bounds.center;
        Vector3 nearest = _keyCollider.ClosestPoint(body);
        if ((nearest - body).sqrMagnitude > _interactionRadius * _interactionRadius) return false;
        Vector3 delta = _keyCollider.bounds.center - body;
        float distance = delta.magnitude;
        if (distance < 0.001f) return true;
        int count = Physics.RaycastNonAlloc(body, delta / distance, _hits, distance, _obstructionMask, QueryTriggerInteraction.Ignore);
        if (count == _hits.Length) return false;
        var held = _interactions.HeldObject;
        for (int i = 0; i < count; i++)
        {
            Transform hit = _hits[i].transform;
            if (hit == null || hit == player || hit.IsChildOf(player) || Owns(hit.gameObject) ||
                (held != null && (hit == held.transform || hit.IsChildOf(held.transform)))) continue;
            return false;
        }
        return true;
    }
    public bool TryPickUp(Transform player, Camera view)
    {
        if (!CanInteract(player, view)) return false;
        bool started = GameplayFadeTravel.BeginKey(_fadeEffect, _fadeImagePrefab,
            _fadeOutSeconds, _fadeInSeconds, _blackHoldSeconds, out string error);
        Error = error;
        if (!started && GameSaveManager.Instance != null) GameSaveManager.Instance.Report(error);
        return started;
    }

    private void OnValidate()
    {
        if (float.IsNaN(_interactionRadius) || float.IsInfinity(_interactionRadius)) _interactionRadius = 3f;
        _interactionRadius = Mathf.Max(0.01f, _interactionRadius);
    }
}
