using UnityEngine;

public class LevelCompletionController : MonoBehaviour
{
    [Header("Completion Settings")]
    [SerializeField] private bool checkPerfectConditions = true;
    
    private GreenCoinSystem greenCoinSystem;
    private RankSystem rankSystem;
    private ProgressStore progressStore;
    private bool levelCompleted = false;
    private string levelId;
    
    private void Start()
    {
        greenCoinSystem = GetComponent<GreenCoinSystem>();
        rankSystem = GetComponent<RankSystem>();
        levelId = GlobalVariables.levelInfo?.levelID ?? "unknown";
        progressStore = new ProgressStore();
    }
    
    public void CompleteLevel()
    {
        if (levelCompleted) return;
        levelCompleted = true;
        
        bool isPerfect = IsPerfectCompletion();
        PlayerRank currentRank = rankSystem?.GetCurrentRank() ?? PlayerRank.Default;
        
        // Save level completion
        var levelData = progressStore.GetOrCreateLevel(levelId);
        
        if (greenCoinSystem != null)
        {
            bool[] greenCoinsInRun = greenCoinSystem.GetGreenCoinsInRunArray();
            for (int i = 0; i < greenCoinsInRun.Length; i++)
            {
                if (greenCoinsInRun[i])
                    progressStore.MarkGreenCoinCollected(levelId, i, greenCoinsInRun.Length);
            }
        }

        levelData.completed = true;
        levelData.perfect = levelData.perfect || isPerfect;
        
        // Save rank
        if ((int)currentRank > levelData.highestRank)
        {
            levelData.highestRank = (int)currentRank;
        }
        
        // Save best time
        if (GlobalVariables.SpeedrunMode)
        {
            double currentTimeMs = GlobalVariables.elapsedTime.TotalMilliseconds;
            if (levelData.bestTimeMs == 0 || currentTimeMs < levelData.bestTimeMs)
            {
                levelData.bestTimeMs = currentTimeMs;
                GameEvents.TriggerNewBestTime(currentTimeMs);
            }
        }
        
        progressStore.Save();

        if (progressStore.TryGetCheckpoint(levelId, out var cp))
        {
            // only clear if it's for THIS level
            progressStore.ClearCheckpoint();
            progressStore.Save();

            GlobalVariables.checkpoint = -1;
            GameEvents.TriggerCheckpointCleared();
            GameEvents.TriggerCheckpointChanged(GlobalVariables.checkpoint);
        }
        
        // Check for new high score
        if (GlobalVariables.score > levelData.highScore)
        {
            levelData.highScore = GlobalVariables.score;
            progressStore.Save();
            GameEvents.TriggerNewHighScore(GlobalVariables.score);
        }
        
        OnLevelCompleted(isPerfect);
    }
    
    private bool IsPerfectCompletion()
    {
        if (!checkPerfectConditions) return false;
        
        bool allCoins = greenCoinSystem != null && greenCoinSystem.AreAllCoinsCollected();
        bool noInfiniteLives = !GlobalVariables.infiniteLivesMode;
        bool checkpointsDisabled = !GlobalVariables.enableCheckpoints;
        bool timeLimitEnabled = !GlobalVariables.infiniteTimeMode;
        
        return allCoins && noInfiniteLives && checkpointsDisabled && timeLimitEnabled;
    }
    
    private void OnLevelCompleted(bool isPerfect)
    {
        Debug.Log($"Level completed! Perfect: {isPerfect}");
        GameEvents.TriggerPerfectCompletion(isPerfect);
        GameEvents.TriggerLevelComplete();
    }
    
    public void ResetCompletionState()
    {
        levelCompleted = false;
    }
}