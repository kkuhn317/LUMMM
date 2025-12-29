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
    [Tooltip("If enabled, ONLY non-coin items will be resolved by rules (coins always spawn normally).")]
    public bool useConditionalItems = false;

    [Tooltip("Rules are evaluated top-to-bottom. First match wins.")]
    public List<ConditionalSpawnRule> conditionalRules = new();

    [Tooltip("What happens if no rule matches (for non-coin items only).")]
    public ConditionalFallbackMode conditionalFallback = ConditionalFallbackMode.UseSpawnableItems;

    [Tooltip("Used when fallback is UseDefaultPrefab.")]
    public GameObject defaultConditionalPrefab = null;

    private const int GROUND_LAYER = 3;
    private int originalLayer = GROUND_LAYER;

    // Sequential state
    private int nextSpawnIndex = 0;

    // State to control if already used
    public bool IsUsed { get; private set; } = false;

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

        bool hasSpawnables = spawnableItems != null && spawnableItems.Length > 0;
        bool hasConditional = HasConditionalContent();

        // If there is truly nothing to spawn, don't activate.
        if (!hasSpawnables && !hasConditional) return false;

        // Sequential: allow bumps while we still have something to spawn (spawnables OR conditional content)
        if (spawnMode == SpawnMode.Sequential)
            return HasRemainingSpawnables() || hasConditional;

        // AllAtOnce: allow activation if we have spawnables OR conditional content
        return true;
    }

    protected override bool CanActivateFromPlayer(MarioMovement player)
    {
        if (!base.CanActivateFromPlayer(player)) return false;

        if (brickBlock) return true;
        if (IsUsed) return false;

        bool hasSpawnables = spawnableItems != null && spawnableItems.Length > 0;
        bool hasConditional = HasConditionalContent();

        if (!hasSpawnables && !hasConditional) return false;

        if (spawnMode == SpawnMode.Sequential)
            return HasRemainingSpawnables() || hasConditional;

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

        if (!hasSpawnables && !hasConditional) return;

        if (spawnMode != SpawnMode.Sequential || HasRemainingSpawnables() || hasConditional)
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
                BreakBrick(player);
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
            return;

        if (IsUsed)
            return;

        bool hasSpawnables = spawnableItems != null && spawnableItems.Length > 0;
        bool hasConditional = HasConditionalContent();

        // If block truly has no content, become empty on first hit
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

        // All at once (original behavior), but with optional conditional powerup override
        SpawnAllAtOnce(player);
        MarkUsedAndEmpty();
    }

    #endregion

    #region Brick Logic

    private void BreakBrick(MarioMovement player)
    {
        bool hasSpawnables = spawnableItems != null && spawnableItems.Length > 0;
        bool hasConditional = HasConditionalContent();

        // If brick contains something (spawnables or conditional), spawn before breaking
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

    public enum SpawnMode
    {
        AllAtOnce,
        Sequential
    }

    [System.Serializable]
    public class ConditionalSpawnRule
    {
        public string name;
        public PlayerCondition condition = PlayerCondition.IsSmall;
        public GameObject prefabToSpawn;
    }

    public enum PlayerCondition
    {
        Any,
        IsSmall,
        IsBig,
        IsNotSmall
    }

    public enum ConditionalFallbackMode
    {
        UseSpawnableItems,
        UseDefaultPrefab,
        SpawnNothing
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

    // Block has "content" if it has valid conditional rules (even if spawnableItems is empty)
    private bool HasConditionalContent()
    {
        if (!useConditionalItems) return false;

        if (conditionalRules != null)
        {
            foreach (var rule in conditionalRules)
            {
                if (rule != null && rule.prefabToSpawn != null)
                    return true;
            }
        }

        return defaultConditionalPrefab != null;
    }

    private void MarkUsedAndEmpty()
    {
        IsUsed = true;
        ChangeToEmptySprite();
    }

    private bool RuleMatches(PlayerCondition condition, MarioMovement player)
    {
        if (player == null) return false;

        bool isSmall = PowerStates.IsSmall(player.powerupState);
        bool isBig = PowerStates.IsBig(player.powerupState);

        return condition switch
        {
            PlayerCondition.Any => true,
            PlayerCondition.IsSmall => isSmall,
            PlayerCondition.IsBig => isBig,
            PlayerCondition.IsNotSmall => !isSmall,
            _ => false
        };
    }

    private GameObject ResolveConditionalPrefab(MarioMovement player)
    {
        if (conditionalRules == null || conditionalRules.Count == 0)
            return null;

        foreach (var rule in conditionalRules)
        {
            if (rule == null || rule.prefabToSpawn == null) continue;
            if (RuleMatches(rule.condition, player))
                return rule.prefabToSpawn;
        }

        return null;
    }

    #endregion

    #region Item Spawning

    private void SpawnAllAtOnce(MarioMovement player)
    {
        bool hasSpawnables = spawnableItems != null && spawnableItems.Length > 0;

        // If there are NO spawnables, we can still spawn conditional powerup only (Mario-like)
        if (!hasSpawnables)
        {
            if (useConditionalItems)
            {
                var conditionalPrefab = ResolveConditionalPrefab(player);
                if (conditionalPrefab != null)
                {
                    PresentItems(new List<GameObject> { conditionalPrefab });
                }
                else
                {
                    if (conditionalFallback == ConditionalFallbackMode.UseDefaultPrefab && defaultConditionalPrefab != null)
                        PresentItems(new List<GameObject> { defaultConditionalPrefab });
                }
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

        if (!useConditionalItems)
        {
            PresentItems(nonCoins);
            return;
        }

        // Conditional ON: spawn ONE resolved prefab (powerup) if possible
        var conditionalResolved = ResolveConditionalPrefab(player);
        if (conditionalResolved != null)
        {
            PresentItems(new List<GameObject> { conditionalResolved });
            return;
        }

        // No match -> fallback (non-coin only)
        switch (conditionalFallback)
        {
            case ConditionalFallbackMode.UseSpawnableItems:
                PresentItems(nonCoins);
                break;

            case ConditionalFallbackMode.UseDefaultPrefab:
                if (defaultConditionalPrefab != null)
                    PresentItems(new List<GameObject> { defaultConditionalPrefab });
                break;

            case ConditionalFallbackMode.SpawnNothing:
                break;
        }
    }

    private void SpawnNext(MarioMovement player)
    {
        // No spawnableItems, but conditional exists -> spawn conditional once, then empty
        if (spawnableItems == null || spawnableItems.Length == 0)
        {
            if (useConditionalItems)
            {
                var resolved = ResolveConditionalPrefab(player);
                if (resolved != null)
                {
                    PresentItems(new List<GameObject> { resolved });
                }
                else
                {
                    if (conditionalFallback == ConditionalFallbackMode.UseDefaultPrefab && defaultConditionalPrefab != null)
                        PresentItems(new List<GameObject> { defaultConditionalPrefab });
                }
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
            // Non-coin: conditional "powerup slot"
            if (useConditionalItems)
            {
                var resolved = ResolveConditionalPrefab(player);
                if (resolved != null)
                {
                    PresentItems(new List<GameObject> { resolved });
                }
                else
                {
                    switch (conditionalFallback)
                    {
                        case ConditionalFallbackMode.UseSpawnableItems:
                            PresentItems(new List<GameObject> { originalPrefab });
                            break;

                        case ConditionalFallbackMode.UseDefaultPrefab:
                            if (defaultConditionalPrefab != null)
                                PresentItems(new List<GameObject> { defaultConditionalPrefab });
                            break;

                        case ConditionalFallbackMode.SpawnNothing:
                            break;
                    }
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

        if (risingPresenter != null)
            risingPresenter.ResetStop();

        if (animator != null)
            animator.enabled = true;

        if (spriteRenderer != null && isInvisible) spriteRenderer.enabled = false;
    }

    #endregion
}