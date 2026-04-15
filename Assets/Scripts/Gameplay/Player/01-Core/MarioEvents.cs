using System;
using UnityEngine;

/// <summary>
/// Central event bus for all Mario-related one-shot moments.
///
/// RULES:
/// - Events always carry playerIndex so multiplayer works correctly.
/// - Physics/movement modules FIRE events.
/// - MarioAnimator and MarioAudio only LISTEN — they never poll State.
/// - No MonoBehaviour dependencies here. This is a pure static class.
///
/// USAGE:
///   Fire:      MarioEvents.OnJumped?.Invoke(playerIndex);
///   Subscribe: MarioEvents.OnJumped += HandleJumped;
///   Unsub:     MarioEvents.OnJumped -= HandleJumped;
/// </summary>
public static class MarioEvents
{
    // ─── Movement ────────────────────────────────────────────────────────────

    /// <summary>Fired the frame Mario's horizontal direction flips.</summary>
    public static event Action<int> OnFlipped;

    /// <summary>Fired when Mario starts skidding (changing direction at high speed).</summary>
    public static event Action<int> OnSkidStarted;

    /// <summary>Fired when Mario stops skidding.</summary>
    public static event Action<int> OnSkidEnded;

    // ─── Jump ────────────────────────────────────────────────────────────────

    /// <summary>Fired the frame a regular jump begins.</summary>
    public static event Action<int> OnJumped;

    /// <summary>Fired the frame a wall jump begins.</summary>
    public static event Action<int> OnWallJumped;

    /// <summary>Fired the frame a spin jump begins.</summary>
    public static event Action<int> OnSpinJumped;

    /// <summary>Fired when Mario bounces off an enemy via spin jump.</summary>
    public static event Action<int> OnSpinJumpBounced;

    /// <summary>Fired when Mario poofs (kills) an enemy via spin jump. Vector3 is the world spawn position.</summary>
    public static event Action<int, Vector3> OnSpinJumpPoofed;

    // ─── Ground ──────────────────────────────────────────────────────────────

    /// <summary>Fired the frame Mario transitions from airborne to grounded.</summary>
    public static event Action<int> OnLanded;

    /// <summary>Fired the frame Mario walks off a ledge without jumping.</summary>
    public static event Action<int> OnLeftGround;

    // ─── Midair Spin ─────────────────────────────────────────────────────────

    /// <summary>Fired when the midair spin / twirl begins.</summary>
    public static event Action<int> OnMidairSpinStarted;

    /// <summary>Fired when the midair spin / twirl ends.</summary>
    public static event Action<int> OnMidairSpinEnded;

    // ─── Ground Pound ────────────────────────────────────────────────────────

    /// <summary>Fired when the ground pound rotation phase starts.</summary>
    public static event Action<int> OnGroundPoundStarted;

    /// <summary>Fired when the ground pound fall phase starts.</summary>
    public static event Action<int> OnGroundPoundFalling;

    /// <summary>Fired the frame the ground pound hits the ground.</summary>
    public static event Action<int, GameObject> OnGroundPoundLanded;

    /// <summary>Fired when a ground pound is cancelled mid-air.</summary>
    public static event Action<int> OnGroundPoundCancelled;

    // ─── Wall ────────────────────────────────────────────────────────────────

    /// <summary>Fired when Mario starts sliding down a wall.</summary>
    public static event Action<int> OnWallSlideStarted;

    /// <summary>Fired when Mario stops sliding down a wall.</summary>
    public static event Action<int> OnWallSlideEnded;

    // ─── Swimming ────────────────────────────────────────────────────────────

    /// <summary>Fired when Mario enters water.</summary>
    public static event Action<int> OnEnteredWater;

    /// <summary>Fired when Mario exits water.</summary>
    public static event Action<int> OnExitedWater;

    /// <summary>Fired each time Mario strokes while swimming.</summary>
    public static event Action<int> OnSwam;

    /// <summary>Fired when Mario jumps out of the water surface.</summary>
    public static event Action<int> OnJumpedOutOfWater;

    // ─── Climbing ────────────────────────────────────────────────────────────

    /// <summary>Fired when Mario grabs a climbable.</summary>
    public static event Action<int> OnClimbStarted;

    /// <summary>Fired when Mario lets go of a climbable.</summary>
    public static event Action<int> OnClimbEnded;

    // ─── Crouch ──────────────────────────────────────────────────────────────

    /// <summary>Fired when Mario enters crouch.</summary>
    public static event Action<int> OnCrouchStarted;

    /// <summary>Fired when Mario exits crouch.</summary>
    public static event Action<int> OnCrouchEnded;

    /// <summary>Fired when Mario starts crawling.</summary>
    public static event Action<int> OnCrawlStarted;

    /// <summary>Fired when Mario stops crawling.</summary>
    public static event Action<int> OnCrawlEnded;

    // ─── Physics ─────────────────────────────────────────────────────────────

    /// <summary>Fired when the active MarioPhysicsConfig is swapped (e.g. powerup).</summary>
    public static event Action<int> OnPhysicsConfigSwapped;

    // ─── Cape ────────────────────────────────────────────────────────────────

    /// <summary>Fired when the cape attack swing begins (for animator "cape" bool).</summary>
    public static event Action<int> OnCapeAttackStarted;

    /// <summary>Fired when the cape attack ends or is cancelled.</summary>
    public static event Action<int> OnCapeAttackEnded;

    // ─── Fire Power ──────────────────────────────────────────────────────────

    /// <summary>Fired each time Mario shoots a fireball.</summary>
    public static event Action<int> OnFireballShot;

    // ─── Carrying ────────────────────────────────────────────────────────────

    /// <summary>Fired when Mario picks up an object.</summary>
    public static event Action<int, ObjectPhysics> OnPickedUp;

    /// <summary>Fired when Mario drops a carried object.</summary>
    public static event Action<int, ObjectPhysics> OnDropped;

    /// <summary>Fired when Mario throws a carried object.</summary>
    public static event Action<int, ObjectPhysics> OnThrown;

    // ─── Combat / Damage ─────────────────────────────────────────────────────

    /// <summary>Fired when Mario takes damage.</summary>
    public static event Action<int> OnDamaged;

    /// <summary>Fired when Mario hits a ceiling (bonk).</summary>
    public static event Action<int> OnBonked;

    /// <summary>Fired when Mario dies.</summary>
    public static event Action<int> OnDied;

    // ─── Star Power ──────────────────────────────────────────────────────────

    /// <summary>Fired when star power begins.</summary>
    public static event Action<int, float> OnStarPowerStarted; // float = duration

    /// <summary>Fired when star power ends.</summary>
    public static event Action<int> OnStarPowerEnded;

    // ─── Powerup / Transform ─────────────────────────────────────────────────

    /// <summary>Fired when a powerup transformation begins.</summary>
    public static event Action<int> OnPowerUpStarted;

    /// <summary>Fired when Mario gets hit and powers down.</summary>
    public static event Action<int> OnPoweredDown;

    // ─── State Machine ───────────────────────────────────────────────────────

    /// <summary>Fired whenever the FSM transitions to a new state.</summary>
    public static event Action<int, string> OnStateChanged;

    // ─── Misc ────────────────────────────────────────────────────────────────

    /// <summary>Fired when Mario looks up (for camera).</summary>
    public static event Action<int> OnLookUpStarted;

    /// <summary>Fired when Mario stops looking up.</summary>
    public static event Action<int> OnLookUpEnded;

    /// <summary>Fired when Mario reaches a checkpoint.</summary>
    public static event Action<int> OnCheckpointReached;

    /// <summary>Fired when the yeah/celebrate animation should play.</summary>
    public static event Action<int> OnCelebrationStarted;

    /// <summary>Fired when the cape attack animation should play.</summary>
    public static event Action<int> OnCapeAttackSwung;

    // ─── Emotes ──────────────────────────────────────────────────────────────

    /// <summary>Fired when an emote starts. Drives both sprite swap and animator.</summary>
    public static event Action<int, MarioEmote> OnEmoteStarted;

    /// <summary>Fired when an emote ends and Normal should be restored.</summary>
    public static event Action<int> OnEmoteEnded;

    // ─── Internal fire methods (called only by Mario modules) ─────────────────
    // Using explicit fire methods keeps invocation sites clean and
    // makes it easy to add logging/debugging in one place.

    public static void FireFlipped(int p) => OnFlipped?.Invoke(p);
    public static void FireSkidStarted(int p) => OnSkidStarted?.Invoke(p);
    public static void FireSkidEnded(int p) => OnSkidEnded?.Invoke(p);
    public static void FireJumped(int p) => OnJumped?.Invoke(p);
    public static void FireWallJumped(int p) => OnWallJumped?.Invoke(p);
    public static void FireSpinJumped(int p) => OnSpinJumped?.Invoke(p);
    public static void FireSpinJumpBounced(int p) => OnSpinJumpBounced?.Invoke(p);
    public static void FireSpinJumpPoofed(int p, Vector3 pos) => OnSpinJumpPoofed?.Invoke(p, pos);
    public static void FireLanded(int p) => OnLanded?.Invoke(p);
    public static void FireLeftGround(int p) => OnLeftGround?.Invoke(p);
    public static void FireMidairSpinStarted(int p) => OnMidairSpinStarted?.Invoke(p);
    public static void FireMidairSpinEnded(int p) => OnMidairSpinEnded?.Invoke(p);
    public static void FireGroundPoundStarted(int p) => OnGroundPoundStarted?.Invoke(p);
    public static void FireGroundPoundFalling(int p) => OnGroundPoundFalling?.Invoke(p);
    public static void FireGroundPoundLanded(int p, GameObject hit) => OnGroundPoundLanded?.Invoke(p, hit);
    public static void FireGroundPoundCancelled(int p) => OnGroundPoundCancelled?.Invoke(p);
    public static void FireWallSlideStarted(int p) => OnWallSlideStarted?.Invoke(p);
    public static void FireWallSlideEnded(int p) => OnWallSlideEnded?.Invoke(p);
    public static void FireEnteredWater(int p) => OnEnteredWater?.Invoke(p);
    public static void FireExitedWater(int p) => OnExitedWater?.Invoke(p);
    public static void FireSwam(int p) => OnSwam?.Invoke(p);
    public static void FireJumpedOutOfWater(int p) => OnJumpedOutOfWater?.Invoke(p);
    public static void FireClimbStarted(int p) => OnClimbStarted?.Invoke(p);
    public static void FireClimbEnded(int p) => OnClimbEnded?.Invoke(p);
    public static void FireCrouchStarted(int p) => OnCrouchStarted?.Invoke(p);
    public static void FireCrouchEnded(int p) => OnCrouchEnded?.Invoke(p);
    public static void FireCrawlStarted(int p) => OnCrawlStarted?.Invoke(p);
    public static void FireCrawlEnded(int p) => OnCrawlEnded?.Invoke(p);
    public static void FirePhysicsConfigSwapped(int p) => OnPhysicsConfigSwapped?.Invoke(p);
    public static void FireCapeAttackStarted(int p) => OnCapeAttackStarted?.Invoke(p);
    public static void FireCapeAttackEnded(int p) => OnCapeAttackEnded?.Invoke(p);
    public static void FireFireballShot(int p) => OnFireballShot?.Invoke(p);
    public static void FirePickedUp(int p, ObjectPhysics obj) => OnPickedUp?.Invoke(p, obj);
    public static void FireDropped(int p, ObjectPhysics obj) => OnDropped?.Invoke(p, obj);
    public static void FireThrown(int p, ObjectPhysics obj) => OnThrown?.Invoke(p, obj);
    public static void FireDamaged(int p) => OnDamaged?.Invoke(p);
    public static void FireBonked(int p) => OnBonked?.Invoke(p);
    public static void FireDied(int p) => OnDied?.Invoke(p);
    public static void FireStarPowerStarted(int p, float duration) => OnStarPowerStarted?.Invoke(p, duration);
    public static void FireStarPowerEnded(int p) => OnStarPowerEnded?.Invoke(p);
    public static void FirePowerUpStarted(int p) => OnPowerUpStarted?.Invoke(p);
    public static void FirePoweredDown(int p) => OnPoweredDown?.Invoke(p);
    public static void FireStateChanged(int p, string state) => OnStateChanged?.Invoke(p, state);
    public static void FireLookUpStarted(int p) => OnLookUpStarted?.Invoke(p);
    public static void FireLookUpEnded(int p) => OnLookUpEnded?.Invoke(p);
    public static void FireCheckpointReached(int p) => OnCheckpointReached?.Invoke(p);
    public static void FireCelebrationStarted(int p) => OnCelebrationStarted?.Invoke(p);
    public static void FireCapeAttackSwung(int playerIndex) => OnCapeAttackSwung?.Invoke(playerIndex);
    public static void FireEmoteStarted(int p, MarioEmote emote) => OnEmoteStarted?.Invoke(p, emote);
    public static void FireEmoteEnded(int p) => OnEmoteEnded?.Invoke(p);
}