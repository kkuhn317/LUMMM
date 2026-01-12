using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class LevelProgressData
{
    public string levelID;
    public bool completed;
    public bool perfect;
    public bool[] greenCoins;
    public int highestRank;
    public double bestTimeMs;
}

[Serializable]
public class ModifiersData
{
    public bool infiniteLives;
    public bool timeLimitEnabled;
    public int checkpointMode;
}

[Serializable]
public class CheckpointSave
{
    public bool hasCheckpoint;
    public string levelID;
    public int checkpointId;
    public int lives;
    public int coins;
    public double speedrunMs;
    public bool[] greenCoinsInRun;
}

[Serializable]
public class SaveData
{
    public int version = 1;               // for future migrations
    public string profileName = "New File";
    public List<LevelProgressData> levels = new();
    public ModifiersData modifiers = new();
    public CheckpointSave checkpoint = new();
}

public class SaveFileSummary
{
    public int totalLevels;
    public int completedLevels;
    public int perfectLevels;

    public int collectedCoins;
    public int maxCoins;

    public bool rewardAllLevels;
    public bool rewardAllCoins;
    public bool rewardAllPerfect;

    public int StarCount =>
        (rewardAllLevels ? 1 : 0) +
        (rewardAllCoins ? 1 : 0) +
        (rewardAllPerfect ? 1 : 0);
}

[Serializable]
public class LevelDefinition
{
    public string levelID;
    public int greenCoinCount;
}

public static class SaveManager
{
    public const string FileExtension = ".lumm";
    public const int CurrentVersion = 1;

    public static SaveData Current { get; private set; } = new SaveData();
    public static int CurrentSlot { get; private set; } = 0;

    public static string GetSlotFilePath(int slot)
    {
        return Path.Combine(
            Application.persistentDataPath,
            $"save_slot_{slot}{FileExtension}"
        );
    }

    public static bool SlotExists(int slot)
    {
        return File.Exists(GetSlotFilePath(slot));
    }

    public static void Load(int slot)
    {
        CurrentSlot = slot;
        string path = GetSlotFilePath(slot);

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<SaveData>(json);
            Current = data ?? new SaveData();
        }
        else
        {
            Current = new SaveData { version = CurrentVersion };
        }
    }

    public static void Save()
    {
        string path = GetSlotFilePath(CurrentSlot);
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        Current.version = CurrentVersion;

        string json = JsonUtility.ToJson(Current, true);
        File.WriteAllText(path, json);
    }

    // -------- Level helpers --------

    public static LevelProgressData GetLevel(string levelID, int greenCoinCount = 0)
    {
        var level = Current.levels.Find(l => l.levelID == levelID);
        if (level == null)
        {
            level = new LevelProgressData
            {
                levelID = levelID,
                completed = false,
                perfect = false,
                greenCoins = greenCoinCount > 0 ? new bool[greenCoinCount] : Array.Empty<bool>(),
                highestRank = 0,
                bestTimeMs = 0
            };
            Current.levels.Add(level);
        }
        else if (greenCoinCount > 0)
        {
            if (level.greenCoins == null || level.greenCoins.Length < greenCoinCount)
            {
                bool[] newArray = new bool[greenCoinCount];
                if (level.greenCoins != null)
                    Array.Copy(level.greenCoins, newArray, level.greenCoins.Length);
                level.greenCoins = newArray;
            }
        }
        return level;
    }

    public static SaveFileSummary BuildSummary(LevelDefinition[] allLevels)
    {
        var summary = new SaveFileSummary();
        if (allLevels == null || allLevels.Length == 0)
            return summary;

        summary.totalLevels = allLevels.Length;

        int completed = 0;
        int perfect = 0;
        int collectedCoins = 0;
        int maxCoins = 0;

        var progressById = new Dictionary<string, LevelProgressData>();
        foreach (var lp in Current.levels)
        {
            if (!string.IsNullOrEmpty(lp.levelID) && !progressById.ContainsKey(lp.levelID))
                progressById.Add(lp.levelID, lp);
        }

        bool allLevelsCompleted = true;
        bool allCoinsCollected = true;
        bool allPerfect = true;

        foreach (var def in allLevels)
        {
            maxCoins += Mathf.Max(0, def.greenCoinCount);

            progressById.TryGetValue(def.levelID, out var lp);

            bool levelCompleted = lp != null && lp.completed;
            bool levelPerfect   = lp != null && lp.perfect;

            if (levelCompleted) completed++;
            else allLevelsCompleted = false;

            if (levelPerfect) perfect++;
            else allPerfect = false;

            if (def.greenCoinCount > 0)
            {
                if (lp != null && lp.greenCoins != null)
                {
                    int levelCollected = 0;
                    int len = Mathf.Min(def.greenCoinCount, lp.greenCoins.Length);
                    for (int i = 0; i < len; i++)
                    {
                        if (lp.greenCoins[i]) levelCollected++;
                    }

                    collectedCoins += levelCollected;

                    if (levelCollected < def.greenCoinCount)
                        allCoinsCollected = false;
                }
                else
                {
                    allCoinsCollected = false;
                }
            }
        }

        summary.completedLevels = completed;
        summary.perfectLevels   = perfect;
        summary.collectedCoins  = collectedCoins;
        summary.maxCoins        = maxCoins;

        summary.rewardAllLevels  = allLevelsCompleted;
        summary.rewardAllCoins   = allCoinsCollected;
        summary.rewardAllPerfect = allPerfect;

        return summary;
    }

    #region Export / Import  helpers
    /// <summary>
    /// Exports a slot file to a target directory with a nice name.
    /// Returns the full path of the exported file, or null if failed.
    /// </summary>
    public static string ExportSlot(int slot, string targetDirectory, string fileNameWithoutExtension = null)
    {
        string sourcePath = GetSlotFilePath(slot);
        if (!File.Exists(sourcePath))
            return null;

        if (string.IsNullOrEmpty(targetDirectory))
            return null;

        if (!Directory.Exists(targetDirectory))
            Directory.CreateDirectory(targetDirectory);

        if (string.IsNullOrEmpty(fileNameWithoutExtension))
            fileNameWithoutExtension = $"LevelUp_File_{slot}";

        string targetPath = Path.Combine(targetDirectory, fileNameWithoutExtension + FileExtension);

        File.Copy(sourcePath, targetPath, overwrite: true);
        return targetPath;
    }

    /// <summary>
    /// Imports a .lumm file into the specified slot.
    /// Returns true if import succeeded.
    /// </summary>
    public static bool ImportSlot(int slot, string sourceFilePath)
    {
        if (string.IsNullOrEmpty(sourceFilePath) || !File.Exists(sourceFilePath))
            return false;

        if (!sourceFilePath.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"ImportSlot: file does not have {FileExtension} extension.");
            // you can decide to reject or still accept; here we reject
            return false;
        }

        string destPath = GetSlotFilePath(slot);
        string destDir = Path.GetDirectoryName(destPath);
        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        File.Copy(sourceFilePath, destPath, overwrite: true);

        // Reload the imported save into memory
        Load(slot);
        return true;
    }
    #endregion
}