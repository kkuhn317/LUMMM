using UnityEngine;
using System.Collections;
using PowerupState = PowerStates.PowerupState;

/// <summary>
/// Owns all damage, invincibility, star power, death, and enemy-transformation logic.
///
/// Responsibilities:
/// - DamageMario / forced death guard
/// - Power-down routing to MarioPowerup
/// - Star power start / stop / rainbow color cycling
/// - Invincibility timer + flashing coroutine
/// - TransformIntoObject (enemy curse, pig transform, etc.)
/// - Checkpoint flag UI
///
/// Writes to: State.IsDead, State.InvincibilityTimeRemaining,
///            State.StarPower, State.StarPowerRemainingTime, State.DamagedThisFrame
/// </summary>
[RequireComponent(typeof(MarioCore))]
public class MarioCombat : MonoBehaviour
{
    [Header("Death")]
    public GameObject DeadMarioPrefab;

    [Header("Star Power")]
    private static readonly Color[] StarColors =
        { Color.green, Color.yellow, Color.blue, Color.red };
    private int _starColorIndex;

    [Header("Checkpoint")]
    [SerializeField] private GameObject checkpointFlag;
    [SerializeField] private float checkpointFlagDuration = 2f;
    private Coroutine _checkpointRoutine;

    [Header("Visual")]
    [SerializeField] private Transform visualRoot;

    private MarioCore _core;
    private MarioState State => _core.State;
    private int PlayerIndex => _core.PlayerIndex;
    private bool _flashing;

    private void Awake()
    {
        _core = GetComponent<MarioCore>();
        if (visualRoot == null) visualRoot = transform;
    }

    private void Start()
    {
        if (checkpointFlag != null)
            checkpointFlag.SetActive(false);
    }

    // ─── Update: Timers ──────────────────────────────────────────────────────

    private void Update()
    {
        TickInvincibility();
        TickStarPower();
    }

    private void TickInvincibility()
    {
        if (State.InvincibilityTimeRemaining <= 0f) return;

        State.InvincibilityTimeRemaining -= Time.deltaTime;

        if (!_flashing)
            StartCoroutine(FlashDuringInvincibility());

        if (State.InvincibilityTimeRemaining <= 0f)
            State.InvincibilityTimeRemaining = 0f;
    }

    private void TickStarPower()
    {
        if (!State.StarPower || State.StarPowerRemainingTime <= 0f) return;

        State.StarPowerRemainingTime -= Time.deltaTime;
        if (State.StarPowerRemainingTime <= 0f)
            StopStarPower();
    }

    // ─── Damage ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Main damage entry point. Called by hazards, enemies, and ground detection.
    /// force=true bypasses per-frame guard and invincibility.
    /// </summary>
    public void DamageMario(bool force = false)
    {
        if (GlobalVariables.cheatInvincibility) return;
        if (State.IsDead)                       return;
        if (State.IsTransforming)               return;
        if (State.IsInvincible && !force)        return;
        if (State.DamagedThisFrame && !force)    return;
        if (State.StarPower && !force)           return;

        State.DamagedThisFrame = true;

        if (PowerStates.IsSmall(State.PowerupState))
            ToDead();
        else
            _core.Powerup.PowerDown();
    }

    // ─── Death ───────────────────────────────────────────────────────────────

    public void ToDead(DeathCause cause = null, bool ignoreInvincibility = false)
    {
        if (GlobalVariables.cheatInvincibility) return;
        if (State.IsDead)                       return;
        if (!ignoreInvincibility && State.IsInvincible) return;
        if (!ignoreInvincibility && State.StarPower)    return;

        if (State.Carrying)
            _core.Carry.DropCarry();

        State.IsDead = true;
        _core.StateMachine.ForceTransition(MarioStateID.Dead);
        StartCoroutine(ExecuteDeath(cause));
    }

    private IEnumerator ExecuteDeath(DeathCause cause = null)
    {
        yield return null; // One frame for FSM + audio to react

        // Try to get character-specific death prefab first
        var character = _core.Powerup?.Character;
        GameObject deadPrefab = null;

        if (character != null && cause != null)
        {
            // Find current PowerUpData by matching state and type
            var currentPowerup = FindCurrentPowerUpData();
            deadPrefab = character.GetDeadPrefab(cause, currentPowerup);
        }

        // Fallback to the legacy DeadMarioPrefab field
        if (deadPrefab == null)
            deadPrefab = DeadMarioPrefab;

        if (deadPrefab != null)
            Instantiate(deadPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    /// <summary>
    /// Finds the PowerUpData asset that matches Mario's current powerup state and type.
    /// Used for death prefab lookup.
    /// </summary>
    private PowerUpData FindCurrentPowerUpData()
    {
        var character = _core.Powerup?.Character;
        if (character?.PowerupPrefabs == null) return null;

        // Match against Identity directly — most efficient since it's a reference
        var identity = _core.Powerup?.Identity;
        if (identity != null)
        {
            foreach (var entry in character.PowerupPrefabs)
                if (entry.Data == identity)
                    return entry.Data;
        }

        // Fallback: match by state + type string
        foreach (var entry in character.PowerupPrefabs)
        {
            if (entry.Data != null &&
                entry.Data.PowerupState == State.PowerupState &&
                string.Equals(entry.Data.PowerupType, State.CurrentPowerupType,
                    System.StringComparison.OrdinalIgnoreCase))
                return entry.Data;
        }

        return null;
    }

    /// <summary>
    /// Transforms Mario into another object (e.g. pig curse, enemy magic).
    /// Respects invincibility.
    /// </summary>
    public void TransformIntoObject(GameObject prefab)
    {
        if (State.IsInvincible || State.StarPower || GlobalVariables.cheatInvincibility)
        {
            Debug.Log("[MarioCombat] Transform ignored — Mario is invincible.");
            return;
        }

        if (State.IsDead) return;

        if (State.Carrying)
            _core.Carry.DropCarry();

        State.IsDead = true;
        StartCoroutine(ExecuteTransformation(prefab));
    }

    private IEnumerator ExecuteTransformation(GameObject prefab)
    {
        yield return null;

        GameObject spawned = Instantiate(prefab, transform.position, Quaternion.identity);

        if (spawned.TryGetComponent(out SpriteRenderer sr))
            sr.flipX = !State.FacingRight;

        // DeadMario velocity flip: set facing on the prefab or handle in DeadMario.Start()

        Destroy(gameObject);
    }

    // ─── Invincibility Flash ─────────────────────────────────────────────────

    private IEnumerator FlashDuringInvincibility()
    {
        _flashing = true;
        float flashSpeed = 1f / 20f;

        while (State.InvincibilityTimeRemaining > 0f)
        {
            // Refresh every cycle, considering the  limb renderers may activate/deactivate
            var renderers = GetVisualRenderers();
            SetAlpha(renderers, 0.5f);
            yield return new WaitForSeconds(flashSpeed);
            renderers = GetVisualRenderers();
            SetAlpha(renderers, 1f);
            yield return new WaitForSeconds(flashSpeed);
        }

        SetAlpha(GetVisualRenderers(), 1f);
        _flashing = false;
    }

    // ─── Star Power ──────────────────────────────────────────────────────────

    public void StartStarPower(float duration)
    {
        if (_core == null || _core.State == null) return;
        CancelInvoke(nameof(CycleStarColor));

        State.StarPower              = true;
        State.StarPowerRemainingTime = duration;

        InvokeRepeating(nameof(CycleStarColor), 0f, 0.1f);

        MarioEvents.FireStarPowerStarted(PlayerIndex, duration);
    }

    public void StopStarPower()
    {
        if (_core == null || _core.State == null) return;
        CancelInvoke(nameof(CycleStarColor));

        State.StarPower              = false;
        State.StarPowerRemainingTime = 0f;

        // Reset all Visual renderers to white, preserve alpha
        foreach (var r in GetVisualRenderers())
        {
            var c  = r.color;
            r.color = new Color(1f, 1f, 1f, c.a);
        }

        ComboManager.Instance?.ResetAll();
        MarioEvents.FireStarPowerEnded(PlayerIndex);
    }

    private void CycleStarColor()
    {
        Color color     = StarColors[_starColorIndex];
        _starColorIndex = (_starColorIndex + 1) % StarColors.Length;

        foreach (var r in GetVisualRenderers())
            r.color = color;
    }

    // ─── Checkpoint ──────────────────────────────────────────────────────────

    public void ShowCheckpointFlag()
    {
        if (checkpointFlag == null) return;

        if (_checkpointRoutine != null)
        {
            checkpointFlag.SetActive(false);
            StopCoroutine(_checkpointRoutine);
        }

        _checkpointRoutine = StartCoroutine(CheckpointFlagRoutine());
        MarioEvents.FireCheckpointReached(PlayerIndex);
    }

    private IEnumerator CheckpointFlagRoutine()
    {
        checkpointFlag.SetActive(true);
        yield return new WaitForSeconds(checkpointFlagDuration);
        checkpointFlag.SetActive(false);
        _checkpointRoutine = null;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private SpriteRenderer[] GetVisualRenderers()
        => visualRoot.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

    private static void SetAlpha(SpriteRenderer[] renderers, float alpha)
    {
        foreach (var r in renderers)
        {
            var c  = r.color;
            r.color = new Color(c.r, c.g, c.b, alpha);
        }
    }

    // ─── Trigger / Collision Damage ──────────────────────────────────────────

    /// <summary>
    /// Handles trigger-based hazards (invisible damage zones, lava, etc.).
    /// "Damaging" tag = damage, "Deadly" tag = instant kill.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleDamagingTag(other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        HandleDamagingTag(other.gameObject);
    }

    /// <summary>
    /// Handles solid hazards (spikes, thorns) Mario physically collides with.
    /// Uses Stay so continuous contact (standing on spikes) keeps dealing damage.
    /// </summary>
    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleDamagingTag(collision.gameObject);
    }

    private void HandleDamagingTag(GameObject other)
    {
        if (other.CompareTag("Damaging"))
            DamageMario();
        else if (other.CompareTag("Deadly"))
            ToDead(null, ignoreInvincibility: true); // pits/lava always kill
    }
}