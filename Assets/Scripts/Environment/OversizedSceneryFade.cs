using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class OversizedSceneryFade : MonoBehaviour
{
    [Header("Explicit Scene References")]
    [SerializeField] private Camera _camera;
    [SerializeField] private Renderer[] _renderers = Array.Empty<Renderer>();

    [Header("Whole-Object Distance Fade")]
    [Tooltip("Distance from the nearest point of a prop's bounds before fading begins.")]
    [Min(0f)] [SerializeField] private float _fadeStart = 45f;
    [Tooltip("Distance from the nearest bounds point at which the whole prop is hidden.")]
    [Min(1f)] [SerializeField] private float _fadeEnd = 100f;
    [Tooltip("Also hide the whole prop before its farthest bounds corner can be clipped.")]
    [Min(1f)] [SerializeField] private float _clipSafetyRange = 50f;
    [Min(1f)] [SerializeField] private float _clipMargin = 10f;
    [SerializeField] private bool _fadeEnabled = true;

    public int RegisteredCount => _entries == null ? 0 : _entries.Length;
    public Camera TargetCamera => _camera;

    private static readonly int VisibilityId = Shader.PropertyToID("_SceneryVisibility");
    private Entry[] _entries;
    private MaterialPropertyBlock _block;

    private struct Entry
    {
        public Renderer Renderer;
        public int MaterialIndex;
        public float OriginalVisibility;
    }

    private void OnEnable()
    {
        if (_block == null) _block = new MaterialPropertyBlock();
        CacheEntries();
        RenderPipelineManager.beginContextRendering += BeforeContext;
        RenderPipelineManager.beginCameraRendering += BeforeCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginContextRendering -= BeforeContext;
        RenderPipelineManager.beginCameraRendering -= BeforeCamera;
        RestoreOwned();
        _entries = null;
    }

    private void OnValidate()
    {
        _fadeStart = IsFinite(_fadeStart) ? Mathf.Max(0f, _fadeStart) : 45f;
        _fadeEnd = IsFinite(_fadeEnd) ? Mathf.Max(_fadeStart + 1f, _fadeEnd) : _fadeStart + 55f;
        _clipSafetyRange = IsFinite(_clipSafetyRange) ? Mathf.Max(1f, _clipSafetyRange) : 50f;
        _clipMargin = IsFinite(_clipMargin) ? Mathf.Max(1f, _clipMargin) : 10f;
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private void CacheEntries()
    {
        var entries = new List<Entry>(_renderers.Length);
        foreach (var renderer in _renderers)
        {
            if (renderer == null) continue;
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (material == null || !material.HasProperty(VisibilityId)) continue;
                renderer.GetPropertyBlock(_block, i);
                // Preserve the scope already used by other systems. If no indexed
                // block exists, merge into the renderer-level block instead.
                int index = _block.isEmpty ? -1 : i;
                if (index < 0) renderer.GetPropertyBlock(_block);
                float original = _block.HasFloat(VisibilityId) ? _block.GetFloat(VisibilityId) : material.GetFloat(VisibilityId);
                entries.Add(new Entry { Renderer = renderer, MaterialIndex = index, OriginalVisibility = original });
            }
        }
        _entries = entries.ToArray();
    }

    private void BeforeContext(ScriptableRenderContext context, List<Camera> cameras)
    {
        // Compute before rendering and shadow preparation; all shader passes share
        // the property value from the actual gameplay camera, not shadow distance.
        ApplyForCamera(_camera);
    }

    private void BeforeCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera == null || camera.cameraType == CameraType.Preview) return;
        if (camera == _camera) ApplyForCamera(camera);
        else RestoreOwned(); // Keep auxiliary/reflection/Scene View contribution intact.
    }

    private void LateUpdate()
    {
        if (!_fadeEnabled || _camera == null || !_camera.isActiveAndEnabled) RestoreOwned();
    }

    private void ApplyForCamera(Camera camera)
    {
        if (_entries == null) return;
        if (!_fadeEnabled || camera == null || !camera.isActiveAndEnabled) { RestoreOwned(); return; }
        Vector3 position = camera.transform.position;
        Vector3 forward = camera.transform.forward;
        float range = Mathf.Max(1f, _fadeEnd - _fadeStart);
        foreach (var entry in _entries)
        {
            if (entry.Renderer == null) continue;
            Bounds bounds = entry.Renderer.bounds;
            float distance = Vector3.Distance(position, bounds.ClosestPoint(position));
            float distanceT = Mathf.Clamp01((distance - _fadeStart) / range);
            float visibility = 1f - distanceT * distanceT * (3f - 2f * distanceT);
            Vector3 extents = bounds.extents;
            float farthestDepth = Vector3.Dot(bounds.center - position, forward) +
                Mathf.Abs(forward.x) * extents.x + Mathf.Abs(forward.y) * extents.y + Mathf.Abs(forward.z) * extents.z;
            float safeEnd = camera.farClipPlane - _clipMargin;
            float clipT = Mathf.Clamp01((farthestDepth - (safeEnd - _clipSafetyRange)) / _clipSafetyRange);
            float clipVisibility = 1f - clipT * clipT * (3f - 2f * clipT);
            SetVisibility(entry, Mathf.Min(visibility, clipVisibility) * entry.OriginalVisibility);
        }
    }

    private void SetVisibility(Entry entry, float visibility)
    {
        if (entry.MaterialIndex < 0)
        {
            entry.Renderer.GetPropertyBlock(_block);
            _block.SetFloat(VisibilityId, visibility);
            entry.Renderer.SetPropertyBlock(_block);
        }
        else
        {
            entry.Renderer.GetPropertyBlock(_block, entry.MaterialIndex);
            _block.SetFloat(VisibilityId, visibility);
            entry.Renderer.SetPropertyBlock(_block, entry.MaterialIndex);
        }
    }

    private void RestoreOwned()
    {
        if (_entries == null) return;
        foreach (var entry in _entries)
            if (entry.Renderer != null) SetVisibility(entry, entry.OriginalVisibility);
    }
}
