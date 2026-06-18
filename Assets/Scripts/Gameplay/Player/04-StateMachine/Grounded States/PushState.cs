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

        if (State.OnGround)
        {
            Rb.gravityScale = 0f;
            Rb.drag         = 0f;

            // Get the normal of the floor Mario is standing on
            Vector2 floorNormal = State.FloorNormal;

            // Calculate the Tangent
            Vector2 slopeTangent = new Vector2(floorNormal.y, -floorNormal.x).normalized;

            // Scale the diagonal pushing speed so x velocity stays consistent
            float tangentSpeed = spd;
            if (floorNormal.y > 0.01f)
            {
                tangentSpeed = spd / floorNormal.y;
            }

            Rb.velocity = slopeTangent * (tangentSpeed * dir);
        }
        else
        {
            Rb.gravityScale = Cfg.FallGravity;
            Rb.drag         = 0f;

            // If Mario pushes an object off a ledge, revert to flat horizontal pushing
            // so gravity can naturally pull him down.
            Rb.velocity = new Vector2(spd * dir, Rb.velocity.y);
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