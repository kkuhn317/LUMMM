using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;


#if UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL
// Namespace from Netherlands3D/FileBrowser (fork of StandaloneFileBrowser)
using SFB;
#endif

public class SaveSlotManager : MonoBehaviour
{
    public enum InteractionMode
    {
        Normal,
        Delete,
        CopySelectSource,
        CopySelectDestination,
        ImportSelectDestination,
        ExportSelectSource,
        RenameSelectTarget,
    }

    public InteractionMode CurrentMode { get; private set; } = InteractionMode.Normal;

    public static int ActiveSlotIndex { get; private set; } = 0;

    [Header("UI Cards for A, B, C")]
    public SaveFileUI[] slotCards;

    [Header("Where to go after selecting a file")]
    public string levelSelectSceneName = "SelectLevel";

    [Header("Audio")]
    public AudioClip transitionSound;

    [Header("Import / Export")]
    [Tooltip("If empty, Application.persistentDataPath/ExportedSaves will be used.")]
    public string defaultExportDirectory = "";

    [Header("Name / Rename Popup")]
    public SaveSlotRename renamePopup;

    [Header("Confirm Popup")]
    public ConfirmPopup confirmPopup;
    [SerializeField] private LocalizedString importOverwriteMsg;
    [SerializeField] private LocalizedString importBtn;
    [SerializeField] private LocalizedString cancelBtn;
    [SerializeField] private LocalizedString copyOverwriteMsg;
    [SerializeField] private LocalizedString overwriteBtn;
    [SerializeField] private LocalizedString deleteConfirmMsg;
    [SerializeField] private LocalizedString deleteBtn;

    [Header("Block Input")]
    public UIInputLock uiInputLock;

    public int FocusedSlotIndex { get; private set; } = 0;
    public int LastFocusedSlotIndex { get; private set; } = 0;

    // For copy mode (source slot chosen in step 1)
    private int copySourceIndex = -1;

    // File dialog / file operation state
    private bool isFileDialogOpen = false; // True while any file dialog is open
    private bool isFileOperationInProgress = false; // Used to absorb the Submit right after closing the dialog

    [Header("Actions depending on mode")]
    public RawImage backgroundToTint;
    public Color normalBackgroundColor = Color.white;
    public Color deleteBackgroundColor = Color.red;
    public float backgroundTintDuration = 0.25f;
    private Coroutine backgroundTintRoutine;


    public event System.Action<InteractionMode> ModeChanged;


    private void Start()
    {
        RefreshAllSlots();
        FocusSlot(0);
        SetMode(CurrentMode, refreshVisuals: true, notify: true);
        
        if (renamePopup != null)
        {
            renamePopup.onNameConfirmed.AddListener(OnPopupNameConfirmed);
            renamePopup.onRenameCancelled.AddListener(OnPopupCancelled);
        }
    }

    private void FixDefaultProfileNameForSlot(int slotIndex)
    {
        // Only valid for A/B/C
        if (slotIndex < 0 || slotIndex > 2) return;
        if (!SaveManager.SlotExists(slotIndex)) return;

        int prevSlot = SaveManager.CurrentSlot;

        // Load the destination slot so we can modify its data
        SaveManager.Load(slotIndex);

        var data = SaveManager.Current;
        if (data == null) return;

        string name = data.profileName;
        SaveSlotNaming.EnsureCorrectDefaultNameForSlot(ref name, (SaveSlotId)slotIndex);

        if (name != data.profileName)
        {
            data.profileName = name;
            SaveManager.Save();
        }

        // Restore previous slot to avoid side effects in menus
        if (prevSlot != slotIndex)
            SaveManager.Load(prevSlot);
    }


    #region Refresh & Focus
    private void RefreshModeVisuals()
    {
        bool isDelete = CurrentMode == InteractionMode.Delete;

        if (backgroundToTint != null)
            SetBackgroundTint(isDelete ? deleteBackgroundColor : normalBackgroundColor);
    }

    private void SetBackgroundTint(Color target)
    {
        if (backgroundToTint == null) return;

        if (backgroundTintRoutine != null)
            StopCoroutine(backgroundTintRoutine);

        backgroundTintRoutine = StartCoroutine(TintBackgroundRoutine(target, backgroundTintDuration));
    }

    private IEnumerator TintBackgroundRoutine(Color target, float duration)
    {
        Color start = backgroundToTint.color;

        if (duration <= 0f)
        {
            backgroundToTint.color = target;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration; // unscaled so it still works if Time.timeScale changes
            backgroundToTint.color = Color.Lerp(start, target, Mathf.Clamp01(t));
            yield return null;
        }

        backgroundToTint.color = target;
    }

    public void RefreshAllSlots()
    {
        if (slotCards == null) return;

        for (int i = 0; i < slotCards.Length; i++)
        {
            if (slotCards[i] != null)
            {
                slotCards[i].Refresh(i);
            }
        }
    }

    public void FocusSlot(int index)
    {
        if (index < 0)
            FocusedSlotIndex = -1;
        else
            FocusedSlotIndex = Mathf.Clamp(index, 0, slotCards.Length - 1);

        LastFocusedSlotIndex = FocusedSlotIndex;

        for (int i = 0; i < slotCards.Length; i++)
        {
            if (slotCards[i] != null)
                slotCards[i].UpdateFocusVisual();
        }
    }
    #endregion

    #region Modes Entry Points & Cancel
    /// <summary>
    /// Called by the Delete button.
    /// If already in Delete mode, pressing again cancels (back to Normal).
    /// </summary>
    public void EnterDeleteMode()
    {
        // Toggle behavior: pressing the same button again cancels the process
        if (CurrentMode == InteractionMode.Delete)
        {
            CancelCurrentMode();
            return;
        }

        SetMode(InteractionMode.Delete); 

        Debug.Log("SaveSlotManager: DELETE MODE enabled. Next submit on a slot will delete.");
    }

    /// <summary>
    /// Called by the Copy button.
    /// First press enters Copy (select source) mode.
    /// Pressing again while in Copy mode cancels and returns to Normal.
    /// </summary>
    public void EnterCopyMode()
    {
        if (CurrentMode == InteractionMode.CopySelectSource ||
            CurrentMode == InteractionMode.CopySelectDestination)
        {
            SetMode(InteractionMode.Normal);
            copySourceIndex = -1;
            return;
        }

        copySourceIndex = -1;
        SetMode(InteractionMode.CopySelectSource);
    }

    /// <summary>
    /// Called by the Import button.
    /// First press enters Import (select destination) mode.
    /// Pressing again while in Import mode cancels and returns to Normal.
    /// </summary>
    public void EnterImportMode()
    {
        if (CurrentMode == InteractionMode.ImportSelectDestination)
        {
            SetMode(InteractionMode.Normal);
            return;
        }

        copySourceIndex = -1;
        SetMode(InteractionMode.ImportSelectDestination);
    }

    /// <summary>
    /// Called by the Export button.
    /// First press enters Export (select source) mode.
    /// Pressing again while in Export mode cancels and returns to Normal.
    /// </summary>
    public void EnterExportMode()
    {
        if (CurrentMode == InteractionMode.ExportSelectSource)
        {
            SetMode(InteractionMode.Normal);
            return;
        }

        copySourceIndex = -1;
        SetMode(InteractionMode.ExportSelectSource);
    }

    /// <summary>
    /// Called by the Rename button.
    /// First press enters Rename mode.
    /// Pressing agagin while in Rename button cancels and returns to Normal.
    /// </summary>
    public void EnterRenameMode()
    {
        if (CurrentMode == InteractionMode.RenameSelectTarget)
        {
            CancelCurrentMode(); // goes back to normal
            return;
        }

        // If you’re in any other special mode, go back to normal first
        if (CurrentMode != InteractionMode.Normal)
            CancelCurrentMode();

        SetMode(InteractionMode.RenameSelectTarget);
        Debug.Log("SaveSlotManager: RENAME MODE enabled. Next submit on a slot will rename.");
    }

    /// <summary>
    /// Cancels any current special mode (Delete, Copy, Import, Export) and returns to Normal mode.
    /// Should be called by the Cancel input (e.g., B / Esc).
    /// </summary>
    public void CancelCurrentMode()
    {
        bool wasSpecialMode = CurrentMode != InteractionMode.Normal;
        copySourceIndex = -1;
        
        SetMode(InteractionMode.Normal);

        if (wasSpecialMode)
        {
            Debug.Log("SaveSlotManager: current mode cancelled, back to NORMAL.");
            // Optional: hide any helper / warning UI here.
        }
    }

    #endregion

    #region Main Entry From UI

    /// <summary>
    /// Main entry point used by SaveFileUI on Submit / Click.
    /// Behavior depends on the current interaction mode.
    /// </summary>
    public void PlayFocusedSlot()
    {
        // Ignore Submit while any file dialog is open or immediately after it closed.
        // This prevents the same keyboard Submit from both confirming the file dialog
        // and also triggering a scene transition / play action.
        if (isFileDialogOpen || isFileOperationInProgress)
        {
            Debug.Log("SaveSlotManager: Ignoring PlayFocusedSlot while a file dialog is open or just closed.");
            return;
        }

        int index = FocusedSlotIndex;

        switch (CurrentMode)
        {
            case InteractionMode.Delete:
                HandleDeleteMode(index);
                break;

            case InteractionMode.CopySelectSource:
                HandleCopySelectSource(index);
                break;

            case InteractionMode.CopySelectDestination:
                HandleCopySelectDestination(index);
                break;

            case InteractionMode.ImportSelectDestination:
                HandleImportDestination(index);
                break;

            case InteractionMode.ExportSelectSource:
                HandleExportSource(index);
                break;
            
            case InteractionMode.RenameSelectTarget:
                HandleRenameSelectTarget(index);
                break;

            case InteractionMode.Normal:
            default:
                PlaySlot(index);
                break;
        }
    }
    #endregion

    #region Mode Logic
    private void SetMode(InteractionMode newMode, bool refreshVisuals = true, bool notify = true, bool forceNotify = false)
    {
        bool changed = CurrentMode != newMode;
        CurrentMode = newMode;

        if (refreshVisuals)
            RefreshModeVisuals();

        if (notify && (changed || forceNotify))
            ModeChanged?.Invoke(CurrentMode);
    }

    private bool SlotHasFile(int index)
    {
        return SaveManager.SlotExists(index);
    }

    private void HandleDeleteMode(int index)
    {
        if (!SlotHasFile(index))
        {
            Debug.Log("SaveSlotManager: Cannot delete an empty slot.");
            SetMode(InteractionMode.Normal);
            return;
        }
        
        if (confirmPopup != null)
        {
            confirmPopup.Show(
                deleteConfirmMsg,
                yes: () =>
                {
                    DeleteFocusedSlot();
                    SetMode(InteractionMode.Normal);
                },
                no: () =>
                {
                    SetMode(InteractionMode.Normal);
                },
                selectYes: false,
                yesText: deleteBtn,
                noText: cancelBtn
            );
            return;
        }

        DeleteFocusedSlot();
        // Always go back to normal after one attempt
        SetMode(InteractionMode.Normal);
    }

    private void HandleCopySelectSource(int index)
    {
        if (!SlotHasFile(index))
        {
            Debug.Log("SaveSlotManager: Cannot copy from an empty slot.");
            return;
        }

        copySourceIndex = index;
        SetMode(InteractionMode.CopySelectDestination);

        Debug.Log($"SaveSlotManager: Source slot set to {index}. Now select destination slot.");
    }

    private void HandleCopySelectDestination(int index)
    {
        Debug.Log($"HandleCopySelectDestination: UIInputLock lockCount before: {uiInputLock?.GetLockCount()}");

        if (copySourceIndex < 0)
        {
            Debug.LogWarning("SaveSlotManager: Copy destination selected but source index is invalid. Returning to normal mode.");
            SetMode(InteractionMode.Normal);
            PlaySlot(index);
            return;
        }

        int fromIndex = copySourceIndex;
        int toIndex = index;

        if (fromIndex == toIndex)
        {
            Debug.Log("SaveSlotManager: Destination equals source. Copy cancelled.");
            copySourceIndex = -1;
            SetMode(InteractionMode.Normal);
            return;
        }

        // Store current selection BEFORE unlocking
        var prevSelected = EventSystem.current?.currentSelectedGameObject;
        Debug.Log($"HandleCopySelectDestination: Storing previous selection: {prevSelected?.name}");

        if (SaveManager.SlotExists(toIndex) && confirmPopup != null)
        {
            // UNLOCK before showing popup (like PipeEnterSequence does)
            uiInputLock?.Unlock(restoreSelection: false);
            
            confirmPopup.Show(
                copyOverwriteMsg,
                yes: () =>
                {
                    // Execute copy
                    CopyInternal(fromIndex, toIndex);
                    copySourceIndex = -1;
                    SetMode(InteractionMode.Normal);
                    
                    // Restore selection
                    if (EventSystem.current != null && prevSelected != null)
                    {
                        EventSystem.current.SetSelectedGameObject(prevSelected);
                        Debug.Log($"HandleCopySelectDestination: Restoring selection after copy (yes): {prevSelected.name}");
                    }
                },
                no: () =>
                {
                    copySourceIndex = -1;
                    SetMode(InteractionMode.Normal);
                    
                    // Restore selection  
                    if (EventSystem.current != null && prevSelected != null)
                    {
                        EventSystem.current.SetSelectedGameObject(prevSelected);
                        Debug.Log($"HandleCopySelectDestination: Restoring selection after copy (no): {prevSelected.name}");
                    }
                },
                selectYes: false, // Change to true if you want "Yes" selected
                yesText: overwriteBtn,
                noText: cancelBtn
            );
            return;
        }

        CopyInternal(fromIndex, toIndex);
        copySourceIndex = -1;
        SetMode(InteractionMode.Normal);
        
        // Restore selection when no popup is shown
        if (EventSystem.current != null && prevSelected != null)
        {
            EventSystem.current.SetSelectedGameObject(prevSelected);
            Debug.Log($"HandleCopySelectDestination: Restoring selection (no popup): {prevSelected.name}");
        }
    }

    private void HandleImportDestination(int index)
    {
        Debug.Log($"HandleImportDestination: UIInputLock lockCount before: {uiInputLock?.GetLockCount()}");

        int slotIndex = Mathf.Clamp(index, 0, slotCards.Length - 1);

        // Leave special mode immediately
        SetMode(InteractionMode.Normal);

        if (isFileDialogOpen || isFileOperationInProgress)
        {
            Debug.LogWarning("SaveSlotManager: Cannot open import dialog while another file operation is active.");
            return;
        }

        string saveExtension = FileDataService.SaveExtension;
        if (string.IsNullOrWhiteSpace(saveExtension))
            saveExtension = "lummm";

        if (saveExtension.StartsWith("."))
            saveExtension = saveExtension.Substring(1);

        isFileDialogOpen = true;
        isFileOperationInProgress = true;

        // Save the current selection BEFORE the file dialog
        var selectionBeforeFileDialog = EventSystem.current?.currentSelectedGameObject;
        Debug.Log($"HandleImportDestination: Saving selection before file dialog: {selectionBeforeFileDialog?.name}");

        // Unlock for file dialog
        uiInputLock?.Unlock(restoreSelection: false);

        FilePicker.OpenFileForImport(path =>
        {
            // Start a coroutine to handle the result (allows us to use yield)
            StartCoroutine(ProcessImportFileSelection(slotIndex, path, selectionBeforeFileDialog));
        }, saveExtension);
    }

    private IEnumerator ProcessImportFileSelection(int slotIndex, string path, GameObject selectionBeforeFileDialog)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("SaveSlotManager: Import cancelled or no file selected.");
            
            // Wait a frame for Unity to stabilize
            yield return null;
            
            // Restore selection
            if (EventSystem.current != null && selectionBeforeFileDialog != null)
            {
                EventSystem.current.SetSelectedGameObject(selectionBeforeFileDialog);
                Debug.Log($"Restored selection after cancel: {selectionBeforeFileDialog.name}");
            }
            
            StartCoroutine(ResetFileDialogFlagsNextFrame());
            yield break;
        }

        Debug.Log($"SaveSlotManager: Import selected file: {path} into slot {slotIndex}");
        
        // Wait until the end of a frame for Unity to stabilize after file dialog (and the confirmation pop up appear animation as well)
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForEndOfFrame();
        }

        yield return null;

        if (confirmPopup != null && confirmPopup.animator != null)
        {
            confirmPopup.animator.Rebind();
            confirmPopup.animator.Update(0f);
            confirmPopup.yesButton.animator.Rebind();
            confirmPopup.yesButton.animator.Rebind();
        }

        yield return null;
        
        // Check if we need confirmation popup
        if (SaveManager.SlotExists(slotIndex) && confirmPopup != null)
        {
            Debug.Log($"SaveSlotManager: Showing import confirmation popup");
            
            confirmPopup.Show(
                importOverwriteMsg,
                yes: () => 
                {
                    ImportIntoSlot(slotIndex, path);
                    StartCoroutine(ResetFileDialogFlagsNextFrame());
                    
                    // Restore selection
                    if (EventSystem.current != null && selectionBeforeFileDialog != null)
                    {
                        EventSystem.current.SetSelectedGameObject(selectionBeforeFileDialog);
                        Debug.Log($"Restored selection after import (yes): {selectionBeforeFileDialog.name}");
                    }
                },
                no: () => 
                {
                    StartCoroutine(ResetFileDialogFlagsNextFrame());
                    
                    // Restore selection
                    if (EventSystem.current != null && selectionBeforeFileDialog != null)
                    {
                        EventSystem.current.SetSelectedGameObject(selectionBeforeFileDialog);
                        Debug.Log($"Restored selection after import (no): {selectionBeforeFileDialog.name}");
                    }
                },
                selectYes: false,
                yesText: importBtn,
                noText: cancelBtn
            );
        }
        else
        {
            ImportIntoSlot(slotIndex, path);
            StartCoroutine(ResetFileDialogFlagsNextFrame());
            
            // Restore selection
            if (EventSystem.current != null && selectionBeforeFileDialog != null)
            {
                EventSystem.current.SetSelectedGameObject(selectionBeforeFileDialog);
                Debug.Log($"Restored selection (no popup): {selectionBeforeFileDialog.name}");
            }
        }
    }

    private void HandleExportSource(int index)
    {
        int slotIndex = Mathf.Clamp(index, 0, slotCards.Length - 1);

        if (!SaveManager.SlotExists(slotIndex))
        {
            Debug.LogWarning("SaveSlotManager: Cannot export an empty slot.");
            SetMode(InteractionMode.Normal);
            return;
        }

        // Optionally focus the chosen slot for visual feedback
        FocusSlot(slotIndex);

        // Open an export dialog / share sheet depending on platform
        ShowExportDialogForSlot(slotIndex);

        // Back to normal after one export attempt
        SetMode(InteractionMode.Normal);
    }

    /// <summary>
    /// Opens a platform-appropriate UI so the player can choose where (or how)
    /// to export the save file for the given slot.
    /// </summary>
    private void ShowExportDialogForSlot(int slotIndex)
    {
        // Prevent opening multiple dialogs or interfering with an ongoing file operation
        if (isFileDialogOpen || isFileOperationInProgress)
        {
            Debug.LogWarning("SaveSlotManager: Cannot open export dialog while another file operation is active.");
            return;
        }

        // Get extension from FileDataService (e.g. ".lummm" or "lummm")
        string saveExtension = FileDataService.SaveExtension;

        if (string.IsNullOrWhiteSpace(saveExtension))
        {
            Debug.LogWarning("SaveSlotManager: FileDataService.SaveExtension is empty, using default 'lummm'.");
            saveExtension = "lummm";
        }

        // Normalize to "lummm" for filters and also keep a version with dot
        string extWithoutDot = saveExtension.StartsWith(".") ? saveExtension.Substring(1) : saveExtension;
        string extWithDot = saveExtension.StartsWith(".") ? saveExtension : "." + saveExtension;

        isFileDialogOpen = true;
        isFileOperationInProgress = true;

#if UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL
        // Desktop & WebGL: let the user pick a full path with SaveFilePanel

        string defaultFileName = $"save_slot_{slotIndex}{extWithDot}";

        var extensions = new[]
        {
            new ExtensionFilter("Save Files", extWithoutDot),
            new ExtensionFilter("All Files", "*")
        };

        string path = StandaloneFileBrowser.SaveFilePanel(
            "Export Save",
            "",
            defaultFileName,
            extWithoutDot
        );

        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("SaveSlotManager: Export cancelled by user.");
            StartCoroutine(ResetFileDialogFlagsNextFrame());
            return;
        }

        string directory = Path.GetDirectoryName(path);
        string fileNameNoExt = Path.GetFileNameWithoutExtension(path);

        string exportedPath = SaveManager.ExportSlot(slotIndex, directory, fileNameNoExt);

        if (string.IsNullOrEmpty(exportedPath))
        {
            Debug.LogWarning("SaveSlotManager: Export failed. See previous logs for details.");
        }
        else
        {
            Debug.Log($"SaveSlotManager: Exported slot {slotIndex} to '{exportedPath}'.");
        }

        StartCoroutine(ResetFileDialogFlagsNextFrame());

#elif UNITY_ANDROID || UNITY_IOS
        // Mobile: export to a temp file and open native share/export sheet

        string directory = Path.Combine(Application.persistentDataPath, "TempExport");
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string fileNameNoExt = $"save_slot_{slotIndex}_export";
        string exportedPath = SaveManager.ExportSlot(slotIndex, directory, fileNameNoExt);

        if (string.IsNullOrEmpty(exportedPath))
        {
            Debug.LogWarning("SaveSlotManager: Export failed on mobile. See previous logs for details.");
            StartCoroutine(ResetFileDialogFlagsNextFrame());
            return;
        }

        Debug.Log($"SaveSlotManager: Exported slot {slotIndex} to temp path '{exportedPath}', opening share sheet.");

        // NativeFilePicker.ExportFile will let the user choose where/how to send the file
        NativeFilePicker.ExportFile(
            exportedPath,
            (success) =>
            {
                Debug.Log($"SaveSlotManager: NativeFilePicker.ExportFile completed. Success: {success}");
                StartCoroutine(ResetFileDialogFlagsNextFrame());
            }
        );

#else
        Debug.LogWarning("SaveSlotManager: Export is not supported on this platform.");
        StartCoroutine(ResetFileDialogFlagsNextFrame());
#endif
    }

    private void HandleRenameSelectTarget(int index)
    {
        int slotIndex = Mathf.Clamp(index, 0, slotCards.Length - 1);

        if (!SaveManager.SlotExists(slotIndex))
        {
            Debug.Log("SaveSlotManager: Cannot rename an empty slot.");
            // stay in Rename mode, player can choose another slot
            return;
        }

        if (renamePopup == null)
        {
            Debug.LogWarning("SaveSlotManager: renamePopup is missing.");
            SetMode(InteractionMode.Normal);
            return;
        }

        // Unlock before showing popup so UI can interact
        uiInputLock?.Unlock(restoreSelection: false);

        string currentName = GetSlotProfileName(slotIndex);
        renamePopup.OpenForRename(slotIndex, currentName);
    }

    #endregion

    #region Original Behaviour
    public void PlaySlot(int index)
    {
        if (CurrentMode != InteractionMode.Normal) return;
        if (isFileDialogOpen || isFileOperationInProgress) return;

        // If this is a NEW slot, ask for a name first (create flow).
        if (!SaveManager.SlotExists(index) && renamePopup != null)
        {
            // Unlock BEFORE showing popup so navigation works.
            if (uiInputLock != null && uiInputLock.GetLockCount() > 0)
                uiInputLock.Unlock(restoreSelection: false);

            renamePopup.OpenForNewSlot(index);
            return;
        }

        ActiveSlotIndex = index;
        SaveManager.Load(index);

        if (slotCards != null && index >= 0 && index < slotCards.Length && slotCards[index] != null)
            slotCards[index].Refresh(index);

        AudioManager.Instance?.Play(transitionSound, SoundCategory.SFX);

        if (FadeInOutScene.Instance != null)
            FadeInOutScene.Instance.LoadSceneWithFade(levelSelectSceneName);
        else
            SceneManager.LoadScene(levelSelectSceneName);
    }

    public void DeleteSlot(int index)
    {
        FocusSlot(index);
        DeleteFocusedSlot();
    }

    public void DeleteFocusedSlot()
    {
        int index = FocusedSlotIndex;
        SaveManager.Delete(index);
        RefreshAllSlots();
    }

    /// <summary>
    /// Old API that might still be used by other buttons.
    /// Copies from the currently focused slot to a target index.
    /// </summary>
    public void CopyFocusedTo(int targetIndex)
    {
        int fromIndex = FocusedSlotIndex;
        CopyInternal(fromIndex, targetIndex);
    }

    /// <summary>
    /// Central copy logic used by both copy modes and CopyFocusedTo.
    /// If destination already has a save, it is deleted first so the copy can succeed.
    /// </summary>
    private void CopyInternal(int fromIndex, int targetIndex)
    {
        int toIndex = Mathf.Clamp(targetIndex, 0, slotCards.Length - 1);

        if (fromIndex == toIndex) return;

        if (!SaveManager.SlotExists(fromIndex))
        {
            Debug.LogWarning("CopyInternal: Source slot is empty, cannot copy.");
            return;
        }

        // If destination already has a file, delete it so the copy can succeed
        if (SaveManager.SlotExists(toIndex))
        {
            Debug.Log($"CopyInternal: Destination slot {toIndex} already has a save, deleting it before copy.");
            SaveManager.Delete(toIndex);
        }

        string fromFileName = SaveManager.GetSlotFileName(fromIndex);
        string toFileName = SaveManager.GetSlotFileName(toIndex);

        var dataService = new FileDataService(new JsonSerializer());
        dataService.Copy(fromFileName, toFileName);

        if (toIndex == SaveManager.CurrentSlot)
            SaveManager.Load(toIndex);
        
        FixDefaultProfileNameForSlot(toIndex);

        RefreshAllSlots();
    }
    #endregion

    #region Import / Export
    /// <summary>
    /// Called by the IMPORT button. Opens a cross-platform file picker and imports
    /// the selected save file into the currently focused slot.
    /// </summary>
    public void OnImportButtonClicked()
    {
        if (CurrentMode != InteractionMode.Normal)
        {
            Debug.LogWarning("SaveSlotManager: cannot import while in a special mode.");
            return;
        }

        if (isFileDialogOpen || isFileOperationInProgress)
        {
            Debug.LogWarning("SaveSlotManager: Cannot open import dialog while another file operation is active.");
            return;
        }

        // Get extension from FileDataService, normalize to something like "lummm"
        string saveExtension = FileDataService.SaveExtension;

        if (string.IsNullOrWhiteSpace(saveExtension))
        {
            Debug.LogWarning("SaveSlotManager: FileDataService.SaveExtension is empty, using default 'lummm'.");
            saveExtension = "lummm";
        }

        // If SaveExtension is like ".lummm", remove the dot
        if (saveExtension.StartsWith("."))
            saveExtension = saveExtension.Substring(1);

        isFileDialogOpen = true;
        isFileOperationInProgress = true;

        FilePicker.OpenFileForImport(path =>
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("SaveSlotManager: Import cancelled or no file selected.");
                StartCoroutine(ResetFileDialogFlagsNextFrame());
                return;
            }

            Debug.Log($"SaveSlotManager: Import selected file: {path}");
            ImportIntoFocusedSlot(path);

            StartCoroutine(ResetFileDialogFlagsNextFrame());
        }, saveExtension);
    }

    /// <summary>
    /// Called by the EXPORT button. Puts the manager into export mode.
    /// Actual export is handled when the user selects a slot.
    /// </summary>
    public void OnExportButtonClicked() => EnterExportMode();

    /// <summary>
    /// Exports the currently focused slot to a directory on disk.
    /// Uses SaveManager.ExportSlot and a default export directory.
    /// </summary>
    public void ExportFocusedSlot()
    {
        int slotIndex = FocusedSlotIndex;

        if (!SaveManager.SlotExists(slotIndex))
        {
            Debug.LogWarning("SaveSlotManager: Cannot export an empty slot.");
            return;
        }

        string directory = string.IsNullOrWhiteSpace(defaultExportDirectory)
            ? Path.Combine(Application.persistentDataPath, "ExportedSaves")
            : defaultExportDirectory;

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string fileNameWithoutExtension = $"save_slot_{slotIndex}_export";
        string exportedPath = SaveManager.ExportSlot(slotIndex, directory, fileNameWithoutExtension);

        if (string.IsNullOrEmpty(exportedPath))
        {
            Debug.LogWarning("SaveSlotManager: Export failed. See previous logs for details.");
        }
        else
        {
            Debug.Log($"SaveSlotManager: Exported slot {slotIndex} to '{exportedPath}'.");
        }
    }

    /// <summary>
    /// Imports a save file from a given path into the currently focused slot.
    /// This expects a valid filePath (for example, selected from a file browser).
    /// </summary>
    public void ImportIntoFocusedSlot(string sourceFilePath)
    {
        int slotIndex = FocusedSlotIndex;

        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            Debug.LogWarning("SaveSlotManager: ImportIntoFocusedSlot called with an empty path.");
            return;
        }

        bool ok = SaveManager.ImportSlot(slotIndex, sourceFilePath);

        if (!ok)
        {
            Debug.LogWarning($"SaveSlotManager: Import into slot {slotIndex} failed.");
            return;
        }

        FixDefaultProfileNameForSlot(slotIndex);

        // Refresh UI for this slot after import
        if (slotCards != null &&
            slotIndex >= 0 &&
            slotIndex < slotCards.Length &&
            slotCards[slotIndex] != null)
        {
            slotCards[slotIndex].Refresh(slotIndex);
        }

        Debug.Log($"SaveSlotManager: Imported save into slot {slotIndex} from '{sourceFilePath}'.");
    }

    /// <summary>
    /// Convenience method if you want to import into a specific slot from another script.
    /// </summary>
    public void ImportIntoSlot(int slotIndex, string sourceFilePath)
    {
        FocusSlot(slotIndex);
        ImportIntoFocusedSlot(sourceFilePath);
    }

    /// <summary>
    /// Convenience method if you want to export a specific slot from another script.
    /// </summary>
    public void ExportSlot(int slotIndex)
    {
        FocusSlot(slotIndex);
        ExportFocusedSlot();
    }
    #endregion

    #region Naming / Renaming
    public void BeginRenameFocusedSlot()
    {
        if (renamePopup == null) return;

        int slotIndex = FocusedSlotIndex >= 0 ? FocusedSlotIndex : LastFocusedSlotIndex;
        slotIndex = Mathf.Clamp(slotIndex, 0, slotCards.Length - 1);

        if (!SaveManager.SlotExists(slotIndex))
        {
            Debug.Log("SaveSlotManager: Cannot rename an empty slot.");
            return;
        }

        // Unlock BEFORE showing popup so navigation works.
        if (uiInputLock != null && uiInputLock.GetLockCount() > 0)
            uiInputLock.Unlock(restoreSelection: false);

        string currentName = GetSlotProfileName(slotIndex);
        renamePopup.OpenForRename(slotIndex, currentName);
    }

    private void OnPopupNameConfirmed(int slotIndex, string newName)
    {
        // If we were in Rename mode, rename and exit mode
        if (CurrentMode == InteractionMode.RenameSelectTarget)
        {
            RenameSlot(slotIndex, newName);
            SetMode(InteractionMode.Normal);
            return;
        }

        // Otherwise, Create flow is handled by PipeEnterSequence (see below)
        // so we typically do nothing here for create.
    }

    private void OnPopupCancelled()
    {
        // If we were renaming, exit rename mode on cancel
        if (CurrentMode == InteractionMode.RenameSelectTarget)
            SetMode(InteractionMode.Normal);
    }

    private string GetSlotProfileName(int slotIndex)
    {
        int prevSlot = SaveManager.CurrentSlot;
        SaveManager.Load(slotIndex);

        string name = SaveManager.Current != null ? SaveManager.Current.profileName : "";
        if (string.IsNullOrWhiteSpace(name))
            name = SaveSlotNaming.DefaultNameFor((SaveSlotId)slotIndex);

        if (prevSlot != slotIndex)
            SaveManager.Load(prevSlot);

        return name;
    }

    private void RenameSlot(int slotIndex, string newName)
    {
        int prevSlot = SaveManager.CurrentSlot;

        SaveManager.Load(slotIndex);

        if (SaveManager.Current != null)
        {
            SaveManager.Current.profileName = newName;
            SaveManager.Save();
        }

        if (slotCards != null && slotIndex >= 0 && slotIndex < slotCards.Length && slotCards[slotIndex] != null)
            slotCards[slotIndex].Refresh(slotIndex);

        if (prevSlot != slotIndex)
            SaveManager.Load(prevSlot);
    }
    #endregion


    #region File Dialog State Helpers
    /// <summary>
    /// Resets file dialog flags on the next frame.
    /// This ensures that the Submit input which closed the dialog
    /// is not immediately reused to trigger Play/scene transitions.
    /// </summary>
    private IEnumerator ResetFileDialogFlagsNextFrame()
    {
        // Wait one frame so the input system can "consume" the last Submit
        yield return null;

        isFileDialogOpen = false;
        isFileOperationInProgress = false;
    }
    #endregion
}