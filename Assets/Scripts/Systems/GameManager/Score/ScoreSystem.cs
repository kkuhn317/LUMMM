using UnityEngine;

public class ScoreSystem : MonoBehaviour
{
    private ProgressStore progressStore;
    private int currentHighScore = 0;
    private string levelId;

    private void Start()
    {
        levelId = GlobalVariables.levelInfo?.levelID ?? "unknown";
        progressStore = new ProgressStore();
        
        LoadHighScore();
        
        GameEvents.TriggerScoreChanged(GlobalVariables.score);
        GameEvents.TriggerHighScoreChanged(currentHighScore);
    }

    public void AddScore(int points)
    {
        if (points <= 0) return;
        
        GlobalVariables.score += points;
        
        GameEvents.TriggerScoreChanged(GlobalVariables.score);
        GameEvents.TriggerScoreAdded(points);
        
        if (GlobalVariables.score > currentHighScore)
        {
            currentHighScore = GlobalVariables.score;
            progressStore.SaveHighScoreIfBetter(levelId, currentHighScore);
            progressStore.Save();
            GameEvents.TriggerHighScoreChanged(currentHighScore);
            GameEvents.TriggerNewHighScore(currentHighScore);
        }
    }

    public void AddCoinScore(int coins)
    {
        AddScore(coins * 100);
    }

    private void LoadHighScore()
    {
        if (progressStore.TryGetLevel(levelId, out var levelData))
        {
            currentHighScore = levelData.highScore;
        }
    }

    public int CurrentScore => GlobalVariables.score;
    public int CurrentHighScore => currentHighScore;
}