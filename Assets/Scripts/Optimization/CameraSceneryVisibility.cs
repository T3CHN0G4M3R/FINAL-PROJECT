using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[DefaultExecutionOrder(32000)]
public sealed class CameraSceneryVisibility : MonoBehaviour
{
    [Header("Explicit Scene References")]
    [SerializeField] private Camera _camera;
    [SerializeField] private SceneryVisibilityRegistry _registry;
    [Tooltip("Include shadowed lights even when currently inactive (e.g. sky sun/moon and held flashlight).")]
    [SerializeField] private Light[] _lights = Array.Empty<Light>();
    [Header("Conservative Visibility")]
    [Tooltip("Activation control, not asset unloading. Leave off unless profiling confirms a benefit.")]
    [SerializeField] private bool _visibilityEnabled;
    [Min(0)] [SerializeField] private float _nearbyRadius = 15f;
    [Min(0)] [SerializeField] private float _boundsPadding = 3f;
    [Min(0)] [SerializeField] private float _offscreenGrace = 0.75f;
    [Min(1)] [SerializeField] private int _deactivationsPerFrame = 128;
    [Min(1)] [SerializeField] private int _validationEntriesPerFrame = 32;
    public bool VisibilityEnabled => _visibilityEnabled;
    public int RegisteredCount { get; private set; }
    public int InactiveCount { get; private set; }
    public int ActiveCount => RegisteredCount - InactiveCount;
    public int ShadowProtectedCount { get; private set; }
    public string SafetyStatus { get; private set; }
    public Camera TargetCamera => _camera;

    private SceneryVisibilityRegistry.Cell[] _cells;
    private Bounds _worldBounds;
    private readonly Plane[] _planes = new Plane[6];
    private readonly Vector3[] _shadowCorners = new Vector3[4];
    private readonly List<Component> _components = new List<Component>(8);
    private int _deactivationCell, _deactivationEntry, _validationCell, _validationEntry;
    private int _lastValidationFrame = -1, _lastDeactivationFrame = -1;
    private static readonly Unity.Profiling.ProfilerMarker VisibilityMarker = new Unity.Profiling.ProfilerMarker("SceneryVisibility.Evaluate");
    private static readonly Unity.Profiling.ProfilerMarker ActivationMarker = new Unity.Profiling.ProfilerMarker("SceneryVisibility.Activation");

    private SceneryVisibilityRegistry _builtRegistry;
    private List<Camera> _contextCameras;
    private void InitializeRegistry()
    {
        if (_cells != null || _registry == null || !_visibilityEnabled || !Application.isPlaying) return;
        _builtRegistry = _registry;
        _cells = _registry.Build(out _worldBounds);
        RegisteredCount = 0;
        foreach (var cell in _cells) RegisteredCount += cell.Entries.Length;
    }
    private void OnEnable()
    {
        InitializeRegistry();
        RenderPipelineManager.beginContextRendering += BeforeContext;
        RenderPipelineManager.beginCameraRendering += BeforeCamera;
    }
    private void OnDisable()
    {
        RenderPipelineManager.beginContextRendering -= BeforeContext;
        RenderPipelineManager.beginCameraRendering -= BeforeCamera;
        RestoreOwned();
        _cells = null;
        _builtRegistry = null;
        _contextCameras = null;
        _deactivationCell = _deactivationEntry = _validationCell = _validationEntry = 0;
        _lastValidationFrame = _lastDeactivationFrame = -1;
    }
    private void LateUpdate()
    {
        if (!_visibilityEnabled || _camera == null || !_camera.isActiveAndEnabled || _registry == null ||
            GraphicsSettings.currentRenderPipeline == null) RestoreOwned();
    }
    public void SetVisibilityEnabled(bool value)
    {
        _visibilityEnabled = value;
        if (!value) RestoreOwned();
    }
    private void BeforeCamera(ScriptableRenderContext context, Camera camera)
    {
        // Reflection render requests may occur outside the ordinary camera list. Never hide their contributors.
        if (camera != null && (camera.cameraType == CameraType.Reflection ||
            (camera.cameraType == CameraType.Game && camera != _camera && (_contextCameras == null || !_contextCameras.Contains(camera))))) RestoreOwned();
    }
    private void BeforeContext(ScriptableRenderContext context, List<Camera> cameras)
    {
        InitializeRegistry();
        _contextCameras = cameras;
        if (!Application.isPlaying || _cells == null || !_visibilityEnabled || _camera == null ||
            !_camera.isActiveAndEnabled || _registry == null || _registry != _builtRegistry) { RestoreOwned(); return; }
        var pipeline = GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
        if (pipeline == null || _camera.stereoEnabled) { SafetyStatus = "Unsupported pipeline/stereo: original activation retained"; RestoreOwned(); return; }
        bool targetPresent = false;
        foreach (var camera in cameras)
        {
            if (camera == _camera) targetPresent = true;
            if (camera != null && camera.cameraType == CameraType.Reflection)
            { SafetyStatus = "Reflection capture: original activation retained"; RestoreOwned(); return; }
        }
        // Editor Scene View contexts must not undo game-camera decisions every frame.
        if (!targetPresent) return;
        using (VisibilityMarker.Auto())
        {
            if (_lastValidationFrame != Time.frameCount)
            { _lastValidationFrame = Time.frameCount; ValidateSome(); }
            foreach (var cell in _cells) { cell.Required = false; cell.ShadowProtected = false; }
            foreach (var camera in cameras)
            {
                if (camera == null || !camera.isActiveAndEnabled || camera.cameraType != CameraType.Game) continue;
                GeometryUtility.CalculateFrustumPlanes(camera, _planes);
                Vector3 position = camera.transform.position;
                float radiusSquared = _nearbyRadius * _nearbyRadius;
                foreach (var cell in _cells)
                {
                    Bounds padded = cell.Bounds; padded.Expand(_boundsPadding * 2f);
                    if (GeometryUtility.TestPlanesAABB(_planes, padded) || padded.SqrDistance(position) <= radiusSquared)
                        cell.Required = true;
                }
                ProtectShadows(camera, pipeline.shadowDistance);
            }
            ShadowProtectedCount = 0;
            float now = Time.unscaledTime;
            foreach (var cell in _cells)
            {
                if (cell.ShadowProtected) ShadowProtectedCount += cell.Entries.Length;
                if (!cell.Required) continue;
                cell.LastRequired = now;
                if (cell.InactiveCount == 0) continue;
                foreach (var entry in cell.Entries) Activate(cell, entry);
            }
            if (_lastDeactivationFrame != Time.frameCount)
            { _lastDeactivationFrame = Time.frameCount; DeactivateSome(now); }
            SafetyStatus = "Audited visuals only; collision, AI, LODs and shadows retained";
        }
    }
    private void ProtectShadows(Camera camera, float shadowDistance)
    {
        // A sphere/AABB around the camera is more conservative than the shadow receiver frustum,
        // including cascade padding, wide FOVs and receivers just outside the view edge.
        float depth = Mathf.Min(camera.farClipPlane, Mathf.Max(0, shadowDistance));
        if (depth <= 0) return;
        camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), depth, Camera.MonoOrStereoscopicEye.Mono, _shadowCorners);
        float radius = depth;
        for (int i = 0; i < _shadowCorners.Length; i++) radius = Mathf.Max(radius, _shadowCorners[i].magnitude);
        radius += _boundsPadding;
        Bounds receivers = new Bounds(camera.transform.position, Vector3.one * (radius * 2f));
        foreach (var light in _lights)
        {
            if (light == null || !light.isActiveAndEnabled || light.shadows == LightShadows.None || light.intensity <= 0) continue;
            if (light.type == LightType.Directional)
            {
                // Swept receiver box toward the light, long enough to include any registered caster.
                // This is deliberately loose at low sun angles to avoid disappearing off-screen shadows.
                Vector3 towardLight = -light.transform.forward;
                Vector3 absolute = new Vector3(Mathf.Abs(towardLight.x), Mathf.Abs(towardLight.y), Mathf.Abs(towardLight.z));
                float length = Mathf.Max(0, Vector3.Dot(_worldBounds.center - receivers.center, towardLight) +
                    Vector3.Dot(_worldBounds.extents, absolute) + receivers.extents.magnitude);
                Bounds swept = receivers;
                swept.Encapsulate(new Bounds(receivers.center + towardLight * length, receivers.size));
                foreach (var cell in _cells)
                    if (swept.Intersects(cell.Bounds)) { cell.Required = true; cell.ShadowProtected = true; }
            }
            else if (light.type == LightType.Point || light.type == LightType.Spot)
            {
                float rangeSquared = light.range * light.range;
                if (receivers.SqrDistance(light.transform.position) > rangeSquared) continue;
                foreach (var cell in _cells)
                    if (cell.Bounds.SqrDistance(light.transform.position) <= rangeSquared)
                    { cell.Required = true; cell.ShadowProtected = true; }
            }
            else
            {
                foreach (var cell in _cells) { cell.Required = true; cell.ShadowProtected = true; }
            }
        }
    }
    private bool CheckEntry(SceneryVisibilityRegistry.Cell cell, SceneryVisibilityRegistry.Entry entry)
    {
        if (entry.Retired) return false;
        bool unsafeEntry = entry.Renderer == null || entry.Object == null || entry.Transform == null ||
            entry.Transform.parent != entry.Parent || entry.Transform.localToWorldMatrix != entry.Matrix ||
            !entry.Renderer.enabled;
        if (!unsafeEntry) unsafeEntry = !SceneryVisibilityRegistry.IsSafe(entry.Renderer, _registry.SceneryRoot, _components);
        if (unsafeEntry) { Activate(cell, entry); entry.Retired = true; return false; }
        if (!entry.OwnedInactive && !entry.Object.activeSelf) { entry.Retired = true; return false; }
        if (entry.OwnedInactive && entry.Object.activeSelf)
        {
            // Another owner reactivated it: relinquish control permanently for this session.
            entry.OwnedInactive = false; cell.InactiveCount--; InactiveCount--; entry.Retired = true; return false;
        }
        return true;
    }
    private void Activate(SceneryVisibilityRegistry.Cell cell, SceneryVisibilityRegistry.Entry entry)
    {
        if (!entry.OwnedInactive) return;
        using (ActivationMarker.Auto())
        {
            if (entry.Object != null && !entry.Object.activeSelf) entry.Object.SetActive(true);
            entry.OwnedInactive = false; cell.InactiveCount--; InactiveCount--;
        }
    }
    private void DeactivateSome(float now)
    {
        if (_cells.Length == 0) return;
        int budget = Mathf.Max(1, _deactivationsPerFrame);
        int visitedCells = 0;
        while (budget > 0 && visitedCells < _cells.Length)
        {
            var cell = _cells[_deactivationCell];
            if (cell.Required || now - cell.LastRequired < _offscreenGrace || _deactivationEntry >= cell.Entries.Length)
            { _deactivationCell = (_deactivationCell + 1) % _cells.Length; _deactivationEntry = 0; visitedCells++; continue; }
            var entry = cell.Entries[_deactivationEntry++];
            // Charge inspections too, avoiding an unbounded walk through externally disabled entries.
            budget--;
            if (entry.OwnedInactive || !CheckEntry(cell, entry) || !entry.Object.activeInHierarchy) continue;
            using (ActivationMarker.Auto()) entry.Object.SetActive(false);
            entry.OwnedInactive = true; cell.InactiveCount++; InactiveCount++;
        }
    }
    private void ValidateSome()
    {
        if (_cells.Length == 0) return;
        for (int i = 0; i < Mathf.Max(1, _validationEntriesPerFrame); i++)
        {
            var cell = _cells[_validationCell];
            if (_validationEntry >= cell.Entries.Length)
            { _validationEntry = 0; _validationCell = (_validationCell + 1) % _cells.Length; cell = _cells[_validationCell]; }
            CheckEntry(cell, cell.Entries[_validationEntry++]);
        }
    }
    private void RestoreOwned()
    {
        if (_cells == null || InactiveCount == 0) return;
        foreach (var cell in _cells)
        {
            if (cell.InactiveCount == 0) continue;
            foreach (var entry in cell.Entries) Activate(cell, entry);
            cell.LastRequired = Time.unscaledTime;
        }
    }
}
