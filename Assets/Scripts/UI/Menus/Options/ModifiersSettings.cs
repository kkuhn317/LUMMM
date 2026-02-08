using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

// This script manages the modifiers in the Level Select screen (infinite lives, checkpoints, time limit)
// SaveData-only: NO PlayerPrefs fallback/mirroring.
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
    public CanvasGroup mainCanvasGroup;     // CanvasGroup for the main UI
    public CanvasGroup decisionCanvasGroup; // CanvasGroup for the decision window

    private GameObject previouslySelected;

    private enum BufferedToggleChange
    {
        None,
        InfiniteLives,
        TimeLimit
    }

    private BufferedToggleChange bufferedToggle = BufferedToggleChange.None;
    private bool bufferedToggleValue;

    // Buffer for checkpoint mode change (cycle button)
    private int bufferedCheckpointMode = -1;

    private void Start()
    {
        ConfigureInfiniteLives();
        ConfigureCheckpointMode();
        ConfigureTimeLimit();

        if (confirmDeleteButton != null)
            confirmDeleteButton.onClick.AddListener(OnConfirmDeleteCheckpoint);

        if (cancelDeleteButton != null)
            cancelDeleteButton.onClick.AddListener(OnCancelDeleteCheckpoint);

        if (decisionWindow != null)
            decisionWindow.SetActive(false);

        if (decisionCanvasGroup != null)
        {
            decisionCanvasGroup.interactable = false;
            decisionCanvasGroup.blocksRaycasts = false;
        }
    }

    private void ConfigureInfiniteLives()
    {
        bool enabled = ReadInfiniteLivesFromSave();

        if (infiniteLivesToggle != null)
        {
            infiniteLivesToggle.SetIsOnWithoutNotify(enabled);
            infiniteLivesToggle.onValueChanged.RemoveListener(OnInfiniteLivesClick);
            infiniteLivesToggle.onValueChanged.AddListener(OnInfiniteLivesClick);
        }

        if (infiniteLivesImage != null)
            infiniteLivesImage.sprite = enabled ? enableInfiniteLivesSprite : disableInfiniteLivesSprite;

        GlobalVariables.infiniteLivesMode = enabled;
    }

    private void OnInfiniteLivesClick(bool isEnabled)
    {
        if (IsCheckpointActive())
        {
            bufferedToggle = BufferedToggleChange.InfiniteLives;
            bufferedToggleValue = isEnabled;

            bufferedCheckpointMode = -1; // clear other buffer
            ActivateDecisionWindow();
            return;
        }

        ChangeInfiniteLives(isEnabled);
    }

    private void ChangeInfiniteLives(bool isEnabled)
    {
        if (infiniteLivesImage != null)
            infiniteLivesImage.sprite = isEnabled ? enableInfiniteLivesSprite : disableInfiniteLivesSprite;

        EnsureModifiersExist();
        SaveManager.Current.modifiers.infiniteLivesEnabled = isEnabled;
        SaveManager.Save();

        GlobalVariables.infiniteLivesMode = isEnabled;
    }

    private bool ReadInfiniteLivesFromSave()
    {
        return SaveManager.Current != null
               && SaveManager.Current.modifiers != null
               && SaveManager.Current.modifiers.infiniteLivesEnabled;
    }

    private void ConfigureCheckpointMode()
    {
        int mode = ReadCheckpointModeFromSave();

        if (checkpointCycleUI != null)
        {
            checkpointCycleUI.SetModeInstant(mode);
            checkpointCycleUI.OnRequestModeChange -= OnCheckpointModeRequested;
            checkpointCycleUI.OnRequestModeChange += OnCheckpointModeRequested;
        }

        ApplyCheckpointMode(mode);
    }

    private void OnCheckpointModeRequested(int nextMode)
    {
        // If there's an active checkpoint, require confirmation before changing behavior
        if (IsCheckpointActive())
        {
            bufferedCheckpointMode = nextMode;
            bufferedToggle = BufferedToggleChange.None; // clear other buffer
            ActivateDecisionWindow();
            return;
        }

        SetCheckpointMode(nextMode, animated: true);
    }

    private void SetCheckpointMode(int mode, bool animated)
    {
        mode = Mathf.Clamp(mode, 0, 2);

        if (checkpointCycleUI != null)
        {
            if (animated) checkpointCycleUI.SetModeAnimated(mode);
            else checkpointCycleUI.SetModeInstant(mode);
        }

        EnsureModifiersExist();
        SaveManager.Current.modifiers.checkpointMode = mode;
        SaveManager.Save();

        ApplyCheckpointMode(mode);
    }

    private void ApplyCheckpointMode(int mode)
    {
        GlobalVariables.enableCheckpoints = mode != 0;
        GlobalVariables.checkpointMode = mode;
    }

    private int ReadCheckpointModeFromSave()
    {
        if (SaveManager.Current != null && SaveManager.Current.modifiers != null)
            return Mathf.Clamp(SaveManager.Current.modifiers.checkpointMode, 0, 2);

        return 0; // default Off
    }

    private void ConfigureTimeLimit()
    {
        bool enabled = ReadTimeLimitFromSave();

        if (timeLimitToggle != null)
        {
            timeLimitToggle.SetIsOnWithoutNotify(enabled);
            timeLimitToggle.onValueChanged.RemoveListener(OnTimeLimitClick);
            timeLimitToggle.onValueChanged.AddListener(OnTimeLimitClick);
        }

        if (timeLimitImage != null)
            timeLimitImage.sprite = enabled ? enableTimeLimitSprite : disableTimeLimitSprite;

        GlobalVariables.stopTimeLimit = enabled;
    }

    private void OnTimeLimitClick(bool isEnabled)
    {
        if (IsCheckpointActive())
        {
            bufferedToggle = BufferedToggleChange.TimeLimit;
            bufferedToggleValue = isEnabled;

            bufferedCheckpointMode = -1; // clear other buffer
            ActivateDecisionWindow();
            return;
        }

        ChangeTimeLimit(isEnabled);
    }

    private void ChangeTimeLimit(bool isEnabled)
    {
        if (timeLimitImage != null)
            timeLimitImage.sprite = isEnabled ? enableTimeLimitSprite : disableTimeLimitSprite;

        EnsureModifiersExist();
        SaveManager.Current.modifiers.timeLimitEnabled = isEnabled;
        SaveManager.Save();

        GlobalVariables.stopTimeLimit = isEnabled;
    }

    private bool ReadTimeLimitFromSave()
    {
        return SaveManager.Current != null
               && SaveManager.Current.modifiers != null
               && SaveManager.Current.modifiers.timeLimitEnabled;
    }

    private bool IsCheckpointActive()
    {
        return SaveManager.Current != null
               && SaveManager.Current.checkpoint != null
               && SaveManager.Current.checkpoint.hasCheckpoint;
    }

    private void EnsureModifiersExist()
    {
        if (SaveManager.Current == null) return;

        if (SaveManager.Current.modifiers == null)
            SaveManager.Current.modifiers = new ModifiersData();
    }
    
    private void ActivateDecisionWindow()
    {
        previouslySelected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        if (mainCanvasGroup != null)
        {
            mainCanvasGroup.interactable = false;
            mainCanvasGroup.blocksRaycasts = false;
        }

        if (decisionCanvasGroup != null)
        {
            decisionCanvasGroup.interactable = true;
            decisionCanvasGroup.blocksRaycasts = true;
        }

        if (decisionWindow != null)
            decisionWindow.SetActive(true);

        // Select confirm button
        if (EventSystem.current != null && confirmDeleteButton != null)
            EventSystem.current.SetSelectedGameObject(confirmDeleteButton.gameObject);
    }

    private void DeactivateDecisionWindow()
    {
        if (mainCanvasGroup != null)
        {
            mainCanvasGroup.interactable = true;
            mainCanvasGroup.blocksRaycasts = true;
        }

        if (decisionCanvasGroup != null)
        {
            decisionCanvasGroup.interactable = false;
            decisionCanvasGroup.blocksRaycasts = false;
        }

        if (decisionWindow != null)
            decisionWindow.SetActive(false);

        // Restore selection
        if (EventSystem.current != null && previouslySelected != null)
            EventSystem.current.SetSelectedGameObject(previouslySelected);
    }

    private void OnConfirmDeleteCheckpoint()
    {
        // Clear saved checkpoint data (NEW system only)
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

        // Refresh any UI that shows checkpoint status
        if (LevelSelectionManager.Instance != null)
            LevelSelectionManager.Instance.RefreshCheckpointFlags();

        DeactivateDecisionWindow();

        // If the pending change was a checkpoint mode request, apply it now
        if (bufferedCheckpointMode != -1)
        {
            SetCheckpointMode(bufferedCheckpointMode, animated: true);
            bufferedCheckpointMode = -1;
            bufferedToggle = BufferedToggleChange.None;
            return;
        }

        // Otherwise apply the buffered toggle modifier change
        switch (bufferedToggle)
        {
            case BufferedToggleChange.InfiniteLives:
                ChangeInfiniteLives(bufferedToggleValue);
                break;
            case BufferedToggleChange.TimeLimit:
                ChangeTimeLimit(bufferedToggleValue);
                break;
        }

        bufferedToggle = BufferedToggleChange.None;
    }

    public void OnCancelDeleteCheckpoint()
    {
        // If we were cancelling a checkpoint mode change, revert UI back to saved mode
        if (bufferedCheckpointMode != -1 && checkpointCycleUI != null)
        {
            int savedMode = ReadCheckpointModeFromSave();
            checkpointCycleUI.SetModeInstant(savedMode);
            bufferedCheckpointMode = -1;
        }

        // If we were cancelling a toggle change, revert UI back to saved values
        if (bufferedToggle != BufferedToggleChange.None)
        {
            ConfigureInfiniteLives();
            ConfigureTimeLimit();
            bufferedToggle = BufferedToggleChange.None;
        }

        DeactivateDecisionWindow();
    }
}