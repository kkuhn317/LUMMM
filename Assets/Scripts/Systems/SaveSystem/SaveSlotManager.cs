using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveSlotManager : MonoBehaviour
{
    public static int ActiveSlotIndex { get; private set; } = 0;

    [Header("UI Cards for A, B, C")]
    public SaveFileUI[] slotCards; // 0 = A, 1 = B, 2 = C

    [Header("Where to go after selecting a file")]
    public string levelSelectSceneName = "LevelSelect"; // set in inspector

    [Header("Export folder name (inside persistentDataPath)")]
    public string exportFolderName = "Exports";

    // <-- this is what SaveFileUI is trying to read
    public int FocusedSlotIndex { get; private set; } = 0;

    private void Start()
    {
        RefreshAllSlots();
        FocusSlot(0); // default focus on slot A
    }

    public void RefreshAllSlots()
    {
        for (int i = 0; i < slotCards.Length; i++)
        {
            if (slotCards[i] != null)
                slotCards[i].Refresh(i);
        }
    }

    // <-- this is what SaveFileUI calls when you click a card
    public void FocusSlot(int index)
    {
        FocusedSlotIndex = Mathf.Clamp(index, 0, slotCards.Length - 1);

        for (int i = 0; i < slotCards.Length; i++)
        {
            if (slotCards[i] != null)
                slotCards[i].UpdateFocusVisual();
        }

        Debug.Log($"Focused slot: {FocusedSlotIndex}");
    }

    // Called from side "Play" button
    public void PlayFocusedSlot()
    {
        PlaySlot(FocusedSlotIndex);
    }

    // <-- this is what SaveFileUI.OnClickPlay() uses
    public void PlaySlot(int index)
    {
        ActiveSlotIndex = index;
        SaveManager.Load(index);

        if (string.IsNullOrEmpty(SaveManager.Current.profileName))
        {
            SaveManager.Current.profileName = $"File {(char)('A' + index)}";
            SaveManager.Save();
        }

        SceneManager.LoadScene(levelSelectSceneName);
    }

    // ---- DELETE ----
    public void DeleteFocusedSlot()
    {
        int index = FocusedSlotIndex;
        string path = SaveManager.GetSlotFilePath(index);
        if (File.Exists(path))
            File.Delete(path);

        if (index == ActiveSlotIndex)
            SaveManager.Load(index);

        RefreshAllSlots();
    }

    // ---- COPY (focused -> targetIndex) ----
    public void CopyFocusedTo(int targetIndex)
    {
        int fromIndex = FocusedSlotIndex;
        int toIndex = Mathf.Clamp(targetIndex, 0, slotCards.Length - 1);
        if (fromIndex == toIndex) return;

        string fromPath = SaveManager.GetSlotFilePath(fromIndex);
        string toPath = SaveManager.GetSlotFilePath(toIndex);

        if (!File.Exists(fromPath))
        {
            Debug.LogWarning("CopyFocusedTo: source slot is empty");
            return;
        }

        string dir = Path.GetDirectoryName(toPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.Copy(fromPath, toPath, overwrite: true);

        if (toIndex == ActiveSlotIndex)
            SaveManager.Load(toIndex);

        RefreshAllSlots();
    }

    // ---- EXPORT focused ----
    public void ExportFocusedSlot()
    {
        int slot = FocusedSlotIndex;
        string exportDir = Path.Combine(Application.persistentDataPath, exportFolderName);
        string fileName  = $"LevelUp_File_{(char)('A' + slot)}";

        string exportedPath = SaveManager.ExportSlot(slot, exportDir, fileName);
        if (exportedPath != null)
            Debug.Log($"Exported slot {slot} to: {exportedPath}");
        else
            Debug.LogWarning($"Export failed for slot {slot}");
    }

    // ---- IMPORT (simple version, give it a path) ----
    public void ImportFocusedSlotFromPath(string sourceFilePath)
    {
        int slot = FocusedSlotIndex;
        bool ok = SaveManager.ImportSlot(slot, sourceFilePath);
        if (ok)
        {
            Debug.Log($"Imported into slot {slot} from: {sourceFilePath}");
            RefreshAllSlots();
            FocusSlot(slot);
        }
        else
        {
            Debug.LogWarning($"Import failed from: {sourceFilePath}");
        }
    }
}