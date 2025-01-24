using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[System.Serializable]
public class RebindLayoutData
{
    public Dictionary<string, string> layouts = new Dictionary<string, string>(); // LayoutName -> Rebind JSON
}


public class RebindSaveLoad : MonoBehaviour
{
    private const string LayoutsKey = "rebindLayouts";
    private const string CurrentLayoutKey = "currentLayout";

    public InputActionAsset actions;

    [Header("UI Elements")]
    public TMP_Dropdown layoutDropdown; // Dropdown for selecting layouts
    public TMP_InputField layoutNameInput; // Input field for new layout name
    public Button saveLayoutButton; // Button to confirm saving

    private RebindLayoutData loadedLayouts = new RebindLayoutData();
    private string currentLoadedLayout = ""; // Track the currently loaded layout

    private void Awake()
    {
        LoadLayouts();
        LoadCurrentLayout();

        // Populate dropdown with available layouts
        RefreshDropdown();

        // Add listener for dropdown selection change
        layoutDropdown.onValueChanged.AddListener(delegate { OnDropdownSelectionChanged(); });

        // Add listener for save button
        saveLayoutButton.onClick.AddListener(() => SaveNewLayout());
    }

    /*public void OnEnable()
    {
        var rebinds = PlayerPrefs.GetString("rebinds");
        if (!string.IsNullOrEmpty(rebinds))
            actions.LoadBindingOverridesFromJson(rebinds);
    }

    public void OnDisable()
    {
        var rebinds = actions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("rebinds", rebinds);
    }*/
    
    /// <summary>
    /// Saves the current bindings as a new layout.
    /// </summary>
    public void SaveNewLayout()
    {
        string layoutName = layoutNameInput.text.Trim();
        if (string.IsNullOrEmpty(layoutName))
        {
            Debug.LogError("Layout name cannot be empty.");
            return;
        }

        if (loadedLayouts.layouts.ContainsKey(layoutName))
        {
            Debug.LogError($"Layout '{layoutName}' already exists. Choose a different name.");
            return;
        }

        SaveCurrentBindings(layoutName);

        // Update UI
        RefreshDropdown();
        layoutDropdown.value = layoutDropdown.options.FindIndex(option => option.text == layoutName);
    }

    /// <summary>
    /// Saves the current bindings under a specified layout name.
    /// </summary>
    public void SaveCurrentBindings(string layoutName)
    {
        string rebindJson = actions.SaveBindingOverridesAsJson();
        loadedLayouts.layouts[layoutName] = rebindJson;

        string json = JsonUtility.ToJson(loadedLayouts);
        PlayerPrefs.SetString(LayoutsKey, json);
        PlayerPrefs.Save();

        Debug.Log($"Saved layout '{layoutName}'.");
    }

    /// <summary>
    /// Loads a specific layout if it exists.
    /// </summary>
    public void LoadLayout(string layoutName)
    {
        if (loadedLayouts.layouts.TryGetValue(layoutName, out string rebindJson))
        {
            actions.LoadBindingOverridesFromJson(rebindJson);
            PlayerPrefs.SetString(CurrentLayoutKey, layoutName);
            PlayerPrefs.Save();

            currentLoadedLayout = layoutName; // Track loaded layout
            Debug.Log($"Loaded layout '{layoutName}'.");
        }
        else
        {
            Debug.LogError($"Layout '{layoutName}' not found.");
        }
    }

    /// <summary>
    /// Called when the dropdown selection changes.
    /// Loads the selected layout.
    /// </summary>
    private void OnDropdownSelectionChanged()
    {
        string selectedLayout = layoutDropdown.options[layoutDropdown.value].text;
        LoadLayout(selectedLayout);
    }

    /// <summary>
    /// Updates the dropdown list with available layouts.
    /// </summary>
    private void RefreshDropdown()
    {
        layoutDropdown.ClearOptions();
        List<string> layoutNames = GetSavedLayouts();

        if (layoutNames.Count == 0)
        {
            layoutNames.Add("Default"); // Ensure there's at least one option
        }

        layoutDropdown.AddOptions(layoutNames);

        // Set dropdown to currently loaded layout
        int selectedIndex = layoutNames.IndexOf(currentLoadedLayout);
        layoutDropdown.value = selectedIndex >= 0 ? selectedIndex : 0;
    }

    /// <summary>
    /// Loads all stored layouts from PlayerPrefs.
    /// </summary>
    private void LoadLayouts()
    {
        string json = PlayerPrefs.GetString(LayoutsKey, "{}");
        loadedLayouts = JsonUtility.FromJson<RebindLayoutData>(json);
    }

    /// <summary>
    /// Loads the currently selected layout if it exists.
    /// </summary>
    private void LoadCurrentLayout()
    {
        string currentLayout = PlayerPrefs.GetString(CurrentLayoutKey, "");
        if (!string.IsNullOrEmpty(currentLayout))
        {
            LoadLayout(currentLayout);
        }
    }

    /// <summary>
    /// Returns a list of all saved layouts.
    /// </summary>
    public List<string> GetSavedLayouts()
    {
        return new List<string>(loadedLayouts.layouts.Keys);
    }
}