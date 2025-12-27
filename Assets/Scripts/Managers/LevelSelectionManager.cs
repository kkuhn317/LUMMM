using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Globalization;

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
        playButton.gameObject.SetActive(false); // Deactivate the button if it's initially active
        // Set the enabled state of the videoLinkButton based on persistent event listeners
        videoLinkButton.enabled = videoLinkButton.onClick.GetPersistentEventCount() > 0;
        if (bestTimeText != null)
            bestTimeText.text = "--:--.--";
    }

    public static bool IsLevelPlayable(LevelButton button)
    {
        return !string.IsNullOrEmpty(button.levelInfo.levelScene) && (!button.levelInfo.beta || GlobalVariables.cheatBetaMode);
    }

    public void OnLevelButtonClick(LevelButton button)
    {
        if (selectedLevelButton != null)
        {
            // Deactivate selectionMark of the previously selected button
            selectedLevelButton.selectionMark.SetActive(false);
        }

        selectedLevelButton = button;

        selectedLevelButton.selectionMark.SetActive(true);

        // Update the level info text
        levelNameText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Level_" + button.levelInfo.levelID);
        videoYearText.text = button.levelInfo.videoYear;
        levelDescriptionText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Desc_" + button.levelInfo.levelID);
        
        // Update best time text
        if (bestTimeText != null)
        {
            string key = $"BestTimeMs_{button.levelInfo.levelID}";
            string msStr = PlayerPrefs.GetString(key, "");

            if (double.TryParse(msStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double ms) && ms > 0)
            {
                var ts = TimeSpan.FromMilliseconds(ms);
                bestTimeText.text = ts.ToString(@"m\:ss\.ff");
            }
            else
            {
                bestTimeText.text = "--:--.--";
            }
        }

        // Remove any existing listeners from the play button's onClick event
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
            print(table.GetEntry("WatchVideo").GetLocalizedString());
            videoLinkText.text = table.GetEntry("WatchVideo").GetLocalizedString();
            videoLinkButton.onClick.AddListener(OpenVideoLink);
        }

        // Set custom video link text if available
        var videoLinkTextEntry = table.GetEntry("VideoLinkText_" + button.levelInfo.levelID);
        string customVideoLinkText = videoLinkTextEntry != null ? videoLinkTextEntry.GetLocalizedString() : null;
        print("custom video text: " + customVideoLinkText);
        if (!string.IsNullOrEmpty(customVideoLinkText))
        {
            videoLinkText.text = customVideoLinkText;
        }

        // Enable the correct animator icon
        print(animatorIcons.Count);
        foreach (AnimatorIcon animator in animatorIcons)
        {
            animator.gameObject.SetActive(animator.marioAnimator == button.marioAnimator);
        }

        playButton.gameObject.SetActive(true);
        playButton.onClick.RemoveAllListeners(); // Remove previous listeners if any

        playButton.gameObject.SetActive(true);

        if (!IsLevelPlayable(button))
        {
            playButton.interactable = false;
            gameObject.SetActive(false);
            return;
        }

        playButton.interactable = true;

        onValidLevelSelected?.Invoke();
    }

    // Allows you open an URL
    private void OpenVideoLink()
    {
        Application.OpenURL(selectedLevelButton.levelInfo.videoLink);
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
        // Load the saved info
        GlobalVariables.lives = PlayerPrefs.GetInt("SavedLives", 3);
        GlobalVariables.coinCount = PlayerPrefs.GetInt("SavedCoins", 0);
        if (GlobalVariables.enableCheckpoints) {
            GlobalVariables.checkpoint = PlayerPrefs.GetInt("SavedCheckpoint", -1);
        } else {
            GlobalVariables.checkpoint = -1;
        }
        GlobalVariables.SetTimerOffsetFromString(PlayerPrefs.GetString("SavedSpeedrunTime"));

        // The saved green coins will be handled by GameManager (where we need to check if it's the saved level)
    }

    private void StartLevel()
    {
        if (string.IsNullOrEmpty(selectedLevelButton.levelInfo.levelScene))
        {
            return;
        }

        DestroyGameMusic.DestroyGameMusicObjects();
        // Reset global variables for the level
        GlobalVariables.levelInfo = selectedLevelButton.levelInfo;
        GlobalVariables.ResetForLevel();

        // Check if level is a save game
        if (selectedLevelButton.levelInfo.levelID == PlayerPrefs.GetString("SavedLevel", "none"))
        {
            LoadSaveGame();
        } else {
            // Remove saved info if it's not the saved level
            PlayerPrefs.DeleteKey("SavedLevel");
        }

        // Load the scene
        FadeInOutScene.Instance.LoadSceneWithFade("LevelIntro");
    }

    // Animator Icons
    public void AddAnimatorIcon(AnimatorIcon animator)
    {
        animatorIcons.Add(animator);
    }

    public void RefreshCheckpointFlags()
    {
        foreach (var button in levelButtons)
        {
            button.UpdateCheckpointFlag();
        }
    }
}
