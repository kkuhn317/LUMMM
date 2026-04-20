using UnityEngine;

/// <summary>
/// Mario is rising after a jump (air timer still active, jump held).
///
/// Gravity rules (from original ModifyPhysics):
/// - While air timer active AND jump held:  riseGravity (near-zero, sustain the jump)
/// - Jump released early:                   fallGravity immediately (short hop)
/// - Air timer expired, still moving up:    peakGravity (smooth apex)
/// - Moving down:                           fallGravity
///
/// Transitions out to:
/// - Fall       : velocity goes negative or jump released
/// - MidairSpin : spin pressed (if eligible)
/// - GroundPoundSpin : down pressed (if canGroundPound)
/// - WallSlide  : touching wall while falling (checked in FallState)
/// - Grounded   : landed (base)
/// - SwimIdle   : entered water (base)
/// </summary>
public class RiseState : AirborneStateBase
{
    public override string ID => MarioStateID.Rise;
    public override System.Collections.Generic.IEnumerable<string> Tags => new[] { MarioStateTags.Airborne };

    public override void Enter(string previousState)
    {
        bool wasOnGround = State.OnGround;
        base.Enter(previousState);

        // Execute the jump impulse
        bool useWalkSpeed = Mathf.Abs(Rb.velocity.x) > Cfg.WalkJumpSpeedRequired;
        float speed       = useWalkSpeed ? Cfg.WalkJumpSpeed : Cfg.JumpSpeed;

        Rb.velocity = new Vector2(Rb.velocity.x, 0f);
        Rb.AddForce(Vector2.up * speed, ForceMode2D.Impulse);

        State.AirTimer              = Time.time + (useWalkSpeed ? Cfg.WalkJumpAirtime : Cfg.Airtime);
        State.JumpTimer             = 0f;
        State.MidairSpinUsedThisJump = false;
        State.LastMidairSpinTime    = -999f;
        State.OnGround              = false;

        // Fire jumped for normal, coyote, and climb jumps.
        // Suppress if this is a bounce from a trampoline or note block.
        bool isCoyoteJump = previousState == MarioStateID.Fall && State.AirTimer > Time.time;
        bool isClimbJump  = previousState == MarioStateID.ClimbFront || previousState == MarioStateID.ClimbSide;
        if (!State.Spinning && !State.IsBounced && (wasOnGround || isCoyoteJump || isClimbJump))
            MarioEvents.FireJumped(PlayerIndex);
        State.IsBounced = false; // always clear after reading
    }

    public override void FixedUpdate()
    {
        ApplyRiseGravity();
        ApplyAirHorizontal();
        ClampFallSpeed();
    }

    public override void CheckTransitions()
    {
        // Ground pound - only if allowed and not already in a state that blocks it
        if (State.CanGroundPound && IsPressingDown && !State.IsCrouching 
            && State.CurrentClimbable == null)
        {
            RequestTransition(MarioStateID.GroundPoundSpin);
            return;
        }

        // Midair spin logic
        if (CanStartMidairSpin())
        {
            State.SpinPressed = false;
            RequestTransition(MarioStateID.MidairSpin);
            return;
        }

        // Wall slide: moving downward, pressing into a wall
        if (Rb.velocity.y < 0f && State.Direction.x != 0f && !State.Pushing)
        {
            bool checkRight = State.Direction.x > 0f;
            if (Core.WallDetection.CheckWall(checkRight))
            {
                RequestTransition(MarioStateID.WallSlide);
                return;
            }
        }

        // This call now handles the unified vertical-only climbing check
        base.CheckTransitions(); 
    }
    // ─── Gravity ─────────────────────────────────────────────────────────────

    private void ApplyRiseGravity()
    {
        bool airTimerActive = State.AirTimer > Time.time;
        bool jumpHeld = State.Spinning
            ? State.SpinHeld // spin jump: hold spin button to sustain
            : State.JumpPressed; // regular jump: hold jump button to sustain

        if (!airTimerActive || Rb.velocity.y < Cfg.StartFallingSpeed)
        {
            // Apex or beyond: use peak gravity while still going up
            Rb.gravityScale = Rb.velocity.y > 0f ? Cfg.PeakGravity : Cfg.FallGravity;
        }
        else if (Rb.velocity.y > 0f && !jumpHeld)
        {
            // Jump released early — cut to fall gravity immediately (short hop)
            Rb.gravityScale  = Cfg.FallGravity;
            State.AirTimer   = Time.time - 1f; // Expire timer
        }
        else
        {
            Rb.gravityScale = Cfg.RiseGravity;
        }
    }

    private bool CanStartMidairSpin()
    {
        if (!State.CanMidairSpin)           return false;
        if (State.IsMidairSpinning)         return false;
        if (State.Spinning)                 return false;
        if (!State.SpinPressed)             return false; // checked on press, not hold

        if (!State.AllowMultipleMidairSpins)
            return !State.MidairSpinUsedThisJump;

        bool firstSpin   = !State.MidairSpinUsedThisJump;
        bool cooldownOk  = Time.time >= State.LastMidairSpinTime + Cfg.MidairSpinCooldown;
        return firstSpin || cooldownOk;
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Mario is falling (velocity negative, or walked off a ledge).
///
/// Gravity: fallGravity always, unless entering via ledge walk-off
/// in which case walkJumpAirtime is granted (coyote-time equivalent).
///
/// Transitions out to:
/// - WallSlide      : touching wall
/// - MidairSpin     : spin pressed (if eligible)
/// - GroundPoundSpin: down pressed (if canGroundPound)
/// - Grounded       : landed (base)
/// - SwimIdle       : entered water (base)
/// </summary>
public class FallState : AirborneStateBase
{
    public override string ID => MarioStateID.Fall;
    public override System.Collections.Generic.IEnumerable<string> Tags => new[] { MarioStateTags.Airborne };

    public override void Enter(string previousState)
    {
        base.Enter(previousState);

        // Coyote time: walked off a ledge — grant a short air timer
        bool walkedOff = previousState == MarioStateID.Idle ||
                         previousState == MarioStateID.Walk ||
                         previousState == MarioStateID.Run  ||
                         previousState == MarioStateID.Skid;
        if (walkedOff)
            State.AirTimer = Time.time + Cfg.CoyoteTime;

        Rb.gravityScale = Cfg.FallGravity;

        MarioEvents.FireLeftGround(PlayerIndex);
    }

    public override void FixedUpdate()
    {
        Rb.gravityScale = Cfg.FallGravity;
        Rb.drag         = 0f;
        ApplyAirHorizontal();
        ClampFallSpeed();
    }

    public override void CheckTransitions()
    {
        // Buffered jump (coyote time still active)
        if (Time.time < State.JumpTimer && State.AirTimer > Time.time && !Machine.IsJumpBlocked())
        {
            RequestTransition(State.SpinJumpQueued && State.CanSpinJump
                ? MarioStateID.SpinJump
                : MarioStateID.Rise);
            return;
        }

        // Climbable grab — handled by base.CheckTransitions with vertical intent for both types

        // Ground pound.
        // Clear climb-exit guard once the player releases down, requiring a fresh press.
        if (!IsPressingDown)
            State.ClimbExitedWhilePressingDown = false;

        if (IsPressingDown && State.CanGroundPound
            && !State.WallSliding && !State.Climbing && !State.JustLeftClimbing
            && !State.ClimbExitedWhilePressingDown && !State.JumpedWhileCrouching
            && State.CurrentClimbable == null)
        {
            RequestTransition(MarioStateID.GroundPoundSpin);
            return;
        }

        // Midair spin — consume SpinPressed so holding the button doesn't retrigger
        if (CanStartMidairSpin())
        {
            State.SpinPressed = false;
            RequestTransition(MarioStateID.MidairSpin);
            return;
        }

        // Wall slide: moving downward, pressing into a wall
        // IsCrouching/IsCrawling can be true mid-air after a crouch-jump — don't block wall slide for that
        if (Rb.velocity.y < 0f && State.Direction.x != 0f && !State.Pushing)
        {
            bool checkRight = State.Direction.x > 0f;
            if (Core.WallDetection.CheckWall(checkRight))
            {
                RequestTransition(MarioStateID.WallSlide);
                return;
            }
        }

        base.CheckTransitions(); // landed, water, climb
    }

    private bool CanStartMidairSpin()
    {
        if (!State.CanMidairSpin)   return false;
        if (State.IsMidairSpinning) return false;
        if (State.Spinning)         return false;
        if (!State.SpinPressed)     return false;

        if (!State.AllowMultipleMidairSpins)
            return !State.MidairSpinUsedThisJump;

        bool firstSpin  = !State.MidairSpinUsedThisJump;
        bool cooldownOk = Time.time >= State.LastMidairSpinTime + Cfg.MidairSpinCooldown;
        return firstSpin || cooldownOk;
    }
}