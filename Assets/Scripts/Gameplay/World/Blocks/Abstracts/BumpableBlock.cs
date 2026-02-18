using System.Collections;
using UnityEngine;

public abstract class BumpableBlock : MonoBehaviour, IBumpable, IGroundPoundable
{
    [Header("Bump Settings")]
    public float bounceHeight = 0.5f;
    public float bounceSpeed = 4f;

    protected Vector2 originalPosition;
    protected BoxCollider2D boxCollider;
    protected AudioSource audioSource;

    protected bool isBouncing;
    protected bool skipBounceThisHit = false;
    protected bool canBounce = true;

    private Coroutine currentBounceCoroutine;
    
    protected virtual void Awake()
    {
        originalPosition = transform.position;
        boxCollider = GetComponent<BoxCollider2D>();
        audioSource = GetComponent<AudioSource>();
        
        // Add warnings for missing critical components
        if (boxCollider == null)
            Debug.LogWarning($"BumpableBlock on {gameObject.name} is missing BoxCollider2D", this);
    }

    #region Activation & Collision
    // Common collision detection for all blocks
    protected virtual void OnCollisionEnter2D(Collision2D other)
    {
        if (!CanActivate(other)) return;
        
        MarioMovement player = GetPlayerFromCollision(other);
        if (player == null) return;

        Bump(BlockHitDirection.Up, player);
    }

    // Common ground pound implementation for all blocks
    public virtual void OnGroundPound(MarioMovement player)
    {
        if (!CanActivateFromPlayer(player)) return;
        Bump(BlockHitDirection.Down, player);
    }

    // Reusable activation checks - override in derived classes for specific logic
    protected virtual bool CanActivate(Collision2D other)
    {
        return other.collider.CompareTag("Player") && CheckHitFromBelow(other);
    }

    protected virtual bool CanActivateFromPlayer(MarioMovement player)
    {
        return player != null;
    }

    // Reusable player extraction
    protected MarioMovement GetPlayerFromCollision(Collision2D other)
    {
        return other.gameObject.GetComponent<MarioMovement>();
    }

    protected virtual bool CheckHitFromBelow(Collision2D other)
    {
        Vector2 impulse = Vector2.zero;

        for (int i = 0; i < other.contactCount; i++)
        {
            ContactPoint2D contact = other.GetContact(i);
            impulse += contact.normal * contact.normalImpulse;
            impulse.x += contact.tangentImpulse * contact.normal.y;
            impulse.y -= contact.tangentImpulse * contact.normal.x;
        }

        // Prevent triggering from top corner
        if (impulse.y <= 0 || other.transform.position.y > transform.position.y)
            return false;

        return true;
    }
    #endregion

    #region Bump Logic
    public void Bump(BlockHitDirection direction, MarioMovement player)
    {
        if (!canBounce || isBouncing)
            return;

        // Stop previous coroutine if exists
        if (currentBounceCoroutine != null)
        {
            StopCoroutine(currentBounceCoroutine);
            // Make sure it returns to original position
            transform.position = originalPosition;
        }
        
        currentBounceCoroutine = StartCoroutine(BumpSequence(direction, player));
    }

    private IEnumerator BumpSequence(BlockHitDirection direction, MarioMovement player)
    {
        isBouncing = true;
        skipBounceThisHit = false;

        originalPosition = transform.position;

        OnBeforeBounce(direction, player);

        HandleEnemies(direction, player);

        if (!skipBounceThisHit)
        {
            yield return BounceRoutine(direction);
        }

        OnAfterBounce(direction, player);

        isBouncing = false;
        currentBounceCoroutine = null;
    }

    protected virtual IEnumerator BounceRoutine(BlockHitDirection direction)
    {
        float targetOffset = direction == BlockHitDirection.Down ? -bounceHeight : bounceHeight;
        float targetY = originalPosition.y + targetOffset;

        // Move towards target position (either up or down)
        float currentY = transform.position.y;
        
        while (Mathf.Abs(currentY - targetY) > 0.01f)
        {
            currentY = Mathf.MoveTowards(currentY, targetY, bounceSpeed * Time.deltaTime);
            transform.position = new Vector3(originalPosition.x, currentY, transform.position.z);
            yield return null;
        }

        // Make sure exact position is set
        transform.position = new Vector3(originalPosition.x, targetY, transform.position.z);

        // Return to original position
        currentY = targetY;
        
        while (Mathf.Abs(currentY - originalPosition.y) > 0.01f)
        {
            currentY = Mathf.MoveTowards(currentY, originalPosition.y, bounceSpeed * Time.deltaTime);
            transform.position = new Vector3(originalPosition.x, currentY, transform.position.z);
            yield return null;
        }

        // Make sure exact position is set
        transform.position = originalPosition;
    }
    #endregion

    #region Enemy Interaction
    protected virtual void HandleEnemies(BlockHitDirection direction, MarioMovement player)
    {
        if (boxCollider == null)
            return;

        if (direction == BlockHitDirection.Up || direction == BlockHitDirection.Down)
        {
            Vector2 worldCenter = (Vector2)transform.TransformPoint(boxCollider.offset) + Vector2.up * 0.05f;
            Vector2 worldSize = Vector2.Scale(boxCollider.size, transform.lossyScale);

            Collider2D[] hits = Physics2D.OverlapBoxAll(
                worldCenter,
                worldSize,
                0f,
                LayerMask.GetMask("Enemy")
            );

            foreach (var col in hits)
            {
                EnemyAI enemy = col.GetComponent<EnemyAI>();
                if (enemy == null)
                    continue;

                bool hitFromLeft = transform.position.x < enemy.transform.position.x;

                // treat bumped enemy as "Koopa Kick" / bump 100 pts
                ComboResult combo = new ComboResult(RewardType.Score, PopupID.Score100, 100);
                // GameManager.Instance.AddScorePoints(100);
                GameManager.Instance?.GetSystem<ScoreSystem>()?.AddScore(100);

                if (ScorePopupManager.Instance != null)
                    ScorePopupManager.Instance.ShowPopup(combo, enemy.transform.position, player.powerupState);

                enemy.KnockAway(hitFromLeft);
            }
        }
    }
    #endregion

    #region Override Points
    protected virtual void OnBeforeBounce(BlockHitDirection direction, MarioMovement player) { }

    protected virtual void OnAfterBounce(BlockHitDirection direction, MarioMovement player) { }
    #endregion

    #region Editor
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(boxCollider.bounds.center + Vector3.up * 0.05f, boxCollider.bounds.size);
    }
#endif
    #endregion
}