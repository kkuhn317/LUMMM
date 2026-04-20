using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.InputSystem;
using System.Collections;
using PowerupState = PowerStates.PowerupState;

/// <summary>
/// Drives the powerup transformation animation and then spawns the new Mario prefab.
///
/// Lives on the "transform shell" prefab. Spawned by MarioPowerup.SpawnShell().
/// oldPlayer is destroyed by MarioPowerup immediately after StartTransformation()
/// returns, so all data needed from it must be cached here before Invoke fires.
///
/// All state is cached as plain value types — no MarioCore reference is kept
/// since Unity resets private MonoBehaviour fields between frames.
/// </summary>
public class PlayerTransformation : MonoBehaviour
{
    [Header("Set in Inspector (on this prefab)")]
    public GameObject oldChild;
    public GameObject newChild;

    [Header("Set by MarioPowerup before StartTransformation()")]
    public GameObject oldPlayer;
    public GameObject newPlayer;

    // ─── Powerup state ────────────────────────────────────────────────────────
    private PowerupState _oldPowerupState;
    private PowerupState _newPowerupState;

    // ─── Transform ────────────────────────────────────────────────────────────
    private float   _cachedOldFeetY;
    private Vector3 _cachedSpawnPosition;
    private bool    _cachedOnGround;
    private bool    _cachedFacingRight;
    private Transform _cachedParent;

    // ─── All runtime state cached as plain values ─────────────────────────────
    private float       _cachedInvincibilityTime;
    private Vector2     _cachedVelocity;
    private bool        _cachedStarPower;
    private float       _cachedStarPowerTime;
    private bool        _cachedPressRunToGrab;
    private bool        _cachedCrouchToGrab;
    private CarryMethod _cachedCarryMethod;
    private bool        _cachedJumpPressed;
    private bool        _cachedRunPressed;
    private Vector2     _cachedMoveInput;
    private bool        _cachedCanWallJump;
    private bool        _cachedCanWallJumpHolding;
    private bool        _cachedCanSpinJump;
    private bool        _cachedCanGroundPound;
    private bool        _cachedCanMidairSpin;
    private bool        _cachedAllowMultipleMidairSpins;
    private Climbable   _cachedCurrentClimbable;
    private LayerMask   _cachedGroundLayer;
    private PlayerInput _cachedPlayerInput;
    private GameObject  _cachedCarriedObject;

    // ─── Registry ─────────────────────────────────────────────────────────────
    private PlayerRegistry _registry;
    private int            _cachedPlayerIndex = -1;

    public void StartTransformation()
    {
        _cachedSpawnPosition = transform.position;

        if (oldPlayer == null || newPlayer == null)
        {
            Debug.LogError("[PT] oldPlayer or newPlayer is null!");
            return;
        }

        var oldCore = oldPlayer.GetComponent<MarioCore>();
        var newCore = newPlayer.GetComponent<MarioCore>();

        Debug.Log($"[PT] oldCore={oldCore} newCore={newCore} oldPlayer={oldPlayer.name} newPlayer={newPlayer.name}");

        if (oldCore == null || newCore == null)
        {
            Debug.LogError($"[PT] Missing MarioCore! oldCore={oldCore} newCore={newCore}");
            return;
        }

        // ── Cache feet Y ──────────────────────────────────────────────────────
        var oldCol = oldPlayer.GetComponent<Collider2D>();
        _cachedOldFeetY = oldCol != null ? oldCol.bounds.min.y : oldPlayer.transform.position.y;

        // ── Cache all runtime state as plain values ───────────────────────────
        var s = oldCore.State;
        _cachedVelocity                 = oldCore.Rb.velocity;
        _cachedFacingRight              = s.FacingRight;
        _cachedOnGround                 = s.OnGround;
        _cachedInvincibilityTime        = s.InvincibilityTimeRemaining;
        _cachedStarPower                = s.StarPower;
        _cachedStarPowerTime            = s.StarPowerRemainingTime;
        _cachedJumpPressed              = s.JumpPressed;
        _cachedRunPressed               = s.RunPressed;
        _cachedMoveInput                = s.MoveInput;
        _cachedCanWallJump              = s.CanWallJump;
        _cachedCanWallJumpHolding       = s.CanWallJumpWhenHoldingObject;
        _cachedCanSpinJump              = s.CanSpinJump;
        _cachedCanGroundPound           = s.CanGroundPound;
        _cachedCanMidairSpin            = s.CanMidairSpin;
        _cachedAllowMultipleMidairSpins = s.AllowMultipleMidairSpins;
        _cachedCurrentClimbable         = s.CurrentClimbable;
        _cachedParent                   = oldPlayer.transform.parent;
        _cachedGroundLayer              = oldCore.Physics.GroundLayer;
        _cachedPressRunToGrab           = oldCore.Carry.PressRunToGrab;
        _cachedCrouchToGrab             = oldCore.Carry.CrouchToGrab;
        _cachedCarryMethod              = oldCore.Carry.CarryMethod;
        _cachedPlayerInput              = oldPlayer.GetComponent<PlayerInput>();
        _cachedPlayerIndex              = oldCore.PlayerIndex;

        // Cache carried object — detach it from old Mario so it survives destruction
        if (s.Carrying && oldCore.Carry.HeldObjectPosition.transform.childCount > 0)
        {
            _cachedCarriedObject = oldCore.Carry.HeldObjectPosition.transform.GetChild(0).gameObject;
            _cachedCarriedObject.transform.SetParent(null);
        }

        // ── Register shell as active player ───────────────────────────────────
        if (_registry == null)
        {
            _registry = GameManager.Instance != null
                ? GameManager.Instance.GetSystem<PlayerRegistry>()
                : FindObjectOfType<PlayerRegistry>(true);
        }

        var selfCore = GetComponent<MarioCore>();
        if (selfCore != null)
            _registry?.RegisterPlayer(selfCore, _cachedPlayerIndex);

        // ── Read powerup identity ─────────────────────────────────────────────
        var oldPowerup = oldPlayer.GetComponent<MarioPowerup>();
        var newPowerup = newPlayer.GetComponent<MarioPowerup>();

        _oldPowerupState = oldPowerup?.PowerupState ?? s.PowerupState;
        _newPowerupState = newPowerup?.PowerupState ?? newCore.State.PowerupState;

        // ── Set sprite libraries on animation children ────────────────────────
        var oldLib = oldPlayer.GetComponentInChildren<SpriteLibrary>();
        var newLib = newPlayer.GetComponentInChildren<SpriteLibrary>();

        var oldChildLib = oldChild.GetComponent<SpriteLibrary>();
        var newChildLib = newChild.GetComponent<SpriteLibrary>();
        if (oldLib != null && oldChildLib != null) oldChildLib.spriteLibraryAsset = oldLib.spriteLibraryAsset;
        if (newLib != null && newChildLib != null) newChildLib.spriteLibraryAsset = newLib.spriteLibraryAsset;

        // ── Sync oldChild to the exact sprite the player was showing ─────────
        // Find SpriteSimple by name — it's the single-sprite node that matches
        // oldChild's structure. We search all children and match by name to avoid
        // hardcoding a path that may differ between prefab variants.
        var shellSR = oldChild.GetComponent<SpriteRenderer>();
        if (shellSR != null)
        {
            SpriteRenderer playerSR = null;
            foreach (var sr in oldPlayer.GetComponentsInChildren<SpriteRenderer>())
            {
                if (sr.gameObject.name == "SpriteSimple")
                {
                    playerSR = sr;
                    break;
                }
            }
            if (playerSR != null)
                shellSR.sprite = playerSR.sprite;
            else
                Debug.LogWarning("[PT] Could not find SpriteSimple on old player.");
        }

        oldChild.transform.localScale = oldPlayer.transform.localScale;
        newChild.transform.localScale = newPlayer.transform.localScale;

        // ── Flip shell and children to match old player facing ────────────────
        if (!s.FacingRight)
        {
            var scale = transform.localScale;
            transform.localScale = new Vector3(-scale.x, scale.y, scale.z);
            oldChild.GetComponent<SpriteRenderer>().flipX = true;
            newChild.GetComponent<SpriteRenderer>().flipX = true;
        }

        // ── Play the correct transition animation ─────────────────────────────
        var animator = GetComponent<Animator>();

        bool wasBig   = PowerStates.IsBig(_oldPowerupState);
        bool wasSmall = _oldPowerupState == PowerupState.small;
        bool wasTiny  = _oldPowerupState == PowerupState.tiny;
        bool isBig    = PowerStates.IsBig(_newPowerupState);
        bool isSmall  = _newPowerupState == PowerupState.small;
        bool isTiny   = _newPowerupState == PowerupState.tiny;

        if      (wasBig   && isTiny)              animator.Play("BigToTiny");
        else if (wasTiny  && isBig)               animator.Play("TinyToBig");
        else if (wasBig   && isSmall)             animator.Play("BigToSmall");
        else if (wasSmall && isBig)               animator.Play("SmallToBig");
        else if ((wasSmall || wasTiny) && isTiny) animator.Play("SmallToTiny");
        else if (wasBig   && isBig)               animator.Play("BigToBig");
        else                                       animator.Play("SmallToBig");

        Invoke(nameof(SpawnNewMario), 1f);
    }

    private void SpawnNewMario()
    {
        if (newPlayer == null)
        {
            Debug.LogError("[PT] newPlayer is null in SpawnNewMario!");
            Destroy(gameObject);
            return;
        }

        GameObject newMario = Instantiate(newPlayer, _cachedSpawnPosition, Quaternion.identity);

        // ── Align feet ────────────────────────────────────────────────────────
        var newCol = newMario.GetComponent<Collider2D>();
        if (newCol != null)
        {
            float deltaY = _cachedOldFeetY - newCol.bounds.min.y;
            newMario.transform.position += new Vector3(0f, deltaY, 0f);
        }

        var newCore = newMario.GetComponent<MarioCore>();
        if (newCore == null)
        {
            Debug.LogError("[PT] New Mario has no MarioCore!");
            Destroy(gameObject);
            return;
        }

        // Block damage immediately — enemy colliders may already be overlapping
        newCore.State.InvincibilityTimeRemaining = _cachedInvincibilityTime;
        newCore.State.IsTransforming             = true;

        // ── Apply all cached state directly ───────────────────────────────────
        var t = newCore.State;

        // Transfer velocity so physics deceleration continues naturally after
        // transformation — grounded drag will handle stopping if no input is held.
        newCore.Rb.velocity = _cachedVelocity;
        t.PlayerIndex                        = _cachedPlayerIndex;
        t.JumpPressed                        = _cachedJumpPressed;
        t.RunPressed                         = _cachedRunPressed;
        // MoveInput is intentionally not transferred — the cached value is 1 second
        // old and causes phantom movement. MarioInput's Move callback will set it
        // correctly on the next input event. SyncHeldButtons reads current physical
        // state for buttons (Run, Jump) but Move is event-driven so we leave it as
        // the default zero from the fresh prefab.
        newMario.GetComponent<MarioInput>()?.SyncHeldButtons();
        t.CanWallJump                        = _cachedCanWallJump;
        t.CanWallJumpWhenHoldingObject       = _cachedCanWallJumpHolding;
        t.CanSpinJump                        = _cachedCanSpinJump;
        t.CanGroundPound                     = _cachedCanGroundPound;
        t.CanMidairSpin                      = _cachedCanMidairSpin;
        t.AllowMultipleMidairSpins           = _cachedAllowMultipleMidairSpins;
        // CanCrawl is derived from the new Mario's powerup state, not transferred
        // from the old Mario — big Mario sets it false and would poison small Mario.
        var newPowerup = newMario.GetComponent<MarioPowerup>();
        t.CanCrawl = newPowerup != null
            ? PowerStates.IsSmall(newPowerup.PowerupState)
            : PowerStates.IsSmall(t.PowerupState);
        t.CurrentClimbable                   = _cachedCurrentClimbable;
        newCore.Carry.PressRunToGrab         = _cachedPressRunToGrab;
        newCore.Carry.CrouchToGrab           = _cachedCrouchToGrab;
        newCore.Carry.CarryMethod            = _cachedCarryMethod;
        newCore.Physics.GroundLayer          = _cachedGroundLayer;

        if (_cachedParent != null)
            newMario.transform.SetParent(_cachedParent, worldPositionStays: true);

        if (_cachedStarPower)
            newCore.Combat.StartStarPower(_cachedStarPowerTime);

        // Re-attach carried object to new Mario
        if (_cachedCarriedObject != null)
        {
            _cachedCarriedObject.transform.SetParent(newCore.Carry.HeldObjectPosition.transform);
            _cachedCarriedObject.transform.localPosition = Vector3.zero;
            newCore.State.Carrying = true;
        }

        if (_cachedPlayerInput != null && newMario.TryGetComponent(out PlayerInput dst))
        {
            try { dst.SwitchCurrentControlScheme(_cachedPlayerInput.devices.ToArray()); }
            catch { Debug.Log("[PT] Could not transfer input device."); }
        }

        // ── Register and transition ───────────────────────────────────────────
        if (_registry != null && _cachedPlayerIndex >= 0)
            _registry.RegisterPlayer(newCore, _cachedPlayerIndex);

        newCore.StartCoroutine(ForceStateNextFrame(newCore));

        Destroy(gameObject);
    }

    private IEnumerator ForceStateNextFrame(MarioCore target)
    {
        yield return null;
        if (target == null) yield break;

        target.State.IsTransforming = false; // safe to take damage now
        target.StateMachine.ForceTransition(
            _cachedOnGround ? MarioStateID.Idle : MarioStateID.Fall);

        target.Physics.FlipTo(_cachedFacingRight);
    }
}