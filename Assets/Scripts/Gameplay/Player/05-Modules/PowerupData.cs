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

    [Header("Projectile Ability (optional)")]
    [Tooltip("Projectile to shoot (fireball, iceball, ...). Null = this power grants no shooting.")]
    public GameObject projectile;
    [Tooltip("Sound played when shooting this projectile.")]
    public AudioClip projectileSound;

    [Header("Sprite Library (optional)")]
    [Tooltip("Overrides the character's NormalSpriteLibrary for this powerup (same body, a few\n" +
             "different frames like Old Fire). Null = use NormalSpriteLibrary.")]
    public UnityEngine.U2D.Animation.SpriteLibraryAsset overrideSpriteLibrary;
}