using UnityEngine;
using UnityEngine.InputSystem;
using PowerupState = PowerStates.PowerupState;

/// <summary>
/// Owns all powerup state transitions: power-up, power-down, and runtime
/// state transfer to the new Mario prefab.
///
/// Character identity and prefab variants live in CharacterData — a single
/// ScriptableObject shared by all prefabs of the same character.
///
/// Power-down is dynamic: Fire → Big, Big → Small, Small → Tiny.
///
/// Writes: State.IsTransforming, State.InvincibilityTimeRemaining
/// </summary>
[RequireComponent(typeof(MarioCore))]
public class MarioPowerup : MonoBehaviour
{
    [Header("This Prefab's Powerup Identity")]
    [Tooltip("The PowerUpData asset that defines this prefab's powerup state and type.")]
    public PowerUpData Identity;

    [Header("Character")]
    [Tooltip("All character-specific prefab variants and shell. Shared across all prefabs of this character.")]
    public CharacterData Character;

    [Header("Sprite Library")]
    public UnityEngine.U2D.Animation.SpriteLibraryAsset NormalSpriteLibrary;

    [Tooltip("Persistent sprite skin (SMB, Pixelcraftian). Null = the NormalSpriteLibrary look. " +
             "Usually set per level (LevelPaletteSetup) and carried across transforms.")]
    public MarioSkin CurrentSkin;


    private MarioCore  _core;
    private MarioState State       => _core.State;
    private int        PlayerIndex => _core.PlayerIndex;

    // Convenience accessors for other scripts that need the raw values
    public PowerupState PowerupState       => Identity != null ? Identity.PowerupState : PowerStates.PowerupState.small;
    public string       CurrentPowerupType => Identity != null ? Identity.PowerupType  : "";

    private void Awake()
    {
        _core = GetComponent<MarioCore>();

        // Seed runtime state from the Identity ScriptableObject.
        // MarioState is created fresh in MarioCore.Awake() with defaults,
        // so MarioPowerup must initialize powerup identity here.
        if (Identity != null)
        {
            _core.State.PowerupState       = Identity.PowerupState;
            _core.State.CurrentPowerupType = Identity.PowerupType ?? "";
        }
        else
        {
            Debug.LogWarning($"[MarioPowerup] No Identity (PowerUpData) assigned on {gameObject.name}.");
        }

        ApplyPowerupAppearance(Identity);
    }

    // ─── Power Up ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by powerup pickups. Looks up the character-specific prefab
    /// from CharacterData so each character gets their own variant automatically.
    /// </summary>
    public void ChangePowerup(PowerUpData data)
    {
        if (State.IsTransforming) return;

        if (Character == null)
        {
            Debug.LogWarning($"[MarioPowerup] No CharacterData assigned on {gameObject.name}.");
            return;
        }

        // Any real powerup change morphs through the shell — including same-tier element
        // swaps like Fire -> Ice (blocking exact-same pickups is handled earlier in
        // PowerUp.canGetPowerup). Prefer an element-specific prefab if one is registered;
        // otherwise fall back to the base prefab for this tier and let the carried identity
        // + palette express the element. This is what lets Fire/Ice share the one Big body:
        // FindPrefab misses (no Fire/Ice prefab) -> GetPrefabForState(power) -> Big Mario.
        var newMarioPrefab = Character.FindPrefab(data)
                          ?? Character.GetPrefabForState(data.PowerupState);
        if (newMarioPrefab == null)
        {
            Debug.LogWarning($"[MarioPowerup] No prefab found in '{Character.CharacterName}' for powerup '{data?.name}'.");
            return;
        }

        State.IsTransforming     = true;
        State.CurrentPowerupType = data.PowerupType ?? "";

        var shell = SpawnShell(newMarioPrefab, data);
        if (shell == null) return;

        MarioEvents.FirePowerUpStarted(PlayerIndex);
        Destroy(gameObject);
    }

    // ─── Power Down ──────────────────────────────────────────────────────────

    /// <summary>
    /// Called by MarioCombat when Mario takes damage while not small.
    /// Dynamically finds the correct down-tier prefab:
    ///   power → big, big → small, small → tiny
    /// </summary>
    public void PowerDown()
    {
        Debug.Log($"[PowerDown] Called. PowerupState={State.PowerupState} Character={Character?.CharacterName}");
        
        State.InvincibilityTimeRemaining = _core.Physics.Config.DamageInvincibilityTime;

        if (Character == null)
        {
            Debug.LogWarning("[PowerDown] Character is null!");
            return;
        }

        var powerDownPrefab = Character.GetPowerDownPrefab(State.PowerupState);
        Debug.Log($"[PowerDown] powerDownPrefab={powerDownPrefab?.name ?? "NULL"}");
        
        if (powerDownPrefab == null)
        {
            Debug.LogWarning($"[PowerDown] No prefab found for state={State.PowerupState}");
            return;
        }

        State.IsTransforming = true;

        var shell = SpawnShell(powerDownPrefab);
        Debug.Log($"[PowerDown] shell={shell?.name ?? "NULL"}");

        if (shell == null)
        {
            State.IsTransforming = false;
            return;
        }

        _core.Audio?.PlayDamageSound();
        MarioEvents.FirePoweredDown(PlayerIndex);
        Destroy(gameObject);
    }

    /// <summary>
    /// Grants or revokes the projectile ability (FirePower) from the powerup data, so Fire/Ice
    /// are data on ONE Big prefab instead of separate prefabs. Null data, or data with no
    /// projectile, disables shooting (plain Big).
    /// </summary>
    private void ApplyAbility(PowerUpData data)
    {
        var shooter = GetComponentInChildren<FirePower>(true);
        if (shooter == null) return;
        shooter.Configure(data != null ? data.projectile      : null,
                          data != null ? data.projectileSound : null);
    }

    /// <summary>
    /// Applies every persistent part of the current powerup look together, preventing the
    /// sprite library, skin row, and element row from becoming desynchronized.
    /// </summary>
    private void ApplyPowerupAppearance(PowerUpData data)
    {
        ApplyAbility(data);
        ApplySpriteLibrary(data);
        ApplySkinRow();
        ApplyElement(data);
    }

    /// <summary>
    /// Overrides this Mario's powerup identity at runtime — used when a base-tier prefab
    /// is reused for an element expressed via palette (e.g. one "power" body for Fire and
    /// Ice). Reseeds the same state fields Awake() does.
    /// </summary>
    public void ApplyIdentity(PowerUpData data)
    {
        if (data == null) return;
        Identity                 = data;
        State.PowerupState       = data.PowerupState;
        State.CurrentPowerupType = data.PowerupType ?? "";
        ApplyPowerupAppearance(data);
    }

    // ─── State Transfer ──────────────────────────────────────────────────────

    /// <summary>
    /// Copies all runtime state from this Mario to the newly spawned Mario.
    /// Called by PlayerTransformation.SpawnNewMario after the animation completes.
    /// </summary>
    public void TransferToNewMario(MarioCore target)
    {
        var t = target.State;

        // Persistent skin. SetSpriteSkin also re-resolves the current Fire/Ice row through
        // that skin, regardless of whether targetIdentity was applied before or after transfer.
        var targetPowerup = target.GetComponent<MarioPowerup>();
        if (targetPowerup != null)
            targetPowerup.SetSpriteSkin(CurrentSkin);

        // Physics
        target.Rb.velocity = _core.Rb.velocity;

        // Parent (keeps moving-platform relationship)
        if (transform.parent != null)
            target.transform.SetParent(transform.parent, worldPositionStays: true);

        // Facing
        target.Physics.FlipTo(State.FacingRight);

        // Carry settings
        target.Carry.PressRunToGrab = _core.Carry.PressRunToGrab;
        target.Carry.CrouchToGrab   = _core.Carry.CrouchToGrab;
        target.Carry.CarryMethod    = _core.Carry.CarryMethod;

        // Identity
        t.PlayerIndex = State.PlayerIndex;

        // Damage invincibility
        t.InvincibilityTimeRemaining = State.InvincibilityTimeRemaining;

        // Star power
        if (State.StarPower)
            target.Combat.StartStarPower(State.StarPowerRemainingTime);

        // Input device
        if (TryGetComponent(out PlayerInput src) && target.TryGetComponent(out PlayerInput dst))
        {
            try   { dst.SwitchCurrentControlScheme(src.devices.ToArray()); }
            catch { Debug.Log("[MarioPowerup] Could not transfer input device — expected with one controller."); }
        }

        // Carried object
        _core.Carry.TransferCarryTo(target.Carry);

        // Mobile button state
        t.JumpPressed = State.JumpPressed;
        t.RunPressed  = State.RunPressed;
        t.MoveInput   = State.MoveInput;

        // Ability flags
        t.CanCrawl                     = State.CanCrawl;
        t.CanWallJump                  = State.CanWallJump;
        t.CanWallJumpWhenHoldingObject = State.CanWallJumpWhenHoldingObject;
        t.CanSpinJump                  = State.CanSpinJump;
        t.CanGroundPound               = State.CanGroundPound;
        t.CanMidairSpin                = State.CanMidairSpin;
        t.AllowMultipleMidairSpins     = State.AllowMultipleMidairSpins;
        t.CurrentClimbable             = State.CurrentClimbable;

        // Ground layer mask
        target.Physics.GroundLayer = _core.Physics.GroundLayer;
    }

    // ─── Sprite Library ──────────────────────────────────────────────────────

    public void ResetSpriteLibrary()
    {
        // "Reset" means back to what the CURRENT powerup + skin should look like.
        ApplySpriteLibrary(Identity);
        ApplySkinRow();
        ApplyElement(Identity);
    }

    /// <summary>
    /// Sets the swap rig's SpriteLibrary from powerup data: the powerup's overrideSpriteLibrary
    /// if it defines one, otherwise the character's NormalSpriteLibrary. Lets "same body, a few
    /// different frames" variants (Old Fire) live as data instead of a prefab.
    /// </summary>
    private void ApplySpriteLibrary(PowerUpData data)
    {
        var lib = GetComponentInChildren<UnityEngine.U2D.Animation.SpriteLibrary>();
        if (lib == null) return;

        UnityEngine.U2D.Animation.SpriteLibraryAsset asset = null;
        if (data != null && data.overrideSpriteLibrary != null)
            asset = data.overrideSpriteLibrary;                   // powerup-specific frames (OldFire)
        else if (CurrentSkin != null)
            asset = CurrentSkin.LibraryFor(State.PowerupState);   // persistent sprite skin (SMB/Pixel)
        if (asset == null)
            asset = NormalSpriteLibrary;                          // character default

        if (asset != null) lib.spriteLibraryAsset = asset;
    }

    /// <summary>
    /// Sets the persistent sprite skin and re-applies the library for the current size. Carried
    /// across transforms so SMB/Pixelcraftian survive size changes and power-downs.
    /// </summary>
    public void SetSpriteSkin(MarioSkin skin)
    {
        CurrentSkin = skin;
        ApplySpriteLibrary(Identity);
        ApplySkinRow();
        ApplyElement(Identity);
    }

    /// <summary>
    /// Pushes the current skin's palette row (its recolor for THIS size) onto MarioPalette's skin
    /// layer. No-op with no skin. Called wherever the library is applied, so color and shapes stay
    /// in lock-step across transforms. An active fire/ice element still shows over this row.
    /// </summary>
    private void ApplySkinRow()
    {
        if (_core.Palette == null || CurrentSkin == null) return;
        _core.Palette.SetSkin(CurrentSkin.RowFor(State.PowerupState));
    }

    /// <summary>
    /// Applies the element color (fire/ice) RESOLVED THROUGH THE SKIN, so NES-fire uses the NES
    /// fire row while Modern-fire uses the powerup's own row. data with PaletteRow &lt; 0 (a size
    /// powerup or power-down) clears the element so the skin shows through.
    /// </summary>
    public void ApplyElement(PowerUpData data)
    {
        if (_core.Palette == null) return;

        int row = -1;
        if (data != null && data.PaletteRow >= 0)
            row = CurrentSkin != null
                ? CurrentSkin.ElementRowFor(data.PowerupType, data.PaletteRow)
                : data.PaletteRow;

        _core.Palette.SetElement(row);
    }

    // ─── Internal ────────────────────────────────────────────────────────────

    private GameObject SpawnShell(GameObject newMarioPrefab, PowerUpData identity = null)
    {
        if (Character.TransformShellPrefab == null)
        {
            Debug.LogError($"[MarioPowerup] No TransformShellPrefab set on CharacterData '{Character.CharacterName}'.");
            return null;
        }

        var shell = Instantiate(Character.TransformShellPrefab, transform.position, Quaternion.identity);
        var pt    = shell.GetComponent<PlayerTransformation>();

        if (pt != null)
        {
            pt.oldPlayer = gameObject;
            pt.newPlayer      = newMarioPrefab;
            pt.targetIdentity = identity;   // carries element type + palette to the new Mario
            pt.StartTransformation();
        }
        else
        {
            Debug.LogError("[MarioPowerup] TransformShellPrefab is missing a PlayerTransformation component.");
        }

        return shell;
    }
}