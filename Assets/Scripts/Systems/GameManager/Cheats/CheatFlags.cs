using System;

/// <summary>
/// Pure state container for all cheat flags.
/// This class ONLY holds state and raises events - no side effects!
/// All cheat behaviors are handled by CheatController.
/// </summary>
public static class CheatFlags
{
    // Events - only for notifying when state changes
    public static event Action<bool> OnPlushiesChanged;
    public static event Action<StartPowerupMode> OnStartPowerupModeChanged;
    public static event Action<bool> OnInvincibilityChanged;
    public static event Action<bool> OnAllAbilitiesChanged;
    public static event Action<bool> OnDarknessChanged;
    public static event Action<bool> OnRandomizerChanged;
    public static event Action<bool> OnBetaModeChanged;

    // Private state fields
    private static bool plushies;
    private static StartPowerupMode startPowerupMode = StartPowerupMode.None;
    private static bool invincibility;
    private static bool allAbilities;
    private static bool darkness;
    private static bool randomizer;
    private static bool betaMode;

    // Public properties - only change state and raise events
    public static bool Plushies
    {
        get => plushies;
        set
        {
            if (plushies == value) return;
            plushies = value;
            OnPlushiesChanged?.Invoke(value);
        }
    }

    public static StartPowerupMode StartPowerup
    {
        get => startPowerupMode;
        set
        {
            if (startPowerupMode == value) return;
            startPowerupMode = value;
            OnStartPowerupModeChanged?.Invoke(value);
        }
    }

    public static bool Invincibility
    {
        get => invincibility;
        set
        {
            if (invincibility == value) return;
            invincibility = value;
            OnInvincibilityChanged?.Invoke(value);
        }
    }

    public static bool AllAbilities
    {
        get => allAbilities;
        set
        {
            if (allAbilities == value) return;
            allAbilities = value;
            OnAllAbilitiesChanged?.Invoke(value);
        }
    }

    public static bool Darkness
    {
        get => darkness;
        set
        {
            if (darkness == value) return;
            darkness = value;
            OnDarknessChanged?.Invoke(value);
        }
    }

    public static bool Randomizer
    {
        get => randomizer;
        set
        {
            if (randomizer == value) return;
            randomizer = value;
            OnRandomizerChanged?.Invoke(value);
        }
    }

    public static bool BetaMode
    {
        get => betaMode;
        set
        {
            if (betaMode == value) return;
            betaMode = value;
            OnBetaModeChanged?.Invoke(value);
        }
    }
}