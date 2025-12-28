using UnityEngine;

[CreateAssetMenu(menuName = "Popups/Popup Style Set")]
public class PopupStyleSet : ScriptableObject
{
    [Header("Tiny Mario Style")]
    public PopupStyle tinyStyle;

    [Header("Small Mario Style")]
    public PopupStyle smallStyle;

    [Header("Big Mario Style")]
    public PopupStyle bigStyle;

    [Header("Power Mario Style (Fire / Ice / Power)")]
    public PopupStyle powerStyle;

    public PopupStyle GetStyle(PowerStates.PowerupState state)
    {
        switch (state)
        {
            case PowerStates.PowerupState.tiny:
                return tinyStyle;

            case PowerStates.PowerupState.small:
                return smallStyle;

            case PowerStates.PowerupState.big:
                return bigStyle;

            case PowerStates.PowerupState.power:
                return powerStyle;

            default:
                return smallStyle; // fallback
        }
    }
}
