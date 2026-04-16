using UnityEngine;

/// <summary>
/// Shared base for all grounded states: Idle, Walk, Run, Skid, Crouch, Crawl, Push.
///
/// Centralizes the transition checks that every grounded state needs:
/// - Fall off ledge → Fall
/// - Jump pressed   → Rise (or SpinJump / WallJump)
/// - Enter water    → SwimIdle
/// - Grab climbable → ClimbFront / ClimbSide
/// - Ground pound   → not valid while grounded (blocked by CanTransition)
///
/// Concrete states override FixedUpdate for their specific movement logic,
/// and can override CheckTransitions to add their own checks before calling base.
/// </summary>
public abstract class GroundedStateBase : MarioStateBase
{
    protected MarioPhysicsConfig Cfg => Core.Physics.Config;
    protected Rigidbody2D Rb => Core.Rb;

    // ─── Shared Enter/Exit ───────────────────────────────────────────────────

    // Tracks whether this is the first FixedUpdate after landing.
    // Used by ApplyGroundedDrag to skip deceleration drag on the landing frame
    // so incoming momentum is fully preserved when touching down.
    private bool _justLanded;

    public override void Enter(string previousState)
    {
        // Zero out gravity — ground detection handles vertical position
        Rb.gravityScale = 0f;
        State.OnGround  = true;
        _justLanded     = true; // suppress drag for one frame on landing
    }

    public override void Exit(string nextState)
    {
        // Restore gravity for airborne states
        // (Swimming and Climbing states will override it themselves)
        if (nextState == MarioStateID.Rise   ||
            nextState == MarioStateID.Fall   ||
            nextState == MarioStateID.SpinJump ||
            nextState == MarioStateID.WallJump)
        {
            Rb.gravityScale = Cfg.RiseGravity;
        }

        // End look up immediately on exit — avoids a one-frame flicker where
        // HandleLookUp fires LookUpEnded after the animator already moved on
        if (_wasLookingUp)
        {
            MarioEvents.FireLookUpEnded(PlayerIndex);
            State.IsLookingUp = false;
            _wasLookingUp     = false;
        }
    }

    // ─── Shared Physics ──────────────────────────────────────────────────────

    // ─── Look Up ─────────────────────────────────────────────────────────────
    // Handled in the base so any grounded state (Idle, Walk, Run…) can look up.
    // IdleState no longer needs its own HandleLookUp.

    private bool _wasLookingUp;

    public override void Update()
    {
        HandleLookUp();
    }

    private void HandleLookUp()
    {
        // Only look up when there is no horizontal movement and not using an object
        bool lookingUp = !HasHorizontalInput && IsPressingUp && !State.IsUsingObject
                      && !State.ClimbExitedWhilePressingUp;

        if (lookingUp && !_wasLookingUp)
            MarioEvents.FireLookUpStarted(PlayerIndex);
        else if (!lookingUp && _wasLookingUp)
            MarioEvents.FireLookUpEnded(PlayerIndex);

        State.IsLookingUp = lookingUp;
        _wasLookingUp     = lookingUp;
    }

    public override void FixedUpdate()
    {
        ApplyGroundedDrag();
        ClampHorizontalSpeed();
        HandleFacing();
    }



    protected void ApplyGroundedDrag()
    {
        // Skip drag entirely on the landing frame so incoming momentum is preserved.
        // Without this, the high deceleration drag values fire immediately on landing
        // and bleed off horizontal speed, making the ground feel "sticky".
        if (_justLanded)
        {
            _justLanded = false;
            Rb.drag = 0f;
            return;
        }



        float horizontal = State.Direction.x;
        bool  holding    = Mathf.Abs(horizontal) > 0.01f && !IsChangingDirection;

        if (holding)
        {
            Rb.drag = 0f;
            return;
        }

        // Deceleration drag — scales with speed for feel.
        // On slopes velocity is diagonal so use magnitude, not just X —
        // otherwise vel.x of 0.21 on a 45° slope (total speed ~0.3) triggers
        // the maximum drag branch and kills all momentum.
        float spd = State.FloorAngle != 0f ? Rb.velocity.magnitude : Mathf.Abs(Rb.velocity.x);
        Rb.drag = spd switch
        {
            < 0.5f  => 100_000_000f,
            < 5f    => 8f / spd,
            _       => 1.5f
        };

        // Extra drag multiplier when crouching or facing opposite to velocity
        if (State.IsCrouching || (!State.FacingRight && Rb.velocity.x > 0) || (State.FacingRight && Rb.velocity.x < 0))
            Rb.drag *= 1.5f;

        if (Rb.drag > 100_000_000f) Rb.drag = 100_000_000f;
    }

    protected void ClampHorizontalSpeed()
    {
        float maxSpd = State.RunPressed ? Cfg.MaxRunSpeed : Cfg.MaxSpeed;
        if (Mathf.Abs(Rb.velocity.x) > maxSpd && State.RunPressed)
        {
            Rb.velocity = new Vector2(Mathf.Sign(Rb.velocity.x) * maxSpd, Rb.velocity.y);
        }
    }

    protected void HandleFacing()
    {
        float horizontal = State.Direction.x;
        if (!State.IsCapeActive && horizontal != 0f)
            Core.Physics.FlipTo(horizontal > 0f);
    }

    // ─── Ceiling Obstruction Check ───────────────────────────────────────────

    /// <summary>
    /// Returns true if there is a ceiling close enough above the player that
    /// they cannot stand up from a crouch.  Fires three upward raycasts
    /// (left, centre, right) from the top of the crouched collider over a
    /// distance equal to the height difference between the full and crouched
    /// collider, plus a small tolerance.
    ///
    /// Used by CrouchState and CrawlState: when the player releases the
    /// crouch button, the state only exits if this returns false.
    /// </summary>
    protected bool HasCeilingObstruction()
    {
        var col = Core.Collider;

        // Top of the current (crouched) collider in world space
        float crouchedTop = Core.Rb.position.y + col.offset.y + col.size.y * 0.5f;

        // Extra height needed to stand fully upright
        float standHeight = Core.ColliderOriginalHeight;
        float crouchHeight = Cfg.CrouchColliderHeight;
        float neededClearance = (standHeight - crouchHeight) + 0.05f; // 0.05 m tolerance

        Vector2 origin      = new Vector2(Core.Rb.position.x, crouchedTop);
        Vector2 originLeft  = origin + new Vector2(-Cfg.RaycastSeparation, 0f);
        Vector2 originRight = origin + new Vector2( Cfg.RaycastSeparation, 0f);

        LayerMask groundLayer = Core.Physics.GroundLayer;

        RaycastHit2D hitLeft   = Physics2D.Raycast(originLeft,  Vector2.up, neededClearance, groundLayer);
        RaycastHit2D hitCenter = Physics2D.Raycast(origin,       Vector2.up, neededClearance, groundLayer);
        RaycastHit2D hitRight  = Physics2D.Raycast(originRight, Vector2.up, neededClearance, groundLayer);

#if UNITY_EDITOR
        Color rayColor = (hitLeft.collider || hitCenter.collider || hitRight.collider)
            ? Color.red : Color.green;
        Debug.DrawRay(originLeft,  Vector2.up * neededClearance, rayColor);
        Debug.DrawRay(origin,       Vector2.up * neededClearance, rayColor);
        Debug.DrawRay(originRight, Vector2.up * neededClearance, rayColor);
#endif

        return hitLeft.collider != null
            || hitCenter.collider != null
            || hitRight.collider != null;
    }

    // ─── Shared Transition Checks ────────────────────────────────────────────

    public override void CheckTransitions()
    {
        // Priority order matters here — highest priority first

        // 1. Death / locked handled by MarioCombat directly via ForceTransition

        // 2. Fell off a ledge
        if (!State.OnGround)
        {
            RequestTransition(MarioStateID.Fall);
            return;
        }

        // 3. Entered water
        if (State.Swimming)
        {
            RequestTransition(MarioStateID.SwimIdle);
            return;
        }

        // 4. Grabbed climbable
        // Clear climb-exit guards once player releases the respective direction
        if (!IsPressingDown)
            State.ClimbExitedWhilePressingDown = false;
        if (!IsPressingUp)
            State.ClimbExitedWhilePressingUp = false;

        if (!State.Climbing && State.CurrentClimbable != null
            && !State.ClimbExitedWhilePressingDown
            && !State.JustLeftClimbing
            && Mathf.Abs(State.Direction.y) > 0.5f
            && Time.time >= State.JumpTimer) // don't grab if jump was just pressed
        {
            var method = State.CurrentClimbable.climbMethod;
            RequestTransition(method == Climbable.ClimbMethod.Side
                ? MarioStateID.ClimbSide
                : MarioStateID.ClimbFront);
            return;
        }

        // 5. Jump / spin jump buffered
        if (Time.time < State.JumpTimer && !Machine.IsJumpBlocked())
        {
            if (State.SpinJumpQueued && State.CanSpinJump)
            {
#if UNITY_EDITOR
                Debug.Log($"[Jump] Transitioning to SpinJump. SpinQueued={State.SpinJumpQueued} CanSpinJump={State.CanSpinJump}");
#endif
                RequestTransition(MarioStateID.SpinJump);
                return;
            }
            RequestTransition(MarioStateID.Rise);
            return;
        }
    }
}