using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is on the HideShow Button in the rebind menu. It hides or shows the mobile buttons
public class MobileHideShow : MonoBehaviour
{
    public GameObject[] mobileButtons;

    // Start is called before the first frame update
    void Start()
    {
        // Start with the buttons hidden
        foreach (GameObject button in mobileButtons)
        {
            button.SetActive(false);
        }
    }

    public void HideShow()
    {
        foreach (GameObject button in mobileButtons)
        {
            button.SetActive(!button.activeSelf);
        }
    }
}
