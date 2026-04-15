using UnityEngine;

/// <summary>
/// Grants Mario the ability to wall-slide and wall-jump.
/// Sets State.CanWallJump when enabled/disabled.
///
/// Physics live in WallSlideState and WallJumpState.
/// </summary>
public class WallJumpAbility : MarioAbility
{
    [Tooltip("If true, Mario can wall jump even while carrying an object")]
    public bool AllowWhenCarrying = false;

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
        State.CanWallJump                  = false;
        State.CanWallJumpWhenHoldingObject = false;
    }

    private void ApplyFlags()
    {
        State.CanWallJump                  = true;
        State.CanWallJumpWhenHoldingObject = AllowWhenCarrying;
    }
}
