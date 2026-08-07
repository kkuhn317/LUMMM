using System;
using UnityEngine;

// This chain MUST mirror the one inside OpenFileForImport exactly. Deriving a
// "simpler" equivalent is how this file broke: with the build target set to
// WebGL, the Editor defines BOTH UNITY_EDITOR and UNITY_WEBGL, so a guard of
// (UNITY_STANDALONE || UNITY_EDITOR) && !UNITY_WEBGL drops the using while the
// body still selects its UNITY_EDITOR branch and calls into SFB.
#if UNITY_WEBGL && !UNITY_EDITOR
// WebGL player: SFB is unusable here, no using needed.
#elif UNITY_STANDALONE || UNITY_EDITOR
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
    ///
    /// NOTE: WebGL is deliberately NOT handled here. A browser file dialog can
    /// only be opened from inside a real user gesture, so it must be armed from
    /// OnPointerDown (WebGLImportPointerTrigger -> SaveSlotManager.BeginWebGLImport
    /// -> WebGLFilePicker.Arm), not from a synchronous call like this one.
    /// </summary>
    public static void OpenFileForImport(Action<string> onFileSelected, string extensionWithoutDot = "lummm")
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // Previously this branch fell through to StandaloneFileBrowser.OpenFilePanel.
        // That throws a NullReferenceException on WebGL: StandaloneFileBrowser's
        // static constructor has no UNITY_WEBGL case, so _platformWrapper stays
        // null. (StandaloneFileBrowserWebGL exists but throws
        // NotImplementedException from every method and is never instantiated.)
        Debug.LogError(
            "FilePicker.OpenFileForImport is not usable on WebGL. Use " +
            "WebGLImportPointerTrigger on the slot cards instead."
        );

        onFileSelected?.Invoke(null);

#elif UNITY_STANDALONE || UNITY_EDITOR
        // Desktop + Editor: Netherlands3D/FileBrowser

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

        NativeFilePicker.PickFile(
            (path) =>
            {
                // path will be null if user cancelled
                onFileSelected?.Invoke(path);
            }
        );

#else
        Debug.LogWarning("FilePicker.OpenFileForImport: platform not supported yet.");
        onFileSelected?.Invoke(null);
#endif
    }
}