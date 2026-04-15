using UnityEngine;

/// <summary>
/// Maps a death cause + optional powerup state to a character-specific death prefab.
/// Used in CharacterData to define all death variants for a character.
///
/// Lookup priority:
/// 1. Exact match: cause + powerup both match
/// 2. Cause only: cause matches, PowerupState is null (any powerup)
/// 3. Default fallback: defaultDeathCause on CharacterData
/// </summary>
[System.Serializable]
public struct DeathPrefabEntry
{
    [Tooltip("What killed Mario. Required.")]
    public DeathCause Cause;

    [Tooltip("Which powerup Mario had. Leave null to match any powerup.")]
    public PowerUpData PowerupState;

    [Tooltip("The death prefab to spawn for this combination.")]
    public GameObject Prefab;
}
