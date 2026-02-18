using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CheckpointManager : MonoBehaviour
{
    private readonly List<Checkpoint> checkpoints = new List<Checkpoint>();

    private string levelId;
    private ProgressStore progressStore;
    private GreenCoinSystem greenCoinSystem;
    private PlayerRegistry playerRegistry;

    // locked state at the exact moment the checkpoint was touched
    private bool hasCheckpointSnapshot;
    private int snapshotCheckpointId;
    private int snapshotCoins;
    private int snapshotScore;
    private double snapshotSpeedrunMs;
    private bool[] snapshotGreenCoinsInRun;

    private void Awake()
    {
        progressStore = new ProgressStore();

        greenCoinSystem = GetComponent<GreenCoinSystem>();
        if (greenCoinSystem == null)
            Debug.LogError($"{nameof(CheckpointManager)} requires {nameof(GreenCoinSystem)} on the same GameObject.");

        playerRegistry = FindObjectOfType<PlayerRegistry>(true);
        if (playerRegistry == null)
            Debug.LogError($"{nameof(CheckpointManager)} requires a {nameof(PlayerRegistry)} in the scene.");

        levelId = ResolveLevelId();
        ApplyModifiersFromSaveData();
    }

    private void Start()
    {
        levelId = ResolveLevelId();
        StartCoroutine(EnsureModifiersThenLoadCheckpoint());
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerRespawnedFromDeath += HandlePlayerRespawnedFromDeath;
        GameEvents.OnCheckpointLoaded += HandleCheckpointLoaded;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerRespawnedFromDeath -= HandlePlayerRespawnedFromDeath;
        GameEvents.OnCheckpointLoaded -= HandleCheckpointLoaded;
    }

    private void HandlePlayerRespawnedFromDeath(MarioMovement player)
    {
        TryPlacePlayerAtActiveCheckpoint(player);
    }

    private void HandleCheckpointLoaded()
    {
        if (playerRegistry == null) return;
        foreach (var p in playerRegistry.GetAllPlayers())
            TryPlacePlayerAtActiveCheckpoint(p);
    }

    private void TryPlacePlayerAtActiveCheckpoint(MarioMovement player)
    {
        if (player == null) return;

        if (!GlobalVariables.enableCheckpoints) return;
        if (GlobalVariables.checkpoint == -1) return;

        Checkpoint active = null;
        foreach (var cp in checkpoints)
        {
            if (cp == null) continue;
            if (cp.checkpointID == GlobalVariables.checkpoint)
            {
                active = cp;
                break;
            }
        }

        if (active == null) return;

        player.transform.position = active.SpawnPosition;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;
    }

    private IEnumerator EnsureModifiersThenLoadCheckpoint()
    {
        int frames = 0;
        while (SaveManager.Current == null && frames < 10)
        {
            frames++;
            yield return null;
        }

        ApplyModifiersFromSaveData();

        LoadCheckpoint();
        UpdateCheckpointVisuals(playFeedbackForActive: false);

        if (playerRegistry != null)
        {
            foreach (var p in playerRegistry.GetAllPlayers())
                TryPlacePlayerAtActiveCheckpoint(p);
        }

        InvokeRespawnActivationForActiveCheckpoint();
    }

    private void ApplyModifiersFromSaveData()
    {
        int mode = 0;

        if (SaveManager.Current != null && SaveManager.Current.modifiers != null)
            mode = Mathf.Clamp(SaveManager.Current.modifiers.checkpointMode, 0, 2);

        GlobalVariables.checkpointMode = mode;
        GlobalVariables.enableCheckpoints = (mode != 0);

        if (SaveManager.Current != null && SaveManager.Current.modifiers != null)
        {
            GlobalVariables.infiniteLivesMode = SaveManager.Current.modifiers.infiniteLivesEnabled;
            GlobalVariables.stopTimeLimit = SaveManager.Current.modifiers.timeLimitEnabled;
        }
    }

    private string ResolveLevelId()
    {
        if (GameManager.Instance != null &&
            !string.IsNullOrEmpty(GameManager.Instance.CurrentLevelId))
            return GameManager.Instance.CurrentLevelId;

        var id = GlobalVariables.levelInfo != null ? GlobalVariables.levelInfo.levelID : null;
        if (!string.IsNullOrEmpty(id)) return id;

        return SceneManager.GetActiveScene().name;
    }

    public void RegisterCheckpoint(Checkpoint checkpoint)
    {
        if (checkpoint == null) return;
        if (checkpoints.Contains(checkpoint)) return;

        checkpoints.Add(checkpoint);

        checkpoint.RefreshEnabledState();
    }

    public void UnregisterCheckpoint(Checkpoint checkpoint)
    {
        if (checkpoint == null) return;
        checkpoints.Remove(checkpoint);
    }

    public void ActivateCheckpoint(Checkpoint checkpoint)
    {
        if (checkpoint == null) return;

        if (!GlobalVariables.enableCheckpoints)
            return;

        GlobalVariables.checkpoint = checkpoint.checkpointID;

        hasCheckpointSnapshot = true;
        snapshotCheckpointId = GlobalVariables.checkpoint;
        snapshotCoins = GlobalVariables.coinCount;
        snapshotScore = GlobalVariables.score;
        snapshotSpeedrunMs = GlobalVariables.speedrunTimer.Elapsed.TotalMilliseconds;
        snapshotGreenCoinsInRun = greenCoinSystem != null
            ? greenCoinSystem.GetGreenCoinsInRunArray()
            : new bool[0];

        UpdateCheckpointVisuals(playFeedbackForActive: true);
        SaveCurrentCheckpoint();

        GameEvents.TriggerCheckpointReached(checkpoint.checkpointID);
        GameEvents.TriggerCheckpointChanged(GlobalVariables.checkpoint);
    }

    private void UpdateCheckpointVisuals(bool playFeedbackForActive = false)
    {
        foreach (var cp in checkpoints)
        {
            if (cp == null) continue;

            cp.RefreshEnabledState();

            if (!cp.IsEnabledByMode)
                continue;

            if (cp.checkpointID == GlobalVariables.checkpoint)
                cp.SetActive(playFeedbackForActive);
            else
                cp.SetPassive();
        }
    }

    public void LoadCheckpoint()
    {
        levelId = ResolveLevelId();

        if (!progressStore.TryGetCheckpoint(levelId, out var checkpoint))
        {
            GlobalVariables.checkpoint = -1;
            GlobalVariables.coinCount = 0;
            GlobalVariables.score = 0;

            hasCheckpointSnapshot = false;
            snapshotGreenCoinsInRun = null;

            return;
        }

        GlobalVariables.checkpoint = checkpoint.checkpointId;
        GlobalVariables.coinCount = checkpoint.coins;
        GlobalVariables.lives = checkpoint.lives;
        GlobalVariables.score = checkpoint.score;

        if (greenCoinSystem != null && checkpoint.greenCoinsInRun != null)
            greenCoinSystem.ApplyGreenCoinsInRun(checkpoint.greenCoinsInRun);
        
        hasCheckpointSnapshot = true;
        snapshotCheckpointId = GlobalVariables.checkpoint;
        snapshotCoins = GlobalVariables.coinCount;
        snapshotScore = GlobalVariables.score;
        snapshotSpeedrunMs = checkpoint.speedrunMs;
        snapshotGreenCoinsInRun = checkpoint.greenCoinsInRun;

        GameEvents.TriggerCheckpointLoaded();
        GameEvents.TriggerCheckpointChanged(GlobalVariables.checkpoint);
    }

    private void InvokeRespawnActivationForActiveCheckpoint()
    {
        if (GlobalVariables.checkpoint == -1) return;

        foreach (var cp in checkpoints)
        {
            if (cp == null) continue;
            if (cp.checkpointID != GlobalVariables.checkpoint) continue;

            cp.InvokeRespawnActivation();
            break;
        }
    }

    public void SaveCurrentCheckpoint()
    {
        if (!HasCheckpoint) return;

        levelId = ResolveLevelId();

        if (!hasCheckpointSnapshot)
        {
            hasCheckpointSnapshot = true;
            snapshotCheckpointId = GlobalVariables.checkpoint;
            snapshotCoins = GlobalVariables.coinCount;
            snapshotScore = GlobalVariables.score;
            snapshotSpeedrunMs = GlobalVariables.speedrunTimer.Elapsed.TotalMilliseconds;
            snapshotGreenCoinsInRun = greenCoinSystem != null
                ? greenCoinSystem.GetGreenCoinsInRunArray()
                : new bool[0];
        }

        progressStore.SaveCheckpoint(
            levelId: levelId,
            checkpointId: snapshotCheckpointId,
            coins: snapshotCoins,
            lives: GlobalVariables.lives,
            score: snapshotScore,
            speedrunMs: snapshotSpeedrunMs,
            greenCoinsInRun: snapshotGreenCoinsInRun
        );

        progressStore.Save();
        GameEvents.TriggerCheckpointSaved();
    }

    public void ClearCheckpoint()
    {
        progressStore.ClearCheckpoint();
        progressStore.Save();

        GlobalVariables.checkpoint = -1;

        hasCheckpointSnapshot = false;
        snapshotGreenCoinsInRun = null;

        GameEvents.TriggerCheckpointCleared();
        GameEvents.TriggerCheckpointChanged(GlobalVariables.checkpoint);

        UpdateCheckpointVisuals();
    }

    public bool HasCheckpoint => GlobalVariables.checkpoint != -1;
    public int CurrentCheckpointId => GlobalVariables.checkpoint;
}