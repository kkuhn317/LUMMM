using System.Collections;
using UnityEngine;

public class NoteBlockController : BumpableBlock
{
    [Header("Note Block Settings")]
    public AudioClip[] noteSounds;
    public float[] noteSoundProbabilities;
    public float animationDistance = 0.5f;
    public float animationDuration = 0.2f;
    
    [Header("Player Boost")]
    public float jumpBoostForce = 5f;
    public bool applyJumpBoost = true;

    private bool isAnimating = false;

    protected override void Awake()
    {
        base.Awake();
        canBounce = true;
    }

    #region Bounce Handling
    protected override void OnCollisionEnter2D(Collision2D other)
    {
        base.OnCollisionEnter2D(other); // keeps handling bump from below

        // Detect if the player landed on top
        MarioCore player = GetPlayerFromCollision(other);
        if (player == null) return;

        foreach (var contact in other.contacts)
        {
            // If the normal points downwards, the player is on top
            if (contact.normal.y < -0.5f)
            {
                // downward bump
                Bump(BlockHitDirection.Down, player);
                return;
            }
        }
    }

    protected override void OnBeforeBounce(BlockHitDirection direction, MarioCore player)
    {
        PlayRandomNoteSound();
        
        if (applyJumpBoost && player != null && direction != BlockHitDirection.Side)
        {
            ApplyJumpBoost(player);
        }
    }

    protected override IEnumerator BounceRoutine(BlockHitDirection direction)
    {
        isAnimating = true;

        float dirMult = direction == BlockHitDirection.Down ? -1f : 1f;
        float elapsed = 0f;

        // Multiply duration by 2 because the original Note Block logic
        // ran two separate 'animationDuration' loops (one up, one down).
        float totalTime = animationDuration * 2f; 

        while (elapsed < totalTime)
        {
            elapsed += Time.deltaTime;
            
            // Calculate percentage from 0.0 to 1.0
            float t = elapsed / totalTime;
            
            // Evaluate sine wave for smooth easing
            float currentHeight = Mathf.Sin(t * Mathf.PI) * animationDistance;

            transform.position = (Vector3)originalPosition 
                + Vector3.up * (dirMult * currentHeight);
                
            yield return null;
        }

        // Snap perfectly back to the origin
        transform.position = originalPosition;
        isAnimating = false;
    }
    #endregion

    #region Audio
    private void PlayRandomNoteSound()
    {
        if (noteSounds.Length == 0 || audioSource == null) return;

        AudioClip selectedSound = noteSounds[0]; // Default to first sound

        if (noteSoundProbabilities.Length > 0 && noteSoundProbabilities.Length == noteSounds.Length)
        {
            // Probability-based selection
            float totalProbability = 0f;
            foreach (float probability in noteSoundProbabilities)
            {
                totalProbability += probability;
            }

            float randomValue = Random.Range(0f, totalProbability);
            float probabilitySum = 0f;

            for (int i = 0; i < noteSoundProbabilities.Length; i++)
            {
                probabilitySum += noteSoundProbabilities[i];
                if (randomValue <= probabilitySum)
                {
                    selectedSound = noteSounds[i];
                    break;
                }
            }
        }
        else
        {
            // Fallback: random selection
            selectedSound = noteSounds[Random.Range(0, noteSounds.Length)];
        }

        audioSource.PlayOneShot(selectedSound);
    }
    #endregion

    #region Player Interaction
    private void ApplyJumpBoost(MarioCore player)
    {
        Rigidbody2D playerRb = player.Rb;
        if (playerRb == null) return;

        // Only boost if player is below the block's center (hit from below or top, not side)
        if (playerRb.position.y > transform.position.y)
            return;

        Vector2 velocity = playerRb.velocity;
        velocity.y = Mathf.Max(velocity.y, 0f);
        playerRb.velocity = velocity;
        playerRb.AddForce(Vector2.up * jumpBoostForce, ForceMode2D.Impulse);
    }
    #endregion

    #region Enemy Interaction
    protected override void HandleEnemies(BlockHitDirection direction, MarioCore player)
    {
        // Note blocks typically don't damage enemies, but you can enable this if needed but we might not need it
        // base.HandleEnemies(direction, player);
    }
    #endregion

    #region Public Methods
    // Prevent multiple simultaneous activations
    public override void Bump(BlockHitDirection direction, MarioCore player)
    {
        if (!canBounce || isBouncing || isAnimating)
            return;

        base.Bump(direction, player);
    }
    #endregion
}