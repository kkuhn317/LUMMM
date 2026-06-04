using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class QuestionBlock : BumpableBlock
{
    [Header("Question Block Settings")]
    public bool isInvisible = false;
    public bool brickBlock = false;

    [Header("Items")]
    public GameObject[] spawnableItems;

    [Header("Spawn Mode")]
    public SpawnMode spawnMode = SpawnMode.AllAtOnce;

    [Header("Rise Up Presentation")]
    public float itemMoveHeight = 1f;
    public float itemMoveSpeed = 1f;
    public AudioClip itemRiseSound;

    [Tooltip("Optional. If null, it will auto-add one to this GameObject.")]
    public RisingItemPresenter risingPresenter;

    public UnityEvent onBlockActivated;
    public Sprite emptyBlockSprite;

    [Header("Coin Popup")]
    public string popUpCoinAnimationName = "";

    [Header("Conditional Powerup Override")]
    [Tooltip("Rules are evaluated top-to-bottom. First match wins.\n" +
             "NOTE: Coins always spawn from spawnableItems list.\n" +
             "Non-coin items may be replaced by conditional rules.")]
    public ConditionalItemRules conditionalItemRules = new();

    private const int GROUND_LAYER = 3;
    private int originalLayer = GROUND_LAYER;

    // Sequential state
    private int nextSpawnIndex = 0;

    // State to control if already used
    public bool IsUsed { get; private set; } = false;
    private bool pendingBrickBreak = false;
    private bool hasPlayedRiseSoundThisHit = false;

    // This bucket holds the slow-rising items while the block animates
    private List<GameObject> delayedSpawns = new List<GameObject>();

    // Cache references
    private SpriteRenderer spriteRenderer;
    private PlatformEffector2D platformEffector;
    private Animator animator;

    protected override void Awake()
    {
        base.Awake();

        spriteRenderer = GetComponent<SpriteRenderer>();
        platformEffector = GetComponent<PlatformEffector2D>();
        animator = GetComponent<Animator>();

        if (risingPresenter == null)
        {
            risingPresenter = GetComponent<RisingItemPresenter>();
            if (risingPresenter == null)
                risingPresenter = gameObject.AddComponent<RisingItemPresenter>();
        }

        nextSpawnIndex = 0;

        if (isInvisible && spriteRenderer != null)
            spriteRenderer.enabled = false;
    }

    #region Activation

    protected override bool CanActivate(Collision2D other)
    {
        if (!base.CanActivate(other)) return false;
        if (brickBlock) return true;
        if (IsUsed) return false;

        bool hasSpawnables = spawnableItems != null && spawnableItems.Length > 0;
        bool hasConditional = HasConditionalContent();
        bool hasEventListeners = onBlockActivated != null && onBlockActivated.GetPersistentEventCount() > 0;

        if (!hasSpawnables && !hasConditional && !hasEventListeners) return false;

        if (spawnMode == SpawnMode.Sequential)
            return HasRemainingSpawnables() || hasConditional || hasEventListeners;

        return true;
    }

    protected override bool CanActivateFromPlayer(MarioCore player)
    {
        if (!base.CanActivateFromPlayer(player)) return false;
        if (brickBlock) return true;
        if (IsUsed) return false;

        bool hasSpawnables = spawnableItems != null && spawnableItems.Length > 0;
        bool hasConditional = HasConditionalContent();
        bool hasEventListeners = onBlockActivated != null && onBlockActivated.GetPersistentEventCount() > 0;

        if (!hasSpawnables && !hasConditional && !hasEventListeners) return false;

        if (spawnMode == SpawnMode.Sequential)
            return HasRemainingSpawnables() || hasConditional || hasEventListeners;

        return true;
    }

    public void ActivateFromSide(MarioCore player = null)
    {
        if (brickBlock)
        {
            Bump(BlockHitDirection.Side, player);
            return;
        }

        if (IsUsed) return;

        bool hasSpawnables = spawnableItems != null && spawnableItems.Length > 0;
        bool hasConditional = HasConditionalContent();
        bool hasEventListeners = onBlockActivated != null && onBlockActivated.GetPersistentEventCount() > 0;

        if (!hasSpawnables && !hasConditional && !hasEventListeners) return;

        if (spawnMode != SpawnMode.Sequential || HasRemainingSpawnables() || hasConditional || hasEventListeners)
        {
            Bump(BlockHitDirection.Side, player);
        }
    }

    #endregion

    #region Bounce Handling

    protected override void OnBeforeBounce(BlockHitDirection direction, MarioCore player)
    {
        if (IsUsed && !brickBlock)
        {
            skipBounceThisHit = true;
            return;
        }
        
        onBlockActivated?.Invoke();

        if (isInvisible)
        {
            isInvisible = false;
            if (spriteRenderer != null)
                spriteRenderer.enabled = true;
            if (platformEffector != null)
                platformEffector.useOneWay = false;
            gameObject.layer = originalLayer;
        }

        if (brickBlock && player != null)
        {
            if (!PowerStates.IsSmall(player.State.PowerupState))
            {
                pendingBrickBreak = true;
                skipBounceThisHit = true;
                IsUsed = true;
                return;
            }
            else
            {
                skipBounceThisHit = false;
            }
        }

        delayedSpawns.Clear();
        hasPlayedRiseSoundThisHit = false;

        if (!IsUsed)
        {
            bool hasSpawnables = spawnableItems != null && spawnableItems.Length > 0;
            bool hasConditional = HasConditionalContent();

            if (hasSpawnables || hasConditional)
            {
                // Figure out exactly what is spawning on this hit
                List<GameObject> itemsForThisHit = GetItemsForThisHit(player);

                // Sort them into the Instant bucket or the Delayed bucket
                foreach (GameObject item in itemsForThisHit)
                {
                    if (item == null) continue;

                    if (item.GetComponent<Coin>() != null)
                    {
                        PresentCoins(new List<GameObject> { item });
                    }
                    else if (item.GetComponent<InstantReveal>() != null)
                    {
                        PresentInstantItem(item);
                    }
                    else
                    {
                        delayedSpawns.Add(item); // Save for after the bounce!
                    }
                }
            }

            // Change to empty sprite if no more spawnables (and its not just a plain brick block)
            bool isEmptyBrickBlock = brickBlock && !hasSpawnables && !hasConditional;

            if (!isEmptyBrickBlock)
            {
                if (spawnMode == SpawnMode.Sequential)
                {
                    if (!HasRemainingSpawnables())
                    {
                        MarkUsedAndEmpty();
                    }
                }
                else
                {
                    MarkUsedAndEmpty(); // All-At-Once blocks are always empty after 1 hit
                }
            }
        }
    }

    protected override void OnAfterBounce(BlockHitDirection direction, MarioCore player)
    {
        if (brickBlock)
        {
            if (pendingBrickBreak)
            {
                pendingBrickBreak = false;
                BreakBrick(player);
            }
            return;
        }

        // The bounce is finished, spawn any slow-rising items we saved.
        if (delayedSpawns.Count > 0)
        {
            PresentDelayedItems(delayedSpawns);
            delayedSpawns.Clear();
        }
    }

    #endregion

    #region Brick Logic

    private void BreakBrick(MarioCore player)
    {
        // If brick contains something, evaluate what drops before destroying
        bool hasSpawnables = spawnableItems != null && spawnableItems.Length > 0;
        bool hasConditional = HasConditionalContent();

        if (hasSpawnables || hasConditional)
        {
            List<GameObject> itemsForThisHit = GetItemsForThisHit(player);
            foreach (var item in itemsForThisHit)
            {
                if (item.GetComponent<Coin>() != null) PresentCoins(new List<GameObject> { item });
                else PresentInstantItem(item); // If the block explodes, ALL items pop instantly
            }
        }

        GameManager.Instance?.GetSystem<ScoreSystem>()?.AddScore(50);

        var breakable = GetComponent<BreakableBlocks>();
        if (breakable != null)
            breakable.Break();
    }

    #endregion

    #region Spawn Sorting Logic

    [System.Serializable]
    public enum SpawnMode
    {
        AllAtOnce,
        Sequential
    }

    // Calculates exactly what items should spawn on the CURRENT hit
    private List<GameObject> GetItemsForThisHit(MarioCore player)
    {
        List<GameObject> result = new List<GameObject>();

        if (spawnMode == SpawnMode.AllAtOnce)
        {
            bool hasSpawnables = spawnableItems != null && spawnableItems.Length > 0;

            if (!hasSpawnables)
            {
                if (IsConditionalActive())
                {
                    var conditional = ResolveConditionalItem(player);
                    if (conditional != null) result.Add(conditional);
                }
                return result;
            }

            List<GameObject> nonCoins = new List<GameObject>();
            foreach (GameObject item in spawnableItems)
            {
                if (item == null) continue;
                if (item.GetComponent<Coin>() != null) result.Add(item); 
                else nonCoins.Add(item);
            }

            if (nonCoins.Count > 0)
            {
                if (IsConditionalActive())
                {
                    var conditional = ResolveConditionalItem(player);
                    if (conditional != null) result.Add(conditional);
                    else result.AddRange(nonCoins);
                }
                else
                {
                    result.AddRange(nonCoins);
                }
            }
        }
        else // Sequential
        {
            if (spawnableItems == null || spawnableItems.Length == 0)
            {
                if (IsConditionalActive())
                {
                    var conditional = ResolveConditionalItem(player);
                    if (conditional != null) result.Add(conditional);
                }
                return result;
            }

            GameObject originalPrefab = null;
            while (nextSpawnIndex < spawnableItems.Length && originalPrefab == null)
            {
                originalPrefab = spawnableItems[nextSpawnIndex];
                nextSpawnIndex++;
            }

            if (originalPrefab == null) return result;

            if (originalPrefab.GetComponent<Coin>() != null)
            {
                result.Add(originalPrefab);
            }
            else
            {
                if (IsConditionalActive())
                {
                    var conditional = ResolveConditionalItem(player);
                    if (conditional != null) result.Add(conditional);
                    else result.Add(originalPrefab);
                }
                else
                {
                    result.Add(originalPrefab);
                }
            }
        }

        return result;
    }

    private bool HasRemainingSpawnables()
    {
        if (spawnableItems == null) return false;
        for (int i = nextSpawnIndex; i < spawnableItems.Length; i++)
        {
            if (spawnableItems[i] != null) return true;
        }
        return false;
    }

    private bool HasConditionalContent()
    {
        return conditionalItemRules != null &&
               conditionalItemRules.enabled &&
               conditionalItemRules.HasAnyConfiguredItem();
    }

    private void MarkUsedAndEmpty()
    {
        IsUsed = true;
        ChangeToEmptySprite();
    }

    /// <summary>
    /// Resolve a conditional item using ConditionalItemRules.
    /// Returns null if no match and rules are set to ReturnNull.
    /// Returns fallbackItem if no match and rules are set to UseFallbackItem.
    /// </summary>
    private GameObject ResolveConditionalItem(MarioCore player)
    {
        if (conditionalItemRules != null && conditionalItemRules.enabled)
        {
            return conditionalItemRules.Resolve(player);
        }
        return null;
    }

    private bool IsConditionalActive()
    {
        return conditionalItemRules != null && 
               conditionalItemRules.enabled && 
               conditionalItemRules.HasAnyConfiguredItem();
    }

    #endregion

    #region Item Presentation

    private void ChangeToEmptySprite()
    {
        if (animator != null) animator.enabled = false;
        if (spriteRenderer != null && emptyBlockSprite != null) spriteRenderer.sprite = emptyBlockSprite;
    }

    private void TryPlayRiseSound()
    {
        if (!hasPlayedRiseSoundThisHit && audioSource != null && itemRiseSound != null)
        {
            audioSource.PlayOneShot(itemRiseSound);
            hasPlayedRiseSoundThisHit = true; // Lock the gate!
        }
    }

    private void PresentCoins(List<GameObject> coins)
    {
        if (coins == null || coins.Count == 0 || boxCollider == null) return;
        float startY = originalPosition.y + boxCollider.size.y;

        foreach (GameObject coinPrefab in coins)
        {
            if (coinPrefab == null) continue;
            
            GameObject coin;
            // Check if it's already in the scene
            if (coinPrefab.scene.IsValid())
            {
                coin = coinPrefab;
                coin.SetActive(true); // Wake it up
            }
            else
            {
                coin = Instantiate(coinPrefab, transform.parent);
            }
            
            coin.transform.position = new Vector2(originalPosition.x, startY);

            Coin coinScript = coin.GetComponent<Coin>();
            if (coinScript != null)
            {
                coinScript.popUpAnimationName = popUpCoinAnimationName;
                coinScript.PopUp();
            }
        }
    }

    private void PresentInstantItem(GameObject prefab)
    {
        // Play sound
        TryPlayRiseSound();

        // Check if it's a scene object or a project prefab
        GameObject spawnedItem;
        if (prefab.scene.IsValid())
        {
            spawnedItem = prefab;
            spawnedItem.SetActive(true);
        }
        else
        {
            spawnedItem = Instantiate(prefab, transform.parent);
        }

        float startY = originalPosition.y + (boxCollider != null ? boxCollider.size.y : 1f);
        spawnedItem.transform.position = new Vector2(originalPosition.x, startY);
    }

    private void PresentDelayedItems(List<GameObject> items)
    {
        if (items.Count > 0) TryPlayRiseSound();

        // Pass everything to the RisingPresenter to handle the slow animation
        foreach (GameObject prefab in items)
        {
            if (prefab == null) continue;
            
            risingPresenter.PresentRising(
                prefab: prefab,
                parent: transform.parent,
                origin: originalPosition,
                moveHeight: itemMoveHeight,
                moveSpeed: itemMoveSpeed,
                audioSource: audioSource,
                riseSound: null // Rise sound is already handled here
            );
        }
    }

    #endregion

    #region Public Methods

    public void StopRiseUp()
    {
        if (risingPresenter != null) risingPresenter.StopAllRising();
    }

    public void OnPlayerGrabItem()
    {
        StopRiseUp();
    }

    public void ResetBlock()
    {
        IsUsed = false;
        nextSpawnIndex = 0;
        pendingBrickBreak = false;
        delayedSpawns.Clear();

        if (risingPresenter != null) risingPresenter.ResetStop();
        if (animator != null) animator.enabled = true;
        if (spriteRenderer != null && isInvisible) spriteRenderer.enabled = false;
    }

    #endregion
}