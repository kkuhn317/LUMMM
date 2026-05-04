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

        if (IsChangingDirection && Mathf.Abs(Rb.velocity.x) >= Cfg.MaxRunSpeed * 0.9f)
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

        // FIX: On a slope, measure total speed along the slope rather than just X.
        // velocity.x alone under-counts speed going uphill and over-counts downhill,
        // causing SlowDownForce to fire at the wrong times.
        float speed = State.FloorAngle != 0f ? Rb.velocity.magnitude : Mathf.Abs(Rb.velocity.x);

        // Compare direction along the slope, not raw velocity.x — on a slope
        // velocity.x sign can mismatch input sign even when moving forward.
        Vector2 slopeDir = moveDir * Mathf.Sign(horizontal);
        float speedAlongInput = Vector2.Dot(Rb.velocity, slopeDir);
        bool movingWithInput = speedAlongInput > 0f;

        if (speed <= Cfg.MaxSpeed || !movingWithInput)
            Rb.AddForce(horizontal * Cfg.MoveSpeed * moveDir);
        else
            Rb.AddForce(-slopeDir * Cfg.SlowDownForce);
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

        if (IsChangingDirection && Mathf.Abs(Rb.velocity.x) >= Cfg.MaxRunSpeed * 0.9f)
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
        Rb.AddForce(horizontal * Cfg.RunSpeed * moveDir);
    }
}