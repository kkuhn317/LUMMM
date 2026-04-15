using UnityEngine;

/// <summary>
/// Lightweight ground detection for cutscene puppet Marios.
/// Mirrors MarioGroundDetection's two-raycast approach using the same
/// GroundLayer from MarioPhysics so behaviour is consistent with the real player.
///
/// Attach this to the cutscene Mario prefab. FlagArrival calls CheckGround(stepDistance)
/// each frame during the gravity drop, passing this frame's fall distance as the ray length
/// so the puppet snaps to the ground exactly when it would reach it — no more, no less.
/// </summary>
public class PuppetGroundDetection : MonoBehaviour
{
    [Tooltip("Horizontal separation between the two rays. Should match MarioPhysicsConfig.RaycastSeparation.")]
    public float raycastSeparation = 0.35f;

    [Tooltip("How far above the ground hit point to place the puppet pivot. Should match MarioPhysicsConfig.GroundLength - GroundSink.")]
    public float pivotOffset = 0.5f;

    // ─── Runtime ─────────────────────────────────────────────────────────────

    public bool  IsGrounded { get; private set; }
    public float GroundY    { get; private set; }

    private LayerMask _groundLayer;
    private bool      _initialized;

    public LayerMask GroundLayer => _groundLayer;

    // ─── Init ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Call once after spawning. Grabs GroundLayer from the first MarioPhysics in the scene.
    /// </summary>
    public void Initialize()
    {
        var physics = FindObjectOfType<MarioPhysics>();
        if (physics != null)
        {
            _groundLayer = physics.GroundLayer;
            _initialized = true;
        }
        else
        {
            Debug.LogWarning("[PuppetGroundDetection] No MarioPhysics found in scene — ground detection disabled.");
        }
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Cast downward by exactly stepDistance — this frame's fall amount.
    /// Returns true if the ground is within that distance, meaning the puppet
    /// would pass through it this frame and should snap instead.
    /// </summary>
    public bool CheckGround(float stepDistance)
    {
        if (!_initialized) return false;

        float rayLen = stepDistance + pivotOffset;

        Vector3 originLeft  = transform.position + new Vector3( raycastSeparation, 0f, 0f);
        Vector3 originRight = transform.position + new Vector3(-raycastSeparation, 0f, 0f);

        RaycastHit2D hitLeft  = Physics2D.Raycast(originLeft,  Vector2.down, rayLen, _groundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(originRight, Vector2.down, rayLen, _groundLayer);

        bool leftValid  = hitLeft.collider  != null;
        bool rightValid = hitRight.collider != null;

        IsGrounded = leftValid || rightValid;

        if (IsGrounded)
        {
            RaycastHit2D best = (leftValid && rightValid)
                ? (hitLeft.point.y > hitRight.point.y ? hitLeft : hitRight)
                : (leftValid ? hitLeft : hitRight);

            GroundY = best.point.y + pivotOffset;
        }

        return IsGrounded;
    }

    // ─── Gizmos ──────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Vector3 left  = transform.position + new Vector3( raycastSeparation, 0f, 0f);
        Vector3 right = transform.position + new Vector3(-raycastSeparation, 0f, 0f);
        Gizmos.DrawLine(left,  left  + Vector3.down * pivotOffset);
        Gizmos.DrawLine(right, right + Vector3.down * pivotOffset);
    }
}