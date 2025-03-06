using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Extra functionality for the Game Manager in the test level in the rebind menu
public class OptionsGameManager : MonoBehaviour
{
    public CanvasGroup rebindCanvasGroup;
    public GameObject[] mobileButtons;
    public RebindSettings rebindSettings;

    public void OnPause() {
        rebindCanvasGroup.interactable = true;
        foreach (GameObject button in mobileButtons)
        {
            button.SetActive(false);
        }
    }

    public void OnResume() {
        rebindCanvasGroup.interactable = false;
        foreach (GameObject button in mobileButtons)
        {
            button.SetActive(true);
        }
    }

    // Used by the back button in the rebind window so that you can leave the scene
    public void SetNormalTimeScale() {
        Time.timeScale = 1f;
    }

    public bool CanTogglePause() {
        return rebindSettings.CanTogglePause;
    }
}
