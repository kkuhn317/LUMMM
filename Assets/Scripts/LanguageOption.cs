using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class LanguageOption : MonoBehaviour
{
    public List<Button> languageButtons;
    public List<string> languageCodes;

    void Start() {
        // Set click events for each language button
        for (int i = 0; i < languageButtons.Count; i++)
        {
            int index = i;
            languageButtons[i].onClick.AddListener(() => SetLanguage(languageCodes[index]));
        }
    }

    void OnEnable()
    {
        // make the button for the current language selected
        string currentLanguage = PlayerPrefs.GetString("Language", "en");
        for (int i = 0; i < languageCodes.Count; i++)
        {
            if (currentLanguage == languageCodes[i])
            {
                languageButtons[i].Select();
            }
        }
    }

    public void SetLanguage(string language)
    {
        PlayerPrefs.SetString("Language", language);
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(language);
    }
}
