#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.Localization.Settings;

[InitializeOnLoad]
public static class TMP_EditorRefresh
{
    static TMP_EditorRefresh()
    {
        // Listen for when the selected locale changes in the editor
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
    }

    private static void OnSelectedLocaleChanged(UnityEngine.Localization.Locale locale)
    {
        // Find every TMP component in the current open scene
        TMP_Text[] allTextComponents = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        
        foreach (TMP_Text textComp in allTextComponents)
        {
            if (textComp != null)
            {
                // Force TMP to clear cache and rebuild the layout
                textComp.SetAllDirty();
                textComp.UpdateMeshPadding();
            }
        }
        
        // Refresh the scene view to show changes immediately
        SceneView.RepaintAll();
    }
}
#endif
