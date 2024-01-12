using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CursorHelper
{
    public static void HideCursor()
    {
        if (!GlobalVariables.OnScreenControls) {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public static void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
