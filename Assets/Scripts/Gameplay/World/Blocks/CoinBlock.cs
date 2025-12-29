using UnityEngine;

public class CoinBlock : BumpableBlock
{
    [Header("Visuals")]
    public Sprite emptyBlockSprite;

    [Header("Coin Spawn")]
    public GameObject coinPrefab;
    public string popUpCoinAnimationName = "";
    public AudioClip coinSound;

    [Header("Timing")]
    [Tooltip("How long (seconds) the block stays in multi-coin mode after the FIRST hit.")]
    public float activeWindowSeconds = 3.8f;

    [Tooltip("If true, each hit while active refreshes the window timer.")]
    public bool refreshWindowOnHit = false;

    [Header("Coin Cap (Optional)")]
    [Tooltip("If > 0, caps the number of coins given during the active window. If <= 0, unlimited during window.")]
    public int maxCoinsDuringWindow = 0;

    [Header("Bonus Reward")]
    public bool enableBonusReward = false;

    [Tooltip("If player reaches this many hits during the active window, bonus can be earned.")]
    public int bonusThreshold = 10;

    [Header("Conditional Bonus Rules")]
    [Tooltip("Rules are evaluated top-to-bottom. First match wins.\n" +
             "Use 'Any' condition for default bonus, or set fallback item in ConditionalItemRules.")]
    public ConditionalItemRules conditionalBonusRules = new();

    public enum BonusAwardTiming
    {
        ImmediatelyWhenThresholdReached,
        OnFinalHitAfterExpiry
    }

    [Tooltip("When the bonus spawns if earned.")]
    public BonusAwardTiming bonusAwardTiming = BonusAwardTiming.OnFinalHitAfterExpiry;

    [Header("Bonus Presentation")]
    [Tooltip("How high the bonus reward rises.")]
    public float bonusMoveHeight = 1f;

    [Tooltip("How fast the bonus reward rises.")]
    public float bonusMoveSpeed = 1f;

    public AudioClip itemRiseSound;

    [Tooltip("Optional. If null, it will auto-add one to this GameObject.")]
    public RisingItemPresenter risingPresenter;

    [Header("After Empty Behavior")]
    [Tooltip("If true, the block can still bounce after it becomes empty (visual-only).")]
    public bool allowBounceWhenEmpty = false;

    private bool isUsed = false;

    // timing state
    private bool isActive = false;
    private float expireTime = 0f;

    // coin state
    private int coinsGivenDuringWindow = 0;

    // bonus state
    private bool bonusEarned = false;
    private bool bonusSpawned = false;

    // last player who hit (used for conditional bonus resolution)
    private MarioMovement lastHitPlayer;

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (risingPresenter == null)
        {
            risingPresenter = GetComponent<RisingItemPresenter>();
            if (risingPresenter == null)
                risingPresenter = gameObject.AddComponent<RisingItemPresenter>();
        }
        
        // Validate bonus configuration
        ValidateBonusConfiguration();
    }

    // Prevent activation once empty (unless allowBounceWhenEmpty)
    protected override bool CanActivate(Collision2D other)
    {
        if (!base.CanActivate(other)) return false;
        if (isUsed) return allowBounceWhenEmpty;
        return true;
    }

    protected override bool CanActivateFromPlayer(MarioMovement player)
    {
        if (!base.CanActivateFromPlayer(player)) return false;
        if (isUsed) return allowBounceWhenEmpty;
        return true;
    }

    protected override void OnBeforeBounce(BlockHitDirection direction, MarioMovement player)
    {
        // BounceRoutine runs BEFORE OnAfterBounce in BumpableBlock,
        // so we must skip the bounce here if we don't want empty blocks to bounce.
        if (isUsed && !allowBounceWhenEmpty)
        {
            skipBounceThisHit = true;
        }
    }

    protected override void OnAfterBounce(BlockHitDirection direction, MarioMovement player)
    {
        lastHitPlayer = player;

        // If empty, do nothing (we already handled bounce skipping in OnBeforeBounce).
        if (isUsed) return;

        // First activation starts the time window
        if (!isActive)
        {
            isActive = true;
            expireTime = Time.time + activeWindowSeconds;

            // Reset counters for this run
            coinsGivenDuringWindow = 0;
            bonusEarned = false;
            bonusSpawned = false;
        }

        bool windowExpired = Time.time > expireTime;

        // If window expired, finalize
        if (windowExpired)
        {
            // If bonus is earned and we spawn on final hit after expiry
            if (enableBonusReward && bonusEarned && !bonusSpawned &&
                bonusAwardTiming == BonusAwardTiming.OnFinalHitAfterExpiry)
            {
                bonusSpawned = true;
                SpawnBonusReward();
            }

            MarkUsed();
            return;
        }

        // Window still active: give coin if under cap (cap optional)
        if (maxCoinsDuringWindow <= 0 || coinsGivenDuringWindow < maxCoinsDuringWindow)
        {
            GiveCoin(player);
            coinsGivenDuringWindow++;

            if (refreshWindowOnHit)
                expireTime = Time.time + activeWindowSeconds;

            HandleBonusProgress();
        }
        else
        {
            // Even if we aren't giving more coins, still allow bonus tracking if you want:
            // (Here it's tied to coinsGivenDuringWindow, so hitting after cap won't progress.)
            // If desired, add a separate hit counter.
            HandleBonusProgress();
        }
    }

    private void GiveCoin(MarioMovement player)
    {
        if (coinPrefab == null) return;

        if (audioSource != null && coinSound != null)
            audioSource.PlayOneShot(coinSound);

        // Spawn coin above block
        float startY = originalPosition.y;
        if (boxCollider != null)
            startY += boxCollider.size.y;

        GameObject coin = Instantiate(coinPrefab, transform.parent);
        coin.transform.position = new Vector2(originalPosition.x, startY);

        Coin coinScript = coin.GetComponent<Coin>();
        if (coinScript != null)
        {
            coinScript.popUpAnimationName = popUpCoinAnimationName;
            coinScript.PopUp();
        }
    }

    private void HandleBonusProgress()
    {
        if (!enableBonusReward) return;
        if (bonusSpawned) return;

        // Check if conditional bonus rules are configured
        if (!IsBonusConfigured())
            return;

        // Earn the bonus as soon as threshold is reached during the active window.
        if (!bonusEarned && coinsGivenDuringWindow >= bonusThreshold)
        {
            bonusEarned = true;

            // spawn immediately when threshold reached
            if (bonusAwardTiming == BonusAwardTiming.ImmediatelyWhenThresholdReached)
            {
                bonusSpawned = true;
                SpawnBonusReward();
            }
        }
    }

    private void SpawnBonusReward()
    {
        GameObject resolved = null;

        if (conditionalBonusRules != null && conditionalBonusRules.enabled)
            resolved = conditionalBonusRules.Resolve(lastHitPlayer);

        if (resolved == null)
        {
            // If no rules match and no fallback configured, don't spawn anything
#if UNITY_EDITOR
            if (conditionalBonusRules != null && conditionalBonusRules.enabled && enableBonusReward)
            {
                Debug.LogWarning($"{gameObject.name}: Bonus earned but no conditional rule matched and no fallback item configured.", this);
            }
#endif
            return;
        }

        if (risingPresenter == null)
        {
            // Fallback: spawn at position without presentation
            Instantiate(resolved, originalPosition, Quaternion.identity, transform.parent);
            return;
        }

        risingPresenter.PresentRising(
            prefab: resolved,
            parent: transform.parent,
            origin: originalPosition,
            moveHeight: bonusMoveHeight,
            moveSpeed: bonusMoveSpeed,
            audioSource: audioSource,
            riseSound: itemRiseSound
        );
    }

    private void MarkUsed()
    {
        if (isUsed) return;
        isUsed = true;

        if (spriteRenderer != null && emptyBlockSprite != null)
            spriteRenderer.sprite = emptyBlockSprite;

        if (animator != null)
            animator.enabled = false;
    }

    // Optional reset (useful for editor testing)
    public void ResetBlock()
    {
        isUsed = false;
        isActive = false;
        coinsGivenDuringWindow = 0;

        bonusEarned = false;
        bonusSpawned = false;

        if (risingPresenter != null)
            risingPresenter.ResetStop();

        if (animator != null)
            animator.enabled = true;
    }

    #region Helper Methods

    /// <summary>
    /// Returns true if bonus rewards are properly configured.
    /// </summary>
    private bool IsBonusConfigured()
    {
        return conditionalBonusRules != null &&
               conditionalBonusRules.enabled &&
               conditionalBonusRules.HasAnyConfiguredItem();
    }

    /// <summary>
    /// Validates bonus configuration and logs warnings for potentially confusing setups.
    /// </summary>
    private void ValidateBonusConfiguration()
    {
#if UNITY_EDITOR
        if (enableBonusReward)
        {
            if (conditionalBonusRules == null)
            {
                Debug.LogWarning($"{gameObject.name}: Bonus enabled but conditionalBonusRules is null.", this);
            }
            else if (!conditionalBonusRules.enabled)
            {
                Debug.LogWarning($"{gameObject.name}: Bonus enabled but conditionalBonusRules.enabled is false.", this);
            }
            else if (!conditionalBonusRules.HasAnyConfiguredItem())
            {
                Debug.LogWarning($"{gameObject.name}: Bonus enabled but no rules or fallback item configured in conditionalBonusRules.", this);
            }
            
            // Warn if bonusThreshold is too high relative to coin cap
            if (maxCoinsDuringWindow > 0 && bonusThreshold > maxCoinsDuringWindow)
            {
                Debug.LogWarning($"{gameObject.name}: bonusThreshold ({bonusThreshold}) > maxCoinsDuringWindow ({maxCoinsDuringWindow}). Bonus may never be earned.", this);
            }
        }
#endif
    }

    #endregion
}