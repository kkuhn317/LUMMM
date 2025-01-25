using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// This script manages the modifiers in the Level Select screen (infinite lives, checkpoints, no time limit)
// Additionally, it initializes the GlobalVariables with the current settings from PlayerPrefs
// So make sure the player goes to the Level Select screen before entering a level
public class ModifiersSettings : MonoBehaviour
{
    [Header("Gameplay Toggles")]
    public Toggle infiniteLivesToggle;
    public Toggle checkpointsToggle;
    public Toggle timeLimitToggle;

    [Header("Toggle Sprites")]
    public Image infiniteLivesImage;
    public Image checkpointsImage;
    public Image timeLimitImage;
    public Sprite enableInfiniteLivesSprite;
    public Sprite disableInfiniteLivesSprite;
    public Sprite enableCheckpointsSprite;
    public Sprite disableCheckpointsSprite;
    public Sprite enableTimeLimitSprite;
    public Sprite disableTimeLimitSprite;

    [Header("Decision Window")]
    public GameObject decisionWindow; // Window to confirm deletion of the checkpoint
    public Button confirmDeleteButton;
    public Button cancelDeleteButton;
    public CanvasGroup mainCanvasGroup; // CanvasGroup for the main UI
    public CanvasGroup decisionCanvasGroup; // CanvasGroup for the decision window

    private GameObject previouslySelected; // To track the previously selected UI element
    private string bufferedModifierKey; // The key of the modifier that the player wants to change (while the decision window is open)
    private bool bufferedModifierValue; // The value of the modifier that the player wants to change (while the decision window is open)

    private void Start()
    {
        ConfigureInfiniteLives();
        ConfigureCheckpoints();
        ConfigureTimeLimit();

        // Set up the buttons for the decision window
        confirmDeleteButton.onClick.AddListener(OnConfirmDeleteCheckpoint);
        cancelDeleteButton.onClick.AddListener(OnCancelDeleteCheckpoint);
    }

    /* INFINITE LIVES */
    private void ConfigureInfiniteLives()
    {
        bool isInfiniteLivesEnabled = PlayerPrefs.GetInt(SettingsKeys.InfiniteLivesKey, 0) == 1;
        infiniteLivesToggle.isOn = isInfiniteLivesEnabled;
        infiniteLivesImage.sprite = isInfiniteLivesEnabled ? enableInfiniteLivesSprite : disableInfiniteLivesSprite;
        GlobalVariables.infiniteLivesMode = isInfiniteLivesEnabled; // Initialize the GlobalVariables with the current setting
        infiniteLivesToggle.onValueChanged.AddListener(OnInfiniteLivesClick);
    }

    private void OnInfiniteLivesClick(bool isEnabled)
    {
        if (isSaveGameAvailable())
        {
            bufferedModifierKey = SettingsKeys.InfiniteLivesKey;
            bufferedModifierValue = isEnabled;
            ActivateDecisionWindow();
        }
        else
        {
            ChangeInfiniteLives(isEnabled);
        }
    }

    private void ChangeInfiniteLives(bool isEnabled)
    {
        infiniteLivesImage.sprite = isEnabled ? enableInfiniteLivesSprite : disableInfiniteLivesSprite;
        PlayerPrefs.SetInt(SettingsKeys.InfiniteLivesKey, isEnabled ? 1 : 0);
        GlobalVariables.infiniteLivesMode = isEnabled;
    }

    /* CHECKPOINTS */
    private void ConfigureCheckpoints()
    {
        bool areCheckpointsEnabled = PlayerPrefs.GetInt(SettingsKeys.CheckpointsKey, 0) == 1;
        checkpointsToggle.isOn = areCheckpointsEnabled;
        checkpointsImage.sprite = areCheckpointsEnabled ? enableCheckpointsSprite : disableCheckpointsSprite;
        GlobalVariables.enableCheckpoints = areCheckpointsEnabled; // Initialize the GlobalVariables with the current setting
        checkpointsToggle.onValueChanged.AddListener(OnCheckpointsClick);
    }

    private void OnCheckpointsClick(bool isEnabled)
    {
        if (isSaveGameAvailable())
        {
            bufferedModifierKey = SettingsKeys.CheckpointsKey;
            bufferedModifierValue = isEnabled;
            ActivateDecisionWindow();
        }
        else
        {
            ChangeCheckpoints(isEnabled);
        }
    }

    private void ChangeCheckpoints(bool isEnabled)
    {
        checkpointsImage.sprite = isEnabled ? enableCheckpointsSprite : disableCheckpointsSprite;
        PlayerPrefs.SetInt(SettingsKeys.CheckpointsKey, isEnabled ? 1 : 0);
        GlobalVariables.enableCheckpoints = isEnabled;
    }

    /* TIME LIMIT */
    private void ConfigureTimeLimit()
    {
        bool isTimeLimitEnabled = PlayerPrefs.GetInt(SettingsKeys.TimeLimitKey, 0) == 1;
        timeLimitToggle.isOn = isTimeLimitEnabled;
        timeLimitImage.sprite = isTimeLimitEnabled ? enableTimeLimitSprite : disableTimeLimitSprite;
        GlobalVariables.stopTimeLimit = isTimeLimitEnabled; // Initialize the GlobalVariables with the current setting
        timeLimitToggle.onValueChanged.AddListener(OnTimeLimitClick);
    }

    private void OnTimeLimitClick(bool isEnabled)
    {
        if (isSaveGameAvailable())
        {
            bufferedModifierKey = SettingsKeys.TimeLimitKey;
            bufferedModifierValue = isEnabled;
            ActivateDecisionWindow();
        }
        else
        {
            ChangeTimeLimit(isEnabled);
        }
    }

    private void ChangeTimeLimit(bool isEnabled)
    {
        timeLimitImage.sprite = isEnabled ? enableTimeLimitSprite : disableTimeLimitSprite;
        PlayerPrefs.SetInt(SettingsKeys.TimeLimitKey, isEnabled ? 1 : 0);
        GlobalVariables.stopTimeLimit = isEnabled;
    }

    /* SAVE GAME */
    private bool isSaveGameAvailable()
    {
        return PlayerPrefs.HasKey("SavedLevel");
    }

    private void ActivateDecisionWindow()
    {
        // Track the currently selected UI element
        previouslySelected = EventSystem.current.currentSelectedGameObject;

        // Disable main UI
        mainCanvasGroup.interactable = false;
        mainCanvasGroup.blocksRaycasts = false;

        // Enable decision window
        decisionCanvasGroup.interactable = true;
        decisionCanvasGroup.blocksRaycasts = true;
        decisionWindow.SetActive(true);

        // Select the confirm button
        EventSystem.current.SetSelectedGameObject(confirmDeleteButton.gameObject);
    }

    private void DeactivateDecisionWindow()
    {
        // Enable main UI
        mainCanvasGroup.interactable = true;
        mainCanvasGroup.blocksRaycasts = true;

        // Disable decision window
        decisionCanvasGroup.interactable = false;
        decisionCanvasGroup.blocksRaycasts = false;
        decisionWindow.SetActive(false);

        // Restore focus to the previously selected UI element
        if (previouslySelected != null)
        {
            EventSystem.current.SetSelectedGameObject(previouslySelected);
        }
    }

    private void OnConfirmDeleteCheckpoint()
    {
        // Clear saved checkpoint data
        PlayerPrefs.DeleteKey("SavedLevel");
        LevelSelectionManager.Instance.RefreshCheckpointFlags();

        // Close the decision window
        DeactivateDecisionWindow();

        // Apply the buffered modifier change
        switch (bufferedModifierKey)
        {
            case SettingsKeys.InfiniteLivesKey:
                ChangeInfiniteLives(bufferedModifierValue);
                break;
            case SettingsKeys.CheckpointsKey:
                ChangeCheckpoints(bufferedModifierValue);
                break;
            case SettingsKeys.TimeLimitKey:
                ChangeTimeLimit(bufferedModifierValue);
                break;
        }
    }

    public void OnCancelDeleteCheckpoint()
    {
        // Simply close the decision window without applying changes
        DeactivateDecisionWindow();
    }
}