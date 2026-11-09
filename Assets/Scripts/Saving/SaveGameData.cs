using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SaveGameData
{
    public int version = 1;
    public string runId;
    public int slot;
    public long utcTicks;
    public long commitOrder;
    public string currentScene;
    public List<SavedSceneState> scenes = new List<SavedSceneState>();

    public static bool SupportedScene(string path) => path == "Assets/Scenes/Neighborhood Scene.unity" || path == "Assets/Scenes/FarmScene.unity";
    public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && Mathf.Abs(value) < 1000000f;
    public static bool Finite(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);
    public static bool RotationValid(Quaternion q)
    {
        float norm = q.x*q.x + q.y*q.y + q.z*q.z + q.w*q.w;
        return Finite(q.x) && Finite(q.y) && Finite(q.z) && Finite(q.w) && Mathf.Abs(norm - 1f) < 0.01f;
    }
    public string Validate()
    {
        if (version != 1) return "Incompatible save version";
        if (string.IsNullOrWhiteSpace(runId) || runId.Length > 64 || slot < 0 || slot > 3 || utcTicks <= 0 || utcTicks > DateTime.MaxValue.Ticks || commitOrder <= 0) return "Invalid save metadata";
        if (!SupportedScene(currentScene) || scenes == null || scenes.Count == 0 || scenes.Count > 2) return "Invalid saved scene";
        var paths = new HashSet<string>();
        foreach (var scene in scenes)
        {
            if (scene == null || !SupportedScene(scene.path) || !paths.Add(scene.path) || !Finite(scene.position) || !Finite(scene.yaw) || !Finite(scene.pitch) || Mathf.Abs(scene.pitch) > 90.01f || scene.objects == null || scene.objects.Count > 2000) return "Invalid scene state";
            var ids = new HashSet<string>();
            bool heldFound = string.IsNullOrEmpty(scene.heldId);
            foreach (var item in scene.objects)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.id) || item.id.Length > 64 || !ids.Add(item.id) || !Finite(item.position) || !Finite(item.scale) || !RotationValid(item.rotation) || (item.kind != 0 && item.kind != 1)) return "Invalid object state";
                if (item.id == scene.heldId) { if (item.kind != 0 || !item.active) return "Invalid held item"; heldFound = true; }
            }
            if (!heldFound) return "Missing held item";
        }
        return paths.Contains(currentScene) ? null : "Missing current scene state";
    }
}

[Serializable]
public sealed class SavedSceneState
{
    public string path;
    public Vector3 position;
    public float yaw;
    public float pitch;
    public string heldId;
    public List<SavedObjectState> objects = new List<SavedObjectState>();
}

[Serializable]
public sealed class SavedObjectState
{
    public string id;
    public int kind; // 0 pickup, 1 door
    public Vector3 position;
    public Quaternion rotation = Quaternion.identity;
    public Vector3 scale = Vector3.one;
    public bool active = true;
    public bool inUse;
}

public sealed class SaveSlotInfo
{
    public int Slot { get; }
    public SaveGameData Data { get; }
    public string Error { get; }
    public bool Valid => Data != null && Error == null;
    public SaveSlotInfo(int slot, SaveGameData data, string error) { Slot = slot; Data = data; Error = error; }
    public string Label
    {
        get
        {
            string name = Slot == 0 ? "Autosave" : "Slot " + Slot;
            if (!Valid) return name + " - " + (Error ?? "Empty");
            return name + " - " + System.IO.Path.GetFileNameWithoutExtension(Data.currentScene) + "\n" + new DateTime(Data.utcTicks, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
