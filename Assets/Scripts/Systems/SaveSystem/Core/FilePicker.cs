using System;
using UnityEngine;

#if UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL
// Namespace from Netherlands3D/FileBrowser (fork of StandaloneFileBrowser)
using SFB;
#endif

public static class FilePicker
{
    /// <summary>
    /// Opens a cross-platform file picker for importing a save file.
    /// onFileSelected will receive:
    ///  - a valid path string, or
    ///  - null if the user cancelled or something went wrong.
    /// </summary>
    public static void OpenFileForImport(Action<string> onFileSelected, string extensionWithoutDot = "lummm")
    {
#if UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL
        // Desktop + WebGL: Netherlands3D/FileBrowser
        // In WebGL, this opens the browser file dialog and copies the file to a local path. :contentReference[oaicite:2]{index=2}

        var extensions = new[]
        {
            new ExtensionFilter("Save Files", extensionWithoutDot),
            new ExtensionFilter("All Files", "*" )
        };

        string[] paths = StandaloneFileBrowser.OpenFilePanel(
            "Import Save",
            "",
            extensions,
            false
        );

        string selectedPath = (paths != null && paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            ? paths[0]
            : null;

        onFileSelected?.Invoke(selectedPath);

#elif UNITY_ANDROID || UNITY_IOS
        // Mobile: NativeFilePicker (Android & iOS only).

        if (NativeFilePicker.IsFilePickerBusy())
        {
            Debug.LogWarning("FilePicker.OpenFileForImport: Native file picker is already busy.");
            onFileSelected?.Invoke(null);
            return;
        }

        // On Android/iOS, allowedFileTypes son MIMEs/UTIs. Para no complicarnos,
        // dejamos que el usuario elija cualquier archivo y tú validas la extensión luego.
        NativeFilePicker.PickFile(
            (path) =>
            {
                // path will be null if user cancelled
                // On mobile this is usually a temporary copy location managed by the plugin. :contentReference[oaicite:4]{index=4}
                onFileSelected?.Invoke(path);
            }
        );

#else
        Debug.LogWarning("FilePicker.OpenFileForImport: platform not supported yet.");
        onFileSelected?.Invoke(null);
#endif
    }
}