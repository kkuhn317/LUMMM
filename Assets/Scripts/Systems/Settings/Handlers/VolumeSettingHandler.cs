using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class VolumeSettingHandler : MonoBehaviour, ISettingHandler
{
    [SettingTypeFilter(SettingType.MasterVolumeKey, SettingType.BGMVolumeKey, SettingType.SFXVolumeKey)]
    public SettingType settingType;
    public CustomSlider slider;
    public TMP_Text valueText;

    public Color minVolumeColor = new(1f, 0.2f, 0.2f);
    public Color maxVolumeColor = new(0.2f, 0.75f, 1f);

    [Header("Preview")]
    public AudioClip previewClip;
    public SoundCategory previewCategory = SoundCategory.SFX;
    public bool loopPreview = false;

    private Mario inputActions;
    private bool wasSelected = false;

    public SettingType SettingType => settingType;

    private void EnsureInputActions()
    {
        if (inputActions == null)
            inputActions = new Mario();
    }

    private void Awake()
    {
        EnsureInputActions();
    }

    private void OnEnable()
    {
        EnsureInputActions();
        inputActions.Enable();
    }

    private void OnDisable()
    {
        if (inputActions != null)
            inputActions.Disable();
    }

    private void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Dispose();
            inputActions = null;
        }
    }

    private void Start()
    {
        if (slider == null)
        {
            Debug.LogError("[VolumeSettingHandler] Slider reference is missing.", this);
            return;
        }

        if (valueText == null)
        {
            Debug.LogError("[VolumeSettingHandler] ValueText reference is missing.", this);
            return;
        }

        slider.onValueChanged.AddListener(OnValueChanged);
        ApplyFromSaved();
    }

    private void Update()
    {
        if (slider == null || EventSystem.current == null)
            return;

        bool isSelectedNow = EventSystem.current.currentSelectedGameObject == slider.gameObject;

        if (wasSelected && !isSelectedNow)
        {
            Debug.Log("[VolumeSettingHandler] Deselected (via slider focus)");

            if (previewCategory != SoundCategory.SFX && AudioManager.Instance != null)
            {
                AudioManager.Instance.StopCategory(previewCategory);
            }
        }

        wasSelected = isSelectedNow;
    }

    private void PlayPreview()
    {
        if (previewClip == null || AudioManager.Instance == null)
            return;

        if (AudioManager.Instance.IsPlaying(previewClip))
            return;

        AudioManager.Instance.Play(previewClip, previewCategory, 1f, 1f, loopPreview);
    }

    public void OnValueChanged(float value)
    {
        SetText(value);

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetVolume(settingType, value);

        Save();
        PlayPreview();
    }

    public void ApplyFromSaved()
    {
        if (slider == null)
            return;

        float saved = PlayerPrefs.GetFloat(SettingsKeys.Get(settingType), 0.5f);
        slider.SetValueWithoutNotify(saved);
        SetText(saved);

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetVolume(settingType, saved);
    }

    public void Save()
    {
        if (slider == null)
            return;

        PlayerPrefs.SetFloat(SettingsKeys.Get(settingType), slider.value);
        PlayerPrefs.Save();
    }

    public void Apply(int index) { }
    public void Apply(bool value) { }

    public void RefreshUI()
    {
        if (slider != null)
            OnValueChanged(slider.value);
    }

    private void SetText(float volume)
    {
        if (valueText == null)
            return;

        int intVolume = Mathf.RoundToInt(volume * 100);
        valueText.text = intVolume.ToString("00") + "%";

        valueText.color = intVolume switch
        {
            0 => minVolumeColor,
            100 => maxVolumeColor,
            _ => Color.white
        };
    }
}