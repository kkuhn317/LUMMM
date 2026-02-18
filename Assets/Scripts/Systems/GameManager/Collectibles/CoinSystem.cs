using UnityEngine;

public class CoinSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool saveCoinsAfterDeath = true;
    [SerializeField] private bool enableTotalCoinTracking = false;

    private LifeSystem lifeSystem;
    private ScoreSystem scoreSystem;
    private string levelId;

    private int totalCoinsInLevel;
    private bool hasCalculatedTotalCoins;

    // Real counter (never wraps at 100)
    private int totalCoinsCollectedThisLevel;

    private void Awake()
    {
        lifeSystem = GetComponent<LifeSystem>();
        scoreSystem = GetComponent<ScoreSystem>();
        levelId = GlobalVariables.levelInfo?.levelID ?? "unknown";

        if (lifeSystem == null) Debug.LogError($"{nameof(CoinSystem)} requires {nameof(LifeSystem)}.");
        if (scoreSystem == null) Debug.LogError($"{nameof(CoinSystem)} requires {nameof(ScoreSystem)}.");
    }

    private void Start()
    {
        if (!saveCoinsAfterDeath)
        {
            SetCoins(0);
        }
        else
        {
            LoadCoinsFromCheckpoint();
        }

        // real counter starts from UI counter
        totalCoinsCollectedThisLevel = GlobalVariables.coinCount;

        if (enableTotalCoinTracking)
            CalculateTotalCoinsInLevel();
    }

    public void AddCoin(int value = 1)
    {
        if (value <= 0) return;

        totalCoinsCollectedThisLevel += value;
        GlobalVariables.coinCount += value;

        if (GlobalVariables.coinCount >= 100)
        {
            GlobalVariables.coinCount -= 100;
            lifeSystem.AddLife();
            GameEvents.TriggerExtraLifeGained();
        }

        scoreSystem.AddCoinScore(value);

        GameEvents.TriggerCoinsAdded(value);
        GameEvents.TriggerCoinsChanged(GlobalVariables.coinCount);

        if (enableTotalCoinTracking && AreAllCoinsCollected())
            GameEvents.TriggerAllCoinsCollected(true);
    }

    public void RemoveCoins(int amount)
    {
        if (amount <= 0) return;

        GlobalVariables.coinCount = Mathf.Max(0, GlobalVariables.coinCount - amount);
        totalCoinsCollectedThisLevel = Mathf.Max(0, totalCoinsCollectedThisLevel - amount);

        GameEvents.TriggerCoinsChanged(GlobalVariables.coinCount);
    }

    public void SetCoins(int amount)
    {
        GlobalVariables.coinCount = Mathf.Max(0, amount);
        totalCoinsCollectedThisLevel = GlobalVariables.coinCount;

        GameEvents.TriggerCoinsChanged(GlobalVariables.coinCount);
    }

    public void CalculateTotalCoinsInLevel()
    {
        if (hasCalculatedTotalCoins) return;

        totalCoinsInLevel = 0;

        foreach (var block in FindObjectsOfType<QuestionBlock>())
        {
            if (block.spawnableItems.Length == 0 && !block.brickBlock)
                totalCoinsInLevel++;
        }

        foreach (var coin in FindObjectsOfType<Coin>())
        {
            if (coin.type != Coin.Amount.green)
                totalCoinsInLevel += coin.GetCoinValue();
        }

        hasCalculatedTotalCoins = true;
        GameEvents.TriggerTotalCoinsCalculated(totalCoinsInLevel);
    }

    public bool AreAllCoinsCollected()
    {
        if (!enableTotalCoinTracking) return false;
        return totalCoinsCollectedThisLevel >= totalCoinsInLevel;
    }

    private void LoadCoinsFromCheckpoint()
    {
        var store = new ProgressStore();
        if (store.TryGetCheckpoint(levelId, out var checkpoint))
        {
            SetCoins(checkpoint.coins);
        }
    }

    public int CurrentCoinCount => GlobalVariables.coinCount;
    public int TotalCoinsInLevel => totalCoinsInLevel;
    public bool IsTotalCoinTrackingEnabled => enableTotalCoinTracking;
}