using UnityEngine;

[CreateAssetMenu(menuName = "Popups/Popup Data Set")]
public class PopupDataSet : ScriptableObject
{
    public PopupSpriteEntry[] entries;

    public Sprite GetSprite(PopupID id, PowerStates.PowerupState state)
    {
        foreach (var entry in entries)
        {
            if (entry.id == id)
            {
                return entry.GetForState(state);
            }
        }

        Debug.LogWarning($"PopupDataSet: No entry found for PopupID: {id}");
        return null;
    }
}