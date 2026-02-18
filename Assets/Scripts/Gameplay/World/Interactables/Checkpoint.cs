using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Checkpoint : MonoBehaviour
{
    public enum CheckpointMode
    {
        Visual,     // Uses sprite/audio/particles
        Invisible   // Only updates respawn (minimal feedback)
    }

    [Header("Checkpoint")]
    public int checkpointID; // Unique ID for this checkpoint
    public CheckpointMode checkpointMode = CheckpointMode.Visual;

    [Header("Events")]
    [Tooltip("Triggers when player RESPAWNS at this checkpoint (including on level load).")]
    public UnityEvent OnRespawnActivation;

    [Header("Feedback (Visual mode only)")]
    public AudioClip checkpointSound;
    public Sprite passive;
    public Sprite[] active;
    public ParticleSystem checkpointParticles;
    public GameObject particle; // Custom star particle prefab (optional)

    [Header("Behaviour")]
    public bool disableColliderOnActivate = true;

    [Header("Spawn")]
    public Vector2 spawnOffset = Vector2.zero;

    [Header("Score Reward (optional)")]
    public bool giveScoreOnTouch = true;
    public int checkpointScore = 2000;

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Collider2D checkpointCollider;

    private CheckpointManager checkpointManager;
    private bool respawnInvokedThisLoad;

    public bool IsEnabledByMode { get; private set; }

    // Position used by other systems to place the player
    public Vector2 SpawnPosition => transform.position + (Vector3)spawnOffset + new Vector3(0, 0, -1);

    private void Awake()
    {
        checkpointCollider = GetComponent<Collider2D>();
        if (checkpointCollider == null)
            Debug.LogWarning($"Checkpoint '{name}' has no Collider2D. It needs a trigger collider to work.");

        if (checkpointMode == CheckpointMode.Visual)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            audioSource = GetComponent<AudioSource>();
        }

        RefreshEnabledState();

        // If enabled and not active yet, ensure passive sprite in Visual mode
        if (checkpointMode == CheckpointMode.Visual && spriteRenderer != null && passive != null)
            spriteRenderer.sprite = passive;
    }

    private void OnEnable()
    {
        // Register early (Awake/OnEnable happens before most Start calls)
        checkpointManager = FindObjectOfType<CheckpointManager>();
        if (checkpointManager != null)
            checkpointManager.RegisterCheckpoint(this);

        GameEvents.OnCheckpointLoaded += HandleCheckpointLoaded;
    }

    private void Start()
    {
        // Fallback: if something loads checkpoint before we subscribed
        TryInvokeRespawnActivationIfActive();
    }

    private void OnDisable()
    {
        GameEvents.OnCheckpointLoaded -= HandleCheckpointLoaded;

        if (checkpointManager != null)
            checkpointManager.UnregisterCheckpoint(this);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        RefreshEnabledState();
        if (!IsEnabledByMode)
            return;

        // Optional score reward BEFORE saving (so it’s included in SaveCurrentCheckpoint)
        if (giveScoreOnTouch)
        {
            GlobalVariables.score += checkpointScore;
            GameEvents.TriggerScoreChanged(GlobalVariables.score);
        }

        // Delegate activation + save to manager (refactor style)
        checkpointManager ??= FindObjectOfType<CheckpointManager>();
        if (checkpointManager != null)
            checkpointManager.ActivateCheckpoint(this);
        else
            Debug.LogError($"{nameof(Checkpoint)}: No {nameof(CheckpointManager)} found in scene.");
    }

    // OldCheckpoint-style centralized rule for 0/1/2 behavior
    private bool IsAllowedByGlobalMode()
    {
        // 0: off, 1: visual-only, 2: invisible-only
        int mode = GlobalVariables.checkpointMode;

        if (!GlobalVariables.enableCheckpoints || mode == 0)
            return false;

        return (mode == 1 && checkpointMode == CheckpointMode.Visual)
            || (mode == 2 && checkpointMode == CheckpointMode.Invisible);
    }

    public void RefreshEnabledState()
    {
        IsEnabledByMode = IsAllowedByGlobalMode();

        if (!IsEnabledByMode)
        {
            // Don’t disable GameObject (keeps registration stable). Just disable interaction/visuals.
            if (checkpointCollider != null)
                checkpointCollider.enabled = false;

            if (spriteRenderer != null)
                spriteRenderer.enabled = false;

            return;
        }

        // Enabled
        if (checkpointCollider != null)
            checkpointCollider.enabled = true;

        if (checkpointMode == CheckpointMode.Visual && spriteRenderer != null)
            spriteRenderer.enabled = true;
    }

    /// <summary>
    /// Active visual state + optional transient feedback.
    /// playFeedback:
    ///   true  -> plays sound/particles (when touched)
    ///   false -> only sets active sprite (when loading/respawning)
    /// </summary>
    public void SetActive(bool playFeedback = true)
    {
        // Optionally disable the collider after activation
        if (disableColliderOnActivate && checkpointCollider != null)
            checkpointCollider.enabled = false;

        // Always show active sprite in Visual mode (even if playFeedback is false)
        if (checkpointMode == CheckpointMode.Visual &&
            spriteRenderer != null &&
            active != null &&
            active.Length > 0)
        {
            spriteRenderer.sprite = active[0];
        }

        // Skip transient feedback when respawning/loading
        if (!playFeedback)
            return;

        // Feedback only for Visual checkpoints
        if (checkpointMode == CheckpointMode.Visual)
        {
            if (checkpointSound != null)
            {
                if (audioSource != null) audioSource.PlayOneShot(checkpointSound);
                else AudioSource.PlayClipAtPoint(checkpointSound, transform.position);
            }

            if (checkpointParticles != null)
                checkpointParticles.Play();

            if (particle != null)
                SpawnParticles();
        }
    }

    public void SetPassive()
    {
        // Re-enable collider if we’re in passive state (unless disabled by mode)
        if (checkpointCollider != null && IsEnabledByMode)
            checkpointCollider.enabled = true;

        if (checkpointMode == CheckpointMode.Visual && spriteRenderer != null && passive != null)
            spriteRenderer.sprite = passive;
    }

    public void InvokeRespawnActivation()
    {
        // This is what OldCheckpoint did: fire event when spawning here.
        OnRespawnActivation?.Invoke();
    }

    private void HandleCheckpointLoaded()
    {
        // When manager loads from save, fire respawn activation on the active checkpoint
        TryInvokeRespawnActivationIfActive();
    }

    private void TryInvokeRespawnActivationIfActive()
    {
        if (respawnInvokedThisLoad) return;

        if (GlobalVariables.checkpoint == checkpointID)
        {
            respawnInvokedThisLoad = true;
            InvokeRespawnActivation();
        }
    }

    #region Particles
    private void SpawnParticles()
    {
        // Spawn 8 particles around the checkpoint and move them outwards
        int[] verticalDirections = { -1, 0, 1 };
        int[] horizontalDirections = { -1, 0, 1 };

        for (int i = 0; i < verticalDirections.Length; i++)
        {
            for (int j = 0; j < horizontalDirections.Length; j++)
            {
                if (verticalDirections[i] == 0 && horizontalDirections[j] == 0)
                    continue;

                float distance = (verticalDirections[i] != 0 && horizontalDirections[j] != 0) ? 0.7f : 1f;
                Vector3 startOffset = new Vector3(horizontalDirections[j] * distance, verticalDirections[i] * distance, 0);

                GameObject newParticle = Instantiate(particle, transform.position + startOffset, Quaternion.identity);

                // Optional: if you have StarMoveOutward in the project (OldCheckpoint style)
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
        Vector3 basePosition = transform.position;
        Vector3 spawnPosition = (Vector3)SpawnPosition;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(basePosition, spawnPosition);
        Gizmos.DrawWireSphere(spawnPosition, 0.2f);

#if UNITY_EDITOR
        Handles.Label(spawnPosition + Vector3.up * 0.3f, $"ID {checkpointID}");
#endif
    }
    #endregion
}