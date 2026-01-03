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

        // Brick blocks can be bumped repeatedly (and may break when big)
        if (brickBlock) return true;

        // Used blocks are dead
        if (IsUsed) return false;

        // Always allow activation if we have ANY content OR if onBlockActivated has listeners
        bool hasSpawnables = spawnableItems != null && spawnableItems.Length > 0;
        bool hasConditional = HasConditionalContent();
        bool hasEventListeners = onBlockActivated != null && onBlockActivated.GetPersistentEventCount() > 0;

        // Allow activation if we have content OR event listeners
        if (!hasSpawnables && !hasConditional && !hasEventListeners) return false;

        // Sequential: allow bumps while we still have something to spawn
        if (spawnMode == SpawnMode.Sequential)
            return HasRemainingSpawnables() || hasConditional || hasEventListeners;

        // AllAtOnce: always allow if we have content or event listeners
        return true;
    }

    protected override bool CanActivateFromPlayer(MarioMovement player)
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

    public void ActivateFromSide(MarioMovement player = null)
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

    protected override void OnBeforeBounce(BlockHitDirection direction, MarioMovement player)
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
            if (!PowerStates.IsSmall(player.powerupState))
            {
                pendingBrickBreak = true;
                skipBounceThisHit = true;
                IsUsed = true;
                return;
            }
            else
            {
                skipBounceThisHit = false;
                return;
            }
        }
    }

    protected override void OnAfterBounce(BlockHitDirection direction, MarioMovement player)
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

        if (IsUsed)
            return;

        bool hasSpawnables = spawnableItems != null && spawnableItems.Length > 0;
        bool hasConditional = HasConditionalContent();

        // If block has no content but has event listeners, just become empty
        if (!hasSpawnables && !hasConditional)
        {
            MarkUsedAndEmpty();
            return;
        }

        if (spawnMode == SpawnMode.Sequential)
        {
            SpawnNext(player);
            return;
        }

        // All at once
        SpawnAllAtOnce(player);
        MarkUsedAndEmpty();
    }

    #endregion

    #region Brick Logic

    private void BreakBrick(MarioMovement player)
    {
        bool hasSpawnables = spawnableItems != null && spawnableItems.Length > 0;
        bool hasConditional = HasConditionalContent();

        // If brick contains something, spawn before breaking
        if (hasSpawnables || hasConditional)
        {
            SpawnAllAtOnce(player);
        }

        var breakable = GetComponent<BreakableBlocks>();
        if (breakable != null)
            breakable.Break();
    }

    #endregion

    #region Spawn Helpers

    [System.Serializable]
    public enum SpawnMode
    {
        AllAtOnce,
        Sequential
    }

    private bool HasRemainingSpawnables()
    {
        if (spawnableItems == null) return false;

        for (int i = nextSpawnIndex; i < spawnableItems.Length; i++)
        {
            if (spawnableItems[i] != null)
                return true;
        }
        return false;
    }

    // Block has "content" if it has valid conditional rules
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
    private GameObject ResolveConditionalItem(MarioMovement player)
    {
        if (conditionalItemRules != null && conditionalItemRules.enabled)
        {
            return conditionalItemRules.Resolve(player);
        }
        return null;
    }

    #endregion

    #region Item Spawning

    private void SpawnAllAtOnce(MarioMovement player)
    {
        bool hasSpawnables = spawnableItems != null && spawnableItems.Length > 0;

        // If there are NO spawnables, we can still spawn conditional powerup only
        if (!hasSpawnables)
        {
            if (IsConditionalActive())
            {
                var conditionalItem = ResolveConditionalItem(player);
                if (conditionalItem != null)
                    PresentItems(new List<GameObject> { conditionalItem });
            }
            return;
        }

        List<GameObject> coins = new List<GameObject>();
        List<GameObject> nonCoins = new List<GameObject>();

        foreach (GameObject item in spawnableItems)
        {
            if (item == null) continue;

            if (item.GetComponent<Coin>() != null)
                coins.Add(item);
            else
                nonCoins.Add(item);
        }

        // Coins always spawn normally
        if (coins.Count > 0)
            PresentCoins(coins);

        // Non-coins: either original behavior or conditional override
        if (nonCoins.Count == 0) return;

        if (!IsConditionalActive())
        {
            PresentItems(nonCoins);
            return;
        }

        // Conditional ON: try resolve items using rules
        var conditionalResolved = ResolveConditionalItem(player);

        if (conditionalResolved != null)
        {
            // If we got an item from rules (either matched rule or fallback), spawn it
            PresentItems(new List<GameObject> { conditionalResolved });
        }
        else
        {
            // If Resolve() returns null (NoMatchMode.ReturnNull with no match), 
            // spawn original non-coins as fallback
            PresentItems(nonCoins);
        }
    }

    private void SpawnNext(MarioMovement player)
    {
        // No spawnableItems, but conditional exists -> spawn conditional once, then empty
        if (spawnableItems == null || spawnableItems.Length == 0)
        {
            if (IsConditionalActive())
            {
                var conditionalItem = ResolveConditionalItem(player);
                if (conditionalItem != null)
                    PresentItems(new List<GameObject> { conditionalItem });
            }
            MarkUsedAndEmpty();
            return;
        }

        // Find next valid prefab
        GameObject originalPrefab = null;
        while (nextSpawnIndex < spawnableItems.Length && originalPrefab == null)
        {
            originalPrefab = spawnableItems[nextSpawnIndex];
            nextSpawnIndex++;
        }

        if (originalPrefab == null)
        {
            MarkUsedAndEmpty();
            return;
        }

        // Coins always spawn the same (never conditional)
        if (originalPrefab.GetComponent<Coin>() != null)
        {
            PresentCoins(new List<GameObject> { originalPrefab });
        }
        else
        {
            // Non-coin: use conditional rules if active
            if (IsConditionalActive())
            {
                var conditionalResolved = ResolveConditionalItem(player);

                if (conditionalResolved != null)
                {
                    PresentItems(new List<GameObject> { conditionalResolved });
                }
                else
                {
                    // If Resolve() returns null, spawn original as fallback
                    PresentItems(new List<GameObject> { originalPrefab });
                }
            }
            else
            {
                PresentItems(new List<GameObject> { originalPrefab });
            }
        }

        if (!HasRemainingSpawnables())
        {
            MarkUsedAndEmpty();
        }
    }

    private void ChangeToEmptySprite()
    {
        if (animator != null)
            animator.enabled = false;

        if (spriteRenderer != null && emptyBlockSprite != null)
            spriteRenderer.sprite = emptyBlockSprite;
    }

    private void PresentCoins(List<GameObject> coins)
    {
        if (coins == null || coins.Count == 0 || boxCollider == null) return;

        float startY = originalPosition.y + boxCollider.size.y;

        foreach (GameObject coinPrefab in coins)
        {
            if (coinPrefab == null) continue;

            GameObject coin = Instantiate(coinPrefab, transform.parent);
            coin.transform.position = new Vector2(originalPosition.x, startY);

            Coin coinScript = coin.GetComponent<Coin>();
            if (coinScript != null)
            {
                coinScript.popUpAnimationName = popUpCoinAnimationName;
                coinScript.PopUp();
            }
        }
    }

    private void PresentItems(List<GameObject> items)
    {
        if (items == null || items.Count == 0) return;

        // Use shared presenter (no duplication)
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
                riseSound: itemRiseSound
            );
        }
    }

    #endregion

    #region Conditional Helper Methods

    /// <summary>
    /// Returns true if conditional item rules are active and configured.
    /// </summary>
    private bool IsConditionalActive()
    {
        return conditionalItemRules != null && 
               conditionalItemRules.enabled && 
               conditionalItemRules.HasAnyConfiguredItem();
    }

    #endregion

    #region Public Methods

    public void StopRiseUp()
    {
        if (risingPresenter != null)
            risingPresenter.StopAllRising();
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

        if (risingPresenter != null)
            risingPresenter.ResetStop();

        if (animator != null)
            animator.enabled = true;

        if (spriteRenderer != null && isInvisible) spriteRenderer.enabled = false;
    }

    #endregion
}