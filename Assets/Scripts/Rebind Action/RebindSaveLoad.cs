using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Localization.Settings;

[System.Serializable]
public class RebindLayoutData
{
    public Dictionary<string, string> layouts = new Dictionary<string, string>(); // LayoutName -> Rebind JSON
}


public class RebindSaveLoad : MonoBehaviour
{
    private const string LayoutsKey = "rebindLayouts";
    private const string CurrentLayoutKey = "currentLayout";
    private const string DefaultLayoutName = "DEFAULT";

    public InputActionAsset actions;

    [Header("UI Elements")]
    public TMP_Dropdown layoutDropdown; // Dropdown for selecting layouts
    public TMP_InputField layoutNameInput; // Input field for new layout name
     public GameObject createLayout;
    public Button createLayoutConfirmButton; // Button to confirm creating
    public GameObject editLayout;
    public TMP_InputField editLayoutNameInput;
    public Button editLayoutConfirmButton;
    public Button deleteLayoutButton; // Button to delete layout
    public TMP_Text errorText;

    private RebindLayoutData loadedLayouts = new RebindLayoutData();
    private string currentLoadedLayout = DefaultLayoutName; // Track the currently loaded layout

    private void Awake()
    {
        LoadLayouts();
        LoadCurrentLayout();

        // Populate dropdown with available layouts
        RefreshDropdown();

        // Add listener for dropdown selection change
        layoutDropdown.onValueChanged.AddListener(delegate { OnDropdownSelectionChanged(); });

        // Add listener for save button
        createLayoutConfirmButton.onClick.AddListener(() => SaveNewLayout());

        // Add listener for delete button
        deleteLayoutButton.onClick.AddListener(() => DeleteCurrentLayout());

        // Add listener for edit layout button
        editLayoutConfirmButton.onClick.AddListener(() => EditLayoutName());

        // Force uppercase input in both fields
        layoutNameInput.onValueChanged.AddListener(delegate { ForceUppercase(layoutNameInput); });
        editLayoutNameInput.onValueChanged.AddListener(delegate { ForceUppercase(editLayoutNameInput); });
    }

    private void ForceUppercase(TMP_InputField inputField)
    {
        inputField.text = inputField.text.ToUpper();
    }

    private void DisplayError(string messageKey)
    {
        string localizedMessage = LocalizationSettings.StringDatabase.GetLocalizedString("RebindError_" + messageKey);
        errorText.text = localizedMessage; // Set error text in UI
        errorText.gameObject.SetActive(true); 
    }

    public void OnDisable()
    {
        // Save current bindings to layout
        SaveCurrentBindings(currentLoadedLayout);
        // Save all layouts to PlayerPrefs
        string json = JsonConvert.SerializeObject(loadedLayouts);
        PlayerPrefs.SetString(LayoutsKey, json);

        Debug.Log("Saved all layouts to PlayerPrefs.");
    }
    
    /// <summary>
    /// Saves the current bindings as a new layout.
    /// </summary>
    public void SaveNewLayout()
    {
        // Save current bindings to current layout
        SaveCurrentBindings(currentLoadedLayout);

        // Get the new layout name
        string layoutName = layoutNameInput.text.Trim();
        if (string.IsNullOrEmpty(layoutName))
        {
            DisplayError("EmptyLayoutName");
            return;
        }

        if (loadedLayouts.layouts.ContainsKey(layoutName))
        {
            DisplayError("LayoutExists");
            return;
        }

        // Reset all bindings to default
        foreach (var actionMap in actions.actionMaps)
        {
            actionMap.RemoveAllBindingOverrides();
        }
        // Save the new layout (default bindings)
        SaveCurrentBindings(layoutName);

        currentLoadedLayout = layoutName; // Update current loaded layout

        // Update UI
        RefreshDropdown();
        layoutDropdown.value = layoutDropdown.options.FindIndex(option => option.text == layoutName);

        // Deactivate the create layout once you finish
        createLayout.SetActive(false);
        errorText.gameObject.SetActive(false);
        // NOTE: This has the side effect of calling OnDropdownSelectionChanged()
        // Which currently saves the new layout again. This is fine for now.
    }

    /// <summary>
    /// Saves the current bindings under a specified layout name.
    /// </summary>
    public void SaveCurrentBindings(string layoutName)
    {
        Debug.Log($"Saving current bindings to layout '{layoutName}'");
        string rebindJson = actions.SaveBindingOverridesAsJson();
        loadedLayouts.layouts[layoutName] = rebindJson;

        string json = JsonConvert.SerializeObject(loadedLayouts);
        PlayerPrefs.SetString(LayoutsKey, json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Loads a specific layout if it exists.
    /// </summary>
    public void LoadLayout(string layoutName)
    {
        Debug.Log($"Loading layout '{layoutName}'");
        if (loadedLayouts.layouts.TryGetValue(layoutName, out string rebindJson))
        {
            actions.LoadBindingOverridesFromJson(rebindJson);
            PlayerPrefs.SetString(CurrentLayoutKey, layoutName);
            PlayerPrefs.Save();

            currentLoadedLayout = layoutName; // Track loaded layout
            editLayoutNameInput.text = layoutName; // edit the layout name input field
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
        // Save current bindings to the currently loaded layout before switching
        SaveCurrentBindings(currentLoadedLayout);
        
        // Load the new layout
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
            Debug.LogWarning("layoutNames is empty. LoadLayouts should've added at least one layout.");
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
        Debug.Log("Loading all layouts...");
        string json = PlayerPrefs.GetString(LayoutsKey, "");
        if (string.IsNullOrEmpty(json))
        {
            InitDefaultLayout();
        } else
        {
            loadedLayouts = JsonConvert.DeserializeObject<RebindLayoutData>(json);
        }
    }

    /// <summary>
    /// Loads the currently selected layout if it exists.
    /// </summary>
    private void LoadCurrentLayout()
    {
        string currentLayout = PlayerPrefs.GetString(CurrentLayoutKey, "");
        if (string.IsNullOrEmpty(currentLayout))
        {
            // If no current layout is saved, default to the first available layout
            // This will likely be the "Default" layout
            currentLayout = GetSavedLayouts()[0];
        } else 
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

    /// <summary>
    /// Deletes the currently selected layout.
    /// </summary>
    public void DeleteCurrentLayout()
    {
        if (!string.IsNullOrEmpty(currentLoadedLayout))
        {
            loadedLayouts.layouts.Remove(currentLoadedLayout);

            // If no layouts are left, initialize the default layout
            if (loadedLayouts.layouts.Count == 0)
            {
                InitDefaultLayout();    // Also saves to PlayerPrefs
            } else {
                // Set current layout to the first available layout or empty
                string nextLayout = loadedLayouts.layouts.Count > 0 ? GetSavedLayouts()[0] : "";
                LoadLayout(nextLayout); // Also saves to PlayerPrefs
            }

            // Refresh the dropdown
            RefreshDropdown();
        }
    }

    /// <summary>
    /// Renames the currently selected layout.
    /// </summary>
    public void EditLayoutName()
    {
        string newLayoutName = editLayoutNameInput.text.Trim();

        if (string.IsNullOrEmpty(newLayoutName))
        {
            DisplayError("EmptyLayoutName");
            return;
        }

        if (loadedLayouts.layouts.ContainsKey(newLayoutName))
        {
            DisplayError("LayoutExists");
            return;
        }

        if (!loadedLayouts.layouts.ContainsKey(currentLoadedLayout))
        {
            DisplayError("LayoutNotExist");
            return;
        }

        // Rename the layout
        string rebindJson = loadedLayouts.layouts[currentLoadedLayout];
        loadedLayouts.layouts.Remove(currentLoadedLayout);
        loadedLayouts.layouts[newLayoutName] = rebindJson;

        // Update the currently loaded layout name
        currentLoadedLayout = newLayoutName;

        // Save changes
        PlayerPrefs.SetString(LayoutsKey, JsonConvert.SerializeObject(loadedLayouts));
        PlayerPrefs.SetString(CurrentLayoutKey, currentLoadedLayout);
        PlayerPrefs.Save();

        // Refresh the dropdown with the new name
        RefreshDropdown();
        layoutDropdown.value = layoutDropdown.options.FindIndex(option => option.text == newLayoutName);

        // Deactivate the edit layout once you finish
        editLayout.SetActive(false);
        errorText.gameObject.SetActive(false);
        
        Debug.Log($"Renamed layout '{currentLoadedLayout}' to '{newLayoutName}'");
    }

    // <summary>
    // Adds a default layout for when the list is empty.
    // </summary>
    private void InitDefaultLayout()
    {
        // Reset all bindings to default
        foreach (var actionMap in actions.actionMaps)
        {
            actionMap.RemoveAllBindingOverrides();
        }
        SaveCurrentBindings(DefaultLayoutName); // Also saves LayoutsKey to PlayerPrefs
        currentLoadedLayout = DefaultLayoutName;

        // Save currentLoadedLayout to PlayerPrefs
        PlayerPrefs.SetString(CurrentLayoutKey, currentLoadedLayout);
        PlayerPrefs.Save();
    }
}