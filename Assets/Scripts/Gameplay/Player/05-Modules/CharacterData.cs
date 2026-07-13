using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;
using PowerupState = PowerStates.PowerupState;

/// <summary>
/// Defines all prefab variants for a single character (Mario, Luigi, etc.).
/// Assigned once per character and shared across all that character's prefabs.
///
/// To add a new powerup variant for a character:
/// 1. Create the new prefab
/// 2. Add an entry here — no other files need changing.
/// </summary>
[CreateAssetMenu(fileName = "CharacterData", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    public string CharacterName;

    [Header("Emote Sprite Libraries")]
    [Tooltip("Maps emotes to this character's sprite libraries.")]
    public List<EmoteEntry> EmoteLibraries = new();

    [System.Serializable]
    public class EmoteEntry
    {
        public MarioEmote emote;
        public SpriteLibraryAsset library;
        [Tooltip("Optional sound played when this emote is triggered for this character.")]
        public AudioClip audio;
    }

    public SpriteLibraryAsset GetEmoteLibrary(MarioEmote emote)
    {
        foreach (var e in EmoteLibraries)
            if (e.emote == emote) return e.library;

        // Fallback to Normal
        foreach (var e in EmoteLibraries)
            if (e.emote == MarioEmote.Normal) return e.library;

        return null;
    }

    public AudioClip GetEmoteAudio(MarioEmote emote)
    {
        foreach (var e in EmoteLibraries)
            if (e.emote == emote) return e.audio;
        return null;
    }

    [Header("Transform Shell")]
    [Tooltip("Shell prefab that holds the PlayerTransformation animation component.")]
    public GameObject TransformShellPrefab;

    [Header("Powerup Prefabs")]
    [Tooltip("Maps PowerUpData assets to this character's prefabs.")]
    public PowerupPrefabEntry[] PowerupPrefabs;

    [Header("Death")]
    [Tooltip("Default death cause used as final fallback.")]
    public DeathCause DefaultDeathCause;

    [Tooltip("Maps cause + powerup combinations to death prefabs.")]
    public DeathPrefabEntry[] DeathPrefabs;

    // ─── Death Lookup ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the correct death prefab based on cause and current powerup.
    ///
    /// Priority:
    /// 1. Exact match: cause + powerup both match
    /// 2. Cause only: cause matches, PowerupState is null (wildcard)
    /// 3. Default: DefaultDeathCause + null powerup
    /// </summary>
    public GameObject GetDeadPrefab(DeathCause cause, PowerUpData currentPowerup)
    {
        if (DeathPrefabs == null) return null;

        // 1. Exact match: cause + powerup
        foreach (var entry in DeathPrefabs)
            if (entry.Cause == cause && entry.PowerupState == currentPowerup)
                return entry.Prefab;

        // 2. Cause only (any powerup)
        foreach (var entry in DeathPrefabs)
            if (entry.Cause == cause && entry.PowerupState == null)
                return entry.Prefab;

        // 3. Default fallback
        if (DefaultDeathCause != null)
        {
            foreach (var entry in DeathPrefabs)
                if (entry.Cause == DefaultDeathCause && entry.PowerupState == null)
                    return entry.Prefab;
        }

        return null;
    }

    // ─── Lookup ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Finds the prefab for the given PowerUpData by reference equality.
    /// </summary>
    public GameObject FindPrefab(PowerUpData data)
    {
        if (PowerupPrefabs == null || data == null) return null;

        foreach (var entry in PowerupPrefabs)
            if (entry.Data == data)
                return entry.Prefab;

        return null;
    }

    /// <summary>
    /// Returns the prefab Mario should revert to when taking damage,
    /// based on his current powerup state.
    ///
    /// power  → big
    /// big    → small
    /// small  → tiny (if exists, otherwise null → MarioCombat handles death)
    /// tiny   → null (already smallest, MarioCombat kills)
    /// </summary>
    public GameObject GetPowerDownPrefab(PowerupState currentState)
    {
        PowerupState targetState = currentState switch
        {
            PowerupState.power => PowerupState.big,
            PowerupState.big   => PowerupState.small,
            PowerupState.small => PowerupState.tiny,
            _                  => PowerupState.tiny
        };

        return FindPrefabByState(targetState);
    }

    /// <summary>
    /// Public lookup: returns this character's base prefab for a given powerup
    /// state (e.g. the small variant), or null if none is defined. Unlike
    /// GetPowerDownPrefab this does not step a tier — it returns the exact state.
    /// </summary>
    public GameObject GetPrefabForState(PowerupState state) => FindPrefabByState(state);

    /// <summary>
    /// Finds the base prefab for a state.
    ///
    /// Priority:
    /// 1. Matching state with an empty PowerupType.
    /// 2. Matching state whose data has no element palette/projectile. This supports
    ///    base assets named "big", "small", etc. instead of requiring an empty type.
    /// 3. First matching state as a safe legacy fallback.
    /// </summary>
    private GameObject FindPrefabByState(PowerupState state)
    {
        if (PowerupPrefabs == null) return null;

        foreach (var entry in PowerupPrefabs)
            if (entry.Data != null &&
                entry.Prefab != null &&
                entry.Data.PowerupState == state &&
                string.IsNullOrWhiteSpace(entry.Data.PowerupType))
                return entry.Prefab;

        foreach (var entry in PowerupPrefabs)
            if (entry.Data != null &&
                entry.Prefab != null &&
                entry.Data.PowerupState == state &&
                entry.Data.PaletteRow < 0 &&
                entry.Data.projectile == null)
                return entry.Prefab;

        foreach (var entry in PowerupPrefabs)
            if (entry.Data != null &&
                entry.Prefab != null &&
                entry.Data.PowerupState == state)
                return entry.Prefab;

        return null;
    }
}