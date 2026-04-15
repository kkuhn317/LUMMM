using UnityEngine;

/// <summary>
/// Identifies the surface material of a ground object for footstep audio.
/// Attach to any ground object or tiles, platforms, moving platforms, etc.
/// </summary>
public class SurfaceType : MonoBehaviour
{
    public SurfaceMaterial Material = SurfaceMaterial.Stone;
}
