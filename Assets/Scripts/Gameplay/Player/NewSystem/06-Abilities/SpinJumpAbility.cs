using UnityEngine;

/// <summary>
/// Grants Mario the spin jump (lower, spinning arc jump).
/// Sets State.CanSpinJump when enabled/disabled.
///
/// Also owns the spin jump visual effect prefabs and listens to
/// MarioEvents to spawn them at the correct world position.
/// </summary>
public class SpinJumpAbility : MarioAbility
{
    [Header("Effects")]
    [Tooltip("Spawned at contact point when Mario bounces off an enemy with spin jump.")]
    public GameObject SpinJumpBouncePrefab;

    [Tooltip("Spawned at contact point when Mario poofs an enemy with spin jump.")]
    public GameObject SpinJumpPoofPrefab;

    public override void Initialize(MarioCore core)
    {
        base.Initialize(core);
        ApplyFlags();
    }

    private void OnEnable()
    {
        if (Core == null) return;
        State.CanSpinJump = true;
        MarioEvents.OnSpinJumpBounced += OnSpinJumpBounced;
        MarioEvents.OnSpinJumpPoofed  += OnSpinJumpPoofed;
    }

    private void OnDisable()
    {
        if (Core == null) return;
        State.CanSpinJump    = false;
        State.SpinJumpQueued = false;
        State.Spinning       = false;
        MarioEvents.OnSpinJumpBounced -= OnSpinJumpBounced;
        MarioEvents.OnSpinJumpPoofed  -= OnSpinJumpPoofed;
    }

    private void OnSpinJumpBounced(int playerIndex)
    {
        if (playerIndex != PlayerIndex || SpinJumpBouncePrefab == null) return;
        Instantiate(SpinJumpBouncePrefab, Core.transform.position, Quaternion.identity);
    }

    private void OnSpinJumpPoofed(int playerIndex, Vector3 spawnPos)
    {
        if (playerIndex != PlayerIndex || SpinJumpPoofPrefab == null) return;
        Instantiate(SpinJumpPoofPrefab, spawnPos, Quaternion.identity);
    }

    public override void onSpinPressed()
    {
        // SpinJumpQueued is already set by MarioInput.
    }

    private void ApplyFlags() => State.CanSpinJump = true;
}
