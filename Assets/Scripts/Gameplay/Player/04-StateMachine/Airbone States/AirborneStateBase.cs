using UnityEngine;

/// <summary>
/// Shared base for all airborne states: Rise, Fall, WallSlide, MidairSpin,
/// SpinJump, WallJump, and the three GroundPound substates.
///
/// Centralizes:
/// - Horizontal air movement (force-based, with air turn multiplier)
/// - Terminal velocity clamping
/// - Shared transition checks every airborne state needs:
///     landed → grounded state
///     entered water → SwimIdle
///     grabbed climbable → Climb
/// </summary>
public abstract class AirborneStateBase : MarioStateBase
{
    protected MarioPhysicsConfig Cfg => Core.Physics.Config;
    protected Rigidbody2D        Rb  => Core.Rb;

    // ─── Enter / Exit ────────────────────────────────────────────────────────

    public override void Enter(string previousState)
    {
        Rb.drag         = 0f;
        State.OnGround  = false;
    }

    // ─── Shared Physics ──────────────────────────────────────────────────────

    /// <summary>
    /// Standard horizontal air control. Call from FixedUpdate in concrete states
    /// that allow steering (Rise, Fall). Skip it in GroundPound and WallSlide.
    /// </summary>
    protected void ApplyAirHorizontal()
    {
        float   horizontal = State.Direction.x;
        if (horizontal == 0f) return;

        float   speedMult = IsChangingDirection ? Cfg.AirTurnMult : 1f;
        Vector2 force     = Vector2.right * horizontal * Cfg.MoveSpeed * speedMult;

        // Only accelerate up to max run speed
        if (Mathf.Abs(Rb.velocity.x) < Cfg.MaxRunSpeed ||
            Mathf.Sign(horizontal) != Mathf.Sign(Rb.velocity.x))
        {
            Rb.AddForce(force);
        }
    }

    /// <summary>
    /// Clamps downward velocity to terminal velocity.
    /// Pass a custom cap for states that override it (wall slide, ground pound).
    /// </summary>
    protected void ClampFallSpeed(float? overrideCap = null)
    {
        float cap = overrideCap ?? Cfg.TerminalVelocity;
        if (-Rb.velocity.y > cap)
            Rb.velocity = new Vector2(Rb.velocity.x, -cap);
    }

    // ─── Shared Transition Checks ────────────────────────────────────────────
    public override void CheckTransitions()
    {
        // 1. Grounded check
        if (State.OnGround) { OnLanded(); return; }

        // 2. Water check
        if (State.Swimming) { RequestTransition(MarioStateID.SwimIdle); return; }

        // 3. CLIMBING CHECK
        if (State.CurrentClimbable != null && !State.JustLeftClimbing)
        {
            // Clear "just detached" guards if the player isn't pressing that direction anymore
            if (State.Direction.y <= 0.5f) State.ClimbExitedWhilePressingUp = false;
            if (State.Direction.y >= -0.5f) State.ClimbExitedWhilePressingDown = false;

            // Only allow attaching with vertical intent
            bool verticalIntent = Mathf.Abs(State.Direction.y) > 0.5f;

            if (verticalIntent)
            {
                if (State.Direction.y > 0.5f && State.ClimbExitedWhilePressingUp) return;
                if (State.Direction.y < -0.5f && State.ClimbExitedWhilePressingDown) return;

                var method = State.CurrentClimbable.climbMethod;
                RequestTransition(method == Climbable.ClimbMethod.Side
                    ? MarioStateID.ClimbSide
                    : MarioStateID.ClimbFront);
                return;
            }
        }
    }

    /// <summary>
    /// Called when OnGround becomes true. Override to customize landing behaviour.
    /// Default: transition to Idle, or Crouch if Mario jumped while crouching.
    /// </summary>
    protected virtual void OnLanded()
    {
        MarioEvents.FireLanded(PlayerIndex);

        if (State.JumpedWhileCrouching)
        {
            State.JumpedWhileCrouching = false;

            if (IsPressingDown)
            {
                // Still holding down — go straight back to crouch
                RequestTransition(MarioStateID.Crouch);
            }
            else
            {
                // Released down mid-air — restore collider and go idle
                State.IsCrouching = false;
                var col = Core.Collider;
                col.size   = new Vector2(col.size.x, Core.ColliderOriginalHeight);
                col.offset = new Vector2(col.offset.x, Core.ColliderOriginalOffsetY);
                MarioEvents.FireCrouchEnded(PlayerIndex);
                RequestTransition(MarioStateID.Idle);
            }
            return;
        }

        RequestTransition(MarioStateID.Idle);
    }
}