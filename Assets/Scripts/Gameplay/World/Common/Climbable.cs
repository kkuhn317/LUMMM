using UnityEngine;

/// <summary>
/// Place on any ladder, rope, or vine GameObject with a trigger collider.
/// </summary>
public class Climbable : MonoBehaviour
{
    public enum ClimbMethod { Front, Side }

    [Tooltip("Front = ladder/rope (Mario faces forward, moves X and Y).\n" +
             "Side  = pipe/wall (Mario hugs the surface, moves Y only).")]
    public ClimbMethod climbMethod = ClimbMethod.Front;

    [Tooltip("Movement speed while climbing.")]
    public float climbSpeed = 4f;

    [Tooltip("Width of the climbable surface — used by ClimbSideState to lock Mario's X position.")]
    public float width = 1f;

    [Tooltip("Reference point for the pole center. If null, uses transform.position. " +
             "Assign a child transform placed at the pole's X center.")]
    public Transform poleCenter;

    /// <summary>The X position of the pole center — uses poleCenter if assigned, else transform.</summary>
    public float PoleCenterX => poleCenter != null ? poleCenter.position.x : transform.position.x;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var core = other.GetComponentInParent<MarioCore>() ?? other.GetComponent<MarioCore>();
        if (core == null) return;

        // Don't override CurrentClimbable if Mario is already actively climbing
        // a different climbable — this prevents a Side climbable from being
        // overwritten by an adjacent Front climbable trigger (and vice versa)
        // while Mario is mid-climb.
        if (core.State.Climbing && core.State.CurrentClimbable != null
            && core.State.CurrentClimbable != this)
            return;

        // Don't re-grab immediately after detaching — JustLeftClimbing is set
        // for one frame on exit to prevent re-entry while still inside the trigger.
        if (core.State.JustLeftClimbing)
            return;

        core.State.CurrentClimbable = this;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var core = other.GetComponentInParent<MarioCore>() ?? other.GetComponent<MarioCore>();
        if (core == null) return;

        // Re-set CurrentClimbable after JustLeftClimbing expires so Mario can re-grab
        if (!core.State.Climbing && !core.State.JustLeftClimbing
            && core.State.CurrentClimbable == null)
            core.State.CurrentClimbable = this;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var core = other.GetComponentInParent<MarioCore>() ?? other.GetComponent<MarioCore>();
        if (core == null) return;

        if (core.State.CurrentClimbable == this)
            core.State.CurrentClimbable = null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.35f);
        var col = GetComponent<Collider2D>();
        if (col != null)
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
    }
}