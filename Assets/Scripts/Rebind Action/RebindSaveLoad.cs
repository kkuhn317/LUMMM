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
    public string rebindJSON = "";
    public MobileRebindingData mobileData = new();
}

public class RebindSaveLoad : MonoBehaviour
{
    public const string LayoutsKey = "rebindLayouts";
    public const string CurrentLayoutKey = "currentLayout";
    public const string DefaultLayoutName = "DEFAULT";

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
    public Slider buttonPressedOpacitySlider;
    public Slider buttonUnpressedOpacitySlider;

    // Quicker reference to the layouts from GlobalVariables
    private static Dictionary<string, RebindLayoutData> LoadedLayouts {
        get => GlobalVariables.Layouts;
        set => GlobalVariables.Layouts = value;
    }

    // Quicker reference to the current loaded layout
    private static string currentLoadedLayout {
        get => GlobalVariables.currentLayoutName;
        set => GlobalVariables.currentLayoutName = value;
    }

    private void Awake()
    {
        LoadLayouts();  // Only really needed to initialize Default Layout if needed, but it also makes it easier to use in editor

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
        string json = JsonConvert.SerializeObject(LoadedLayouts);
        PlayerPrefs.SetString(LayoutsKey, json);

        Debug.Log("Saved all layouts to PlayerPrefs.");
    }

    // Called from StartupThings
    public static void OnGameStart() {
        // Get the current control layout
        currentLoadedLayout = PlayerPrefs.GetString(RebindSaveLoad.CurrentLayoutKey, "DEFAULT");
        string json = PlayerPrefs.GetString(RebindSaveLoad.LayoutsKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            try {
                LoadedLayouts = JsonConvert.DeserializeObject<Dictionary<string, RebindLayoutData>>(json);
            } catch {
                Debug.Log("Unable to load layouts. Maybe it was saved in an older format previously.");
            }
        } else {
            Debug.Log("No saved layouts found. Default layout will be created when rebind menu is opened");
        }
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

        if (LoadedLayouts.ContainsKey(layoutName))
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

        GameManager.Instance.ResumeGame();
        // Deactivate the create layout once you finish
        createLayout.SetActive(false);
        errorText.gameObject.SetActive(false);
        layoutNameInput.text = "";
        // NOTE: This has the side effect of calling OnDropdownSelectionChanged()
        // Which currently saves the new layout again. This is fine for now.
    }

    /// <summary>
    /// Saves the current bindings under a specified layout name.
    /// Also saves the opacity slider values.
    /// </summary>
    public void SaveCurrentBindings(string layoutName)
    {
        Debug.Log($"Saving current bindings to layout '{layoutName}'");
        string rebindJson = actions.SaveBindingOverridesAsJson();

        // Create new layout if not present
        if (!LoadedLayouts.ContainsKey(layoutName)) {
            LoadedLayouts[layoutName] = new();
        }
        LoadedLayouts[layoutName].rebindJSON = rebindJson;
        LoadedLayouts[layoutName].mobileData.buttonPressedOpacity = buttonPressedOpacitySlider.value;
        LoadedLayouts[layoutName].mobileData.buttonUnpressedOpacity = buttonUnpressedOpacitySlider.value;

        string json = JsonConvert.SerializeObject(LoadedLayouts);
        PlayerPrefs.SetString(LayoutsKey, json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Saves mobile bindings to the current layout.
    /// </summary>
    public static void SaveMobileBindings(Dictionary<string, MobileRebindingData.MobileButtonData> rebindings)
    {
        Debug.Log($"Saving mobile bindings to layout '{currentLoadedLayout}'");
        LoadedLayouts[currentLoadedLayout].mobileData.buttonData = rebindings;

        string json = JsonConvert.SerializeObject(LoadedLayouts);
        PlayerPrefs.SetString(LayoutsKey, json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Loads a specific layout if it exists.
    /// Also sets the opacity slider values.
    /// </summary>
    public void LoadLayout(string layoutName)
    {
        Debug.Log($"Loading layout '{layoutName}'");
        if (LoadedLayouts.TryGetValue(layoutName, out RebindLayoutData layout))
        {
            actions.LoadBindingOverridesFromJson(layout.rebindJSON);
            buttonPressedOpacitySlider.value = layout.mobileData.buttonPressedOpacity;
            buttonUnpressedOpacitySlider.value = layout.mobileData.buttonUnpressedOpacity;
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
            try {
                LoadedLayouts = JsonConvert.DeserializeObject<Dictionary<string, RebindLayoutData>>(json);
            } catch {
                print("Unable to load layouts. Maybe it was saved in an older format previously.");
                InitDefaultLayout();
            }
        }
    }

    /// <summary>
    /// Loads the currently selected layout if it exists.
    /// </summary>
    private void LoadCurrentLayout()
    {
        string currentLayout = PlayerPrefs.GetString(CurrentLayoutKey, "");
        if (string.IsNullOrEmpty(currentLayout) || !LoadedLayouts.ContainsKey(currentLayout))
        {
            // If no current layout is saved or it is not present in LoadedLayouts, default to the first available layout
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
        return new List<string>(LoadedLayouts.Keys);
    }

    /// <summary>
    /// Deletes the currently selected layout.
    /// </summary>
    public void DeleteCurrentLayout()
    {
        if (!string.IsNullOrEmpty(currentLoadedLayout))
        {
            LoadedLayouts.Remove(currentLoadedLayout);

            // If no layouts are left, initialize the default layout
            if (LoadedLayouts.Count == 0)
            {
                InitDefaultLayout();    // Also saves to PlayerPrefs
            } else {
                // Set current layout to the first available layout or empty
                string nextLayout = LoadedLayouts.Count > 0 ? GetSavedLayouts()[0] : "";
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

        if (newLayoutName == currentLoadedLayout)
        {
            // Allow renaming to the same name since it does not cause duplication
            editLayout.SetActive(false);
            errorText.gameObject.SetActive(false);
            return;
        }

        if (LoadedLayouts.ContainsKey(newLayoutName))
        {
            DisplayError("LayoutExists");
            return;
        }

        if (!LoadedLayouts.ContainsKey(currentLoadedLayout))
        {
            DisplayError("LayoutNotExist");
            return;
        }

        // Rename the layout
        RebindLayoutData layout = LoadedLayouts[currentLoadedLayout];
        LoadedLayouts.Remove(currentLoadedLayout);
        LoadedLayouts[newLayoutName] = layout;

        // Update the currently loaded layout name
        currentLoadedLayout = newLayoutName;

        // Save changes
        PlayerPrefs.SetString(LayoutsKey, JsonConvert.SerializeObject(LoadedLayouts));
        PlayerPrefs.SetString(CurrentLayoutKey, currentLoadedLayout);
        PlayerPrefs.Save();

        // Refresh the dropdown with the new name
        RefreshDropdown();
        layoutDropdown.value = layoutDropdown.options.FindIndex(option => option.text == newLayoutName);

        GameManager.Instance.ResumeGame();
        // Deactivate the edit layout once you finish
        editLayout.SetActive(false);
        errorText.gameObject.SetActive(false);
        
        Debug.Log($"Renamed layout '{currentLoadedLayout}' to '{newLayoutName}'");
    }

    /// <summary>
    /// Resets all bindings to their defaults.
    /// Also resets the sliders to their default positions.
    /// </summary>
    public void ResetBindings()
    {
        // Remove all overrides for all action maps
        foreach (var actionMap in actions.actionMaps)
        {
            actionMap.RemoveAllBindingOverrides();
        }

        buttonPressedOpacitySlider.value = MobileRebindingData.DefaultPressedOpacity;
        buttonUnpressedOpacitySlider.value = MobileRebindingData.DefaultUnpressedOpacity;
    }

    // <summary>
    // Adds a default layout for when the list is empty.
    // </summary>
    private void InitDefaultLayout()
    {
        // Reset to default
        ResetBindings();
        
        // Not calling SaveCurrentBindings because of the sliders
        // (not sure if its an issue, but being on the safe side just in case)
        string rebindJson = actions.SaveBindingOverridesAsJson();

        // Create new layout
        LoadedLayouts[DefaultLayoutName] = new()
        {
            rebindJSON = rebindJson
        };
        currentLoadedLayout = DefaultLayoutName;

        // Save layout to PlayerPrefs
        string json = JsonConvert.SerializeObject(LoadedLayouts);
        PlayerPrefs.SetString(LayoutsKey, json);

        // Save currentLoadedLayout to PlayerPrefs
        PlayerPrefs.SetString(CurrentLayoutKey, currentLoadedLayout);
        PlayerPrefs.Save();
    }
}