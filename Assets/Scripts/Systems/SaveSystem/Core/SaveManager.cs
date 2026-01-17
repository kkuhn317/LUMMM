using System;
using System.IO;
using UnityEngine;

public static class SaveManager
{
    private static readonly IDataService dataService = 
        new FileDataService(new JsonSerializer());
    
    public static int CurrentVersion = 1;
    public static SaveData Current { get; private set; } = new SaveData();
    public static int CurrentSlot { get; private set; } = 0;
    
    public static string GetSlotFileName(int slot)
    {
        return $"save_slot_{slot}";
    }
    
    public static bool SlotExists(int slot)
    {
        string fileName = GetSlotFileName(slot);

        // resolve the full path for that file
        var dataService = new FileDataService(new JsonSerializer());
        string fullPath = dataService.GetSavePath(fileName);

        // Check if the file actually exists on disk
        return File.Exists(fullPath);
    }
    
    public static void Delete(int slot)
    {
        string fileName = GetSlotFileName(slot);
        dataService.Delete(fileName);
    }
    
    public static void Load(int slot)
    {
        CurrentSlot = slot;
        string fileName = GetSlotFileName(slot);
        
        var loaded = dataService.Load(fileName);
        if (loaded != null)
        {
            Current = loaded;
        }
        else
        {
            // New save for this slot
            Current = new SaveData 
            { 
                version = CurrentVersion,
                profileName = $"PLAYER {(char)('A' + slot)}"
            };
            
            // Initialize with defaults
            if (Current.modifiers == null)
                Current.modifiers = new ModifiersData();
            
            Current.modifiers.timeLimitEnabled = false;
            Current.modifiers.infiniteLivesEnabled = false;
            Current.modifiers.checkpointMode = 0;
            
            // Save the new file
            Save();
        }
    }
    
    public static void Save()
    {
        string fileName = GetSlotFileName(CurrentSlot);
        Current.version = CurrentVersion;
        dataService.Save(Current, fileName, overwrite: true);
    }
    
    public static SaveFileSummary BuildSummary(LevelInfo[] allLevelInfos)
    {
        var summary = new SaveFileSummary();
        if (allLevelInfos == null || allLevelInfos.Length == 0) return summary;

        summary.totalLevels = allLevelInfos.Length;
        int completed = 0, perfect = 0, collectedGreenCoins = 0, maxGreenCoins = 0;

        foreach (var levelInfo in allLevelInfos)
        {
            // Get green coin count (you need to implement this)
            int greenCoinCount = SaveLoadSystem.Instance?.GetGreenCoinCountForLevel(levelInfo.levelID) ?? 0;
            maxGreenCoins += greenCoinCount;
            
            var lp = Current.levels.Find(l => l.levelID == levelInfo.levelID);

            if (lp != null && lp.completed) completed++;
            if (lp != null && lp.perfect) perfect++;

            if (greenCoinCount > 0 && lp?.greenCoins != null)
            {
                int len = Mathf.Min(greenCoinCount, lp.greenCoins.Length);
                for (int i = 0; i < len; i++) 
                    if (lp.greenCoins[i]) collectedGreenCoins++;
            }
        }

        summary.completedLevels = completed;
        summary.perfectLevels = perfect;
        summary.collectedGreenCoins = collectedGreenCoins;
        summary.maxGreenCoins = maxGreenCoins;
        summary.rewardAllLevels = (completed >= allLevelInfos.Length);
        summary.rewardAllGreenCoins = (collectedGreenCoins >= maxGreenCoins && maxGreenCoins > 0);
        summary.rewardAllPerfect = (perfect >= allLevelInfos.Length);

        return summary;
    }
    
    public static bool HasCheckpointForLevel(string levelID)
    {
        return Current.checkpoint.hasCheckpoint && 
            Current.checkpoint.levelID == levelID;
    }

    public static string ExportSlot(int slot, string targetDirectory, string fileNameWithoutExtension)
    {
        string slotFileName = GetSlotFileName(slot);
        string exportPath = Path.Combine(targetDirectory, fileNameWithoutExtension + FileDataService.SaveExtension);
        
        try
        {
            dataService.Export(slotFileName, exportPath);
            return exportPath;
        }
        catch
        {
            return null;
        }
    }
    
    public static bool ImportSlot(int slot, string sourceFilePath)
    {
        try
        {
            // Validate external file
            if (!File.Exists(sourceFilePath))
            {
                Debug.LogWarning($"[SaveManager] Import failed: Source file not found: {sourceFilePath}");
                return false;
            }

            // Read raw JSON from exported save
            string json = File.ReadAllText(sourceFilePath);

            // Deserialize using the same serializer used by the SaveSystem
            var serializer = new JsonSerializer();
            SaveData loadedData = serializer.Deserialize<SaveData>(json);

            if (loadedData == null)
            {
                Debug.LogError($"[SaveManager] Import failed: Could not deserialize save file: {sourceFilePath}");
                return false;
            }

            // Update profile name based on target slot (optional behavior)
            loadedData.profileName = $"PLAYER {(char)('A' + slot)}";

            // Determine internal slot file name (e.g., save_slot_2)
            string slotFileName = GetSlotFileName(slot);

            // Save into internal SaveSystem path without affecting other slots
            dataService.Save(loadedData, slotFileName, overwrite: true);

            // Reload slot to update UI and internal state
            Load(slot);

            Debug.Log($"[SaveManager] Successfully imported save file into slot {slot}.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] ImportSlot Exception: {ex}");
            return false;
        }
    }
}