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
    [Header("References")]
    public GameObject BubblePrefab;
    public GameObject SplashWaterPrefab;

    private MarioCore _core;
    private MarioState State => _core.State;
    private int PlayerIndex  => _core.PlayerIndex;

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
        {
            // Physically outside water but state says swimming — correct it
            State.Swimming = false;
            MarioEvents.FireExitedWater(_core.PlayerIndex);
        }
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

        State.Swimming                  = false;
        State.GroundPoundInWater        = false;
        State.WaterGroundPoundStartTime = 0f;

        // Jump out of water if moving upward
        if (_core.Rb.velocity.y > 0f && !State.GroundPounding)
        {
            JumpOutOfWater();
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
    /// Applies the upward impulse when exiting water with upward velocity.
    /// Called by OnTriggerExit2D and SwimmingStateBase.
    /// </summary>
    public void JumpOutOfWater()
    {
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