using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;
using UnityEngine.Localization.Settings;
using System.Collections;

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

    private Resolution[] availableResolutions;
    private bool isWebGL => Application.platform == RuntimePlatform.WebGLPlayer;
    private Coroutine fullscreenCheckCoroutine;

    private void Start()
    {
        LocalizationSettings.InitializationOperation.WaitForCompletion();
        
        availableResolutions = GetResolutions();
        ConfigureResolution();
        ConfigureFullscreenToggle();
        ConfigureGraphicsQuality();
        ConfigureOnScreenControls();
        ConfigureSpeedrunMode();

        // For WebGL, start listening for fullscreen changes (ESC key detection)
        if (isWebGL)
        {
            StartFullscreenMonitoring();
            
            // Set a reasonable default window size for WebGL when not in fullscreen
            if (!Screen.fullScreen)
            {
                SetReasonableWebGLWindowSize();
            }
        }
    }

    private void OnDestroy()
    {
        // Stop the coroutine when this object is destroyed
        if (fullscreenCheckCoroutine != null)
        {
            StopCoroutine(fullscreenCheckCoroutine);
        }
    }

    private void StartFullscreenMonitoring()
    {
        if (fullscreenCheckCoroutine != null)
        {
            StopCoroutine(fullscreenCheckCoroutine);
        }
        fullscreenCheckCoroutine = StartCoroutine(CheckFullscreenChanges());
    }

    private void SetReasonableWebGLWindowSize()
    {
        // Use your canvas size (960x600) for WebGL
        int targetWidth = 960;
        int targetHeight = 600;
        
        // Only set if we're not already at the correct dimensions
        if (Screen.width != targetWidth || Screen.height != targetHeight)
        {
            Screen.SetResolution(targetWidth, targetHeight, Screen.fullScreen);
            resolutionText.text = targetWidth + "x" + targetHeight;
            PlayerPrefs.SetString(SettingsKeys.ResolutionKey, targetWidth + "x" + targetHeight);
        }
    }

    Resolution[] GetResolutions()
    {
        if (isWebGL)
        {
            // For WebGL, provide your canvas resolution as the primary option
            return new Resolution[]
            {
                new Resolution { width = 960, height = 600 }, // Your canvas size
                new Resolution { width = 1280, height = 720 },
                new Resolution { width = 1920, height = 1080 }
            };
        }
        
        Resolution[] resolutions;
        if (Application.platform == RuntimePlatform.Android)
        {
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
        if (isWebGL)
        {
            // For WebGL, use your canvas resolution as default
            string defaultResolution = "960x600";
            resolutionText.text = defaultResolution;
            resolutionDropdown.interactable = false;
            return;
        }

        string savedResolution = PlayerPrefs.GetString(SettingsKeys.ResolutionKey, Screen.currentResolution.width + "x" + Screen.currentResolution.height);
        string[] resolutionParts = savedResolution.Split('x');
        int savedWidth = int.Parse(resolutionParts[0]);
        int savedHeight = int.Parse(resolutionParts[1]);
        
        resolutionText.text = savedWidth + "x" + savedHeight;

        // Populate resolution dropdown
        resolutionDropdown.ClearOptions();
        int selectedIndex = 0;
        
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            string resText = availableResolutions[i].width + "x" + availableResolutions[i].height;
            TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(resText);
            resolutionDropdown.options.Add(option);

            if (availableResolutions[i].width == savedWidth && availableResolutions[i].height == savedHeight)
            {
                selectedIndex = i;
            }
        }

        resolutionDropdown.SetValueWithoutNotify(selectedIndex);
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(ChangeResolution);
    }

    public void ChangeResolution(int index)
    {
        if (isWebGL)
        {
            return;
        }

        if (index >= 0 && index < availableResolutions.Length)
        {
            Resolution selectedResolution = availableResolutions[index];
            
            // Get the saved fullscreen state
            bool isFullscreen = PlayerPrefs.GetInt(SettingsKeys.FullscreenKey, 1) == 1;
            
            // Apply resolution and fullscreen together
            Screen.SetResolution(selectedResolution.width, selectedResolution.height, isFullscreen);
            
            resolutionText.text = selectedResolution.width + "x" + selectedResolution.height;
            PlayerPrefs.SetString(SettingsKeys.ResolutionKey, selectedResolution.width + "x" + selectedResolution.height);
        }
    }

    private void ConfigureFullscreenToggle()
    {
        bool isFullscreen = PlayerPrefs.GetInt(SettingsKeys.FullscreenKey, 1) == 1;
        
        // For WebGL, we need to sync with the actual screen state
        if (isWebGL)
        {
            isFullscreen = Screen.fullScreen;
            PlayerPrefs.SetInt(SettingsKeys.FullscreenKey, isFullscreen ? 1 : 0);
        }
        
        fullscreenToggle.isOn = isFullscreen;
        fullscreenImage.sprite = isFullscreen ? fullscreenOnSprite : fullscreenOffSprite;
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleValueChanged);
    }

    private void OnFullscreenToggleValueChanged(bool wantFullscreen)
    {
        if (isWebGL)
        {
            HandleWebGLFullscreen(wantFullscreen);
        }
        else
        {
            HandleDesktopFullscreen(wantFullscreen);
        }
    }

    private void HandleDesktopFullscreen(bool wantFullscreen)
    {
        fullscreenImage.sprite = wantFullscreen ? fullscreenOnSprite : fullscreenOffSprite;
        
        string savedResolution = PlayerPrefs.GetString(SettingsKeys.ResolutionKey, Screen.currentResolution.width + "x" + Screen.currentResolution.height);
        string[] resolutionParts = savedResolution.Split('x');
        int width = int.Parse(resolutionParts[0]);
        int height = int.Parse(resolutionParts[1]);
        
        Screen.SetResolution(width, height, wantFullscreen);
        PlayerPrefs.SetInt(SettingsKeys.FullscreenKey, wantFullscreen ? 1 : 0);
    }

    private void HandleWebGLFullscreen(bool wantFullscreen)
    {
        bool currentlyFullscreen = Screen.fullScreen;

        if (wantFullscreen && !currentlyFullscreen)
        {
            // Entering fullscreen
            Screen.fullScreen = true;
            PlayerPrefs.SetInt(SettingsKeys.FullscreenKey, 1);
            UpdateFullscreenUI(true);
        }
        else if (!wantFullscreen && currentlyFullscreen)
        {
            // Exiting fullscreen - use coroutine for safety
            StartCoroutine(ExitFullscreenSafely());
        }
        else
        {
            UpdateFullscreenUI(currentlyFullscreen);
        }
    }

    private IEnumerator ExitFullscreenSafely()
    {
        // Exit fullscreen
        Screen.fullScreen = false;
        PlayerPrefs.SetInt(SettingsKeys.FullscreenKey, 0);
        UpdateFullscreenUI(false);
        
        // Wait for fullscreen transition
        yield return new WaitForSeconds(0.1f);
        
        // Set resolution safely
        int targetWidth = 960;
        int targetHeight = 600;
        
        if (Screen.width != targetWidth || Screen.height != targetHeight)
        {
            Screen.SetResolution(targetWidth, targetHeight, false);
            resolutionText.text = targetWidth + "x" + targetHeight;
            PlayerPrefs.SetString(SettingsKeys.ResolutionKey, targetWidth + "x" + targetHeight);
        }
        
        // Wait for resolution to apply
        yield return new WaitForEndOfFrame();
    }

    private IEnumerator SetReasonableSizeAfterFullscreen()
    {
        // Wait for fullscreen transition to complete
        yield return new WaitForSeconds(0.1f);
        
        // Use your exact canvas size for WebGL windowed mode
        int targetWidth = 960;
        int targetHeight = 600;
        
        // Only resize if we're not already at the correct size
        if (Screen.width != targetWidth || Screen.height != targetHeight)
        {
            Screen.SetResolution(targetWidth, targetHeight, false);
            resolutionText.text = targetWidth + "x" + targetHeight;
            PlayerPrefs.SetString(SettingsKeys.ResolutionKey, targetWidth + "x" + targetHeight);
        }
    }

    private void UpdateFullscreenUI(bool isFullscreen)
    {
        // Use SetIsOnWithoutNotify to update the toggle without triggering the change event
        fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);
        fullscreenImage.sprite = isFullscreen ? fullscreenOnSprite : fullscreenOffSprite;
        
        Debug.Log($"Updated fullscreen UI to: {isFullscreen}");
    }

    private IEnumerator CheckFullscreenChanges()
    {
        bool lastFullscreenState = Screen.fullScreen;
        Debug.Log("Started monitoring fullscreen changes for ESC key. Initial state: " + lastFullscreenState);
        
        while (true)
        {
            yield return new WaitForSeconds(0.2f); // Check more frequently (0.2 seconds)
            
            bool currentFullscreenState = Screen.fullScreen;
            
            if (currentFullscreenState != lastFullscreenState)
            {
                Debug.Log($"ESC KEY DETECTED: Fullscreen changed from {lastFullscreenState} to {currentFullscreenState}");
                
                // Update the UI to match the new state
                UpdateFullscreenUI(currentFullscreenState);
                PlayerPrefs.SetInt(SettingsKeys.FullscreenKey, currentFullscreenState ? 1 : 0);
                
                // If we exited fullscreen via ESC, set reasonable size
                if (!currentFullscreenState)
                {
                    Debug.Log("ESC detected - setting windowed mode with 960x600 resolution");
                    StartCoroutine(SetReasonableSizeAfterFullscreen());
                }
                
                lastFullscreenState = currentFullscreenState;
            }
        }
    }

    // ... rest of your existing methods (ConfigureGraphicsQuality, ConfigureOnScreenControls, etc.)
    private void ConfigureGraphicsQuality()
    {
        int currentQualityIndex = PlayerPrefs.GetInt(SettingsKeys.GraphicsQualityKey, QualitySettings.GetQualityLevel());
        string[] qualityLevels = QualitySettings.names;

        if (currentQualityIndex >= qualityLevels.Length)
            currentQualityIndex = 0;

        graphicsQualityDropdown.ClearOptions();

        foreach (string qualityLevel in qualityLevels)
        {
            string translatedQualityLevel = LocalizationSettings.StringDatabase.GetLocalizedString("Quality_" + qualityLevel);
            TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(translatedQualityLevel);
            graphicsQualityDropdown.options.Add(option);
        }
        
        graphicsQualityDropdown.SetValueWithoutNotify(currentQualityIndex);
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

    private void OnOnScreenControlsToggleValueChanged(bool isOnScreenControlsEnabled)
    {
        OnScreenControlsToggle.isOn = isOnScreenControlsEnabled;
        PlayerPrefs.SetInt(SettingsKeys.OnScreenControlsKey, isOnScreenControlsEnabled ? 1 : 0);
        OnScreenControlsImage.sprite = isOnScreenControlsEnabled ? enabledOnScreenControls : disabledOnScreenControls;

        GlobalVariables.OnScreenControls = isOnScreenControlsEnabled;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateMobileControls();
        }
    }

    private void ConfigureSpeedrunMode()
    {
        bool isSpeedrunModeEnabled = PlayerPrefs.GetInt(SettingsKeys.SpeedrunModeKey, 0) == 1;
        
        SpeedrunModeToggle.isOn = isSpeedrunModeEnabled;
        SpeedrunModeImage.sprite = isSpeedrunModeEnabled ? enabledSpeedrunMode : disabledSpeedrunMode;

        SpeedrunModeToggle.onValueChanged.AddListener(OnSpeedrunModeToggleChanged);
    }

    private void OnSpeedrunModeToggleChanged(bool isSpeedrunModeEnabled)
    {
        PlayerPrefs.SetInt(SettingsKeys.SpeedrunModeKey, isSpeedrunModeEnabled ? 1 : 0);
        GlobalVariables.SpeedrunMode = isSpeedrunModeEnabled;
        SpeedrunModeImage.sprite = isSpeedrunModeEnabled ? enabledSpeedrunMode : disabledSpeedrunMode;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateSpeedrunTimerVisiblity();
        }
    }
}