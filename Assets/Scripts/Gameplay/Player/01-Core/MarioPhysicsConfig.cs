using UnityEngine;

/// <summary>
/// All tunable physics and movement values for Mario, as a ScriptableObject.
///
/// Why ScriptableObject?
/// - Lives as an asset, not on the prefab — easy to share between P1 and P2.
/// - Survives powerup transitions (no need to retransfer float fields).
/// - Designer-friendly: tweak in the Inspector without touching prefabs.
/// - Supports multiple configs (e.g. SmallMarioConfig, FireMarioConfig).
///
/// HOW TO CREATE:
///   Right-click in Project → Create → Mario → Physics Config
/// </summary>
[CreateAssetMenu(menuName = "Mario/Physics Config", fileName = "MarioPhysicsConfig")]
public class MarioPhysicsConfig : ScriptableObject
{
    [Header("Horizontal Movement")]
    public float MoveSpeed    = 10f;
    public float RunSpeed     = 20f;
    public float SlowDownForce = 5f;
    public float MaxSpeed     = 7f;
    public float MaxRunSpeed  = 10f;

    [Header("Turning")]
    [Tooltip("Speed multiplier when skidding at max run speed")]
    public float SkidSpeedMult  = 0.7f;
    [Tooltip("Speed multiplier when changing direction in the air")]
    public float AirTurnMult    = 1.5f;
    [Tooltip("Crawl force multiplier (small Mario only)")]
    public float CrawlForceMult = 2f;
    [Tooltip("Crawl max speed multiplier")]
    public float CrawlMaxSpeedMult = 0.5f;

    [Header("Vertical Movement / Jump")]
    public float JumpSpeed             = 11f;   // Standing jump
    public float WalkJumpSpeed         = 12f;   // Moving jump
    public float WalkJumpSpeedRequired = 1f;    // Min horizontal speed for walk jump
    public float JumpDelay             = 0.25f; // Jump buffer window
    public float TerminalVelocity      = 10f;
    public float StartFallingSpeed     = 1f;    // Below this vertical speed, begin falling gravity

    [Header("Gravity")]
    public float RiseGravity = 0f;
    public float PeakGravity = 1f;
    public float FallGravity = 5f;

    [Header("Airtime")]
    public float Airtime = 1f;
    public float WalkJumpAirtime = 1.5f;
    public float CoyoteTime = 0.08f; // Window after walking off a ledge where jumping is still allowed
    public float SpinJumpAirtime = 1.0f; // Airtime specifically for spin jump (separate from regular jump)

    [Header("Crouch")]
    public float CrouchColliderHeight  = 0.5f;
    public float CrouchColliderOffsetY = -0.25f;

    [Header("Wall Jump")]
    public float WallJumpHoldTime = 0.25f;

    [Header("Spin Jump")]
    public float SpinMultiplier = 0.8f;

    [Header("Midair Spin")]
    public float MidairSpinDuration            = 0.40f;
    public float MidairSpinStallTime           = 0.15f;
    public float MidairSpinGravityMult         = 0.25f;
    public float MidairSpinFallSpeedCap        = 2.0f;
    public float MidairSpinUpwardBoost         = 2f;
    [Range(0f, 1f)]
    public float MidairSpinHorizontalPreserve  = 0.8f;
    public float MidairSpinCooldown            = 0.20f;

    [Header("Ground Pound")]
    public float GroundPoundSpinTime = 0.5f;    // Freeze duration before falling
    public float GroundPoundLandLockTime = 0.25f; // Lock duration after landing

    [Header("Swimming")]
    public float SwimForce          = 5f;
    public float SwimGravity        = 1f;
    public float SwimDrag           = 3f;
    public float SwimTerminalVelocity = 2f;
    public float BubbleSpawnDelay   = 2.5f;

    [Header("Collision / Raycasts")]
    [Tooltip("Steepest slope angle Mario can walk on (degrees from horizontal). Surfaces steeper than this are treated as walls.")]
    public float MaxWalkableAngle   = 60f;
    public float GroundLength       = 0.6f;
    public float GroundSink         = 0.1f;
    public float CeilingLength      = 0.5f;
    public float RaycastSeparation  = 0.35f;
    public float RaycastOffsetX     = 0f;

    [Header("Ceiling Corner Correction")]
    public Vector2 CeilingCorrectionOffset = Vector2.zero;
    public float CeilingCorrectionRayLength = 0.1f;
    public float CeilingCorrectionThreshold = 0.1f;
    public Vector2 CeilingCorrectionThresholdOffset = Vector2.zero;

    [Header("Floor Corner Correction")]
    public Vector2 FloorCorrectionOffset = Vector2.zero;
    public float FloorCorrectionRayLength = 0.1f;
    public float FloorCorrectionThreshold = 0.1f;
    public Vector2 FloorCorrectionThresholdOffset = Vector2.zero;

    [Header("Carrying")]
    public float GrabRaycastDistanceSmall = -0.1f;
    public float GrabRaycastDistanceLarge = -0.4f;

    [Header("Damage")]
    public float DamageInvincibilityTime = 3f;

    [Header("Animation")]
    public float WalkAnimatorSpeed = 0.125f;
}