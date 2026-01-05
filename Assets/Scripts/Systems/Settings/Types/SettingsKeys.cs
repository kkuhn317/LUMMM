using System.Collections.Generic;

public enum SettingType
{
    FullscreenKey,
    ResolutionKey,
    GraphicsQualityKey,
    MasterVolumeKey,
    BGMVolumeKey,
    SFXVolumeKey,
    // VoiceVolumeKey,
    OnScreenControlsKey,
    SpeedrunModeKey,
    InfiniteLivesKey,
    CheckpointsKey,
    CheckpointModeKey,
    TimeLimitKey,
}

public static class SettingsKeys
{
    // Stored PlayerPrefs key strings
    public const string FullscreenKey = "Fullscreen";
    public const string ResolutionKey = "Resolution";
    public const string GraphicsQualityKey = "GraphicsQuality";
    public const string MasterVolumeKey = "MasterVolume";
    public const string BGMVolumeKey = "BGMVolume";
    public const string SFXVolumeKey = "SFXVolume";
    // public const string VoiceVolumeKey = "VoiceVolume";
    public const string OnScreenControlsKey = "OnScreenControls";
    public const string SpeedrunModeKey = "SpeedrunMode";

    // Modifiers
    public const string InfiniteLivesKey = "InfiniteLives";
    public const string CheckpointsKey = "Checkpoints";
    public const string CheckpointModeKey = "CheckpointMode";
    public const string TimeLimitKey = "TimeLimit";

    // stored string mapping
    private static readonly Dictionary<SettingType, string> keys = new()
    {
        { SettingType.FullscreenKey, FullscreenKey },
        { SettingType.ResolutionKey, ResolutionKey },
        { SettingType.GraphicsQualityKey, GraphicsQualityKey },

        { SettingType.MasterVolumeKey, MasterVolumeKey },
        { SettingType.BGMVolumeKey, BGMVolumeKey },
        { SettingType.SFXVolumeKey, SFXVolumeKey },
        // { SettingType.VoiceVolumeKey, VoiceVolumeKey },

        { SettingType.OnScreenControlsKey, OnScreenControlsKey },
        { SettingType.SpeedrunModeKey, SpeedrunModeKey },

        { SettingType.InfiniteLivesKey, InfiniteLivesKey },
        { SettingType.CheckpointsKey, CheckpointsKey },
        { SettingType.CheckpointModeKey, CheckpointModeKey },
        { SettingType.TimeLimitKey, TimeLimitKey },
    };

    public static string Get(SettingType type) =>
        keys.TryGetValue(type, out var value) ? value : type.ToString();
}