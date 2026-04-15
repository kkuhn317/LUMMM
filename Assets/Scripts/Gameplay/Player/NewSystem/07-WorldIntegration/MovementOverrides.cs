using UnityEngine;

/// <summary>
/// Zone trigger that overrides corner correction for the player inside it.
/// Place on a zone collider set as trigger. When Mario enters, corner
/// correction is forced to enableCornerCorrection. On exit, it is restored
/// to the opposite value.
///
/// Ported from CornerCorrectionOverride.cs — now reads/writes MarioState
/// via MarioCore instead of MarioMovement.doCornerCorrection.
/// </summary>
public class CornerCorrectionOverride : MonoBehaviour
{
    [Tooltip("false = disable corner correction in this zone, true = enable it")]
    public bool enableCornerCorrection = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var core = other.GetComponent<MarioCore>();
        if (core != null)
            core.State.DoCornerCorrection = enableCornerCorrection;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var core = other.GetComponent<MarioCore>();
        if (core != null)
            core.State.DoCornerCorrection = !enableCornerCorrection;
    }
}

// ─────────────────────────────────────────────────────────────────────────────

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
        var core = other.GetComponent<MarioCore>();
        if (core != null)
            core.State.DoMovingPlatformMomentum = false;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var core = other.GetComponent<MarioCore>();
        if (core != null)
            core.State.DoMovingPlatformMomentum = true;
    }
}
