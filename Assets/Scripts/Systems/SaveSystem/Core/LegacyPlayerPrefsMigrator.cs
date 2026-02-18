using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public static class LegacyPlayerPrefsMigrator
{
    private const string MigrationFlagKey = "LegacyProgressMigrated_v1";

    // Checkpoint-related keys (old system)
    private const string SavedLivesKey = "SavedLives";
    private const string SavedCoinsKey = "SavedCoins";
    private const string SavedCheckpointKey = "SavedCheckpoint";
    private const string SavedLevelKey = "SavedLevel";
    private const string SavedSpeedrunTimeKey = "SavedSpeedrunTime";
    private const string SavedGreenCoinKey = "SavedGreenCoin"; // + index

    public static void TryMigrate(SaveLoadSystem saveLoadSystem)
    {
        // Already migrated? Don't run again
        if (PlayerPrefs.GetInt(MigrationFlagKey, 0) == 1)
        {
            Debug.Log("[LegacyMigration] Already migrated. Skipping.");
            return;
        }

        Debug.Log("[LegacyMigration] Checking PlayerPrefs for legacy data…");

        // Debug dump of the most important keys
        Debug.Log("  SavedLevel       = " + PlayerPrefs.GetString(SavedLevelKey, "<none>"));
        Debug.Log("  SavedCheckpoint  = " + PlayerPrefs.GetInt(SavedCheckpointKey, -999));
        Debug.Log("  SavedLives       = " + PlayerPrefs.GetInt(SavedLivesKey, -999));
        Debug.Log("  SavedCoins       = " + PlayerPrefs.GetInt(SavedCoinsKey, -999));
        Debug.Log("  InfiniteLivesKey = " + PlayerPrefs.GetInt(SettingsKeys.InfiniteLivesKey, -1));
        Debug.Log("  CheckpointModeKey= " + PlayerPrefs.GetInt(SettingsKeys.CheckpointModeKey, -1));
        Debug.Log("  CheckpointsKey   = " + PlayerPrefs.GetInt(SettingsKeys.CheckpointsKey, -1));
        Debug.Log("  TimeLimitKey     = " + PlayerPrefs.GetInt(SettingsKeys.TimeLimitKey, -1));

        // Decide if there's anything at all worth migrating
        bool hasAnyLegacyLevelData = HasAnyLegacyLevelData(saveLoadSystem);
        bool hasAnyLegacyCheckpoint = HasAnyLegacyCheckpointData();
        bool hasAnyLegacyModifiers = HasAnyLegacyModifiersData();

        if (!hasAnyLegacyLevelData && !hasAnyLegacyCheckpoint && !hasAnyLegacyModifiers)
        {
            Debug.Log("[LegacyMigration] No legacy PlayerPrefs detected. Marking as migrated.");
            PlayerPrefs.SetInt(MigrationFlagKey, 1);
            PlayerPrefs.Save();
            return;
        }

        Debug.Log("[LegacyMigration] Legacy data detected. Migrating…");

        int targetSlot = FindMigrationTargetSlot();
        Debug.Log($"[LegacyMigration] Target slot = {targetSlot}");

        // Load/create that slot
        SaveManager.Load(targetSlot);

        var data = SaveManager.Current;
        if (data == null)
        {
            Debug.LogError("[LegacyMigration] SaveManager.Current is null after Load(targetSlot). Aborting migration.");
            return;
        }

        if (data.levels == null)
            data.levels = new List<LevelProgressData>();

        // First, Checkpoint
        MigrateCheckpointFromPlayerPrefs(saveLoadSystem, data);

        // Then, Levels (completed/perfect/time/rank/highscore/green coins)
        if (hasAnyLegacyLevelData)
            MigrateLevelProgressFromPlayerPrefs(saveLoadSystem, data);

        // And finally, Modifiers
        MigrateModifiersFromPlayerPrefs(data);

        SaveManager.Save();

        // Mark as migrated
        PlayerPrefs.SetInt(MigrationFlagKey, 1);
        PlayerPrefs.Save();

        Debug.Log($"[LegacyMigration] Migration completed into slot {targetSlot}.");
        Debug.Log($"[LegacyMigration] Final checkpoint: has={data.checkpoint?.hasCheckpoint}, level={data.checkpoint?.levelID}");
        Debug.Log($"[LegacyMigration] Final modifiers: inf={data.modifiers?.infiniteLivesEnabled}, mode={data.modifiers?.checkpointMode}, timeLimit={data.modifiers?.infiniteTimeEnabled}");
    }

    // Detection helpers
    private static bool HasAnyLegacyCheckpointData()
    {
        if (PlayerPrefs.HasKey(SavedLevelKey))        return true;
        if (PlayerPrefs.HasKey(SavedLivesKey))        return true;
        if (PlayerPrefs.HasKey(SavedCoinsKey))        return true;
        if (PlayerPrefs.HasKey(SavedCheckpointKey))   return true;
        if (PlayerPrefs.HasKey(SavedSpeedrunTimeKey)) return true;
        // SavedGreenCoin<i> we treat as part of the same set
        return false;
    }

    private static bool HasAnyLegacyLevelData(SaveLoadSystem saveLoadSystem)
    {
        if (saveLoadSystem.AllLevelInfos == null)
            return false;

        foreach (var info in saveLoadSystem.AllLevelInfos)
        {
            string id = info.levelID;
            if (PlayerPrefs.HasKey("LevelCompleted_"    + id)) return true;
            if (PlayerPrefs.HasKey("LevelPerfect_"      + id)) return true;
            if (PlayerPrefs.HasKey("HighestPlayerRank_" + id)) return true;
            if (PlayerPrefs.HasKey("HighScore_"         + id)) return true;

            string bestKey = $"BestTimeMs_{id}";
            if (PlayerPrefs.HasKey(bestKey)) return true;
        }

        return false;
    }

    private static bool HasAnyLegacyModifiersData()
    {
        if (PlayerPrefs.HasKey(SettingsKeys.InfiniteLivesKey))  return true;
        if (PlayerPrefs.HasKey(SettingsKeys.CheckpointModeKey)) return true;
        if (PlayerPrefs.HasKey(SettingsKeys.CheckpointsKey))    return true;
        if (PlayerPrefs.HasKey(SettingsKeys.TimeLimitKey))      return true;
        return false;
    }

    // Slot selection
    private static int FindMigrationTargetSlot()
    {
        var dataService = new FileDataService(new JsonSerializer());
        var savedNames = dataService.ListSavedGames() ?? Enumerable.Empty<string>();

        HashSet<int> usedSlots = new HashSet<int>();

        foreach (var name in savedNames)
        {
            if (!name.StartsWith("save_slot_", StringComparison.OrdinalIgnoreCase))
                continue;

            string numberPart = name.Substring("save_slot_".Length);
            if (int.TryParse(numberPart, out int slotIndex))
            {
                usedSlots.Add(slotIndex);
            }
        }

        int candidate = 0;
        while (usedSlots.Contains(candidate))
            candidate++;

        return candidate;
    }

    // Checkpoint migration
    private static void MigrateCheckpointFromPlayerPrefs(SaveLoadSystem saveLoadSystem, SaveData data)
    {
        string savedLevel = PlayerPrefs.GetString(SavedLevelKey, "none");
        if (string.IsNullOrEmpty(savedLevel) || savedLevel.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("[LegacyMigration] No SavedLevel; skipping checkpoint migration.");
            return;
        }

        var checkpoint = new CheckpointSave
        {
            hasCheckpoint = true,
            levelID       = savedLevel,
            lives         = PlayerPrefs.GetInt(SavedLivesKey, 3),
            coins         = PlayerPrefs.GetInt(SavedCoinsKey, 0),
            checkpointId  = PlayerPrefs.GetInt(SavedCheckpointKey, -1),
            speedrunMs    = ParseLegacySpeedrunTime(PlayerPrefs.GetString(SavedSpeedrunTimeKey, string.Empty))
        };

        int greenCoinCount = saveLoadSystem.GetGreenCoinCountForLevel(savedLevel);
        if (greenCoinCount > 0)
        {
            checkpoint.greenCoinsInRun = new bool[greenCoinCount];
            for (int i = 0; i < greenCoinCount; i++)
            {
                int val = PlayerPrefs.GetInt(SavedGreenCoinKey + i, 0);
                checkpoint.greenCoinsInRun[i] = (val == 1);
            }
        }
        else
        {
            checkpoint.greenCoinsInRun = null;
        }

        data.checkpoint = checkpoint;

        Debug.Log(
            $"[LegacyMigration] Migrated checkpoint → " +
            $"level={checkpoint.levelID}, lives={checkpoint.lives}, coins={checkpoint.coins}, cpId={checkpoint.checkpointId}"
        );
    }

    private static double ParseLegacySpeedrunTime(string savedTime)
    {
        if (string.IsNullOrWhiteSpace(savedTime))
            return 0;

        if (double.TryParse(savedTime, NumberStyles.Float, CultureInfo.InvariantCulture, out double ms))
            return ms;

        if (TimeSpan.TryParse(savedTime, out var ts))
            return ts.TotalMilliseconds;

        return 1; // "has something"
    }

    // Level progress migration
    private static void MigrateLevelProgressFromPlayerPrefs(SaveLoadSystem saveLoadSystem, SaveData data)
    {
        if (saveLoadSystem.AllLevelInfos == null || saveLoadSystem.AllLevelInfos.Length == 0)
        {
            Debug.Log("[LegacyMigration] No AllLevelInfos; skipping level progress migration.");
            return;
        }

        foreach (var levelInfo in saveLoadSystem.AllLevelInfos)
        {
            string levelID = levelInfo.levelID;

            var lp = data.levels.Find(l => l.levelID == levelID);
            if (lp == null)
            {
                lp = new LevelProgressData
                {
                    levelID         = levelID,
                    completed       = false,
                    perfect         = false,
                    greenCoins      = new bool[Mathf.Max(0, levelInfo.totalGreenCoins)],
                    totalGreenCoins = 0,
                    highestRank     = -1,
                    bestTimeMs      = 0,
                    highScore       = 0
                };
                data.levels.Add(lp);
            }

            // Completed / Perfect
            bool legacyCompleted = PlayerPrefs.GetInt("LevelCompleted_" + levelID, 0) == 1;
            bool legacyPerfect   = PlayerPrefs.GetInt("LevelPerfect_"   + levelID, 0) == 1;

            lp.completed |= legacyCompleted;
            lp.perfect   |= legacyPerfect;

            // Highest Rank
            if (PlayerPrefs.HasKey("HighestPlayerRank_" + levelID))
            {
                int legacyRank = PlayerPrefs.GetInt("HighestPlayerRank_" + levelID, (int)PlayerRank.Default);
                if (legacyRank > lp.highestRank)
                    lp.highestRank = legacyRank;
            }

            // Best Time
            string bestKey = $"BestTimeMs_{levelID}";
            string msStr   = PlayerPrefs.GetString(bestKey, "");
            if (double.TryParse(msStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double legacyMs) &&
                legacyMs > 0)
            {
                if (lp.bestTimeMs <= 0 || legacyMs < lp.bestTimeMs)
                    lp.bestTimeMs = legacyMs;
            }

            // HighScore
            int legacyHighScore = PlayerPrefs.GetInt("HighScore_" + levelID, 0);
            if (legacyHighScore > lp.highScore)
                lp.highScore = legacyHighScore;

            // Permanent green coins: CollectedCoin<i>_<levelID>
            if (lp.greenCoins != null && lp.greenCoins.Length > 0)
            {
                for (int i = 0; i < lp.greenCoins.Length; i++)
                {
                    string key = "CollectedCoin" + i + "_" + levelID;
                    int collected = PlayerPrefs.GetInt(key, 0);
                    if (collected == 1)
                    {
                        lp.greenCoins[i] = true;
                    }
                }
            }

            // Recalc totalGreenCoins
            lp.totalGreenCoins = 0;
            if (lp.greenCoins != null)
            {
                foreach (bool c in lp.greenCoins)
                    if (c) lp.totalGreenCoins++;
            }

            Debug.Log(
                $"[LegacyMigration] Level '{levelID}': completed={lp.completed}, perfect={lp.perfect}, " +
                $"rank={lp.highestRank}, bestMs={lp.bestTimeMs}, highScore={lp.highScore}, " +
                $"greenCoins={lp.totalGreenCoins}/{lp.greenCoins?.Length ?? 0}"
            );
        }
    }

    // Modifiers migration
    private static void MigrateModifiersFromPlayerPrefs(SaveData data)
    {
        if (data.modifiers == null)
            data.modifiers = new ModifiersData();

        var m = data.modifiers;

        // Infinite Lives
        m.infiniteLivesEnabled = PlayerPrefs.GetInt(SettingsKeys.InfiniteLivesKey, 0) == 1;

        // Checkpoint mode: prefer new key, fallback to old bool
        int checkpointMode = PlayerPrefs.GetInt(SettingsKeys.CheckpointModeKey, -999);
        if (checkpointMode == -999)
        {
            bool enabled = PlayerPrefs.GetInt(SettingsKeys.CheckpointsKey, 0) == 1;
            checkpointMode = enabled ? 1 : 0;
        }
        m.checkpointMode = Mathf.Clamp(checkpointMode, 0, 2);

        // Time Limit Enabled
        m.infiniteTimeEnabled = PlayerPrefs.GetInt(SettingsKeys.TimeLimitKey, 0) == 1;

        Debug.Log(
            $"[LegacyMigration] Modifiers migrated → " +
            $"infiniteLives={m.infiniteLivesEnabled}, checkpointMode={m.checkpointMode}, timeLimit={m.infiniteTimeEnabled}"
        );
    }
}