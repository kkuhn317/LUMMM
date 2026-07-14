using UnityEngine;
using UnityEngine.U2D.Animation;

/// <summary>
/// Single owner of the sprite's _PaletteRow. Two independent callers compose here:
///   - MarioPowerup sets the resting transformation (normal/fire/ice) — PERSISTENT.
///   - MarioCombat drives the star flash — TEMPORARY, overrides the rest row, then releases.
///
/// Priority: while a star is active it wins; when it clears, we fall back to whatever
/// transformation is current (NOT to normal). Driven per-renderer via a
/// MaterialPropertyBlock, scoped to visualRoot, so it never touches the shared material.
/// </summary>
public class MarioPalette : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [Tooltip("Row for the un-powered look. -1 = bypass (show the sprite's own colors).")]
    [SerializeField] private float normalRow = -1f;

    [Tooltip("Configured PaletteSwapMasked material. On Awake it is assigned to every body\n" +
             "renderer under visualRoot (those with a SpriteResolver), so the limb rig recolors\n" +
             "like SpriteSimple. Non-body markers (e.g. Checkpoint Icon) are skipped. Null = leave\n" +
             "existing materials as-is.")]
    [SerializeField] private Material paletteMaterial;

    private static readonly int PaletteRowID = Shader.PropertyToID("_PaletteRow");
    private MaterialPropertyBlock _mpb;

    private float _skinRow;    // persistent skin (NES/SMB/Modern) — survives power-ups & transforms
    private float _elementRow; // current element (fire/ice); -1 = none, so the skin shows through
    private float _currentRow; // row actually applied right now (star frame OR rest)
    private bool  _starring;   // is a star override active?

    private void Awake()
    {
        if (visualRoot == null) visualRoot = transform;
        AssignMaterial();
        _skinRow    = normalRow;
        _elementRow = -1f;
        Apply(RestRow);
    }

    /// <summary>
    /// Persistent skin row (NES/SMB/Modern). Survives power-ups and transformations — the
    /// element shows OVER it and the skin returns when the element clears. Set by
    /// LevelPaletteSetup and carried across transforms.
    /// </summary>
    public void SetSkin(float row)
    {
        _skinRow = row;
        if (!_starring) Apply(RestRow);
    }

    /// <summary>
    /// Current element row (fire/ice). Pass -1 to clear it and fall back to the skin. Set by the
    /// powerup path; a size-only change passes -1 so the skin shows through unchanged.
    /// </summary>
    public void SetElement(float row)
    {
        _elementRow = row;
        if (!_starring) Apply(RestRow);
    }

    /// <summary>One frame of the star flash (temporary override of the rest row).</summary>
    public void SetStarFrame(float row)
    {
        _starring = true;
        Apply(row);
    }

    /// <summary>Star ended — return to the current transformation, whatever it is.</summary>
    public void ClearStar()
    {
        _starring = false;
        Apply(RestRow);
    }

    /// <summary>The palette row currently applied (star frame while flashing, else rest).</summary>
    public float CurrentRow => _currentRow;

    /// <summary>The resting look when not flashing: the element if one is active, else the skin.</summary>
    public float RestRow => _elementRow >= 0f ? _elementRow : _skinRow;

    /// <summary>The persistent skin row on its own — for carrying it across transformations.</summary>
    public float SkinRow => _skinRow;

    /// <summary>The configured PaletteSwapMasked material, for objects that must match this Mario.</summary>
    public Material PaletteMaterial => paletteMaterial;

    /// <summary>
    /// Re-scans the player hierarchy, assigns the palette material, and reapplies the current row.
    /// Call this after a cutscene adds, enables, or repurposes a SpriteResolver at runtime.
    /// </summary>
    public void RefreshRenderers()
    {
        AssignMaterial();
        Apply(_currentRow);
    }

    /// <summary>
    /// Pushes paletteMaterial onto every body renderer under visualRoot. A "body" renderer
    /// is one with a SpriteResolver (SpriteSimple and every Limb_* have one); markers like
    /// Checkpoint Icon have no resolver and are left untouched. This is why the limbs weren't
    /// recoloring: they were on the default Sprites material, not this one.
    /// </summary>
    private void AssignMaterial()
    {
        if (paletteMaterial == null) return;
        foreach (var r in visualRoot.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (r == null) continue;
            if (r.GetComponent<SpriteResolver>() == null) continue; // skip non-body renderers
            r.sharedMaterial = paletteMaterial;
        }
    }

    private void Apply(float row)
    {
        _currentRow = row;
        _mpb ??= new MaterialPropertyBlock();
        foreach (var r in visualRoot.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetFloat(PaletteRowID, row);
            r.SetPropertyBlock(_mpb);
        }
    }
}