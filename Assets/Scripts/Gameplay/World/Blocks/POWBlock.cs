using System.Collections;
using System.Collections.Generic;
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
    public bool affectVisibleEnemies = true;
    public bool affectAllEnemies = false;
    public bool playKickSounds = true;
    public int maxUses = 1;
    public POWUseState[] useStates; // Per-use sprite + event

    [Header("Events")]
    public UnityEvent onPOWActivated;
    public UnityEvent onPOWDepleted;
    public UnityEvent onEnemyAffected;

    [Header("Effects")]
    public AudioClip powBlockSound;
    public GameObject destructionEffect;

    private int currentUses;
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    private bool hasCachedCamera;

    // To avoid firing onUse multiple times for the same state
    private int lastUseStateIndex = -1;

    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentUses = maxUses;
        UpdateAppearance();
        
        // Cache camera reference
        mainCamera = Camera.main;
        hasCachedCamera = mainCamera != null;
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
        if (currentUses <= 0)
        {
            skipBounceThisHit = true;
            return;
        }

        ActivatePOWEffect();
        currentUses--;
        UpdateAppearance();
    }

    protected override void OnAfterBounce(BlockHitDirection direction, MarioMovement player)
    {
        if (currentUses <= 0)
        {
            onPOWDepleted?.Invoke();
            DestroyPOWBlock();
        }
    }
    #endregion

    #region POW Effect
    public void ActivatePOWEffect()
    {
        // Collect enemies to affect
        List<EnemyAI> enemiesToAffect = new List<EnemyAI>();

        if (affectVisibleEnemies)
        {
            enemiesToAffect.AddRange(GetVisibleEnemies());
        }

        if (affectAllEnemies)
        {
            enemiesToAffect.AddRange(GetAllEnemies());
        }

        // Remove duplicates
        HashSet<EnemyAI> uniqueEnemies = new HashSet<EnemyAI>(enemiesToAffect);

        foreach (EnemyAI enemy in uniqueEnemies)
        {
            if (enemy == null)
                continue;

            bool knockRight = Random.value > 0.5f;
            enemy.KnockAway(knockRight, playKickSounds);

            // Per-enemy event
            onEnemyAffected?.Invoke();
        }

        // Play sound using the cached camera
        if (powBlockSound != null && hasCachedCamera && mainCamera != null)
        {
            AudioSource.PlayClipAtPoint(powBlockSound, mainCamera.transform.position, 1f);
        }

        // Screen shake effect
        if (hasCachedCamera && mainCamera != null)
        {
            StartCoroutine(ScreenShakeEffect());
        }

        // Global POW activation event
        onPOWActivated?.Invoke();
    }

    private List<EnemyAI> GetVisibleEnemies()
    {
        List<EnemyAI> visibleEnemies = new List<EnemyAI>();

        if (!hasCachedCamera || mainCamera == null) return visibleEnemies;

        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(Vector3.zero);
        Vector3 topRight = mainCamera.ViewportToWorldPoint(Vector3.one);

        Collider2D[] colliders = Physics2D.OverlapAreaAll(bottomLeft, topRight);

        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                EnemyAI enemy = collider.GetComponent<EnemyAI>();
                if (enemy != null)
                {
                    visibleEnemies.Add(enemy);
                }
            }
        }

        return visibleEnemies;
    }

    private List<EnemyAI> GetAllEnemies()
    {
        // Include inactive enemies as well
        EnemyAI[] enemies = FindObjectsOfType<EnemyAI>(true);
        return new List<EnemyAI>(enemies);
    }
    #endregion

    #region Visual Effects
    private void UpdateAppearance()
    {
        if (spriteRenderer == null || useStates == null || useStates.Length == 0)
            return;

        // Same mapping as before: currentUses - 1 -> index
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

    private IEnumerator ScreenShakeEffect()
    {
        if (!hasCachedCamera || mainCamera == null) yield break;

        Vector3 originalPosition = mainCamera.transform.position;
        float shakeDuration = 0.3f;
        float shakeMagnitude = 0.1f;

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            mainCamera.transform.position = originalPosition + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = originalPosition;
    }
    #endregion

    #region Destruction
    private void DestroyPOWBlock()
    {
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