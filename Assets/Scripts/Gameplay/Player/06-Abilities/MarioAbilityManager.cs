using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Owns the ability collection and resolves which moves are currently available.
///
/// This replaces:
/// - The List<MarioAbility> on MarioMovement
/// - EnableAllAbilities(bool)
/// - The ability flag fields (canCrawl, canWallJump, etc.) — those live on MarioState
///
/// Separate from MarioCore so the ability policy doesn't clutter Core.
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
    [Tooltip("If true, these values take priority over LevelInfo and runtime ability overrides.")]
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
        ApplyEffectiveAbilities();
    }

    private void Update()
    {
        // Also detect the override being switched off so the active runtime
        // policy (cheat, options test, or LevelInfo) is restored immediately.
        if (debugOverrideMoves  != _lastDebugOverride  ||
            (debugOverrideMoves &&
             (debugCanCrawl       != _lastCrawl          ||
              debugCanWallJump    != _lastWallJump       ||
              debugCanSpinJump    != _lastSpinJump       ||
              debugCanGroundPound != _lastGroundPound    ||
              debugCanMidairSpin  != _lastMidairSpin     ||
              debugCanCape        != _lastCape)))
        {
            ApplyEffectiveAbilities();
        }
    }

    private MarioMoves GetDebugMoveMask()
    {
        MarioMoves moves = MarioMoves.None;
        if (debugCanCrawl)       moves |= MarioMoves.Crawl;
        if (debugCanWallJump)    moves |= MarioMoves.WallJump;
        if (debugCanSpinJump)    moves |= MarioMoves.Spin;
        if (debugCanGroundPound) moves |= MarioMoves.GroundPound;
        if (debugCanMidairSpin)  moves |= MarioMoves.Twirl;
        if (debugCanCape)        moves |= MarioMoves.Cape;
        return moves;
    }

    private void RememberDebugSettings()
    {
        _lastDebugOverride  = debugOverrideMoves;
        _lastCrawl          = debugCanCrawl;
        _lastWallJump       = debugCanWallJump;
        _lastSpinJump       = debugCanSpinJump;
        _lastGroundPound    = debugCanGroundPound;
        _lastMidairSpin     = debugCanMidairSpin;
        _lastCape           = debugCanCape;
    }

    /// <summary>
    /// Captures the live per-instance editor override so a powerup prefab swap can
    /// preserve it instead of reverting to the destination prefab's serialized values.
    /// </summary>
    public bool TryGetDebugMoveOverride(out MarioMoves moves)
    {
        moves = GetDebugMoveMask();
        return debugOverrideMoves;
    }

    /// <summary>Restores a debug override captured from the previous Mario body.</summary>
    public void SetDebugMoveOverride(bool enabled, MarioMoves moves)
    {
        debugOverrideMoves  = enabled;
        debugCanCrawl       = moves.HasFlag(MarioMoves.Crawl);
        debugCanWallJump    = moves.HasFlag(MarioMoves.WallJump);
        debugCanSpinJump    = moves.HasFlag(MarioMoves.Spin);
        debugCanGroundPound = moves.HasFlag(MarioMoves.GroundPound);
        debugCanMidairSpin  = moves.HasFlag(MarioMoves.Twirl);
        debugCanCape        = moves.HasFlag(MarioMoves.Cape);

        ApplyEffectiveAbilities();
    }
#endif

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
        ApplyEffectiveAbilities();
    }

    /// <summary>
    /// Resolves abilities from the current level plus any scene-wide override.
    /// Because every newly transformed Mario runs this in Start, overrides
    /// survive powerup prefab replacements without copying temporary state.
    /// </summary>
    public void ApplyEffectiveAbilities()
    {
#if UNITY_EDITOR
        // An explicit per-instance debug configuration has the highest priority.
        // It still uses the normal mask application path, so debug behavior
        // cannot drift away from runtime behavior.
        if (debugOverrideMoves)
        {
            MarioMoves debugMoves = GetDebugMoveMask();
            RememberDebugSettings();
            ApplyMoveMask(debugMoves);
            Debug.Log($"[MarioAbilityManager] Abilities synced from editor debug override: {debugMoves}");
            return;
        }

        RememberDebugSettings();
#endif

        bool grantAll = CheatFlags.AllAbilities || OptionsGameManager.GrantsAllAbilities;
        var info = GlobalVariables.levelInfo;

        if (!grantAll && info == null)
        {
            Debug.LogWarning("[MarioAbilityManager] No LevelInfo found — abilities not synced.");
            return;
        }

        MarioMoves moves = grantAll ? MarioMoves.All : info.marioMoves;
        ApplyMoveMask(moves);

        string source = CheatFlags.AllAbilities
            ? "abilityfreak"
            : OptionsGameManager.GrantsAllAbilities ? "options ability test" : "LevelInfo";
        Debug.Log($"[MarioAbilityManager] Abilities synced from {source}: {moves}");
    }

    /// <summary>Compatibility entry point for editor tooling and older callers.</summary>
    public void ApplyFromLevelInfo() => ApplyEffectiveAbilities();

    private void ApplyMoveMask(MarioMoves moves)
    {
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
            ApplyMoveMask(MarioMoves.All);
            Debug.Log("[MarioAbilityManager] All abilities cheat ON");
        }
        else
        {
            ApplyEffectiveAbilities();
            Debug.Log("[MarioAbilityManager] All abilities cheat OFF (effective abilities restored)");
        }
    }
}
