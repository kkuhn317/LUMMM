using UnityEngine;

public enum FileSelectActionType
{
    None,
    EnterSlot,    // pipes
    DeleteSlot,
    CopySlot,
    Import,
    Export,
    RenameSlot
}

public class FileSelectInteractable : MonoBehaviour
{
    public FileSelectActionType actionType;
    public int slotIndex; // for file A/B/C, if needed
}