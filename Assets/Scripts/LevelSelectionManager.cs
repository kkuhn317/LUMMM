using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

public class LevelSelectionManager : MonoBehaviour
{
    public enum MarioAnimator
    {
        LevelUp,
        ItsA3,
        None,
    }

    public static LevelSelectionManager Instance { get; private set; }

    private List<AnimatorIcon> animatorIcons = new();

    public TMP_Text levelNameText;
    public TMP_Text videoYearText;
    public Button videoLinkButton;
    public TMP_Text videoLinkText;

    public GameObject LevelUpImage;
    public TMP_Text levelDescriptionText;
    public Button playButton;
    public Sprite[] GreenCoinsprite; // 0 - uncollected, 1 - collected
    public Sprite[] minirankTypes; // 0 - poison, 1 - mushroom, 2 - flower, 3 - 1up, 4 - star
    public LevelButton selectedLevelButton;

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
        playButton.gameObject.SetActive(false); // Deactivate the button if it's initially active
    }

    public static bool IsLevelPlayable(LevelButton button)
    {
        return !string.IsNullOrEmpty(button.levelInfo.levelScene) && (!button.levelInfo.beta || GlobalVariables.enableBetaMode);
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

        // Remove any existing listeners from the play button's onClick event
        videoLinkButton.onClick.RemoveAllListeners();

        var table = LocalizationSettings.StringDatabase.GetTable("Game Text");

        if (string.IsNullOrEmpty(button.levelInfo.videoLink))
        {
            videoLinkText.text = "";
        }
        else
        {
            print(table.GetEntry("WatchVideo").GetLocalizedString());
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
        print(animatorIcons.Count);
        foreach (AnimatorIcon animator in animatorIcons)
        {
            animator.gameObject.SetActive(animator.marioAnimator == button.marioAnimator);
        }

        playButton.gameObject.SetActive(true);
        playButton.onClick.RemoveAllListeners(); // Remove previous listeners if any

        if (!IsLevelPlayable(button))
        {
            playButton.gameObject.SetActive(false);
            return;
        }

    }

    // Allows you open an URL
    private void OpenVideoLink()
    {
        Application.OpenURL(selectedLevelButton.levelInfo.videoLink);
    }

    public void OnPlayButtonClick()
    {   
        if (selectedLevelButton == null)
        {
            return;
        }
        if (!IsLevelPlayable(selectedLevelButton))
        {
            return;
        }


        if (!string.IsNullOrEmpty(selectedLevelButton.levelInfo.levelScene))
        {
            DestroyGameMusic.DestroyGameMusicObjects();
            
            // Set the level info in GlobalVariables
            GlobalVariables.levelInfo = selectedLevelButton.levelInfo;
            // Reset global variables for the level
            GlobalVariables.ResetForLevel();

            // Check if level is a save game
            if (selectedLevelButton.levelInfo.levelID == PlayerPrefs.GetString("SavedLevel", "none"))
            {
                LoadSaveGame();
            } else {
                // Remove saved info
                PlayerPrefs.DeleteKey("SavedLevel");
            }

            // Open the scene
            FadeInOutScene.Instance.LoadSceneWithFade(selectedLevelButton.levelInfo.levelScene);
        }
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

        // The saved green coins will be handled by GameManager (where we need to check if it's the saved level)
    }

    // Animator Icons
    public void AddAnimatorIcon(AnimatorIcon animator)
    {
        animatorIcons.Add(animator);
    }
}
