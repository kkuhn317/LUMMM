using UnityEngine;
using UnityEngine.UI;

public class ToggleSettingHandler : MonoBehaviour, ISettingHandler
{
    [Header("Setting Config")]
    [SettingTypeFilter(SettingType.FullscreenKey, SettingType.OnScreenControlsKey, SettingType.SpeedrunModeKey, SettingType.InfiniteLivesKey, SettingType.CheckpointModeKey, SettingType.TimeLimitKey)]
    public SettingType settingType;

    [SerializeField] private AudioClip toggleSound;

    [Header("UI")]
    public Toggle toggle;

    private bool currentValue;

    public SettingType SettingType => settingType;

    void Start()
    {
        if (toggle != null)
            toggle.onValueChanged.AddListener(OnToggleChanged);

        ApplyFromSaved();
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool value)
    {
        Apply(value);
        Save();

        if (toggleSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.Play(toggleSound, SoundCategory.SFX);
        }
        else
        {
            Debug.LogWarning("Either a toggleSound wasn't referenced or the AudioManager isn't on the scene");
        }
    }

    public void Toggle()
    {
        Apply(!currentValue);
        Save();

        if (toggle != null)
            toggle.isOn = currentValue;
    }

    public void Apply(bool value)
    {
        currentValue = value;

        if (toggle != null)
            toggle.SetIsOnWithoutNotify(currentValue);
    }

    public void Apply(int index) { }

    public void ApplyFromSaved()
    {
        currentValue = PlayerPrefs.GetInt(SettingsKeys.Get(settingType), 0) == 1;
        Apply(currentValue);
    }

    public void Save() => PlayerPrefs.SetInt(SettingsKeys.Get(settingType), currentValue ? 1 : 0);

    public void RefreshUI() => Apply(currentValue);
}