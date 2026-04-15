// ─────────────────────────────────────────────────────────────────────────────
// State Stubs
//
// Each state lives in its own file under StateMachine/States/.
// These stubs make the project compile while layers 3+ are being built.
// Replace each stub with its full implementation file when ready.
// ─────────────────────────────────────────────────────────────────────────────

// ── Grounded ── (implemented in StateMachine/States/Grounded/) ───────────────

// ── Airborne ── (implemented in StateMachine/States/Airborne/) ───────────────

// ── Ground Pound ── (implemented in StateMachine/States/Airborne/) ───────────

// ── Swimming ── (implemented in StateMachine/States/Swimming/) ───────────────

// ── Climbing ── (implemented in StateMachine/States/Climbing/) ───────────────

// ── Special ──────────────────────────────────────────────────────────────────

/// <summary>
/// Locked state: input and physics fully suspended.
/// Used for cutscenes, powerup animations, level up, yeah/celebrate.
/// Other systems call MarioCore.Freeze() then ForceTransition(Locked).
/// </summary>
public class LockedState : MarioStateBase
{
    public override string ID => MarioStateID.Locked;
    public override System.Collections.Generic.IEnumerable<string> Tags => new[] { MarioStateTags.Locked };

    public override void Enter(string previousState)
    {
        Core.Freeze();
        Core.DisableInputs();
    }

    public override void Exit(string nextState)
    {
        Core.Unfreeze();
        Core.EnableInputs();
    }
}

/// <summary>
/// Dead state: terminal. Mario cannot leave this state via normal transitions.
/// Only ForceTransition() can exit it (e.g. respawn system).
/// </summary>
public class DeadState : MarioStateBase
{
    public override string ID => MarioStateID.Dead;
    public override System.Collections.Generic.IEnumerable<string> Tags => new[] { MarioStateTags.Dead };

    public override void Enter(string previousState)
    {
        Core.Freeze();
        Core.DisableInputs();
        MarioEvents.FireDied(Core.PlayerIndex);
    }
}