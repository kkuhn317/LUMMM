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

    // Powerup identity applied to the new Mario after the morph (carries element type +
    // palette row). Null = keep the spawned prefab's own identity (used for power-downs).
    public PowerUpData targetIdentity;

    // ─── Shell palette (keeps element / star flash alive during the morph) ─────
    private float _cachedOldRow;        // old Mario's REST row (skin or element) -> oldChild
    private float _cachedSkinRow;       // old Mario's persistent palette skin -> carried to the new body
    private MarioSkin _cachedSpriteSkin; // old Mario's persistent SPRITE skin -> carried to the new body
    private int   _cachedStarRowStart;  // star block, cached from old Mario's MarioCombat
    private int   _cachedStarRowCount;
    private int   _shellStarFrame;
    private MaterialPropertyBlock _shellMpb;
    private static readonly int PaletteRowID = Shader.PropertyToID("_PaletteRow");

    // ─── Powerup state ────────────────────────────────────────────────────────
    private PowerupState _oldPowerupState;
    private PowerupState _newPowerupState;

    // ─── Transform ────────────────────────────────────────────────────────────
    private float     _cachedOldFeetY;
    private float     _cachedOldPivotToFeetOffset; // signed distance from pivot to feet, always <= 0
    private Vector3   _cachedSpawnPosition;
    private bool      _cachedOnGround;
    private bool      _cachedFacingRight;
    private Transform _cachedParent;

    // ─── All runtime state cached as plain values ─────────────────────────────
    private float       _cachedInvincibilityTime;
    private Vector2     _cachedVelocity;
    private bool        _cachedStarPower;
    private float       _cachedStarPowerTime;
    private GameObject  _cachedStarMusicInstance;
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
        // Use MarioCore.Collider / child collider, not GetComponent on the root.
        // The root can have no collider or a different helper collider, which causes
        // small → big swaps to spawn lower/higher than the original player.
        var oldCol = GetPrimaryBodyCollider(oldCore, oldPlayer);
        _cachedOldFeetY             = oldCol != null ? oldCol.bounds.min.y : oldPlayer.transform.position.y;
        _cachedOldPivotToFeetOffset = _cachedOldFeetY - _cachedSpawnPosition.y;

        // ── Cache all runtime state as plain values ───────────────────────────
        var s = oldCore.State;
        _cachedVelocity                 = oldCore.Rb.velocity;
        _cachedFacingRight              = s.FacingRight;
        _cachedOnGround                 = s.OnGround;
        _cachedInvincibilityTime        = s.InvincibilityTimeRemaining;
        _cachedStarPower                = s.StarPower;
        _cachedStarPowerTime            = s.StarPowerRemainingTime;

        // Transfer ownership of the LIVE star-music object before old Mario is destroyed.
        // MusicManager remains registered to this same object throughout the shell animation,
        // so the star theme never mutes, restarts, or loses its playback position.
        if (_cachedStarPower && oldCore.Combat != null)
            _cachedStarMusicInstance = oldCore.Combat.DetachStarMusicForTransformation();

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

        // Cache carried object — detach it from old Mario so it survives destruction.
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
        _newPowerupState = targetIdentity != null
            ? targetIdentity.PowerupState
            : newPowerup?.PowerupState ?? newCore.State.PowerupState;

        _cachedOldRow       = oldCore.Palette != null ? oldCore.Palette.RestRow : -1f;
        _cachedSkinRow      = oldCore.Palette != null ? oldCore.Palette.SkinRow : -1f;
        _cachedSpriteSkin   = oldPowerup != null ? oldPowerup.CurrentSkin : null;
        _cachedStarRowStart = oldCore.Combat != null ? oldCore.Combat.StarRowStart : 0;
        _cachedStarRowCount = oldCore.Combat != null ? oldCore.Combat.StarRowCount : 1;

        // ── Set sprite libraries on animation children ────────────────────────
        var oldLib = oldPlayer.GetComponentInChildren<SpriteLibrary>();
        var newLib = newPlayer.GetComponentInChildren<SpriteLibrary>();

        var oldChildLib = oldChild.GetComponent<SpriteLibrary>();
        var newChildLib = newChild.GetComponent<SpriteLibrary>();

        if (oldLib != null && oldChildLib != null)
        {
            oldChildLib.spriteLibraryAsset = oldLib.spriteLibraryAsset;
            oldChild.GetComponent<SpriteResolver>()?.ResolveSpriteToSpriteRenderer();
        }

        if (newChildLib != null)
        {
            // Resolve the TARGET side before the morph begins. The referenced newPlayer is
            // a prefab asset, so its SpriteLibrary still contains its default look at this point.
            // Use the same precedence as MarioPowerup.ApplySpriteLibrary:
            // powerup override > persistent skin library for target size > prefab default.
            SpriteLibraryAsset targetLibrary = null;

            if (targetIdentity != null && targetIdentity.overrideSpriteLibrary != null)
                targetLibrary = targetIdentity.overrideSpriteLibrary;
            else if (_cachedSpriteSkin != null)
                targetLibrary = _cachedSpriteSkin.LibraryFor(_newPowerupState);

            if (targetLibrary == null && newLib != null)
                targetLibrary = newLib.spriteLibraryAsset;

            if (targetLibrary != null)
            {
                newChildLib.spriteLibraryAsset = targetLibrary;
                newChild.GetComponent<SpriteResolver>()?.ResolveSpriteToSpriteRenderer();
            }
        }

        // ── Sync oldChild to the exact sprite the player was showing ─────────
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
            {
                shellSR.sprite = playerSR.sprite;

                // Inherit the palette material so the mask (and thus the element + star)
                // renders during the morph. Same character = same PaletteSwapMasked material
                // across sizes, so one material serves both children; each child's per-sprite
                // mask travels with its own secondary texture. Without this the children keep
                // Sprites-Default and SetChildRow's _PaletteRow write is silently ignored.
                if (playerSR.sharedMaterial != null)
                {
                    shellSR.sharedMaterial = playerSR.sharedMaterial;
                    var newSR = newChild.GetComponent<SpriteRenderer>();
                    if (newSR != null) newSR.sharedMaterial = playerSR.sharedMaterial;
                }
            }
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

        if      (wasBig && isTiny)                 animator.Play("BigToTiny");
        else if (wasTiny && isBig)                 animator.Play("TinyToBig");
        else if (wasBig && isSmall)                animator.Play("BigToSmall");
        else if (wasSmall && isBig)                animator.Play("SmallToBig");
        else if ((wasSmall || wasTiny) && isTiny)  animator.Play("SmallToTiny");
        else if (wasBig && isBig)                  animator.Play("BigToBig");
        else                                       animator.Play("SmallToBig");

        // Keep the palette alive through the morph. The shell children use the
        // PaletteSwapMasked material; drive _PaletteRow so the element (or, if starred,
        // the star flash) doesn't drop out during the 1s transition.
        if (_cachedStarPower)
        {
            _shellStarFrame = 0;
            InvokeRepeating(nameof(CycleShellStar), 0f, 0.1f);
        }
        else
        {
            SetChildRow(oldChild, _cachedOldRow);
            SetChildRow(newChild, ResolveTargetRestRow());
        }

        Invoke(nameof(SpawnNewMario), 1f);
    }

    private void SpawnNewMario()
    {
        CancelInvoke(nameof(CycleShellStar));
        if (newPlayer == null)
        {
            Debug.LogError("[PT] newPlayer is null in SpawnNewMario!");
            Destroy(gameObject);
            return;
        }

        // Seed the Y so new Mario's feet start near _cachedOldFeetY before correction.
        // Using the old pivot→feet offset as the seed minimises the post-sync correction
        // and guarantees it is always upward (never the negative delta that caused the teleport).
        float seedY      = _cachedOldFeetY - _cachedOldPivotToFeetOffset;
        var   seedPos    = new Vector3(_cachedSpawnPosition.x, seedY, _cachedSpawnPosition.z);
        GameObject newMario = Instantiate(newPlayer, seedPos, Quaternion.identity);

        var newCore = newMario.GetComponent<MarioCore>();
        if (newCore == null)
        {
            Debug.LogError("[PT] New Mario has no MarioCore!");
            Destroy(gameObject);
            return;
        }

        // ── Align feet ────────────────────────────────────────────────────────
        // The downward teleport happens because new Mario is instantiated at the old
        // Mario's pivot, but big Mario's body collider lives on a child object whose
        // localPosition.y differs from small Mario's. This makes newCol.bounds.min.y
        // land higher than _cachedOldFeetY, so deltaY goes negative → teleports down.
        //
        // Fix: seed the spawn Y using the old Mario's known pivot→feet offset so the
        // new Mario starts close to the right place. After SyncTransforms, measure the
        // true remaining error and correct it. Because we start close, the correction
        // is always small and always positive (upward) — the negative delta is impossible.
        //
        // Block overlaps (growing under a block) are left to Rigidbody depenetration,
        // which handles them correctly over 1-2 frames without shoving Mario sideways.
        // The only intervention needed is cancelling any cached upward velocity so Mario
        // doesn't rocket into the ceiling on frame one.
        var newCol = GetPrimaryBodyCollider(newCore, newMario);
        if (newCol != null)
        {
            Physics2D.SyncTransforms();

            float remainingError = _cachedOldFeetY - newCol.bounds.min.y;
            if (!Mathf.Approximately(remainingError, 0f))
            {
                newMario.transform.position += new Vector3(0f, remainingError, 0f);
                Physics2D.SyncTransforms();
            }

            if (newCore.Rb != null && newCore.Rb.velocity.y > 0f)
                newCore.Rb.velocity = new Vector2(newCore.Rb.velocity.x, 0f);
        }

        // Block damage immediately — enemy colliders may already be overlapping.
        newCore.State.InvincibilityTimeRemaining = _cachedInvincibilityTime;
        newCore.State.IsTransforming             = true;

        // ── Apply all non-input state ─────────────────────────────────────────
        var t = newCore.State;

        newCore.Rb.velocity                  = _cachedVelocity;
        t.PlayerIndex                        = _cachedPlayerIndex;
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

        t.CurrentClimbable           = _cachedCurrentClimbable;
        newCore.Carry.PressRunToGrab = _cachedPressRunToGrab;
        newCore.Carry.CrouchToGrab   = _cachedCrouchToGrab;
        newCore.Carry.CarryMethod    = _cachedCarryMethod;
        newCore.Physics.GroundLayer  = _cachedGroundLayer;

        if (_cachedParent != null)
            newMario.transform.SetParent(_cachedParent, worldPositionStays: true);

        // Carry the SKIN across the transform (NES/SMB persists through size changes and
        // power-downs) and set the ELEMENT from the powerup: a size-only change or a power-down
        // passes -1, clearing the element so the skin shows through again; fire/ice pass their
        // row, which shows over the skin. Star, if carried below, overrides and ClearStar
        // returns to whichever the current rest is.
        // Carry the persistent SPRITE skin (SMB/Pixelcraftian) onto the new body BEFORE identity
        // is applied, so ApplySpriteLibrary picks the skin's library for the NEW size.
        if (newCore.Powerup != null) newCore.Powerup.CurrentSkin = _cachedSpriteSkin;

        if (targetIdentity != null)
            newCore.Powerup?.ApplyIdentity(targetIdentity);   // sets state + applies skin/override library
        else
            newCore.Powerup?.ResetSpriteLibrary();            // power-down: apply skin library for the body's own state

        // With a sprite skin, ApplyIdentity/ResetSpriteLibrary above already set the skin row for
        // the NEW size (skin.RowFor). Only carry the raw cached row when there's NO skin asset
        // (a quick color-only NES/SMB row set via startingPaletteRow) — else we'd stomp RowFor.
        if (_cachedSpriteSkin == null)
            newCore.Palette?.SetSkin(_cachedSkinRow);
        newCore.Powerup?.ApplyElement(targetIdentity);   // element color resolved through the skin (NES-fire vs Modern-fire)

        if (_cachedStarPower)
        {
            // Adopt the same live music object that belonged to old Mario. StartStarPower then
            // rebinds the star state using Continue mode instead of creating/restarting a track.
            newCore.Combat.ResumeStarPowerAfterTransformation(
                _cachedStarPowerTime,
                _cachedStarMusicInstance
            );
        }

        // Re-attach carried object to new Mario.
        if (_cachedCarriedObject != null)
        {
            _cachedCarriedObject.transform.SetParent(newCore.Carry.HeldObjectPosition.transform);
            _cachedCarriedObject.transform.localPosition = Vector3.zero;
            newCore.State.Carrying = true;
        }

        // ── Device transfer, best-effort ──────────────────────────────────────
        if (_cachedPlayerInput != null && newMario.TryGetComponent(out PlayerInput dst))
        {
            try
            {
                dst.SwitchCurrentControlScheme(_cachedPlayerInput.devices.ToArray());
            }
            catch
            {
                Debug.Log("[PT] Could not transfer input device.");
            }
        }

        // ── Seed input state from snapshot ────────────────────────────────────
        // The new PlayerInput's action phases are all Waiting, so no held/released
        // callbacks are fired automatically. Seed from the cached values, then let
        // MarioInput poll raw controls after the transformation.
        t.RunPressed  = _cachedRunPressed;
        t.JumpPressed = _cachedJumpPressed;
        t.MoveInput   = _cachedMoveInput;
        t.Direction   = _cachedMoveInput;

        newMario.GetComponent<MarioInput>()?.BeginPostTransformationPolling();

        // ── Register ──────────────────────────────────────────────────────────
        if (_registry != null && _cachedPlayerIndex >= 0)
            _registry.RegisterPlayer(newCore, _cachedPlayerIndex);

        newCore.StartCoroutine(ForceStateNextFrame(newCore));

        Destroy(gameObject);
    }

    private static Collider2D GetPrimaryBodyCollider(MarioCore core, GameObject player)
    {
        if (core != null
            && core.Collider != null
            && core.Collider.enabled
            && !core.Collider.isTrigger
            && (core.CrushCollider == null || core.Collider != core.CrushCollider))
        {
            return core.Collider;
        }

        if (player != null)
        {
            var boxes = player.GetComponentsInChildren<BoxCollider2D>(true);

            foreach (var box in boxes)
            {
                if (box == null || !box.enabled || box.isTrigger)
                    continue;

                if (core != null && box == core.CrushCollider)
                    continue;

                if (box.GetComponent<CrushDetection>() != null)
                    continue;

                return box;
            }
        }

        if (core != null && core.Collider != null)
            return core.Collider;

        return player != null
            ? player.GetComponentInChildren<Collider2D>(true)
            : null;
    }

    /// <summary>
    /// Resolves the palette row that the target side of the shell should show.
    /// This mirrors MarioPowerup's layered palette logic before the target Mario exists:
    /// a skin-specific Fire/Ice row wins, otherwise the target-size skin row is shown.
    /// </summary>
    private float ResolveTargetRestRow()
    {
        float targetSkinRow = _cachedSpriteSkin != null
            ? _cachedSpriteSkin.RowFor(_newPowerupState)
            : _cachedSkinRow;

        if (targetIdentity == null || targetIdentity.PaletteRow < 0)
            return targetSkinRow;

        return _cachedSpriteSkin != null
            ? _cachedSpriteSkin.ElementRowFor(targetIdentity.PowerupType, targetIdentity.PaletteRow)
            : targetIdentity.PaletteRow;
    }

    // ─── Shell palette painting (during the morph) ───────────────────────────

    private void CycleShellStar()
    {
        int   count = Mathf.Max(1, _cachedStarRowCount);
        float row   = _cachedStarRowStart + (_shellStarFrame % count);
        _shellStarFrame++;
        SetChildRow(oldChild, row);
        SetChildRow(newChild, row);
    }

    private void SetChildRow(GameObject child, float row)
    {
        if (child == null) return;
        var sr = child.GetComponent<SpriteRenderer>();
        if (sr == null) return;
        _shellMpb ??= new MaterialPropertyBlock();
        sr.GetPropertyBlock(_shellMpb);
        _shellMpb.SetFloat(PaletteRowID, row);
        sr.SetPropertyBlock(_shellMpb);
    }

    private IEnumerator ForceStateNextFrame(MarioCore target)
    {
        yield return null;

        if (target == null)
            yield break;

        // Clear the transformation lock so the combat system can land hits again.
        target.State.IsTransforming = false;

        // Restore facing — the new prefab spawns with default scale.
        target.Physics.FlipTo(_cachedFacingRight);
    }
}