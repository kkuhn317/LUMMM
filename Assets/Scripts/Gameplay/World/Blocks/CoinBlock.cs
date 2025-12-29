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

    [Tooltip("If true, each successful hit while active refreshes the timer window.")]
    public bool refreshWindowOnHit = false;

    [Header("Limits / Rewards")]
    [Tooltip("Soft cap (classic feel). Set <= 0 to disable.")]
    public int maxCoinsDuringWindow = 10;

    [Header("Bonus Reward")]
    [Tooltip("If enabled, coin block can award a bonus if hit fast enough.")]
    public bool enableBonusReward = true;

    [Tooltip("If player reaches this many hits during the active window, bonus can be earned.")]
    public int bonusThreshold = 10;

    [Tooltip("Optional (mushroom, gold block, etc.)")]
    public GameObject bonusRewardPrefab;

    public enum BonusAwardTiming
    {
        ImmediatelyWhenThresholdReached,
        OnFinalHitAfterExpiry
    }

    [Tooltip("When the bonus is actually spawned.")]
    public BonusAwardTiming bonusAwardTiming = BonusAwardTiming.OnFinalHitAfterExpiry;

    [Header("Bonus Presentation")]
    [Tooltip("If enabled, the bonus reward rises out of the block like a Question Block powerup.")]
    public bool bonusUsesRiseUp = true;

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
    private bool bonusEarned = false; // threshold reached during window
    private bool bonusSpawned = false; // bonus actually spawned

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (risingPresenter == null)
        {
            risingPresenter = GetComponent<RisingItemPresenter>();
            if (risingPresenter == null)
                risingPresenter = gameObject.AddComponent<RisingItemPresenter>();
        }
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

            // First coin is always granted
            GiveCoin(player);
            coinsGivenDuringWindow = 1;

            // Check bonus conditions after first coin
            HandleBonusProgress();

            return;
        }

        bool windowExpired = Time.time > expireTime;

        // If window expired, give one last coin, then (optionally) award bonus on this final hit, then become empty.
        if (windowExpired)
        {
            GiveCoin(player);

            if (enableBonusReward &&
                bonusAwardTiming == BonusAwardTiming.OnFinalHitAfterExpiry &&
                bonusEarned && !bonusSpawned &&
                bonusRewardPrefab != null)
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

            // Bonus progress (earn/spawn depending on timing)
            HandleBonusProgress();
        }
        else
        {
            // Soft cap reached: do nothing, keep waiting for expiry.
            // Note: If you want the player to still be able to "earn" the bonus even after soft cap,
            // switch HandleBonusProgress() to use a separate hit counter instead of coinsGivenDuringWindow.
        }
    }

    private void HandleBonusProgress()
    {
        if (!enableBonusReward) return;
        if (bonusRewardPrefab == null) return;
        if (bonusSpawned) return;

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

    private void SpawnBonusReward()
    {
        if (bonusRewardPrefab == null) return;

        if (!bonusUsesRiseUp)
        {
            GameObject reward = Instantiate(bonusRewardPrefab, transform.parent, true);
            reward.transform.position = originalPosition;
            return;
        }

        risingPresenter.PresentRising(
            prefab: bonusRewardPrefab,
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
        isUsed = true;
        isActive = false;

        if (animator != null)
            animator.enabled = false;

        if (spriteRenderer != null && emptyBlockSprite != null)
            spriteRenderer.sprite = emptyBlockSprite;
    }

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
}