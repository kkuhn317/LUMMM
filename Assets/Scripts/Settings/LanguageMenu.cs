using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LanguageMenu : MenuBase
{
    public List<Button> languageButtons;
    public List<string> languageCodes;

    public override void Open()
    {
        base.Open();
        HighlightCurrentLanguage();
    }

    private void Start()
    {
        for (int i = 0; i < languageButtons.Count; i++)
        {
            int index = i;
            languageButtons[i].onClick.AddListener(() => SetLanguage(languageCodes[index]));
        }
    }

    private void HighlightCurrentLanguage()
    {
        string currentLanguage = PlayerPrefs.GetString("Language", "en");
        for (int i = 0; i < languageCodes.Count; i++)
        {
            if (currentLanguage == languageCodes[i])
            {
                languageButtons[i].Select();
            }
        }
    }

    private void SetLanguage(string language)
    {
        PlayerPrefs.SetString("Language", language);
        PlayerPrefs.Save();
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(language);
    }
}