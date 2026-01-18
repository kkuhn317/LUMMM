using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Globalization;
using UnityEngine.SceneManagement;

public class LevelSelectionManager : MonoBehaviour
{
    public enum MarioAnimator
    {
        LevelUp,
        ItsA3,
        None,
        TeamNellyAqua,
        Pixelcraftian
    }

    public enum MarioLiveCounter
    {
        Normal,
        Tiny,
        NES,
        None,
    }

    public static LevelSelectionManager Instance { get; private set; }

    private List<AnimatorIcon> animatorIcons = new();

    public TMP_Text levelNameText;
    public TMP_Text videoYearText;
    public Button videoLinkButton;
    public TMP_Text videoLinkText;
    public TMP_Text bestTimeText;
    public GameObject LevelUpImage;
    public TMP_Text levelDescriptionText;
    public Button playButton;
    public List<LevelButton> levelButtons = new List<LevelButton>();
    public Sprite[] GreenCoinsprite; // 0 - uncollected, 1 - collected
    public Sprite[] minirankTypes;   // 0 - poison, 1 - mushroom, 2 - flower, 3 - 1up, 4 - star
    public LevelButton selectedLevelButton;

    [Header("Events")]
    public UnityEvent onSceneStart;
    public UnityEvent onValidLevelSelected;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        onSceneStart?.Invoke();
        playButton.interactable = false;
        playButton.gameObject.SetActive(false);

        // Make sure the SaveLoadSystem singleton exists
        if (SaveLoadSystem.Instance == null)
        {
            Debug.LogError("SaveLoadSystem not found! Make sure it's in the scene.");
        }

        // Enable or disable the video link button based on persistent listeners
        videoLinkButton.enabled = videoLinkButton.onClick.GetPersistentEventCount() > 0;

        if (bestTimeText != null)
            bestTimeText.text = "--:--.--";
    }

    public static bool IsLevelPlayable(LevelButton button)
    {
        if (button == null || button.levelInfo == null) return false;
        return !string.IsNullOrEmpty(button.levelInfo.levelScene) &&
               (!button.levelInfo.beta || GlobalVariables.cheatBetaMode);
    }

    public void OnLevelButtonClick(LevelButton button)
    {
        if (selectedLevelButton != null && selectedLevelButton != button)
        {
            selectedLevelButton.selectionMark.SetActive(false);
        }

        selectedLevelButton = button;

        if (selectedLevelButton.selectionMark != null)
            selectedLevelButton.selectionMark.SetActive(true);

        // Level info text
        levelNameText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Level_" + button.levelInfo.levelID);
        videoYearText.text = button.levelInfo.videoYear;
        levelDescriptionText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Desc_" + button.levelInfo.levelID);

        // Best time text using SaveData first, PlayerPrefs as fallback
        if (bestTimeText != null)
        {
            double bestMs = GetBestTimeMs(button.levelInfo.levelID);

            if (bestMs > 0)
            {
                var ts = TimeSpan.FromMilliseconds(bestMs);
                bestTimeText.text = ts.ToString(@"m\:ss\.ff");
            }
            else
            {
                bestTimeText.text = "--:--.--";
            }
        }

        // Video link setup
        videoLinkButton.onClick.RemoveAllListeners();

        var table = LocalizationSettings.StringDatabase.GetTable("Game Text");

        if (string.IsNullOrEmpty(button.levelInfo.videoLink))
        {
            videoLinkText.text = "";
            videoLinkButton.enabled = false;
        }
        else
        {
            videoLinkButton.enabled = true;
            videoLinkText.text = table.GetEntry("WatchVideo").GetLocalizedString();
            videoLinkButton.onClick.AddListener(OpenVideoLink);
        }

        // Optional custom text for the video link
        var videoLinkTextEntry = table.GetEntry("VideoLinkText_" + button.levelInfo.levelID);
        string customVideoLinkText = videoLinkTextEntry != null ? videoLinkTextEntry.GetLocalizedString() : null;
        if (!string.IsNullOrEmpty(customVideoLinkText))
        {
            videoLinkText.text = customVideoLinkText;
        }

        // Animator icon selection
        foreach (AnimatorIcon animator in animatorIcons)
        {
            if (animator != null)
                animator.gameObject.SetActive(animator.marioAnimator == button.marioAnimator);
        }

        playButton.gameObject.SetActive(true);
        playButton.onClick.RemoveAllListeners();
        playButton.onClick.AddListener(OnPlayButtonClick);

        if (!IsLevelPlayable(button))
        {
            playButton.interactable = false;
            return;
        }

        playButton.interactable = true;
        onValidLevelSelected?.Invoke();
    }

    private void OpenVideoLink()
    {
        if (selectedLevelButton != null && !string.IsNullOrEmpty(selectedLevelButton.levelInfo.videoLink))
        {
            Application.OpenURL(selectedLevelButton.levelInfo.videoLink);
        }
    }

    public void OnPlayButtonClick()
    {
        if (selectedLevelButton == null || !IsLevelPlayable(selectedLevelButton))
        {
            return;
        }

        StartLevel();
    }

    /// <summary>
    /// Returns the best time in milliseconds for a level.
    /// Uses SaveData first, then falls back to the legacy PlayerPrefs key "BestTimeMs_<levelID>".
    /// </summary>
    private double GetBestTimeMs(string levelID)
    {
        double bestMs = 0;

        // New save system first
        var progress = SaveLoadSystem.Instance?.GetLevelProgress(levelID);
        if (progress != null && progress.bestTimeMs > 0)
        {
            bestMs = progress.bestTimeMs;
        }

        // Legacy PlayerPrefs fallback
        if (bestMs <= 0)
        {
            string key = $"BestTimeMs_{levelID}";
            string msStr = PlayerPrefs.GetString(key, "");

            if (double.TryParse(msStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double legacyMs) && legacyMs > 0)
            {
                bestMs = legacyMs;
            }
        }

        return bestMs;
    }

    /// <summary>
    /// Loads checkpoint data from the new SaveManager system for this level.
    /// Returns true if a valid checkpoint was loaded.
    /// </summary>
    private bool TryLoadCheckpointFromSaveManager(string levelID)
    {
        var checkpoint = SaveManager.Current.checkpoint;

        if (!checkpoint.hasCheckpoint || checkpoint.levelID != levelID)
            return false;

        GlobalVariables.lives = checkpoint.lives;
        GlobalVariables.coinCount = checkpoint.coins;
        GlobalVariables.checkpoint = checkpoint.checkpointId;

        if (checkpoint.speedrunMs > 0)
        {
            // Basic restoration for the timer; the exact elapsed value handling can be refined later.
            GlobalVariables.speedrunTimer = new System.Diagnostics.Stopwatch();
            GlobalVariables.speedrunTimer.Start();
        }

        Debug.Log($"Loading checkpoint from SaveData for level {levelID}, checkpoint {checkpoint.checkpointId}");
        return true;
    }

    /// <summary>
    /// Loads checkpoint data from the legacy PlayerPrefs keys if they belong to this level.
    /// Returns true if a valid legacy save was loaded.
    /// </summary>
    private bool TryLoadCheckpointFromPlayerPrefs(string levelID)
    {
        string savedLevel = PlayerPrefs.GetString("SavedLevel", "none");
        if (savedLevel != levelID)
        {
            return false;
        }

        // Lives and coins from legacy save
        GlobalVariables.lives = PlayerPrefs.GetInt("SavedLives", selectedLevelButton.levelInfo.lives);
        GlobalVariables.coinCount = PlayerPrefs.GetInt("SavedCoins", 0);

        // Checkpoints depend on the current checkpoint mode
        if (GlobalVariables.enableCheckpoints)
        {
            GlobalVariables.checkpoint = PlayerPrefs.GetInt("SavedCheckpoint", -1);
        }
        else
        {
            GlobalVariables.checkpoint = -1;
        }

        // Restore speedrun timer offset if it exists
        string savedTime = PlayerPrefs.GetString("SavedSpeedrunTime", string.Empty);
        if (!string.IsNullOrEmpty(savedTime))
        {
            GlobalVariables.SetTimerOffsetFromString(savedTime);
        }

        Debug.Log($"Loading LEGACY checkpoint from PlayerPrefs for level {levelID}");
        return true;
    }

    private void StartLevel()
    {
        if (selectedLevelButton == null ||
            string.IsNullOrEmpty(selectedLevelButton.levelInfo.levelScene))
        {
            Debug.LogError("Cannot start level: No level selected or no scene specified");
            return;
        }

        // Destroy any existing game music
        DestroyGameMusic.DestroyGameMusicObjects();

        // Reset global variables for the level
        GlobalVariables.levelInfo = selectedLevelButton.levelInfo;
        GlobalVariables.ResetForLevel();

        string levelID = selectedLevelButton.levelInfo.levelID;
        bool loadedCheckpoint = false;

        // First try checkpoint from the new save system
        if (SaveManager.HasCheckpointForLevel(levelID))
        {
            loadedCheckpoint = TryLoadCheckpointFromSaveManager(levelID);
        }

        // If there is no save-data checkpoint, try legacy PlayerPrefs
        if (!loadedCheckpoint)
        {
            loadedCheckpoint = TryLoadCheckpointFromPlayerPrefs(levelID);
        }

        // If nothing could be loaded, start fresh
        if (!loadedCheckpoint)
        {
            GlobalVariables.lives = selectedLevelButton.levelInfo.lives;
            GlobalVariables.coinCount = 0;
            GlobalVariables.checkpoint = -1;
        }

        // Update gameplay modifiers from SaveData (new system)
        var modifiers = SaveManager.Current.modifiers;
        if (modifiers != null)
        {
            GlobalVariables.infiniteLivesMode = modifiers.infiniteLivesEnabled;
            GlobalVariables.stopTimeLimit = modifiers.timeLimitEnabled;
            GlobalVariables.enableCheckpoints = modifiers.checkpointMode != 0;
            GlobalVariables.checkpointMode = modifiers.checkpointMode;
        }

        // Load the scene
        if (FadeInOutScene.Instance != null)
        {
            FadeInOutScene.Instance.LoadSceneWithFade("LevelIntro");
        }
        else
        {
            SceneManager.LoadScene("LevelIntro");
        }
    }

    // Animator Icons support
    public void AddAnimatorIcon(AnimatorIcon animator)
    {
        if (animator != null && !animatorIcons.Contains(animator))
        {
            animatorIcons.Add(animator);
        }
    }

    public void RefreshCheckpointFlags()
    {
        foreach (var button in levelButtons)
        {
            if (button != null)
                button.UpdateCheckpointFlag();
        }
    }

    // Refresh all level buttons UI
    public void RefreshAllLevelButtons()
    {
        foreach (var button in levelButtons)
        {
            if (button != null)
                button.RefreshUI();
        }
    }
}