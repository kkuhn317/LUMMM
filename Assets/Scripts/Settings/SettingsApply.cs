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

        var parts = savedResolution.Split('x');
        int w = int.Parse(parts[0]);
        int h = int.Parse(parts[1]);

    #if UNITY_WEBGL && !UNITY_EDITOR
        // For WebGL, just set resolution and fullscreen once
        Screen.SetResolution(w, h, savedFullscreen);
        yield return null; // Allow one frame for stabilization
        
        // Optional: Add a small delay for WebGL fullscreen to stabilize
        yield return new WaitForSeconds(0.1f);
    #else
        // Desktop platforms
        Screen.SetResolution(w, h, savedFullscreen);
        yield return null; // allow one frame for stabilization
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
        // Prefer new 0/1/2 mode key if it exists
        int mode = PlayerPrefs.HasKey(SettingsKeys.CheckpointModeKey)
            ? PlayerPrefs.GetInt(SettingsKeys.CheckpointModeKey, 0)
            : (PlayerPrefs.GetInt(SettingsKeys.CheckpointsKey, 0) == 1 ? 1 : 0);

        GlobalVariables.checkpointMode = mode;       // 0=Off, 1=Visual, 2=Silent
        GlobalVariables.enableCheckpoints = mode != 0;

        // Optional: keep legacy key in sync so older code stays consistent
        PlayerPrefs.SetInt(SettingsKeys.CheckpointsKey, mode != 0 ? 1 : 0);
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