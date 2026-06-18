using UnityEngine;

/// <summary>
/// Mario is walking (run button not held, or speed below run threshold).
///
/// Transitions out to:
/// - Idle   : no horizontal input and nearly stopped
/// - Run    : run button pressed
/// - Skid   : changing direction at high speed
/// - Crouch : pressing down
/// - Fall   : walked off ledge (base)
/// - Rise   : jump pressed (base)
/// </summary>
public class WalkState : GroundedStateBase
{
    public override string ID => MarioStateID.Walk;
    public override System.Collections.Generic.IEnumerable<string> Tags
        => new[] { MarioStateTags.Grounded };

    public override void FixedUpdate()
    {
        if (!State.OnGround)
        {
            Rb.gravityScale = Cfg.FallGravity;
            Rb.drag = 0f;
            return;
        }

        ApplyMovementForce();
        base.FixedUpdate();
    }

    public override void CheckTransitions()
    {
        if (IsPressingDown && State.CanCrouch && !State.Carrying && !State.Climbing)
        {
            RequestTransition(MarioStateID.Crouch);
            return;
        }

        // On a slope velocity is diagonal — use total magnitude, not just X,
        // otherwise uphill movement reads as stopped and transitions to Idle.
        float currentSpeed = State.FloorAngle != 0f ? Rb.velocity.magnitude : Mathf.Abs(Rb.velocity.x);
        if (!HasHorizontalInput && currentSpeed < 0.1f)
        {
            RequestTransition(MarioStateID.Idle);
            return;
        }

        if (State.RunPressed && Machine.CurrentStateID != MarioStateID.Run)
        {
            RequestTransition(MarioStateID.Run);
            return;
        }

        if (IsChangingDirection && currentSpeed >= Cfg.MaxRunSpeed * 0.9f)
        {
            RequestTransition(MarioStateID.Skid);
            return;
        }

        base.CheckTransitions();
    }

    // ─── Movement ────────────────────────────────────────────────────────────

    protected virtual void ApplyMovementForce()
    {
        float horizontal = State.Direction.x;
        if (horizontal == 0f) return;

        Vector2 moveDir = GetSlopeMoveDir();
        float speed = State.FloorAngle != 0f ? Rb.velocity.magnitude : Mathf.Abs(Rb.velocity.x);

        Vector2 slopeDir = moveDir * Mathf.Sign(horizontal);
        float speedAlongInput = Vector2.Dot(Rb.velocity, slopeDir);
        bool movingWithInput = speedAlongInput > 0f;

        float mult = GetSlopeMultiplier(horizontal);
        float effectiveMaxSpeed = Cfg.MaxSpeed * mult;
        float effectiveForce = Cfg.MoveSpeed * mult;

        if (speed <= effectiveMaxSpeed || !movingWithInput)
        {
            Rb.AddForce(horizontal * effectiveForce * moveDir);
        }
        else
        {
            float activeBrakingForce = Cfg.SlowDownForce;
            if (slopeDir.y > 0.01f)
                {
                    // Multiply the braking force when going uphill, so you slow down faster
                    activeBrakingForce *= 4f; 
                }

            Rb.AddForce(-slopeDir * activeBrakingForce);
        }
    }

    protected Vector2 GetSlopeMoveDir()
    {
        // Derive the slope tangent directly from the stored hit normal.
        // This is always correct regardless of which way the slope faces.
        // normal=(0.71, 0.71) → tangent right-along-slope = (0.71, -0.71)
        //                       tangent left-along-slope  = (-0.71, 0.71)
        // Using FloorAngle (a scalar) loses the sign of normal.x and always
        // produces an upper-right direction, killing uphill force.
        Vector2 n = State.FloorNormal;
        if (n == Vector2.zero) return Vector2.right; // flat ground fallback
        // Rotate normal 90° clockwise → right-facing tangent along slope surface
        return new Vector2(n.y, -n.x);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Mario is running (run button held, speed above walk threshold).
///
/// Transitions out to:
/// - Walk : run button released
/// - Skid : changing direction at max run speed
/// - Idle : no input and stopped
/// - Crouch: pressing down
/// - Fall  : walked off ledge (base)
/// - Rise  : jump pressed (base)
/// </summary>
public class RunState : WalkState
{
    public override string ID => MarioStateID.Run;
    public override System.Collections.Generic.IEnumerable<string> Tags
        => new[] { MarioStateTags.Grounded };


    public override void CheckTransitions()
    {
        if (IsPressingDown && State.CanCrouch && !State.Carrying && !State.Climbing)
        {
            RequestTransition(MarioStateID.Crouch);
            return;
        }

        float currentSpeed = State.FloorAngle != 0f ? Rb.velocity.magnitude : Mathf.Abs(Rb.velocity.x);
        if (!HasHorizontalInput && currentSpeed < 0.1f)
        {
            RequestTransition(MarioStateID.Idle);
            return;
        }

        if (!State.RunPressed)
        {
            RequestTransition(MarioStateID.Walk);
            return;
        }

        if (IsChangingDirection && currentSpeed >= Cfg.MaxRunSpeed * 0.9f)
        {
            RequestTransition(MarioStateID.Skid);
            return;
        }

        base.CheckTransitions();
    }

    protected override void ApplyMovementForce()
    {
        float horizontal = State.Direction.x;
        if (horizontal == 0f) return;

        Vector2 moveDir = GetSlopeMoveDir();
        float speed = State.FloorAngle != 0f ? Rb.velocity.magnitude : Mathf.Abs(Rb.velocity.x);

        Vector2 slopeDir = moveDir * Mathf.Sign(horizontal);
        float speedAlongInput = Vector2.Dot(Rb.velocity, slopeDir);
        bool movingWithInput = speedAlongInput > 0f;

        float mult = GetSlopeMultiplier(horizontal);
        float effectiveMaxSpeed = Cfg.MaxRunSpeed * mult;
        float effectiveForce = Cfg.RunSpeed * mult;

        if (speed <= effectiveMaxSpeed || !movingWithInput)
        {
            Rb.AddForce(horizontal * effectiveForce * moveDir);
        }
        else
        {
            float activeBrakingForce = Cfg.SlowDownForce;
            if (slopeDir.y > 0.01f)
                {
                    // Multiply the braking force when going uphill, so you slow down faster
                    activeBrakingForce *= 4f; 
                }

            Rb.AddForce(-slopeDir * activeBrakingForce);
        }
    }
}