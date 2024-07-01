using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pipeSpawner : MonoBehaviour
{
    public GameObject enemy;
    public Vector2 spawnOffset; // Offset from pipe position to start spawning enemies
    public Vector2 movement;    // Set to (0,0) to disable animation
    public float spawnRate;
    public int maxEnemies;
    private AudioSource audioSource;
    private List<GameObject> enemies;
    private ObjectPhysics.ObjectMovement objectMovement;

    // Start is called before the first frame update
    void Start()
    {
        enemies = new List<GameObject>();
        InvokeRepeating("SpawnEnemy", 0, spawnRate);

        if (enemy.TryGetComponent<ObjectPhysics>(out var physics))
        {
            objectMovement = physics.movement;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Check for dead enemies
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] == null)
            {
                enemies.RemoveAt(i);
                i--;
            }
        }
    }

    void SpawnEnemy()
    {
        if (enemies.Count < maxEnemies)
        {
            GameObject newEnemy = Instantiate(enemy, transform.position + (Vector3)spawnOffset, Quaternion.identity);

            // If no movement, don't do the animation
            if (movement == Vector2.zero)
            {
                return;
            }

            if (newEnemy.TryGetComponent<ObjectPhysics>(out var physics))
            {
                physics.movement = ObjectPhysics.ObjectMovement.still;
            }

            Collider2D enemyCollider = newEnemy.GetComponent<Collider2D>();
            if (enemyCollider != null)
            {
                enemyCollider.enabled = false;
            }

            SpriteRenderer enemyRenderer = newEnemy.GetComponent<SpriteRenderer>();
            int ogLayer = 0;

            if (enemyRenderer != null)
            {
                ogLayer = enemyRenderer.sortingLayerID;
                newEnemy.transform.SetParent(this.transform.parent);
                enemyRenderer.sortingLayerID = 0;
                enemyRenderer.sortingOrder = -1;
            }

            StartCoroutine(MoveOut(newEnemy, ogLayer));
            enemies.Add(newEnemy);
        }
    }

    IEnumerator MoveOut(GameObject enemy, int ogLayer)
    {
        while (true)
        {
            enemy.transform.position += (Vector3)movement * Time.deltaTime;
            if (Vector2.Distance(enemy.transform.position, transform.position + (Vector3)spawnOffset) > 1)
            {
                break;
            }
            yield return null;
        }

        if (enemy.TryGetComponent<ObjectPhysics>(out var physics))
        {
            physics.movement = objectMovement;
        }

        SpriteRenderer enemyRenderer = enemy.GetComponent<SpriteRenderer>();
        if (enemyRenderer != null)
        {
            enemyRenderer.sortingLayerID = ogLayer;
            enemyRenderer.sortingOrder = 0;
        }

        Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }
    }
}
