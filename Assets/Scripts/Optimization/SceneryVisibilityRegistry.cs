using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SceneryVisibilityRegistry : MonoBehaviour
{
    [Header("Audited Scene References")]
    [SerializeField] private Transform _sceneryRoot;
    [SerializeField] private MeshRenderer[] _visuals = Array.Empty<MeshRenderer>();
    [Min(1)] [SerializeField] private float _cellSize = 12f;
    public int RegisteredCount => _visuals.Length;
    public Transform SceneryRoot => _sceneryRoot;
    public MeshRenderer[] Visuals => _visuals;

    public sealed class Entry
    {
        public MeshRenderer Renderer;
        public Transform Transform;
        public GameObject Object;
        public Transform Parent;
        public Matrix4x4 Matrix;
        public bool OwnedInactive;
        public bool Retired;
    }

    public sealed class Cell
    {
        public Bounds Bounds;
        public Entry[] Entries;
        public float LastRequired;
        public bool Required;
        public bool ShadowProtected;
        public int InactiveCount;
    }

    public Cell[] Build(out Bounds worldBounds)
    {
        var groups = new Dictionary<Vector3Int, List<Entry>>();
        var unique = new HashSet<MeshRenderer>();
        worldBounds = new Bounds();
        bool first = true;
        var components = new List<Component>(8);
        foreach (var renderer in _visuals)
        {
            if (renderer == null || !unique.Add(renderer) || !renderer.gameObject.activeInHierarchy ||
                !renderer.enabled || !IsSafe(renderer, _sceneryRoot, components)) continue;
            var bounds = renderer.bounds;
            if (first) { worldBounds = bounds; first = false; } else worldBounds.Encapsulate(bounds);
            float size = Mathf.Max(1f, _cellSize);
            var key = new Vector3Int(Mathf.FloorToInt(bounds.center.x / size),
                Mathf.FloorToInt(bounds.center.y / size), Mathf.FloorToInt(bounds.center.z / size));
            if (!groups.TryGetValue(key, out var entries)) { entries = new List<Entry>(); groups.Add(key, entries); }
            entries.Add(new Entry { Renderer = renderer, Transform = renderer.transform,
                Object = renderer.gameObject, Parent = renderer.transform.parent, Matrix = renderer.transform.localToWorldMatrix });
        }
        var cells = new Cell[groups.Count];
        int index = 0;
        foreach (var group in groups.Values)
        {
            var bounds = group[0].Renderer.bounds;
            for (int i = 1; i < group.Count; i++) bounds.Encapsulate(group[i].Renderer.bounds);
            cells[index++] = new Cell { Bounds = bounds, Entries = group.ToArray(), LastRequired = Time.unscaledTime };
        }
        return cells;
    }

    // Only the explicitly audited root can own visuals. NavMeshModifier is passive bake metadata.
    public static bool IsSafe(MeshRenderer renderer, Transform root, List<Component> components)
    {
        if (renderer == null || root == null || !renderer.transform.IsChildOf(root) ||
            renderer.transform.childCount != 0 || !renderer.CompareTag("Untagged") ||
            (renderer.lightmapIndex >= 0 && renderer.lightmapIndex < 65534)) return false;
        components.Clear();
        renderer.GetComponents(components);
        if (components.Count != 3) return false;
        foreach (var component in components)
            if (component == null || !(component is Transform || component is MeshRenderer || component is MeshFilter)) return false;
        for (var ancestor = renderer.transform.parent; ancestor != null; ancestor = ancestor.parent)
        {
            if (!ancestor.CompareTag("Untagged")) return false;
            components.Clear(); ancestor.GetComponents(components);
            foreach (var component in components)
            {
                if (component == null || component is Animator || component is Rigidbody || component is LODGroup ||
                    component is ParticleSystem || (component is MonoBehaviour && !(component is Unity.AI.Navigation.NavMeshModifier))) return false;
            }
        }
        return true;
    }
}
