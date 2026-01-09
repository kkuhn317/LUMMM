using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class POWUseState
{
    public Sprite sprite;
    public UnityEvent onUse; // fired when we transition into this state
}

public class POWBlock : BumpableBlock
{
    [Header("POW Block Settings")]
    [Tooltip("How many times this POW block can be used before it is depleted.")]
    public int maxUses = 1;

    [Tooltip("Per-use visuals & events. Index 0 = first use, 1 = second, etc.")]
    public POWUseState[] useStates; // Per-use sprite + event

    [SerializeField] private POWEffect powEffect;

    [Header("Block Events")]
    [Tooltip("Invoked every time the POW block is successfully used.")]
    public UnityEvent onPOWActivated;

    [Tooltip("Invoked once when the POW block is fully depleted.")]
    public UnityEvent onPOWDepleted;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string useTrigger = "hit";
    [SerializeField] private string hitTrigger = "finalUse";
    [SerializeField] private float timeBeforeDestroy = 1f;
    public GameObject destructionEffect;

    private bool isDestroying;

    private int currentUses;
    private SpriteRenderer spriteRenderer;

    // To avoid firing onUse multiple times for the same state
    private int lastUseStateIndex = -1;

    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (powEffect == null)
            powEffect = GetComponent<POWEffect>();

        currentUses = maxUses;
        UpdateAppearance();
    }

    #region Activation
    protected override bool CanActivate(Collision2D other)
    {
        if (!base.CanActivate(other)) return false;
        return currentUses > 0;
    }

    protected override bool CanActivateFromPlayer(MarioMovement player)
    {
        if (!base.CanActivateFromPlayer(player)) return false;
        return currentUses > 0;
    }
    #endregion

    #region Bounce Handling
    protected override void OnBeforeBounce(BlockHitDirection direction, MarioMovement player)
    {
        // If we are out of uses, cancel the bounce
        if (currentUses <= 0)
        {
            skipBounceThisHit = true;
            return;
        }

        if (animator != null && !string.IsNullOrEmpty(useTrigger))
        {
            animator.SetTrigger(useTrigger);
        }

        // Apply the POW use (effect + usage count + appearance + event)
        ApplyPOWUseInternal();
    }

    protected override void OnAfterBounce(BlockHitDirection direction, MarioMovement player)
    {
        base.OnAfterBounce(direction, player);
        HandleDepletionIfNeeded();
    }
    #endregion

    #region POW Effect
    /// <summary>
    /// Public entry point in case you want to trigger it from signals, animations, etc.
    /// This bypasses the bounce, so we also handle depletion directly here.
    /// </summary>
    public void ApplyPOWUse()
    {
        if (currentUses <= 0) return;

        ApplyPOWUseInternal();
        HandleDepletionIfNeeded();
    }

    private void ApplyPOWUseInternal()
    {
        // Trigger the shared POW effect (composition)
        if (powEffect != null)
        {
            powEffect.ActivatePOWEffect();
        }
        else
        {
            Debug.LogWarning($"POWBlock on {name} has no POWEffect assigned.");
        }

        currentUses--;
        UpdateAppearance();

        // Block-level event
        onPOWActivated?.Invoke();
    }

    private void HandleDepletionIfNeeded()
    {
        if (currentUses <= 0 && !isDestroying)
        {
            onPOWDepleted?.Invoke();
            DestroyPOWBlock();
        }
    }

    private void UpdateAppearance()
    {
        if (spriteRenderer == null || useStates == null || useStates.Length == 0)
            return;

        // Map currentUses -> index (1..N -> 0..N-1), clamped
        int index = Mathf.Clamp(currentUses - 1, 0, useStates.Length - 1);
        POWUseState state = useStates[index];

        if (state.sprite != null)
        {
            spriteRenderer.sprite = state.sprite;
        }

        // Only fire the onUse event when we actually transition into a new state
        if (index != lastUseStateIndex)
        {
            lastUseStateIndex = index;
            state.onUse?.Invoke();
        }
    }
    #endregion

    #region Destruction
    private void DestroyPOWBlock()
    {
        if (isDestroying) return;
        isDestroying = true;

        StartCoroutine(DestroyRoutine());
    }

    private IEnumerator DestroyRoutine()
    {
        if (animator != null && !string.IsNullOrEmpty(hitTrigger))
        {
            animator.SetTrigger(hitTrigger);
        }

        yield return new WaitForSeconds(timeBeforeDestroy);

        if (destructionEffect != null)
        {
            Instantiate(destructionEffect, transform.position, Quaternion.identity);
        }

        if (boxCollider != null) boxCollider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        Destroy(gameObject, 1f);
    }
    #endregion
}