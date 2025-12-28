using UnityEngine;

[System.Serializable]
public class PopupSpriteEntry
{
    public PopupID id;

    public Sprite tinySprite;
    public Sprite smallSprite;
    public Sprite bigSprite;
    public Sprite powerSprite;

    public Sprite GetForState(PowerStates.PowerupState state)
    {
        return state switch
        {
            PowerStates.PowerupState.tiny  => tinySprite,
            PowerStates.PowerupState.small => smallSprite,
            PowerStates.PowerupState.big   => bigSprite,
            PowerStates.PowerupState.power => powerSprite,
            _ => bigSprite
        };
    }
}