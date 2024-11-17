using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        infiniteLivesToggle.onValueChanged.AddListener(OnInfiniteLivesClick);
    }

    private void OnInfiniteLivesClick(bool isEnabled)
    {
        if (isSaveGameAvailable())
        {
            // If the player has a saved game, ask for confirmation before changing the modifier
            bufferedModifierKey = SettingsKeys.InfiniteLivesKey;
            bufferedModifierValue = isEnabled;
            decisionWindow.SetActive(true);
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
        checkpointsToggle.onValueChanged.AddListener(OnCheckpointsClick);
    }

    private void OnCheckpointsClick(bool isEnabled)
    {
        if (isSaveGameAvailable())
        {
            // If the player has a saved game, ask for confirmation before changing the modifier
            bufferedModifierKey = SettingsKeys.CheckpointsKey;
            bufferedModifierValue = isEnabled;
            decisionWindow.SetActive(true);
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
        timeLimitToggle.onValueChanged.AddListener(OnTimeLimitClick);
    }

    private void OnTimeLimitClick(bool isEnabled)
    {
        if (isSaveGameAvailable())
        {
            // If the player has a saved game, ask for confirmation before changing the modifier
            bufferedModifierKey = SettingsKeys.TimeLimitKey;
            bufferedModifierValue = isEnabled;
            decisionWindow.SetActive(true);
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

    private void OnConfirmDeleteCheckpoint()
    {
        // Clear saved checkpoint data
        PlayerPrefs.DeleteKey("SavedLevel");

        LevelSelectionManager.Instance.RefreshCheckpointFlags();

        // Close the decision window
        decisionWindow.SetActive(false);

        // Change the modifier
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

    private void OnCancelDeleteCheckpoint()
    {
        // Simply close the decision window without deleting the checkpoint
        decisionWindow.SetActive(false);
    }
}