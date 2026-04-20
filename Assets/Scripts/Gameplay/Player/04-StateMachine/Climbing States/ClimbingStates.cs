using UnityEngine;

/// <summary>
/// Shared base for both climbing states.
///
/// Centralizes:
/// - Gravity/drag zero on enter
/// - Full state cleanup on enter (ground pound, spin, midair spin)
/// - Exit to Fall when climbable is lost
/// - Jump/spin-jump queued → SpinJump or Rise
/// - Gravity restore on exit
/// </summary>
public abstract class ClimbingStateBase : MarioStateBase
{
    protected MarioPhysicsConfig Cfg => Core.Physics.Config;
    protected Rigidbody2D        Rb  => Core.Rb;

    public override void Enter(string previousState)
    {
        State.Climbing = true;

        // Zero physics
        Rb.velocity     = Vector2.zero;
        Rb.gravityScale = 0f;
        Rb.drag         = 0f;

        // Cancel any active movement states
        State.Spinning              = false;
        State.SpinJumpQueued        = false;
        State.SpinPressed           = false;
        State.IsMidairSpinning      = false;
        State.GroundPounding        = false;
        State.GroundPoundRotating   = false;
        State.GroundPoundLanded     = false;
        State.GroundPoundInWater    = false;
        State.WaterGroundPoundStartTime = 0f;

        MarioEvents.FireClimbStarted(PlayerIndex);
    }

    public override void Exit(string nextState)
    {
        State.Climbing                   = false;
        State.JustLeftClimbing           = true;
        State.JustLeftClimbingTimer      = 0.15f; // lasts longer than one frame
        State.ClimbExitedWhilePressingDown = State.Direction.y < -0.5f;
        State.ClimbExitedWhilePressingUp   = State.Direction.y >  0.5f;

        // Keep CurrentClimbable set — Climbable.OnTriggerExit2D will clear it
        Rb.gravityScale = 1f;
        Rb.drag         = 0f;

        // If jumping off the vine while pressing sideways, face that direction
        bool goingAirborne = nextState == MarioStateID.Rise
                          || nextState == MarioStateID.Fall
                          || nextState == MarioStateID.SpinJump
                          || nextState == MarioStateID.WallJump;
        if (goingAirborne && Mathf.Abs(State.MoveInput.x) > 0.01f)
            Core.Physics.FlipTo(State.MoveInput.x > 0f);

        MarioEvents.FireClimbEnded(PlayerIndex);
    }

    public override void FixedUpdate()
    {
        // Both states need gravity disabled every frame
        Rb.gravityScale = 0f;
        Rb.drag         = 0f;
    }

    public override void CheckTransitions()
    {
        // Lost the climbable → fall
        if (State.CurrentClimbable == null)
        {
            RequestTransition(MarioStateID.Fall);
            return;
        }

        // Jump pressed: spin jump or regular jump
        if (Time.time < State.JumpTimer && !Machine.IsJumpBlocked())
        {
            if (State.SpinJumpQueued && State.CanSpinJump)
            {
                RequestTransition(MarioStateID.SpinJump);
                return;
            }
            RequestTransition(MarioStateID.Rise);
            return;
        }

        // Entered water while climbing
        if (State.Swimming)
        {
            RequestTransition(MarioStateID.SwimIdle);
            return;
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Mario is climbing a ladder or rope (front-facing climbable).
/// Both X and Y velocity are set directly from input × climbSpeed.
///
/// Transitions out to:
/// - Fall    : released climbable or moved off it (base)
/// - Rise    : jump pressed (base)
/// - SpinJump: spin jump queued (base)
/// - Idle    : touched the ground while climbing down (detected by ground check)
/// </summary>
public class ClimbFrontState : ClimbingStateBase
{
    public override string ID => MarioStateID.ClimbFront;
    public override System.Collections.Generic.IEnumerable<string> Tags => new[] { MarioStateTags.Climbing };

    public override void FixedUpdate()
    {
        base.FixedUpdate(); // gravity/drag

        var climbable = State.CurrentClimbable;
        if (climbable == null) return;

        // Use MoveInput directly instead of Direction — Direction is only propagated
        // in MarioInput.Update, so it can lag one FixedUpdate behind on keyboard
        // where key events fire instantly but Update hasn't run yet.
        Vector2 input = State.MoveInput;

        Rb.velocity = new Vector2(
            input.x * climbable.climbSpeed,
            input.y * climbable.climbSpeed
        );

        // MarioGroundDetection skips its raycast entirely while State.Climbing is true,
        // so we own OnGround here. Only set it when actively pressing down —
        // neutral or upward input clears it so the player stays on the climbable.
        if (input.y < -0.5f)
        {
            var hit = Core.GroundDetection.CheckGround();
            State.OnGround = hit.HasValue;
        }
        else
        {
            State.OnGround = false;
        }

        // Animator receives speed via event system (MarioAnimatorController listens)
        State.Velocity = Rb.velocity;
    }

    public override void CheckTransitions()
    {
        // Exit to Idle when grounded, unless pressing up (player wants to climb back up).
        // Use MoveInput for the same reason as FixedUpdate — avoids Update lag on keyboard.
        if (State.OnGround && State.MoveInput.y <= 0f)
        {
            RequestTransition(MarioStateID.Idle);
            return;
        }

        // Cancel spin every frame while climbing (unless spin jump queued)
        if (!State.SpinJumpQueued)
        {
            State.Spinning   = false;
            State.SpinPressed = false;
        }

        base.CheckTransitions();
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Mario is climbing a wall or pipe from the side.
/// Only Y velocity is set. X position is locked to the climbable.
///
/// Side-climb detach logic:
/// - Stick goes to neutral → sideClimbCanMoveToSide = true
/// - Once neutral was hit, pressing away from the wall → detach (Fall)
/// - Pressing toward the wall while facing it → flip (face the other side)
///
/// Transitions out to:
/// - Fall    : pressed away from wall (detach), or climbable lost (base)
/// - Rise    : jump pressed (base)
/// - SpinJump: spin jump queued (base)
/// - Idle    : grounded (base)
/// </summary>
public class ClimbSideState : ClimbingStateBase
{
    public override string ID => MarioStateID.ClimbSide;
    public override System.Collections.Generic.IEnumerable<string> Tags => new[] { MarioStateTags.Climbing };

    // Set to true after a flip — player must release stick before detach/flip is possible again
    private bool _waitingForNeutral = false;

    public override void Enter(string previousState)
    {
        _waitingForNeutral = false;

        var climbable = State.CurrentClimbable;
        if (climbable != null)
        {
            // Determine which side Mario is approaching from using his actual
            // position relative to the climbable center — this is reliable even
            // when re-entering after a flip, unlike State.FacingRight which
            // retains the last flipped value from the previous climb session.
            bool marioIsOnLeft = Core.Rb.position.x < climbable.PoleCenterX;
            Core.Physics.FlipTo(marioIsOnLeft); // on left → face right, on right → face left

            // Snap position immediately so FixedUpdate doesn't fight us
            float xPos = marioIsOnLeft
                ? climbable.PoleCenterX - climbable.width / 2f
                : climbable.PoleCenterX + climbable.width / 2f;
            Core.Rb.position = new Vector2(xPos, Core.Rb.position.y);
        }

        base.Enter(previousState);
    }

    public override void Exit(string nextState)
    {
        // Side climbing: keep current facing direction on exit
        // (Don't flip to movement direction like front climbing does)
        State.Climbing                    = false;
        State.JustLeftClimbing            = true;
        State.JustLeftClimbingTimer       = 0.15f;
        State.CurrentClimbable            = null; // clear immediately so Fall can't re-enter
        State.ClimbExitedWhilePressingDown = State.Direction.y < -0.5f;
        State.ClimbExitedWhilePressingUp   = State.Direction.y >  0.5f;

        Rb.gravityScale = 1f;
        Rb.drag         = 0f;
        MarioEvents.FireClimbEnded(PlayerIndex);
        // NOTE: intentionally NOT calling base.Exit to skip the facing-direction flip
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate(); // gravity/drag

        var climbable = State.CurrentClimbable;
        if (climbable == null) return;

        // Vertical movement only
        Rb.velocity = new Vector2(0f, State.Direction.y * climbable.climbSpeed);

        // Own the OnGround check (MarioGroundDetection skips while Climbing)
        if (State.Direction.y < -0.5f)
        {
            var hit = Core.GroundDetection.CheckGround();
            State.OnGround = hit.HasValue;
        }
        else if (State.Direction.y > 0.5f)
            State.OnGround = false;

        // Lock horizontal position to the climbable surface
        float xPos = State.FacingRight
            ? climbable.PoleCenterX - climbable.width / 2f  // facing right = on left side of pipe
            : climbable.PoleCenterX + climbable.width / 2f; // facing left = on right side of pipe

        // Use rb.position so the physics body moves correctly — writing
        // transform.position on a Rigidbody causes a discontinuity that makes
        // Mario drift horizontally and overlap adjacent climbable triggers.
        Core.Rb.position = new Vector2(xPos, Core.Rb.position.y);

        State.Velocity = Rb.velocity;

        // Clear waiting flag once stick returns to neutral
        if (State.MoveInput.x == 0f)
            _waitingForNeutral = false;
    }

    public override void CheckTransitions()
    {
        // Cancel spin every frame
        if (!State.SpinJumpQueued)
        {
            State.Spinning    = false;
            State.SpinPressed = false;
        }

        // Side-detach logic:
        // - Pressing in same direction as facing (into surface) = detach
        // - Pressing opposite direction = flip; must release stick before next action
        if (!_waitingForNeutral && State.MoveInput.x != 0f)
        {
            bool pressingInFacingDirection = ( State.FacingRight && State.MoveInput.x < 0f)
                                          || (!State.FacingRight && State.MoveInput.x > 0f);
            if (pressingInFacingDirection)
            {
                // Check if there is another ClimbSide climbable in the detach direction
                // so we can transfer directly without going through Fall.
                float detachDir  = State.FacingRight ? -1f : 1f; // direction Mario is pressing away
                float searchX    = Core.Rb.position.x + detachDir * (State.CurrentClimbable != null ? State.CurrentClimbable.width + 0.6f : 1.2f);
                var   hits       = Physics2D.OverlapCircleAll(new Vector2(searchX, Core.Rb.position.y), 0.4f);
                Climbable nextClimbable = null;
                foreach (var hit in hits)
                {
                    var c = hit.GetComponent<Climbable>();
                    if (c != null && c != State.CurrentClimbable && c.climbMethod == Climbable.ClimbMethod.Side)
                    {
                        nextClimbable = c;
                        break;
                    }
                }

                if (nextClimbable != null)
                {
                    // Transfer directly to the next climbable — no fall gap.
                    // Mario is moving in detachDir, so he arrives on the opposite side
                    // of the next climbable (e.g. moving left → lands on right side).
                    State.CurrentClimbable = nextClimbable;
                    _waitingForNeutral     = true;

                    // Pre-set facing so Enter uses the correct side instead of
                    // reading position (which hasn't moved to the new climbable yet).
                    // Moving left (detachDir=-1) → arriving from right → face left (FacingRight=false).
                    // Moving right (detachDir=+1) → arriving from left → face right (FacingRight=true).
                    bool arriveFromLeft = detachDir > 0f;
                    Core.Physics.FlipTo(arriveFromLeft);

                    // Snap X to the correct side of the new climbable immediately
                    float newX = arriveFromLeft
                        ? nextClimbable.PoleCenterX - nextClimbable.width / 2f
                        : nextClimbable.PoleCenterX + nextClimbable.width / 2f;
                    Core.Rb.position = new Vector2(newX, Core.Rb.position.y);

                    RequestTransition(MarioStateID.ClimbSide);
                    return;
                }

                RequestTransition(MarioStateID.Fall);
                return;
            }
            else
            {
                Core.Physics.FlipTo(!State.FacingRight);
                _waitingForNeutral = true;
            }
        }

        // Exit to Idle when grounded
        if (State.OnGround && State.Direction.y <= 0f)
        {
            RequestTransition(MarioStateID.Idle);
            return;
        }

        base.CheckTransitions();
    }
}