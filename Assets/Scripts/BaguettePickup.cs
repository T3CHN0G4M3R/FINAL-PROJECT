using UnityEngine;

[DisallowMultipleComponent]
public sealed class BaguettePickup : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private GameObject _baguetteObject;
    [SerializeField] private Collider _baguetteCollider;
    [SerializeField] private Transform _player;
    [SerializeField] private Collider _playerBody;
    [SerializeField] private InteractionSystem _interactions;
    [Header("Interaction")]
    [Tooltip("Maximum camera-ray distance. Look directly at the Baguette to pick it up.")]
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
        if (_baguetteObject == null) return;
        _originalPosition = _baguetteObject.transform.localPosition;
        _originalRotation = _baguetteObject.transform.localRotation;
        _originalScale = _baguetteObject.transform.localScale;
        _initialized = true;
    }

    private void OnEnable()
    {
        GameSaveManager.BaguetteChanged += Reconcile;
        Reconcile();
    }

    private void Start() => Reconcile();
    private void OnDisable() => GameSaveManager.BaguetteChanged -= Reconcile;

    private void Reconcile()
    {
        if (!_initialized || _baguetteObject == null) return;
        bool available = GameSaveManager.Instance == null || !GameSaveManager.Instance.HasBaguette;
        if (available)
        {
            _baguetteObject.transform.localPosition = _originalPosition;
            _baguetteObject.transform.localRotation = _originalRotation;
            _baguetteObject.transform.localScale = _originalScale;
        }
        _baguetteObject.SetActive(available);
    }

    public bool Owns(GameObject obj) => obj != null && _baguetteObject != null &&
        (obj == _baguetteObject || obj.transform.IsChildOf(_baguetteObject.transform));

    public bool CanInteract(Transform player, Camera view)
    {
        var manager = GameSaveManager.Instance;
        if (!isActiveAndEnabled || manager == null || manager.HasBaguette || manager.Busy || manager.EncounterInProgress ||
            PauseMenuManager.IsGamePaused || !Application.isFocused || player == null || player != _player || view == null ||
            _interactions == null || _interactions.PersistenceInputBlocked || _baguetteObject == null || !_baguetteObject.activeInHierarchy ||
            _baguetteCollider == null || !_baguetteCollider.enabled || _playerBody == null) return false;
        
        Ray ray = view.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int count = Physics.RaycastNonAlloc(ray, _hits, _interactionRadius, _obstructionMask, QueryTriggerInteraction.Ignore);
        if (count == _hits.Length) return false;

        var held = _interactions.HeldObject;
        Collider nearest = null;
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            Transform hit = _hits[i].transform;
            if (hit == null || hit == player || hit.IsChildOf(player) ||
                (held != null && (hit == held.transform || hit.IsChildOf(held.transform)))) continue;
            if (_hits[i].distance >= nearestDistance) continue;
            nearestDistance = _hits[i].distance;
            nearest = _hits[i].collider;
        }
        return nearest != null && Owns(nearest.gameObject);
    }

    public bool TryPickUp(Transform player, Camera view)
    {
        if (!CanInteract(player, view)) return false;
        bool started = GameplayFadeTravel.Begin("Assets/Scenes/Neighborhood Scene.unity", true, _fadeEffect,
            _fadeImagePrefab, _fadeOutSeconds, _fadeInSeconds, _blackHoldSeconds, out string error);
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