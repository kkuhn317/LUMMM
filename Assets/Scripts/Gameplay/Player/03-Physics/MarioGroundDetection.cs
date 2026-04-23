using UnityEngine;

/// <summary>
/// Owns all ground checks and upward/downward probe rays every FixedUpdate.
///
/// Responsibilities:
/// - Ground detection (box cast, slope-aware)
/// - Slope angle tracking and velocity projection
/// - Ground snapping (stick to ground surface)
/// - Moving platform parenting / momentum transfer
/// - Ceiling detection (for hittable blocks — future use)
/// - Corner correction (pushes Mario around tight ceiling edges)
/// - Detects ground pound landing and tells GroundPoundLandState
///
/// Writes to: State.OnGround, State.GroundPosition, State.FloorAngle,
///            State.OnMovingPlatform, State.WasGrounded
///
/// Reads from: State.IsCrouching, State.GroundPounding, State.Pushing
/// </summary>
[DefaultExecutionOrder(-100)] // Must run before MarioStateMachine (-90) so State.OnGround
                              // is up-to-date when the FSM's FixedUpdate reads it.
[RequireComponent(typeof(MarioCore))]
public class MarioGroundDetection : MonoBehaviour
{
    private MarioCore _core;
    private MarioState State => _core.State;
    private MarioPhysicsConfig Cfg => _core.Physics.Config;

    // Gizmo state — tracks which correction fired this frame for color feedback
    private bool _ceilingCCFiredLeft;
    private bool _ceilingCCFiredRight;
    private bool _verticalCCFiredLeft;
    private bool _verticalCCFiredRight;

    // ─── Probe Offsets (computed from config) ────────────────────────────────

    private Vector3 HOffset =>
        new(0f, State.IsCrouching ? Cfg.CrouchColliderOffsetY : 0f, 0f);

    private Vector3 CeilingProbeLeft =>
        new(-Cfg.CeilingProbeSeparation + Cfg.CeilingProbeOffsetX, 0f, 0f);

    private Vector3 CeilingProbeMid =>
        new(Cfg.CeilingProbeOffsetX, 0f, 0f);

    private Vector3 CeilingProbeRight =>
        new(Cfg.CeilingProbeSeparation + Cfg.CeilingProbeOffsetX, 0f, 0f);

    private Vector3 GroundPoundProbeLeft =>
        new(-Cfg.GroundPoundProbeSeparation + Cfg.GroundPoundProbeOffsetX, 0f, 0f);

    private Vector3 GroundPoundProbeRight =>
        new(Cfg.GroundPoundProbeSeparation + Cfg.GroundPoundProbeOffsetX, 0f, 0f);
    
    private const float GroundSupportProbeDistanceAir = 0.08f;
    private const float GroundSupportProbeDistanceGrounded = 0.18f;
    // Extra downward probe reach added per unit of downhill speed to prevent
    // losing ground contact when descending a slope quickly.
    private const float GroundSupportProbeVelocityScale = 0.04f;
    private const float GroundSupportProbeMaxExtra = 0.20f;
    private const float FlatSupportNormalY = 0.85f;
    private const int GroundSeamFlatFrameBuffer = 0;
    private const float GroundSupportHeightTolerance = 0.03f;

    private bool _treatGroundAsFlat;
    private int _groundSeamFlatFrames;
    private float _groundSeamSnapY;

    /// <summary>
    /// Set this to true from RiseState/SpinJumpState Enter() to prevent
    /// MarioGroundDetection from zeroing the jump velocity on the overlap frame.
    /// Automatically cleared after one FixedUpdate.
    /// </summary>
    public bool SkipConstraintsThisFrame { get; set; }


    private void Awake()
    {
        _core = GetComponent<MarioCore>();
    }

    // ─── FixedUpdate ─────────────────────────────────────────────────────────

    private void FixedUpdate()
    {
        _ceilingCCFiredLeft = _ceilingCCFiredRight = false;
        _verticalCCFiredLeft = _verticalCCFiredRight = false;
        _treatGroundAsFlat = false;

        // Do NOT cache SkipConstraintsThisFrame here.
        // GD runs at execution order -100, FSM at -90.
        // RiseState.Enter() sets the flag during FSM.FixedUpdate, which hasn't run yet.
        // We read the live property at point-of-use instead (see ProcessGrounding).

        bool wasOnMovingPlatform = State.OnMovingPlatform;

        // Do not use land grounding while swimming.
        // Water floor contact should not trigger snap-to-ground / slope pinning / land constraints.
        if (State.Swimming)
        {
            if (wasOnMovingPlatform && transform.parent != null
                && transform.parent.CompareTag("MovingPlatform"))
            {
                _core.Physics.TransferMovingPlatformMomentum();
                transform.parent = null;
            }

            State.OnGround = false;
            State.OnMovingPlatform = false;
            State.OnConveyor = null;
            State.FloorAngle = 0f;
            State.FloorNormal = Vector2.up;

            CheckCeiling();
            return;
        }

        bool wasGrounded = State.OnGround;

        bool skipGroundCheck = State.Climbing;

        RaycastHit2D? hit = skipGroundCheck ? null : CheckGround();

        if (hit.HasValue)
        {
            ProcessGrounding(hit.Value, wasGrounded, wasOnMovingPlatform);
            ApplyConveyorBelt();
        }
        else
        {
            State.OnGround = false;
            ProcessAirborne(wasOnMovingPlatform);
        }

        if (_core.Rb.velocity.y > 0f)
            ApplyCornerCorrection();

        if (!State.OnGround && _core.Rb.velocity.y < 0f && Mathf.Abs(_core.Rb.velocity.x) > 0.1f)
            ApplyVerticalCornerCorrection();

        CheckCeiling();
    }

    // ─── Ground Detection ────────────────────────────────────────────────────

    private Vector2 GroundCheckOffset
    {
        get
        {
            return Cfg.GroundCheckOffset;
        }
    }

    public RaycastHit2D? CheckGround()
    {
        Vector2 boxSize = Cfg.GroundCheckSize;
        Vector2 boxOrigin = (Vector2)transform.position + GroundCheckOffset;

        Collider2D[] overlaps = Physics2D.OverlapBoxAll(
            boxOrigin,
            boxSize,
            0f,
            _core.Physics.GroundLayer);

        bool anyOverlap = false;
        foreach (Collider2D col in overlaps)
        {
            if (col == null)
                continue;

            if (State.PushingObject != null && col.gameObject == State.PushingObject.gameObject)
                continue;

            anyOverlap = true;
            break;
        }

        float supportDistance = State.OnGround
            ? GroundSupportProbeDistanceGrounded
            : GroundSupportProbeDistanceAir;

        // When grounded on a slope, extend the probe proportionally to downhill speed
        // so we don't lose contact when moving fast down a slope.
        if (State.OnGround && State.FloorAngle != 0f)
        {
            float downhillSpeed = Mathf.Max(0f, -_core.Rb.velocity.y);
            float extra = Mathf.Min(downhillSpeed * GroundSupportProbeVelocityScale, GroundSupportProbeMaxExtra);
            supportDistance += extra;
        }

        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            boxOrigin,
            boxSize,
            0f,
            Vector2.down,
            supportDistance,
            _core.Physics.GroundLayer);

        bool anyHit = false;

        bool hasFlatSupport = false;
        bool hasLeftSlope = false;
        bool hasRightSlope = false;

        RaycastHit2D closestHit = default;
        float closestDistance = float.MaxValue;

        RaycastHit2D highestHit = default;
        float highestPointY = float.MinValue;
        float highestNormalY = float.MinValue;

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (State.PushingObject != null && hit.transform.gameObject == State.PushingObject.gameObject)
                continue;

            if (hit.normal.y <= Cfg.GroundMinNormalY)
                continue;

            anyHit = true;

            if (hit.normal.y >= FlatSupportNormalY)
                hasFlatSupport = true;

            if (hit.normal.x < -0.1f)
                hasLeftSlope = true;
            else if (hit.normal.x > 0.1f)
                hasRightSlope = true;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
            }

            bool betterHighest =
                hit.point.y > highestPointY + 0.001f ||
                (Mathf.Abs(hit.point.y - highestPointY) <= 0.001f && hit.normal.y > highestNormalY);

            if (betterHighest)
            {
                highestPointY = hit.point.y;
                highestNormalY = hit.normal.y;
                highestHit = hit;
            }
        }

        if (!anyOverlap && !anyHit)
        {
            State.OnGround = false;
            _treatGroundAsFlat = false;
            _groundSeamFlatFrames = 0;
            return null;
        }

        // Height delta between the highest and closest hit points.
        // On open slope, adjacent tiles differ by ~tan(slope_angle) * box_width ≈ 0.25–0.40 units.
        // At a flat→slope corner the delta jumps to ~0.50+ units.
        // Use 0.45 as the threshold: catches corner transitions, ignores normal slope variation.
        float supportHeightDelta = anyHit ? Mathf.Abs(highestPointY - closestHit.point.y) : 0f;
        const float SeamHeightDeltaThreshold = 0.45f;

        bool seamDetected =
            (hasFlatSupport && (hasLeftSlope || hasRightSlope)) ||
            (hasLeftSlope && hasRightSlope) ||
            (supportHeightDelta > SeamHeightDeltaThreshold);

        if (seamDetected)
            _groundSeamFlatFrames = GroundSeamFlatFrameBuffer;
        else if (_groundSeamFlatFrames > 0)
            _groundSeamFlatFrames--;

        _treatGroundAsFlat = seamDetected || _groundSeamFlatFrames > 0;

        RaycastHit2D bestHit = _treatGroundAsFlat ? highestHit : closestHit;
        _groundSeamSnapY = highestPointY;

        // Prevent re-grounding while rising in an airborne state.
        // We keep the IsAirborne guard here intentionally: without it, any small upward
        // velocity on a slope seam sets OnGround=false for one frame, causing jitter.
        // The jump-cancellation issue is handled in SnapToGround instead.
        bool isAirborneState = _core.StateMachine.IsAirborne;
        bool movingUpInWorld = _core.Rb.velocity.y > 0.05f;
        bool goingUp = isAirborneState && movingUpInWorld;

        State.OnGround = !goingUp;

    #if UNITY_EDITOR
        if (anyHit)
            Debug.Log($"[Ground] overlap={anyOverlap} hit={anyHit} onGround={State.OnGround} seam={seamDetected} flatOverride={_treatGroundAsFlat} supportDist={supportDistance:F2} vel={_core.Rb.velocity} normal={bestHit.normal}");
    #endif

#if UNITY_EDITOR
        Debug.Log($"[Seam] flatSupport={hasFlatSupport} leftSlope={hasLeftSlope} rightSlope={hasRightSlope} heightDelta={supportHeightDelta:F4} highest={highestPointY:F4} closest={closestHit.point.y:F4} seamDetected={seamDetected} treatFlat={_treatGroundAsFlat}");
#endif

        if (!State.OnGround || !anyHit)
            return null;

        return bestHit;
    }

    // ─── Grounded Processing ─────────────────────────────────────────────────

    private void ProcessGrounding(RaycastHit2D hit, bool wasGrounded, bool wasOnMovingPlatform)
    {
        bool wasInAir = !wasGrounded;

        // ── Moving Platform ──────────────────────────────────────────────────

        if (hit.transform.CompareTag("MovingPlatform"))
        {
            State.OnMovingPlatform = true;

            if (transform.parent != hit.transform)
                transform.parent = hit.transform;
        }
        else
        {
            if (wasOnMovingPlatform && transform.parent != null
                && transform.parent.CompareTag("MovingPlatform"))
            {
                _core.Physics.TransferMovingPlatformMomentum();
                transform.parent = null;
            }

            State.OnMovingPlatform = false;
        }

        // ── Conveyor ─────────────────────────────────────────────────────────

        State.OnConveyor = hit.transform.GetComponentInParent<ConveyorBelt>();

        // ── Slope ────────────────────────────────────────────────────────────

        float newAngle = 0f;
        Vector2 newNormal = _treatGroundAsFlat ? Vector2.up : hit.normal.normalized;

        if (!_treatGroundAsFlat && Mathf.Abs(hit.normal.x) > 0.1f)
        {
            newAngle = Vector2.SignedAngle(hit.normal, Vector2.up) * Mathf.Sign(hit.normal.x);

            if (hit.transform.CompareTag("Slope")
                && hit.transform.TryGetComponent(out Slope slope))
            {
                newAngle = slope.angle;
            }
        }

        if (Mathf.Abs(newAngle) > Cfg.MaxWalkableAngle)
        {
            State.OnGround = false;
            return;
        }

        Vector2 oldNormal = State.FloorNormal.sqrMagnitude > 0.0001f ? State.FloorNormal.normalized : Vector2.up;
        bool surfaceChanged = Vector2.Angle(oldNormal, newNormal) > 0.1f;

        if (surfaceChanged && !wasInAir)
        {
            const float inputDeadzone = 0.1f;
            const float slopeStopSpeed = 0.8f;

            // When the previous frame was in flat/seam mode, oldNormal is Vector2.up
            // and dotting against it gives the wrong tangent speed for a slope.
            // Use newNormal's tangent directly in that case — velocity is already horizontal.
            Vector2 tangentRef = _treatGroundAsFlat ? newNormal : oldNormal;
            float tangentSpeed = Vector2.Dot(_core.Rb.velocity, GetGroundTangent(tangentRef));

            // Do not preserve tiny residual motion when the player is not actively moving.
            if (Mathf.Abs(State.Direction.x) < inputDeadzone && Mathf.Abs(tangentSpeed) < slopeStopSpeed)
                tangentSpeed = 0f;

            ReprojectVelocityOnSurface(newNormal, tangentSpeed);
        }

        if (newAngle != 0f && wasInAir)
        {
            const float inputDeadzone = 0.1f;

            float tangentSpeed = Vector2.Dot(_core.Rb.velocity, GetGroundTangent(newNormal));

            // Preserve intentional horizontal landing, but do not create sideways drift
            // from a purely vertical drop onto a slope.
            if (Mathf.Abs(State.Direction.x) < inputDeadzone)
                tangentSpeed = 0f;

            ReprojectVelocityOnSurface(newNormal, tangentSpeed, keepIntoSurfaceVelocity: false);
        }

        State.FloorAngle = newAngle;
        State.FloorNormal = newNormal;
        State.GroundPosition = hit.point;

        // ── Ground Snap ──────────────────────────────────────────────────────

        bool isFlat = _treatGroundAsFlat || Mathf.Abs(newAngle) < 1f;

        if (!_core.State.Climbing && !SkipConstraintsThisFrame)
        {
            SkipConstraintsThisFrame = false; // consume
            SnapToGround(hit);

            if (isFlat)
            {
#if UNITY_EDITOR
                if (Mathf.Abs(_core.Rb.velocity.y) > 0.01f)
                    Debug.Log($"[Constraint] FLAT zeroing vel.y={_core.Rb.velocity.y:F4} vel={_core.Rb.velocity}");
#endif
                _core.Rb.velocity = new Vector2(_core.Rb.velocity.x, 0f);
            }
            else
            {
                float awayFromSurface = Vector2.Dot(_core.Rb.velocity, hit.normal);

#if UNITY_EDITOR
                if (_core.Rb.velocity.sqrMagnitude > 0.001f)
                    Debug.Log($"[Constraint] slope vel={_core.Rb.velocity} normal={hit.normal} away={awayFromSurface:F3} normalVelCheck={Vector2.Dot(_core.Rb.velocity, hit.normal):F3}");
#endif

                if (awayFromSurface < 2.0f)
                {
                    float tangentSpeed = Vector2.Dot(_core.Rb.velocity, GetGroundTangent(hit.normal));
                    ReprojectVelocityOnSurface(hit.normal, tangentSpeed);
                    _core.Rb.gravityScale = 0f;
                }

                const float inputDeadzone = 0.1f;

                bool noMoveInput = Mathf.Abs(State.Direction.x) < inputDeadzone;
                if (noMoveInput && !State.OnConveyor && !State.OnMovingPlatform)
                {
                    ReprojectVelocityOnSurface(newNormal, 0f);
                    _core.Rb.gravityScale = 0f;
                }
            }
        }

        // ── Ground Pound Landing ─────────────────────────────────────────────

        if (State.GroundPounding && !State.GroundPoundLanded)
        {
            var gpLandState = _core.StateMachine.GetState<GroundPoundLandState>();
            if (gpLandState != null)
            {
                float groundLen = Cfg.GroundPoundProbeReach;
                RaycastHit2D gpHit1 = Physics2D.Raycast(
                    transform.position + GroundPoundProbeLeft + HOffset,
                    Vector2.down,
                    groundLen,
                    _core.Physics.GroundLayer);

                RaycastHit2D gpHit2 = Physics2D.Raycast(
                    transform.position + GroundPoundProbeRight + HOffset,
                    Vector2.down,
                    groundLen,
                    _core.Physics.GroundLayer);

                var hitObjects = new System.Collections.Generic.List<GameObject>();
                if (gpHit1.collider != null)
                    hitObjects.Add(gpHit1.transform.gameObject);

                if (gpHit2.collider != null && (hitObjects.Count == 0 || gpHit2.transform.gameObject != hitObjects[0]))
                    hitObjects.Add(gpHit2.transform.gameObject);

                gpLandState.SetHitObjects(hitObjects);
            }

            _core.StateMachine.RequestTransition(MarioStateID.GroundPoundLand);
        }

        // ── Damaging Ground ──────────────────────────────────────────────────

        if (hit.transform.CompareTag("Damaging"))
            _core.Combat.DamageMario();

        // ── State Cleanup on Landing ─────────────────────────────────────────

        State.Spinning = false;
        State.SpinPressed = false;

        if (State.IsMidairSpinning)
        {
            State.IsMidairSpinning = false;
            MarioEvents.FireMidairSpinEnded(_core.PlayerIndex);
        }

        if (wasInAir)
            ComboManager.Instance?.ResetStomp();

        if (State.Direction.x == 0f && wasInAir && Mathf.Abs(_core.Rb.velocity.x) > 0.01f)
            _core.Physics.FlipTo(_core.Rb.velocity.x > 0f);
    }

    // ─── Airborne Processing ─────────────────────────────────────────────────

    private void ProcessAirborne(bool wasOnMovingPlatform)
    {
        if (wasOnMovingPlatform && transform.parent != null
            && transform.parent.CompareTag("MovingPlatform"))
        {
            _core.Physics.TransferMovingPlatformMomentum();
            transform.parent = null;
        }

        State.OnMovingPlatform = false;
        State.OnConveyor = null;

        if (State.FloorAngle != 0f && _core.Rb.velocity.y > 0f && !_core.StateMachine.IsAirborne)
        {
#if UNITY_EDITOR
            Debug.Log($"[Airborne] zeroing vel.y on slope exit vel={_core.Rb.velocity}");
#endif
            _core.Rb.velocity = new Vector2(_core.Rb.velocity.x, 0f);
        }

        State.FloorAngle = 0f;
        State.FloorNormal = Vector2.up;
    }

    // ─── Ceiling Corner Correction ───────────────────────────────────────────

    private void ApplyCornerCorrection()
    {
        var bounds = _core.Collider.bounds;
        float startHeight = bounds.size.y / 2f + (_core.Rb.velocity.y * Time.fixedDeltaTime) + 0.01f + Cfg.CeilingCorrectionOffset.y;
        float halfWidth = bounds.size.x / 2f;
        float rayLen = halfWidth + Cfg.CeilingCorrectionRayLength;

        Vector3 origin = transform.position + new Vector3(Cfg.CeilingCorrectionOffset.x, startHeight, 0f);

        RaycastHit2D hitLeft = Physics2D.Raycast(origin, Vector2.left, rayLen, _core.Physics.GroundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(origin, Vector2.right, rayLen, _core.Physics.GroundLayer);

        if (hitLeft.collider == null && hitRight.collider == null)
            return;

        float distLeft = hitLeft.collider == null ? 999f : hitLeft.distance;
        float distRight = hitRight.collider == null ? 999f : hitRight.distance;
        float gapWidth = (distLeft + distRight) - bounds.size.x;

        if (gapWidth < 0f)
            return;

        float playerLeft = transform.position.x - halfWidth + Cfg.CeilingCorrectionThresholdOffset.x;
        float playerRight = transform.position.x + halfWidth + Cfg.CeilingCorrectionThresholdOffset.x;

        if (hitLeft.collider != null
            && hitLeft.point.x < playerLeft + Cfg.CeilingCorrectionThreshold
            && hitLeft.point.x > playerLeft)
        {
            Debug.Log($"[CornerCorrect] Nudge right (ceiling) | contact={hitLeft.point.x:F3} playerLeft={playerLeft:F3} threshold={Cfg.CeilingCorrectionThreshold:F3}");
            _ceilingCCFiredLeft = true;
            _core.Rb.position = new Vector2(hitLeft.point.x + halfWidth * 1.2f, _core.Rb.position.y);
        }
        else if (hitRight.collider != null
                 && hitRight.point.x > playerRight - Cfg.CeilingCorrectionThreshold
                 && hitRight.point.x < playerRight)
        {
            Debug.Log($"[CornerCorrect] Nudge left (ceiling) | contact={hitRight.point.x:F3} playerRight={playerRight:F3} threshold={Cfg.CeilingCorrectionThreshold:F3}");
            _ceilingCCFiredRight = true;
            _core.Rb.position = new Vector2(hitRight.point.x - halfWidth * 1.2f, _core.Rb.position.y);
        }
    }

    // ─── Floor Corner Correction ─────────────────────────────────────────────

    private void ApplyVerticalCornerCorrection()
    {
        var bounds = _core.Collider.bounds;
        float startHeight = bounds.size.y / 2f + Mathf.Abs(_core.Rb.velocity.y * Time.fixedDeltaTime) + 0.01f;
        float halfWidth = bounds.size.x / 2f;
        float rayLen = halfWidth + Cfg.FloorCorrectionRayLength;

        Vector3 origin = transform.position + new Vector3(Cfg.FloorCorrectionOffset.x, -startHeight + Cfg.FloorCorrectionOffset.y, 0f);

        RaycastHit2D hitLeft = Physics2D.Raycast(origin, Vector2.left, rayLen, _core.Physics.GroundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(origin, Vector2.right, rayLen, _core.Physics.GroundLayer);

        if (hitLeft.collider == null || hitRight.collider == null)
            return;

        float distLeft = hitLeft.distance;
        float distRight = hitRight.distance;
        float gapWidth = (distLeft + distRight) - bounds.size.x;

        if (gapWidth < 0f)
            return;

        float playerLeft = transform.position.x - halfWidth + Cfg.FloorCorrectionThresholdOffset.x;
        float playerRight = transform.position.x + halfWidth + Cfg.FloorCorrectionThresholdOffset.x;

        if (hitLeft.collider != null
            && hitLeft.point.x > playerLeft - Cfg.FloorCorrectionThreshold
            && hitLeft.point.x < playerLeft)
        {
            Debug.Log($"[VCornerCorrect] Nudge right | gapWidth={gapWidth:F3} leftContact={hitLeft.point.x:F3} playerLeft={playerLeft:F3}");
            _verticalCCFiredLeft = true;
            _core.Rb.position = new Vector2(hitLeft.point.x + halfWidth * 1.2f, _core.Rb.position.y);
        }
        else if (hitRight.collider != null
                 && hitRight.point.x < playerRight + Cfg.FloorCorrectionThreshold
                 && hitRight.point.x > playerRight)
        {
            Debug.Log($"[VCornerCorrect] Nudge left | gapWidth={gapWidth:F3} rightContact={hitRight.point.x:F3} playerRight={playerRight:F3}");
            _verticalCCFiredRight = true;
            _core.Rb.position = new Vector2(hitRight.point.x - halfWidth * 1.2f, _core.Rb.position.y);
        }
    }

    // ─── Ceiling Detection ───────────────────────────────────────────────────

    private void CheckCeiling()
    {
        float ceilLen = State.IsCrouching ? Cfg.CeilingLength / 2f : Cfg.CeilingLength;

        RaycastHit2D ceilLeft = Physics2D.Raycast(
            transform.position + CeilingProbeLeft + HOffset,
            Vector2.up,
            ceilLen,
            _core.Physics.GroundLayer);

        RaycastHit2D ceilMid = Physics2D.Raycast(
            transform.position + CeilingProbeMid + HOffset,
            Vector2.up,
            Cfg.CeilingLength,
            _core.Physics.GroundLayer);

        RaycastHit2D ceilRight = Physics2D.Raycast(
            transform.position + CeilingProbeRight + HOffset,
            Vector2.up,
            ceilLen,
            _core.Physics.GroundLayer);

        if (ceilLeft.collider == null && ceilMid.collider == null && ceilRight.collider == null)
            return;

        RaycastHit2D solidHit = default;
        foreach (var ceilingHit in new[] { ceilLeft, ceilMid, ceilRight })
        {
            if (ceilingHit.collider == null)
                continue;

            if (ceilingHit.collider.TryGetComponent<PlatformEffector2D>(out _))
                continue;

            if (ceilingHit.normal.y < -0.5f)
            {
                solidHit = ceilingHit;
                break;
            }
        }

        if (solidHit.collider == null)
            return;

        if (_core.Rb.velocity.y > 0f)
        {
            IBumpable bumpable = solidHit.collider.GetComponent<IBumpable>()
                                ?? solidHit.collider.GetComponentInParent<IBumpable>();

            if (bumpable != null)
                bumpable.Bump(BlockHitDirection.Up, _core);

            MarioEvents.FireBonked(_core.PlayerIndex);
        }
    }

    // ─── Gizmos ──────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        var core = _core != null ? _core : GetComponent<MarioCore>();
        if (!enabled || core == null)
            return;

        var physics = core.Physics != null ? core.Physics : core.GetComponent<MarioPhysics>();
        var collider = core.Collider != null ? core.Collider : core.GetComponentInChildren<BoxCollider2D>();
        var cfg = physics != null ? physics.Config : null;
        if (cfg == null || collider == null)
            return;

        bool crouching = Application.isPlaying && core.State.IsCrouching;
        float ceilLen = crouching ? cfg.CeilingLength / 2f : cfg.CeilingLength;

        float ceilSep = cfg.CeilingProbeSeparation;
        float ceilOffX = cfg.CeilingProbeOffsetX;
        Vector3 ceilingLeft = new Vector3(-ceilSep + ceilOffX, 0f, 0f);
        Vector3 ceilingRight = new Vector3(ceilSep + ceilOffX, 0f, 0f);
        Vector3 ceilingMid = new Vector3(ceilOffX, 0f, 0f);

        // Ground overlap box (red)
        Gizmos.color = Color.red;

        Vector2 groundBoxSize = cfg.GroundCheckSize;
        Vector2 groundOffset = cfg.GroundCheckOffset;

        Vector3 groundOrigin = transform.position + (Vector3)groundOffset;
        Gizmos.DrawWireCube(groundOrigin, groundBoxSize);

        // Downward probe for snap / floor info
        Gizmos.color = Color.cyan;

        Vector3 probeOrigin = groundOrigin;
        float probeDistance = GroundSupportProbeDistanceGrounded;
        Gizmos.DrawLine(probeOrigin, probeOrigin + Vector3.down * probeDistance);

        // Ground pound landing rays (magenta)
        Gizmos.color = Color.magenta;

        float gpSep = cfg.GroundPoundProbeSeparation;
        float gpOffX = cfg.GroundPoundProbeOffsetX;
        float gpReach = cfg.GroundPoundProbeReach;

        Vector3 gpLeft = new Vector3(-gpSep + gpOffX, 0f, 0f);
        Vector3 gpRight = new Vector3(gpSep + gpOffX, 0f, 0f);

        Vector3 gpOffset = Vector3.zero;
        if (crouching)
            gpOffset.y += cfg.CrouchColliderOffsetY;

        DrawRay(transform.position + gpLeft + gpOffset, Vector2.down, gpReach);
        DrawRay(transform.position + gpRight + gpOffset, Vector2.down, gpReach);

        // Ceiling rays (yellow)
        Gizmos.color = Color.yellow;
        DrawRay(transform.position + ceilingLeft, Vector2.up, ceilLen);
        DrawRay(transform.position + ceilingMid, Vector2.up, cfg.CeilingLength);
        DrawRay(transform.position + ceilingRight, Vector2.up, ceilLen);

        // Ceiling corner correction gizmos
        {
            float ccHalfW = collider.size.x / 2f;
            float ccHalfH = collider.size.y / 2f;
            float ccRayLen = ccHalfW + cfg.CeilingCorrectionRayLength;
            Vector3 ccCenter = transform.position + new Vector3(collider.offset.x, collider.offset.y);
            float topY = ccCenter.y + ccHalfH + cfg.CeilingCorrectionOffset.y;
            float ccOriginX = ccCenter.x + cfg.CeilingCorrectionOffset.x;
            float ccLeft = ccCenter.x - ccHalfW + cfg.CeilingCorrectionThresholdOffset.x;
            float ccRight = ccCenter.x + ccHalfW + cfg.CeilingCorrectionThresholdOffset.x;
            Vector3 topOrigin = new Vector3(ccOriginX, topY);

            Gizmos.color = (_ceilingCCFiredLeft || _ceilingCCFiredRight) ? Color.red : new Color(1f, 0.5f, 0f);
            DrawRay(topOrigin, Vector2.left, ccRayLen);
            DrawRay(topOrigin, Vector2.right, ccRayLen);

            float ceilThreshY = topY + 0.05f + cfg.CeilingCorrectionThresholdOffset.y;
            Gizmos.color = _ceilingCCFiredLeft ? Color.red : Color.green;
            Gizmos.DrawLine(new Vector3(ccLeft, ceilThreshY),
                            new Vector3(ccLeft - cfg.CeilingCorrectionThreshold, ceilThreshY));

            Gizmos.color = _ceilingCCFiredRight ? Color.red : Color.green;
            Gizmos.DrawLine(new Vector3(ccRight, ceilThreshY),
                            new Vector3(ccRight + cfg.CeilingCorrectionThreshold, ceilThreshY));
        }

        // Floor corner correction gizmos
        {
            float vccHalfW = collider.size.x / 2f;
            float vccHalfH = collider.size.y / 2f;
            float vccRayLen = vccHalfW + cfg.FloorCorrectionRayLength;
            Vector3 colCenter = transform.position + new Vector3(collider.offset.x, collider.offset.y);
            float bottomY = colCenter.y - vccHalfH + cfg.FloorCorrectionOffset.y;
            float vccOriginX = colCenter.x + cfg.FloorCorrectionOffset.x;
            float leftEdge = colCenter.x - vccHalfW + cfg.FloorCorrectionThresholdOffset.x;
            float rightEdge = colCenter.x + vccHalfW + cfg.FloorCorrectionThresholdOffset.x;
            Vector3 bottomOrigin = new Vector3(vccOriginX, bottomY);

            Gizmos.color = (_verticalCCFiredLeft || _verticalCCFiredRight) ? Color.red : new Color(1f, 0.5f, 0f);
            DrawRay(bottomOrigin, Vector2.left, vccRayLen);
            DrawRay(bottomOrigin, Vector2.right, vccRayLen);

            float floorThreshY = bottomY - 0.05f + cfg.FloorCorrectionThresholdOffset.y;
            Gizmos.color = _verticalCCFiredLeft ? Color.red : Color.green;
            Gizmos.DrawLine(new Vector3(leftEdge, floorThreshY),
                            new Vector3(leftEdge - cfg.FloorCorrectionThreshold, floorThreshY));

            Gizmos.color = _verticalCCFiredRight ? Color.red : Color.green;
            Gizmos.DrawLine(new Vector3(rightEdge, floorThreshY),
                            new Vector3(rightEdge + cfg.FloorCorrectionThreshold, floorThreshY));
        }
    }

    private static void DrawRay(Vector3 origin, Vector2 dir, float len)
        => Gizmos.DrawLine(origin, origin + (Vector3)(dir * len));

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private Vector2 GetGroundTangent(Vector2 normal)
    {
        Vector2 n = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector2.up;
        return new Vector2(n.y, -n.x).normalized;
    }

    private void ReprojectVelocityOnSurface(Vector2 normal, float tangentialSpeed, bool keepIntoSurfaceVelocity = true)
    {
        Vector2 n = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector2.up;
        Vector2 t = GetGroundTangent(n);

        float normalSpeed = keepIntoSurfaceVelocity ? Vector2.Dot(_core.Rb.velocity, n) : 0f;
        if (normalSpeed > 0f)
            normalSpeed = 0f;

        _core.Rb.velocity = t * tangentialSpeed + n * normalSpeed;
    }

    private void SnapToGround(RaycastHit2D hit)
    {
        Vector2 offset = GroundCheckOffset;
        float sensorBottomFromPivot = -(offset.y - (Cfg.GroundCheckSize.y * 0.5f));

        float supportY = _treatGroundAsFlat
            ? _groundSeamSnapY
            : Mathf.Max(hit.point.y, _groundSeamSnapY - GroundSupportHeightTolerance);

        float targetY = supportY + sensorBottomFromPivot - Cfg.GroundSink;

        float distanceToTarget = Mathf.Abs(_core.Rb.position.y - targetY);

        // Only check the vertical component of velocity for the snap guard.
        // Using the full dot product against the slope normal caused horizontal
        // movement to read as "moving away" and block the snap entirely.
        float verticalVelocity = _core.Rb.velocity.y;

#if UNITY_EDITOR
        float awayDebug = Vector2.Dot(_core.Rb.velocity, hit.normal);
        Debug.Log($"[Snap] dist={distanceToTarget:F4} away={awayDebug:F4} vel={_core.Rb.velocity} normal={hit.normal} posY={_core.Rb.position.y:F4} targetY={targetY:F4}");
#endif

        // Allow snapping as long as the player isn't actively moving upward.
        if (distanceToTarget <= 0.25f && verticalVelocity <= 0.05f)
        {
            _core.Rb.position = new Vector2(_core.Rb.position.x, targetY);

            // Kill upward velocity component along the surface normal to prevent bouncing.
            // Only do this on real slope surfaces — skip when _treatGroundAsFlat is active
            // (seam between flat+slope tiles) because the seam normal is unreliable and
            // dotting horizontal velocity against it creates a spurious downward component
            // that then causes sliding after a landing.
            if (!_treatGroundAsFlat)
            {
                float awayFromSurface = Vector2.Dot(_core.Rb.velocity, hit.normal);
                if (awayFromSurface > 0f)
                {
#if UNITY_EDITOR
                    Debug.Log($"[Snap] STRIPPING away vel: {hit.normal * awayFromSurface} from {_core.Rb.velocity}");
#endif
                    _core.Rb.velocity -= hit.normal * awayFromSurface;
                }
            }
        }
    }

    private void ApplyConveyorBelt()
    {
        if (State.OnConveyor == null)
            return;

        _core.Rb.position += State.OnConveyor.Velocity * Time.fixedDeltaTime;
    }
}