using System.Collections.Generic;
using UnityEngine;

public class GreenCoinSystem : MonoBehaviour
{
    [Header("Green Coin References")]
    [SerializeField] private GameObject[] greenCoins;

    private readonly List<GameObject> collectedGreenCoins = new List<GameObject>(); // permanents
    private readonly List<GameObject> collectedGreenCoinsInRun = new List<GameObject>();  // snapshot/run (checkpoint)

    private string levelId;
    private ProgressStore progressStore;
    private ScoreSystem scoreSystem;

    private void Start()
    {
        levelId = GlobalVariables.levelInfo?.levelID ?? "unknown";
        progressStore = new ProgressStore();
        scoreSystem = GetComponent<ScoreSystem>();

        LoadCollectedGreenCoins();
    }

    public void CollectGreenCoin(GameObject greenCoin)
    {
        if (greenCoin == null) return;

        int coinIndex = System.Array.IndexOf(greenCoins, greenCoin);
        if (coinIndex < 0 || coinIndex >= greenCoins.Length) return;

        if (!collectedGreenCoins.Contains(greenCoin))
            collectedGreenCoins.Add(greenCoin);

        if (!collectedGreenCoinsInRun.Contains(greenCoin))
            collectedGreenCoinsInRun.Add(greenCoin);

        scoreSystem?.AddScore(2000);

        greenCoin.SetActive(false);

        GameEvents.TriggerGreenCoinCollected(greenCoin);
        GameEvents.TriggerGreenCoinProgress(collectedGreenCoins.Count, greenCoins.Length, coinIndex);

        bool allCollected = AreAllCoinsCollected();
        GameEvents.TriggerAllGreenCoinsCollected(allCollected);
    }

    private void LoadCollectedGreenCoins()
    {
        if (greenCoins == null || greenCoins.Length == 0) return;

        if (!progressStore.TryGetLevel(levelId, out var levelData)) return;
        if (levelData.greenCoins == null) return;

        for (int i = 0; i < Mathf.Min(greenCoins.Length, levelData.greenCoins.Length); i++)
        {
            var coin = greenCoins[i];
            if (coin == null) continue;
            if (!levelData.greenCoins[i]) continue;

            // Permanent
            collectedGreenCoins.Add(coin);
            
            // if it's permanent, it should also be in-run since it won't respawn after checkpoint load
            if (!collectedGreenCoinsInRun.Contains(coin))
                collectedGreenCoinsInRun.Add(coin);
            
            // half alpha
            var renderer = coin.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                var c = renderer.color;
                c.a = 0.5f;
                renderer.color = c;
            }
        }

        int collectedCount = collectedGreenCoins.Count;
        for (int i = 0; i < Mathf.Min(greenCoins.Length, levelData.greenCoins.Length); i++)
        {
            if (levelData.greenCoins[i])
                GameEvents.TriggerGreenCoinProgress(collectedCount, greenCoins.Length, i);
        }

        GameEvents.TriggerAllGreenCoinsCollected(AreAllCoinsCollected());
    }

    /// <summary>
    /// Restaura el estado “in-run” desde checkpoint.
    /// Cualquier coin marcada true en greenCoinsInRun debe NO respawnear (se desactiva).
    /// </summary>
    public void ApplyGreenCoinsInRun(bool[] greenCoinsInRun)
    {
        if (greenCoins == null || greenCoins.Length == 0) return;
        if (greenCoinsInRun == null) return;

        for (int i = 0; i < greenCoins.Length; i++)
        {
            var coin = greenCoins[i];
            if (coin == null) continue;

            bool shouldBeCollectedThisRun = i < greenCoinsInRun.Length && greenCoinsInRun[i];

            // Si ya era permanente, LoadCollectedGreenCoins() ya la agregó al in-run.
            if (shouldBeCollectedThisRun && !collectedGreenCoinsInRun.Contains(coin))
            {
                collectedGreenCoinsInRun.Add(coin);
                coin.SetActive(false); // no respawn al cargar checkpoint
            }
        }

        // Disparar eventos de progreso para que HUD/WinScreen queden correctos después de LoadCheckpoint
        int collectedCount = collectedGreenCoinsInRun.Count;
        for (int i = 0; i < greenCoins.Length; i++)
        {
            if (greenCoins[i] == null) continue;

            bool isCollected = collectedGreenCoinsInRun.Contains(greenCoins[i]);
            if (isCollected)
                GameEvents.TriggerGreenCoinProgress(collectedCount, greenCoins.Length, i);
        }

        GameEvents.TriggerAllGreenCoinsCollected(AreAllCoinsCollected());
    }

    public bool[] GetGreenCoinsInRunArray()
    {
        bool[] result = new bool[greenCoins.Length];

        for (int i = 0; i < greenCoins.Length; i++)
        {
            if (greenCoins[i] != null)
                result[i] = collectedGreenCoinsInRun.Contains(greenCoins[i]);
        }

        return result;
    }

    public bool AreAllCoinsCollected()
    {
        if (greenCoins == null || greenCoins.Length == 0) return true;

        for (int i = 0; i < greenCoins.Length; i++)
        {
            var coin = greenCoins[i];
            if (coin == null) continue;

            if (!collectedGreenCoinsInRun.Contains(coin))
                return false;
        }

        return true;
    }

    public int GetCollectedCount() => collectedGreenCoinsInRun.Count;
    public int GetTotalCount() => greenCoins?.Length ?? 0;
}