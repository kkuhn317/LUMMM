using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Owns all raw input reading and translates it into clean state flags.
///
/// Responsibilities:
/// - Reads Unity Input System callbacks
/// - Applies deadzone processing (separate per axis)
/// - Provides mobile button fallbacks
/// - Writes processed values to MarioState
/// - Notifies abilities of shoot/extra action presses
///
/// Does NOT make any gameplay decisions — it only records what the player did.
/// FSM states read from MarioState to decide what to do with that input.
/// </summary>
[RequireComponent(typeof(MarioCore))]
public class MarioInput : MonoBehaviour
{
    // ─── Deadzone ────────────────────────────────────────────────────────────

    [Header("Deadzone")]
    [Tooltip("Input below this magnitude is treated as zero")]
    [Range(0f, 1f)] public float LowerDeadzone = 0.3f;

    [Tooltip("Input above this magnitude is snapped to 1")]
    [Range(0f, 1f)] public float UpperDeadzone = 0.9f;

    // ─── References ──────────────────────────────────────────────────────────

    private MarioCore  _core;
    private MarioState State => _core.State;
    
    // Helpers
    private int  _jumpPressedFrame = -1;
    private bool _wasPressingDown;

    // ─── Post-transformation / level-start input polling ──────────────────────
    // The Unity Input System only fires action callbacks on state *transitions*
    // (press edge, release edge). If a button is already held when a PlayerInput
    // is created — either because the player held run before the level loaded, or
    // because a new Mario prefab was instantiated mid-hold during a powerup
    // transformation — the action starts and stays in Waiting phase. No performed
    // or canceled callbacks ever fire for that hold.
    //
    // Fix: while an action is in Waiting phase, poll InputControl.IsPressed() /
    // ReadValue() directly — raw hardware state, completely independent of action
    // lifecycle. Polling stops per-action the moment that action leaves Waiting
    // (a real press edge was detected), at which point normal callbacks resume.
    private bool        _pollingRun;
    private bool        _pollingJump;
    private bool        _pollingMove;
    private InputAction _runActionCache;
    private InputAction _jumpActionCache;
    private InputAction _moveActionCache;

    // ─── Lifecycle ───────────────────────────────────────────────────────────

    private void Awake()
    {
        _core = GetComponent<MarioCore>();
    }

    private void Start()
    {
        // Activate polling on every Mario spawn — covers both level start (button
        // held before scene loaded) and post-transformation (new prefab, no edge seen).
        // If nothing is held the polling loop exits on the very first Update tick.
        BeginPostTransformationPolling();
    }

    private void Update()
    {
        if (State.InputLocked || State.IsFrozen || State.IsPaused) return;

        // Propagate processed move input to direction every frame
        State.Direction = State.MoveInput;

        // Raw-control polling 
        // For each polled action: if the phase left Waiting, a real edge was detected
        // and normal callbacks resume — stop polling. Otherwise read raw hardware.
        if (_pollingRun || _pollingJump || _pollingMove)
        {
            var pi = _core.PlayerInput;

            if (_pollingRun)
            {
                if (_runActionCache == null)
                    _runActionCache = pi?.actions?.FindAction("Run", throwIfNotFound: false);

                if (_runActionCache == null || _runActionCache.phase != InputActionPhase.Waiting)
                {
                    _pollingRun     = false;
                    _runActionCache = null;
                }
                else
                {
                    State.RunPressed = IsRunHeld(_runActionCache);
                }
            }

            if (_pollingJump)
            {
                if (_jumpActionCache == null)
                    _jumpActionCache = pi?.actions?.FindAction("Jump", throwIfNotFound: false);

                if (_jumpActionCache == null || _jumpActionCache.phase != InputActionPhase.Waiting)
                {
                    _pollingJump     = false;
                    _jumpActionCache = null;
                }
                else
                {
                    State.JumpPressed = IsJumpHeld(_jumpActionCache);
                }
            }

            if (_pollingMove)
            {
                if (_moveActionCache == null)
                    _moveActionCache = pi?.actions?.FindAction("Move", throwIfNotFound: false);

                if (_moveActionCache == null || _moveActionCache.phase != InputActionPhase.Waiting)
                {
                    _pollingMove     = false;
                    _moveActionCache = null;
                }
                else
                {
                    State.MoveInput = ReadHeldMovement(_moveActionCache);
                    State.Direction = State.MoveInput;
                }
            }
        }

        UpdateDownPressedEdge();

        // Continuous carry check (non-press-to-grab mode)
        if (State.RunPressed
            && (!_core.Carry.CrouchToGrab || State.Direction.y < -0.5f)
            && !State.Carrying)
        {
            _core.Carry.CheckForCarry();
        }

        // Flamethrower cheat
        if (GlobalVariables.cheatFlamethrower && State.ShootPressed)
            _core.NotifyAbilities(a => a.onShootPressed());
    }

    // ─── Input Action Callbacks ───────────────────────────────────────────────
    // These are called by Unity's PlayerInput component via SendMessages or
    // UnityEvents. Method names must match the Input Action names exactly.

    // Move ────────────────────────────────────────────────────────────────────

    public void Move(InputAction.CallbackContext context)
    {
        _pollingMove     = false; // Real edge received — polling no longer needed
        _moveActionCache = null;

        State.MoveInput = CombineWithMobileMovement(ApplyDeadzone(context.ReadValue<Vector2>()));
        // Only write Direction immediately if not locked — otherwise Update's
        // guard would block it but the direct write here bypassed it, letting
        // HandleFacing flip Mario during freezes/cutscenes.
        if (!State.InputLocked && !State.IsFrozen && !State.IsPaused)
            State.Direction = State.MoveInput;
    }

    // Run ─────────────────────────────────────────────────────────────────────

    public void Run(InputAction.CallbackContext context)
    {
        if (context.performed) OnRunPressed();
        if (context.canceled && !State.InputLocked && !MobileHeldInputState.Run)
            OnRunReleased();
    }

    public void OnRunPressed()
    {
        State.RunPressed = true;
        _pollingRun      = false; // Real press edge — polling no longer needed
        _runActionCache  = null;

        if (_core.Carry.PressRunToGrab
            && (!_core.Carry.CrouchToGrab || State.Direction.y < -0.5f)
            && !State.Carrying)
        {
            _core.Carry.CheckForCarry();
        }
    }

    public void OnRunReleased()
    {
        // Ignore if jump was pressed this same frame — Input System dual-binding artifact
        if (Time.frameCount == _jumpPressedFrame) return;
        State.RunPressed = false;
        _pollingRun      = false;
        _runActionCache  = null;
    }

    /// <summary>
    /// Activates raw-control polling for Run and Move so Update() tracks actual
    /// hardware state while both actions are in Waiting phase. Called on Start()
    /// (covers buttons held before the level loaded) and by PlayerTransformation
    /// after spawning a new Mario (covers buttons held during a powerup animation).
    /// Polling stops per-action as soon as a real press edge is detected.
    /// </summary>
    public void BeginPostTransformationPolling()
    {
        RestoreHeldMobileInputs();
        _pollingRun      = true;
        _pollingJump     = true;
        _pollingMove     = true;
        _runActionCache  = null;
        _jumpActionCache = null;
        _moveActionCache = null;
    }

    /// <summary>
    /// Reads both sources that can hold Run. The on-screen B button is a
    /// persistent logical toggle, so it has no pressed InputControl for the
    /// Unity Input System poll to discover after Mario's prefab is replaced.
    /// </summary>
    private static bool IsRunHeld(InputAction runAction)
    {
        if (GlobalVariables.OnScreenControls && MobileHeldInputState.Run)
            return true;

        if (runAction == null) return false;

        foreach (var control in runAction.controls)
        {
            if (control.IsPressed()) return true;
        }

        return false;
    }

    private static bool IsJumpHeld(InputAction jumpAction)
    {
        if (GlobalVariables.OnScreenControls && MobileHeldInputState.Jump)
            return true;

        if (jumpAction == null) return false;

        // A freshly-instantiated PlayerInput can leave an already-held action in
        // Waiting, where InputAction.IsPressed() still reports false. Read the
        // bound controls themselves so Space/W/Up survive a prefab swap mid-hold.
        foreach (var control in jumpAction.controls)
        {
            if (control.IsPressed()) return true;
        }

        return false;
    }

    /// <summary>
    /// Seeds held UI inputs onto a newly created Mario without replaying press
    /// edges. Extra/interact actions remain edge-only and are not repeated.
    /// </summary>
    private void RestoreHeldMobileInputs()
    {
        if (!GlobalVariables.OnScreenControls) return;

        State.RunPressed  |= MobileHeldInputState.Run;
        State.JumpPressed |= MobileHeldInputState.Jump;
        State.ShootPressed |= IsMobileShootHeld();
        State.SpinHeld     |= IsMobileSpinHeld();
    }

    private static bool IsMobileShootHeld()
    {
        return GlobalVariables.OnScreenControls
            && MobileHeldInputState.Use
            && !CheatFlags.AllAbilities;
    }

    private static bool IsMobileSpinHeld()
    {
        return GlobalVariables.OnScreenControls
            && (MobileHeldInputState.Spin
                || (MobileHeldInputState.Use && CheatFlags.AllAbilities));
    }

    private void UpdateDownPressedEdge()
    {
        bool isDownNow = State.Direction.y < -0.5f;

        if (!isDownNow)
        {
            _wasPressingDown = false;
            State.DownPressed = false;

            // Once Down is released, ground pound may be triggered again
            // by a new fresh Down press.
            State.RequireDownReleaseForGroundPound = false;
            return;
        }

        if (isDownNow && !_wasPressingDown)
        {
            State.DownPressed = true;
        }

        _wasPressingDown = isDownNow;
    }

    /// <summary>
    /// Resamples only the currently held movement direction after a temporary gameplay lock.
    /// This is used by pipes: entering a pipe intentionally clears MoveInput, but Unity does not
    /// emit another performed callback when the same direction remains held through the exit.
    /// Reading the action directly restores movement without turning a held Jump into a new jump.
    /// </summary>
    public void SyncHeldMovement()
    {
        var pi = _core.PlayerInput;
        var moveAction = pi?.actions?.FindAction("Move", throwIfNotFound: false);

        State.MoveInput = ReadHeldMovement(moveAction);
        if (!State.InputLocked && !State.IsFrozen && !State.IsPaused)
            State.Direction = State.MoveInput;
    }

    /// <summary>
    /// Syncs all held-button state after inputs are re-enabled (e.g. after a door animation).
    /// Unity's Input System does not re-fire performed for already-held buttons on re-activation,
    /// so we read physical state directly.
    /// </summary>
    public void SyncHeldButtons()
    {
        var pi = _core.PlayerInput;
        if (pi == null || pi.actions == null) return;

        var runAction  = pi.actions.FindAction("Run",  throwIfNotFound: false);
        var jumpAction = pi.actions.FindAction("Jump", throwIfNotFound: false);

        State.RunPressed = IsRunHeld(runAction);
        State.JumpPressed = IsJumpHeld(jumpAction);

        SyncHeldMovement();
    }

    // Jump ────────────────────────────────────────────────────────────────────

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && !State.InputLocked) OnJumpPressed();
        if (context.canceled && !State.InputLocked && !MobileHeldInputState.Jump)
            OnJumpReleased();
    }

    public void OnJumpPressed()
    {
        _pollingJump     = false;
        _jumpActionCache = null;
        _jumpPressedFrame = Time.frameCount;
        State.JumpTimer = Time.time + _core.Physics.Config.JumpDelay;
        State.JumpPressed = true;
        State.SpinJumpQueued = false;

        // Prevent a carried crouch/crawl Down press from being reused for ground pound
        State.DownPressed = false;

        Debug.Log($"[Jump] JumpPressed fired. OnGround={State.OnGround} RunPressed={State.RunPressed} JumpTimer={State.JumpTimer} Time={Time.time}");
    }

    public void OnJumpReleased()
    {
        State.JumpPressed = false;
        _pollingJump      = false;
        _jumpActionCache  = null;
    }

    // Spin ────────────────────────────────────────────────────────────────────

    public void Spin(InputAction.CallbackContext context)
    {
        if (State.InputLocked || State.IsCapeActive) return;

        if (context.performed) OnSpinPressed();
        if (context.canceled && !IsMobileSpinHeld()) OnSpinReleased();
    }

    public void OnSpinPressed()
    {
        State.SpinHeld = true; // Always track physical hold regardless of Spinning state

        bool airborne = !State.OnGround && !State.Swimming
                     && !State.GroundPounding && !State.WallSliding && !State.Climbing;

        if (airborne)
        {
            // Midair spin eligibility — actual transition handled in RiseState/FallState
            // Just set the flag here; states read it in CheckTransitions
            if (!State.Spinning && !State.SpinJumpQueued
                && State.CanMidairSpin && !State.IsMidairSpinning)
            {
                State.SpinPressed = true;
            }
            // Never queue a ground spin jump while airborne
            return;
        }

        // Ground / wall / climb → queue spin jump
        if (!State.CanSpinJump) return;

        State.JumpTimer      = Time.time + _core.Physics.Config.JumpDelay;
        State.SpinPressed    = true;
        State.SpinJumpQueued = true;
    }

    public void OnSpinReleased() { State.SpinPressed = false; State.SpinHeld = false; }

    // Shoot ───────────────────────────────────────────────────────────────────

    public void Shoot(InputAction.CallbackContext context)
    {
        if (State.InputLocked) return;

        if (context.performed) OnShootPressed();
        if (context.canceled && !IsMobileShootHeld()) OnShootReleased();
    }

    public void OnShootPressed()
    {
        State.ShootPressed = true;
        _core.NotifyAbilities(a => a.onShootPressed());
    }

    public void OnShootReleased() => State.ShootPressed = false;

    // Extra Action ────────────────────────────────────────────────────────────

    public void ExtraAction(InputAction.CallbackContext context)
    {
        if (State.InputLocked) return;
        if (context.performed) OnExtraActionPressed();
    }

    public void OnExtraActionPressed()
    {
        _core.NotifyAbilities(a => a.onExtraActionPressed());
    }

    // Use (levers, interactables) ─────────────────────────────────────────────

    public void Use(InputAction.CallbackContext context)
    {
        if (context.performed) OnUsePressed();
    }

    public void OnUsePressed()
    {
        _core.Carry.TryUseObject();
    }

    // Crouch ──────────────────────────────────────────────────────────────────
    // Dedicated crouch button — supports keyboard, gamepad, and supplements
    // mobile (which uses OnMobileDownPressed). Sets MoveInput.y = -1 while
    // held, exactly the same as pressing down on the move axis.

    public void Crouch(InputAction.CallbackContext context)
    {
        if (State.InputLocked) return;

        if (context.performed)
            State.MoveInput = new Vector2(State.MoveInput.x, -1f);
        if (context.canceled)
            State.MoveInput = new Vector2(State.MoveInput.x,  0f);
    }

    // ─── Mobile Fallbacks ────────────────────────────────────────────────────
    // Called by on-screen button UI elements directly.

    private void SetMobileMoveInput(Vector2 value)
    {
        MobileHeldInputState.Move = value;

        if (value != Vector2.zero)
        {
            _pollingMove     = false;
            _moveActionCache = null;
        }

        var moveAction = _core.PlayerInput?.actions?.FindAction("Move", throwIfNotFound: false);
        State.MoveInput = ReadHeldMovement(moveAction);
        if (!State.InputLocked && !State.IsFrozen && !State.IsPaused)
            State.Direction = State.MoveInput;
    }

    public void OnMobileLeftPressed()  => SetMobileMoveInput(new Vector2(-1f, MobileHeldInputState.Move.y));
    public void OnMobileLeftReleased() => SetMobileMoveInput(new Vector2( 0f, MobileHeldInputState.Move.y));
    public void OnMobileRightPressed() => SetMobileMoveInput(new Vector2( 1f, MobileHeldInputState.Move.y));
    public void OnMobileRightReleased()=> SetMobileMoveInput(new Vector2( 0f, MobileHeldInputState.Move.y));
    public void OnMobileUpPressed()    => SetMobileMoveInput(new Vector2(MobileHeldInputState.Move.x,  1f));
    public void OnMobileUpReleased()   => SetMobileMoveInput(new Vector2(MobileHeldInputState.Move.x,  0f));
    public void OnMobileDownPressed()  => SetMobileMoveInput(new Vector2(MobileHeldInputState.Move.x, -1f));
    public void OnMobileDownReleased() => SetMobileMoveInput(new Vector2(MobileHeldInputState.Move.x,  0f));

    private Vector2 ReadHeldMovement(InputAction moveAction)
    {
        Vector2 hardware = moveAction != null
            ? ApplyDeadzone(moveAction.ReadValue<Vector2>())
            : Vector2.zero;
        return CombineWithMobileMovement(hardware);
    }

    private static Vector2 CombineWithMobileMovement(Vector2 hardware)
    {
        if (!GlobalVariables.OnScreenControls)
            return hardware;

        Vector2 mobile = MobileHeldInputState.Move;
        return new Vector2(
            mobile.x != 0f ? mobile.x : hardware.x,
            mobile.y != 0f ? mobile.y : hardware.y);
    }

    // ─── Deadzone Processing ─────────────────────────────────────────────────

    private Vector2 ApplyDeadzone(Vector2 raw)
    {
        return new Vector2(
            ApplyAxis(raw.x),
            ApplyAxis(raw.y)
        );
    }

    private float ApplyAxis(float value)
    {
        float abs = Mathf.Abs(value);
        if (abs < LowerDeadzone) return 0f;
        if (abs > UpperDeadzone) return Mathf.Sign(value);
        return value;
    }
}
