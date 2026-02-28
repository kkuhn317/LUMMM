using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Manages Level Select modifiers:
/// - Infinite Lives (toggle)
/// - Time Limit (toggle)
/// - Checkpoint Mode (cycle: 0=Off, 1=Classic, 2=Silent)
///
/// If a checkpoint is currently saved, changing any modifier prompts a Decision Window
/// (shown as a GUIManager overlay) to confirm deleting the checkpoint before applying.
/// </summary>
public class ModifiersSettings : MonoBehaviour
{
    [Header("Gameplay Toggles")]
    [SerializeField] private Toggle infiniteLivesToggle;
    [SerializeField] private Toggle timeLimitToggle;

    [Header("Checkpoint Cycle Button (0=Off, 1=Classic, 2=Silent)")]
    [SerializeField] private CheckpointCycleUI checkpointCycleUI;

    [Header("Toggle Sprites")]
    [SerializeField] private Image infiniteLivesImage;
    [SerializeField] private Image timeLimitImage;
    [SerializeField] private Sprite enableInfiniteLivesSprite;
    [SerializeField] private Sprite disableInfiniteLivesSprite;
    [SerializeField] private Sprite enableTimeLimitSprite;
    [SerializeField] private Sprite disableTimeLimitSprite;

    [Header("Decision Window (GUIManager Overlay)")]
    [SerializeField] private GameObject decisionWindow;
    [SerializeField] private Button confirmDeleteButton;
    [SerializeField] private Button cancelDeleteButton;

    [SerializeField] private string decisionWindowMenuName = "DecisionWindow";

    private enum BufferedToggleChange { None, InfiniteLives, TimeLimit }

    private BufferedToggleChange bufferedToggle = BufferedToggleChange.None;
    private bool bufferedToggleValue;

    // Buffer for checkpoint mode change (cycle button)
    private int bufferedCheckpointMode = -1;

    private void Start()
    {
        // Initialize UI from save
        ConfigureInfiniteLives();
        ConfigureCheckpointMode();
        ConfigureTimeLimit();

        // Hook decision window buttons
        if (confirmDeleteButton != null)
        {
            confirmDeleteButton.onClick.RemoveListener(OnConfirmDeleteCheckpoint);
            confirmDeleteButton.onClick.AddListener(OnConfirmDeleteCheckpoint);
        }

        if (cancelDeleteButton != null)
        {
            cancelDeleteButton.onClick.RemoveListener(OnCancelDeleteCheckpoint);
            cancelDeleteButton.onClick.AddListener(OnCancelDeleteCheckpoint);
        }
        
        if (decisionWindow != null)
            decisionWindow.SetActive(false);
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
            BufferToggle(BufferedToggleChange.InfiniteLives, isEnabled);
            OpenDecisionOverlay();
            return;
        }

        ApplyInfiniteLives(isEnabled);
    }

    private void ApplyInfiniteLives(bool isEnabled)
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

        GlobalVariables.infiniteTimeMode = enabled;
    }

    private void OnTimeLimitClick(bool isEnabled)
    {
        if (IsCheckpointActive())
        {
            BufferToggle(BufferedToggleChange.TimeLimit, isEnabled);
            OpenDecisionOverlay();
            return;
        }

        ApplyTimeLimit(isEnabled);
    }

    private void ApplyTimeLimit(bool isEnabled)
    {
        if (timeLimitImage != null)
            timeLimitImage.sprite = isEnabled ? enableTimeLimitSprite : disableTimeLimitSprite;

        EnsureModifiersExist();
        SaveManager.Current.modifiers.infiniteTimeEnabled = isEnabled;
        SaveManager.Save();

        GlobalVariables.infiniteTimeMode = isEnabled;
    }

    private bool ReadTimeLimitFromSave()
    {
        return SaveManager.Current != null
               && SaveManager.Current.modifiers != null
               && SaveManager.Current.modifiers.infiniteTimeEnabled;
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

        ApplyCheckpointModeGlobals(mode);
    }

    private void OnCheckpointModeRequested(int nextMode)
    {
        if (IsCheckpointActive())
        {
            BufferCheckpointMode(nextMode);
            OpenDecisionOverlay();
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

        ApplyCheckpointModeGlobals(mode);
    }

    private void ApplyCheckpointModeGlobals(int mode)
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

    private void OpenDecisionOverlay()
    {
        if (GUIManager.Instance == null)
        {
            Debug.LogError("ModifiersSettings: GUIManager.Instance is null. Add a GUIManager to the scene.");
            ClearBuffersAndRevertUI();
            return;
        }

        Selectable returnTo = null;
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            returnTo = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();

        GUIManager.Instance.OpenMenuAsOverlay(decisionWindowMenuName, returnTo);
    }

    private void CloseDecisionOverlay()
    {
        if (GUIManager.Instance == null)
        {
            Debug.LogError("ModifiersSettings: GUIManager.Instance is null. Add a GUIManager to the scene.");
            return;
        }

        GUIManager.Instance.CloseOverlay(decisionWindowMenuName);
    }

    private void OnConfirmDeleteCheckpoint()
    {
        DeleteCheckpointFromSave();
        RefreshCheckpointUI();

        CloseDecisionOverlay();

        // Apply buffered checkpoint mode if any, else apply buffered toggle
        if (bufferedCheckpointMode != -1)
        {
            SetCheckpointMode(bufferedCheckpointMode, animated: true);
            bufferedCheckpointMode = -1;
            bufferedToggle = BufferedToggleChange.None;
            return;
        }

        switch (bufferedToggle)
        {
            case BufferedToggleChange.InfiniteLives:
                ApplyInfiniteLives(bufferedToggleValue);
                break;

            case BufferedToggleChange.TimeLimit:
                ApplyTimeLimit(bufferedToggleValue);
                break;
        }

        bufferedToggle = BufferedToggleChange.None;
    }

    public void OnCancelDeleteCheckpoint()
    {
        // Revert UI back to saved state
        if (bufferedCheckpointMode != -1)
        {
            int savedMode = ReadCheckpointModeFromSave();
            if (checkpointCycleUI != null)
                checkpointCycleUI.SetModeInstant(savedMode);

            bufferedCheckpointMode = -1;
        }

        if (bufferedToggle != BufferedToggleChange.None)
        {
            ConfigureInfiniteLives();
            ConfigureTimeLimit();
            bufferedToggle = BufferedToggleChange.None;
        }

        CloseDecisionOverlay();
    }

    private void BufferToggle(BufferedToggleChange which, bool value)
    {
        bufferedToggle = which;
        bufferedToggleValue = value;
        bufferedCheckpointMode = -1; // clear other buffer
    }

    private void BufferCheckpointMode(int nextMode)
    {
        bufferedCheckpointMode = nextMode;
        bufferedToggle = BufferedToggleChange.None; // clear other buffer
    }

    private void ClearBuffersAndRevertUI()
    {
        bufferedCheckpointMode = -1;
        bufferedToggle = BufferedToggleChange.None;

        ConfigureInfiniteLives();
        ConfigureCheckpointMode();
        ConfigureTimeLimit();
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

    private void DeleteCheckpointFromSave()
    {
        if (SaveManager.Current == null || SaveManager.Current.checkpoint == null)
            return;

        SaveManager.Current.checkpoint.hasCheckpoint = false;
        SaveManager.Current.checkpoint.levelID = "";
        SaveManager.Current.checkpoint.checkpointId = 0;
        SaveManager.Current.checkpoint.coins = 0;
        SaveManager.Current.checkpoint.speedrunMs = 0;
        SaveManager.Current.checkpoint.greenCoinsInRun = null;

        SaveManager.Save();
    }

    private void RefreshCheckpointUI()
    {
        if (LevelSelectionManager.Instance != null)
            LevelSelectionManager.Instance.RefreshCheckpointFlags();
    }
}