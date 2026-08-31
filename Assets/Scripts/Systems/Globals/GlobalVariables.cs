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
    public static bool infiniteTimeMode = false;
    public static bool enableCheckpoints = false;
    public static int checkpointMode = 0;

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
    // Compatibility accessors for older callers. New held-input code uses the
    // centralized MobileHeldInputState below.
    public static bool mobileRunButtonPressed
    {
        get => MobileHeldInputState.Run;
        set => MobileHeldInputState.Run = value;
    }
    public static Vector2 mobileMoveInput
    {
        get => MobileHeldInputState.Move;
        set => MobileHeldInputState.Move = value;
    }

    // Secret codes
    public static bool cheatPlushies = false;
    public static bool cheatBetaMode = false;
    public static bool cheatInvincibility = false;
    public static bool cheatAllAbilities = false;
    public static bool cheatStartTiny = false;
    public static bool cheatStartIce = false;
    public static bool cheatFlamethrower = false;
    public static bool cheatDarkness = false;
    public static bool cheatRandomizer = false;

    // Speedrun Timer
    public static Stopwatch speedrunTimer = new();  // Don't read the time from this, use elapsedTime instead!
    public static TimeSpan timerOffset = TimeSpan.Zero;
    public static TimeSpan elapsedTime => timerOffset.Add(speedrunTimer.Elapsed);

    // Other
    public static void ResetForLevel()
    {
        lives = levelInfo.lives;
        coinCount = 0;
        score = 0;
        checkpoint = -1;
        MobileHeldInputState.ResetTransient();
        speedrunTimer.Reset();  // Reset to 0 and stop
        timerOffset = TimeSpan.Zero;
    }
}

/// <summary>
/// Logical held state for UI controls. Unlike InputAction controls, these
/// values survive replacement of the Mario prefab during a transformation.
/// One-shot actions such as Extra and object interaction are intentionally not
/// stored here; Use represents the held X/shoot control.
/// </summary>
public static class MobileHeldInputState
{
    public static Vector2 Move;
    public static bool Run;
    public static bool Jump;
    public static bool Use;
    public static bool Spin;

    public static void Reset()
    {
        ResetTransient();
        Run = false;
    }

    public static void ResetTransient()
    {
        Move = Vector2.zero;
        Jump = false;
        Use = false;
        Spin = false;
    }
}
