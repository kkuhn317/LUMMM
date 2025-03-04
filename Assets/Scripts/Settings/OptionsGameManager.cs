using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Extra functionality for the Game Manager in the test level in the rebind menu
public class OptionsGameManager : MonoBehaviour
{
    public CanvasGroup rebindCanvasGroup;
    public GameObject[] mobileButtons;

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
}
