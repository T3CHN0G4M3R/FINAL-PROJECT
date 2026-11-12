using System;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public sealed class SirenHeadAI : MonoBehaviour
{
    public enum EnemyState { Roam, Chase, Investigate, Search, Capture }
    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _player;
    [SerializeField] private Collider _playerBody;
    [SerializeField] private Transform[] _sightTargets;
    [SerializeField] private Transform _sightOrigin;
    [SerializeField] private Transform _captureOrigin;
    [SerializeField] private Transform[] _streetPoints;
    [SerializeField] private InteractionSystem _interactions;
    [SerializeField] private GameplaySaveAdapter _saveAdapter;
    [Header("Enemy Audio")]
    [SerializeField] private AudioSource _chaseAudio;
    [SerializeField] private AudioSource _footstepAudio;
    [SerializeField] private AudioClip[] _footstepClips;
    [Min(0.05f)] [SerializeField] private float _walkStepInterval = 0.8f;
    [Min(0.05f)] [SerializeField] private float _runStepInterval = 0.45f;
    private float _stepTime;
    [Header("Movement (units / second)")]
    [Min(0)] [SerializeField] private float _walkingSpeed = 2f;
    [Min(0)] [SerializeField] private float _runningSpeed = 6f;
    [Min(0.1f)] [SerializeField] private float _acceleration = 12f;
    [Min(0)] [SerializeField] private float _angularSpeed = 180f;
    [Min(0.1f)] [SerializeField] private float _arrivalTolerance = 0.5f;
    [Header("Sight")]
    [Min(0)] [SerializeField] private float _sightRange = 30f;
    [Range(0, 360)] [SerializeField] private float _fieldOfView = 100f;
    [SerializeField] private LayerMask _occlusionMask = ~0;
    [Min(0.02f)] [SerializeField] private float _perceptionInterval = 0.1f;
    [Min(0.02f)] [SerializeField] private float _repathInterval = 0.2f;
    [Min(0)] [SerializeField] private float _sightLossGrace = 0.5f;
    [Header("Patrol and Last-Seen Search")]
    [Tooltip("Seconds searching locally AFTER reaching the last sighting (or investigation timeout). Zero skips search.")]
    [Min(0)] [SerializeField] private float _searchDuration = 15f;
    [Min(0)] [SerializeField] private float _searchRadius = 12f;
    [Min(0)] [SerializeField] private float _investigationTimeout = 15f;
    [Min(0)] [SerializeField] private float _waypointDwell = 0f;
    [Min(0)] [SerializeField] private float _searchDwell = 1f;
    [Min(0.1f)] [SerializeField] private float _samplingDistance = 2f;
    [Range(1, 32)] [SerializeField] private int _destinationAttempts = 12;
    [Min(0.1f)] [SerializeField] private float _stuckTimeout = 3f;
    [Min(0.1f)] [SerializeField] private float _retryDelay = 0.5f;
    [Header("Capture")]
    [Tooltip("Distance from CaptureOrigin to the closest point on the real player capsule. Walls still block capture.")]
    [Min(0)] [SerializeField] private float _captureDistance = 1.5f;
    [Min(0)] [SerializeField] private float _captureVerticalTolerance = 1.8f;
    [Header("Animation")]
    [Min(0.05f)] [SerializeField] private float _walkPlayback = 1f;
    [Min(0.05f)] [SerializeField] private float _runPlayback = 1f;
    [Min(0)] [SerializeField] private float _animationBlend = 0.15f;
    [Header("Protected Interiors")]
    [SerializeField] private BoxCollider[] _protectedInteriors;
    public bool IsPlayerProtected
    {
        get
        {
            if (_player == null || _protectedInteriors == null) return false;
            foreach (var volume in _protectedInteriors)
            {
                if (volume == null || !volume.enabled || !volume.gameObject.activeInHierarchy) continue;
                Vector3 point = volume.transform.InverseTransformPoint(_player.position) - volume.center;
                Vector3 half = volume.size * 0.5f;
                if (Mathf.Abs(point.x) < half.x && Mathf.Abs(point.y) < half.y && Mathf.Abs(point.z) < half.z) return true;
            }
            return false;
        }
    }
    [Header("Debug")]
    [SerializeField] private bool _drawGizmos = true;public EnemyState State { get; private set; }
    public Vector3 LastSeenPosition { get; private set; }
    public float SearchTimeRemaining => State == EnemyState.Search ? Mathf.Max(0, _searchDuration - _stateTime) : 0;
    public NavMeshPathStatus PathStatus => _agent != null && _agent.isOnNavMesh ? _agent.pathStatus : NavMeshPathStatus.PathInvalid;
    public NavMeshAgent Agent => _agent;
    public Animator Animator => _animator;
    public Transform Player => _player;
    public event Action<SirenHeadAI> CaptureRequested;
    public bool EncounterReady { get; set; }
    private NavMeshAgent _agent;
    private NavMeshPath _path;
    private readonly RaycastHit[] _sightHits = new RaycastHit[128];
    private float _stateTime, _perceptionTime, _repathTime, _lostTime, _dwellTime, _stuckTime, _retryTime;
    private int _previousPoint = -1;
    private int _animationHash;
    private bool _visible, _destination, _arrivalHandled;
    private Vector3 _previousPosition;
    private float _savedAnimatorSpeed = 1f;
    private bool _suspended;
    private static readonly int Playback = Animator.StringToHash("Playback");
    private static readonly int Idle = Animator.StringToHash("Base Layer.Idle");
    private static readonly int Walk = Animator.StringToHash("Base Layer.Walk");
    private static readonly int Run = Animator.StringToHash("Base Layer.Run");

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _path = new NavMeshPath();
        _agent.acceleration = _acceleration;
        _agent.angularSpeed = _angularSpeed;
        _agent.stoppingDistance = Mathf.Min(_arrivalTolerance, _captureDistance * 0.5f);
        _agent.autoTraverseOffMeshLink = false;
        if (_animator != null) _animator.applyRootMotion = false;
        _previousPosition = transform.position;
    }
    private void Start()
    {
        if (_player == null || _playerBody == null || _animator == null || _sightOrigin == null || _captureOrigin == null || _saveAdapter == null)
        {
            Debug.LogError("Siren Head requires explicit player, body, Animator, sight, capture and save references.", this);
            enabled = false;
            return;
        }
        EnterState(EnemyState.Roam);
    }
    private bool GameplayAvailable => _saveAdapter != null && _saveAdapter.Ready &&
        !PauseMenuManager.IsGamePaused && Time.timeScale > 0 &&
        (GameSaveManager.Instance == null || !GameSaveManager.Instance.Busy) &&
        (PixeLadder.EasyTransition.SceneTransitioner.Instance == null || !PixeLadder.EasyTransition.SceneTransitioner.Instance.IsTransitioning);
    private void Update()
    {
        if (State == EnemyState.Capture) return;
        if (!GameplayAvailable || !_agent.enabled || !_agent.isOnNavMesh)
        {
            if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;
            if (!_suspended && _animator != null) { _savedAnimatorSpeed = _animator.speed; _animator.speed = 0; _suspended = true; }
            return;
        }
        if (_suspended) { _animator.speed = _savedAnimatorSpeed; _suspended = false; }
        _agent.isStopped = false;
        if (IsPlayerProtected)
        {
            _visible = false;
            _lostTime = 0;
            if (State == EnemyState.Chase || State == EnemyState.Investigate || State == EnemyState.Search) EnterState(EnemyState.Roam);
        }
        float dt = Time.deltaTime;_stateTime += dt;
        _perceptionTime -= dt; _repathTime -= dt; _retryTime -= dt;
        if (_perceptionTime <= 0)
        {
            _perceptionTime = _perceptionInterval;
            _visible = CanSeePlayer() || IsPlayerNearby();
            if (_visible)
            {
                LastSeenPosition = _player.position;
                _lostTime = 0;
                if (State != EnemyState.Chase) EnterState(EnemyState.Chase);
            }
        }
        if (CanCapturePlayer()) { CaptureRequested?.Invoke(this); if (State == EnemyState.Capture) return; }
        if (State == EnemyState.Chase)
        {
            if (!_visible) _lostTime += dt;
            if (_lostTime >= _sightLossGrace && !_visible) EnterState(EnemyState.Investigate);
            else if (_repathTime <= 0) { _repathTime = _repathInterval; SetReachableDestination(LastSeenPosition); }
        }
        if (State == EnemyState.Investigate && (Arrived() || _stateTime >= _investigationTimeout || !_destination)) EnterState(EnemyState.Search);
        if (State == EnemyState.Search && _stateTime >= _searchDuration) EnterState(EnemyState.Roam);
        if (State == EnemyState.Roam || State == EnemyState.Search)
        {
            if (_destination && Arrived())
            {
                if (!_arrivalHandled) { _arrivalHandled = true; _dwellTime = State == EnemyState.Search ? _searchDwell : _waypointDwell; }
                _dwellTime -= dt;
                if (_dwellTime <= 0) _destination = false;
            }
            if (!_destination && _retryTime <= 0)
            {
                bool found = State == EnemyState.Roam ? ChooseStreetPoint() : ChooseSearchPoint();
                if (!found) _retryTime = _retryDelay;
            }
        }
        // A failed/stalled path must not keep the enemy running into a wall forever.
        if (_destination && !_agent.pathPending && !Arrived())
        {
            _stuckTime = (transform.position - _previousPosition).sqrMagnitude < 0.00001f ? _stuckTime + dt : 0;
            if (_stuckTime >= _stuckTimeout || _agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                _agent.ResetPath(); _destination = false; _stuckTime = 0; _retryTime = _retryDelay;
                if (State == EnemyState.Investigate) EnterState(EnemyState.Search);
            }
        }
        _previousPosition = transform.position;
        AnimateLocomotion();
        UpdateFootsteps(dt);
    }
    private void UpdateFootsteps(float dt)
    {
        if (_footstepAudio == null || _footstepClips == null || _footstepClips.Length == 0) return;
        if (_agent.velocity.sqrMagnitude <= 0.01f) { _stepTime = 0; return; }
        _stepTime -= dt;
        if (_stepTime > 0) return;
        _stepTime = State == EnemyState.Chase ? _runStepInterval : _walkStepInterval;
        AudioClip clip = _footstepClips[UnityEngine.Random.Range(0, _footstepClips.Length)];
        if (clip != null) _footstepAudio.PlayOneShot(clip);
    }
    private void EnterState(EnemyState state)
    {
        if (_chaseAudio != null)
        {
            if (state == EnemyState.Chase)
            {
                if (!_chaseAudio.isPlaying) _chaseAudio.Play();
            }
            else _chaseAudio.Stop();
        }
        if (state == EnemyState.Capture && _footstepAudio != null) _footstepAudio.Stop();
        _stepTime = 0;
        State = state; _stateTime = 0; _stuckTime = 0; _repathTime = 0; _retryTime = 0;
        _destination = false; _arrivalHandled = false;
        if (_agent.enabled && _agent.isOnNavMesh) _agent.ResetPath();
        _agent.speed = state == EnemyState.Chase ? _runningSpeed : _walkingSpeed;
        _agent.autoBraking = true;
        if (state == EnemyState.Investigate) SetReachableDestination(LastSeenPosition);
    }
    private bool Arrived() => _destination && !_agent.pathPending && _agent.hasPath &&
        _agent.remainingDistance <= Mathf.Max(_arrivalTolerance, _agent.stoppingDistance);
    private bool SetReachableDestination(Vector3 position)
    {
        var filter = new NavMeshQueryFilter { agentTypeID = _agent.agentTypeID, areaMask = _agent.areaMask };
        if (!NavMesh.SamplePosition(position, out var hit, _samplingDistance, filter) ||
            !_agent.CalculatePath(hit.position, _path) || _path.status != NavMeshPathStatus.PathComplete) return false;
        if (!_agent.SetPath(_path)) return false;
        _destination = true; _arrivalHandled = false;
        return true;
    }
    private bool ChooseStreetPoint()
    {
        if (_streetPoints == null || _streetPoints.Length == 0) return false;
        int start = UnityEngine.Random.Range(0, _streetPoints.Length);
        for (int i = 0; i < _streetPoints.Length; i++)
        {
            int index = (start + i) % _streetPoints.Length;
            var point = _streetPoints[index];
            if (point == null || (index == _previousPoint && _streetPoints.Length > 1) ||
                Vector3.Distance(point.position, transform.position) < _arrivalTolerance * 2) continue;
            if (SetReachableDestination(point.position)) { _previousPoint = index; return true; }
        }
        return false;
    }
    private bool ChooseSearchPoint()
    {
        for (int i = 0; i < _destinationAttempts; i++)
        {
            var offset = UnityEngine.Random.insideUnitCircle * _searchRadius;
            var target = LastSeenPosition + new Vector3(offset.x, 0, offset.y);
            if (Vector3.Distance(target, transform.position) > _arrivalTolerance * 2 && SetReachableDestination(target)) return true;
        }
        return false;
    }
    [Header("Proximity Awareness")]
    [Tooltip("Chase a nearby player even behind the enemy. Walls still block body-level awareness. Zero disables proximity only.")]
    [Min(0)] [SerializeField] private float _proximityChaseRadius = 8f;
    public bool IsPlayerNearby()
    {
        if (_proximityChaseRadius <= 0 || _playerBody == null || _captureOrigin == null) return false;
        if (IsPlayerProtected) return false;
        Vector3 origin = _captureOrigin.position;
        Vector3 target = _playerBody.bounds.center;
        return (target - origin).sqrMagnitude <= _proximityChaseRadius * _proximityChaseRadius && ClearLine(origin, target);
    }

    public bool CanSeePlayer()
    {
        if (IsPlayerProtected) return false;
        if (_player == null || _sightOrigin == null) return false;
        Vector3 delta = _player.position - transform.position;
        if (delta.sqrMagnitude > _sightRange * _sightRange) return false;
        delta.y = 0;
        if (delta.sqrMagnitude > 0.001f && Vector3.Angle(transform.forward, delta) > _fieldOfView * 0.5f) return false;
        if (_sightTargets != null) foreach (var target in _sightTargets)
            if (target != null && ClearLine(_sightOrigin.position, target.position)) return true;
        return _playerBody != null && ClearLine(_sightOrigin.position, _playerBody.bounds.center);
    }
    private bool ClearLine(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        int count = Physics.RaycastNonAlloc(from, delta.normalized, _sightHits, delta.magnitude, _occlusionMask, QueryTriggerInteraction.Ignore);
        if (count == _sightHits.Length) return false; // Overflow is blocked, never a false-positive detection.
        var held = _interactions != null ? _interactions.HeldObject : null;
        for (int i = 0; i < count; i++)
        {
            Transform hit = _sightHits[i].transform;
            if (hit.IsChildOf(transform) || hit.IsChildOf(_player) || (held != null && hit.IsChildOf(held.transform))) continue;
            return false;
        }
        return true;
    }
    public bool CanCapturePlayer()
    {
        if (!EncounterReady || !GameplayAvailable || _playerBody == null || _captureOrigin == null) return false;
        if (IsPlayerProtected) return false;
        Vector3 origin = _captureOrigin.position;
        if (Mathf.Abs(_playerBody.bounds.center.y - origin.y) > _captureVerticalTolerance) return false;
        Vector3 closest = _playerBody.ClosestPoint(origin);
        return (closest - origin).sqrMagnitude <= _captureDistance * _captureDistance && ClearLine(origin, _playerBody.bounds.center);
    }
    public void BeginCapture()
    {
        if (_suspended) { _animator.speed = _savedAnimatorSpeed; _suspended = false; }
        EnterState(EnemyState.Capture);
        if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;
        Vector3 direction = _player.position - transform.position; direction.y = 0;
        if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(direction);
        _animator.SetFloat(Playback, 1f);
        _animator.CrossFadeInFixedTime(Idle, _animationBlend);
        _animationHash = Idle;
    }
    public void AbortCapture(){ EnterState(EnemyState.Roam); _animationHash = 0; }
    private void AnimateLocomotion()
    {
        bool moving = _agent.velocity.sqrMagnitude > 0.01f;
        int hash = !moving ? Idle : State == EnemyState.Chase ? Run : Walk;
        _animator.SetFloat(Playback, !moving ? 1 : State == EnemyState.Chase ? _runPlayback : _walkPlayback);
        if (_animationHash != hash) { _animator.CrossFadeInFixedTime(hash, _animationBlend); _animationHash = hash; }
    }
    private void OnDisable()
    {
        if (_chaseAudio != null) _chaseAudio.Stop();
        if (_footstepAudio != null) _footstepAudio.Stop();
        if (_agent != null && _agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;
        if (_suspended && _animator != null) { _animator.speed = _savedAnimatorSpeed; _suspended = false; }
    }
    private void OnValidate()
    {
        _walkingSpeed = Mathf.Max(0, _walkingSpeed); _runningSpeed = Mathf.Max(0, _runningSpeed);
        _proximityChaseRadius = Mathf.Max(0, _proximityChaseRadius);
        _perceptionInterval = Mathf.Max(0.02f, _perceptionInterval);_repathInterval = Mathf.Max(0.02f, _repathInterval);
        _searchDuration = Mathf.Max(0, _searchDuration); _investigationTimeout = Mathf.Max(0, _investigationTimeout);
        _destinationAttempts = Mathf.Clamp(_destinationAttempts, 1, 32); _samplingDistance = Mathf.Max(0.1f, _samplingDistance);
    }
    private void OnDrawGizmosSelected()
    {
        if (!_drawGizmos) return;
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, _sightRange);
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0, _fieldOfView * 0.5f, 0) * transform.forward * _sightRange);
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0, -_fieldOfView * 0.5f, 0) * transform.forward * _sightRange);
        if (Application.isPlaying) { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(LastSeenPosition, _searchRadius); }
        if (_captureOrigin != null)
        {
            Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(_captureOrigin.position, _proximityChaseRadius);
            Gizmos.color = Color.red; Gizmos.DrawWireSphere(_captureOrigin.position, _captureDistance);
        }
        if (_streetPoints != null)foreach (var point in _streetPoints) if (point != null) { Gizmos.color = Color.green; Gizmos.DrawSphere(point.position, 0.25f); }
    }
}
