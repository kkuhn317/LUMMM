using UnityEngine;
using System.Collections;

/// <summary>
/// Shared base for both swimming states.
///
/// Centralizes:
/// - Swimming physics (swimGravity, swimDrag, terminal velocity)
/// - Water exit detection → Rise or Fall on exit
/// - Ground pound in water timeout logic
/// - Bubble spawning coroutine lifecycle
/// </summary>
public abstract class SwimmingStateBase : MarioStateBase
{
    protected MarioPhysicsConfig Cfg => Core.Physics.Config;
    protected Rigidbody2D        Rb  => Core.Rb;

    // Water ground pound timeout (original: 1 second)
    private const float WaterGroundPoundDuration = 1f;

    public override void Enter(string previousState)
    {
        State.Swimming  = true;
        State.Spinning  = false;
        State.OnGround  = false;

        // Cancel any active ground pound cleanly
        if (State.GroundPounding)
        {
            State.GroundPounding            = false;
            State.GroundPoundRotating       = false;
            State.GroundPoundInWater        = false;
            State.WaterGroundPoundStartTime = 0f;
            MarioEvents.FireGroundPoundCancelled(PlayerIndex);
        }

        Rb.gravityScale = Cfg.SwimGravity;
        Rb.drag         = Cfg.SwimDrag;

        MarioEvents.FireEnteredWater(PlayerIndex);
    }

    public override void Exit(string nextState)
    {
        // Only clear swimming flag if actually leaving water
        // (SwimIdle ↔ Swim transitions keep it true)
        bool stayingInWater = nextState == MarioStateID.SwimIdle || nextState == MarioStateID.Swim;
        if (!stayingInWater)
        {
            State.Swimming                  = false;
            State.GroundPoundInWater        = false;
            State.WaterGroundPoundStartTime = 0f;
            MarioEvents.FireExitedWater(PlayerIndex);
        }
    }

    public override void FixedUpdate()
    {
        Rb.gravityScale = Cfg.SwimGravity;
        Rb.drag         = Cfg.SwimDrag;

        // Clamp upward swim speed
        if (Rb.velocity.y > Cfg.SwimTerminalVelocity * 2f)
            Rb.velocity = new Vector2(Rb.velocity.x, Cfg.SwimTerminalVelocity * 2f);

        // Clamp downward speed
        if (-Rb.velocity.y > Cfg.SwimTerminalVelocity)
            Rb.velocity = new Vector2(Rb.velocity.x, -Cfg.SwimTerminalVelocity);

        HandleWaterGroundPoundTimeout();
    }

    public override void CheckTransitions()
    {
        // Exited water
        if (!State.Swimming)
        {
            // Moving upward on exit → jump out of water arc
            if (Rb.velocity.y > 0f)
            {
                Core.Swimming.JumpOutOfWater();
                RequestTransition(MarioStateID.Rise);
            }
            else
            {
                RequestTransition(MarioStateID.Fall);
            }
            return;
        }

        // Grounded underwater (e.g. lake floor)
        if (State.OnGround)
        {
            RequestTransition(MarioStateID.Idle);
            return;
        }
    }

    // ─── Water Ground Pound Timeout ──────────────────────────────────────────

    private void HandleWaterGroundPoundTimeout()
    {
        if (!State.GroundPounding) return;

        if (!State.GroundPoundInWater)
        {
            State.GroundPoundInWater        = true;
            State.WaterGroundPoundStartTime = Time.time;
            return;
        }

        if (!State.GroundPoundRotating &&
            Time.time - State.WaterGroundPoundStartTime > WaterGroundPoundDuration)
        {
            // Timeout: cancel ground pound
            State.GroundPounding            = false;
            State.GroundPoundInWater        = false;
            State.WaterGroundPoundStartTime = 0f;
            MarioEvents.FireGroundPoundCancelled(PlayerIndex);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Mario is in water but not actively stroking — drifting with swim physics.
///
/// Transitions out to:
/// - Swim     : jump pressed (stroke)
/// - Rise/Fall: exited water (base)
/// - Idle     : grounded on lake floor (base)
/// </summary>
public class SwimIdleState : SwimmingStateBase
{
    public override string ID => MarioStateID.SwimIdle;
    public override System.Collections.Generic.IEnumerable<string> Tags => new[] { MarioStateTags.Swimming };

    public override void Enter(string previousState)
    {
        base.Enter(previousState);

        // Coming from a ground pound land underwater
        bool fromGroundPoundLand = previousState == MarioStateID.GroundPoundLand;
        if (fromGroundPoundLand)
        {
            // Animator already handles the re-entry via event
        }
    }

    public override void CheckTransitions()
    {
        // Stroke: jump pressed
        if (Time.time < State.JumpTimer && !State.GroundPounding)
        {
            RequestTransition(MarioStateID.Swim);
            return;
        }

        base.CheckTransitions();
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Mario performed a swim stroke — upward impulse applied.
/// Immediately returns to SwimIdle after the impulse frame.
/// The animator plays the stroke animation via OnSwam event.
///
/// Transitions out to:
/// - SwimIdle : next frame (stroke is instantaneous)
/// - Rise/Fall: exited water (base)
/// </summary>
public class SwimState : SwimmingStateBase
{
    public override string ID => MarioStateID.Swim;
    public override System.Collections.Generic.IEnumerable<string> Tags => new[] { MarioStateTags.Swimming };

    public override void Enter(string previousState)
    {
        base.Enter(previousState);

        // Apply swim impulse
        Rb.AddForce(Vector2.up * Cfg.SwimForce, ForceMode2D.Impulse);

        State.JumpTimer = 0f;
        State.OnGround  = false;

        MarioEvents.FireSwam(PlayerIndex);
    }

    public override void CheckTransitions()
    {
        // Stroke is a single-frame impulse — immediately return to idle
        RequestTransition(MarioStateID.SwimIdle);
    }
}
