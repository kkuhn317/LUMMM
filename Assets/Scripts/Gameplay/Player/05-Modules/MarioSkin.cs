using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;
using PowerupState = PowerStates.PowerupState;

/// <summary>
/// A skin — the whole look, both axes as data. Per size/state it can carry:
///   - a SPRITE library (different shapes, e.g. Pixelcraftian tiny), and
///   - a PALETTE row (recolor, e.g. pixelRow / nesRow / smbRow).
///
/// Either axis is optional per size: a color-only skin (NES/SMB) sets defaultRow and no
/// libraries; a shape-only skin sets libraries and leaves rows at -1; Pixelcraftian sets both
/// (a tiny library + a default recolor row for every size). The skin is carried across
/// transforms and the row + library for the CURRENT size are re-resolved on each morph — so one
/// skin asset gives the full look at every size, on one shared body set.
///
/// Precedence: a powerup's overrideSpriteLibrary beats the skin's library; the skin's row goes
/// to MarioPalette's skin layer, so an active fire/ice element still shows over it.
/// </summary>
[CreateAssetMenu(fileName = "MarioSkin", menuName = "Game/Mario Skin")]
public class MarioSkin : ScriptableObject
{
    public string skinName;

    [Tooltip("Palette row applied at EVERY size (the recolor). -1 = no recolor. Per-size entries " +
             "below can override it. Unmasked sheets ignore the row, so a baked-color size is safe.")]
    public int defaultRow = -1;

    [System.Serializable]
    public class Entry
    {
        public PowerupState state;
        [Tooltip("Sprite library for this size. Null = inherit the character's NormalSpriteLibrary.")]
        public SpriteLibraryAsset library;
        [Tooltip("Use a different palette row for this size instead of defaultRow.")]
        public bool overrideRow;
        public int row;
    }

    [Tooltip("Per-size overrides. Add an entry only for a size whose shapes and/or colors differ.")]
    public List<Entry> libraries = new();

    [System.Serializable]
    public class ElementRow
    {
        [Tooltip("Powerup type this recolors — must match PowerUpData.PowerupType (e.g. \"fire\", \"ice\").")]
        public string type;
        [Tooltip("Palette row for this element IN THIS SKIN (e.g. nes_fire). This is what makes NES-fire " +
                 "differ from Modern-fire.")]
        public int row;
    }

    [Tooltip("Skin-specific element colors. Without an entry for a type, that element uses the " +
             "powerup's own PaletteRow (the default/Modern color).")]
    public List<ElementRow> elementRows = new();

    /// <summary>Sprite library for a size, or null to inherit NormalSpriteLibrary.</summary>
    public SpriteLibraryAsset LibraryFor(PowerupState state)
    {
        foreach (var e in libraries)
            if (e.state == state && e.library != null) return e.library;
        return null;
    }

    /// <summary>Palette row for a size: the per-size override if set, else defaultRow.</summary>
    public int RowFor(PowerupState state)
    {
        foreach (var e in libraries)
            if (e.state == state && e.overrideRow) return e.row;
        return defaultRow;
    }

    /// <summary>
    /// The element color row for this skin (e.g. NES-fire), or the fallback (the powerup's own
    /// PaletteRow = default/Modern element color) when the skin doesn't recolor that element.
    /// </summary>
    public int ElementRowFor(string type, int fallback)
    {
        if (!string.IsNullOrWhiteSpace(type))
        {
            foreach (var e in elementRows)
            {
                if (string.Equals(
                    e.type?.Trim(),
                    type.Trim(),
                    System.StringComparison.OrdinalIgnoreCase))
                {
                    return e.row;
                }
            }
        }

        return fallback;
    }
}