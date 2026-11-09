using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public sealed class SaveFileRepository
{
    private const long MaximumBytes = 4 * 1024 * 1024;
    public string DirectoryPath { get; }
    public SaveFileRepository(string directory = null) { DirectoryPath = directory ?? Path.Combine(Application.persistentDataPath, "Saves"); }
    public string SlotPath(int slot)
    {
        if (slot < 0 || slot > 3) throw new ArgumentOutOfRangeException(nameof(slot));
        return Path.Combine(DirectoryPath, slot == 0 ? "autosave.json" : "slot" + slot + ".json");
    }
    private SaveGameData ReadFile(string path, int slot)
    {
        if (new FileInfo(path).Length > MaximumBytes) throw new InvalidDataException("Save exceeds size limit");
        var data = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(path, Encoding.UTF8));
        string error = data == null ? "Unreadable save" : data.Validate();
        if (error != null || data.slot != slot) throw new InvalidDataException(error ?? "Incorrect slot");
        return data;
    }
    public SaveSlotInfo Read(int slot)
    {
        try
        {
            string path = SlotPath(slot);
            if (!File.Exists(path)) return new SaveSlotInfo(slot, null, File.Exists(path + ".bak") ? "Backup available - select Recover" : "Empty");
            var data = ReadFile(path, slot);
            if (!Application.CanStreamedLevelBeLoaded(data.currentScene)) return new SaveSlotInfo(slot, null, "Scene missing or disabled");
            return new SaveSlotInfo(slot, data, null);
        }
        catch (Exception e) { return new SaveSlotInfo(slot, null, "Unavailable: " + e.Message); }
    }
    public SaveSlotInfo[] List() => Enumerable.Range(0, 4).Select(Read).ToArray();
    public SaveSlotInfo Newest() => List().Where(s => s.Valid).OrderByDescending(s => s.Data.commitOrder).ThenByDescending(s => s.Data.utcTicks).ThenBy(s => s.Slot).FirstOrDefault();
    public bool Exists(int slot) => File.Exists(SlotPath(slot)) || File.Exists(SlotPath(slot) + ".bak");
    public bool HasBackup(int slot)
    {
        try { ReadFile(SlotPath(slot) + ".bak", slot); return true; } catch { return false; }
    }
    public bool Recover(int slot, out string error)
    {
        try
        {
            var data = ReadFile(SlotPath(slot) + ".bak", slot);
            if (!Application.CanStreamedLevelBeLoaded(data.currentScene)) throw new InvalidDataException("Backup scene missing or disabled");
            return Commit(data, false, out error);
        }
        catch (Exception e) { error = e.Message; return false; }
    }
    public bool Commit(SaveGameData data, out string error) => Commit(data, true, out error);
    private bool Commit(SaveGameData data, bool updateMetadata, out string error)
    {
        string temp = null;
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            if (updateMetadata)
            {
                long max = List().Where(s => s.Valid).Select(s => s.Data.commitOrder).DefaultIfEmpty(0).Max();
                data.utcTicks = DateTime.UtcNow.Ticks;
                data.commitOrder = Math.Max(data.utcTicks, checked(max + 1));
            }
            string validation = data.Validate();
            if (validation != null) throw new InvalidDataException(validation);
            string target = SlotPath(data.slot);
            temp = target + ".tmp";
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data, true));
            if (bytes.Length > MaximumBytes) throw new InvalidDataException("Save exceeds size limit");
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None)) { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            ReadFile(temp, data.slot);
            if (File.Exists(target)) File.Replace(temp, target, target + ".bak", true);
            else File.Move(temp, target);
            error = null;
            return true;
        }
        catch (Exception e) { error = e.Message; return false; }
        finally { if (temp != null) { try { if (File.Exists(temp)) File.Delete(temp); } catch { } } }
    }
    public bool ResetAutosave(out string error)
    {
        try
        {
            // Rename first: an IO failure never destroys the existing record.
            string path = SlotPath(0);
            string archive = path + ".previous";
            if (File.Exists(path)) { if (File.Exists(archive)) File.Replace(path, archive, null); else File.Move(path, archive); }
            string backup = path + ".bak";
            if (File.Exists(backup)) File.Delete(backup);
            error = null; return true;
        }
        catch (Exception e) { error = e.Message; return false; }
    }
}
