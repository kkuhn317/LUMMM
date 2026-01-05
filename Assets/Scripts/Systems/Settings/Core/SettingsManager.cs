using System.Collections;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Lifetime")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        ApplyAllNonResolution();
        yield return ApplyResolutionAndFullscreen();
    }

    public void ApplyAllNonResolution()
    {
        ApplyMasterVolume();
        ApplyBGMVolume();
        ApplySFXVolume();

        ApplyGraphicsQuality();
        ApplyInfiniteLives();
        ApplyCheckpoints();
        ApplyOnScreenControls();
        ApplySpeedrunMode();
    }

    private void ApplyMasterVolume()
    {
        float volume = PlayerPrefs.GetFloat(SettingsKeys.MasterVolumeKey, 1f);
        AudioListener.volume = volume;
        
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetVolume(SettingType.MasterVolumeKey, volume);
    }

    private void ApplyBGMVolume()
    {
        if (AudioManager.Instance == null) return;

        float volume = PlayerPrefs.GetFloat(SettingsKeys.BGMVolumeKey, 1f);
        AudioManager.Instance.SetVolume(SettingType.BGMVolumeKey, volume);
    }

    private void ApplySFXVolume()
    {
        if (AudioManager.Instance == null) return;

        float volume = PlayerPrefs.GetFloat(SettingsKeys.SFXVolumeKey, 1f);
        AudioManager.Instance.SetVolume(SettingType.SFXVolumeKey, volume);
    }

    private IEnumerator ApplyResolutionAndFullscreen()
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

        Screen.SetResolution(w, h, savedFullscreen);
        yield return null;

#if UNITY_WEBGL && !UNITY_EDITOR
        yield return new WaitForSeconds(0.1f);
#endif
    }

    private void ApplyGraphicsQuality()
    {
        int qualityLevel = PlayerPrefs.GetInt(SettingsKeys.GraphicsQualityKey, QualitySettings.GetQualityLevel());
        QualitySettings.SetQualityLevel(qualityLevel);
    }

    private void ApplyInfiniteLives()
    {
        GlobalVariables.infiniteLivesMode = PlayerPrefs.GetInt(SettingsKeys.InfiniteLivesKey, 0) == 1;
    }

    private void ApplyCheckpoints()
    {
        int mode = PlayerPrefs.HasKey(SettingsKeys.CheckpointModeKey)
            ? PlayerPrefs.GetInt(SettingsKeys.CheckpointModeKey, 0)
            : (PlayerPrefs.GetInt(SettingsKeys.CheckpointsKey, 0) == 1 ? 1 : 0);

        GlobalVariables.checkpointMode = mode;
        GlobalVariables.enableCheckpoints = mode != 0;

        PlayerPrefs.SetInt(SettingsKeys.CheckpointsKey, mode != 0 ? 1 : 0);
    }

    private void ApplyOnScreenControls()
    {
        GlobalVariables.OnScreenControls =
            PlayerPrefs.GetInt(SettingsKeys.OnScreenControlsKey, Application.isMobilePlatform ? 1 : 0) == 1;
    }

    private void ApplySpeedrunMode()
    {
        GlobalVariables.SpeedrunMode = PlayerPrefs.GetInt(SettingsKeys.SpeedrunModeKey, 0) == 1;
    }
}