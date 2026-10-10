using UnityEngine;
using System.Collections;

/// <summary>
/// Owns all water interaction logic that lives outside the FSM:
/// - OnTriggerEnter/Exit for water volumes
/// - Bubble spawning coroutine
/// - JumpOutOfWater impulse
///
/// Why not in the FSM states?
/// Unity trigger callbacks (OnTriggerEnter2D etc.) must live on a MonoBehaviour.
/// States are plain C# classes, so water entry/exit detection belongs here.
/// This module sets State.Swimming and requests FSM transitions accordingly.
/// </summary>
[RequireComponent(typeof(MarioCore))]
public class MarioSwimming : MonoBehaviour
{
    private const float WaterExitGraceTime = 0.25f;

    [Header("References")]
    public GameObject BubblePrefab;
    public GameObject SplashWaterPrefab;

    private MarioCore _core;
    private MarioState State => _core.State;
    private int PlayerIndex  => _core.PlayerIndex;
    private bool _waterExitHandled;

    private static readonly int _waterLayer = -1; // Cached lazily


    private void Awake()
    {
        _core = GetComponent<MarioCore>();
    }

    private void FixedUpdate()
    {
        // Authoritatively sync State.Swimming to whether Mario is physically
        // inside a water collider right now. This prevents the swim-outside-water
        // bug where spamming jump at the surface causes OnTriggerEnter2D to re-fire
        // while Mario is already in the air.
        bool inWater = IsInWater();
        if (!inWater && State.Swimming)
            HandleWaterExit();
    }

    private bool IsInWater()
    {
        var col = _core.Collider;
        if (col == null) return false;
        var hits = Physics2D.OverlapBoxAll(col.bounds.center, col.bounds.size * 0.9f, 0f);
        foreach (var hit in hits)
            if (IsWater(hit)) return true;
        return false;
    }

    private void Start()
    {
        StartCoroutine(SpawnBubbles());
    }

    // ─── Water Entry / Exit ──────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsWater(other)) return;

        bool wasSwimming = State.Swimming;

        if (!wasSwimming)
        {
            _waterExitHandled = false;
            State.LastSwimStrokeTime = -999f;
            State.WaterExitJumpRequested = false;
            State.WaterExitCoyoteTimer = 0f;
        }

        State.Swimming                  = true;
        State.Spinning                  = false;
        State.WaterGroundPoundStartTime = 0f;

        if (!wasSwimming)
            MarioEvents.FireEnteredWater(PlayerIndex);

        // If ground pounding, cancel it (FSM handles this in SwimmingStateBase.Enter)
        // but set the flag now so the transition takes it into account
        if (State.GroundPounding)
        {
            State.GroundPoundInWater = false;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsWater(other)) return;

        // Ground pound in water timeout tracking
        if (State.GroundPounding && !State.GroundPoundInWater)
        {
            State.GroundPoundInWater        = true;
            State.WaterGroundPoundStartTime = Time.time;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsWater(other)) return;

        // Double-check we're actually fully out of the water
        // (original code worked around a crush detector issue)
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(transform.position, 0.1f);
        foreach (var col in overlaps)
        {
            if (IsWater(col)) return; // Still in water, don't exit
        }

        HandleWaterExit();
    }

    private void HandleWaterExit()
    {
        if (_waterExitHandled) return;
        _waterExitHandled = true;

        bool recentlyStroked = Time.time - State.LastSwimStrokeTime <= WaterExitGraceTime;
        bool shouldJump = recentlyStroked && State.JumpPressed && !State.GroundPounding;

        State.Swimming                  = false;
        State.GroundPoundInWater        = false;
        State.WaterGroundPoundStartTime = 0f;
        State.LastSwimStrokeTime        = -999f;
        State.WaterExitJumpRequested    = shouldJump;
        State.WaterExitCoyoteTimer      = !shouldJump && !State.GroundPounding
            ? Time.time + _core.Physics.Config.CoyoteTime
            : 0f;
        // AirTimer also powers ground coyote jumps. Do not carry a swim's
        // leftover timer into Fall and accidentally grant a normal jump.
        State.AirTimer = 0f;

        if (shouldJump)
        {
            _core.StateMachine.RequestTransition(MarioStateID.Rise);
        }
        else if (State.GroundPounding)
        {
            State.GroundPounding = false;
            MarioEvents.FireGroundPoundCancelled(PlayerIndex);
            _core.StateMachine.RequestTransition(MarioStateID.Fall);
        }
        else
        {
            _core.StateMachine.RequestTransition(MarioStateID.Fall);
        }

        MarioEvents.FireExitedWater(PlayerIndex);
    }

    // ─── Jump Out of Water ───────────────────────────────────────────────────

    /// <summary>
    /// Applies the upward impulse for an immediate or coyote-time water exit jump.
    /// </summary>
    public void JumpOutOfWater()
    {
        // Keep ground constraints from consuming the launch near a water edge.
        _core.GroundDetection.SkipConstraintsThisFrame = true;
        State.WaterExitCoyoteTimer = 0f;
        _core.Rb.drag = 0f;
        _core.Rb.gravityScale = _core.Physics.Config.RiseGravity;

        _core.Rb.velocity = new Vector2(_core.Rb.velocity.x, 0f);
        _core.Rb.AddForce(0.75f * _core.Physics.Config.JumpSpeed * Vector2.up, ForceMode2D.Impulse);

        State.JumpTimer = 0f;
        State.AirTimer  = Time.time + _core.Physics.Config.Airtime;

        if (SplashWaterPrefab != null)
            Instantiate(SplashWaterPrefab, transform.position, Quaternion.identity);

        MarioEvents.FireJumpedOutOfWater(PlayerIndex);
    }

    // ─── Bubble Spawning ─────────────────────────────────────────────────────

    private IEnumerator SpawnBubbles()
    {
        while (true)
        {
            yield return new WaitForSeconds(_core.Physics.Config.BubbleSpawnDelay);

            if (State.Swimming && BubblePrefab != null)
                Instantiate(BubblePrefab, transform.position, Quaternion.identity);
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static bool IsWater(Collider2D col)
    {
        return col.gameObject.layer == LayerMask.NameToLayer("Water");
    }
}
