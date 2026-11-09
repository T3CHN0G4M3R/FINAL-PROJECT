using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SaveIdValidator
{
    [MenuItem("Tools/Save Game/Validate active gameplay scene IDs")]
    private static void ValidateActiveScene()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (!SaveGameData.SupportedScene(scene.path)) { Debug.LogWarning("Select Neighborhood Scene or FarmScene"); return; }
        var ids = new HashSet<string>();
        int count = 0;
        foreach (var root in scene.GetRootGameObjects()) foreach (var id in root.GetComponentsInChildren<PersistentSaveId>(true))
        {
            count++;
            if (string.IsNullOrWhiteSpace(id.Id) || !ids.Add(id.Id)) Debug.LogError("Missing or duplicate save ID", id);
        }
        Debug.Log("Checked " + count + " scene save IDs");
    }

    public static PersistentSaveId EnsureSceneId(GameObject obj, HashSet<string> assigned)
    {
        if (!SaveGameData.SupportedScene(obj.scene.path)) throw new InvalidOperationException("Only supported gameplay scene instances may receive save IDs");
        // Read every component before adding scene-owned persistence metadata.
        foreach (var component in obj.GetComponents<Component>()) if (component == null) throw new InvalidOperationException("Missing script on " + obj.name);
        var id = obj.GetComponent<PersistentSaveId>();
        if (id == null) id = Undo.AddComponent<PersistentSaveId>(obj);
        if (string.IsNullOrWhiteSpace(id.Id))
        {
            var so = new SerializedObject(id);
            so.FindProperty("_id").stringValue = Guid.NewGuid().ToString("N");
            so.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(id);
        }
        if (!assigned.Add(id.Id)) throw new InvalidOperationException("Duplicate save ID on " + obj.name + "; resolve the copied instance explicitly");
        return id;
    }
}
