using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Mario's Hierarchical Finite State Machine.
///
/// EXTENSIBILITY:
/// - State IDs are strings. New states never require editing MarioStateID.
/// - Call RegisterState() at any time (from powerup Awake, ability Initialize,
///   or any external script) to inject a new state or replace an existing one.
/// - Super-state queries (IsGrounded, IsAirborne, etc.) are tag-based:
///   states declare their tags; queries are O(1) HashSet lookups.
///   Add new tags in MarioStateTags and declare them on your state's Tags property.
/// - CanTransition rules use HasTag() — they automatically work for any new
///   state that opts into the relevant tag.
///
/// MULTIPLAYER:
/// Each player has their own MarioCore + MarioStateMachine instance.
/// All events carry PlayerIndex — listeners filter by index.
/// No static state anywhere in the FSM.
///
/// HOW TO ADD A NEW STATE:
/// 1. Create a class extending MarioStateBase with a unique string ID.
/// 2. Override Tags to declare super-state membership.
/// 3. Call Core.StateMachine.RegisterState(new MyState()) from anywhere
///    that has access to Core (ability Initialize, powerup Awake, etc.).
/// 4. Done — no central file needs editing.
/// </summary>
[DefaultExecutionOrder(-90)]
public class MarioStateMachine : MonoBehaviour
{
    // ─── State Registry ──────────────────────────────────────────────────────

    private readonly Dictionary<string, MarioStateBase>      _states   = new();
    private readonly Dictionary<string, HashSet<string>>     _tagIndex = new(); // stateId → tags
    private readonly Dictionary<string, HashSet<string>>     _tagStates= new(); // tag → stateIds

    // ─── Current State ───────────────────────────────────────────────────────

    public MarioStateBase CurrentState  { get; private set; }
    public string         CurrentStateID => CurrentState?.ID ?? MarioStateID.Idle;
    public string         PreviousStateID { get; private set; } = MarioStateID.Idle;

    // ─── Super-State Queries ─────────────────────────────────────────────────
    // All O(1). Automatically includes any registered state with the matching tag.

    public bool IsGrounded => HasTag(CurrentStateID, MarioStateTags.Grounded);
    public bool IsAirborne => HasTag(CurrentStateID, MarioStateTags.Airborne);
    public bool IsSwimming => HasTag(CurrentStateID, MarioStateTags.Swimming);
    public bool IsClimbing => HasTag(CurrentStateID, MarioStateTags.Climbing);
    public bool IsLocked   => HasTag(CurrentStateID, MarioStateTags.Locked);
    public bool IsDead     => HasTag(CurrentStateID, MarioStateTags.Dead);

    // ─── Pending Transition ──────────────────────────────────────────────────

    private bool   _hasTransitionRequest;
    private string _pendingTransition;

    // ─── References ──────────────────────────────────────────────────────────

    private MarioCore _core;
    public  int       PlayerIndex => _core.PlayerIndex;

    // ─── Lifecycle ───────────────────────────────────────────────────────────

    private void Awake()
    {
        _core = GetComponent<MarioCore>();
    }

    private void Start()
    {
        BuildDefaultStates();
        ForceTransition(MarioStateID.Idle);
    }

    private void Update()
    {
        CurrentState?.Update();
    }

    private void FixedUpdate()
    {
        CurrentState?.FixedUpdate();
        CurrentState?.CheckTransitions();

        if (_hasTransitionRequest)
        {
            ExecuteTransition(_pendingTransition);
            _hasTransitionRequest = false;
        }
    }

    // ─── State Registration ──────────────────────────────────────────────────

    /// <summary>
    /// Registers a state with the FSM.
    /// Replaces any existing state with the same ID — powerups can override
    /// built-in states (e.g. replace RiseState with a double-jump variant).
    /// Safe to call from Awake/Start or during gameplay.
    /// </summary>
    public void RegisterState(MarioStateBase state)
    {
        state.Initialize(_core);

        // Remove old tag entries for this ID if replacing
        if (_tagIndex.TryGetValue(state.ID, out var oldTags))
        {
            foreach (var tag in oldTags)
            {
                if (_tagStates.TryGetValue(tag, out var stateSet))
                    stateSet.Remove(state.ID);
            }
        }

        _states[state.ID] = state;

        // Index new tags
        var newTags = new HashSet<string>(state.Tags);
        _tagIndex[state.ID] = newTags;

        foreach (var tag in newTags)
        {
            if (!_tagStates.TryGetValue(tag, out var stateSet))
            {
                stateSet = new HashSet<string>();
                _tagStates[tag] = stateSet;
            }
            stateSet.Add(state.ID);
        }
    }

    // ─── Tag Queries ─────────────────────────────────────────────────────────

    /// <summary>Returns true if the given state ID has the given tag.</summary>
    public bool HasTag(string stateId, string tag)
    {
        return _tagIndex.TryGetValue(stateId, out var tags) && tags.Contains(tag);
    }

    /// <summary>Returns all registered state IDs that have the given tag.</summary>
    public IEnumerable<string> GetStatesWithTag(string tag)
    {
        return _tagStates.TryGetValue(tag, out var states)
            ? (IEnumerable<string>)states
            : System.Array.Empty<string>();
    }

    // ─── Transition API ──────────────────────────────────────────────────────

    /// <summary>
    /// Request a transition. Validated against CanTransition rules.
    /// Deferred to end of FixedUpdate to avoid re-entrancy.
    /// Last request in a frame wins.
    /// </summary>
    public void RequestTransition(string to)
    {
        if (!CanTransition(to))
        {
#if UNITY_EDITOR
            // Only warn if this is a genuine blocked transition, not a same-state no-op.
            if (to != CurrentStateID)
                Debug.LogWarning($"[FSM P{PlayerIndex}] Blocked: {CurrentStateID} → {to}");
#endif
            return;
        }

        _hasTransitionRequest = true;
        _pendingTransition    = to;
    }

    /// <summary>
    /// Bypasses CanTransition validation.
    /// Use only for: initialization, death, cutscene start, external forced transitions.
    /// </summary>
    public void ForceTransition(string to)
    {
        if (!_states.ContainsKey(to))
        {
            Debug.LogError($"[FSM P{PlayerIndex}] State not registered: '{to}'");
            return;
        }
        ExecuteTransition(to);
    }

    // ─── Jump Block Check ────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if any active ability is blocking the jump.
    /// Called by GroundedStateBase before allowing Rise/SpinJump transitions.
    /// </summary>
    public bool IsJumpBlocked()
    {
        bool blocked = false;
        _core.NotifyAbilities(a => { if (a.isBlockingJump) blocked = true; });
        return blocked;
    }

    // ─── Transition Rules ────────────────────────────────────────────────────

    /// <summary>
    /// Permissive by default — only restricts transitions that would cause bugs.
    /// Tag-based so rules automatically cover new states that opt into tags.
    /// </summary>
    private bool CanTransition(string to)
    {
        // Silent no-op: already in this state. Not an error, not a warning.
        if (to == CurrentStateID) return false;
        
        if (!_states.ContainsKey(to))
        {
            Debug.LogError($"[FSM P{PlayerIndex}] Unknown state: '{to}'");
            return false;
        }

        // Dead is a terminal state
        if (IsDead) return false;

        // Locked only allows forced-out (ForceTransition bypasses this)
        if (IsLocked && !HasTag(to, MarioStateTags.Dead)) return false;

        // Ground pound requires being airborne
        if (to == MarioStateID.GroundPoundSpin && !IsAirborne) return false;

        // Wall slide requires being airborne
        if (to == MarioStateID.WallSlide && !IsAirborne) return false;

        // Midair spin requires airborne and not already spinning
        if (to == MarioStateID.MidairSpin &&
            (!IsAirborne || CurrentStateID == MarioStateID.MidairSpin))
            return false;

        // Climbing requires a climbable in range
        if ((to == MarioStateID.ClimbFront || to == MarioStateID.ClimbSide) &&
            _core.State.CurrentClimbable == null)
            return false;

        // Side climb blocks re-entry for one frame after detaching
        if (to == MarioStateID.ClimbSide && _core.State.JustLeftClimbing)
            return false;

        return true;
    }

    // ─── Execution ───────────────────────────────────────────────────────────

    private void ExecuteTransition(string to)
    {
        if (!_states.TryGetValue(to, out var nextState))
        {
            Debug.LogError($"[FSM P{PlayerIndex}] State not registered: '{to}'");
            return;
        }

        string from = CurrentStateID;

        CurrentState?.Exit(to);

        PreviousStateID = from;
        CurrentState    = nextState;

        CurrentState.Enter(from);

        MarioEvents.FireStateChanged(PlayerIndex, to);

#if UNITY_EDITOR
        Debug.Log($"[FSM P{PlayerIndex}] {from} → {to}");
#endif
    }

    // ─── Convenience ─────────────────────────────────────────────────────────

    public bool IsInState(string id) => CurrentStateID == id;

    public T GetState<T>() where T : MarioStateBase
    {
        foreach (var state in _states.Values)
            if (state is T typed) return typed;
        return null;
    }

    public MarioStateBase GetState(string id)
        => _states.TryGetValue(id, out var s) ? s : null;

    // ─── Default State Construction ──────────────────────────────────────────

    private void BuildDefaultStates()
    {
        RegisterState(new IdleState());
        RegisterState(new WalkState());
        RegisterState(new RunState());
        RegisterState(new SkidState());
        RegisterState(new CrouchState());
        RegisterState(new CrawlState());
        RegisterState(new PushState());
        RegisterState(new RiseState());
        RegisterState(new FallState());
        RegisterState(new WallSlideState());
        RegisterState(new MidairSpinState());
        RegisterState(new SpinJumpState());
        RegisterState(new WallJumpState());
        RegisterState(new GroundPoundSpinState());
        RegisterState(new GroundPoundFallState());
        RegisterState(new GroundPoundLandState());
        RegisterState(new SwimIdleState());
        RegisterState(new SwimState());
        RegisterState(new ClimbFrontState());
        RegisterState(new ClimbSideState());
        RegisterState(new LockedState());
        RegisterState(new DeadState());
    }
}