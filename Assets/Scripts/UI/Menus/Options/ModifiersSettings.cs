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
    public Toggle timeLimitToggle;

    [Header("Checkpoint Cycle Button (0=Off, 1=Classic, 2=Silent)")]
    [SerializeField] private CheckpointCycleUI checkpointCycleUI;

    [Header("Toggle Sprites")]
    public Image infiniteLivesImage;
    public Image timeLimitImage;
    public Sprite enableInfiniteLivesSprite;
    public Sprite disableInfiniteLivesSprite;
    public Sprite enableTimeLimitSprite;
    public Sprite disableTimeLimitSprite;

    [Header("Decision Window")]
    public GameObject decisionWindow; // Window to confirm deletion of the checkpoint
    public Button confirmDeleteButton;
    public Button cancelDeleteButton;
    public CanvasGroup mainCanvasGroup; // CanvasGroup for the main UI
    public CanvasGroup decisionCanvasGroup; // CanvasGroup for the decision window

    private GameObject previouslySelected; // To track the previously selected UI element

    // Buffer for toggle-based changes (infinite lives / time limit)
    private string bufferedModifierKey;
    private bool bufferedModifierValue;

    // Buffer for checkpoint mode change (cycle button)
    private int bufferedCheckpointMode = -1;

    private void Start()
    {
        ConfigureInfiniteLives();
        ConfigureCheckpointMode();
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

            bufferedCheckpointMode = -1; // clear other buffer
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

    /* CHECKPOINT MODE (0=Off, 1=Classic, 2=Silent) */
    private void ConfigureCheckpointMode()
    {
        int mode = ReadCheckpointModeWithFallback();

        // Initialize UI
        if (checkpointCycleUI != null)
        {
            checkpointCycleUI.SetModeInstant(mode);
            checkpointCycleUI.OnRequestModeChange += OnCheckpointModeRequested;
        }

        // Apply to globals + prefs mirror
        ApplyCheckpointMode(mode);
    }

    private void OnCheckpointModeRequested(int nextMode)
    {
        nextMode = Mathf.Clamp(nextMode, 0, 2);

        // If there's a saved checkpoint, we require confirmation before changing behavior
        if (isSaveGameAvailable())
        {
            bufferedCheckpointMode = nextMode;

            bufferedModifierKey = null; // clear other buffer
            ActivateDecisionWindow();
            return;
        }

        SetCheckpointMode(nextMode, animated: true);
    }

    private void SetCheckpointMode(int mode, bool animated)
    {
        mode = Mathf.Clamp(mode, 0, 2);

        // UI
        if (checkpointCycleUI != null)
        {
            if (animated) checkpointCycleUI.SetModeAnimated(mode);
            else checkpointCycleUI.SetModeInstant(mode);
        }

        // Persist + apply
        PlayerPrefs.SetInt(SettingsKeys.CheckpointModeKey, mode);

        // Legacy mirror so older code that still reads CheckpointsKey keeps working
        PlayerPrefs.SetInt(SettingsKeys.CheckpointsKey, mode != 0 ? 1 : 0);

        ApplyCheckpointMode(mode);
    }

    private void ApplyCheckpointMode(int mode)
    {
        // Existing global used across the project
        GlobalVariables.enableCheckpoints = mode != 0;
        GlobalVariables.checkpointMode = mode;
    }

    private int ReadCheckpointModeWithFallback()
    {
        // Prefer new 0/1/2 mode key if it exists
        if (PlayerPrefs.HasKey(SettingsKeys.CheckpointModeKey))
            return PlayerPrefs.GetInt(SettingsKeys.CheckpointModeKey, 0);

        // Fallback to old bool key (0/1)
        bool enabled = PlayerPrefs.GetInt(SettingsKeys.CheckpointsKey, 0) == 1;
        return enabled ? 1 : 0;
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

            bufferedCheckpointMode = -1; // clear other buffer
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
        // You already use SavedLevel in the current script :contentReference[oaicite:2]{index=2}
        return PlayerPrefs.HasKey("SavedLevel");
    }

    private void ActivateDecisionWindow()
    {
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

        // Restore selection
        if (previouslySelected != null)
        {
            EventSystem.current.SetSelectedGameObject(previouslySelected);
        }
    }

    private void OnConfirmDeleteCheckpoint()
    {
        // Clear saved checkpoint data
        PlayerPrefs.DeleteKey("SavedLevel");

        // If your project *also* uses "SavedCheckpoint" elsewhere, you may want to clear it too:
        // PlayerPrefs.DeleteKey("SavedCheckpoint");

        LevelSelectionManager.Instance.RefreshCheckpointFlags();
        DeactivateDecisionWindow();

        // If the pending change was a checkpoint mode request, apply it now
        if (bufferedCheckpointMode != -1)
        {
            SetCheckpointMode(bufferedCheckpointMode, animated: true);
            bufferedCheckpointMode = -1;
            return;
        }

        // Otherwise apply the buffered toggle modifier change
        switch (bufferedModifierKey)
        {
            case SettingsKeys.InfiniteLivesKey:
                ChangeInfiniteLives(bufferedModifierValue);
                break;
            case SettingsKeys.TimeLimitKey:
                ChangeTimeLimit(bufferedModifierValue);
                break;
        }
    }

    public void OnCancelDeleteCheckpoint()
    {
        // If we were cancelling a checkpoint mode change, revert UI back to saved mode
        if (bufferedCheckpointMode != -1 && checkpointCycleUI != null)
        {
            int savedMode = ReadCheckpointModeWithFallback();
            checkpointCycleUI.SetModeInstant(savedMode);
            bufferedCheckpointMode = -1;
        }

        DeactivateDecisionWindow();
    }
}