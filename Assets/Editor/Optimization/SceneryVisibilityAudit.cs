using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SceneryVisibilityAudit
{
    [MenuItem("Tools/Optimization/Audit Selected Scenery Root")]
    private static void AuditSelected()
    {
        if (EditorApplication.isPlaying || Selection.activeTransform == null)
        { Debug.LogWarning("Select a scenery root in Edit Mode. This command only audits; it does not modify the scene."); return; }
        Audit(Selection.activeTransform, out var report);
        Debug.Log(report, Selection.activeTransform);
    }

    public static MeshRenderer[] Audit(Transform root, out string report)
    {
        if (root == null || EditorApplication.isPlaying) throw new System.InvalidOperationException("Audit requires a scene root in Edit Mode.");
        var components = new List<Component>(8);
        var candidates = new List<MeshRenderer>();
        var targetIds = new HashSet<int>();
        int rejected = 0, referenced = 0;
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(false))
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                !SceneryVisibilityRegistry.IsSafe(renderer, root, components)) { rejected++; continue; }
            candidates.Add(renderer);
            targetIds.Add(renderer.GetInstanceID());
            targetIds.Add(renderer.gameObject.GetInstanceID());
            targetIds.Add(renderer.transform.GetInstanceID());
            targetIds.Add(renderer.GetComponent<MeshFilter>().GetInstanceID());
        }
        var incoming = new HashSet<int>();
        foreach (var sceneRoot in root.gameObject.scene.GetRootGameObjects())
        foreach (var component in sceneRoot.GetComponentsInChildren<Component>(true))
        {
            if (component == null || component is SceneryVisibilityRegistry || component is CameraSceneryVisibility ||
                !(component is MonoBehaviour || component is LODGroup || component is SkinnedMeshRenderer || component is Joint)) continue;
            using (var serialized = new SerializedObject(component))
            {
                var property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference || property.propertyPath == "m_GameObject") continue;
                    var value = property.objectReferenceValue;
                    if (value != null && targetIds.Contains(value.GetInstanceID())) incoming.Add(value.GetInstanceID());
                }
            }
        }
        var result = new List<MeshRenderer>(candidates.Count);
        foreach (var renderer in candidates)
        {
            if (incoming.Contains(renderer.GetInstanceID()) || incoming.Contains(renderer.gameObject.GetInstanceID()) ||
                incoming.Contains(renderer.transform.GetInstanceID()) || incoming.Contains(renderer.GetComponent<MeshFilter>().GetInstanceID()))
            { referenced++; continue; }
            result.Add(renderer);
        }
        report = "Scenery audit: " + root.name + "; safe unreferenced visual leaves=" + result.Count +
            "; excluded component/ancestor/tag/LOD/baked configuration=" + rejected + "; excluded referenced visuals=" + referenced +
            ". No objects, physics, materials or prefab assets changed.";
        return result.ToArray();
    }
}
