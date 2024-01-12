using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;

public class GameSettings : MonoBehaviour
{
    [Header("UI")]
    public Toggle fullscreenToggle;
    public Toggle InfiniteLivesToggle;
    public Toggle CheckpointsToggle;
    public Toggle OnScreenControlsToggle;
    public TMP_Dropdown resolutionDropdown;
    public TMP_Text resolutionText;
    public TMP_Dropdown graphicsQualityDropdown;
    public TMP_Text graphicsQualityText;   

    [Header("Fullscreen")]
    public Image fullscreenImage;
    public Sprite fullscreenOnSprite;
    public Sprite fullscreenOffSprite;

    [Header("Gameplay")]
    public Image InfiniteLivesMode;
    public Image CheckpointsAllowed;
    public Sprite enableInfiniteLivesMode;
    public Sprite disabledInfiniteLivesMode;
    public Sprite enableCheckpoints;
    public Sprite disabledCheckpoints;

    private void Start()
    {
        ConfigureResolution();
        ConfigureFullscreenToggle();
        ConfigureGraphicsQuality();
        ConfigureInfiniteLives();
        ConfigureCheckpoints();
        ConfigureOnScreenControls();
    }

    Resolution[] GetResolutions()
    {
        Resolution[] resolutions = Screen.resolutions.Select(resolution => new Resolution { width = resolution.width, height = resolution.height }).Distinct().ToArray();
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
            TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(qualityLevel);
            graphicsQualityDropdown.options.Add(option);
        }

        // Set the selected graphics quality
        graphicsQualityDropdown.value = currentQualityIndex;
        graphicsQualityDropdown.RefreshShownValue();
        graphicsQualityText.text = QualitySettings.names[currentQualityIndex];
        graphicsQualityDropdown.onValueChanged.AddListener(ChangeGraphicsQuality);
    }

    public void ChangeGraphicsQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        graphicsQualityText.text = QualitySettings.names[qualityIndex];
        PlayerPrefs.SetInt(SettingsKeys.GraphicsQualityKey, qualityIndex);
    }

    private void ConfigureInfiniteLives()
    {
        bool isInfiniteLivesMode = PlayerPrefs.GetInt(SettingsKeys.InfiniteLivesKey, 0) == 1;
        InfiniteLivesToggle.isOn = isInfiniteLivesMode;
        InfiniteLivesMode.sprite = isInfiniteLivesMode ? enableInfiniteLivesMode : disabledInfiniteLivesMode;
        InfiniteLivesToggle.onValueChanged.AddListener(OnInfiniteLivesToggleValueChanged);
    }

    private void OnInfiniteLivesToggleValueChanged(bool isInfiniteLivesMode)
    {
        InfiniteLivesToggle.isOn = isInfiniteLivesMode;
        InfiniteLivesMode.sprite = isInfiniteLivesMode ? enableInfiniteLivesMode : disabledInfiniteLivesMode;
        PlayerPrefs.SetInt(SettingsKeys.InfiniteLivesKey, isInfiniteLivesMode ? 1 : 0);

        GlobalVariables.infiniteLivesMode = isInfiniteLivesMode;
    }

    private void ConfigureCheckpoints()
    {
        bool isCheckpointAllowed = PlayerPrefs.GetInt(SettingsKeys.CheckpointsKey, 0) == 1;
        CheckpointsToggle.isOn = isCheckpointAllowed;
        CheckpointsAllowed.sprite = isCheckpointAllowed ? enableCheckpoints : disabledCheckpoints;
        CheckpointsToggle.onValueChanged.AddListener(OnCheckpointsToggleValueChanged);
    }

    private void OnCheckpointsToggleValueChanged(bool isCheckpointAllowed)
    {
        CheckpointsToggle.isOn = isCheckpointAllowed;
        CheckpointsAllowed.sprite = isCheckpointAllowed ? enableCheckpoints : disabledCheckpoints;
        PlayerPrefs.SetInt(SettingsKeys.CheckpointsKey, isCheckpointAllowed ? 1 : 0);

        GlobalVariables.enableCheckpoints = isCheckpointAllowed;
    }

    private void ConfigureOnScreenControls()
    {
        bool isOnScreenControlsEnabled = PlayerPrefs.GetInt(SettingsKeys.OnScreenControlsKey, Application.isMobilePlatform ? 1 : 0) == 1;
        OnScreenControlsToggle.isOn = isOnScreenControlsEnabled;
        OnScreenControlsToggle.onValueChanged.AddListener(OnOnScreenControlsToggleValueChanged);
    }

    private void OnOnScreenControlsToggleValueChanged(bool isOnScreenControlsEnabled)
    {
        OnScreenControlsToggle.isOn = isOnScreenControlsEnabled;
        PlayerPrefs.SetInt(SettingsKeys.OnScreenControlsKey, isOnScreenControlsEnabled ? 1 : 0);
    }
}
