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

    private void Start()
    {
        StartCoroutine(SpawnBubbles());
    }

    // ─── Water Entry / Exit ──────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsWater(other)) return;

        State.Swimming                  = true;
        State.Spinning                  = false;
        State.WaterGroundPoundStartTime = 0f;

        // FSM will pick this up in CheckTransitions next FixedUpdate
        // No need to force-transition here — state reads State.Swimming

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
