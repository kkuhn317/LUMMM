using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

public class AudioManager : MonoBehaviour
{
    [SerializeField] Slider masterVolumeSlider;
    [SerializeField] Slider bgmVolumeSlider;
    [SerializeField] Slider sfxVolumeSlider;
    [SerializeField] TMP_Text masterVolumeText;
    [SerializeField] TMP_Text bgmVolumeText;
    [SerializeField] TMP_Text sfxVolumeText;
    [SerializeField] AudioMixer musicMixer;
    [SerializeField] AudioMixer sfxMixer;

    void Start()
    {
        // Set initial volume values
        masterVolumeSlider.value = PlayerPrefs.GetFloat(SettingsKeys.MasterVolumeKey, 1f);
        bgmVolumeSlider.value = PlayerPrefs.GetFloat(SettingsKeys.BGMVolumeKey, 1f);
        sfxVolumeSlider.value = PlayerPrefs.GetFloat(SettingsKeys.SFXVolumeKey, 1f);

        // Update the volume and text labels
        UpdateVolumeText(masterVolumeText, masterVolumeSlider.value);
        UpdateVolumeText(bgmVolumeText, bgmVolumeSlider.value);
        UpdateVolumeText(sfxVolumeText, sfxVolumeSlider.value);
    }

    // Update is called once per frame
    void Update()
    {
        ChangeMasterVolume();
        ChangeBGMVolume();
        ChangeSFXVolume();
    }

    public void ChangeMasterVolume()
    {
        float volume = masterVolumeSlider.value;
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(SettingsKeys.MasterVolumeKey, volume);
        UpdateVolumeText(masterVolumeText, volume);
    }

    public void ChangeBGMVolume()
    {
        float volume = bgmVolumeSlider.value;
        if (volume == 0)
            musicMixer.SetFloat("MusicVolume", -80f); // To avoid -Infinity when volume is 0
        else
            musicMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20f);
        PlayerPrefs.SetFloat(SettingsKeys.BGMVolumeKey, volume);
        UpdateVolumeText(bgmVolumeText, volume);
    }

    public void ChangeSFXVolume()
    {
        float volume = sfxVolumeSlider.value;
        if (volume == 0)
            sfxMixer.SetFloat("SFXVolume", -80f); // To avoid -Infinity when volume is 0
        else
            sfxMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20f);
        PlayerPrefs.SetFloat(SettingsKeys.SFXVolumeKey, volume);
        UpdateVolumeText(sfxVolumeText, volume);
    }

    private void UpdateVolumeText(TMP_Text textComponent, float volumeValue)
    {
        int volumePercentage = Mathf.RoundToInt(volumeValue * 100f);
        textComponent.text = volumePercentage.ToString() + "%";

        if (volumePercentage == 100) {
            textComponent.color = Color.yellow;

        } else if (volumePercentage == 0) {
            textComponent.color = Color.red;
        } else {
            textComponent.color = Color.white;
        }
    }
}
