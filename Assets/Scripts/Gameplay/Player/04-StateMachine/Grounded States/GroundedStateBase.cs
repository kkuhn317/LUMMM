using UnityEngine;

/// <summary>
/// Shared base for all grounded states: Idle, Walk, Run, Skid, Crouch, Crawl, Push.
///
/// Centralizes the transition checks that every grounded state needs:
/// - Fall off ledge → Fall
/// - Jump pressed   → Rise (or SpinJump / WallJump)
/// - Enter water    → SwimIdle
/// - Grab climbable → ClimbFront / ClimbSide
/// - Ground pound   → not valid while grounded (blocked by CanTransition)
///
/// Concrete states override FixedUpdate for their specific movement logic,
/// and can override CheckTransitions to add their own checks before calling base.
/// </summary>
public abstract class GroundedStateBase : MarioStateBase
{
    protected MarioPhysicsConfig Cfg => Core.Physics.Config;
    protected Rigidbody2D Rb => Core.Rb;

    private bool _justLanded;
    private bool _wasLookingUp;

    public override void Enter(string previousState)
    {
        Rb.gravityScale = 0f;
        State.OnGround = true;
        _justLanded = true;
    }

    public override void Exit(string nextState)
    {
        if (nextState == MarioStateID.Rise ||
            nextState == MarioStateID.Fall ||
            nextState == MarioStateID.SpinJump ||
            nextState == MarioStateID.WallJump)
        {
            Rb.gravityScale = Cfg.RiseGravity;
        }

        if (_wasLookingUp)
        {
            MarioEvents.FireLookUpEnded(PlayerIndex);
            State.IsLookingUp = false;
            _wasLookingUp = false;
        }
    }

    protected Vector2 GroundNormal =>
        State.FloorNormal.sqrMagnitude > 0.0001f ? State.FloorNormal.normalized : Vector2.up;

    protected Vector2 GroundTangent
    {
        get
        {
            Vector2 n = GroundNormal;
            return new Vector2(n.y, -n.x).normalized;
        }
    }

    protected float GroundSpeed => Vector2.Dot(Rb.velocity, GroundTangent);
    protected float GroundAbsSpeed => Mathf.Abs(GroundSpeed);

    protected void SetGroundSpeed(float speed)
    {
        Vector2 n = GroundNormal;
        Vector2 t = GroundTangent;

        float normalSpeed = Vector2.Dot(Rb.velocity, n);
        if (normalSpeed > 0f)
            normalSpeed = 0f;

        Rb.velocity = t * speed + n * normalSpeed;
    }

    protected Vector2 GetGroundMoveDir()
    {
        return GroundTangent;
    }

    public override void Update()
    {
        HandleLookUp();
    }

    private void HandleLookUp()
    {
        bool lookingUp = !HasHorizontalInput && IsPressingUp && !State.IsUsingObject
                      && !State.ClimbExitedWhilePressingUp;

        if (lookingUp && !_wasLookingUp)
            MarioEvents.FireLookUpStarted(PlayerIndex);
        else if (!lookingUp && _wasLookingUp)
            MarioEvents.FireLookUpEnded(PlayerIndex);

        State.IsLookingUp = lookingUp;
        _wasLookingUp = lookingUp;
    }

    public override void FixedUpdate()
    {
        ApplyGroundedDrag();
        ClampHorizontalSpeed();
        HandleFacing();
    }

    protected void ApplyGroundedDrag()
    {
        const float inputDeadzone = 0.1f;
        const float normalStopSpeed = 0.25f;

        float horizontal = State.Direction.x;
        bool holding = Mathf.Abs(horizontal) > inputDeadzone && !IsChangingDirection;
        float spd = GroundAbsSpeed;

        bool onSlope = Mathf.Abs(State.FloorAngle) > 0.1f;
        bool shouldPinToSlope = onSlope
                            && !holding
                            && State.OnConveyor == null
                            && !State.OnMovingPlatform;

        if (shouldPinToSlope)
        {
            SetGroundSpeed(0f);

            Vector2 n = GroundNormal;
            float normalSpeed = Vector2.Dot(Rb.velocity, n);
            if (Mathf.Abs(normalSpeed) < normalStopSpeed)
                Rb.velocity -= n * normalSpeed;

            Rb.drag = 0f;
            _justLanded = false;
            return;
        }

        if (_justLanded)
            _justLanded = false;

        if (holding)
        {
            Rb.drag = 0f;
            return;
        }

        Rb.drag = spd switch
        {
            < 0.5f => 100_000_000f,
            < 5f => 8f / Mathf.Max(spd, 0.01f),
            _ => 1.5f
        };

        if (State.IsCrouching || (!State.FacingRight && GroundSpeed > 0f) || (State.FacingRight && GroundSpeed < 0f))
            Rb.drag *= 1.5f;

        if (Rb.drag > 100_000_000f)
            Rb.drag = 100_000_000f;
    }

    protected void ClampHorizontalSpeed()
    {
        // ONLY hard-clamp the absolute maximum run speed ceiling.
        // If Mario is walking but currently moving faster than walk speed (coasting),
        // WalkState's SlowDownForce will handle braking him smoothly.
        float maxSpd = Cfg.MaxRunSpeed; 
        float speed = GroundSpeed;

        if (Mathf.Abs(speed) > maxSpd)
            SetGroundSpeed(Mathf.Sign(speed) * maxSpd);
    }

    protected void HandleFacing()
    {
        float horizontal = State.Direction.x;
        if (!State.IsCapeActive && horizontal != 0f)
            Core.Physics.FlipTo(horizontal > 0f);
    }

    protected bool HasCeilingObstruction()
    {
        var col = Core.Collider;

        float crouchedTop = Core.Rb.position.y + col.offset.y + col.size.y * 0.5f;

        float standHeight = Core.ColliderOriginalHeight;
        float crouchHeight = Cfg.CrouchColliderHeight;
        float neededClearance = (standHeight - crouchHeight) + 0.05f;

        Vector2 origin = new Vector2(Core.Rb.position.x + Cfg.CeilingProbeOffsetX, crouchedTop);
        Vector2 originLeft = origin + new Vector2(-Cfg.CeilingProbeSeparation, 0f);
        Vector2 originRight = origin + new Vector2(Cfg.CeilingProbeSeparation, 0f);

        LayerMask groundLayer = Core.Physics.GroundLayer;

        RaycastHit2D hitLeft = Physics2D.Raycast(originLeft, Vector2.up, neededClearance, groundLayer);
        RaycastHit2D hitCenter = Physics2D.Raycast(origin, Vector2.up, neededClearance, groundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(originRight, Vector2.up, neededClearance, groundLayer);

    #if UNITY_EDITOR
        Color rayColor = (hitLeft.collider || hitCenter.collider || hitRight.collider)
            ? Color.red : Color.green;
        Debug.DrawRay(originLeft, Vector2.up * neededClearance, rayColor);
        Debug.DrawRay(origin, Vector2.up * neededClearance, rayColor);
        Debug.DrawRay(originRight, Vector2.up * neededClearance, rayColor);
    #endif

        return hitLeft.collider != null
            || hitCenter.collider != null
            || hitRight.collider != null;
    }

    public override void CheckTransitions()
    {
        if (!State.OnGround)
        {
            RequestTransition(MarioStateID.Fall);
            return;
        }

        if (State.Swimming)
        {
            RequestTransition(MarioStateID.SwimIdle);
            return;
        }

        if (!IsPressingDown)
            State.ClimbExitedWhilePressingDown = false;
        if (!IsPressingUp)
            State.ClimbExitedWhilePressingUp = false;

        if (!State.Climbing && State.CurrentClimbable != null
            && !State.ClimbExitedWhilePressingDown
            && !State.JustLeftClimbing
            && Mathf.Abs(State.Direction.y) > 0.5f
            && Time.time >= State.JumpTimer)
        {
            var method = State.CurrentClimbable.climbMethod;
            RequestTransition(method == Climbable.ClimbMethod.Side
                ? MarioStateID.ClimbSide
                : MarioStateID.ClimbFront);
            return;
        }

        if (Time.time < State.JumpTimer && !Machine.IsJumpBlocked())
        {
            if (State.SpinJumpQueued && State.CanSpinJump)
            {
#if UNITY_EDITOR
                Debug.Log($"[Jump] Transitioning to SpinJump. SpinQueued={State.SpinJumpQueued} CanSpinJump={State.CanSpinJump}");
#endif
                RequestTransition(MarioStateID.SpinJump);
                return;
            }

            RequestTransition(MarioStateID.Rise);
            return;
        }
    }
}