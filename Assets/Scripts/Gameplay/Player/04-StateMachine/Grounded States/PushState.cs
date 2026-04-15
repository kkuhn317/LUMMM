using UnityEngine;

/// <summary>
/// Mario is pushing a heavy object.
/// Velocity is set directly to the pushing speed — no force-based movement.
/// Entered/exited by the object's script via MarioCore.State directly.
///
/// Transitions out to:
/// - Idle  : push object removed (State.Pushing becomes false)
/// - Fall  : walked off ledge (base)
/// - Rise  : jump pressed (base)
/// </summary>
public class PushState : GroundedStateBase
{
    public override string ID => MarioStateID.Push;
    public override System.Collections.Generic.IEnumerable<string> Tags => new[] { MarioStateTags.Grounded };

    public override void FixedUpdate()
    {
        if (!State.Pushing || State.PushingObject == null)
            return;

        int   dir = State.FacingRight ? 1 : -1;
        float spd = State.PushingSpeed;

        // Direct velocity set — no forces while pushing
        Rb.velocity = new Vector2(spd * dir, Rb.velocity.y);

        if (State.OnGround)
        {
            Rb.gravityScale = 0f;
            Rb.drag         = 0f;
        }
        else
        {
            Rb.gravityScale = Cfg.FallGravity;
            Rb.drag         = 0f;
        }
    }

    public override void CheckTransitions()
    {
        if (!State.Pushing || State.PushingObject == null)
        {
            RequestTransition(MarioStateID.Idle);
            return;
        }

        // Still allow jumping and ledge-fall while pushing
        base.CheckTransitions();
    }
}