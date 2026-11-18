using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[DisallowMultipleComponent]
public sealed class VehicleInteraction : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Collider _vehicleCollider;
    [SerializeField] private Transform _player;
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private InteractionSystem _interactions;

    [Header("Interaction Settings")]
    [SerializeField] private float _interactionDistance = 4f;
    [SerializeField] private LayerMask _interactableLayers = ~0;

    [Header("Transition")]
    [SerializeField] private string _targetScene = "Assets/Scenes/CarScene.unity";
    [SerializeField] private UIAlphaFadeEffect _fadeEffect;
    [SerializeField] private UnityEngine.UI.Image _fadeImagePrefab;
    [SerializeField] private float _fadeOutSeconds = 1f;
    [SerializeField] private float _fadeInSeconds = 1f;
    [SerializeField] private float _blackHoldSeconds = 0.2f;

    public string PromptMessage { get; private set; }
    private bool _isTransitioning = false;

    private Collider _playerBody;

    private void Awake()
    {
        if (_player != null) _playerBody = _player.GetComponent<Collider>();
    }

    public bool CanInteract(Transform player, Camera view)
    {
        if (!isActiveAndEnabled || _vehicleCollider == null || !_vehicleCollider.enabled ||
            player == null || player != _player || view == null || PauseMenuManager.IsGamePaused ||
            (GameSaveManager.Instance != null && (GameSaveManager.Instance.Busy || GameSaveManager.Instance.EncounterInProgress))) return false;

        Vector3 playerPosition = _playerBody != null ? _playerBody.bounds.center : player.position;
        // Bounds also support the car's non-convex MeshCollider. No aiming is required.
        Vector3 nearestPoint = _vehicleCollider.bounds.ClosestPoint(playerPosition);
        if ((nearestPoint - playerPosition).sqrMagnitude > _interactionDistance * _interactionDistance) return false;

        PromptMessage = GameSaveManager.Instance != null && GameSaveManager.Instance.HasBaguette
            ? "Press F to break the window"
            : "Locked until Baguette is obtained";
        return true;
    }

    public bool TryInteract(Transform player, Camera view)
    {
        if (!CanInteract(player, view) || GameSaveManager.Instance == null || !GameSaveManager.Instance.HasBaguette) return false;
        bool started = GameplayFadeTravel.Begin(_targetScene, false, _fadeEffect, _fadeImagePrefab,
            _fadeOutSeconds, _fadeInSeconds, _blackHoldSeconds, out string error);
        _isTransitioning = started;
        if (!started)
        {
            PromptMessage = error;
            GameSaveManager.Instance.Report(error);
        }
        return started;
    }
}