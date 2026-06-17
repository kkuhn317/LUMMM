using UnityEngine;

/// <summary>
/// Owns physics utility operations that don't belong to any single state:
/// - FlipTo / Flip (facing direction)
/// - Moving platform momentum transfer
/// - Config reference (ScriptableObject)
///
/// States drive their own physics (forces, gravity) directly via Core.Rb.
/// This module handles the cross-cutting operations states call via Core.Physics.
/// </summary>
[RequireComponent(typeof(MarioCore))]
public class MarioPhysics : MonoBehaviour
{
    [Header("Config")]
    public MarioPhysicsConfig Config;

    [Header("Layers")]
    public LayerMask GroundLayer;
    public LayerMask CeilingLayer; // For detecting bumps against ceiling

    private MarioCore  _core;
    private MarioState State => _core.State;

    private void Awake()
    {
        _core = GetComponent<MarioCore>();
    }


    // ─── Facing Direction ────────────────────────────────────────────────────

    /// <summary>
    /// Flips Mario to face the given direction if not already facing it.
    /// Scales the Visual child object on X — does not touch SpriteRenderer.flipX
    /// since the new hierarchy uses pivot-based transforms.
    /// </summary>
    public void FlipTo(bool right)
    {
        if (State.FacingRight == right) return;
        if (State.GroundPounding || State.GroundPoundRotating) return;

        Flip();
    }

    private void Flip()
    {
        State.FacingRight = !State.FacingRight;

        // Scale the Visual root to flip all child pivots at once
        // This replaces sprite.flipX and handles the skeletal hierarchy correctly
        Transform visual = GetVisualRoot();
        if (visual != null)
        {
            float scaleX = State.FacingRight ? 1f : -1f;
            visual.localScale = new Vector3(scaleX, 1f, 1f);
        }

        // Keep RelLocs child correctly oriented (carry position, etc.)
        // The Rel Locs object needs to mirror too
        Transform relLocs = GetRelLocsRoot();
        if (relLocs != null)
        {
            float relScaleX = State.FacingRight ? 1f : -1f;
            relLocs.localScale = new Vector3(relScaleX, 1f, 1f);
        }

        MarioEvents.FireFlipped(_core.PlayerIndex);
    }

    // ─── Moving Platform ─────────────────────────────────────────────────────

    /// <summary>
    /// Transfers the moving platform's velocity to Mario when he steps off.
    /// Called by MarioGroundDetection when the platform parent changes.
    /// </summary>
    public void TransferMovingPlatformMomentum()
    {
        if (!State.DoMovingPlatformMomentum) return;
        
        if (State.OnMovingPlatform != null && State.OnMovingPlatform.TryGetComponent(out MovingPlatform platform))
        {
            _core.Rb.velocity += platform.velocity;
        }
        else if (State.OnConveyor != null && State.OnConveyor.TryGetComponent(out ConveyorBelt belt))
        {
            _core.Rb.velocity += belt.Velocity;
        }
    }

    // ─── Visual Root Helpers ─────────────────────────────────────────────────
    // Cached lazily so we don't GetChild every flip

    private Transform _visualRoot;
    private Transform _relLocsRoot;

    private Transform GetVisualRoot()
    {
        if (_visualRoot != null) return _visualRoot;

        // Convention: first child named "Visual"
        foreach (Transform child in _core.transform)
        {
            if (child.name == "Visual")
            {
                _visualRoot = child;
                return _visualRoot;
            }
        }

        // Fallback: first child (old hierarchy)
        if (_core.transform.childCount > 0)
            _visualRoot = _core.transform.GetChild(0);

        return _visualRoot;
    }

    private Transform GetRelLocsRoot()
    {
        if (_relLocsRoot != null) return _relLocsRoot;

        foreach (Transform child in _core.transform)
        {
            if (child.name == "Rel Locs")
            {
                _relLocsRoot = child;
                return _relLocsRoot;
            }
        }

        return null;
    }

    // ─── Config Swap (powerups) ──────────────────────────────────────────────

    private MarioPhysicsConfig _baseConfig;

    /// <summary>
    /// Replaces the active physics config. Stores the original so
    /// RestoreConfig() can undo it. Powerup prefabs call this on Awake
    /// via MarioPowerup.TransferToNewMario, or abilities call it directly.
    ///
    /// Pass null to restore the original config.
    /// </summary>
    public void SwapConfig(MarioPhysicsConfig newConfig)
    {
        if (newConfig == null) { RestoreConfig(); return; }

        if (_baseConfig == null)
            _baseConfig = Config; // Capture original on first swap

        Config = newConfig;
        MarioEvents.FirePhysicsConfigSwapped(_core.PlayerIndex);
    }

    /// <summary>Restores the original config that was active before any swap.</summary>
    public void RestoreConfig()
    {
        if (_baseConfig == null) return;
        Config      = _baseConfig;
        _baseConfig = null;
        MarioEvents.FirePhysicsConfigSwapped(_core.PlayerIndex);
    }

    // ─── Zero Velocity Helper ────────────────────────────────────────────────

    public void ZeroVelocity() => _core.Rb.velocity = Vector2.zero;
}