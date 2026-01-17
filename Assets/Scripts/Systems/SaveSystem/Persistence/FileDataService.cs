using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class FileDataService : IDataService
{
    private const string SaveFolderName = "Saves";
    public const string SaveExtension = ".lummm";

    private readonly ISerializer _serializer;

    public FileDataService(ISerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    private string GetSavesDirectory()
    {
        string dir = Path.Combine(Application.persistentDataPath, SaveFolderName);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }

    public string GetSavePath(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Save file name cannot be null or empty.", nameof(name));

        string dir = GetSavesDirectory();

        if (!name.EndsWith(SaveExtension, StringComparison.OrdinalIgnoreCase))
            name += SaveExtension;

        return Path.Combine(dir, name);
    }

    private string GetFileName(SaveData data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        if (string.IsNullOrWhiteSpace(data.profileName))
            throw new ArgumentException("SaveData.profileName is null or empty. Set it before saving.");

        return data.profileName;
    }

    public void Save(SaveData data, bool overwrite = true)
    {
        Save(data, GetFileName(data), overwrite);
    }

    public void Save(SaveData data, string name, bool overwrite = true)
    {
        try
        {
            string filePath = GetSavePath(name);

            if (File.Exists(filePath) && !overwrite)
            {
                throw new IOException(
                    $"[FileDataService] Save failed: file already exists and overwrite is false: {filePath}");
            }

            string json = _serializer.Serialize(data);
            File.WriteAllText(filePath, json);

            Debug.Log($"[FileDataService] Saved game to '{filePath}'.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FileDataService] Save error: {ex}");
        }
    }

    public SaveData Load(string name)
    {
        try
        {
            string filePath = GetSavePath(name);

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[FileDataService] Load failed: file not found: {filePath}");
                return null;
            }

            string json = File.ReadAllText(filePath);
            SaveData data = _serializer.Deserialize<SaveData>(json);

            if (data == null)
            {
                Debug.LogWarning($"[FileDataService] Load warning: deserialized SaveData is null for '{filePath}'.");
            }

            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FileDataService] Load error: {ex}");
            return null;
        }
    }

    public void Delete(string name)
    {
        try
        {
            string filePath = GetSavePath(name);

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[FileDataService] Delete failed: file not found: {filePath}");
                return;
            }

            File.Delete(filePath);
            Debug.Log($"[FileDataService] Deleted save '{name}'.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FileDataService] Delete error: {ex}");
        }
    }

    public void DeleteAll()
    {
        try
        {
            string dir = GetSavesDirectory();
            string[] files = Directory.GetFiles(dir, "*" + SaveExtension);

            foreach (string file in files)
            {
                File.Delete(file);
            }

            Debug.Log($"[FileDataService] Deleted all saves in '{dir}'.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FileDataService] DeleteAll error: {ex}");
        }
    }

    public IEnumerable<string> ListSavedGames()
    {
        try
        {
            string dir = GetSavesDirectory();
            if (!Directory.Exists(dir))
            {
                return Enumerable.Empty<string>();
            }

            string[] files = Directory.GetFiles(dir, "*" + SaveExtension);

            return files
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FileDataService] ListSavedGames error: {ex}");
            return Enumerable.Empty<string>();
        }
    }

    public void Rename(string oldName, string newName)
    {
        try
        {
            string oldPath = GetSavePath(oldName);
            string newPath = GetSavePath(newName);

            if (!File.Exists(oldPath))
            {
                Debug.LogWarning($"[FileDataService] Rename failed: source file not found: {oldPath}");
                return;
            }

            if (File.Exists(newPath))
            {
                throw new IOException(
                    $"[FileDataService] Rename failed: target file already exists: {newPath}");
            }

            File.Move(oldPath, newPath);
            Debug.Log($"[FileDataService] Renamed save from '{oldName}' to '{newName}'.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FileDataService] Rename error: {ex}");
        }
    }

    public void Copy(string sourceName, string destName)
    {
        try
        {
            string sourcePath = GetSavePath(sourceName);
            string destPath   = GetSavePath(destName);

            if (!File.Exists(sourcePath))
            {
                Debug.LogWarning($"[FileDataService] Copy failed: source file not found: {sourcePath}");
                return;
            }

            if (File.Exists(destPath))
            {
                throw new IOException(
                    $"[FileDataService] Copy failed: destination file already exists: {destPath}");
            }

            File.Copy(sourcePath, destPath);
            Debug.Log($"[FileDataService] Copied save from '{sourceName}' to '{destName}'.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FileDataService] Copy error: {ex}");
        }
    }

    public void Import(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[FileDataService] Import failed: file not found: {filePath}");
                return;
            }

            string json = File.ReadAllText(filePath);
            SaveData deserialized = null;

            try
            {
                deserialized = _serializer.Deserialize<SaveData>(json);
            }
            catch (Exception innerEx)
            {
                Debug.LogWarning(
                    $"[FileDataService] Import validation failed: JSON is not valid SaveData. {innerEx}");
            }

            if (deserialized == null)
            {
                Debug.LogWarning("[FileDataService] Import validation failed: deserialized SaveData is null.");
                return;
            }

            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string destPath = GetSavePath(fileName);

            File.Copy(filePath, destPath, overwrite: true);

            Debug.Log($"[FileDataService] Imported save from '{filePath}' as '{fileName}'.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FileDataService] Import error: {ex}");
        }
    }

    public void Export(string name, string filePath)
    {
        try
        {
            // Resolve the internal source path for the save slot/file
            string sourcePath = GetSavePath(name);

            if (!File.Exists(sourcePath))
            {
                Debug.LogWarning($"[FileDataService] Export failed: source save not found: {sourcePath}");
                return;
            }

            string targetPath = filePath;

            // If the provided path is a directory (or looks like one), build the full file path inside it
            if (Directory.Exists(filePath) ||
                filePath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                filePath.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                string dir = filePath;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string fileName = name.EndsWith(SaveExtension, StringComparison.OrdinalIgnoreCase)
                    ? name
                    : name + SaveExtension;

                targetPath = Path.Combine(dir, fileName);
            }
            else
            {
                // Ensure the target directory exists if a full file path was passed
                string dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

            // Copy the file and overwrite if it already exists.
            // Export is treated as "update this backup file" rather than "fail if it exists".
            File.Copy(sourcePath, targetPath, overwrite: true);

            Debug.Log($"[FileDataService] Exported save '{name}' to '{targetPath}'.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FileDataService] Export error: {ex}");
        }
    }
}