using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PersistentSaveId : MonoBehaviour
{
    [SerializeField] private string _id;
    public string Id => _id;
    public bool IsDoor => _door != null;
    private InteractiveDoor _door;
    private Flashlight _flashlight;
    private Light _light;
    private Transform _originalParent;
    private Vector3 _originalScale;
    private Rigidbody _body;
    private bool _kinematic;
    private bool _detectCollisions;
    private Collider[] _colliders;
    private bool[] _colliderEnabled;
    private bool _initialized;

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        _door = GetComponent<InteractiveDoor>();
        _flashlight = GetComponent<Flashlight>();
        _light = GetComponentInChildren<Light>(true);
        _originalParent = transform.parent;
        _originalScale = transform.localScale;
        _body = GetComponent<Rigidbody>();
        if (_body != null) { _kinematic = _body.isKinematic; _detectCollisions = _body.detectCollisions; }
        _colliders = GetComponentsInChildren<Collider>(true);
        _colliderEnabled = new bool[_colliders.Length];
        for (int i = 0; i < _colliders.Length; i++) _colliderEnabled[i] = _colliders[i].enabled;
    }
    private void Awake() => Initialize();
    public SavedObjectState Capture(bool held)
    {
        Initialize();
        return new SavedObjectState { id = _id, kind = IsDoor ? 1 : 0, position = transform.position, rotation = transform.rotation, scale = held ? _originalScale : transform.localScale, active = gameObject.activeSelf, inUse = IsDoor ? _door.IsInUse : _flashlight != null ? _flashlight.IsInUse : _light != null && _light.enabled };
    }
    public void ApplyWorld(SavedObjectState state)
    {
        Initialize();
        if (IsDoor) { _door.RestoreSavedState(state.inUse); return; }
        transform.SetParent(_originalParent, false);
        transform.localScale = state.scale;
        transform.SetPositionAndRotation(state.position, state.rotation);
        gameObject.SetActive(state.active);
        for (int i = 0; i < _colliders.Length; i++) if (_colliders[i] != null) _colliders[i].enabled = _colliderEnabled[i];
        if (_body != null)
        {
            _body.isKinematic = _kinematic;
            _body.detectCollisions = _detectCollisions;
            if (!_body.isKinematic) { _body.linearVelocity = Vector3.zero; _body.angularVelocity = Vector3.zero; }
        }
    }
    public void ApplyUseState(SavedObjectState state)
    {
        if (IsDoor) return;
        if (_flashlight != null) _flashlight.RestoreSavedState(state.inUse);
        else if (_light != null) _light.enabled = state.inUse;
    }
}
