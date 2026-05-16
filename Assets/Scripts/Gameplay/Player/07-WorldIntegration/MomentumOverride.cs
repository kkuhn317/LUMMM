using UnityEngine;

/// <summary>
/// Zone trigger that disables moving-platform momentum transfer when Mario
/// is inside the area (e.g. door thresholds, narrow passages).
///
/// Ported from MomentumOverride.cs — now uses MarioState via MarioCore.
/// </summary>
public class MomentumOverride : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var core = other.GetComponentInParent<MarioCore>();
        if (core != null) {
            core.State.DoMovingPlatformMomentum = false;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var core = other.GetComponentInParent<MarioCore>();
        if (core != null)
            core.State.DoMovingPlatformMomentum = true;
    }
}
