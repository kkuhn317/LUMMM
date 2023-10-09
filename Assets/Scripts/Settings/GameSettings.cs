using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameSettings : MonoBehaviour
{
    [Header("UI")]
    public Toggle fullscreenToggle;
    public Toggle InfiniteLivesToggle;
    public Toggle CheckpointsToggle;
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

    private const string FullscreenPlayerPrefsKey = "Fullscreen";
    private const string ResolutionPlayerPrefsKey = "Resolution";
    private const string GraphicsQualityPlayerPrefsKey = "GraphicsQuality";
    private const string InfiniteLivesPlayerPrefsKey = "InfiniteLives";
    private const string CheckpointsPlayerPrefsKey = "Checkpoints";

    private void Start()
    {
        ConfigureResolution();
        ConfigureFullscreenToggle();
        ConfigureGraphicsQuality();
        ConfigureInfiniteLives();
        ConfigureCheckpoints();
    }

    private void ConfigureResolution()
    {
        // Get current screen resolution from PlayerPrefs or use the current screen resolution
        string savedResolution = PlayerPrefs.GetString(ResolutionPlayerPrefsKey, Screen.currentResolution.width + "x" + Screen.currentResolution.height);
        string[] resolutionParts = savedResolution.Split('x');
        int savedWidth = int.Parse(resolutionParts[0]);
        int savedHeight = int.Parse(resolutionParts[1]);
        Resolution currentResolution = new Resolution { width = savedWidth, height = savedHeight };
        resolutionText.text = currentResolution.width + "x" + currentResolution.height;

        // Get all available resolutions
        Resolution[] availableResolutions = Screen.resolutions;

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
    }

    private void ConfigureFullscreenToggle()
    {
        bool isFullscreen = PlayerPrefs.GetInt(FullscreenPlayerPrefsKey, 1) == 1;
        fullscreenToggle.isOn = isFullscreen;
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleValueChanged);
    }

    private void OnFullscreenToggleValueChanged(bool isFullscreen)
    {
        fullscreenImage.sprite = isFullscreen ? fullscreenOnSprite : fullscreenOffSprite;
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FullscreenPlayerPrefsKey, isFullscreen ? 1 : 0);
    }

    private void ConfigureGraphicsQuality()
    {
        int currentQualityIndex = PlayerPrefs.GetInt(GraphicsQualityPlayerPrefsKey, QualitySettings.GetQualityLevel());
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
    }

    public void ChangeGraphicsQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        graphicsQualityText.text = QualitySettings.names[qualityIndex];
        PlayerPrefs.SetInt(GraphicsQualityPlayerPrefsKey, qualityIndex);
    }

    public void ChangeResolution(int index)
    {
        Resolution[] availableResolutions = Screen.resolutions;

        if (index >= 0 && index < availableResolutions.Length)
        {
            Resolution selectedResolution = availableResolutions[index];
            Screen.SetResolution(selectedResolution.width, selectedResolution.height, Screen.fullScreen);
            resolutionText.text = selectedResolution.width + "x" + selectedResolution.height;
            PlayerPrefs.SetString(ResolutionPlayerPrefsKey, selectedResolution.width + "x" + selectedResolution.height);
        }
    }

    private void ConfigureInfiniteLives()
    {
        bool isInfiniteLivesMode = PlayerPrefs.GetInt(InfiniteLivesPlayerPrefsKey, 0) == 1;
        InfiniteLivesToggle.isOn = isInfiniteLivesMode;
        InfiniteLivesToggle.onValueChanged.AddListener(OnInfiniteLivesToggleValueChanged);
    }

    private void ConfigureCheckpoints()
    {
        bool isCheckpointAllowed = PlayerPrefs.GetInt(CheckpointsPlayerPrefsKey, 0) == 1;
        CheckpointsToggle.isOn = isCheckpointAllowed;
        CheckpointsToggle.onValueChanged.AddListener(OnCheckpointsToggleValueChanged);
    }

    private void OnInfiniteLivesToggleValueChanged(bool isInfiniteLivesMode)
    {
        InfiniteLivesToggle.isOn = isInfiniteLivesMode;
        InfiniteLivesMode.sprite = isInfiniteLivesMode ? enableInfiniteLivesMode : disabledInfiniteLivesMode;
        PlayerPrefs.SetInt(InfiniteLivesPlayerPrefsKey, isInfiniteLivesMode ? 1 : 0);
    }

    private void OnCheckpointsToggleValueChanged(bool isCheckpointAllowed)
    {
        CheckpointsToggle.isOn = isCheckpointAllowed;
        CheckpointsAllowed.sprite = isCheckpointAllowed ? enableCheckpoints : disabledCheckpoints;
        PlayerPrefs.SetInt(CheckpointsPlayerPrefsKey, isCheckpointAllowed ? 1 : 0);
    }
}
