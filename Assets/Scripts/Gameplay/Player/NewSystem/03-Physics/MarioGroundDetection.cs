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

        // Use the hit normal to detect slope — more reliable than the stale FloorAngle
        // because it reflects the actual surface this frame.
        // On a slope, uphill movement produces positive velocity.y which must not
        // be treated as airborne. We allow it whenever the surface is angled.
        // The centre ray is especially important on slope seams where the left/right
        // rays may straddle two different collider triangles and give inconsistent normals.
        bool anyHit  = hit1Valid || hitCValid || hit2Valid;
        bool onSlope = false;
        if (anyHit)
        {
            // Pick the highest hit point among all three rays as the best representative
            RaycastHit2D bestHit = default;
            float bestY = float.MinValue;
            if (hit1Valid && hit1.point.y > bestY) { bestHit = hit1; bestY = hit1.point.y; }
            if (hitCValid && hitC.point.y > bestY) { bestHit = hitC; bestY = hitC.point.y; }
            if (hit2Valid && hit2.point.y > bestY) { bestHit = hit2; }
            onSlope = Mathf.Abs(bestHit.normal.x) > 0.1f;
        }
        // goingUp: true when velocity has meaningful upward component.
        // On a slope, uphill movement produces positive vel.y naturally (e.g. vel=(3.5,3.5))
        // so we must not treat that as "jumping". Instead we check whether the velocity
        // is moving AWAY from the surface (outward along the normal). If it is large,
        // Mario genuinely jumped; if small/negative, it's just slope movement.
        // We approximate this with the raw vel.y threshold only on flat ground,
        // and use a higher threshold on slopes to avoid killing uphill walking.
        bool goingUp;
        if (onSlope && anyHit)
        {
            // Pick best hit for normal check
            RaycastHit2D slopeHit = default;
            float sY = float.MinValue;
            if (hit1Valid && hit1.point.y > sY) { slopeHit = hit1; sY = hit1.point.y; }
            if (hitCValid && hitC.point.y > sY) { slopeHit = hitC; sY = hitC.point.y; }
            if (hit2Valid && hit2.point.y > sY)    slopeHit = hit2;
            // Moving away from surface along normal = real jump. Threshold 1.0 filters jitter.
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

        // Return the highest hit among all valid rays — on a slope this is the
        // point closest to Mario's feet, giving the most accurate surface normal.
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

        // ConveyorBelt is on the parent — the raycast hits a child collider (left/middle/right).
        // Walk up the hierarchy to find it.
        State.OnConveyor = hit.transform.GetComponentInParent<ConveyorBelt>();

        // ── Slope ────────────────────────────────────────────────────────────

        // Derive the angle from the hit surface normal — works on any surface
        // automatically, no "Slope" tag needed. Flat ground normal is Vector2.up
        // (angle = 0). Angled surfaces produce a rotated normal we convert to degrees.
        // The Slope component can still override for manual tuning if present.
        float newAngle = 0f;
        if (Mathf.Abs(hit.normal.x) > 0.1f)  // must match the onSlope threshold in CheckGround —
        {                                      // both gates must agree or a surface is treated as flat
                                               // for OnGround but angled for projection, corrupting vel.x
            newAngle = Vector2.SignedAngle(hit.normal, Vector2.up) * Mathf.Sign(hit.normal.x);

            // Slope component override (optional manual tuning)
            if (hit.transform.CompareTag("Slope")
                && hit.transform.TryGetComponent(out Slope slope))
            {
                newAngle = slope.angle;
            }
        }

        // Too steep to walk on — treat as airborne so Mario slides off
        if (Mathf.Abs(newAngle) > Cfg.MaxWalkableAngle)
        {
            State.OnGround = false;
            return;
        }

#if UNITY_EDITOR
        // Debug.Log($"[Slope] normal={hit.normal} newAngle={newAngle:F1} FloorAngle={State.FloorAngle:F1} vel={_core.Rb.velocity} OnGround={State.OnGround}");
#endif

        // Derive tangent from actual hit.normal — rotate 90° clockwise:
        // normal=(nx,ny) → right-tangent=(ny,-nx). Correct for any slope direction.
        Vector2 slopeVec = new Vector2(hit.normal.y, -hit.normal.x);

        // Transition between slopes (slope-to-slope only): reproject velocity onto new slope.
        // Skip when coming from flat (FloorAngle==0) — the velocity constraint below
        // handles that naturally. Reprojecting flat→slope introduces a negative-Y lurch
        // that causes the visible hesitation when Mario walks onto a slope from flat ground.
        bool fromSlope = Mathf.Abs(State.FloorAngle) > 0.1f;
        if (newAngle != State.FloorAngle && !wasInAir && fromSlope)
        {
            float speed = Mathf.Abs(_core.Rb.velocity.x) * Mathf.Sign(_core.Rb.velocity.x);
            _core.Rb.velocity = slopeVec * speed;
        }

        // Landing on a slope: reproject velocity onto the slope tangent so
        // gravity-induced Y doesn't persist and trigger a false goingUp.
        // Use dot product along the tangent rather than velocity.x alone —
        // velocity.x misses the Y contribution on diagonal landing trajectories.
        if (newAngle != 0f && wasInAir)
        {
            float speedAlongSlope = Vector2.Dot(_core.Rb.velocity, slopeVec);
            _core.Rb.velocity = slopeVec * speedAlongSlope;
        }

        State.FloorAngle  = newAngle;
        State.FloorNormal = hit.normal; // stored so WalkRunState can derive correct tangent
        State.GroundPosition = hit.point;

        // ── Ground Snap ──────────────────────────────────────────────────────

        bool isFlat = Mathf.Abs(newAngle) < 1f;

        // Flat-ground snap removed: gravityScale is 0 while grounded so Mario
        // cannot sink into the surface without it. Writing rb.position every frame
        // caused a physics discontinuity on off-grid surfaces (fractional hit.point.y)
        // that bled horizontal velocity no matter how the condition was guarded.
        // Slopes use the stick force below instead.

        // Velocity constraint while grounded:
        // Flat  → kill Y so Mario doesn't sink or float.
        // Slope → kill only the component perpendicular to the surface (the normal),
        //         which glues Mario to the slope without affecting how fast he moves
        //         along it in either direction. This replaces the old projection onto
        //         slopeVec which was direction-dependent and destroyed uphill movement.
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
                if (awayFromSurface < 2.0f) // raised from 0.5 so sprint entry doesn't skip the constraint
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
                // Collect all unique hit objects from both raycasts so that
                // landing between two blocks bumps both of them.
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

        // FIX: Clear FloorAngle when truly airborne so the Y-velocity gate
        // works normally on flat ground again after leaving a slope.
        // Also kill residual upward Y from slope tangent when walking off the top —
        // otherwise Fall state inherits it and Mario briefly floats upward.
        if (State.FloorAngle != 0f && _core.Rb.velocity.y > 0f && !_core.StateMachine.IsAirborne)
            _core.Rb.velocity = new Vector2(_core.Rb.velocity.x, 0f);
        State.FloorAngle  = 0f;
        State.FloorNormal = Vector2.up; // reset to flat so GetSlopeMoveDir returns Vector2.right
    }

    // ─── Corner Correction ───────────────────────────────────────────────────

    private void ApplyCornerCorrection()
    {
        var bounds        = _core.Collider.bounds;
        float startHeight = bounds.size.y / 2f + (_core.Rb.velocity.y * Time.fixedDeltaTime) + 0.01f;
        float halfWidth   = bounds.size.x / 2f;
        float rayLen      = halfWidth * 1.1f;

        Vector3 origin = transform.position + new Vector3(0f, startHeight, 0f);

        RaycastHit2D hitLeft  = Physics2D.Raycast(origin, Vector2.left,  rayLen, _core.Physics.GroundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(origin, Vector2.right, rayLen, _core.Physics.GroundLayer);

        if (hitLeft.collider == null && hitRight.collider == null) return;

        float distLeft  = hitLeft.collider  == null ? 999f : hitLeft.distance;
        float distRight = hitRight.collider == null ? 999f : hitRight.distance;
        float gapWidth  = (distLeft + distRight) - bounds.size.x;

        if (gapWidth < 0f) return;

        float playerLeft  = transform.position.x - halfWidth;
        float playerRight = transform.position.x + halfWidth;

        if (hitLeft.collider != null
            && hitLeft.point.x < playerLeft + Cfg.CornerCorrection
            && hitLeft.point.x > playerLeft)
        {
            Debug.Log($"[CornerCorrect] Nudge right (ceiling) | contact={hitLeft.point.x:F3} playerLeft={playerLeft:F3} threshold={Cfg.CornerCorrection:F3}");
            _ceilingCCFiredLeft = true;
            _core.Rb.position = new Vector2(hitLeft.point.x + halfWidth * 1.2f, _core.Rb.position.y);
        }
        else if (hitRight.collider != null
            && hitRight.point.x > playerRight - Cfg.CornerCorrection
            && hitRight.point.x < playerRight)
        {
            Debug.Log($"[CornerCorrect] Nudge left (ceiling) | contact={hitRight.point.x:F3} playerRight={playerRight:F3} threshold={Cfg.CornerCorrection:F3}");
            _ceilingCCFiredRight = true;
            _core.Rb.position = new Vector2(hitRight.point.x - halfWidth * 1.2f, _core.Rb.position.y);
        }
    }

    // ─── Vertical Corner Correction ─────────────────────────────────────────
    /// <summary>
    /// Exact mirror of ApplyCornerCorrection but for the bottom of Mario.
    /// When falling and a bottom corner clips a wall edge, nudges Mario
    /// horizontally so he slides into the gap. Uses the same CornerCorrection
    /// threshold and ray length logic as the ceiling version.
    /// </summary>
    private void ApplyVerticalCornerCorrection()
    {
        var bounds        = _core.Collider.bounds;
        float startHeight = bounds.size.y / 2f + Mathf.Abs(_core.Rb.velocity.y * Time.fixedDeltaTime) + 0.01f;
        float halfWidth   = bounds.size.x / 2f;
        float rayLen      = halfWidth * 1.1f;

        // Origin at bottom of Mario (negative Y — mirror of ceiling's positive Y)
        Vector3 origin = transform.position + new Vector3(0f, -startHeight, 0f);

        RaycastHit2D hitLeft  = Physics2D.Raycast(origin, Vector2.left,  rayLen, _core.Physics.GroundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(origin, Vector2.right, rayLen, _core.Physics.GroundLayer);

        // Both sides must hit — only then is there a gap to slide into
        if (hitLeft.collider == null || hitRight.collider == null) return;

        float distLeft  = hitLeft.distance;
        float distRight = hitRight.distance;
        float gapWidth  = (distLeft + distRight) - bounds.size.x;

        if (gapWidth < 0f) return;
        float playerLeft  = transform.position.x - halfWidth;
        float playerRight = transform.position.x + halfWidth;

        if (hitLeft.collider != null
            && hitLeft.point.x > playerLeft - Cfg.CornerCorrection
            && hitLeft.point.x < playerLeft)
        {
            Debug.Log($"[VCornerCorrect] Nudge right | gapWidth={gapWidth:F3} leftContact={hitLeft.point.x:F3} playerLeft={playerLeft:F3}");
            _verticalCCFiredLeft = true;
            _core.Rb.position = new Vector2(hitLeft.point.x + halfWidth * 1.2f, _core.Rb.position.y);
        }
        else if (hitRight.collider != null
            && hitRight.point.x < playerRight + Cfg.CornerCorrection
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
        // Works in both edit mode and play mode
        var core = _core != null ? _core : GetComponent<MarioCore>();
        if (!enabled || core == null) return;

        // Physics and Collider are set in Awake — fall back to GetComponent in edit mode
        var physics  = core.Physics  != null ? core.Physics  : core.GetComponent<MarioPhysics>();
        var collider = core.Collider != null ? core.Collider : core.GetComponentInChildren<BoxCollider2D>();
        var cfg      = physics != null ? physics.Config : null;
        if (cfg == null || collider == null) return;

        bool crouching = Application.isPlaying && core.State.IsCrouching;
        float groundLen = crouching ? cfg.GroundLength / 2f : cfg.GroundLength;
        float ceilLen   = crouching ? cfg.CeilingLength / 2f : cfg.CeilingLength;

        // Recompute offsets from config (runtime properties unavailable in edit mode)
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

        // Ceiling corner correction gizmos — orange rays + green threshold at top
        {
            float ccHalfW    = collider.size.x / 2f;
            float ccHalfH    = collider.size.y / 2f;
            float ccRayLen   = ccHalfW * 1.1f;
            Vector3 ccCenter = transform.position + new Vector3(collider.offset.x, collider.offset.y);
            float topY       = ccCenter.y + ccHalfH;
            float ccLeft     = ccCenter.x - ccHalfW;
            float ccRight    = ccCenter.x + ccHalfW;
            Vector3 topOrigin = new Vector3(ccCenter.x, topY);

            Gizmos.color = (_ceilingCCFiredLeft || _ceilingCCFiredRight) ? Color.red : new Color(1f, 0.5f, 0f);
            DrawRay(topOrigin, Vector2.left,  ccRayLen);
            DrawRay(topOrigin, Vector2.right, ccRayLen);

            Gizmos.color = _ceilingCCFiredLeft  ? Color.red : Color.green;
            Gizmos.DrawLine(new Vector3(ccLeft,                         topY + 0.05f),
                            new Vector3(ccLeft  - cfg.CornerCorrection,  topY + 0.05f));
            Gizmos.color = _ceilingCCFiredRight ? Color.red : Color.green;
            Gizmos.DrawLine(new Vector3(ccRight,                        topY + 0.05f),
                            new Vector3(ccRight + cfg.CornerCorrection,  topY + 0.05f));
        }

        // Vertical corner correction gizmos — exact mirror of ceiling gizmos
        float vccHalfW       = collider.size.x / 2f;
        float vccHalfH       = collider.size.y / 2f;
        float vccRayLen      = vccHalfW * 1.1f;
        Vector3 colCenter    = transform.position + new Vector3(collider.offset.x, collider.offset.y);
        float bottomY        = colCenter.y - vccHalfH;
        float leftEdge       = colCenter.x - vccHalfW;
        float rightEdge      = colCenter.x + vccHalfW;
        Vector3 bottomOrigin = new Vector3(colCenter.x, bottomY);

        // Orange rays from bottom center — left and right (mirrors ceiling)
        Gizmos.color = new Color(1f, 0.5f, 0f);
        DrawRay(bottomOrigin, Vector2.left,  vccRayLen);
        DrawRay(bottomOrigin, Vector2.right, vccRayLen);

        // Green threshold markers — show how far OUTSIDE each edge the correction zone extends
        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            new Vector3(leftEdge,                         bottomY - 0.05f),
            new Vector3(leftEdge  - cfg.CornerCorrection, bottomY - 0.05f));
        Gizmos.DrawLine(
            new Vector3(rightEdge,                         bottomY - 0.05f),
            new Vector3(rightEdge + cfg.CornerCorrection,  bottomY - 0.05f));
    }

    private static void DrawRay(Vector3 origin, Vector2 dir, float len)
        => Gizmos.DrawLine(origin, origin + (Vector3)(dir * len));

    // ─── Helpers ─────────────────────────────────────────────────────────────

    // ── Conveyor Belt ────────────────────────────────────────────────────────

    /// <summary>
    /// Applies the conveyor belt effect as a direct position offset, completely
    /// separate from Mario's own physics velocity. This means it can never
    /// accumulate — Mario's walk force, drag, and jump all work on clean velocity,
    /// and the belt just moves him in world space on top of that every frame.
    /// </summary>
    private void ApplyConveyorBelt()
    {
        if (State.OnConveyor == null) return;

        _core.Rb.position += State.OnConveyor.Velocity * Time.fixedDeltaTime;
    }
}