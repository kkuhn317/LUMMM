using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveLoadSystem : PersistentSingleton<SaveLoadSystem>
{
    
    [Header("Game Configuration")]
    [SerializeField] private LevelInfo[] allLevelInfos;

    public LevelInfo[] AllLevelInfos => allLevelInfos;
    public static new SaveLoadSystem Instance => _instance;
    private static SaveLoadSystem _instance;
    private IDataService dataService;

    protected override void Awake()
    {
        base.Awake();
        _instance = this;
        dataService = new FileDataService(new JsonSerializer());

        if (allLevelInfos != null)
        {
            var duplicates = allLevelInfos.GroupBy(x => x.levelID)
                                          .Where(g => g.Count() > 1)
                                          .Select(g => g.Key);
            if (duplicates.Any())
            {
                Debug.LogError($"Duplicate level IDs found: {string.Join(", ", duplicates)}");
            }
        }

        LegacyPlayerPrefsMigrator.TryMigrate(this);
    }

    #region Level Info
    public LevelInfo GetLevelInfo(string levelID)
    {
        if (allLevelInfos == null) return null;
        return allLevelInfos.FirstOrDefault(info => info.levelID == levelID);
    }

    public LevelInfo[] GetPlayableLevels()
    {
        if (allLevelInfos == null) return new LevelInfo[0];
        return allLevelInfos.Where(info => !info.beta || GlobalVariables.cheatBetaMode).ToArray();
    }

    public int GetGreenCoinCountForLevel(string levelID)
    {
        var levelInfo = GetLevelInfo(levelID);
        return levelInfo?.totalGreenCoins ?? 3;
    }

    public LevelProgressData GetLevelProgress(string levelID)
    {
        return SaveManager.Current.levels.Find(l => l.levelID == levelID);
    }

    public bool IsLevelCompleted(string levelID)
    {
        var progress = GetLevelProgress(levelID);
        if (progress != null)
        {
            return progress.completed;
        }

        return PlayerPrefs.GetInt("LevelCompleted_" + levelID, 0) == 1;
    }

    public bool IsLevelPerfect(string levelID)
    {
        var progress = GetLevelProgress(levelID);
        if (progress != null)
        {
            return progress.perfect;
        }
        
        return PlayerPrefs.GetInt("LevelPerfect_" + levelID, 0) == 1;
    }

    public bool[] GetGreenCoins(string levelID)
    {
        var progress = GetLevelProgress(levelID);
        return progress?.greenCoins;
    }

    public int GetHighestRank(string levelID)
    {
        var progress = GetLevelProgress(levelID);
        return progress?.highestRank ?? -1;
    }

    public bool HasCheckpoint(string levelID)
    {
        return SaveManager.Current.checkpoint.levelID == levelID &&
            SaveManager.Current.checkpoint.hasCheckpoint;
    }
    #endregion

    public SaveFileSummary BuildSummary()
    {
        var playableLevels = GetPlayableLevels();
        return SaveManager.BuildSummary(playableLevels);
    }

    public void NewGame(int slot = 0)
    {
        SaveManager.Load(slot);
        
        if (FadeInOutScene.Instance != null)
            FadeInOutScene.Instance.LoadSceneWithFade("SelectLevel");
        else
            SceneManager.LoadScene("SelectLevel");
    }

    public void SaveGame()
    {
        SaveManager.Save();
        Debug.Log("Game Saved");
    }

    public void LoadGame(int slot)
    {
        SaveManager.Load(slot);
        Debug.Log("Game Loaded");
    }

    public void DeleteSave(int slot)
    {
        SaveManager.Delete(slot);
        Debug.Log("Save Deleted");
    }

    public void ReloadGame() => SaveManager.Load(SaveManager.CurrentSlot);
    
    public SaveData GetCurrentSaveData() => SaveManager.Current;
    
    public int GetCurrentSlot() => SaveManager.CurrentSlot;
}