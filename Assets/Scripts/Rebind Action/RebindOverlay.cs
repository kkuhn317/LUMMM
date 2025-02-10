using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Samples.RebindUI;
using UnityEngine.UI;

public class RebindOverlay : MonoBehaviour
{
    [HideInInspector] public RebindActionUI rebindActionUI;   // Reference to the button that enables the rebind overlay
    public TMP_Text[] timerTexts;
    private float timer = 0f;

    void OnEnable() {
        timer = 5f;
    }

    void Update() {
        timer -= Time.unscaledDeltaTime;
        if (timer <= 0) {
            Cancel();
        }
        foreach (TMP_Text timerText in timerTexts) {
            timerText.text = Mathf.Ceil(timer).ToString();
            // show 2 decimal places
            //timerText.text = timer.ToString("F2");
        }
    }

    // Called from the Cancel button in the rebind overlay
    public void Cancel() {
        print("cancelling");
        if (rebindActionUI == null) return;
        rebindActionUI.ongoingRebind.Cancel();
    }

    // Called when the rebind overlay is disabled by the RebindActionUI
    void OnDisable() {
        if (rebindActionUI == null) return;
        EventSystem.current.SetSelectedGameObject(rebindActionUI.gameObject);
        rebindActionUI = null;
    }
    
}
