using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem.Interactions;

public static class GlobalVariables
{
    public static int lives = 3;
    public static int score = 0;
    public static int coinCount = 0;
    public static LevelInfo levelInfo;

    // The id of the last checkpoint the player touched
    // -1 means no checkpoint
    public static int checkpoint = -1;

    // Modifiers
    public static bool infiniteLivesMode = false;
    public static bool enableCheckpoints = false;
    public static bool stopTimeLimit = false;

    // Settings
    public static bool OnScreenControls = false;
    public static bool SpeedrunMode = false;
    public static Dictionary<string, RebindLayoutData> Layouts = new();
    public static string currentLayoutName = RebindSaveLoad.DefaultLayoutName;
    public static RebindLayoutData currentLayout {
        get {
            try {
                return Layouts[currentLayoutName];
            } catch {
                Debug.Log("Layout not loaded (should only happen in editor).");
                return new();
            }
        }
    }

    // Mobile
    public static bool mobileRunButtonPressed = false;

    // Secret codes
    public static bool enablePlushies = false;
    public static bool enableBetaMode = false;

    public static void ResetForLevel()
    {
        GlobalVariables.lives = levelInfo.lives;
        coinCount = 0;
        score = 0;
        checkpoint = -1;
    }
}