using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnpauseBlockingWindow : MonoBehaviour
{
    public RebindSettings rebindSettings;
    void OnEnable()
    {
        rebindSettings.DisableTogglePause();
    }

    void OnDisable()
    {
        rebindSettings.EnableTogglePause();
    }
}
