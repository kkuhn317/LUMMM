using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DropdownSettingHandler : MonoBehaviour, ISettingHandler
{
    [Header("Setting Config")]
    [SettingTypeFilter(SettingType.GraphicsQualityKey, SettingType.ResolutionKey)]
    public SettingType settingType;

    [Header("UI")]
    [SerializeField] private TMP_Dropdown dropdown;

    [Header("SFX")]
    [SerializeField] private AudioClip navigateSfx; // tick while browsing items (list open)
    [SerializeField] private AudioClip pressSfx;    // confirm when value changes

    [Header("Performance")]
    [SerializeField] private float selectionCheckInterval = 0.05f; // Check every 0.05s instead of every frame

    private int currentValue;
    public SettingType SettingType => settingType;

    private bool wasExpanded;
    private Coroutine monitorRoutine;
    private GameObject lastSelectedObject;
    private bool ignoreFirstSelect = true;
    private WaitForSeconds checkWait;

    // Prevent press sfx when we update dropdown value from code (load/refresh)
    private bool suppressCommitSfx;

    private void Reset()
    {
        dropdown = GetComponent<TMP_Dropdown>();
    }

    private void Awake()
    {
        checkWait = new WaitForSeconds(selectionCheckInterval);
    }

    private void Start()
    {
        if (dropdown != null)
            dropdown.onValueChanged.AddListener(OnValueChanged);

        ApplyFromSaved();
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(OnValueChanged);

        // Safety: if destroyed while expanded, ensure we release suppression
        if (wasExpanded)
            UISfxGate.PopSuppressSelectSfx();

        if (monitorRoutine != null)
            StopCoroutine(monitorRoutine);
    }

    private void LateUpdate()
    {
        if (dropdown == null) return;

        bool isExpanded = dropdown.IsExpanded;

        if (!wasExpanded && isExpanded)
        {
            // Dropdown opened: mute global select SFX while list is open
            UISfxGate.PushSuppressSelectSfx();
            
            // Suppress the first auto-highlight tick when the list appears
            UISfxGate.SuppressNextSelectSfx = true;
            
            // Start monitoring dropdown item selection
            if (monitorRoutine != null) StopCoroutine(monitorRoutine);
            monitorRoutine = StartCoroutine(MonitorDropdownSelection());
            ignoreFirstSelect = true;
        }
        else if (wasExpanded && !isExpanded)
        {
            // Dropdown closed: re-enable global select SFX
            UISfxGate.PopSuppressSelectSfx();
            
            if (monitorRoutine != null)
            {
                StopCoroutine(monitorRoutine);
                monitorRoutine = null;
            }
            lastSelectedObject = null;
        }

        wasExpanded = isExpanded;
    }

    private IEnumerator MonitorDropdownSelection()
    {
        // Wait for dropdown to fully spawn
        yield return new WaitForEndOfFrame();
        
        // Skip first frame to avoid initial selection
        yield return checkWait;
        
        while (dropdown.IsExpanded)
        {
            yield return checkWait; // Check less frequently
            
            if (EventSystem.current == null) continue;
            
            GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
            if (currentSelected == null) continue;
            
            // Check if selected object is in our dropdown list
            if (!IsInDropdownList(currentSelected)) continue;
            
            // Check if selection changed
            if (currentSelected != lastSelectedObject)
            {
                // Skip the first selection when dropdown opens
                if (ignoreFirstSelect)
                {
                    ignoreFirstSelect = false;
                }
                else
                {
                    PlayNavigateSound();
                }
                
                lastSelectedObject = currentSelected;
            }
        }
    }

    private bool IsInDropdownList(GameObject obj)
    {
        if (obj == null) return false;
        
        Transform listRoot = FindSpawnedDropdownListRoot();
        if (listRoot == null) return false;
        
        return obj.transform.IsChildOf(listRoot);
    }

    private Transform FindSpawnedDropdownListRoot()
    {
        if (dropdown == null) return null;
        
        // Cache canvas reference
        Canvas canvas = dropdown.GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        
        // Cache the dropdown list once when opened
        if (!dropdown.IsExpanded) return null;
        
        // Search through immediate children first (more efficient)
        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            Transform child = canvas.transform.GetChild(i);
            if (child.gameObject.activeInHierarchy && child.name == "Dropdown List")
            {
                return child;
            }
        }
        
        // If not found in immediate children, search deeper
        Transform[] allTransforms = canvas.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            // TMP dropdown lists are named "Dropdown List"
            if (t.name == "Dropdown List" && t.gameObject.activeInHierarchy)
            {
                return t;
            }
        }
        
        return null;
    }

    private void PlayNavigateSound()
    {
        if (navigateSfx != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.Play(navigateSfx, SoundCategory.SFX);
        }
    }

    private void OnValueChanged(int index)
    {
        // Apply immediately for responsiveness
        Apply(index);
        
        // Save in a coroutine to avoid frame spike
        StartCoroutine(SaveWithDelay());
        
        // Don't play press SFX for code-driven value sets (load/refresh)
        if (suppressCommitSfx) return;

        if (pressSfx != null && AudioManager.Instance != null)
            AudioManager.Instance.Play(pressSfx, SoundCategory.SFX);
    }

    private IEnumerator SaveWithDelay()
    {
        // Wait one frame to spread out the work
        yield return null;
        Save();
    }

    public void Apply(int index)
    {
        currentValue = index;

        if (dropdown != null)
        {
            // Setting UI value from code should not trigger commit SFX
            suppressCommitSfx = true;
            dropdown.SetValueWithoutNotify(currentValue);
            suppressCommitSfx = false;
        }
    }

    public void Apply(bool value) { }
    public void Apply(float value) { }

    public void ApplyFromSaved()
    {
        currentValue = PlayerPrefs.GetInt(SettingsKeys.Get(settingType), 0);
        Apply(currentValue);
    }

    public void Save()
    {
        PlayerPrefs.SetInt(SettingsKeys.Get(settingType), currentValue);
        PlayerPrefs.Save();
    }

    public void RefreshUI() => Apply(currentValue);
}