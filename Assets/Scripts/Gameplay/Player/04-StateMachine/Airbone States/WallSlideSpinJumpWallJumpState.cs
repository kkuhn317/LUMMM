using UnityEngine;

/// <summary>
/// Mario is sliding down a wall.
///
/// Physics: horizontal velocity zeroed, terminal velocity divided by 3.
/// Hold-timer: if player holds away from wall long enough, exit wall slide.
///
/// Transitions out to:
/// - WallJump : jump pressed
/// - Fall     : released from wall (timer expired or no longer touching)
/// - Grounded : landed (base)
/// - SwimIdle : entered water (base)
/// </summary>
public class WallSlideState : AirborneStateBase
{
    public override string ID => MarioStateID.WallSlide;
    public override System.Collections.Generic.IEnumerable<string> Tags => new[] { MarioStateTags.Airborne };

    private float _wallJumpHoldTimer;

    public override void Enter(string previousState)
    {
        base.Enter(previousState);

        State.WallSliding = true;
        State.Spinning    = false;
        // Can't be crouched while wall sliding — restore collider
        if (State.IsCrouching)
        {
            State.IsCrouching = false;
            var col = Core.Collider;
            col.size   = new Vector2(col.size.x, Core.ColliderOriginalHeight);
            col.offset = new Vector2(col.offset.x, Core.ColliderOriginalOffsetY);
            MarioEvents.FireCrouchEnded(PlayerIndex);
        }

        // Face the wall
        Core.Physics.FlipTo(State.Direction.x > 0f);

        _wallJumpHoldTimer = Time.time + Cfg.WallJumpHoldTime;

        MarioEvents.FireWallSlideStarted(PlayerIndex);
    }

    public override void Exit(string nextState)
    {
        State.WallSliding = false;
        MarioEvents.FireWallSlideEnded(PlayerIndex);
    }

    public override void FixedUpdate()
    {
        // Kill horizontal velocity — stuck to wall
        Rb.velocity     = new Vector2(0f, Rb.velocity.y);
        Rb.gravityScale = Cfg.FallGravity;
        Rb.drag         = 0f;

        // Terminal velocity is much slower on the wall
        ClampFallSpeed(Cfg.TerminalVelocity / 3f);

        // Hold-away timer: if pressing away from wall, start countdown
        float h = State.Direction.x;
        bool holdingWall = h == 0f
            || (State.FacingRight && h > 0f)
            || (!State.FacingRight && h < 0f);

        if (holdingWall)
            _wallJumpHoldTimer = Time.time + Cfg.WallJumpHoldTime;
    }

    public override void CheckTransitions()
    {
        // Jump or spin pressed → wall jump (spin variant if spin queued)
        if (Time.time < State.JumpTimer)
        {
            if (State.SpinJumpQueued && State.CanSpinJump)
                State.Spinning = true; // flag so WallJumpState knows it's a spin wall jump
            RequestTransition(MarioStateID.WallJump);
            return;
        }

        // Held away from wall too long → fall
        if (Time.time > _wallJumpHoldTimer)
        {
            RequestTransition(MarioStateID.Fall);
            return;
        }

        // No longer touching the wall
        if (!Core.WallDetection.CheckWall(State.FacingRight))
        {
            RequestTransition(MarioStateID.Fall);
            return;
        }

        base.CheckTransitions(); // landed, water, climb
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Mario performed a spin jump from the ground or a climbable.
/// Shorter airtime (0.6x), spinning flag set for spin-bounce interactions.
///
/// Transitions out to:
/// - Fall     : apex reached
/// - MidairSpin : spin pressed mid-air (if eligible)
/// - Grounded : landed (base)
/// </summary>
public class SpinJumpState : AirborneStateBase
{
    public override string ID => MarioStateID.SpinJump;
    public override System.Collections.Generic.IEnumerable<string> Tags => new[] { MarioStateTags.Airborne };

    // Grace period after launch during which the short-hop cut cannot fire.
    // Without this, tapping the spin button instead of holding it collapses
    // the spin jump to a tiny hop on the very next FixedUpdate because
    // jumpHeld is already false by the time ApplyRiseGravity runs.
    private float _jumpGraceTime;

    public override void Enter(string previousState)
    {
        base.Enter(previousState);

        // Reset physics in case we came from climbing
        Rb.bodyType     = RigidbodyType2D.Dynamic;
        Rb.gravityScale = Cfg.RiseGravity;
        Rb.drag         = 0f;

        Rb.velocity  = new Vector2(Rb.velocity.x, 0f);
        Core.GroundDetection.SkipConstraintsThisFrame = true;
        Rb.AddForce(Vector2.up * Cfg.JumpSpeed * Cfg.SpinMultiplier, ForceMode2D.Impulse);

        State.AirTimer   = Time.time + Cfg.SpinJumpAirtime;
        _jumpGraceTime   = Time.time + 0.1f; // 100 ms — prevents short-hop cut on a tap
        State.JumpTimer  = 0f;
        State.Spinning   = true;
        // Spin jump exits crouch — restore collider
        if (State.IsCrouching)
        {
            State.IsCrouching = false;
            var col = Core.Collider;
            col.size   = new Vector2(col.size.x, Core.ColliderOriginalHeight);
            col.offset = new Vector2(col.offset.x, Core.ColliderOriginalOffsetY);
            MarioEvents.FireCrouchEnded(PlayerIndex);
        }
        State.OnGround   = false;
        State.MidairSpinUsedThisJump = false;
        State.LastMidairSpinTime     = -999f;

        Core.NotifyAbilities(a => a.onSpinPressed());

        MarioEvents.FireSpinJumped(PlayerIndex);
    }

    public override void Exit(string nextState)
    {
        // Keep Spinning true if going to Fall — so the spin animation
        // continues through the arc until landing clears it.
        if (nextState != MarioStateID.Fall)
            State.Spinning = false;
    }

    public override void FixedUpdate()
    {
        CheckCeilingBonk();
        ApplyRiseGravity();
        ApplyAirHorizontal();
        ClampFallSpeed();
    }

    public override void CheckTransitions()
    {
        if (IsPressingDown && State.CanGroundPound && !State.WallSliding && !State.JumpedWhileCrouching)
        {
            RequestTransition(MarioStateID.GroundPoundSpin);
            return;
        }

        if (Rb.velocity.y <= 0f)
        {
            RequestTransition(MarioStateID.Fall);
            return;
        }

        base.CheckTransitions();
    }

    private void ApplyRiseGravity()
    {
        bool airTimerActive = State.AirTimer > Time.time;
        bool jumpHeld       = State.SpinHeld;

        if (!airTimerActive || Rb.velocity.y < Cfg.StartFallingSpeed)
            Rb.gravityScale = Rb.velocity.y > 0f ? Cfg.PeakGravity : Cfg.FallGravity;
        else if (Rb.velocity.y > 0f && !jumpHeld && Time.time > _jumpGraceTime)
        {
            // Only allow short-hop cut after the grace period — prevents a tap
            // from collapsing the spin jump to a tiny hop immediately after launch.
            Rb.gravityScale = Cfg.FallGravity;
            State.AirTimer  = Time.time - 1f;
        }
        else
            Rb.gravityScale = Cfg.RiseGravity;
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Mario jumped off a wall. Horizontal impulse pushes him away from the wall.
/// 75% of normal jump height.
///
/// Transitions out to: same as RiseState.
/// </summary>
public class WallJumpState : AirborneStateBase
{
    public override string ID => MarioStateID.WallJump;
    public override System.Collections.Generic.IEnumerable<string> Tags => new[] { MarioStateTags.Airborne };

    public override void Enter(string previousState)
    {
        base.Enter(previousState);

        Rb.gravityScale = Cfg.RiseGravity;

        bool isSpinWallJump = State.Spinning && State.CanSpinJump;
        if (!isSpinWallJump) State.Spinning = false;

        // Vertical impulse (75% of normal, spin uses SpinMultiplier on top)
        bool useWalkSpeed = Mathf.Abs(Rb.velocity.x) > Cfg.WalkJumpSpeedRequired;
        float baseSpeed   = useWalkSpeed ? Cfg.WalkJumpSpeed : Cfg.JumpSpeed;
        float speed       = baseSpeed * 0.75f * (isSpinWallJump ? Cfg.SpinMultiplier : 1f);

        Rb.velocity = new Vector2(Rb.velocity.x, 0f);
        Rb.AddForce(Vector2.up * speed, ForceMode2D.Impulse);

        // Horizontal impulse away from wall
        int   wallDir = State.FacingRight ? -1 : 1;
        Rb.AddForce(Vector2.right * wallDir * Cfg.JumpSpeed, ForceMode2D.Impulse);

        // Flip to face away from wall
        Core.Physics.FlipTo(!State.FacingRight);

        State.AirTimer              = Time.time + (isSpinWallJump ? Cfg.SpinJumpAirtime : Cfg.WalkJumpAirtime) * 0.75f;
        State.JumpTimer             = 0f;
        State.MidairSpinUsedThisJump = false;
        State.LastMidairSpinTime    = -999f;
        State.OnGround              = false;

        MarioEvents.FireWallJumped(PlayerIndex);
    }

    public override void FixedUpdate()
    {
        CheckCeilingBonk();
        ApplyRiseGravity();
        ApplyAirHorizontal();
        ClampFallSpeed();
    }

    public override void CheckTransitions()
    {
        if (IsPressingDown && State.CanGroundPound)
        {
            RequestTransition(MarioStateID.GroundPoundSpin);
            return;
        }

        if (Rb.velocity.y <= 0f)
        {
            RequestTransition(MarioStateID.Fall);
            return;
        }

        base.CheckTransitions();
    }

    private void ApplyRiseGravity()
    {
        bool airTimerActive = State.AirTimer > Time.time;
        bool jumpHeld       = State.JumpPressed;

        if (!airTimerActive || Rb.velocity.y < Cfg.StartFallingSpeed)
            Rb.gravityScale = Rb.velocity.y > 0f ? Cfg.PeakGravity : Cfg.FallGravity;
        else if (Rb.velocity.y > 0f && !jumpHeld)
        {
            Rb.gravityScale = Cfg.FallGravity;
            State.AirTimer  = Time.time - 1f;
        }
        else
            Rb.gravityScale = Cfg.RiseGravity;
    }
}