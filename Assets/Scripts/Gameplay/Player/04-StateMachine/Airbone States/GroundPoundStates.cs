using UnityEngine;

/// <summary>
/// Phase 1 of ground pound: brief freeze + rotation in the air.
/// Duration: groundPoundSpinTime (default 0.5s).
///
/// Physics: gravity = 0, velocity = 0. Mario hangs in the air.
/// After the timer, automatically transitions to GroundPoundFall.
///
/// Can be cancelled by pressing up → Fall.
/// </summary>
public class GroundPoundSpinState : AirborneStateBase
{
    public override string ID => MarioStateID.GroundPoundSpin;
    public override System.Collections.Generic.IEnumerable<string> Tags => new[] { MarioStateTags.Airborne };

    private float _fallStartTime;

    public override void Enter(string previousState)
    {
        base.Enter(previousState);

        // End any midair spin cleanly
        State.IsMidairSpinning = false;
        State.Spinning         = false;

        State.GroundPounding        = true;
        State.GroundPoundRotating   = true;
        State.GroundPoundLanded     = false;

        Rb.velocity     = Vector2.zero;
        Rb.gravityScale = 0f;
        Rb.drag         = 0f;

        _fallStartTime = Time.time + Cfg.GroundPoundSpinTime;

        MarioEvents.FireGroundPoundStarted(PlayerIndex);
    }

    public override void Exit(string nextState)
    {
        State.GroundPoundRotating = false;

        if (nextState != MarioStateID.GroundPoundFall)
        {
            // Cancelled or interrupted: clean up fully
            State.GroundPounding            = false;
            State.GroundPoundInWater        = false;
            State.WaterGroundPoundStartTime = 0f;
            MarioEvents.FireGroundPoundCancelled(PlayerIndex);
        }
    }

    public override void FixedUpdate()
    {
        // Hold still during spin
        Rb.velocity     = Vector2.zero;
        Rb.gravityScale = 0f;
        Rb.drag         = 0f;
    }

    public override void CheckTransitions()
    {
        // Cancel: pressing up during spin phase
        if (State.Direction.y > 0f)
        {
            State.GroundPounding = false;
            MarioEvents.FireGroundPoundCancelled(PlayerIndex);
            RequestTransition(MarioStateID.Fall);
            return;
        }

        // Timer elapsed → begin the fall
        if (Time.time >= _fallStartTime)
        {
            RequestTransition(MarioStateID.GroundPoundFall);
            return;
        }

        // Landed during spin (extremely rare, but handle it)
        if (State.OnGround)
        {
            RequestTransition(MarioStateID.GroundPoundLand);
            return;
        }

        // Water cancels ground pound
        if (State.Swimming)
        {
            State.GroundPounding = false;
            MarioEvents.FireGroundPoundCancelled(PlayerIndex);
            RequestTransition(MarioStateID.SwimIdle);
            return;
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Phase 2: Mario falls fast toward the ground.
/// Gravity = fallGravity, terminal velocity = 1.5x normal.
/// No horizontal control.
///
/// Cancel: pressing up → Fall (restores normal physics).
///
/// Transitions out to:
/// - GroundPoundLand : OnGround becomes true
/// - Fall            : cancelled by pressing up
/// - SwimIdle        : entered water
/// </summary>
public class GroundPoundFallState : AirborneStateBase
{
    public override string ID => MarioStateID.GroundPoundFall;
    public override System.Collections.Generic.IEnumerable<string> Tags => new[] { MarioStateTags.Airborne };

    public override void Enter(string previousState)
    {
        base.Enter(previousState);

        State.GroundPoundRotating = false;

        // Launch downward
        Rb.velocity     = new Vector2(Rb.velocity.x, -Cfg.JumpSpeed * 1.5f);
        Rb.gravityScale = Cfg.FallGravity;
        Rb.drag         = 0f;

        MarioEvents.FireGroundPoundFalling(PlayerIndex);
    }

    public override void Exit(string nextState)
    {
        if (nextState != MarioStateID.GroundPoundLand)
        {
            // Cancelled or interrupted
            State.GroundPounding            = false;
            State.GroundPoundInWater        = false;
            State.WaterGroundPoundStartTime = 0f;
            MarioEvents.FireGroundPoundCancelled(PlayerIndex);
        }
    }

    public override void FixedUpdate()
    {
        Rb.gravityScale = Cfg.FallGravity;
        Rb.drag         = 0f;

        // Fall faster than normal terminal velocity
        ClampFallSpeed(Cfg.TerminalVelocity * 1.5f);
    }

    public override void CheckTransitions()
    {
        // Cancel: pressing up
        if (State.Direction.y > 0f)
        {
            State.GroundPounding = false;
            Rb.gravityScale      = Cfg.FallGravity;
            MarioEvents.FireGroundPoundCancelled(PlayerIndex);
            RequestTransition(MarioStateID.Fall);
            return;
        }

        // Landed
        if (State.OnGround)
        {
            RequestTransition(MarioStateID.GroundPoundLand);
            return;
        }

        // Water cancels mid-fall
        if (State.Swimming)
        {
            State.GroundPounding = false;
            MarioEvents.FireGroundPoundCancelled(PlayerIndex);
            RequestTransition(MarioStateID.SwimIdle);
            return;
        }

        // Disappeared platform edge case: landed flag was set but ground
        // is gone now — handled by CheckTransitions via OnGround check above
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Phase 3: Mario has hit the ground. Brief input lock before returning to Idle.
/// Lock duration: groundPoundLandLockTime (default 0.25s).
///
/// Physics: velocity = 0, gravity = 0. Locked to ground.
/// Edge case: if the ground disappears, resume GroundPoundFall.
///
/// Fires IGroundPoundable.OnGroundPound on the hit object.
/// Spawns ground pound particles.
/// </summary>
public class GroundPoundLandState : AirborneStateBase
{
    public override string ID => MarioStateID.GroundPoundLand;
    public override System.Collections.Generic.IEnumerable<string> Tags => new[] { MarioStateTags.Airborne };

    private float          _unlockTime;
    private System.Collections.Generic.List<GameObject> _hitObjects = new();

    // Called by MarioGroundDetection when the ground pound lands.
    // Accepts all objects hit (e.g. two blocks when landing between them).
    public void SetHitObjects(System.Collections.Generic.List<GameObject> objects) => _hitObjects = objects;

    public override void Enter(string previousState)
    {
        // Do NOT call base.Enter — we want to stay "on ground"
        State.GroundPoundLanded         = true;
        State.GroundPoundRotating       = false;
        State.GroundPoundInWater        = false;
        State.WaterGroundPoundStartTime = 0f;

        Rb.velocity     = Vector2.zero;
        Rb.gravityScale = 0f;
        Rb.drag         = 0f;

        _unlockTime = Time.time + Cfg.GroundPoundLandLockTime;

        Debug.Log($"[GP Land] hitObjects count={_hitObjects.Count}");

        // Notify all groundpoundable objects (e.g. both blocks when landing between two)
        foreach (var obj in _hitObjects)
        {
            if (obj == null) continue;
            var gp = obj.GetComponent<IGroundPoundable>() 
                ?? obj.transform.root.GetComponent<IGroundPoundable>();
            Debug.Log($"[GP Land] obj={obj.name} gp={gp != null}");
            gp?.OnGroundPound(Core);

            var bumpable = obj.transform.root.GetComponentInChildren<IBumpable>();
            bumpable?.Bump(BlockHitDirection.Down, Core);
        }

        MarioEvents.FireGroundPoundLanded(PlayerIndex, _hitObjects.Count > 0 ? _hitObjects[0] : null);
    }

    public override void Exit(string nextState)
    {
        State.GroundPounding    = false;
        State.GroundPoundLanded = false;
        _hitObjects.Clear();
    }

    public override void FixedUpdate()
    {
        // If something externally cancelled the ground pound (e.g. trampoline)
        // while we are still in this state, release the lock immediately.
        if (!State.GroundPounding)
        {
            RequestTransition(MarioStateID.Rise);
            return;
        }

        // Locked to ground
        Rb.velocity     = Vector2.zero;
        Rb.gravityScale = 0f;

        // Edge case: ground disappeared (destroyed block, etc.)
        if (!State.OnGround)
        {
            State.GroundPoundLanded = false;
            RequestTransition(MarioStateID.GroundPoundFall);
        }
    }

    public override void CheckTransitions()
    {
        // Ground disappeared
        if (!State.OnGround)
        {
            RequestTransition(MarioStateID.GroundPoundFall);
            return;
        }

        // Lock expired → return to idle (or swim idle if in water)
        if (Time.time >= _unlockTime)
        {
            if (State.Swimming)
            {
                RequestTransition(MarioStateID.SwimIdle);
            }
            else
            {
                // Go through OnLanded so crouch-jump state is restored correctly
                OnLanded();
            }
            return;
        }
    }
}