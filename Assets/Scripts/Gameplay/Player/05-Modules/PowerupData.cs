using UnityEngine;
using PowerupState = PowerStates.PowerupState;

/// <summary>
/// Defines what a powerup does — completely character-agnostic.
/// The actual prefab to spawn is looked up on MarioPowerup per character.
/// </summary>
[CreateAssetMenu(fileName = "PowerUpData", menuName = "Game/PowerUp Data")]
public class PowerUpData : ScriptableObject
{
    [Header("Powerup Identity")]
    public PowerupState PowerupState;
    public string PowerupType = "";

    [Header("Palette")]
    [Tooltip("Row in the character's TargetPalette for this element. -1 = normal (bypass).")]
    public int PaletteRow = -1;
}