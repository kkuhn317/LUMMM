using UnityEngine;
using UnityEngine.Events;

public class RankSystem : MonoBehaviour
{
    [Header("Rank Settings")]
    [SerializeField] private int scoreForSRank = 10000;
    [SerializeField] private int scoreForARank = 9000;
    [SerializeField] private int scoreForBRank = 7000;
    [SerializeField] private int scoreForCRank = 5000;
    [SerializeField] private int scoreForDRank = 3000;

    [Header("Events")]
    public UnityEvent onSetCurrentRank;

    private PlayerRank currentRank = PlayerRank.Default;
    private PlayerRank highestRank = PlayerRank.Default;
    private PlayerRank attemptBestRank = PlayerRank.Default;

    private string levelId;
    private ProgressStore progressStore;

    private void OnEnable()
    {
        GameEvents.OnLevelComplete += OnLevelComplete;
    }

    private void OnDisable()
    {
        GameEvents.OnLevelComplete -= OnLevelComplete;
    }

    private void Start()
    {
        levelId = GlobalVariables.levelInfo?.levelID ?? "unknown";
        progressStore = new ProgressStore();

        LoadHighestRank();

        // attempt starts at whatever is already saved, but we won't overwrite it unless level completes
        attemptBestRank = highestRank;

        ForceUpdateCurrentRank(invokeEvenIfSame: true);

        GameEvents.TriggerRankChanged(currentRank);
        GameEvents.TriggerHighestRankChanged(highestRank);

        onSetCurrentRank?.Invoke();
    }

    private void Update()
    {
        ForceUpdateCurrentRank(invokeEvenIfSame: false);
    }

    private void ForceUpdateCurrentRank(bool invokeEvenIfSame)
    {
        PlayerRank newRank = CalculateRank(GlobalVariables.score);

        if (!invokeEvenIfSame && newRank == currentRank)
            return;

        currentRank = newRank;
        GameEvents.TriggerRankChanged(currentRank);

        if (currentRank > attemptBestRank)
        {
            attemptBestRank = currentRank;
        }

        onSetCurrentRank?.Invoke();
    }

    private void OnLevelComplete()
    {
        if (attemptBestRank > highestRank)
        {
            highestRank = attemptBestRank;
            SaveHighestRank();
            GameEvents.TriggerHighestRankChanged(highestRank);
        }
    }

    private PlayerRank CalculateRank(int score)
    {
        if (score >= scoreForSRank) return PlayerRank.S;
        if (score >= scoreForARank) return PlayerRank.A;
        if (score >= scoreForBRank) return PlayerRank.B;
        if (score >= scoreForCRank) return PlayerRank.C;
        if (score >= scoreForDRank) return PlayerRank.D;
        return PlayerRank.Default;
    }

    private void LoadHighestRank()
    {
        if (progressStore.TryGetLevel(levelId, out var levelData))
        {
            highestRank = (PlayerRank)levelData.highestRank;
        }
    }

    private void SaveHighestRank()
    {
        var levelData = progressStore.GetOrCreateLevel(levelId);
        if ((int)highestRank > levelData.highestRank)
        {
            levelData.highestRank = (int)highestRank;
            progressStore.Save();
        }
    }

    public PlayerRank GetCurrentRank() => currentRank;
    public PlayerRank GetHighestRank() => highestRank;
}