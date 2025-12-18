using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Checkpoint : MonoBehaviour
{
    public enum CheckpointMode
    {
        Visual,        // Uses sprite / audio / particles
        SilentTrigger  // Only updates respawn; no feedback required
    }

    [Header("Checkpoint")]
    public int checkpointID; // Unique ID for this checkpoint
    public CheckpointMode checkpointMode = CheckpointMode.Visual;

    [Header("Feedback (Visual mode only)")]
    public AudioClip CheckpointSound;
    public Sprite passive;
    public Sprite[] active;
    public ParticleSystem checkpointParticles;
    public GameObject particle;                       // Custom star particle prefab

    [Header("Behaviour")]
    public bool disableColliderOnActivate = true;

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Collider2D checkpointCollider;

    [Header("Spawn")]
    public Vector2 spawnOffset = new(0, 0);

    // Position used by GameManager to place the player
    public Vector2 SpawnPosition
    {
        get
        {
            return transform.position + (Vector3)spawnOffset + new Vector3(0, 0, -1);
        }
    }

    private void Awake()
    {
        checkpointCollider = GetComponent<Collider2D>();
        if (checkpointCollider == null)
        {
            Debug.LogWarning($"Checkpoint '{name}' has no Collider2D. It needs a trigger collider to work.");
        }

        // These are only needed for Visual mode
        if (checkpointMode == CheckpointMode.Visual)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            audioSource = GetComponent<AudioSource>();
        }

        // NEW: Enable/disable based on BOTH "checkpoints enabled" and the selected checkpoint type (0/1/2)
        if (IsAllowedByGlobalMode())
        {
            EnableCheckpoint();
        }
        else
        {
            DisableCheckpoint();
        }
    }

    private void Start()
    {
        GameManager.Instance.AddCheckpoint(this);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        if (!IsAllowedByGlobalMode())
            return;

        // Activate this checkpoint
        SetActive();

        GlobalVariables.checkpoint = checkpointID;

        // refresh all checkpoints so only the correct one is visually active
        GameManager.Instance.OnCheckpointActivated(this);

        GameManager.Instance.SaveProgress();
    }

    // Centralized rule for 0/1/2 behavior
    private bool IsAllowedByGlobalMode()
    {
        // 0: off, 1: visual-only, 2: silent-only
        int mode = GlobalVariables.checkpointMode;

        if (!GlobalVariables.enableCheckpoints || mode == 0)
            return false;

        return (mode == 1 && checkpointMode == CheckpointMode.Visual)
            || (mode == 2 && checkpointMode == CheckpointMode.SilentTrigger);
    }

    /// <summary>
    /// Activates this checkpoint.
    /// playFeedback:
    ///   true  -> changes sprite, plays sound, particles (when the player touches it).
    ///   false -> only static visual state (active sprite), no sound/particle burst (when respawning / loading).
    /// </summary>
    public void SetActive(bool playFeedback = true)
    {
        Debug.Log($"Checkpoint {checkpointID} activated ({checkpointMode})");

        // Optionally disable the collider after activation
        if (disableColliderOnActivate && checkpointCollider != null)
        {
            checkpointCollider.enabled = false;
        }

        // For Visual mode, always show the active sprite when the checkpoint is active,
        // regardless of whether we play full feedback or not.
        if (checkpointMode == CheckpointMode.Visual &&
            spriteRenderer != null &&
            active != null &&
            active.Length > 0)
        {
            spriteRenderer.sprite = active[0];
        }

        // If we should not play feedback (for example when respawning)
        // or this is a SilentTrigger checkpoint, then skip sound/particles.
        if (!playFeedback || checkpointMode == CheckpointMode.SilentTrigger)
            return;

        // From here on, this is only the transient feedback (sound / particles)

        // Play sound
        if (audioSource != null && CheckpointSound != null)
        {
            audioSource.PlayOneShot(CheckpointSound);
        }

        // Play built-in particle system
        if (checkpointParticles != null)
        {
            checkpointParticles.Play();
        }

        // Spawn custom particles
        if (particle != null)
        {
            SpawnParticles();
        }
    }

    public void EnableCheckpoint()
    {
        gameObject.SetActive(true);

        if (checkpointCollider != null)
            checkpointCollider.enabled = true;

        // When enabled and not yet activated, show the passive sprite in Visual mode
        if (checkpointMode == CheckpointMode.Visual && spriteRenderer != null && passive != null)
        {
            spriteRenderer.sprite = passive;
        }
    }

    public void DisableCheckpoint()
    {
        gameObject.SetActive(false);

        if (checkpointCollider != null)
            checkpointCollider.enabled = false;
    }

    #region Particles
    private void SpawnParticles()
    {
        // Spawn 8 particles around the checkpoint and move them outwards
        int[] verticalDirections = new int[] { -1, 0, 1 };
        int[] horizontalDirections = new int[] { -1, 0, 1 };

        for (int i = 0; i < verticalDirections.Length; i++)
        {
            for (int j = 0; j < horizontalDirections.Length; j++)
            {
                // Skip the (0,0) direction
                if (verticalDirections[i] == 0 && horizontalDirections[j] == 0)
                    continue;

                float distance = (verticalDirections[i] != 0 && horizontalDirections[j] != 0) ? 0.7f : 1f;
                Vector3 startOffset = new Vector3(horizontalDirections[j] * distance, verticalDirections[i] * distance, 0);

                GameObject newParticle = Instantiate(particle, transform.position + startOffset, Quaternion.identity);

                // Make the particles move outwards at a constant speed
                var moveOut = newParticle.GetComponent<StarMoveOutward>();
                if (moveOut != null)
                {
                    moveOut.direction = new Vector2(horizontalDirections[j], verticalDirections[i]);
                    moveOut.speed = 2f;
                }
            }
        }
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmos()
    {
        // Draw spawn position based on spawnOffset so you can see where the player will appear
        Vector3 basePosition = transform.position;
        Vector3 spawnPosition = (Vector3)SpawnPosition;

        // Line from checkpoint origin to spawn position
        Gizmos.color = Color.green;
        Gizmos.DrawLine(basePosition, spawnPosition);

        // Small sphere at the spawn position
        Gizmos.DrawWireSphere(spawnPosition, 0.2f);

        // Draw the checkpoint ID as a label in the Scene view
#if UNITY_EDITOR
        Handles.Label(spawnPosition + Vector3.up * 0.3f, $"ID {checkpointID}");
#endif
    }
    #endregion
}