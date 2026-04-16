using System.Collections;
using UnityEngine;

/// <summary>
/// Base class for all hittable/bumpable blocks.
///
/// Subclasses override:
///   CanActivate(Collision2D) — whether a physics collision activates the block
///   CanActivateFromPlayer(MarioCore) — additional per-player gate
///   OnBeforeBounce(direction, player) — runs before bounce; set skipBounceThisHit = true to cancel
///   OnAfterBounce(direction, player) — runs after bounce; spawn items, decrement counters, etc.
///   BounceRoutine(direction) — override the entire bounce animation
///   HandleEnemies(direction, player) — handle enemies sitting on the block when bumped
///   OnCollisionEnter2D(Collision2D) — extend collision handling (call base to keep default bump)
/// </summary>
public class BumpableBlock : MonoBehaviour, IBumpable
{
    protected BoxCollider2D boxCollider;
    protected AudioSource audioSource;
    protected Vector2 originalPosition;

    /// <summary>Whether this block can currently bounce at all.</summary>
    protected bool canBounce  = true;

    /// <summary>True while the bounce animation coroutine is running.</summary>
    protected bool isBouncing = false;

    /// <summary>
    /// Set to true in OnBeforeBounce to skip the animation for this specific hit.
    /// Automatically reset to false before every hit.
    /// </summary>
    protected bool skipBounceThisHit = false;

    [Header("Bounce Settings")]
    [SerializeField] private float bounceHeight   = 0.15f;
    [SerializeField] private float bounceDuration = 0.1f;
    [SerializeField] private AudioClip bumpSound;

    protected virtual void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        audioSource = GetComponent<AudioSource>();
        originalPosition = transform.position;
    }

    /// <summary>
    /// Whether a physics collision should activate this block.
    /// Base: allows hits from below only (contact normal pointing down).
    /// </summary>
    protected virtual bool CanActivate(Collision2D other)
    {
        foreach (ContactPoint2D contact in other.contacts)
        {
            // Normal points upward = player hit the block from below, pushing up into it.
            // Use contact point Y vs block bottom edge to distinguish from a side hit.
            if (contact.normal.y > 0.5f)
            {
                float blockBottom = boxCollider != null
                    ? boxCollider.bounds.min.y
                    : transform.position.y;
                // Contact point must be near the bottom of the block, not the top
                if (contact.point.y <= blockBottom + 0.2f)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Additional per-player gate called when a MarioCore is involved.
    /// Base: always returns true.
    /// </summary>
    protected virtual bool CanActivateFromPlayer(MarioCore player)
    {
        return true;
    }

    protected virtual void OnCollisionEnter2D(Collision2D other)
    {
        Debug.Log($"[Block] OnCollisionEnter2D fired by {other.gameObject.name}");
    
        foreach (ContactPoint2D contact in other.contacts)
            Debug.Log($"[Block] contact normal={contact.normal} point={contact.point} blockBottom={boxCollider.bounds.min.y}");

        if (!CanActivate(other)) return;

        MarioCore player = GetPlayerFromCollision(other);

        if (player != null)
        {
            if (!CanActivateFromPlayer(player)) return;
            Bump(BlockHitDirection.Up, player);
        }
        else
        {
            Bump(BlockHitDirection.Up, null);
        }
    }

    /// <summary>
    /// Trigger a bump hit from any source (physics, scripted side hit, etc.).
    /// </summary>
    public virtual void Bump(BlockHitDirection direction, MarioCore player)
    {
        if (!canBounce || isBouncing) return;

        skipBounceThisHit = false;
        OnBeforeBounce(direction, player);

        HandleEnemies(direction, player);

        if (!skipBounceThisHit)
            StartCoroutine(BounceRoutineInternal(direction, player));
        else
            OnAfterBounce(direction, player);
    }

    protected virtual void OnBeforeBounce(BlockHitDirection direction, MarioCore player) { }

    protected virtual void OnAfterBounce(BlockHitDirection direction, MarioCore player) { }

    protected virtual void HandleEnemies(BlockHitDirection direction, MarioCore player)
    {
        Vector2 center = (Vector2)transform.position + Vector2.up * (boxCollider.size.y * 0.5f + 0.1f);
        Vector2 size   = new Vector2(boxCollider.size.x * 0.9f, 0.2f);

        bool knockLeft = player != null
            ? player.transform.position.x > transform.position.x
            : true;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyAI>() ?? hit.GetComponentInParent<EnemyAI>();
            if (enemy != null)
            {
                enemy.KnockAwayFromBlock(knockLeft);
                continue;
            }

            // Fallback for non-EnemyAI physics objects on top of the block
            /*var obj = hit.GetComponent<ObjectPhysics>() ?? hit.GetComponentInParent<ObjectPhysics>();
            if (obj is PowerUp) return;
            obj?.KnockAway(knockLeft);*/
        }
    }

    /// <summary>
    /// Override this to replace the entire bounce animation.
    /// Signature matches NoteBlockController's override exactly.
    /// </summary>
    protected virtual IEnumerator BounceRoutine(BlockHitDirection direction)
    {
        float dirMult = direction == BlockHitDirection.Down ? -1f : 1f;
        float half = bounceDuration / 2f;
        float elapsed = 0f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            transform.position = (Vector3)originalPosition
                + Vector3.up * (dirMult * bounceHeight * (elapsed / half));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            transform.position = (Vector3)originalPosition
                + Vector3.up * (dirMult * bounceHeight * (1f - elapsed / half));
            yield return null;
        }

        transform.position = originalPosition;
    }

    // Internal wrapper, manages isBouncing flag and fires OnAfterBounce
    private IEnumerator BounceRoutineInternal(BlockHitDirection direction, MarioCore player)
    {
        isBouncing = true;

        if (bumpSound != null && audioSource != null)
            audioSource.PlayOneShot(bumpSound);

        yield return StartCoroutine(BounceRoutine(direction));

        isBouncing = false;
        OnAfterBounce(direction, player);
    }

    /// <summary>
    /// Extracts a MarioCore from a collision. Used by subclasses.
    /// </summary>
    protected MarioCore GetPlayerFromCollision(Collision2D other)
    {
        return other.gameObject.GetComponent<MarioCore>()
            ?? other.gameObject.GetComponentInParent<MarioCore>();
    }
}