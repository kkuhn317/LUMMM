using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public class GameSettings : MonoBehaviour
{
    [Header("UI")]
    public Toggle fullscreenToggle;
    public Toggle OnScreenControlsToggle;
    public Toggle SpeedrunModeToggle;
    public TMP_Dropdown resolutionDropdown;
    public TMP_Text resolutionText;
    public TMP_Dropdown graphicsQualityDropdown;
    public TMP_Text graphicsQualityText;

    [Header("Fullscreen")]
    public Image fullscreenImage;
    public Sprite fullscreenOnSprite;
    public Sprite fullscreenOffSprite;

    [Header("Gameplay")]
    public Image OnScreenControlsImage;
    public Sprite enabledOnScreenControls;
    public Sprite disabledOnScreenControls;

    public Image SpeedrunModeImage;
    public Sprite enabledSpeedrunMode;
    public Sprite disabledSpeedrunMode;

    private void Start()
    {
        ConfigureResolution();
        ConfigureFullscreenToggle();
        ConfigureGraphicsQuality();
        ConfigureOnScreenControls();
        ConfigureSpeedrunMode();
    }

    Resolution[] GetResolutions()
    {
        Resolution[] resolutions;
        if (Application.platform == RuntimePlatform.Android)
        {
            // Swap ONLY if height is greater than width (on Kevin's android phone, the resolutions are in portrait mode which needs to be swapped)
            resolutions = Screen.resolutions.Select(resolution => new Resolution { width = Mathf.Max(resolution.width, resolution.height), height = Math.Min(resolution.width, resolution.height) }).Distinct().ToArray();
        }
        else
        {
            resolutions = Screen.resolutions.Select(resolution => new Resolution { width = resolution.width, height = resolution.height }).Distinct().ToArray();
        }
        return resolutions;
    }

    private void ConfigureResolution()
    {
        // Get current screen resolution from PlayerPrefs or use the current screen resolution
        string savedResolution = PlayerPrefs.GetString(SettingsKeys.ResolutionKey, Screen.currentResolution.width + "x" + Screen.currentResolution.height);
        string[] resolutionParts = savedResolution.Split('x');
        int savedWidth = int.Parse(resolutionParts[0]);
        int savedHeight = int.Parse(resolutionParts[1]);
        Resolution currentResolution = new Resolution { width = savedWidth, height = savedHeight };
        resolutionText.text = currentResolution.width + "x" + currentResolution.height;

        // Get all available resolutions
        Resolution[] availableResolutions = GetResolutions();

        // Populate resolution dropdown
        resolutionDropdown.ClearOptions();
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            string resolutionText = availableResolutions[i].width + "x" + availableResolutions[i].height;
            TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(resolutionText);
            resolutionDropdown.options.Add(option);

            if (availableResolutions[i].width == savedWidth && availableResolutions[i].height == savedHeight)
            {
                resolutionDropdown.value = i;
            }
        }

        // Set the selected resolution
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(ChangeResolution);
    }

    public void ChangeResolution(int index)
    {
        Resolution[] availableResolutions = GetResolutions();

        if (index >= 0 && index < availableResolutions.Length)
        {
            Resolution selectedResolution = availableResolutions[index];
            Screen.SetResolution(selectedResolution.width, selectedResolution.height, Screen.fullScreen);
            resolutionText.text = selectedResolution.width + "x" + selectedResolution.height;
            PlayerPrefs.SetString(SettingsKeys.ResolutionKey, selectedResolution.width + "x" + selectedResolution.height);
        }
    }

    private void ConfigureFullscreenToggle()
    {
        bool isFullscreen = PlayerPrefs.GetInt(SettingsKeys.FullscreenKey, 1) == 1;
        fullscreenToggle.isOn = isFullscreen;
        fullscreenImage.sprite = isFullscreen ? fullscreenOnSprite : fullscreenOffSprite;
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleValueChanged);
    }

    private void OnFullscreenToggleValueChanged(bool isFullscreen)
    {
        fullscreenImage.sprite = isFullscreen ? fullscreenOnSprite : fullscreenOffSprite;
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(SettingsKeys.FullscreenKey, isFullscreen ? 1 : 0);
    }

    private void ConfigureGraphicsQuality()
    {
        int currentQualityIndex = PlayerPrefs.GetInt(SettingsKeys.GraphicsQualityKey, QualitySettings.GetQualityLevel());
        graphicsQualityDropdown.ClearOptions();
        string[] qualityLevels = QualitySettings.names;

        foreach (string qualityLevel in qualityLevels)
        {
            string translatedQualityLevel = LocalizationSettings.StringDatabase.GetLocalizedString("Quality_" + qualityLevel);
            TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(translatedQualityLevel);
            graphicsQualityDropdown.options.Add(option);
        }

        // Set the selected graphics quality
        graphicsQualityDropdown.value = currentQualityIndex;
        graphicsQualityDropdown.RefreshShownValue();
        graphicsQualityText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Quality_" + QualitySettings.names[currentQualityIndex]);
        graphicsQualityDropdown.onValueChanged.AddListener(ChangeGraphicsQuality);
    }

    public void ChangeGraphicsQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        graphicsQualityText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Quality_" + QualitySettings.names[qualityIndex]);
        PlayerPrefs.SetInt(SettingsKeys.GraphicsQualityKey, qualityIndex);
    }

    private void ConfigureOnScreenControls()
    {
        bool isOnScreenControlsEnabled = PlayerPrefs.GetInt(SettingsKeys.OnScreenControlsKey, Application.isMobilePlatform ? 1 : 0) == 1;
        OnScreenControlsToggle.isOn = isOnScreenControlsEnabled;
        OnScreenControlsToggle.onValueChanged.AddListener(OnOnScreenControlsToggleValueChanged);
        OnScreenControlsImage.sprite = isOnScreenControlsEnabled ? enabledOnScreenControls : disabledOnScreenControls;
    }

    private void ConfigureSpeedrunMode()
    {
        // Load saved setting, default to false
        bool isSpeedrunModeEnabled = PlayerPrefs.GetInt(SettingsKeys.SpeedrunModeKey, 0) == 1;
        
        // Apply the setting
        SpeedrunModeToggle.isOn = isSpeedrunModeEnabled;
        SpeedrunModeImage.sprite = isSpeedrunModeEnabled ? enabledSpeedrunMode : disabledSpeedrunMode;

        // Sync with GlobalVariables
        GlobalVariables.SpeedrunMode = isSpeedrunModeEnabled;

        // Listen for changes
        SpeedrunModeToggle.onValueChanged.AddListener(OnSpeedrunModeToggleChanged);
    }

    private void OnOnScreenControlsToggleValueChanged(bool isOnScreenControlsEnabled)
    {
        OnScreenControlsToggle.isOn = isOnScreenControlsEnabled;
        PlayerPrefs.SetInt(SettingsKeys.OnScreenControlsKey, isOnScreenControlsEnabled ? 1 : 0);
        OnScreenControlsImage.sprite = isOnScreenControlsEnabled ? enabledOnScreenControls : disabledOnScreenControls;

        GlobalVariables.OnScreenControls = isOnScreenControlsEnabled;
    }

    private void OnSpeedrunModeToggleChanged(bool isSpeedrunModeEnabled)
    {
        // Save the new setting
        PlayerPrefs.SetInt(SettingsKeys.SpeedrunModeKey, isSpeedrunModeEnabled ? 1 : 0);
        
        // Update the global variable
        GlobalVariables.SpeedrunMode = isSpeedrunModeEnabled;

        // Update UI image
        SpeedrunModeImage.sprite = isSpeedrunModeEnabled ? enabledSpeedrunMode : disabledSpeedrunMode;
    }
}