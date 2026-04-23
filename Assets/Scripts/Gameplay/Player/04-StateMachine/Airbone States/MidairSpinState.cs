using UnityEngine;

/// <summary>
/// Mario is performing a midair spin (twirl/glide).
/// Two internal phases: stall → glide.
///
/// Stall phase (midairSpinStallTime):
///   gravity = 0, velocity held, brief hang in the air.
///
/// Glide phase:
///   gravity = fallGravity * gravityMult (floaty), fall speed capped.
///
/// On Enter: gives an upward boost if falling or at apex.
///           if rising, halves vertical speed to prevent gaining huge height.
///
/// Transitions out to:
/// - Fall     : spin duration expired
/// - Grounded : landed (base)
/// - SwimIdle : entered water (base)
/// </summary>
public class MidairSpinState : AirborneStateBase
{
    public override string ID => MarioStateID.MidairSpin;
    public override System.Collections.Generic.IEnumerable<string> Tags => new[] { MarioStateTags.Airborne };

    private float _spinEndTime;
    private float _spinStartTime;

    public override void Enter(string previousState)
    {
        base.Enter(previousState);

        _spinStartTime = Time.time;
        _spinEndTime   = Time.time + Cfg.MidairSpinDuration;

        // Vertical velocity adjustment on entry
        if (Rb.velocity.y <= 0f)
        {
            // Falling or at apex: give upward boost
            float targetY = Mathf.Max(Rb.velocity.y + Cfg.MidairSpinUpwardBoost,
                                      Cfg.MidairSpinUpwardBoost);
            Rb.velocity = new Vector2(Rb.velocity.x, targetY);
        }
        else
        {
            // Still rising: reduce vertical speed so we don't gain huge height
            Rb.velocity = new Vector2(Rb.velocity.x, Rb.velocity.y * 0.5f);
        }

        // Preserve horizontal momentum
        if (Cfg.MidairSpinHorizontalPreserve < 1f)
        {
            Rb.velocity = new Vector2(
                Rb.velocity.x * Cfg.MidairSpinHorizontalPreserve,
                Rb.velocity.y
            );
        }

        State.IsMidairSpinning       = true;
        State.MidairSpinUsedThisJump = true;
        State.LastMidairSpinTime     = -999f; // will be set on exit

        Core.NotifyAbilities(a => a.onSpinPressed());

        MarioEvents.FireMidairSpinStarted(PlayerIndex);
    }

    public override void Exit(string nextState)
    {
        State.IsMidairSpinning   = false;
        State.LastMidairSpinTime = Time.time; // Start cooldown from spin end
        MarioEvents.FireMidairSpinEnded(PlayerIndex);
    }

    public override void FixedUpdate()
    {
        float elapsed = Time.time - _spinStartTime;

        if (elapsed < Cfg.MidairSpinStallTime)
        {
            // Stall phase: freeze gravity, hold velocity from Enter
            Rb.gravityScale = 0f;
            Rb.drag         = 0f;
        }
        else
        {
            // Glide phase: reduced gravity, capped fall speed
            Rb.gravityScale = Cfg.FallGravity * Cfg.MidairSpinGravityMult;
            Rb.drag         = 0f;
            ClampFallSpeed(Cfg.MidairSpinFallSpeedCap);
        }

        ApplyAirHorizontal();
    }

    public override void CheckTransitions()
    {
        // Duration expired
        if (Time.time >= _spinEndTime)
        {
            RequestTransition(MarioStateID.Fall);
            return;
        }

        // Ground pound cancels spin only on a fresh Down press
        if (State.DownPressed && State.CanGroundPound && !State.RequireDownReleaseForGroundPound)
        {
            State.DownPressed = false;
            RequestTransition(MarioStateID.GroundPoundSpin);
            return;
        }

        base.CheckTransitions(); // landed, water, climb
    }

    protected override void OnLanded()
    {
        // Landing ends the spin naturally
        MarioEvents.FireLanded(PlayerIndex);
        RequestTransition(MarioStateID.Idle);
    }
}
