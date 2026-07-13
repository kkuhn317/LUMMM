using UnityEngine;

/// <summary>
/// Sets the appearance every player starts with in this level. A MarioSkin can define both the
/// sprite library and palette row for each powerup size; startingPaletteRow remains available as
/// a color-only fallback when no MarioSkin is assigned.
///
/// Because this component hooks GameEvents.OnPlayerRegistered, it applies the starting appearance
/// whenever a player registers, including the initial spawn and checkpoint respawns. It also sweeps
/// players that registered before this component subscribed, so scene-placed and runtime-spawned
/// players are both handled.
///
/// Composition with the rest of the appearance system is automatic: star frames temporarily
/// override the resting palette, size-only transformations preserve the active skin, and Fire/Ice
/// transformations use the element row defined by that skin. When the element clears, the skin's
/// palette row and sprite library remain active for the current size.
///
/// Extension points: ResolveStartingSkin() and ResolveStartingRow() are the only places where the
/// level's starting appearance is selected. A character-selection or save-profile system can be
/// given priority there later without changing MarioPalette, MarioPowerup, or the star system.
/// </summary>
public class LevelPaletteSetup : MonoBehaviour
{
    [Tooltip("Palette row players start this level with. -1 = normal (no skin). Set to the NES " +
             "row (or any skin row) on levels that should start with that look.")]
    [SerializeField] private int startingPaletteRow = -1;

    [Tooltip("Sprite skin players start this level with (SMB, Pixelcraftian). Null = normal art. " +
             "This is the different-sprites axis; startingPaletteRow above is the recolor axis.")]
    [SerializeField] private MarioSkin startingSpriteSkin;

    private void OnEnable()  => GameEvents.OnPlayerRegistered += OnPlayerRegistered;
    private void OnDisable() => GameEvents.OnPlayerRegistered -= OnPlayerRegistered;

    private void Start()
    {
        // Catch players that registered before we subscribed (scene-placed players).
        var registry = GameManager.Instance != null
            ? GameManager.Instance.GetSystem<PlayerRegistry>()
            : FindObjectOfType<PlayerRegistry>(true);
        if (registry == null) return;

        foreach (var player in registry.GetAllPlayers())
            Apply(player);
    }

    private void OnPlayerRegistered(MarioCore player, int playerIndex) => Apply(player);

    private void Apply(MarioCore player)
    {
        if (player == null) return;

        // A MarioSkin now carries BOTH sprites and color (row per size), so if one is set it drives
        // the whole look. Fall back to the raw startingPaletteRow only when there's no skin asset.
        var skin = ResolveStartingSkin(player);
        if (skin != null)
            player.Powerup?.SetSpriteSkin(skin);
        else
            player.Palette?.SetSkin(ResolveStartingRow(player));
    }

    /// <summary>
    /// The one place the starting rest row is decided. Priority, for when more sources exist:
    ///   1. character-select / save-profile skin  (NOT wired yet)
    ///   2. this level's startingPaletteRow        (current)
    /// </summary>
    private int ResolveStartingRow(MarioCore player)
    {
        // TODO(character select): when a profile/selection skin exists, return it here first —
        // the level value below becomes the fallback for levels that don't force a skin. e.g.
        //   if (PlayerProfile.HasSkin) return PlayerProfile.SkinRow;
        return startingPaletteRow;
    }

    private MarioSkin ResolveStartingSkin(MarioCore player)
    {
        // TODO(character select): a profile/selection sprite skin would take priority here.
        return startingSpriteSkin;
    }
}