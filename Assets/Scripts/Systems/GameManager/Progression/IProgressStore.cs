public interface IProgressStore
{
    // Per-level progress
    LevelProgressData GetOrCreateLevel(string levelId);
    bool TryGetLevel(string levelId, out LevelProgressData level);
    void SaveHighScoreIfBetter(string levelId, int score);
    void SaveBestTimeIfBetter(string levelId, double timeMs);
    void MarkGreenCoinCollected(string levelId, int coinIndex, int coinCount);

    // Checkpoint progress (run-state)
    bool TryGetCheckpoint(string levelId, out CheckpointSave checkpoint);
    void SaveCheckpoint(string levelId, int checkpointId, int coins, int lives, int score, double speedrunMs, bool[] greenCoinsInRun);
    void ClearCheckpoint();

    // Flush
    void Save();
}