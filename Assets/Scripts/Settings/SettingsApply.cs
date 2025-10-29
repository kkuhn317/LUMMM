using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class SettingsApply : MonoBehaviour
{
    [SerializeField] private AudioMixer musicMixer;
    [SerializeField] private AudioMixer sfxMixer;

    private IEnumerator Start()
    {
        InitMasterVolume();
        InitBGMVolume();
        InitSFXVolume();
        InitGraphicsQuality();
        InitInfiniteLives();
        InitCheckpoints();
        InitOnScreenControls();
        InitSpeedrunMode();

        // Resolution & fullscreen: handled with a special case for WebGL
        yield return InitResolutionAndFullscreen();
    }

    private void InitMasterVolume()
    {
        float volume = PlayerPrefs.GetFloat(SettingsKeys.MasterVolumeKey, 1f);
        AudioListener.volume = volume;
    }

    private void InitBGMVolume()
    {
        float volume = PlayerPrefs.GetFloat(SettingsKeys.BGMVolumeKey, 1f);
        // Map [0..1] to decibels (avoid -Infinity when 0)
        musicMixer.SetFloat("MusicVolume", volume == 0 ? -80f : Mathf.Log10(volume) * 20f);
    }

    private void InitSFXVolume()
    {
        float volume = PlayerPrefs.GetFloat(SettingsKeys.SFXVolumeKey, 1f);
        // Map [0..1] to decibels (avoid -Infinity when 0)
        sfxMixer.SetFloat("SFXVolume", volume == 0 ? -80f : Mathf.Log10(volume) * 20f);
    }

    private IEnumerator InitResolutionAndFullscreen()
    {
        // Read saved resolution (use a sensible default per platform)
        string savedResolution =
#if UNITY_WEBGL && !UNITY_EDITOR
            PlayerPrefs.GetString(SettingsKeys.ResolutionKey, "960x600");
#else
            PlayerPrefs.GetString(
                SettingsKeys.ResolutionKey,
                $"{Screen.currentResolution.width}x{Screen.currentResolution.height}"
            );
#endif
        bool savedFullscreen = PlayerPrefs.GetInt(SettingsKeys.FullscreenKey, 1) == 1;

        // Parse "WxH"
        var parts = savedResolution.Split('x');
        int w = int.Parse(parts[0]);
        int h = int.Parse(parts[1]);

#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL:
        // - Browsers only allow entering fullscreen from a user gesture.
        // - Here we set the internal backbuffer size only.
        // - Actual fullscreen will be applied when the user clicks a button (see method below).
        Screen.SetResolution(w, h, false);
        yield return null; // let the canvas/layout settle one frame
#else
        // Desktop platforms: we can apply fullscreen immediately.
        Screen.SetResolution(w, h, savedFullscreen);
        yield return null; // allow one frame for stabilization
        Screen.fullScreen = savedFullscreen; // explicit for clarity
#endif
    }

    private void InitGraphicsQuality()
    {
        int qualityLevel = PlayerPrefs.GetInt(SettingsKeys.GraphicsQualityKey, QualitySettings.GetQualityLevel());
        QualitySettings.SetQualityLevel(qualityLevel);
    }

    private void InitInfiniteLives()
    {
        GlobalVariables.infiniteLivesMode = PlayerPrefs.GetInt(SettingsKeys.InfiniteLivesKey, 0) == 1;
    }

    private void InitCheckpoints()
    {
        GlobalVariables.enableCheckpoints = PlayerPrefs.GetInt(SettingsKeys.CheckpointsKey, 0) == 1;
    }

    private void InitOnScreenControls()
    {
        GlobalVariables.OnScreenControls =
            PlayerPrefs.GetInt(SettingsKeys.OnScreenControlsKey, Application.isMobilePlatform ? 1 : 0) == 1;
    }

    private void InitSpeedrunMode()
    {
        GlobalVariables.SpeedrunMode = PlayerPrefs.GetInt(SettingsKeys.SpeedrunModeKey, 0) == 1;
    }
}