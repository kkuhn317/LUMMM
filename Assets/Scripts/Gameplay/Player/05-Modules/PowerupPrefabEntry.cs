using UnityEngine;
using PowerupState = PowerStates.PowerupState;

/// <summary>
/// Maps a powerup identity (state + type) to a character-specific prefab.
/// Used in CharacterData to define all prefab variants for a character.
/// </summary>
[System.Serializable]
public struct PowerupPrefabEntry
{
    public PowerUpData Data;
    public GameObject  Prefab;
}