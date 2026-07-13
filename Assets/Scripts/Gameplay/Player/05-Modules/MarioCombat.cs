using UnityEngine;
using System.Collections;
using PowerupState = PowerStates.PowerupState;

/// <summary>
/// Owns all damage, invincibility, star power, death, and enemy-transformation logic.
///
/// Responsibilities:
/// - DamageMario / forced death guard
/// - Power-down routing to MarioPowerup
/// - Star power start / stop / palette-swap cycling
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
    // Star-flash rows are a CONTIGUOUS BLOCK at the end of the TargetPalette, after the
    // transformation/skin rows (normal, fire, ice, nes, bad, ...). MarioPalette owns
    // _PaletteRow; MarioCombat only tells it which star rows to cycle.
    //
    // Unrelated to the shader's _PaletteRows property (the full texture height set on the
    // material): that counts ALL rows in the PNG; these two count only the star block.
    [Tooltip("Index of the first star row (rows before it are transformation/skin palettes).")]
    [SerializeField] private int starRowStart = 5;
    [Tooltip("How many star rows to rotate through.")]
    [SerializeField] private int starRowCount = 3;
    private int _starFrame;

    // Exposed so the transform shell can keep the star flash going during the morph.
    public int StarRowStart => starRowStart;
    public int StarRowCount => starRowCount;

    private const string StarMusicKey = "Star";
    [SerializeField] private GameObject starMusicOverride;
    [SerializeField] private int starMusicPriority = 100;
    private GameObject _starMusicInstance;   // live instance of the star-music prefab

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
        if (State.IsDead) return;
        if (!ignoreInvincibility && State.IsInvincible) return;
        if (!ignoreInvincibility && State.StarPower) return;

        if (State.Carrying)
            _core.Carry.DropCarry();

        if (State.StarPower)
            StopStarPower();

        MusicManager.Instance?.ClearMusicOverrides(MusicManager.MusicStartMode.Continue);

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
        print("transform started");
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

        print("transform executing!!!!");

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

    private void OnDestroy()
    {
        // Don't let the star-music instance outlive this player (transform swap, level end...).
        if (_starMusicInstance != null)
            Destroy(_starMusicInstance);
    }

    public void StartStarPower(float duration)
    {
        if (_core == null || _core.State == null) return;
        CancelInvoke(nameof(CycleStarColor));

        State.StarPower = true;
        State.StarPowerRemainingTime = duration;

        _starFrame = 0;
        InvokeRepeating(nameof(CycleStarColor), 0f, 0.05f);

        if (MusicManager.Instance != null && starMusicOverride != null)
        {
            // MusicManager mutes/unmutes/restarts the object directly, so it needs a LIVE
            // instance — starMusicOverride is a prefab asset and never plays on its own.
            // (This is why removing PowerUp's Instantiate silenced the star: nothing was
            // instantiating the music anymore.) Instantiate once and reuse it.
            if (_starMusicInstance == null)
            {
                _starMusicInstance = Instantiate(starMusicOverride);

                // The star prefab's MusicOverride auto-registers itself with MusicManager on
                // Start (PushMusicOverride) under a legacy key that ReleaseOverride("Star")
                // never clears — so after the star ends the overworld never restores. This
                // path is MusicManager-managed via RequestOverride below, so disable that
                // component (its own comment: don't auto-register player-owned music).
                var legacy = _starMusicInstance.GetComponent<MusicOverride>();
                if (legacy != null) legacy.enabled = false;
            }

            var mode = MusicManager.Instance.HasActiveOverride(StarMusicKey)
                ? MusicManager.MusicStartMode.Continue
                : MusicManager.MusicStartMode.Restart;

            MusicManager.Instance.RequestOverride(
                StarMusicKey,
                _starMusicInstance,
                PlayerIndex,
                starMusicPriority,
                mode
            );
        }

        MarioEvents.FireStarPowerStarted(PlayerIndex, duration);
    }

    public void StopStarPower()
    {
        if (_core == null || _core.State == null) return;
        CancelInvoke(nameof(CycleStarColor));

        State.StarPower = false;
        State.StarPowerRemainingTime = 0f;
        _starFrame = 0;

        // Hand the palette back to MarioPalette — it returns to the current
        // transformation (fire/ice/skin), NOT to normal.
        _core.Palette?.ClearStar();

        // Release the star music override — but NOT while the level is ending. During the
        // flagpole / course-clear sequence the fanfare is already playing (FlagLevelFlow muted
        // the loop and started the ending track). Releasing here runs RefreshActiveMusic, which
        // would restore the overworld loop over the fanfare — the SMB "overworld interrupts
        // Course Clear" quirk. We keep the star itself running to expiry (SMB-faithful: the
        // flash wears off during the fanfare) but skip the music restore. Same IsEndingLevel
        // guard MusicChangeArea uses.
        if (MusicManager.Instance != null && !LevelFlowController.IsEndingLevel)
        {
            MusicManager.Instance.ReleaseOverride(
                StarMusicKey,
                PlayerIndex,
                MusicManager.MusicStartMode.Continue
            );
        }

        ComboManager.Instance?.ResetAll();
        MarioEvents.FireStarPowerEnded(PlayerIndex);
    }

    private void CycleStarColor()
    {
        // Rotate through the star block [starRowStart .. starRowStart + starRowCount).
        int count = Mathf.Max(1, starRowCount);
        int row   = starRowStart + (_starFrame % count);
        _starFrame++;
        _core.Palette?.SetStarFrame(row);   // MarioPalette is the single owner of _PaletteRow
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