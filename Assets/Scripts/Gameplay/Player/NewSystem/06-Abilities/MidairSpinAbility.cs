using UnityEngine;

/// <summary>
/// Grants Mario the midair spin (twirl / glide).
/// Sets State.CanMidairSpin and State.AllowMultipleMidairSpins.
///
/// The actual twirl physics live in MidairSpinState.
/// This ability exposes the tuning knobs designers want per-powerup:
/// - Whether multiple spins are allowed per jump
/// - The cooldown between spins (lives on MarioPhysicsConfig)
/// </summary>
public class MidairSpinAbility : MarioAbility
{
    [Tooltip("If true, Mario can twirl multiple times per jump (with cooldown)")]
    public bool AllowMultipleSpins = false;

    public override void Initialize(MarioCore core)
    {
        base.Initialize(core);
        ApplyFlags();
    }

    private void OnEnable()
    {
        if (Core == null) return;
        ApplyFlags();
    }

    private void OnDisable()
    {
        if (Core == null) return;
        State.CanMidairSpin            = false;
        State.AllowMultipleMidairSpins = false;
    }

    private void ApplyFlags()
    {
        State.CanMidairSpin            = true;
        State.AllowMultipleMidairSpins = AllowMultipleSpins;
    }

    public override void onSpinPressed()
    {
        // The FSM transition is already handled by RiseState/FallState.CheckTransitions.
    }
}
