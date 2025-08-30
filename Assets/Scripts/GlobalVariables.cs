using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

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
                UnityEngine.Debug.Log("Layout not loaded (should only happen in editor).");
                return new();
            }
        }
    }

    // Mobile
    public static bool mobileRunButtonPressed = false;

    // Secret codes
    public static bool cheatPlushies = false;
    public static bool cheatBetaMode = false;
    public static bool cheatInvincibility = false;
    public static bool cheatAllAbilities = false;
    public static bool cheatStartTiny = false;

    // Speedrun Timer
    public static Stopwatch speedrunTimer = new();
    public static TimeSpan timerOffset = TimeSpan.Zero;
    public static TimeSpan elapsedTime => timerOffset.Add(speedrunTimer.Elapsed);

    // Converts to String FOR USE BY PLAYERPREFS (not for displaying)
    public static string ElapsedTimeToString() {
        return elapsedTime.TotalMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public static void SetTimerOffsetFromString(string timeString) {
        timerOffset = TimeSpan.FromMilliseconds(double.Parse(timeString, System.Globalization.CultureInfo.InvariantCulture));
    }

    // Other
    public static void ResetForLevel()
    {
        lives = levelInfo.lives;
        coinCount = 0;
        score = 0;
        checkpoint = -1;
        speedrunTimer.Reset();  // Reset to 0 and stop
        timerOffset = TimeSpan.Zero;
    }
}