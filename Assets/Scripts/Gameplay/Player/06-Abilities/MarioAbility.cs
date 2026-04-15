using UnityEngine;

/// <summary>
/// Base class for all Mario abilities.
///
/// Abilities are MonoBehaviours that sit on the same GameObject as MarioCore.
/// They extend Mario's moveset without touching FSM state or physics directly —
/// they read from MarioState, fire events, and request FSM transitions through Core.
///
/// Lifecycle:
///   MarioAbilityManager.Initialize() calls Initialize(core) on every MarioAbility
///   found on the GameObject after all other modules are ready.
///
/// Hooks (override what you need):
///   onSpinPressed        — spin/twirl/spin-jump button pressed
///   onShootPressed       — shoot/fire button pressed  
///   onExtraActionPressed — extra action button pressed (cape, etc.)
///   onFixedUpdate        — called after the FSM ticks each FixedUpdate
///   onUpdate             — called each Update, for visual / non-physics logic
///
/// isBlockingJump: set true while this ability is active to suppress
/// jumping and spin-jumping (used by CapeAttack during its cooldown).
/// </summary>
public class MarioAbility : MonoBehaviour
{
    /// <summary>
    /// When true, blocks jumping and spin-jumping.
    /// Set by the ability itself (e.g. CapeAttack sets it during cooldown).
    /// Read by RiseFallState and SpinJumpState before allowing a jump.
    /// </summary>
    [HideInInspector] public bool isBlockingJump = false;

    protected MarioCore  Core;
    protected MarioState State       => Core?.State;
    protected int        PlayerIndex => Core?.PlayerIndex ?? 0;

    /// <summary>Called by MarioAbilityManager after all modules are ready.</summary>
    public virtual void Initialize(MarioCore core)
    {
        Core = core;
    }

    public virtual void onSpinPressed()        { }
    public virtual void onShootPressed()       { }
    public virtual void onExtraActionPressed() { }

    /// <summary>
    /// Called every FixedUpdate by MarioCore after the FSM ticks.
    /// Use this instead of MonoBehaviour.FixedUpdate to guarantee execution
    /// order relative to the FSM (always runs after states update).
    /// </summary>
    public virtual void onFixedUpdate() { }

    /// <summary>Called every Update by MarioCore. Use for visual / non-physics logic.</summary>
    public virtual void onUpdate() { }
}