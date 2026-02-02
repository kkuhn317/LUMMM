using UnityEngine;

public class FileSelectSequenceContext
{
    public FileSelectManager manager;
    public SaveSlotManager slotManager;
    public FileSelectMarioController mario;

    public GameObject selectedObject;
    public FileSelectInteractable interactable;
    public Transform anchor;

    // If your custom sequence wants to do the actual action itself, set this true.
    public bool skipDefaultAction;
}