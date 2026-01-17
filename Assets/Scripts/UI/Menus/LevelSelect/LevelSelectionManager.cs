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
    public Sprite[] minirankTypes; // 0 - poison, 1 - mushroom, 2 - flower, 3 - 1up, 4 - star
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
        
        // Ensure SaveLoadSystem is initialized
        if (SaveLoadSystem.Instance == null)
        {
            Debug.LogError("SaveLoadSystem not found! Make sure it's in the scene.");
        }
        
        // Initialize video link button
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

        // Update the level info text
        levelNameText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Level_" + button.levelInfo.levelID);
        videoYearText.text = button.levelInfo.videoYear;
        levelDescriptionText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Desc_" + button.levelInfo.levelID);
        
        // Update best time text from SaveData
        if (bestTimeText != null)
        {
            var progress = SaveLoadSystem.Instance?.GetLevelProgress(button.levelInfo.levelID);
            if (progress != null && progress.bestTimeMs > 0)
            {
                var ts = TimeSpan.FromMilliseconds(progress.bestTimeMs);
                bestTimeText.text = ts.ToString(@"m\:ss\.ff");
            }
            else
            {
                bestTimeText.text = "--:--.--";
            }
        }

        // Remove any existing listeners from the video link button
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

        // Set custom video link text if available
        var videoLinkTextEntry = table.GetEntry("VideoLinkText_" + button.levelInfo.levelID);
        string customVideoLinkText = videoLinkTextEntry != null ? videoLinkTextEntry.GetLocalizedString() : null;
        if (!string.IsNullOrEmpty(customVideoLinkText))
        {
            videoLinkText.text = customVideoLinkText;
        }

        // Enable the correct animator icon
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

    private void LoadSaveGame()
    {
        if (selectedLevelButton == null) return;
        
        string levelID = selectedLevelButton.levelInfo.levelID;
        var checkpoint = SaveManager.Current.checkpoint;
        
        // Check if we have a checkpoint for this level
        if (checkpoint.hasCheckpoint && checkpoint.levelID == levelID)
        {
            // Load from checkpoint data
            GlobalVariables.lives = checkpoint.coins; // Note: checkpoint.coins might be misnamed
            GlobalVariables.coinCount = checkpoint.coins;
            GlobalVariables.checkpoint = checkpoint.checkpointId;
            
            // Convert speedrunMs back to TimeSpan
            if (checkpoint.speedrunMs > 0)
            {
                GlobalVariables.speedrunTimer = new System.Diagnostics.Stopwatch();
                GlobalVariables.speedrunTimer.Start();
                // We need to set elapsed time - this is tricky with Stopwatch
                // Might need to store start time instead
            }
            
            // Green coins from checkpoint will be loaded by GameManager
            Debug.Log($"Loading checkpoint for level {levelID}, checkpoint {checkpoint.checkpointId}");
        }
        else
        {
            // Fresh start
            GlobalVariables.lives = selectedLevelButton.levelInfo.lives;
            GlobalVariables.coinCount = 0;
            GlobalVariables.checkpoint = -1;
            GlobalVariables.speedrunTimer = new System.Diagnostics.Stopwatch();
        }
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

        // Check if level has a checkpoint in SaveData
        bool hasCheckpoint = SaveManager.HasCheckpointForLevel(selectedLevelButton.levelInfo.levelID);
        
        if (hasCheckpoint)
        {
            LoadSaveGame();
        }
        else
        {
            // Fresh start
            GlobalVariables.lives = selectedLevelButton.levelInfo.lives;
            GlobalVariables.coinCount = 0;
            GlobalVariables.checkpoint = -1;
        }

        // Update modifiers from SaveData
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

    // Animator Icons
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