using UnityEngine;

/// <summary>
/// Owns all downward and upward raycasts every FixedUpdate.
///
/// Responsibilities:
/// - Ground detection (two raycasts, slope-aware)
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
    private MarioCore  _core;
    private MarioState State => _core.State;
    private MarioPhysicsConfig Cfg => _core.Physics.Config;

    // Gizmo state — tracks which correction fired this frame for color feedback
    private bool _ceilingCCFiredLeft;
    private bool _ceilingCCFiredRight;
    private bool _verticalCCFiredLeft;
    private bool _verticalCCFiredRight;

    // ─── Raycast Offsets (computed from config) ───────────────────────────────

    private Vector3 HOffset =>
        new(0f, State.IsCrouching ? -(Cfg.GroundLength / 2f) : 0f, 0f);

    private Vector3 RaycastLeft =>
        new( Cfg.RaycastSeparation + Cfg.RaycastOffsetX, 0f, 0f);

    private Vector3 RaycastRight =>
        new(-Cfg.RaycastSeparation + Cfg.RaycastOffsetX, 0f, 0f);

    private void Awake()
    {
        _core = GetComponent<MarioCore>();
    }

    // ─── FixedUpdate ─────────────────────────────────────────────────────────

    private void FixedUpdate()
    {
        _ceilingCCFiredLeft = _ceilingCCFiredRight = false;
        _verticalCCFiredLeft = _verticalCCFiredRight = false;

        bool wasGrounded         = State.OnGround;
        bool wasOnMovingPlatform = State.OnMovingPlatform;

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

        // Vertical corner correction: fire when falling, mirrors ceiling correction
        if (!State.OnGround && _core.Rb.velocity.y < 0f && Mathf.Abs(_core.Rb.velocity.x) > 0.1f)
            ApplyVerticalCornerCorrection();

        CheckCeiling();
    }

    // ─── Ground Detection ────────────────────────────────────────────────────

    public RaycastHit2D? CheckGround()
    {
        float groundLen = State.IsCrouching ? Cfg.GroundLength / 2f : Cfg.GroundLength;

        // Extend rays slightly to bridge flat→slope seams and handle spawn positioning.
        float castLen = groundLen + 0.12f;

        RaycastHit2D hit1 = Physics2D.Raycast(
            transform.position + RaycastLeft  + HOffset, Vector2.down, castLen, _core.Physics.GroundLayer);
        RaycastHit2D hitC = Physics2D.Raycast(
            transform.position               + HOffset, Vector2.down, castLen, _core.Physics.GroundLayer);
        RaycastHit2D hit2 = Physics2D.Raycast(
            transform.position + RaycastRight + HOffset, Vector2.down, castLen, _core.Physics.GroundLayer);

        bool hit1Valid = hit1.collider != null
            && (State.PushingObject == null || hit1.transform.gameObject != State.PushingObject.gameObject);
        bool hitCValid = hitC.collider != null
            && (State.PushingObject == null || hitC.transform.gameObject != State.PushingObject.gameObject);
        bool hit2Valid = hit2.collider != null
            && (State.PushingObject == null || hit2.transform.gameObject != State.PushingObject.gameObject);

        bool anyHit  = hit1Valid || hitCValid || hit2Valid;
        bool onSlope = false;
        if (anyHit)
        {
            RaycastHit2D bestHit = default;
            float bestY = float.MinValue;
            if (hit1Valid && hit1.point.y > bestY) { bestHit = hit1; bestY = hit1.point.y; }
            if (hitCValid && hitC.point.y > bestY) { bestHit = hitC; bestY = hitC.point.y; }
            if (hit2Valid && hit2.point.y > bestY) { bestHit = hit2; }
            onSlope = Mathf.Abs(bestHit.normal.x) > 0.1f;
        }

        bool goingUp;
        if (onSlope && anyHit)
        {
            RaycastHit2D slopeHit = default;
            float sY = float.MinValue;
            if (hit1Valid && hit1.point.y > sY) { slopeHit = hit1; sY = hit1.point.y; }
            if (hitCValid && hitC.point.y > sY) { slopeHit = hitC; sY = hitC.point.y; }
            if (hit2Valid && hit2.point.y > sY)    slopeHit = hit2;
            float awayFromSurface = Vector2.Dot(_core.Rb.velocity, -slopeHit.normal);
            goingUp = awayFromSurface > 1.0f;
        }
        else
        {
            goingUp = _core.Rb.velocity.y > 0.5f;
        }
        State.OnGround = anyHit && !goingUp;

#if UNITY_EDITOR
        if (!State.OnGround && anyHit)
            Debug.Log($"[Ground] OnGround=FALSE despite hit! goingUp={goingUp} vel.y={_core.Rb.velocity.y:F3} onSlope={onSlope} anyHit={anyHit} h1={hit1Valid} hC={hitCValid} h2={hit2Valid}");
#endif

        if (!State.OnGround) return null;

        RaycastHit2D best = default;
        float bestPtY = float.MinValue;
        if (hit1Valid && hit1.point.y > bestPtY) { best = hit1; bestPtY = hit1.point.y; }
        if (hitCValid && hitC.point.y > bestPtY) { best = hitC; bestPtY = hitC.point.y; }
        if (hit2Valid && hit2.point.y > bestPtY) { best = hit2; }
        return best;
    }

    // ─── Grounded Processing ─────────────────────────────────────────────────

    private void ProcessGrounding(RaycastHit2D hit, bool wasGrounded, bool wasOnMovingPlatform)
    {
        bool wasInAir = !wasGrounded;

        // ── Moving Platform ──────────────────────────────────────────────────

        bool firstFrameOnPlatform = false;
        if (hit.transform.CompareTag("MovingPlatform"))
        {
            if (!State.OnMovingPlatform)
                firstFrameOnPlatform = true;

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
        if (Mathf.Abs(hit.normal.x) > 0.1f)
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

        Vector2 slopeVec = new Vector2(hit.normal.y, -hit.normal.x);

        bool fromSlope = Mathf.Abs(State.FloorAngle) > 0.1f;
        if (newAngle != State.FloorAngle && !wasInAir && fromSlope)
        {
            float speed = Mathf.Abs(_core.Rb.velocity.x) * Mathf.Sign(_core.Rb.velocity.x);
            _core.Rb.velocity = slopeVec * speed;
        }

        if (newAngle != 0f && wasInAir)
        {
            float speedAlongSlope = Vector2.Dot(_core.Rb.velocity, slopeVec);
            _core.Rb.velocity = slopeVec * speedAlongSlope;
        }

        State.FloorAngle  = newAngle;
        State.FloorNormal = hit.normal;
        State.GroundPosition = hit.point;

        // ── Ground Snap ──────────────────────────────────────────────────────

        bool isFlat = Mathf.Abs(newAngle) < 1f;

        if (!_core.State.Climbing && _core.StateMachine.IsGrounded)
        {
            if (isFlat)
            {
                _core.Rb.velocity = new Vector2(_core.Rb.velocity.x, 0f);
#if UNITY_EDITOR
                if (Mathf.Abs(newAngle) > 0.1f)
                    Debug.LogWarning($"[Constraint] isFlat fired but newAngle={newAngle} — FloorAngle mismatch!");
#endif
            }
            else
            {
                float awayFromSurface = Vector2.Dot(_core.Rb.velocity, -hit.normal);
#if UNITY_EDITOR
                if (_core.Rb.velocity.sqrMagnitude > 0.001f)
                    Debug.Log($"[Constraint] slope vel={_core.Rb.velocity} normal={hit.normal} away={awayFromSurface:F3} normalVelCheck={Vector2.Dot(_core.Rb.velocity, hit.normal):F3}");
#endif
                if (awayFromSurface < 2.0f)
                {
                    float normalVel = Vector2.Dot(_core.Rb.velocity, hit.normal);
                    if (normalVel > 0f)
                        _core.Rb.velocity -= hit.normal * normalVel;

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
                float groundLen = State.IsCrouching ? Cfg.GroundLength / 2f : Cfg.GroundLength;
                RaycastHit2D gpHit1 = Physics2D.Raycast(transform.position + RaycastLeft  + HOffset, Vector2.down, groundLen, _core.Physics.GroundLayer);
                RaycastHit2D gpHit2 = Physics2D.Raycast(transform.position + RaycastRight + HOffset, Vector2.down, groundLen, _core.Physics.GroundLayer);

                var hitObjects = new System.Collections.Generic.List<GameObject>();
                if (gpHit1.collider != null) hitObjects.Add(gpHit1.transform.gameObject);
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

        State.Spinning    = false;
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
        State.OnConveyor       = null;

        if (State.FloorAngle != 0f && _core.Rb.velocity.y > 0f && !_core.StateMachine.IsAirborne)
            _core.Rb.velocity = new Vector2(_core.Rb.velocity.x, 0f);
        State.FloorAngle  = 0f;
        State.FloorNormal = Vector2.up;
    }

    // ─── Ceiling Corner Correction ───────────────────────────────────────────

    private void ApplyCornerCorrection()
    {
        var bounds        = _core.Collider.bounds;
        float startHeight = bounds.size.y / 2f + (_core.Rb.velocity.y * Time.fixedDeltaTime) + 0.01f + Cfg.CeilingCorrectionOffset.y;
        float halfWidth   = bounds.size.x / 2f;
        float rayLen      = halfWidth + Cfg.CeilingCorrectionRayLength;

        Vector3 origin = transform.position + new Vector3(Cfg.CeilingCorrectionOffset.x, startHeight, 0f);

        RaycastHit2D hitLeft  = Physics2D.Raycast(origin, Vector2.left,  rayLen, _core.Physics.GroundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(origin, Vector2.right, rayLen, _core.Physics.GroundLayer);

        if (hitLeft.collider == null && hitRight.collider == null) return;

        float distLeft  = hitLeft.collider  == null ? 999f : hitLeft.distance;
        float distRight = hitRight.collider == null ? 999f : hitRight.distance;
        float gapWidth  = (distLeft + distRight) - bounds.size.x;

        if (gapWidth < 0f) return;

        float playerLeft  = transform.position.x - halfWidth + Cfg.CeilingCorrectionThresholdOffset.x;
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
        var bounds        = _core.Collider.bounds;
        float startHeight = bounds.size.y / 2f + Mathf.Abs(_core.Rb.velocity.y * Time.fixedDeltaTime) + 0.01f;
        float halfWidth   = bounds.size.x / 2f;
        float rayLen      = halfWidth + Cfg.FloorCorrectionRayLength;

        Vector3 origin = transform.position + new Vector3(Cfg.FloorCorrectionOffset.x, -startHeight + Cfg.FloorCorrectionOffset.y, 0f);

        RaycastHit2D hitLeft  = Physics2D.Raycast(origin, Vector2.left,  rayLen, _core.Physics.GroundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(origin, Vector2.right, rayLen, _core.Physics.GroundLayer);

        if (hitLeft.collider == null || hitRight.collider == null) return;

        float distLeft  = hitLeft.distance;
        float distRight = hitRight.distance;
        float gapWidth  = (distLeft + distRight) - bounds.size.x;

        if (gapWidth < 0f) return;

        float playerLeft  = transform.position.x - halfWidth + Cfg.FloorCorrectionThresholdOffset.x;
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

        RaycastHit2D ceilLeft  = Physics2D.Raycast(transform.position + RaycastLeft  + HOffset, Vector2.up, ceilLen,          _core.Physics.GroundLayer);
        RaycastHit2D ceilMid   = Physics2D.Raycast(transform.position               + HOffset,  Vector2.up, Cfg.CeilingLength, _core.Physics.GroundLayer);
        RaycastHit2D ceilRight = Physics2D.Raycast(transform.position + RaycastRight + HOffset, Vector2.up, ceilLen,          _core.Physics.GroundLayer);

        if (ceilLeft.collider == null && ceilMid.collider == null && ceilRight.collider == null)
            return;

        RaycastHit2D solidHit = default;
        foreach (var hit in new[] { ceilLeft, ceilMid, ceilRight })
        {
            if (hit.collider == null) continue;
            if (hit.collider.TryGetComponent<PlatformEffector2D>(out _)) continue;
            if (hit.normal.y < -0.5f) { solidHit = hit; break; }
        }

        if (solidHit.collider == null) return;

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
        if (!enabled || core == null) return;

        var physics  = core.Physics  != null ? core.Physics  : core.GetComponent<MarioPhysics>();
        var collider = core.Collider != null ? core.Collider : core.GetComponentInChildren<BoxCollider2D>();
        var cfg      = physics != null ? physics.Config : null;
        if (cfg == null || collider == null) return;

        bool crouching = Application.isPlaying && core.State.IsCrouching;
        float groundLen = crouching ? cfg.GroundLength / 2f : cfg.GroundLength;
        float ceilLen   = crouching ? cfg.CeilingLength / 2f : cfg.CeilingLength;

        float sep    = cfg.RaycastSeparation;
        float offX   = cfg.RaycastOffsetX;
        Vector3 left  = new Vector3(-sep + offX, 0f, 0f);
        Vector3 right = new Vector3( sep + offX, 0f, 0f);
        Vector3 hoff  = new Vector3(offX, 0f, 0f);

        // Ground rays (red)
        Gizmos.color = Color.red;
        DrawRay(transform.position + left,  Vector2.down, groundLen);
        DrawRay(transform.position + hoff,  Vector2.down, groundLen);
        DrawRay(transform.position + right, Vector2.down, groundLen);

        // Ceiling rays (yellow)
        Gizmos.color = Color.yellow;
        DrawRay(transform.position + left,  Vector2.up, ceilLen);
        DrawRay(transform.position + hoff,  Vector2.up, cfg.CeilingLength);
        DrawRay(transform.position + right, Vector2.up, ceilLen);

        // Ceiling corner correction gizmos
        {
            float ccHalfW     = collider.size.x / 2f;
            float ccHalfH     = collider.size.y / 2f;
            float ccRayLen    = ccHalfW + cfg.CeilingCorrectionRayLength;
            Vector3 ccCenter  = transform.position + new Vector3(collider.offset.x, collider.offset.y);
            float topY        = ccCenter.y + ccHalfH + cfg.CeilingCorrectionOffset.y;
            float ccOriginX   = ccCenter.x + cfg.CeilingCorrectionOffset.x;
            float ccLeft      = ccCenter.x - ccHalfW + cfg.CeilingCorrectionThresholdOffset.x;
            float ccRight     = ccCenter.x + ccHalfW + cfg.CeilingCorrectionThresholdOffset.x;
            Vector3 topOrigin = new Vector3(ccOriginX, topY);

            Gizmos.color = (_ceilingCCFiredLeft || _ceilingCCFiredRight) ? Color.red : new Color(1f, 0.5f, 0f);
            DrawRay(topOrigin, Vector2.left,  ccRayLen);
            DrawRay(topOrigin, Vector2.right, ccRayLen);

            float ceilThreshY = topY + 0.05f + cfg.CeilingCorrectionThresholdOffset.y;
            Gizmos.color = _ceilingCCFiredLeft  ? Color.red : Color.green;
            Gizmos.DrawLine(new Vector3(ccLeft,                                   ceilThreshY),
                            new Vector3(ccLeft  - cfg.CeilingCorrectionThreshold, ceilThreshY));
            Gizmos.color = _ceilingCCFiredRight ? Color.red : Color.green;
            Gizmos.DrawLine(new Vector3(ccRight,                                  ceilThreshY),
                            new Vector3(ccRight + cfg.CeilingCorrectionThreshold, ceilThreshY));
        }

        // Floor corner correction gizmos
        {
            float vccHalfW      = collider.size.x / 2f;
            float vccHalfH      = collider.size.y / 2f;
            float vccRayLen     = vccHalfW + cfg.FloorCorrectionRayLength;
            Vector3 colCenter   = transform.position + new Vector3(collider.offset.x, collider.offset.y);
            float bottomY       = colCenter.y - vccHalfH + cfg.FloorCorrectionOffset.y;
            float vccOriginX    = colCenter.x + cfg.FloorCorrectionOffset.x;
            float leftEdge      = colCenter.x - vccHalfW + cfg.FloorCorrectionThresholdOffset.x;
            float rightEdge     = colCenter.x + vccHalfW + cfg.FloorCorrectionThresholdOffset.x;
            Vector3 bottomOrigin = new Vector3(vccOriginX, bottomY);

            Gizmos.color = (_verticalCCFiredLeft || _verticalCCFiredRight) ? Color.red : new Color(1f, 0.5f, 0f);
            DrawRay(bottomOrigin, Vector2.left,  vccRayLen);
            DrawRay(bottomOrigin, Vector2.right, vccRayLen);

            float floorThreshY = bottomY - 0.05f + cfg.FloorCorrectionThresholdOffset.y;
            Gizmos.color = _verticalCCFiredLeft  ? Color.red : Color.green;
            Gizmos.DrawLine(new Vector3(leftEdge,                                 floorThreshY),
                            new Vector3(leftEdge  - cfg.FloorCorrectionThreshold, floorThreshY));
            Gizmos.color = _verticalCCFiredRight ? Color.red : Color.green;
            Gizmos.DrawLine(new Vector3(rightEdge,                                floorThreshY),
                            new Vector3(rightEdge + cfg.FloorCorrectionThreshold, floorThreshY));
        }
    }

    private static void DrawRay(Vector3 origin, Vector2 dir, float len)
        => Gizmos.DrawLine(origin, origin + (Vector3)(dir * len));

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void ApplyConveyorBelt()
    {
        if (State.OnConveyor == null) return;
        _core.Rb.position += State.OnConveyor.Velocity * Time.fixedDeltaTime;
    }
}