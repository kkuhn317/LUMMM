using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Abstract base class for every state in Mario's FSM.
///
/// Each concrete state owns:
///   Enter / Exit / Update / FixedUpdate / CheckTransitions
///
/// States never transition themselves — they call RequestTransition().
/// States access everything through Core — never via GetComponent.
///
/// IDs are strings (see MarioStateID) so new states can be added from
/// anywhere without editing the central ID enum.
///
/// Tags declare which super-state groups this state belongs to.
/// Override Tags to opt into groups like MarioStateTags.Airborne.
/// The StateMachine uses these for IsGrounded / IsAirborne queries.
/// </summary>
public abstract class MarioStateBase
{
    // ─── Core Access ─────────────────────────────────────────────────────────

    protected MarioCore          Core        { get; private set; }
    protected MarioState         State       => Core.State;
    protected MarioStateMachine  Machine     => Core.StateMachine;
    protected int                PlayerIndex => Core.PlayerIndex;

    // ─── Identity ────────────────────────────────────────────────────────────

    /// <summary>
    /// The unique string ID of this state.
    /// Use a constant from MarioStateID for built-in states,
    /// or define your own string for custom states.
    /// </summary>
    public abstract string ID { get; }

    /// <summary>
    /// Super-state tags this state belongs to.
    /// Override to declare membership: return new[]{ MarioStateTags.Airborne };
    /// The base implementation returns an empty array (no super-state).
    /// </summary>
    public virtual IEnumerable<string> Tags => System.Array.Empty<string>();

    // ─── Lifecycle ───────────────────────────────────────────────────────────

    public virtual void Initialize(MarioCore core) { Core = core; }

    public virtual void Enter(string previousState) { }
    public virtual void Exit(string nextState) { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
    public virtual void CheckTransitions() { }

    // ─── Transition Helper ───────────────────────────────────────────────────

    protected void RequestTransition(string to) => Machine.RequestTransition(to);

    // ─── Shared Utilities ────────────────────────────────────────────────────

    protected bool IsPressingDown => State.Direction.y < -0.5f;
    protected bool IsPressingUp => State.Direction.y > 0.5f;
    protected bool HasHorizontalInput => Mathf.Abs(State.Direction.x) > 0.01f;
    protected bool IsMovingFast => Mathf.Abs(Core.Rb.velocity.x) > 0.2f;
    protected bool IsChangingDirection =>
        (State.Direction.x > 0 && Core.Rb.velocity.x < 0) ||
        (State.Direction.x < 0 && Core.Rb.velocity.x > 0);

    /// <summary>Uses Mario's crouched collision shape.</summary>
    protected void ApplyCrouchCollider()
    {
        var config = Core.Physics.Config;
        var col = Core.Collider;
        col.size   = new Vector2(col.size.x, config.CrouchColliderHeight);
        col.offset = new Vector2(col.offset.x, config.CrouchColliderOffsetY);

        var crushCol = Core.CrushCollider;
        crushCol.offset = new Vector2(crushCol.offset.x, config.CrouchCrushDetectOffsetY);
    }

    /// <summary>Restores the collision shape cached when Mario was initialized.</summary>
    protected void RestoreStandingCollider()
    {
        var col = Core.Collider;
        col.size   = new Vector2(col.size.x, Core.ColliderOriginalHeight);
        col.offset = new Vector2(col.offset.x, Core.ColliderOriginalOffsetY);

        var crushCol = Core.CrushCollider;
        crushCol.offset = new Vector2(crushCol.offset.x, Core.CrushColliderOriginalOffsetY);
    }

    /// <summary>
    /// Fully exits crouch, restores collision, and notifies listeners.
    /// Do not call this for a normal crouch-jump, which intentionally carries
    /// its crouched pose and collider into the air until another state replaces it.
    /// </summary>
    protected void EndCrouching()
    {
        bool wasCrouching = State.IsCrouching;
        State.IsCrouching = false;

        if (!wasCrouching)
            return;

        RestoreStandingCollider();
        MarioEvents.FireCrouchEnded(PlayerIndex);
    }
}
