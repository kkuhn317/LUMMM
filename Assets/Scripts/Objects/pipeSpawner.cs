using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pipeSpawner : MonoBehaviour
{
    public GameObject enemy;
    public Vector2 movement;
    public float spawnRate;
    public int maxEnemies;
    private AudioSource audioSource;

    private List<GameObject> enemies;

    // Start is called before the first frame update
    void Start()
    {
        enemies = new List<GameObject>();
        InvokeRepeating("SpawnEnemy", 0, spawnRate);
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
            GameObject newEnemy = Instantiate(enemy, transform.position, Quaternion.identity);
            MonoBehaviour[] scripts = newEnemy.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                script.enabled = false;
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

            StartCoroutine(MoveOut(newEnemy, ogLayer, scripts));
            enemies.Add(newEnemy);
        }
    }

    IEnumerator MoveOut(GameObject enemy, int ogLayer, MonoBehaviour[] scripts)
    {
        while (true)
        {
            enemy.transform.position += (Vector3)movement * Time.deltaTime;
            if (Vector2.Distance(enemy.transform.position, transform.position) > 1)
            {
                break;
            }
            yield return null;
        }

        foreach (MonoBehaviour script in scripts)
        {
            script.enabled = true;
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
