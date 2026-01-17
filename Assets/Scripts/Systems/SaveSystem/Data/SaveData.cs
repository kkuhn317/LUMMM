using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelProgressData
{
    public string levelID;
    public bool completed;
    public bool perfect;
    public bool[] greenCoins;
    public int totalGreenCoins;
    public int highestRank;
    public double bestTimeMs;
    public int highScore = 0; 
}

[Serializable]
public class CheckpointSave
{
    public bool hasCheckpoint;
    public string levelID;
    public int checkpointId;
    public int coins;
    public double speedrunMs;
    public bool[] greenCoinsInRun;
}

[Serializable]
public class ModifiersData
{
    public bool infiniteLivesEnabled = false;
    public bool timeLimitEnabled = true;
    public int checkpointMode = 0;
}

[Serializable]
public class SaveData
{
    public int version = 1;            
    public string profileName = "Player A";
    public List<LevelProgressData> levels = new();
    public ModifiersData modifiers = new();
    public CheckpointSave checkpoint = new();
}

public class SaveFileSummary
{
    public int totalLevels;
    public int completedLevels;
    public int perfectLevels;
    public int collectedGreenCoins;
    public int maxGreenCoins;
    public bool rewardAllLevels;
    public bool rewardAllGreenCoins;
    public bool rewardAllPerfect;

    public int StarCount =>
        (rewardAllLevels ? 1 : 0) +
        (rewardAllGreenCoins ? 1 : 0) +
        (rewardAllPerfect ? 1 : 0);
}