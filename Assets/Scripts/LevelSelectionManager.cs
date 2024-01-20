using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectionManager : MonoBehaviour
{
    public static LevelSelectionManager Instance { get; private set; }

    public TMP_Text levelNameText;
    public TMP_Text videoYearText;
    public Button videoLinkButton;
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
        levelNameText.text = button.levelInfo.levelName;
        videoYearText.text = button.levelInfo.videoYear;
        levelDescriptionText.text = button.levelInfo.levelDescription;

        // Remove any existing listeners from the play button's onClick event
        videoLinkButton.onClick.RemoveAllListeners();

        videoLinkButton.onClick.AddListener(OpenVideoLink);

        playButton.gameObject.SetActive(true);
        playButton.onClick.RemoveAllListeners(); // Remove previous listeners if any

        if (string.IsNullOrEmpty(button.levelInfo.levelScene))
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
        GlobalVariables.checkpoint = PlayerPrefs.GetInt("SavedCheckpoint", -1);

        // The saved green coins will be handled by GameManager (where we need to check if it's the saved level)
    }
}
