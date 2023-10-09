using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AudioManager : MonoBehaviour
{
    [SerializeField] Slider masterVolumeSlider;
    [SerializeField] Slider bgmVolumeSlider;
    [SerializeField] Slider sfxVolumeSlider;
    [SerializeField] TMP_Text masterVolumeText;
    [SerializeField] TMP_Text bgmVolumeText;
    [SerializeField] TMP_Text sfxVolumeText;

    void Start()
    {
        // Set initial volume values
        masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        bgmVolumeSlider.value = PlayerPrefs.GetFloat("BGMVolume", 1f);
        sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // Update the volume and text labels
        UpdateVolumeText(masterVolumeText, masterVolumeSlider.value);
        UpdateVolumeText(bgmVolumeText, bgmVolumeSlider.value);
        UpdateVolumeText(sfxVolumeText, sfxVolumeSlider.value);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ChangeMasterVolume()
    {
        float volume = masterVolumeSlider.value;
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("MasterVolume", volume);
        UpdateVolumeText(masterVolumeText, volume);
    }

    public void ChangeBGMVolume()
    {
        float volume = bgmVolumeSlider.value;
        // Set your background music volume here
        PlayerPrefs.SetFloat("BGMVolume", volume);
        UpdateVolumeText(bgmVolumeText, volume);
    }

    public void ChangeSFXVolume()
    {
        float volume = sfxVolumeSlider.value;
        // Set your sound effects volume here
        PlayerPrefs.SetFloat("SFXVolume", volume);
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
