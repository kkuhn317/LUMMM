using UnityEngine;

/// <summary>
/// Owns all horizontal wall raycasts.
///
/// Used by:
/// - FallState.CheckTransitions  → enter WallSlide
/// - WallSlideState.CheckTransitions → exit if wall gone
/// - WallJumpState.Enter → wall jump direction
/// - SpinJump wall check → spin-based wall grab
///
/// Exposes CheckWall() as a public method so states can query on demand
/// rather than polling every frame.
///
/// Ceiling bonk sound is handled by AirborneStateBase.CheckCeilingBonk(),
/// called from rising states (Rise, SpinJump, WallJump).
/// </summary>
[RequireComponent(typeof(MarioCore))]
public class MarioWallDetection : MonoBehaviour
{
    private MarioCore  _core;
    private MarioState State => _core.State;

    private void Awake()
    {
        _core = GetComponent<MarioCore>();
    }

    // ─── Wall Check ──────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if there is a wall in the given direction that Mario
    /// can slide on or wall-jump off.
    ///
    /// Conditions (from original):
    /// - canWallJump must be true
    /// - Mario must be airborne
    /// - Not swimming
    /// - Not carrying (unless canWallJumpWhenHoldingObject)
    /// </summary>
    public bool CheckWall(bool right)
    {
        if (!State.CanWallJump)   return false;
        if (State.OnGround)       return false;
        if (State.Swimming)       return false;
        if (State.Carrying && !State.CanWallJumpWhenHoldingObject) return false;

        float rayLen = _core.Collider.bounds.size.x / 2f + 0.03f;
        Vector2 dir  = right ? Vector2.right : Vector2.left;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, dir, rayLen, _core.Physics.GroundLayer);

        return hit.collider != null;
    }

    // ─── Gizmos ──────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        var core = _core != null ? _core : GetComponent<MarioCore>();
        if (!enabled || core == null) return;

        var col = core.Collider != null ? core.Collider : GetComponentInChildren<BoxCollider2D>();
        float rayLen = col != null
            ? col.bounds.size.x / 2f + 0.03f
            : 0.5f;

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.left  * rayLen);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * rayLen);
    }
}
