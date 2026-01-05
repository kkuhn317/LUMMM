using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropdownSettingHandler : MonoBehaviour, ISettingHandler
{
    [Header("Setting Config")]
    [SettingTypeFilter(SettingType.GraphicsQualityKey, SettingType.ResolutionKey)]
    public SettingType settingType;

    [Header("UI")]
    [SerializeField] private TMP_Dropdown dropdown;

    [Header("SFX")]
    [SerializeField] private AudioClip navigateSfx; // tick while browsing options (list open)
    [SerializeField] private AudioClip pressSfx;    // confirm when value changes

    public SettingType SettingType => settingType;

    private int currentValue;

    // Track open/close
    private bool wasExpanded;

    // Prevent press sfx when we update dropdown value from code (load/refresh)
    private bool suppressCommitSfx;

    // Used to skip the first auto-highlight tick when dropdown opens
    private bool suppressFirstOptionSelectTick = true;

    // Cached canvas (TMP spawns list under a canvas)
    private Canvas cachedCanvas;

    private const string DropdownListName = "Dropdown List";

    private void Reset()
    {
        dropdown = GetComponent<TMP_Dropdown>();
    }

    private void Awake()
    {
        if (dropdown == null)
            dropdown = GetComponent<TMP_Dropdown>();

        cachedCanvas = dropdown != null ? dropdown.GetComponentInParent<Canvas>() : null;
    }

    private void OnEnable()
    {
        // When menus start disabled, we must reset state on enable
        suppressFirstOptionSelectTick = true;
        wasExpanded = dropdown != null && dropdown.IsExpanded;
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
    }

    private void LateUpdate()
    {
        if (dropdown == null) return;

        bool isExpanded = dropdown.IsExpanded;

        if (!wasExpanded && isExpanded)
        {
            HandleDropdownOpened();
            wasExpanded = true;
        }
        else if (wasExpanded && !isExpanded)
        {
            HandleDropdownClosed();
            wasExpanded = false;
        }
    }

    private void HandleDropdownOpened()
    {
        // Mute global select SFX while list is open (so only our tick plays)
        UISfxGate.PushSuppressSelectSfx();

        // Also suppress any "first select" gate you already use elsewhere
        UISfxGate.SuppressNextSelectSfx = true;

        suppressFirstOptionSelectTick = true;

        // Setup list hooks next frame (TMP builds the list asynchronously)
        StartCoroutine(SetupDropdownListHooksNextFrame());
    }

    private void HandleDropdownClosed()
    {
        UISfxGate.PopSuppressSelectSfx();
    }

    private IEnumerator SetupDropdownListHooksNextFrame()
    {
        // Let TMP create the list & toggles
        yield return null;
        yield return new WaitForEndOfFrame();

        if (dropdown == null || !dropdown.IsExpanded) yield break;

        Transform listRoot = FindSpawnedDropdownListRoot();
        if (listRoot == null) yield break;

        // Each option is a Toggle. We hook OnSelect via a relay component.
        Toggle[] toggles = listRoot.GetComponentsInChildren<Toggle>(true);
        if (toggles == null || toggles.Length == 0) yield break;

        for (int i = 0; i < toggles.Length; i++)
        {
            if (toggles[i] == null) continue;

            var relay = toggles[i].GetComponent<OptionSelectSfxRelay>();
            if (relay == null)
                relay = toggles[i].gameObject.AddComponent<OptionSelectSfxRelay>();

            relay.Bind(this);
        }

        // Prime selection to the current value so navigation starts cleanly.
        // We do this AFTER hooks are attached, but we suppress the first tick.
        int index = Mathf.Clamp(dropdown.value, 0, toggles.Length - 1);
        if (EventSystem.current != null && toggles[index] != null)
        {
            EventSystem.current.SetSelectedGameObject(toggles[index].gameObject);
        }
    }

    private Transform FindSpawnedDropdownListRoot()
    {
        if (dropdown == null || !dropdown.IsExpanded) return null;

        if (cachedCanvas == null)
            cachedCanvas = dropdown.GetComponentInParent<Canvas>();

        if (cachedCanvas == null) return null;

        Transform canvasRoot = cachedCanvas.transform;

        // Fast path: immediate children
        for (int i = 0; i < canvasRoot.childCount; i++)
        {
            Transform child = canvasRoot.GetChild(i);
            if (child.gameObject.activeInHierarchy && child.name == DropdownListName)
                return child;
        }

        // Fallback: deep search
        Transform[] all = cachedCanvas.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t.gameObject.activeInHierarchy && t.name == DropdownListName)
                return t;
        }

        return null;
    }

    // Called by relay when a dropdown option is selected (navigated)
    private void OnOptionSelected()
    {
        // Only tick while expanded
        if (dropdown == null || !dropdown.IsExpanded) return;

        // Skip the first auto-highlight when the list appears (and when we prime selection)
        if (suppressFirstOptionSelectTick)
        {
            suppressFirstOptionSelectTick = false;
            return;
        }

        PlayNavigateSound();
    }

    private void PlayNavigateSound()
    {
        if (navigateSfx != null && AudioManager.Instance != null)
            AudioManager.Instance.Play(navigateSfx, SoundCategory.SFX);
    }

    private void OnValueChanged(int index)
    {
        Apply(index);
        StartCoroutine(SaveWithDelay());

        if (suppressCommitSfx) return;

        if (pressSfx != null && AudioManager.Instance != null)
            AudioManager.Instance.Play(pressSfx, SoundCategory.SFX);
    }

    private IEnumerator SaveWithDelay()
    {
        yield return null;
        Save();
    }

    public void Apply(int index)
    {
        currentValue = index;

        if (dropdown != null)
        {
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

    public void RefreshUI()
    {
        Apply(currentValue);
    }

    /// <summary>
    /// Lives on each spawned dropdown option (Toggle) and fires when it becomes selected.
    /// This is what makes the navigate SFX reliable even when menus start inactive.
    /// </summary>
    private sealed class OptionSelectSfxRelay : MonoBehaviour, ISelectHandler
    {
        private DropdownSettingHandler owner;

        public void Bind(DropdownSettingHandler handler)
        {
            owner = handler;
        }

        public void OnSelect(BaseEventData eventData)
        {
            owner?.OnOptionSelected();
        }
    }
}