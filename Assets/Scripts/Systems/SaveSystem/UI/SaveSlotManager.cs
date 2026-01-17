using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        ExportSelectSource
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

    public int FocusedSlotIndex { get; private set; } = 0;

    // For copy mode (source slot chosen in step 1)
    private int copySourceIndex = -1;

    // File dialog / file operation state
    private bool isFileDialogOpen = false; // True while any file dialog is open
    private bool isFileOperationInProgress = false; // Used to absorb the Submit right after closing the dialog

    private void Start()
    {
        RefreshAllSlots();
        FocusSlot(0);
    }

    #region Refresh & Focus
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
        if (slotCards == null || slotCards.Length == 0) return;

        FocusedSlotIndex = Mathf.Clamp(index, 0, slotCards.Length - 1);

        for (int i = 0; i < slotCards.Length; i++)
        {
            if (slotCards[i] != null)
            {
                slotCards[i].UpdateFocusVisual();
            }
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

        // Cancel any previous special mode and enter Delete mode
        CancelCurrentMode();
        CurrentMode = InteractionMode.Delete;
        Debug.Log("SaveSlotManager: DELETE MODE enabled. Next submit on a slot will delete.");
    }

    /// <summary>
    /// Called by the Copy button.
    /// First press enters Copy (select source) mode.
    /// Pressing again while in Copy mode cancels and returns to Normal.
    /// </summary>
    public void EnterCopyMode()
    {
        // Toggle behavior: pressing the same button again cancels the process
        if (CurrentMode == InteractionMode.CopySelectSource ||
            CurrentMode == InteractionMode.CopySelectDestination)
        {
            CancelCurrentMode();
            return;
        }

        // Cancel any previous special mode and enter Copy (select source) mode
        CancelCurrentMode();
        CurrentMode = InteractionMode.CopySelectSource;
        copySourceIndex = -1;
        Debug.Log("SaveSlotManager: COPY MODE enabled. Select a source slot.");
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
            CancelCurrentMode();
            return;
        }

        CancelCurrentMode();
        CurrentMode = InteractionMode.ImportSelectDestination;
        Debug.Log("SaveSlotManager: IMPORT MODE enabled. Select a destination slot for import.");
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
            CancelCurrentMode();
            return;
        }

        CancelCurrentMode();
        CurrentMode = InteractionMode.ExportSelectSource;
        Debug.Log("SaveSlotManager: EXPORT MODE enabled. Select a slot to export.");
    }

    /// <summary>
    /// Cancels any current special mode (Delete, Copy, Import, Export) and returns to Normal mode.
    /// Should be called by the Cancel input (e.g., B / Esc).
    /// </summary>
    public void CancelCurrentMode()
    {
        bool wasSpecialMode = CurrentMode != InteractionMode.Normal;

        CurrentMode = InteractionMode.Normal;
        copySourceIndex = -1;

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

            case InteractionMode.Normal:
            default:
                PlaySlot(index);
                break;
        }
    }
    #endregion

    #region Mode Logic
    private bool SlotHasFile(int index)
    {
        return SaveManager.SlotExists(index);
    }

    private void HandleDeleteMode(int index)
    {
        if (SlotHasFile(index))
        {
            DeleteFocusedSlot();
        }
        else
        {
            Debug.Log("SaveSlotManager: No save file in this slot to delete.");
        }

        // Always go back to normal after one attempt
        CurrentMode = InteractionMode.Normal;
    }

    private void HandleCopySelectSource(int index)
    {
        if (!SlotHasFile(index))
        {
            Debug.Log("SaveSlotManager: Cannot copy from an empty slot.");
            return;
        }

        copySourceIndex = index;
        CurrentMode = InteractionMode.CopySelectDestination;

        Debug.Log($"SaveSlotManager: Source slot set to {index}. Now select destination slot.");
    }

    private void HandleCopySelectDestination(int index)
    {
        if (copySourceIndex < 0)
        {
            // Something went wrong; fall back to normal
            Debug.LogWarning("SaveSlotManager: Copy destination selected but source index is invalid. Returning to normal mode.");
            CurrentMode = InteractionMode.Normal;
            PlaySlot(index);
            return;
        }

        int fromIndex = copySourceIndex;
        int toIndex = index;

        if (fromIndex == toIndex)
        {
            // Copying onto itself – simply cancel copy.
            Debug.Log("SaveSlotManager: Destination equals source. Copy cancelled.");
            CurrentMode = InteractionMode.Normal;
            copySourceIndex = -1;
            return;
        }

        CopyInternal(fromIndex, toIndex);

        // After copying, go back to normal mode
        CurrentMode = InteractionMode.Normal;
        copySourceIndex = -1;
    }

    private void HandleImportDestination(int index)
    {
        int slotIndex = Mathf.Clamp(index, 0, slotCards.Length - 1);

        // Leave special mode immediately (we only want one import attempt)
        CurrentMode = InteractionMode.Normal;

        // Prevent opening multiple dialogs or interfering with an ongoing file operation
        if (isFileDialogOpen || isFileOperationInProgress)
        {
            Debug.LogWarning("SaveSlotManager: Cannot open import dialog while another file operation is active.");
            return;
        }

        // Get extension from FileDataService (e.g. ".lummm" or "lummm")
        string saveExtension = FileDataService.SaveExtension;

        if (string.IsNullOrWhiteSpace(saveExtension))
        {
            Debug.LogWarning("SaveSlotManager: FileDataService.SaveExtension is empty, using default 'lummm'.");
            saveExtension = "lummm";
        }

        if (saveExtension.StartsWith("."))
            saveExtension = saveExtension.Substring(1);

        isFileDialogOpen = true;
        isFileOperationInProgress = true;

        // Capture the chosen slot in a local so the callback always imports to that one
        FilePicker.OpenFileForImport(path =>
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("SaveSlotManager: Import cancelled or no file selected.");
                StartCoroutine(ResetFileDialogFlagsNextFrame());
                return;
            }

            Debug.Log($"SaveSlotManager: Import selected file: {path} into slot {slotIndex}");
            ImportIntoSlot(slotIndex, path);

            StartCoroutine(ResetFileDialogFlagsNextFrame());
        }, saveExtension);
    }

    private void HandleExportSource(int index)
    {
        int slotIndex = Mathf.Clamp(index, 0, slotCards.Length - 1);

        if (!SaveManager.SlotExists(slotIndex))
        {
            Debug.LogWarning("SaveSlotManager: Cannot export an empty slot.");
            CurrentMode = InteractionMode.Normal;
            return;
        }

        // Optionally focus the chosen slot for visual feedback
        FocusSlot(slotIndex);

        // Open an export dialog / share sheet depending on platform
        ShowExportDialogForSlot(slotIndex);

        // Back to normal after one export attempt
        CurrentMode = InteractionMode.Normal;
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
    #endregion

    #region Original Behaviour
    public void PlaySlot(int index)
    {
        // Safety: do not allow scene transitions while in a special mode
        if (CurrentMode != InteractionMode.Normal)
        {
            Debug.LogWarning($"PlaySlot was called while in mode {CurrentMode}. Ignoring play request.");
            return;
        }

        // Also prevent scene transitions right when a file operation just finished
        if (isFileDialogOpen || isFileOperationInProgress)
        {
            Debug.LogWarning("PlaySlot was called while a file dialog is open or just closed. Ignoring play request.");
            return;
        }

        ActiveSlotIndex = index;

        // This will either load an existing save or create a new one for an empty slot
        SaveManager.Load(index);

        // Ensure UI for this slot is updated now, so it stops showing "NEW"
        if (slotCards != null &&
            index >= 0 &&
            index < slotCards.Length &&
            slotCards[index] != null)
        {
            slotCards[index].Refresh(index);
        }

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