using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// A tile that carries surface material data for footstep audio.
/// Use this instead of the default Tile for any ground tile that
/// needs a specific footstep sound.
///
/// Create via: Right-click → Create → Mario → Surface Tile
/// </summary>
[CreateAssetMenu(fileName = "SurfaceTile", menuName = "Mario/Surface Tile")]
public class SurfaceTile : Tile
{
    public SurfaceMaterial Material = SurfaceMaterial.Stone;
}
