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
    private int _jumpPressedFrame = -1;

    // ─── Lifecycle ───────────────────────────────────────────────────────────

    private void Awake()
    {
        _core = GetComponent<MarioCore>();
    }

    private void Update()
    {
        if (State.InputLocked || State.IsFrozen || State.IsPaused) return;

        // Propagate processed move input to direction every frame
        State.Direction = State.MoveInput;

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
        Vector2 raw = context.ReadValue<Vector2>();
        State.MoveInput = ApplyDeadzone(raw);
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
        if (context.canceled && !State.InputLocked) OnRunReleased();
    }

    public void OnRunPressed()
    {
        State.RunPressed = true;

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
    } 

    /// <summary>
    /// Syncs held-button state after inputs are re-enabled (e.g. after a door animation).
    /// Unity's Input System does not re-fire performed for already-held buttons on re-activation,
    /// so we read physical state directly.
    /// </summary>
    public void SyncHeldButtons()
    {
        var pi = _core.PlayerInput;
        if (pi == null || pi.actions == null) return;

        var runAction  = pi.actions.FindAction("Run",  throwIfNotFound: false);
        var jumpAction = pi.actions.FindAction("Jump", throwIfNotFound: false);
        var moveAction = pi.actions.FindAction("Move", throwIfNotFound: false);

        if (runAction  != null) State.RunPressed  = runAction.IsPressed();
        if (jumpAction != null) State.JumpPressed = jumpAction.IsPressed();
        if (moveAction != null)
        {
            State.MoveInput = ApplyDeadzone(moveAction.ReadValue<Vector2>());
            if (!State.InputLocked)
                State.Direction = State.MoveInput;
        }
    }

    // Jump ────────────────────────────────────────────────────────────────────

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && !State.InputLocked) OnJumpPressed();
        if (context.canceled && !State.InputLocked) OnJumpReleased();
    }

    public void OnJumpPressed()
    {
        _jumpPressedFrame = Time.frameCount;
        State.JumpTimer    = Time.time + _core.Physics.Config.JumpDelay;
        State.JumpPressed  = true;
        State.SpinJumpQueued = false;
        Debug.Log($"[Jump] JumpPressed fired. OnGround={State.OnGround} RunPressed={State.RunPressed} JumpTimer={State.JumpTimer} Time={Time.time}");
    }

    public void OnJumpReleased() => State.JumpPressed = false;

    // Spin ────────────────────────────────────────────────────────────────────

    public void Spin(InputAction.CallbackContext context)
    {
        if (State.InputLocked || State.IsCapeActive) return;

        if (context.performed) OnSpinPressed();
        if (context.canceled)  OnSpinReleased();
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
        if (context.canceled)  OnShootReleased();
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

    public void OnMobileLeftPressed()  => State.MoveInput = new Vector2(-1f, State.MoveInput.y);
    public void OnMobileLeftReleased() => State.MoveInput = new Vector2( 0f, State.MoveInput.y);
    public void OnMobileRightPressed() => State.MoveInput = new Vector2( 1f, State.MoveInput.y);
    public void OnMobileRightReleased()=> State.MoveInput = new Vector2( 0f, State.MoveInput.y);
    public void OnMobileUpPressed()    => State.MoveInput = new Vector2(State.MoveInput.x,  1f);
    public void OnMobileUpReleased()   => State.MoveInput = new Vector2(State.MoveInput.x,  0f);
    public void OnMobileDownPressed()  => State.MoveInput = new Vector2(State.MoveInput.x, -1f);
    public void OnMobileDownReleased() => State.MoveInput = new Vector2(State.MoveInput.x,  0f);

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