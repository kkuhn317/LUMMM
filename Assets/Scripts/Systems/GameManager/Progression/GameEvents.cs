using System;
using UnityEngine;

public static class GameEvents
{
    public struct LevelContext
    {
        public readonly string LevelId;
        public readonly float StartingTime;

        public LevelContext(string levelId, float startingTime)
        {
            LevelId = levelId;
            StartingTime = startingTime;
        }
    }

    public static event Action<LevelContext> OnLevelContextChanged;

    public static event Action OnGameInitialized;
    public static event Action OnLevelStarted;
    public static event Action OnLevelComplete;
    public static event Action OnLevelFailed;
    public static event Action OnGameOver;

    public static event Action<MarioMovement, int> OnPlayerRegistered;
    public static event Action<MarioMovement> OnPlayerDeath;
    public static event Action<MarioMovement> OnPlayerRespawned;
    public static event Action<PowerStates.PowerupState> OnPlayerPowerupChanged;

    public static event Action<int> OnLivesChanged;

    public static event Action<int> OnScoreChanged;
    public static event Action<int> OnScoreAdded;
    public static event Action<int> OnHighScoreChanged;
    public static event Action<PlayerRank> OnRankChanged;
    public static event Action<PlayerRank> OnHighestRankChanged;

    public static event Action<int> OnCoinsChanged;
    public static event Action<int> OnCoinsAdded;
    public static event Action OnExtraLifeGained;

    public static event Action<GameObject> OnGreenCoinCollected;
    public static event Action<int, int, int> OnGreenCoinProgress;
    public static event Action<bool> OnAllGreenCoinsCollected;

    public static event Action<int> OnTotalCoinsCalculated;
    public static event Action<bool> OnAllCoinsCollected;

    public static event Action<float> OnTimerChanged;
    public static event Action OnTimeWarning;
    public static event Action OnTimeUp;
    public static event Action<TimeSpan> OnSpeedrunTimeChanged;
    public static event Action<int> OnTimeBonusStarted;
    public static event Action<int> OnTimeBonusTick;
    public static event Action OnTimeBonusComplete;

    public static event Action<int> OnCheckpointReached;
    public static event Action OnCheckpointSaved;
    public static event Action OnCheckpointLoaded;
    public static event Action<int> OnCheckpointChanged;

    public static event Action OnGamePaused;
    public static event Action OnGameResumed;
    public static event Action<string> OnMenuOpened;
    public static event Action OnMenuClosed;

    public static event Action OnUIUpdated;
    public static event Action OnWinScreenShown;
    public static event Action OnGameOverScreenShown;

    public static event Action OnProgressSaved;
    public static event Action OnProgressLoaded;
    public static event Action OnCheckpointCleared;

    public static event Action<string, bool> OnCheatToggled;
    public static event Action<StartPowerupMode> OnStartPowerupChanged;

    public static event Action<bool> OnPerfectCompletion;
    public static event Action<double> OnNewBestTime;
    public static event Action<int> OnNewHighScore;
    public static event Action<MarioMovement> OnPlayerRespawnedFromDeath;
    public static event Action<MarioMovement> OnPlayerStateReset; 

    // -----------------------------
    // Trigger Methods
    // -----------------------------
    public static void TriggerLevelContextChanged(LevelContext ctx) => OnLevelContextChanged?.Invoke(ctx);

    public static void TriggerGameInitialized() => OnGameInitialized?.Invoke();
    public static void TriggerLevelStarted() => OnLevelStarted?.Invoke();
    public static void TriggerLevelComplete() => OnLevelComplete?.Invoke();
    public static void TriggerLevelFailed() => OnLevelFailed?.Invoke();
    public static void TriggerGameOver() => OnGameOver?.Invoke();

    public static void TriggerPlayerRegistered(MarioMovement player, int playerIndex) =>
        OnPlayerRegistered?.Invoke(player, playerIndex);
    public static void TriggerPlayerDeath(MarioMovement player) => OnPlayerDeath?.Invoke(player);
    public static void TriggerPlayerRespawned(MarioMovement player) => OnPlayerRespawned?.Invoke(player);
    public static void TriggerPlayerRespawnedFromDeath(MarioMovement player) =>
        OnPlayerRespawnedFromDeath?.Invoke(player);
    public static void TriggerPlayerStateReset(MarioMovement player) =>
        OnPlayerStateReset?.Invoke(player);
    public static void TriggerPlayerPowerupChanged(PowerStates.PowerupState state) =>
        OnPlayerPowerupChanged?.Invoke(state);

    public static void TriggerLivesChanged(int lives) => OnLivesChanged?.Invoke(lives);

    public static void TriggerScoreChanged(int newScore) => OnScoreChanged?.Invoke(newScore);
    public static void TriggerScoreAdded(int amount) => OnScoreAdded?.Invoke(amount);
    public static void TriggerHighScoreChanged(int newHighScore) => OnHighScoreChanged?.Invoke(newHighScore);
    public static void TriggerRankChanged(PlayerRank rank) => OnRankChanged?.Invoke(rank);
    public static void TriggerHighestRankChanged(PlayerRank rank) => OnHighestRankChanged?.Invoke(rank);

    public static void TriggerCoinsChanged(int newCount) => OnCoinsChanged?.Invoke(newCount);
    public static void TriggerCoinsAdded(int amount) => OnCoinsAdded?.Invoke(amount);
    public static void TriggerExtraLifeGained() => OnExtraLifeGained?.Invoke();

    public static void TriggerGreenCoinCollected(GameObject coin) => OnGreenCoinCollected?.Invoke(coin);
    public static void TriggerGreenCoinProgress(int collected, int total, int index) =>
        OnGreenCoinProgress?.Invoke(collected, total, index);
    public static void TriggerAllGreenCoinsCollected(bool allCollected) =>
        OnAllGreenCoinsCollected?.Invoke(allCollected);

    public static void TriggerTotalCoinsCalculated(int totalCoins) => OnTotalCoinsCalculated?.Invoke(totalCoins);
    public static void TriggerAllCoinsCollected(bool allCollected) => OnAllCoinsCollected?.Invoke(allCollected);

    public static void TriggerTimerChanged(float currentTime) => OnTimerChanged?.Invoke(currentTime);
    public static void TriggerTimeWarning() => OnTimeWarning?.Invoke();
    public static void TriggerTimeUp() => OnTimeUp?.Invoke();
    public static void TriggerSpeedrunTimeChanged(TimeSpan time) => OnSpeedrunTimeChanged?.Invoke(time);
    public static void TriggerTimeBonusStarted(int timeLeft) => OnTimeBonusStarted?.Invoke(timeLeft);
    public static void TriggerTimeBonusTick(int amount) => OnTimeBonusTick?.Invoke(amount);
    public static void TriggerTimeBonusComplete() => OnTimeBonusComplete?.Invoke();

    public static void TriggerCheckpointReached(int checkpointId) => OnCheckpointReached?.Invoke(checkpointId);
    public static void TriggerCheckpointSaved() => OnCheckpointSaved?.Invoke();
    public static void TriggerCheckpointLoaded() => OnCheckpointLoaded?.Invoke();

    public static void TriggerCheckpointChanged(int checkpointId) => OnCheckpointChanged?.Invoke(checkpointId);

    public static void TriggerGamePaused() => OnGamePaused?.Invoke();
    public static void TriggerGameResumed() => OnGameResumed?.Invoke();
    public static void TriggerMenuOpened(string menuName) => OnMenuOpened?.Invoke(menuName);
    public static void TriggerMenuClosed() => OnMenuClosed?.Invoke();

    public static void TriggerUIUpdated() => OnUIUpdated?.Invoke();
    public static void TriggerWinScreenShown() => OnWinScreenShown?.Invoke();
    public static void TriggerGameOverScreenShown() => OnGameOverScreenShown?.Invoke();

    public static void TriggerProgressSaved() => OnProgressSaved?.Invoke();
    public static void TriggerProgressLoaded() => OnProgressLoaded?.Invoke();
    public static void TriggerCheckpointCleared() => OnCheckpointCleared?.Invoke();

    public static void TriggerCheatToggled(string cheatName, bool enabled) => OnCheatToggled?.Invoke(cheatName, enabled);
    public static void TriggerStartPowerupChanged(StartPowerupMode mode) => OnStartPowerupChanged?.Invoke(mode);

    public static void TriggerPerfectCompletion(bool perfect) => OnPerfectCompletion?.Invoke(perfect);
    public static void TriggerNewBestTime(double timeMs) => OnNewBestTime?.Invoke(timeMs);
    public static void TriggerNewHighScore(int score) => OnNewHighScore?.Invoke(score);
}