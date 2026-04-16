using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class SelectionPreservingInputModule : InputSystemUIInputModule
{
    private GameObject _lastSelected;

    public override void Process()
    {
        // Cache before processing
        if (eventSystem.currentSelectedGameObject != null)
        {
            _lastSelected = eventSystem.currentSelectedGameObject;
        }

        base.Process();

        // If selection was cleared, restore it
        if (eventSystem.currentSelectedGameObject == null && _lastSelected != null)
        {
            eventSystem.SetSelectedGameObject(_lastSelected);
        }
    }
}