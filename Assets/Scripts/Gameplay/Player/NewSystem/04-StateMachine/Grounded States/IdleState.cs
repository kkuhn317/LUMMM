using UnityEngine;

/// <summary>
/// Mario is stationary on the ground.
/// No horizontal input, not crouching, not carrying.
///
/// Transitions out to:
/// - Walk/Run  : horizontal input detected
/// - Crouch    : pressing down
/// - Fall      : walked off ledge (via GroundedStateBase)
/// - Rise      : jump pressed (via GroundedStateBase)
/// - LookUp    : pressing up while idle (camera only, not a state change)
/// </summary>
public class IdleState : GroundedStateBase
{
    public override string ID => MarioStateID.Idle;
    public override System.Collections.Generic.IEnumerable<string> Tags
        => new[] { MarioStateTags.Grounded };

    public override void Enter(string previousState)
    {
        base.Enter(previousState);
        State.IsCrouching = false;
        State.IsCrawling  = false;
    }

public override void FixedUpdate()
    {
        base.FixedUpdate(); // drag, clamp, facing
    }

    public override void CheckTransitions()
    {
        // Crouch — skipped when a climbable is in range so down input climbs instead
        if (IsPressingDown && State.CanCrouch && !State.Carrying
            && !State.Climbing)
        {
            RequestTransition(MarioStateID.Crouch);
            return;
        }

        // Start walking
        if (HasHorizontalInput)
        {
            RequestTransition(State.RunPressed ? MarioStateID.Run : MarioStateID.Walk);
            return;
        }

        base.CheckTransitions();
    }

}