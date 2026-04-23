using UnityEngine;
using PowerupState = PowerStates.PowerupState;

/// <summary>
/// Mario is crouching — pressing down while grounded and not carrying.
///
/// On Enter: shrinks the collider.
/// On Exit: restores the collider.
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

        bool goingAirborne = nextState == MarioStateID.Rise
                          || nextState == MarioStateID.SpinJump
                          || nextState == MarioStateID.Fall
                          || nextState == MarioStateID.WallJump;
        if (goingAirborne)
        {
            State.JumpedWhileCrouching = true;

            // Since the player jumped while already holding Down,
            // do not allow that same Down hold to become a ground pound.
            State.RequireDownReleaseForGroundPound = IsPressingDown;
            State.DownPressed = false;

            return;
        }

        State.IsCrouching = false;

        if (nextState != MarioStateID.Crawl)
        {
            RestoreCollider();
            MarioEvents.FireCrouchEnded(PlayerIndex);
        }
    }

    public override void FixedUpdate()
    {
        float spd = GroundAbsSpeed;
        Rb.drag = spd switch
        {
            < 0.5f => 100_000_000f,
            < 5f => 8f / Mathf.Max(spd, 0.01f),
            _ => 1.5f
        };
        Rb.drag *= 1.5f;
        if (Rb.drag > 100_000_000f) Rb.drag = 100_000_000f;

        ClampHorizontalSpeed();
        HandleFacing();
    }

    public override void CheckTransitions()
    {
        if (!IsPressingDown || State.Carrying)
        {
            if (HasCeilingObstruction())
                return;

            RequestTransition(MarioStateID.Idle);
            return;
        }

        if (CanCrawl())
        {
            RequestTransition(MarioStateID.Crawl);
            return;
        }

        base.CheckTransitions();
    }

    private bool CanCrawl()
    {
        return State.CanCrawl
            && State.PowerupState == PowerupState.small
            && HasHorizontalInput
            && GroundAbsSpeed < 0.1f
            && !State.Carrying
            && !State.Swimming;
    }

    private void ShrinkCollider()
    {
        var col = Core.Collider;
        col.size = new Vector2(col.size.x, Cfg.CrouchColliderHeight);
        col.offset = new Vector2(col.offset.x, Cfg.CrouchColliderOffsetY);
    }

    private void RestoreCollider()
    {
        var col = Core.Collider;
        col.size = new Vector2(col.size.x, Core.ColliderOriginalHeight);
        col.offset = new Vector2(col.offset.x, Core.ColliderOriginalOffsetY);
    }
}

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

    public override void Enter(string previousState)
    {
        base.Enter(previousState);

        State.IsCrouching = true;
        State.IsCrawling = true;

        if (previousState != MarioStateID.Crouch)
            ShrinkCollider();

        MarioEvents.FireCrawlStarted(PlayerIndex);
    }

    public override void Exit(string nextState)
    {
        base.Exit(nextState);

        State.IsCrawling = false;
        MarioEvents.FireCrawlEnded(PlayerIndex);

        bool goingAirborne = nextState == MarioStateID.Rise
                          || nextState == MarioStateID.SpinJump
                          || nextState == MarioStateID.Fall
                          || nextState == MarioStateID.WallJump;
        if (goingAirborne)
        {
            State.JumpedWhileCrouching = true;

            // Same fix for crawl-jump.
            State.RequireDownReleaseForGroundPound = IsPressingDown;
            State.DownPressed = false;
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
        Rb.drag = 0f;
        ClampHorizontalSpeed();
        HandleFacing();
    }

    public override void CheckTransitions()
    {
        if (!HasHorizontalInput)
        {
            RequestTransition(MarioStateID.Crouch);
            return;
        }

        if (!IsPressingDown)
        {
            if (HasCeilingObstruction())
                return;

            RequestTransition(MarioStateID.Idle);
            return;
        }

        base.CheckTransitions();
    }

    private void ApplyCrawlForce()
    {
        float horizontal = State.Direction.x;
        if (Mathf.Abs(horizontal) < 0.01f)
            return;

        float groundSpeed = GroundSpeed;
        float maxSpd = Cfg.MaxSpeed * Cfg.CrawlMaxSpeedMult;

        if (Mathf.Abs(groundSpeed) <= maxSpd || Mathf.Sign(horizontal) != Mathf.Sign(groundSpeed))
            Rb.AddForce(horizontal * Cfg.MoveSpeed * Cfg.CrawlForceMult * GetGroundMoveDir());
    }

    private void ShrinkCollider()
    {
        var col = Core.Collider;
        col.size = new Vector2(col.size.x, Cfg.CrouchColliderHeight);
        col.offset = new Vector2(col.offset.x, Cfg.CrouchColliderOffsetY);
    }

    private void RestoreCollider()
    {
        var col = Core.Collider;
        col.size = new Vector2(col.size.x, Core.ColliderOriginalHeight);
        col.offset = new Vector2(col.offset.x, Core.ColliderOriginalOffsetY);
    }
}