using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public sealed class GameplaySaveAdapter : MonoBehaviour
{
    [SerializeField] private StarterAssets.FirstPersonController _player;
    [SerializeField] private InteractionSystem _interactions;
    [SerializeField] private PauseMenuManager _pause;
    [SerializeField] private PersistentSaveId[] _objects;
    public static event Action<GameplaySaveAdapter> SceneReady;
    public bool Ready { get; private set; }
    public string ScenePath => gameObject.scene.path;
    public PauseMenuManager Pause => _pause;
    private readonly Dictionary<string, PersistentSaveId> _lookup = new Dictionary<string, PersistentSaveId>();
    private bool _controllerEnabled;
    private bool _inputEnabled;
    private UnityEngine.InputSystem.PlayerInput _input;
    private bool _blocked;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents() => SceneReady = null;
    private void Awake()
    {
        _input = _player == null ? null : _player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (_objects != null) foreach (var obj in _objects) if (obj != null) obj.Initialize();
        BlockInput(true);
    }
    private IEnumerator Start()
    {
        // All authored Awake/Start methods and pause cursor initialization finish first.
        yield return null;
        yield return new WaitForSecondsRealtime(0.15f);
        Ready = true;
        SceneReady?.Invoke(this);
        if (!HasCoordinator) BlockInput(false);
    }
    public bool HasCoordinator { get; set; }
    public void BlockInput(bool block)
    {
        if (block == _blocked) return;
        _blocked = block;
        if (_interactions != null) _interactions.PersistenceInputBlocked = block;
        if (block)
        {
            _controllerEnabled = _player != null && _player.enabled;
            _inputEnabled = _input != null && _input.enabled;
            if (_player != null) _player.enabled = false;
            if (_input != null) _input.enabled = false;
        }
        else
        {
            if (_player != null) _player.enabled = _controllerEnabled;
            if (_input != null) _input.enabled = _inputEnabled;
        }
    }
    public string ValidateObjects(SavedSceneState state = null)
    {
        if (_player == null || _interactions == null || _pause == null || _objects == null || !SaveGameData.SupportedScene(ScenePath)) return "Incomplete gameplay save references";
        _lookup.Clear();
        foreach (var obj in _objects)
        {
            if (obj == null || string.IsNullOrWhiteSpace(obj.Id) || _lookup.ContainsKey(obj.Id)) return "Missing or duplicate persistent object ID";
            obj.Initialize();
            _lookup.Add(obj.Id, obj);
        }
        if (state != null)
        {
            if (state.path != ScenePath || state.objects.Count != _lookup.Count) return "Saved world does not match this scene";
            foreach (var record in state.objects)
                if (!_lookup.TryGetValue(record.id, out var obj) || obj.IsDoor != (record.kind == 1)) return "Saved object is missing or incompatible";
        }
        return null;
    }
    public SavedSceneState Capture()
    {
        if (!Ready) throw new InvalidOperationException("Gameplay is still initializing");
        string error = ValidateObjects();
        if (error != null) throw new InvalidOperationException(error);
        var held = _interactions.HeldObject;
        string heldId = null;
        var state = new SavedSceneState { path = ScenePath, position = _player.transform.position, yaw = _player.transform.eulerAngles.y, pitch = _player.SavedViewPitch };
        foreach (var obj in _objects)
        {
            bool isHeld = held == obj.gameObject;
            if (isHeld) heldId = obj.Id;
            state.objects.Add(obj.Capture(isHeld));
        }
        if (held != null && heldId == null) throw new InvalidOperationException("Held object is not registered for saving");
        state.heldId = heldId;
        return state;
    }
    public void Restore(SavedSceneState state)
    {
        string error = ValidateObjects(state);
        if (error != null) throw new InvalidOperationException(error);
        _interactions.DropObject();
        foreach (var record in state.objects) _lookup[record.id].ApplyWorld(record);
        _player.RestoreSavedPose(state.position, state.yaw, state.pitch);
        if (!string.IsNullOrEmpty(state.heldId)) _interactions.PickUpObject(_lookup[state.heldId].gameObject);
        foreach (var record in state.objects) _lookup[record.id].ApplyUseState(record);
        Physics.SyncTransforms();
    }
}
