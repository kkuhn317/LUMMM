using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Samples.RebindUI;
using UnityEngine.UI;

public class RebindOverlay : MonoBehaviour
{
    [HideInInspector] public RebindActionUI rebindActionUI;   // Reference to the button that enables the rebind overlay

    // Called from the Cancel button in the rebind overlay
    public void Cancel() {
        if (rebindActionUI == null) return;
        rebindActionUI.ongoingRebind.Cancel();
    }

    // Called when the rebind overlay is disabled by the RebindActionUI
    public void OnDisable() {
        if (rebindActionUI == null) return;
        EventSystem.current.SetSelectedGameObject(rebindActionUI.gameObject);
        rebindActionUI = null;
    }
    
}
