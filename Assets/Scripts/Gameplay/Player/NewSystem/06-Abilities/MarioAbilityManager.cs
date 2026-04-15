using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Owns the ability collection and cheat-unlock snapshot/restore logic.
///
/// This replaces:
/// - The List<MarioAbility> on MarioMovement
/// - CaptureAbilitySnapshot / RestoreAbilitySnapshot
/// - EnableAllAbilities(bool)
/// - The ability flag fields (canCrawl, canWallJump, etc.) — those live on MarioState
///
/// Separate from MarioCore so the cheat and snapshot logic doesn't clutter Core.
/// MarioCore.NotifyAbilities() delegates to this module.
/// </summary>
[RequireComponent(typeof(MarioCore))]
public class MarioAbilityManager : MonoBehaviour
{
    private MarioCore _core;
    private MarioState State => _core.State;
    private List<MarioAbility> _abilities = new();

    // ─── Debug Overrides ────────────────────────────────────────────────────

#if UNITY_EDITOR
    [Header("Debug — Move Overrides")]
    [Tooltip("If true, ignores LevelInfo and uses the values below instead.")]
    [SerializeField] private bool debugOverrideMoves = false;
    [SerializeField] private bool debugCanCrawl = true;
    [SerializeField] private bool debugCanWallJump = true;
    [SerializeField] private bool debugCanSpinJump = true;
    [SerializeField] private bool debugCanGroundPound = true;
    [SerializeField] private bool debugCanMidairSpin = true;
    [SerializeField] private bool debugCanCape = true;

    private bool _lastDebugOverride;
    private bool _lastCrawl, _lastWallJump, _lastSpinJump, _lastGroundPound, _lastMidairSpin, _lastCape;

    private void OnValidate()
    {
        // Only apply in play mode so edit-mode changes don't cause issues
        if (!UnityEngine.Application.isPlaying) return;
        if (_core == null) return;
        ApplyDebugOverrides();
    }

    private void Update()
    {
        if (!debugOverrideMoves) return;

        // Detect any change and re-apply
        if (debugOverrideMoves  != _lastDebugOverride  ||
            debugCanCrawl       != _lastCrawl          ||
            debugCanWallJump    != _lastWallJump        ||
            debugCanSpinJump    != _lastSpinJump        ||
            debugCanGroundPound != _lastGroundPound     ||
            debugCanMidairSpin  != _lastMidairSpin      ||
            debugCanCape        != _lastCape)
        {
            ApplyDebugOverrides();
        }
    }

    private void ApplyDebugOverrides()
    {
        if (!debugOverrideMoves)
        {
            // Switching off — restore from LevelInfo
            if (_lastDebugOverride)
                ApplyFromLevelInfo();
            _lastDebugOverride = false;
            return;
        }

        _lastDebugOverride  = debugOverrideMoves;
        _lastCrawl          = debugCanCrawl;
        _lastWallJump       = debugCanWallJump;
        _lastSpinJump       = debugCanSpinJump;
        _lastGroundPound    = debugCanGroundPound;
        _lastMidairSpin     = debugCanMidairSpin;
        _lastCape           = debugCanCape;

        State.CanCrawl       = debugCanCrawl;
        State.CanWallJump    = debugCanWallJump;
        State.CanSpinJump    = debugCanSpinJump;
        State.CanGroundPound = debugCanGroundPound;
        State.CanMidairSpin  = debugCanMidairSpin;

        var cape = GetComponent<CapeAttack>();
        if (debugCanCape)
        {
            if (cape == null) cape = gameObject.AddComponent<CapeAttack>();
            cape.enabled = true;
            Add(cape);
        }
        else if (cape != null)
        {
            cape.enabled = false;
        }
    }
#endif

    // ─── Snapshot (for cheat toggle) ────────────────────────────────────────

    private bool            _hasSnapshot;
    private AbilitySnapshot _snapshot;

    [System.Serializable]
    private struct AbilitySnapshot
    {
        public bool CanCrawl;
        public bool CanWallJump;
        public bool CanSpinJump;
        public bool CanGroundPound;
        public bool CanMidairSpin;
        public bool HadCape;
        public bool CapeEnabled;
        public bool HadFirePower;
        public bool FirePowerEnabled;
    }

    // ─── Lifecycle ───────────────────────────────────────────────────────────

    private void Awake() => _core = GetComponent<MarioCore>();

    /// <summary>Called by MarioCore.Awake after all modules are cached.</summary>
    public void Initialize(MarioCore core)
    {
        _core = core;
        _abilities.Clear();
        var found = GetComponents<MarioAbility>();
        foreach (var a in found)
        {
            a.Initialize(_core);
            _abilities.Add(a);
        }
    }

    private void Start()
    {
        // Deferred to Start so GlobalVariables.levelInfo is guaranteed to be set
        ApplyFromLevelInfo();
    }

    /// <summary>
    /// Reads marioMoves from GlobalVariables.levelInfo and applies them
    /// to MarioState and ability components. This ensures the pause menu
    /// icons and actual Mario abilities are always in sync.
    /// </summary>
    public void ApplyFromLevelInfo()
    {
        var info = GlobalVariables.levelInfo;
        if (info == null)
        {
            Debug.LogWarning("[MarioAbilityManager] No LevelInfo found — abilities not synced.");
            return;
        }

        var moves = info.marioMoves;

        State.CanCrawl       = moves.HasFlag(MarioMoves.Crawl);
        State.CanWallJump    = moves.HasFlag(MarioMoves.WallJump);
        State.CanSpinJump    = moves.HasFlag(MarioMoves.Spin);
        State.CanGroundPound = moves.HasFlag(MarioMoves.GroundPound);
        State.CanMidairSpin  = moves.HasFlag(MarioMoves.Twirl);

        // Cape
        var cape = GetComponent<CapeAttack>();
        if (moves.HasFlag(MarioMoves.Cape))
        {
            if (cape == null) cape = gameObject.AddComponent<CapeAttack>();
            cape.enabled = true;
            Add(cape);
        }
        else if (cape != null)
        {
            cape.enabled = false;
        }

        Debug.Log($"[MarioAbilityManager] Abilities synced from LevelInfo: {moves}");
    }

    // ─── Notification ────────────────────────────────────────────────────────

    /// <summary>
    /// Iterates backwards so destroyed entries can be removed safely mid-loop.
    /// Called by MarioCore.NotifyAbilities.
    /// </summary>
    public void Notify(System.Action<MarioAbility> action)
    {
        for (int i = _abilities.Count - 1; i >= 0; i--)
        {
            var a = _abilities[i];
            if (a == null) { _abilities.RemoveAt(i); continue; }
            if (!a.enabled)  continue;
            action(a);
        }
    }

    public void Add(MarioAbility ability)
    {
        if (!_abilities.Contains(ability))
        {
            ability.Initialize(_core);
            _abilities.Add(ability);
        }
    }

    public void Remove(MarioAbility ability) => _abilities.Remove(ability);

    // ─── Cheat: Enable All Abilities ────────────────────────────────────────

    public void EnableAllAbilities(bool enable)
    {
        if (enable)
        {
            CaptureSnapshot();

            State.CanCrawl       = true;
            State.CanWallJump    = true;
            State.CanSpinJump    = true;
            State.CanGroundPound = true;
            State.CanMidairSpin  = true;

            var cape = GetComponent<CapeAttack>() ?? gameObject.AddComponent<CapeAttack>();
            cape.enabled = true;
            Add(cape);

            var fire = GetComponent<FirePower>() ?? gameObject.AddComponent<FirePower>();
            fire.enabled = true;
            Add(fire);

            Debug.Log("[MarioAbilityManager] All abilities cheat ON");
        }
        else
        {
            RestoreSnapshot();
            Debug.Log("[MarioAbilityManager] All abilities cheat OFF (snapshot restored)");
        }
    }

    // ─── Snapshot ────────────────────────────────────────────────────────────

    private void CaptureSnapshot()
    {
        var cape = GetComponent<CapeAttack>();
        var fire = GetComponent<FirePower>();
        _snapshot = new AbilitySnapshot
        {
            CanCrawl         = State.CanCrawl,
            CanWallJump      = State.CanWallJump,
            CanSpinJump      = State.CanSpinJump,
            CanGroundPound   = State.CanGroundPound,
            CanMidairSpin    = State.CanMidairSpin,
            HadCape          = cape != null,
            CapeEnabled      = cape != null && cape.enabled,
            HadFirePower     = fire != null,
            FirePowerEnabled = fire != null && fire.enabled,
        };
        _hasSnapshot = true;
    }

    private void RestoreSnapshot()
    {
        if (!_hasSnapshot) return;

        State.CanCrawl       = _snapshot.CanCrawl;
        State.CanWallJump    = _snapshot.CanWallJump;
        State.CanSpinJump    = _snapshot.CanSpinJump;
        State.CanGroundPound = _snapshot.CanGroundPound;
        State.CanMidairSpin  = _snapshot.CanMidairSpin;

        RestoreAbilityComponent<CapeAttack>(_snapshot.HadCape, _snapshot.CapeEnabled);
        RestoreAbilityComponent<FirePower> (_snapshot.HadFirePower, _snapshot.FirePowerEnabled);
    }

    private void RestoreAbilityComponent<T>(bool had, bool wasEnabled) where T : MarioAbility
    {
        var component = GetComponent<T>();
        if (component != null)
        {
            component.enabled = wasEnabled;
        }
        else if (had && wasEnabled)
        {
            var added = gameObject.AddComponent<T>();
            added.enabled = true;
            Add(added);
        }
    }
}