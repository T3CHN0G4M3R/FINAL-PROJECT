using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(10000)]
public sealed class BaguetteHandPresentation : MonoBehaviour
{
    [SerializeField] private GameObject _visual;
    [SerializeField] private Camera _view;
    [SerializeField] private InteractionSystem _interactions;
    [SerializeField] private Vector3 _viewPosition = new Vector3(0.18f, -0.16f, 0.75f);
    [SerializeField] private Vector3 _viewRotation = new Vector3(10f, -35f, 15f);
    private Transform _visualTransform;

    private void Awake()
    {
        if (_visual != null) _visualTransform = _visual.transform;
    }

    private void LateUpdate()
    {
        if (_visual == null || !_visual.activeInHierarchy || _visualTransform == null || _view == null) return;
        Vector3 offset = _viewPosition;
        if (_interactions != null && _interactions.HeldObject != null) offset.x = -offset.x;
        _visualTransform.SetPositionAndRotation(_view.transform.TransformPoint(offset),
            _view.transform.rotation * Quaternion.Euler(_viewRotation));
    }

    private void OnEnable()
    {
        GameSaveManager.BaguetteChanged += Reconcile;
        GameplaySaveAdapter.SceneReady += OnSceneReady;
        Reconcile();
    }

    private void Start() => Reconcile();

    private void OnDisable()
    {
        GameSaveManager.BaguetteChanged -= Reconcile;
        GameplaySaveAdapter.SceneReady -= OnSceneReady;
        if (_visual != null) _visual.SetActive(false);
    }

    private void OnSceneReady(GameplaySaveAdapter adapter)
    {
        if (adapter.gameObject.scene == gameObject.scene) Reconcile();
    }

    private void Reconcile()
    {
        var manager = GameSaveManager.Instance;
        if (_visual != null) _visual.SetActive(manager != null && manager.HasBaguette && !manager.BaguetteUsedOnCar);
    }
}
