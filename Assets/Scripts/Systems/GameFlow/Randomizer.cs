using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Randomizer : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase groundTile; // Assign this in the Inspector
    public float removeTileChance = 0.1f; // Chance to remove an existing tile
    public float addTileChance = 0.1f; // Chance to add a tile where there isn't one
    public float replaceEnemyChance = 0.7f; // Chance to replace enemy with different one
    public GameObject[] enemies;
    public Vector2 tilescale = Vector2.one;

    // Start is called before the first frame update
    void Start()
    {
        if (!GlobalVariables.cheatRandomizer) return;

        if (tilemap != null && groundTile != null)
        {
            RandomizeTiles();
        }
        else
        {
            Debug.Log("Tilemap or GroundTile not assigned in the inspector. Will not randomize tiles.");
        }

        if (enemies != null && enemies.Length > 0)
        {
            RandomizeEnemies();
        }
        else
        {
            Debug.Log("Enemies not assigned in the inspector. Will not randomize enemies.");
        }
    }


    void RandomizeTiles()
    {
        BoundsInt bounds = tilemap.cellBounds;
        TileBase[] allTiles = tilemap.GetTilesBlock(bounds);

        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                int index = x + y * bounds.size.x;
                TileBase currentTile = allTiles[index];
                Vector3Int tilePosition = new Vector3Int(x + bounds.xMin, y + bounds.yMin, 0);

                float rand = Random.value;
                if (currentTile != null && rand < removeTileChance)
                {
                    tilemap.SetTile(tilePosition, null);
                }
                else if (rand < addTileChance)
                {
                    tilemap.SetTile(tilePosition, groundTile);
                    tilemap.SetTransformMatrix(tilePosition, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(tilescale.x, tilescale.y, 1)));
                }
            }
        }
    }

    void RandomizeEnemies()
    {
        EnemyAI[] enemiesInLevel = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (var enemy in enemiesInLevel)
        {
            if (Random.value < replaceEnemyChance)
            {
                Vector3 position = enemy.transform.position;
                Destroy(enemy.gameObject);
                if (enemies != null && enemies.Length > 0)
                {
                    int randomIndex = Random.Range(0, enemies.Length);
                    Instantiate(enemies[randomIndex], position, Quaternion.identity);
                }
            }
        }
    }

}
