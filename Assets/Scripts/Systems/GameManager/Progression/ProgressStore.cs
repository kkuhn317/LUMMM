using System;
using UnityEngine;

public sealed class ProgressStore : IProgressStore
{
    private SaveData Current
    {
        get
        {
            if (SaveManager.Current == null)
                Debug.LogWarning("SaveManager.Current is null. Progress will not be persisted.");
            return SaveManager.Current;
        }
    }

    public LevelProgressData GetOrCreateLevel(string levelId)
    {
        var save = Current;
        if (save == null) return new LevelProgressData { levelID = levelId };

        if (save.levels == null)
            save.levels = new System.Collections.Generic.List<LevelProgressData>();

        var level = save.levels.Find(l => l.levelID == levelId);
        if (level == null)
        {
            level = new LevelProgressData { levelID = levelId };
            save.levels.Add(level);
        }
        return level;
    }

    public bool TryGetLevel(string levelId, out LevelProgressData level)
    {
        level = null;
        var save = Current;
        if (save?.levels == null) return false;

        level = save.levels.Find(l => l.levelID == levelId);
        return level != null;
    }

    public void SaveHighScoreIfBetter(string levelId, int score)
    {
        var level = GetOrCreateLevel(levelId);
        if (score > level.highScore)
            level.highScore = score;
    }

    public void SaveBestTimeIfBetter(string levelId, double timeMs)
    {
        var level = GetOrCreateLevel(levelId);

        // Your code uses: "if (newMs < bestTimeMs || bestTimeMs == 0)"
        if (level.bestTimeMs == 0 || timeMs < level.bestTimeMs)
            level.bestTimeMs = timeMs;
    }

    public void MarkGreenCoinCollected(string levelId, int coinIndex, int coinCount)
    {
        var level = GetOrCreateLevel(levelId);

        // Ensure array exists and is correct size
        if (level.greenCoins == null || level.greenCoins.Length != coinCount)
            level.greenCoins = new bool[coinCount];

        if (coinIndex >= 0 && coinIndex < level.greenCoins.Length)
            level.greenCoins[coinIndex] = true;
    }

    public bool TryGetCheckpoint(string levelId, out CheckpointSave checkpoint)
    {
        checkpoint = null;
        var save = Current;
        if (save?.checkpoint == null) return false;

        if (!save.checkpoint.hasCheckpoint) return false;
        if (save.checkpoint.levelID != levelId) return false;

        checkpoint = save.checkpoint;
        return true;
    }

    public void SaveCheckpoint(
        string levelId,
        int checkpointId,
        int coins,
        int lives,
        int score,
        double speedrunMs,
        bool[] greenCoinsInRun)
    {
        var save = Current;
        if (save == null) return;

        save.checkpoint = new CheckpointSave
        {
            hasCheckpoint = true,
            levelID = levelId,
            checkpointId = checkpointId,
            coins = coins,
            lives = lives,
            score = score,
            speedrunMs = speedrunMs,
            greenCoinsInRun = greenCoinsInRun
        };
    }

    public void ClearCheckpoint()
    {
        var save = Current;
        if (save?.checkpoint == null) return;

        save.checkpoint.hasCheckpoint = false;
        save.checkpoint.levelID = "";
    }

    public void Save()
    {
        if (Current == null) return;
        SaveManager.Save();
    }
}