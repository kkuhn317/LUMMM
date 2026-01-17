using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

// This script manages the modifiers in the Level Select screen (infinite lives, checkpoints, no time limit)
// Additionally, it initializes the GlobalVariables with the current settings from SaveData or PlayerPrefs
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
        // Debug current save state
        Debug.Log($"SaveManager.Current exists: {SaveManager.Current != null}");
        if (SaveManager.Current != null)
        {
            Debug.Log($"Modifiers exists: {SaveManager.Current.modifiers != null}");
            if (SaveManager.Current.modifiers != null)
            {
                Debug.Log($"Infinite Lives: {SaveManager.Current.modifiers.infiniteLivesEnabled}");
                Debug.Log($"Time Limit: {SaveManager.Current.modifiers.timeLimitEnabled}");
                Debug.Log($"Checkpoint Mode: {SaveManager.Current.modifiers.checkpointMode}");
            }
        }
        
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
        bool isInfiniteLivesEnabled = false;
        
        // Try to get from SaveData.modifiers first
        if (SaveManager.Current != null && SaveManager.Current.modifiers != null)
        {
            isInfiniteLivesEnabled = SaveManager.Current.modifiers.infiniteLivesEnabled;
        }
        else
        {
            // Fallback to PlayerPrefs for backward compatibility
            isInfiniteLivesEnabled = PlayerPrefs.GetInt(SettingsKeys.InfiniteLivesKey, 0) == 1;
        }
        
        infiniteLivesToggle.isOn = isInfiniteLivesEnabled;
        infiniteLivesImage.sprite = isInfiniteLivesEnabled ? enableInfiniteLivesSprite : disableInfiniteLivesSprite;
        GlobalVariables.infiniteLivesMode = isInfiniteLivesEnabled;
        infiniteLivesToggle.onValueChanged.AddListener(OnInfiniteLivesClick);
    }

    private void OnInfiniteLivesClick(bool isEnabled)
    {
        if (IsSaveGameAvailable())
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
        
        // Save to SaveData.modifiers
        if (SaveManager.Current != null)
        {
            if (SaveManager.Current.modifiers == null)
                SaveManager.Current.modifiers = new ModifiersData();
                
            SaveManager.Current.modifiers.infiniteLivesEnabled = isEnabled;
            SaveManager.Save();
        }
        
        // Also save to PlayerPrefs for backward compatibility
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

        // Apply to globals
        ApplyCheckpointMode(mode);
    }

    private void OnCheckpointModeRequested(int nextMode)
    {
        nextMode = Mathf.Clamp(nextMode, 0, 2);

        // If there's a saved checkpoint, we require confirmation before changing behavior
        if (IsSaveGameAvailable())
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

        // Save to SaveData.modifiers
        if (SaveManager.Current != null)
        {
            if (SaveManager.Current.modifiers == null)
                SaveManager.Current.modifiers = new ModifiersData();
                
            SaveManager.Current.modifiers.checkpointMode = mode;
            SaveManager.Save();
        }
        
        // Persist to PlayerPrefs for backward compatibility
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
        // First try to get from SaveData.modifiers
        if (SaveManager.Current != null && SaveManager.Current.modifiers != null)
        {
            return SaveManager.Current.modifiers.checkpointMode;
        }
        
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
        bool isTimeLimitEnabled = false;
        
        // Try to get from SaveData.modifiers first
        if (SaveManager.Current != null && SaveManager.Current.modifiers != null)
        {
            isTimeLimitEnabled = SaveManager.Current.modifiers.timeLimitEnabled;
        }
        else
        {
            // Fallback to PlayerPrefs for backward compatibility
            isTimeLimitEnabled = PlayerPrefs.GetInt(SettingsKeys.TimeLimitKey, 0) == 1;
        }
        
        timeLimitToggle.isOn = isTimeLimitEnabled;
        timeLimitImage.sprite = isTimeLimitEnabled ? enableTimeLimitSprite : disableTimeLimitSprite;
        GlobalVariables.stopTimeLimit = !isTimeLimitEnabled;
        timeLimitToggle.onValueChanged.AddListener(OnTimeLimitClick);
    }

    private void OnTimeLimitClick(bool isEnabled)
    {
        if (IsSaveGameAvailable())
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
        
        // Save to SaveData.modifiers
        if (SaveManager.Current != null)
        {
            if (SaveManager.Current.modifiers == null)
                SaveManager.Current.modifiers = new ModifiersData();
                
            SaveManager.Current.modifiers.timeLimitEnabled = isEnabled;
            SaveManager.Save();
        }
        
        // Also save to PlayerPrefs for backward compatibility
        PlayerPrefs.SetInt(SettingsKeys.TimeLimitKey, isEnabled ? 1 : 0);
        GlobalVariables.stopTimeLimit = isEnabled;
    }

    /* SAVE GAME */
    private bool IsSaveGameAvailable()
    {
        // Check new save system first
        if (SaveManager.Current != null && SaveManager.Current.checkpoint != null)
        {
            return SaveManager.Current.checkpoint.hasCheckpoint;
        }
        
        // Fallback to old PlayerPrefs system
        return PlayerPrefs.HasKey("SavedLevel");
    }

    private void ActivateDecisionWindow()
    {
        previouslySelected = EventSystem.current.currentSelectedGameObject;

        // Disable main UI
        if (mainCanvasGroup != null)
        {
            mainCanvasGroup.interactable = false;
            mainCanvasGroup.blocksRaycasts = false;
        }

        // Enable decision window
        if (decisionCanvasGroup != null)
        {
            decisionCanvasGroup.interactable = true;
            decisionCanvasGroup.blocksRaycasts = true;
        }
        
        if (decisionWindow != null)
            decisionWindow.SetActive(true);

        // Select the confirm button
        if (confirmDeleteButton != null)
            EventSystem.current.SetSelectedGameObject(confirmDeleteButton.gameObject);
    }

    private void DeactivateDecisionWindow()
    {
        // Enable main UI
        if (mainCanvasGroup != null)
        {
            mainCanvasGroup.interactable = true;
            mainCanvasGroup.blocksRaycasts = true;
        }

        // Disable decision window
        if (decisionCanvasGroup != null)
        {
            decisionCanvasGroup.interactable = false;
            decisionCanvasGroup.blocksRaycasts = false;
        }
        
        if (decisionWindow != null)
            decisionWindow.SetActive(false);

        // Restore selection
        if (previouslySelected != null)
        {
            EventSystem.current.SetSelectedGameObject(previouslySelected);
        }
    }

    private void OnConfirmDeleteCheckpoint()
    {
        // Clear saved checkpoint data in NEW system
        if (SaveManager.Current != null && SaveManager.Current.checkpoint != null)
        {
            SaveManager.Current.checkpoint.hasCheckpoint = false;
            SaveManager.Current.checkpoint.levelID = "";
            SaveManager.Current.checkpoint.checkpointId = 0;
            SaveManager.Current.checkpoint.coins = 0;
            SaveManager.Current.checkpoint.speedrunMs = 0;
            SaveManager.Current.checkpoint.greenCoinsInRun = null;
            SaveManager.Save();
        }
        
        // Clear saved checkpoint data in OLD system (for backward compatibility)
        PlayerPrefs.DeleteKey("SavedLevel");
        PlayerPrefs.DeleteKey("SavedCheckpoint");

        // Refresh any UI that shows checkpoint status
        if (LevelSelectionManager.Instance != null)
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