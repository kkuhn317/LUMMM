using UnityEngine;
using PowerupState = PowerStates.PowerupState;

/// <summary>
/// Mario is crouching — pressing down while grounded and not carrying.
///
/// On Enter: shrinks the collider.
/// On Exit:  restores the collider.
///
/// Transitions out to:
/// - Crawl : small Mario + horizontal input + nearly stopped
/// - Idle  : released down
/// - Rise  : jump pressed (base)
/// - Fall  : walked off ledge (base)
/// </summary>
public class CrouchState : GroundedStateBase
{
    public override string ID => MarioStateID.Crouch;
    public override System.Collections.Generic.IEnumerable<string> Tags => new[] { MarioStateTags.Grounded };

    public override void Enter(string previousState)
    {
        base.Enter(previousState);

        State.IsCrouching = true;
        ShrinkCollider();

        MarioEvents.FireCrouchStarted(PlayerIndex);
    }

    public override void Exit(string nextState)
    {
        base.Exit(nextState);

        // When going airborne from a crouch/crawl (jump, fall off ledge, etc.),
        // keep the crouched collider mid-air and block ground pound until the
        // player releases down and presses it again deliberately.
        bool goingAirborne = nextState == MarioStateID.Rise
                          || nextState == MarioStateID.SpinJump
                          || nextState == MarioStateID.Fall
                          || nextState == MarioStateID.WallJump;
        if (goingAirborne)
        {
            State.JumpedWhileCrouching = true;
            // Keep IsCrouching true and collider shrunk — airborne states will restore on land
            return;
        }

        State.IsCrouching = false;

        // Only restore collider if not entering crawl
        // (Crawl inherits the crouched collider)
        if (nextState != MarioStateID.Crawl)
        {
            RestoreCollider();
            MarioEvents.FireCrouchEnded(PlayerIndex);
        }
    }

    public override void FixedUpdate()
    {
        // Crouch zeroes horizontal input (no new force) but uses drag to decelerate.
        // This matches original behaviour — no instant stop, just controlled slide-to-halt.
        float spd = Mathf.Abs(Rb.velocity.x);
        Rb.drag = spd switch
        {
            < 0.5f => 100_000_000f,
            < 5f   => 8f / spd,
            _      => 1.5f
        };
        Rb.drag *= 1.5f; // Crouch drag multiplier
        if (Rb.drag > 100_000_000f) Rb.drag = 100_000_000f;

        ClampHorizontalSpeed();
        HandleFacing();
    }

    public override void CheckTransitions()
    {
        // Released crouch — only stand up if there is enough headroom above
        if (!IsPressingDown || State.Carrying)
        {
            // If something is blocking directly above, stay crouched until clear
            if (HasCeilingObstruction())
                return;

            RequestTransition(MarioStateID.Idle);
            return;
        }

        // Transition to crawl (small Mario only)
        if (CanCrawl())
        {
            RequestTransition(MarioStateID.Crawl);
            return;
        }

        base.CheckTransitions();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private bool CanCrawl()
    {
        return State.CanCrawl
            && State.PowerupState == PowerupState.small
            && HasHorizontalInput
            && Mathf.Abs(Rb.velocity.x) < 0.1f
            && !State.Carrying
            && !State.Swimming;
    }

    private void ShrinkCollider()
    {
        var col = Core.Collider;
        col.size   = new Vector2(col.size.x, Cfg.CrouchColliderHeight);
        col.offset = new Vector2(col.offset.x, Cfg.CrouchColliderOffsetY);
    }

    private void RestoreCollider()
    {
        var col = Core.Collider;
        col.size   = new Vector2(col.size.x, Core.ColliderOriginalHeight);
        col.offset = new Vector2(col.offset.x, Core.ColliderOriginalOffsetY);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Small Mario is crawling — crouched and moving horizontally at low speed.
/// Only available when Mario is small (PowerupState.small) and canCrawl is true.
///
/// Physics: 2x force multiplier, 0.5x max speed.
///
/// Transitions out to:
/// - Crouch : no horizontal input or stopped
/// - Idle   : released down
/// - Rise   : jump pressed (base)
/// - Fall   : walked off ledge (base)
/// </summary>
public class CrawlState : GroundedStateBase
{
    public override string ID => MarioStateID.Crawl;
    public override System.Collections.Generic.IEnumerable<string> Tags => new[] { MarioStateTags.Grounded };

    // Crawl inherits the crouched collider from CrouchState.
    // If we enter crawl directly (edge case), shrink it here too.
    public override void Enter(string previousState)
    {
        base.Enter(previousState);

        State.IsCrouching = true;
        State.IsCrawling  = true;

        if (previousState != MarioStateID.Crouch)
            ShrinkCollider();

        MarioEvents.FireCrawlStarted(PlayerIndex);
    }

    public override void Exit(string nextState)
    {
        base.Exit(nextState);

        State.IsCrawling = false;
        MarioEvents.FireCrawlEnded(PlayerIndex);

        // When falling off a ledge or jumping while crawling, block ground pound
        // until down is released and re-pressed — same guard as CrouchState.
        bool goingAirborne = nextState == MarioStateID.Rise
                          || nextState == MarioStateID.SpinJump
                          || nextState == MarioStateID.Fall
                          || nextState == MarioStateID.WallJump;
        if (goingAirborne)
        {
            State.JumpedWhileCrouching = true;
        }

        if (nextState != MarioStateID.Crouch)
        {
            State.IsCrouching = false;
            RestoreCollider();
            MarioEvents.FireCrouchEnded(PlayerIndex);
        }
    }

    public override void FixedUpdate()
    {
        ApplyCrawlForce();
        // Crawling: no drag — force handles acceleration and max speed
        Rb.drag = 0f;
        ClampHorizontalSpeed();
        HandleFacing();
    }

    public override void CheckTransitions()
    {
        // Stop crawling: no input or moving too fast to start crawling again
        bool horizontalStopped = Mathf.Abs(Rb.velocity.x) < 0.05f;
        if (!HasHorizontalInput || (!State.IsCrawling && !horizontalStopped))
        {
            RequestTransition(MarioStateID.Crouch);
            return;
        }

        // Released crouch — only stand up if there is enough headroom above
        if (!IsPressingDown)
        {
            if (HasCeilingObstruction())
                return;

            RequestTransition(MarioStateID.Idle);
            return;
        }

        base.CheckTransitions();
    }

    // ─── Movement ────────────────────────────────────────────────────────────

    private void ApplyCrawlForce()
    {
        float   horizontal = State.Direction.x;
        Vector2 moveDir    = GetSlopeMoveDir();
        float   maxSpd     = Cfg.MaxSpeed * Cfg.CrawlMaxSpeedMult;

        if (Mathf.Abs(Rb.velocity.x) <= maxSpd ||
            Mathf.Sign(horizontal) != Mathf.Sign(Rb.velocity.x))
        {
            Rb.AddForce(horizontal * Cfg.MoveSpeed * Cfg.CrawlForceMult * moveDir);
        }
    }

    private Vector2 GetSlopeMoveDir()
    {
        return new Vector2(
            Mathf.Cos(State.FloorAngle * Mathf.Deg2Rad),
            Mathf.Sin(State.FloorAngle * Mathf.Deg2Rad)
        ).normalized;
    }

    private void ShrinkCollider()
    {
        var col = Core.Collider;
        col.size   = new Vector2(col.size.x, Cfg.CrouchColliderHeight);
        col.offset = new Vector2(col.offset.x, Cfg.CrouchColliderOffsetY);
    }

    private void RestoreCollider()
    {
        var col = Core.Collider;
        col.size   = new Vector2(col.size.x, Core.ColliderOriginalHeight);
        col.offset = new Vector2(col.offset.x, Core.ColliderOriginalOffsetY);
    }
}