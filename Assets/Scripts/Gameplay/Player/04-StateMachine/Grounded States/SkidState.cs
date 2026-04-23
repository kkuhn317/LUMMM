using UnityEngine;

/// <summary>
/// Mario is skidding — he changed direction at high speed and
/// is decelerating against his current momentum.
///
/// Physics: reduced speed multiplier (0.7x) while skidding.
/// Exits as soon as Mario either stops, fully reverses, or releases input.
///
/// Transitions out to:
/// - Idle : stopped
/// - Walk : slowed below run speed and no longer changing direction
/// - Run  : same but run still held
/// - Fall : walked off ledge (base)
/// - Rise : jump pressed (base)
/// </summary>
public class SkidState : GroundedStateBase
{
    public override string ID => MarioStateID.Skid;
    public override System.Collections.Generic.IEnumerable<string> Tags => new[] { MarioStateTags.Grounded };

    private bool _isMaxSpeedSkid;

    public override void Enter(string previousState)
    {
        base.Enter(previousState);

        _isMaxSpeedSkid = GroundAbsSpeed >= Cfg.MaxRunSpeed * 0.9f;

        MarioEvents.FireSkidStarted(PlayerIndex);
    }

    public override void Exit(string nextState)
    {
        base.Exit(nextState);
        MarioEvents.FireSkidEnded(PlayerIndex);
    }

    public override void FixedUpdate()
    {
        if (!State.OnGround)
        {
            Rb.gravityScale = Cfg.FallGravity;
            Rb.drag = 0f;
            return;
        }

        ApplySkidForce();
        base.FixedUpdate();
    }

    public override void CheckTransitions()
    {
        float speed = GroundAbsSpeed;

        if (!IsChangingDirection)
        {
            if (speed < 0.1f)
            {
                RequestTransition(MarioStateID.Idle);
                return;
            }

            RequestTransition(State.RunPressed ? MarioStateID.Run : MarioStateID.Walk);
            return;
        }

        if (!HasHorizontalInput && speed < 0.1f)
        {
            RequestTransition(MarioStateID.Idle);
            return;
        }

        base.CheckTransitions();
    }

    private void ApplySkidForce()
    {
        float horizontal = State.Direction.x;
        if (horizontal == 0f) return;

        float speedMult = _isMaxSpeedSkid ? Cfg.SkidSpeedMult : 1f;
        float speed = State.RunPressed ? Cfg.RunSpeed : Cfg.MoveSpeed;

        Rb.AddForce(horizontal * speed * speedMult * GetGroundMoveDir());
    }
}