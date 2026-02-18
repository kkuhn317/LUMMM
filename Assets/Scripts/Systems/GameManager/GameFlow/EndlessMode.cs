using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EndlessMode : MonoBehaviour
{
    public Tilemap tilemap;
    public GameObject[] enemiesToSpawn;
    public int startDistance = 0;
    private int distanceWritten = 0;
    public int minHeight = 0;
    public int maxHeight = 5;
    public TileBase groundTile; // Assign this in the Inspector

    private PlayerRegistry playerRegistry;

    // Start is called before the first frame update
    void Start()
    {
        distanceWritten = startDistance;
        CacheRegistry();
    }

    private void CacheRegistry()
    {
        // Try getting registry from the refactored GameManager first
        if (GameManager.Instance != null)
            playerRegistry = GameManager.Instance.GetSystem<PlayerRegistry>();

        // Fallback: find it in scene (useful for additive scenes / init order)
        if (playerRegistry == null)
            playerRegistry = FindObjectOfType<PlayerRegistry>(true);
    }

    // Update is called once per frame
    void Update()
    {
        // Ensure we have the registry (covers cases where GM loads later)
        if (playerRegistry == null) CacheRegistry();

        /*if (GameManager.Instance.GetPlayer(0) == null) return;
            float playerDistance = Mathf.Abs(tilemap.transform.position.x - GameManager.Instance.GetPlayer(0).transform.position.x);*/
        
        MarioMovement p1 = playerRegistry != null ? playerRegistry.GetPlayer(0) : null;
        if (p1 == null) return;

        float playerDistance = Mathf.Abs(tilemap.transform.position.x - p1.transform.position.x);

        if (playerDistance > distanceWritten - 20)
        {
            int newDistance = Mathf.FloorToInt(playerDistance) + 20;
            createTiles(distanceWritten, newDistance);
            createEnemies(distanceWritten, newDistance);
            distanceWritten = newDistance;
        }
    }

    void createTiles(int start, int end)
    {
        for (int i = start; i < end; i++)
        {
            for (int j = minHeight; j < maxHeight; j++)
            {
                if (Random.value < 0.2f)
                {
                    Vector3Int cellPosition = new Vector3Int(i, j, 0);
                    tilemap.SetTile(cellPosition, groundTile);
                }
            }
        }
    }
    
    void createEnemies(int start, int end)
    {
        for (int i = start; i < end; i++)
        {
            if (Random.value < 0.3f) // 30% chance to spawn an enemy
            {
                Vector3 spawnPosition = new Vector3(i, Random.Range(minHeight, maxHeight), 0);
                int enemyIndex = Random.Range(0, enemiesToSpawn.Length);
                Instantiate(enemiesToSpawn[enemyIndex], spawnPosition, Quaternion.identity);
            }
        }
    }
}