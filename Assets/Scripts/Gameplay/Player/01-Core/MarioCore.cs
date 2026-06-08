using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// The central hub for a single Mario player instance.
///
/// Responsibilities:
/// - Owns the MarioState (the shared blackboard)
/// - Holds references to all sibling modules
/// - Bootstraps initialization order
/// - Registers with PlayerRegistry for multiplayer
/// - Resets per-frame flags before modules run
///
/// It does NOT contain any gameplay logic.
/// Every module accesses siblings through Core, never via GetComponent on its own.
///
/// Multiplayer: each player prefab has its own MarioCore with a unique PlayerIndex.
/// Events carry PlayerIndex so listeners can filter by player.
/// </summary>
[DefaultExecutionOrder(-100)] // Core initializes before all modules
public class MarioCore : MonoBehaviour
{
    // ─── Player Identity ─────────────────────────────────────────────────────

    [Header("Identity")]
    [Tooltip("0 = P1, 1 = P2, etc. Set by PlayerRegistry at spawn time.")]
    public int PlayerIndex = 0;

    // ─── Shared State ────────────────────────────────────────────────────────

    /// <summary>The single source of truth. Modules read/write here.</summary>
    public MarioState State { get; private set; } = new MarioState();

    // ─── Module References ───────────────────────────────────────────────────
    // All modules are required components. Core caches them here so no
    // module ever needs to call GetComponent on a sibling.

    public MarioStateMachine StateMachine  { get; private set; }
    public MarioInput        Input         { get; private set; }
    public MarioPhysics      Physics       { get; private set; }

    // Detection
    public MarioGroundDetection GroundDetection { get; private set; }
    public MarioWallDetection   WallDetection   { get; private set; }

    // Modules
    public MarioAnimatorController AnimatorController { get; private set; }
    public MarioAudio              Audio              { get; private set; }
    public MarioCombat             Combat             { get; private set; }
    public MarioCarry              Carry              { get; private set; }
    public MarioPowerup            Powerup            { get; private set; }
    public MarioAbilityManager     AbilityManager     { get; private set; }
    public MarioSwimming           Swimming           { get; private set; }

    // ─── Unity Components ────────────────────────────────────────────────────
    // Exposed in inspector so you can verify the correct child objects are wired.
    // Leave blank at edit-time — they are auto-resolved in Awake via GetComponent/GetComponentInChildren.
    [field: SerializeField] public Rigidbody2D   Rb            { get; private set; }
    [field: SerializeField] public BoxCollider2D Collider      { get; private set; }
    [field: SerializeField] public BoxCollider2D CrushCollider { get; private set; }
    [field: SerializeField] public PlayerInput   PlayerInput   { get; private set; }

    // ─── Cached original collider values ─────────────────────────────────────

    public float ColliderOriginalHeight       { get; private set; }
    public float ColliderOriginalOffsetY      { get; private set; }
    public float CrushColliderOriginalOffsetY { get; private set; }

    // ─── Abilities ───────────────────────────────────────────────────────────

    private readonly List<MarioAbility> _abilities = new();
    public IReadOnlyList<MarioAbility> Abilities => _abilities;

    // ─── Lifecycle ───────────────────────────────────────────────────────────

    private void Awake()
    {
        // Create state first — modules may read it in their Awake
        State = new MarioState
        {
            PlayerIndex = PlayerIndex
        };

        // Cache Unity components
        Rb            = GetComponent<Rigidbody2D>();
        Collider      = GetComponentInChildren<BoxCollider2D>();
        CrushCollider = GetComponentInChildren<CrushDetection>().gameObject.GetComponent<BoxCollider2D>();
        PlayerInput   = GetComponent<PlayerInput>();

        ColliderOriginalHeight  = Collider.size.y;
        ColliderOriginalOffsetY = Collider.offset.y;
        CrushColliderOriginalOffsetY = CrushCollider.offset.y;

        // Cache all sibling modules
        // Order matters: StateMachine last so modules are ready when it initializes
        Input              = GetComponent<MarioInput>();
        Physics            = GetComponent<MarioPhysics>();
        GroundDetection    = GetComponent<MarioGroundDetection>();
        WallDetection      = GetComponent<MarioWallDetection>();
        Combat             = GetComponent<MarioCombat>();
        Carry              = GetComponent<MarioCarry>();
        Powerup            = GetComponent<MarioPowerup>();
        Audio              = GetComponent<MarioAudio>();
        AnimatorController = GetComponent<MarioAnimatorController>();
        AbilityManager     = GetComponent<MarioAbilityManager>();
        Swimming           = GetComponent<MarioSwimming>();
        StateMachine       = GetComponent<MarioStateMachine>();

        // Collect abilities and initialize them now — all modules are cached above,
        // so ability OnEnable flags (CanWallJump, CanSpinJump, etc.) will find a
        // valid Core reference. Previously this was deferred to Start(), which meant
        // OnEnable fired before Initialize() and left ability flags as false.
        _abilities.AddRange(GetComponents<MarioAbility>());
        AbilityManager?.Initialize(this);
    }

    private void Start()
    {
        State.FacingRight = true;

        RegisterWithRegistry();
    }

    private void Update()
    {
        if (State.IsPaused || State.IsFrozen) return;

        HandleCarryThrow();
        HandleTimerDeath();
        NotifyAbilities(a => a.onUpdate());
    }

    private void FixedUpdate()
    {
        // Reset transient per-frame flags before any module runs
        State.ResetFrameFlags();

        // Tick ability fixed-update hooks after FSM runs
        NotifyAbilities(a => a.onFixedUpdate());
    }

    // ─── Carry Throw (from original MarioMovement.Update) ────────────────────

    /// <summary>
    /// Handles run-release throw/drop while carrying.
    /// Preserved from original Update() — input releases are detected here
    /// because they need to react the same frame the button is released.
    /// </summary>
    private void HandleCarryThrow()
    {
        if (!State.Carrying || State.RunPressed) return;

        if (!State.OnGround)
        {
            // Airborne: pressing down = hold for ground pound, else throw
            if (State.Direction.y >= -0.5f)
                Carry.ThrowCarry();
            // direction.y < -0.5: ground pound will use the object, don't throw yet
        }
        else
        {
            // Grounded: down = drop, else throw
            if (State.Direction.y < -0.5f)
                Carry.DropCarry();
            else
                Carry.ThrowCarry();
        }
    }

    // ─── Timer Death ─────────────────────────────────────────────────────────

    private void HandleTimerDeath()
    {
        var gm    = GameManager.Instance;
        var timer = gm != null ? gm.GetSystem<TimerManager>() : null;
        if (timer == null) timer = FindObjectOfType<TimerManager>(true);
        if (timer != null && timer.CurrentTime <= 0)
            Combat.ToDead();
    }

    // ─── Ability Management ──────────────────────────────────────────────────

    public void AddAbility(MarioAbility ability)
    {
        if (!_abilities.Contains(ability))
            _abilities.Add(ability);
    }

    public void RemoveAbility(MarioAbility ability)
    {
        _abilities.Remove(ability);
    }

    /// <summary>
    /// Iterates abilities safely (backwards) so destroyed entries are pruned.
    /// </summary>
    public void NotifyAbilities(System.Action<MarioAbility> action)
    {
        for (int i = _abilities.Count - 1; i >= 0; i--)
        {
            if (_abilities[i] == null)
            {
                _abilities.RemoveAt(i);
                continue;
            }
            action(_abilities[i]);
        }
    }

    // ─── Input Control ───────────────────────────────────────────────────────

    public void DisableInputs()
    {
        if (!gameObject.activeSelf) return;

        // Only set the lock flag — do NOT deactivate/disable PlayerInput.
        // DeactivateInput resets all action states so held buttons appear
        // released after re-activation, breaking held-run detection.
        // MarioInput.Update already gates on InputLocked so no input leaks through.
        State.InputLocked = true;
    }

    public void EnableInputs()
    {
        if (!gameObject.activeSelf) return;

        // Just clear the lock — action states were never reset so held buttons
        // like Run are still correctly pressed and MarioInput picks them up immediately.
        State.InputLocked = false;
    }

    // ─── Module Enable / Disable (used by MarioSlip) ─────────────────────────

    /// <summary>
    /// Enables or disables all gameplay modules at once.
    /// Used by MarioSlip, which takes over movement for the slip arc
    /// then hands control back by re-enabling.
    /// </summary>
    public void SetModulesEnabled(bool enabled)
    {
        StateMachine.enabled      = enabled;
        Input.enabled             = enabled;
        GroundDetection.enabled   = enabled;
        WallDetection.enabled     = enabled;

        if (!enabled)
        {
            Rb.gravityScale = 0f;
            Rb.velocity     = Vector2.zero;
        }
    }

    // ─── Pause ───────────────────────────────────────────────────────────────

    public void SetPaused(bool paused)
    {
        State.IsPaused = paused;

        if (paused)
        {
            Rb.velocity = new Vector2(0f, Rb.velocity.y);
            AnimatorController?.SetAnimatorSpeed(0f);
        }
        else
        {
            AnimatorController?.SetAnimatorSpeed(1f);
        }
    }

    // ─── Freeze (cutscenes, transformations) ─────────────────────────────────

    public void Freeze()
    {
        State.IsFrozen   = true;
        Rb.velocity      = Vector2.zero;
        Rb.bodyType      = RigidbodyType2D.Kinematic;
        AnimatorController?.SetAnimatorEnabled(false);
    }

    public void Unfreeze()
    {
        State.IsFrozen = false;
        Rb.bodyType    = RigidbodyType2D.Dynamic;
        AnimatorController?.SetAnimatorEnabled(true);
    }

    // ─── Registry ────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        RegisterWithRegistry();
    }

    private void RegisterWithRegistry()
    {
        var gm       = GameManager.Instance;
        var registry = gm != null ? gm.GetSystem<PlayerRegistry>() : null;
        if (registry == null) registry = FindObjectOfType<PlayerRegistry>(true);
        registry?.RegisterPlayer(this, PlayerIndex);
    }
}