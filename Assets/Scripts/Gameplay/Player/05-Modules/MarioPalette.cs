using UnityEngine;

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

    private static readonly int PaletteRowID = Shader.PropertyToID("_PaletteRow");
    private MaterialPropertyBlock _mpb;

    private float _restRow;    // current transformation row (persistent)
    private float _currentRow; // row actually applied right now (star frame OR rest)
    private bool  _starring;   // is a star override active?

    private void Awake()
    {
        if (visualRoot == null) visualRoot = transform;
        _restRow = normalRow;
        Apply(_restRow);
    }

    /// <summary>Persistent transformation color. Pass a row, or a negative value for normal.</summary>
    public void SetTransformation(float row)
    {
        _restRow = row;
        if (!_starring) Apply(_restRow);   // don't stomp an in-progress star flash
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
        Apply(_restRow);
    }

    /// <summary>The palette row currently applied (star frame while flashing, else rest).</summary>
    public float CurrentRow => _currentRow;

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