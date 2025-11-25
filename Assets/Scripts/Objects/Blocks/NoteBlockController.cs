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
        MarioMovement player = GetPlayerFromCollision(other);
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

    protected override void OnBeforeBounce(BlockHitDirection direction, MarioMovement player)
    {
        PlayRandomNoteSound();
        
        if (applyJumpBoost && player != null)
        {
            ApplyJumpBoost(player);
        }
    }

    protected override IEnumerator BounceRoutine(BlockHitDirection direction)
    {
        isAnimating = true;

        float targetOffset = direction == BlockHitDirection.Down ? -animationDistance : animationDistance;
        // Use Vector2 for all calculations to be consistent with base class
        Vector2 targetPosition = originalPosition + Vector2.up * targetOffset;

        // Move to target position
        float elapsedTime = 0f;
        while (elapsedTime < animationDuration)
        {
            float t = elapsedTime / animationDuration;
            transform.position = Vector2.Lerp(originalPosition, targetPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;

        // Return to original position
        elapsedTime = 0f;
        while (elapsedTime < animationDuration)
        {
            float t = elapsedTime / animationDuration;
            transform.position = Vector2.Lerp(targetPosition, originalPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

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
    private void ApplyJumpBoost(MarioMovement player)
    {
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            // Reset vertical velocity and apply boost
            Vector2 velocity = playerRb.velocity;
            velocity.y = Mathf.Max(velocity.y, 0f); // Only boost if not falling
            playerRb.velocity = velocity;
            playerRb.AddForce(Vector2.up * jumpBoostForce, ForceMode2D.Impulse);
        }
    }
    #endregion

    #region Enemy Interaction
    protected override void HandleEnemies(BlockHitDirection direction, MarioMovement player)
    {
        // Note blocks typically don't damage enemies, but you can enable this if needed but we might not need it
        // base.HandleEnemies(direction, player);
    }
    #endregion

    #region Public Methods
    // Prevent multiple simultaneous activations
    public new void Bump(BlockHitDirection direction, MarioMovement player)
    {
        if (!canBounce || isBouncing || isAnimating)
            return;

        base.Bump(direction, player);
    }
    #endregion
}