using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class POWBlock : BumpableBlock
{
    [Header("POW Block Settings")]
    public bool affectVisibleEnemies = true;
    public bool affectAllEnemies = false;
    public bool playKickSounds = true;
    public int maxUses = 1;
    public Sprite[] useStateSprites; // Sprites for different use states (3 uses, 2 uses, 1 use)

    [Header("Effects")]
    public AudioClip powBlockSound;
    public GameObject destructionEffect;

    private int currentUses;
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    private bool hasCachedCamera;

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
    // Override activation checks for POW block specific logic
    protected override bool CanActivate(Collision2D other)
    {
        if (!base.CanActivate(other)) return false;
        
        // POW block specific: Check if has uses remaining
        return currentUses > 0;
    }

    protected override bool CanActivateFromPlayer(MarioMovement player)
    {
        if (!base.CanActivateFromPlayer(player)) return false;
        
        // POW block specific: Check if has uses remaining
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
            DestroyPOWBlock();
        }
    }
    #endregion

    #region POW Effect
    // Activate the POW block effect on enemies
    // Making it public so it can be called from signal receiver
    public void ActivatePOWEffect()
    {
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
            if (enemy != null && enemy.isActiveAndEnabled)
            {
                bool knockRight = Random.value > 0.5f;
                enemy.KnockAway(knockRight, playKickSounds);
            }
        }

        // Play sound using the cached camera
        if (powBlockSound != null)
        {
            AudioSource.PlayClipAtPoint(powBlockSound, mainCamera.transform.position, 1f);
        }

        // Screen shake effect
        StartCoroutine(ScreenShakeEffect());
    }

    private List<EnemyAI> GetVisibleEnemies()
    {
        List<EnemyAI> visibleEnemies = new List<EnemyAI>();

        if (!hasCachedCamera) return visibleEnemies;

        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(Vector3.zero);
        Vector3 topRight = mainCamera.ViewportToWorldPoint(Vector3.one);

        // Get all colliders in the camera view
        Collider2D[] colliders = Physics2D.OverlapAreaAll(bottomLeft, topRight);

        foreach (Collider2D collider in colliders)
        {
            // Check if the object has the "Enemy" tag
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
        EnemyAI[] enemies = FindObjectsOfType<EnemyAI>(true);
        return new List<EnemyAI>(enemies);
    }
    #endregion

    #region Visual Effects
    private void UpdateAppearance()
    {
        if (spriteRenderer != null && useStateSprites != null && useStateSprites.Length > 0)
        {
            int spriteIndex = Mathf.Clamp(currentUses - 1, 0, useStateSprites.Length - 1);
            spriteRenderer.sprite = useStateSprites[spriteIndex];
        }
    }

    private IEnumerator ScreenShakeEffect()
    {
        // Simple screen shake effect - use cached camera
        if (!hasCachedCamera) yield break;

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
        // Play destruction effect
        if (destructionEffect != null)
        {
            Instantiate(destructionEffect, transform.position, Quaternion.identity);
        }

        // Disable collider and renderer first
        if (boxCollider != null) boxCollider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        // Destroy after a delay to allow effects to play
        Destroy(gameObject, 1f);
    }
    #endregion
}