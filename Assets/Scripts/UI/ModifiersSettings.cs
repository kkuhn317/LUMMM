using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ModifiersSettings : MonoBehaviour
{
    [Header("Gameplay Toggles")]
    public Toggle infiniteLivesToggle;
    public Toggle checkpointsToggle;
    public Toggle timeLimitToggle;

    [Header("Toggle Sprites")]
    public Image infiniteLivesImage;
    public Image checkpointsImage;
    public Image timeLimitImage;
    public Sprite enableInfiniteLivesSprite;
    public Sprite disableInfiniteLivesSprite;
    public Sprite enableCheckpointsSprite;
    public Sprite disableCheckpointsSprite;
    public Sprite enableTimeLimitSprite;
    public Sprite disableTimeLimitSprite;

    private void Start()
    {
        ConfigureInfiniteLives();
        ConfigureCheckpoints();
        ConfigureTimeLimit();
    }

    private void ConfigureInfiniteLives()
    {
        bool isInfiniteLivesEnabled = PlayerPrefs.GetInt(SettingsKeys.InfiniteLivesKey, 0) == 1;
        infiniteLivesToggle.isOn = isInfiniteLivesEnabled;
        infiniteLivesImage.sprite = isInfiniteLivesEnabled ? enableInfiniteLivesSprite : disableInfiniteLivesSprite;
        infiniteLivesToggle.onValueChanged.AddListener(OnInfiniteLivesToggleValueChanged);
    }

    private void OnInfiniteLivesToggleValueChanged(bool isEnabled)
    {
        infiniteLivesImage.sprite = isEnabled ? enableInfiniteLivesSprite : disableInfiniteLivesSprite;
        PlayerPrefs.SetInt(SettingsKeys.InfiniteLivesKey, isEnabled ? 1 : 0);
        GlobalVariables.infiniteLivesMode = isEnabled;
    }

    private void ConfigureCheckpoints()
    {
        bool areCheckpointsEnabled = PlayerPrefs.GetInt(SettingsKeys.CheckpointsKey, 0) == 1;
        checkpointsToggle.isOn = areCheckpointsEnabled;
        checkpointsImage.sprite = areCheckpointsEnabled ? enableCheckpointsSprite : disableCheckpointsSprite;
        checkpointsToggle.onValueChanged.AddListener(OnCheckpointsToggleValueChanged);
    }

    private void OnCheckpointsToggleValueChanged(bool isEnabled)
    {
        checkpointsImage.sprite = isEnabled ? enableCheckpointsSprite : disableCheckpointsSprite;
        PlayerPrefs.SetInt(SettingsKeys.CheckpointsKey, isEnabled ? 1 : 0);
        GlobalVariables.enableCheckpoints = isEnabled;
    }

    private void ConfigureTimeLimit()
    {
        bool isTimeLimitEnabled = PlayerPrefs.GetInt(SettingsKeys.TimeLimitKey, 0) == 1;
        timeLimitToggle.isOn = isTimeLimitEnabled;
        timeLimitImage.sprite = isTimeLimitEnabled ? enableTimeLimitSprite : disableTimeLimitSprite;
        timeLimitToggle.onValueChanged.AddListener(OnTimeLimitToggleValueChanged);
    }

    private void OnTimeLimitToggleValueChanged(bool isEnabled)
    {
        timeLimitImage.sprite = isEnabled ? enableTimeLimitSprite : disableTimeLimitSprite;
        PlayerPrefs.SetInt(SettingsKeys.TimeLimitKey, isEnabled ? 1 : 0);
        GlobalVariables.stopTimeLimit = isEnabled;
    }
}