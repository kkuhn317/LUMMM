using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class POWEffect : MonoBehaviour
{
    [Header("POW Settings")]
    public bool affectVisibleEnemies = true;
    public bool affectAllEnemies = false;
    public bool playKickSounds = true;

    [Header("Events")]
    public UnityEvent onPOWActivated;
    public UnityEvent onEnemyAffected;

    [Header("Effects")]
    public AudioClip powBlockSound;
    public float screenShakeDuration = 0.3f;
    public float screenShakeMagnitude = 0.1f;

    private Camera mainCamera;
    private bool hasCachedCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        hasCachedCamera = mainCamera != null;
    }

    public void ActivatePOWEffect()
    {
        // Collect enemies to affect
        List<EnemyAI> enemiesToAffect = new List<EnemyAI>();

        if (affectVisibleEnemies)
            enemiesToAffect.AddRange(GetVisibleEnemies());

        if (affectAllEnemies)
            enemiesToAffect.AddRange(GetAllEnemies());

        // Remove duplicates
        HashSet<EnemyAI> uniqueEnemies = new HashSet<EnemyAI>(enemiesToAffect);

        foreach (EnemyAI enemy in uniqueEnemies)
        {
            if (enemy == null)
                continue;

            bool knockRight = Random.value > 0.5f;
            enemy.KnockAway(knockRight, playKickSounds);

            onEnemyAffected?.Invoke();
        }

        // POW sound at camera
        if (powBlockSound != null && hasCachedCamera && mainCamera != null)
            AudioSource.PlayClipAtPoint(powBlockSound, mainCamera.transform.position, 1f);

        // Screen shake
        if (hasCachedCamera && mainCamera != null)
            StartCoroutine(ScreenShakeEffect());

        onPOWActivated?.Invoke();
    }

    private List<EnemyAI> GetVisibleEnemies()
    {
        List<EnemyAI> visibleEnemies = new List<EnemyAI>();
        if (!hasCachedCamera || mainCamera == null) return visibleEnemies;

        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(Vector3.zero);
        Vector3 topRight   = mainCamera.ViewportToWorldPoint(Vector3.one);

        Collider2D[] colliders = Physics2D.OverlapAreaAll(bottomLeft, topRight);

        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Enemy"))
            {
                EnemyAI enemy = col.GetComponent<EnemyAI>();
                if (enemy != null)
                    visibleEnemies.Add(enemy);
            }
        }

        return visibleEnemies;
    }

    private List<EnemyAI> GetAllEnemies()
    {
        // Include inactive ones
        EnemyAI[] enemies = FindObjectsOfType<EnemyAI>(true);
        return new List<EnemyAI>(enemies);
    }

    private IEnumerator ScreenShakeEffect()
    {
        if (!hasCachedCamera || mainCamera == null) yield break;

        Vector3 originalPosition = mainCamera.transform.position;
        float elapsed = 0f;

        while (elapsed < screenShakeDuration)
        {
            float x = Random.Range(-1f, 1f) * screenShakeMagnitude;
            float y = Random.Range(-1f, 1f) * screenShakeMagnitude;

            mainCamera.transform.position = originalPosition + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = originalPosition;
    }
}