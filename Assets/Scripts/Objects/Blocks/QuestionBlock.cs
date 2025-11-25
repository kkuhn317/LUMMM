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

    public float itemMoveHeight = 1f;
    public float itemMoveSpeed = 1f;

    public AudioClip itemRiseSound;
    public UnityEvent onBlockActivated;

    public Sprite emptyBlockSprite;

    [Header("Coin Popup")]
    public string popUpCoinAnimationName = "";

    private bool shouldContinueRiseUp = true;
    private const int GROUND_LAYER = 3;
    private int originalLayer = GROUND_LAYER;
    
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

        if (isInvisible && spriteRenderer != null)
            spriteRenderer.enabled = false;
    }

    #region Activation
    // Override activation checks for question block specific logic
    protected override bool CanActivate(Collision2D other)
    {
        if (!base.CanActivate(other)) return false;
        
        // Question block specific: Don't activate if already used (except brick blocks)
        return !IsUsed || brickBlock;
    }

    protected override bool CanActivateFromPlayer(MarioMovement player)
    {
        if (!base.CanActivateFromPlayer(player)) return false;
        
        // Question block specific: Don't activate if already used (except brick blocks)
        return !IsUsed || brickBlock;
    }

    public void ActivateFromSide(MarioMovement player = null)
    {
        // Question block specific side activation
        if (!IsUsed || brickBlock)
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
                BreakBrick();
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
        // If block has items, spawn them
        if (!IsUsed && spawnableItems.Length > 0)
        {
            SpawnItems();
        }
        
        if (!IsUsed)
        {
            IsUsed = true;
            ChangeToEmptySprite();
        }
    }
    #endregion

    #region Brick Logic
    private void BreakBrick()
    {
        if (spawnableItems.Length > 0)
        {
            // Brick that contains item - spawn items before breaking
            SpawnItems();
        }

        var breakable = GetComponent<BreakableBlocks>();
        if (breakable != null)
            breakable.Break();
    }
    #endregion

    #region Item Spawning
    private void SpawnItems()
    {
        if (spawnableItems.Length == 0) return;

        List<GameObject> coins = new List<GameObject>();
        List<GameObject> otherItems = new List<GameObject>();

        foreach (GameObject item in spawnableItems)
        {
            if (item.GetComponent<Coin>() != null)
                coins.Add(item);
            else
                otherItems.Add(item);
        }

        if (coins.Count > 0)
            PresentCoins(coins);

        if (otherItems.Count > 0)
            PresentItems(otherItems);
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
        if (coins.Count == 0 || boxCollider == null) return;

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
        if (items.Count == 0) return;

        if (audioSource != null && itemRiseSound != null)
            audioSource.PlayOneShot(itemRiseSound);

        foreach (GameObject prefab in items)
        {
            if (prefab == null) continue;

            GameObject item = Instantiate(prefab, transform.parent, true);
            item.transform.position = originalPosition;

            // Disable scripts while rising
            MonoBehaviour[] scripts = item.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != null && script != this)
                    script.enabled = false;
            }

            string oldTag = item.tag;
            SpriteRenderer itemSpriteRenderer = item.GetComponent<SpriteRenderer>();
            int oldSortingLayerId = itemSpriteRenderer?.sortingLayerID ?? 0;

            item.tag = "RisingItem";
            
            if (itemSpriteRenderer != null)
            {
                itemSpriteRenderer.sortingLayerID = 0;
                itemSpriteRenderer.sortingOrder = -1;
            }

            StartCoroutine(RiseUpCoroutine(item, oldTag, oldSortingLayerId, scripts));
        }
    }
    #endregion

    #region Item Animation
    private IEnumerator RiseUpCoroutine(GameObject item, string oldTag, int oldSortingLayerId, MonoBehaviour[] scripts)
    {
        if (item == null) yield break;

        BoxCollider2D itemCollider = item.GetComponent<BoxCollider2D>();
        if (itemCollider != null)
            itemCollider.enabled = false;

        float startTime = Time.time;
        bool colliderEnabled = false;
        float targetY = originalPosition.y + itemMoveHeight;

        while (item != null && shouldContinueRiseUp)
        {
            // Use MoveTowards for smoother movement
            float newY = Mathf.MoveTowards(item.transform.position.y, targetY, itemMoveSpeed * Time.deltaTime);
            item.transform.position = new Vector3(item.transform.position.x, newY, item.transform.position.z);

            // Enable collider after short delay
            if (!colliderEnabled && Time.time >= startTime + 0.25f)
            {
                if (itemCollider != null) 
                    itemCollider.enabled = true;
                colliderEnabled = true;
            }

            // Check if reached target height
            if (item.transform.position.y >= targetY - 0.01f)
            {
                RestoreItemProperties(item, oldTag, oldSortingLayerId, scripts);
                break;
            }

            yield return null;
        }
    }

    private void RestoreItemProperties(GameObject item, string oldTag, int oldSortingLayerId, MonoBehaviour[] scripts)
    {
        if (item == null) return;

        // Re-enable scripts
        foreach (MonoBehaviour script in scripts)
        {
            if (script != null && script != this)
                script.enabled = true;
        }

        item.tag = oldTag;
        
        SpriteRenderer itemSpriteRenderer = item.GetComponent<SpriteRenderer>();
        if (itemSpriteRenderer != null)
        {
            itemSpriteRenderer.sortingLayerID = oldSortingLayerId;
            itemSpriteRenderer.sortingOrder = 0;
        }
    }
    #endregion

    #region Public Methods
    public void StopRiseUp()
    {
        shouldContinueRiseUp = false;
    }

    public void OnPlayerGrabItem()
    {
        StopRiseUp();
    }

    // Public method to reset block state if needed
    public void ResetBlock()
    {
        IsUsed = false;
        shouldContinueRiseUp = true;
        
        if (animator != null)
            animator.enabled = true;
    }
    #endregion
}