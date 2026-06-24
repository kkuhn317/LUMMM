using UnityEngine;
using PowerupState = PowerStates.PowerupState;

/// <summary>
/// Pure data object. The single source of truth for all Mario state.
/// No logic lives here — only data that modules read and write.
/// Only modules that own a piece of state should write to it.
/// Everyone else should read only.
/// </summary>
public class MarioState
{
    // ─── Identity ────────────────────────────────────────────────────────────

    /// <summary>Player index for multiplayer (0 = P1, 1 = P2, etc.)</summary>
    public int PlayerIndex;

    // ─── Transform ───────────────────────────────────────────────────────────

    public bool FacingRight = true;

    // ─── Movement ────────────────────────────────────────────────────────────

    public Vector2 Velocity;
    public Vector2 MoveInput;
    public Vector2 Direction; // Processed MoveInput after deadzone

    public bool IsRunning;
    public bool IsMoving;

    // ─── Ground ──────────────────────────────────────────────────────────────

    public bool OnGround;
    public bool WasGrounded;
    public Vector2 GroundPosition;
    public float   FloorAngle;  // -45 = \, 0 = _, 45 = /
    public Vector2 FloorNormal; // raw hit.normal from last ground contact

    /// <summary>Non-null when Mario is standing on a moving platform.</summary>
    public Transform OnMovingPlatform;
    public bool DoMovingPlatformMomentum = true;
    public bool DoCornerCorrection       = true;

    // ─── Conveyor ────────────────────────────────────────────────────────────

    /// <summary>Non-null when Mario is standing on a conveyor belt.</summary>
    public ConveyorBelt OnConveyor;

    // ─── Crouch ──────────────────────────────────────────────────────────────

    public bool IsCrouching;
    public bool IsCrawling; // Small Mario only

    // ─── Airborne ────────────────────────────────────────────────────────────

    public bool IsRising;
    public float AirTimer;

    // ─── Wall ────────────────────────────────────────────────────────────────

    public bool WallSliding;
    public bool IsAgainstWall;

    // ─── Jump ────────────────────────────────────────────────────────────────

    public bool JumpPressed;
    public bool RunPressed;
    public bool SpinPressed;
    public bool SpinHeld;      // True while spin button is physically held, regardless of Spinning state
    public bool DownPressed;
    public bool ShootPressed;
    public float JumpTimer; // Buffered jump timer

    // ─── Spin / Midair ───────────────────────────────────────────────────────

    public bool Spinning;
    public bool SpinJumpQueued;
    public bool IsMidairSpinning;
    public bool MidairSpinUsedThisJump;
    public float LastMidairSpinTime = -999f;
    public float MidairSpinStartTime;
    public float MidairSpinEndTime;

    // ─── Ground Pound ────────────────────────────────────────────────────────

    public bool GroundPounding;
    public bool GroundPoundRotating;
    public bool GroundPoundLanded;
    public bool GroundPoundInWater;
    public float WaterGroundPoundStartTime;
    public bool RequireDownReleaseForGroundPound;

    // ─── Swimming ────────────────────────────────────────────────────────────

    public bool Swimming;
    public bool InWaterfall = false;

    // ─── Climbing ────────────────────────────────────────────────────────────

    public bool Climbing;
    public bool  JustLeftClimbing;
    public float JustLeftClimbingTimer; // counts down; JustLeftClimbing stays true until it hits 0
    public bool IsUsingObject;  // true while interacting with a UseableObject — suppresses look-up
    public bool ClimbExitedWhilePressingDown;
    public bool ClimbExitedWhilePressingUp;   // prevents re-grab while still holding up after jumping off
    public bool JumpedWhileCrouching;
    public Climbable CurrentClimbable;
    public bool SideClimbCanMoveToSide;

    // ─── Carrying ────────────────────────────────────────────────────────────

    public bool Carrying;
    public bool Pushing;
    public ObjectPhysics PushingObject;
    public float PushingSpeed;

    // ─── Look ────────────────────────────────────────────────────────────────

    public bool IsLookingUp;

    // ─── Combat / Damage ─────────────────────────────────────────────────────

    public bool IsDead;
    public bool IsBounced; // true when launched by trampoline/noteblock — suppresses jump sound
    public bool IsInvincible => InvincibilityTimeRemaining > 0f;
    public float InvincibilityTimeRemaining;
    public bool DamagedThisFrame;

    // ─── Star Power ──────────────────────────────────────────────────────────

    public bool StarPower;
    public float StarPowerRemainingTime;

    // ─── Powerup ─────────────────────────────────────────────────────────────

    /// <summary>Seeded by MarioPowerup.Awake() from its serialized identity fields.</summary>
    public PowerupState PowerupState;
    public string CurrentPowerupType;
    public bool IsTransforming;

    // ─── Cape ────────────────────────────────────────────────────────────────

    public bool IsCapeActive;

    // ─── Abilities ───────────────────────────────────────────────────────────

    public bool CanCrouch = true;
    public bool CanCrawl = true;
    public bool CanWallJump = true;
    public bool CanWallJumpWhenHoldingObject;
    public bool CanSpinJump = true;
    public bool CanGroundPound;
    public bool CanMidairSpin = true;
    public bool AllowMultipleMidairSpins;

    // ─── Misc ────────────────────────────────────────────────────────────────

    public bool InputLocked;
    public bool IsPaused;
    public bool IsFrozen;

    /// <summary>
    /// Resets all per-frame transient flags. Called at the start of each FixedUpdate
    /// by MarioCore so no module needs to remember to clean up.
    /// </summary>
    public void ResetFrameFlags()
    {
        DamagedThisFrame = false;
        WasGrounded      = OnGround;

        // Tick down the climbing guard — keep JustLeftClimbing true for the full duration
        if (JustLeftClimbingTimer > 0f)
        {
            JustLeftClimbingTimer -= Time.fixedDeltaTime;
            JustLeftClimbing = JustLeftClimbingTimer > 0f;
        }
        else
        {
            JustLeftClimbing = false;
        }
    }
}