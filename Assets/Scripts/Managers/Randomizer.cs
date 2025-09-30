using System.Collections;
using System.Collections.Generic;
using UnityEditor.U2D.Aseprite;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Randomizer : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase groundTile; // Assign this in the Inspector
    public float removeTileChance = 0.1f; // Chance to remove an existing tile
    public float addTileChance = 0.1f; // Chance to add a tile where there isn't one


    // Start is called before the first frame update
    void Start()
    {
        return; // TODO: Remove this and instead check for cheat code
        if (tilemap == null || groundTile == null)
        {
            Debug.LogWarning("Tilemap or GroundTile not assigned in the inspector. Will not randomize tiles.");
            return;
        }
        RandomizeTiles();
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
                }
            }
        }
    }

}
