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

        var newMarioPrefab = Character.FindPrefab(data);
        if (newMarioPrefab == null)
        {
            Debug.LogWarning($"[MarioPowerup] No prefab found in '{Character.CharacterName}' for powerup '{data?.name}'.");
            return;
        }

        State.IsTransforming     = true;
        State.CurrentPowerupType = data.PowerupType ?? "";

        var shell = SpawnShell(newMarioPrefab);
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

        var shell = SpawnShell(powerDownPrefab);
        Debug.Log($"[PowerDown] shell={shell?.name ?? "NULL"}");
        
        if (shell == null) return;

        _core.Audio?.PlayDamageSound();
        MarioEvents.FirePoweredDown(PlayerIndex);
        Destroy(gameObject);
    }

    // ─── State Transfer ──────────────────────────────────────────────────────

    /// <summary>
    /// Copies all runtime state from this Mario to the newly spawned Mario.
    /// Called by PlayerTransformation.SpawnNewMario after the animation completes.
    /// </summary>
    public void TransferToNewMario(MarioCore target)
    {
        var t = target.State;

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
        // SpriteLibrary lives inside the Visual child hierarchy, not on the root
        var lib = GetComponentInChildren<UnityEngine.U2D.Animation.SpriteLibrary>();
        if (lib != null && NormalSpriteLibrary != null)
            lib.spriteLibraryAsset = NormalSpriteLibrary;
    }

    // ─── Internal ────────────────────────────────────────────────────────────

    private GameObject SpawnShell(GameObject newMarioPrefab)
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
            pt.newPlayer = newMarioPrefab;
            pt.StartTransformation();
        }
        else
        {
            Debug.LogError("[MarioPowerup] TransformShellPrefab is missing a PlayerTransformation component.");
        }

        return shell;
    }
}