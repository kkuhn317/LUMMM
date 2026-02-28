using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.Collections.Generic;

public class LanguageMenu : MonoBehaviour
{
    public List<Button> languageButtons;
    public List<string> languageCodes;

    void Awake()
    {
        for (int i = 0; i < languageButtons.Count; i++)
        {
            int index = i;
            languageButtons[i].onClick.AddListener(() => SetLanguage(languageCodes[index]));
        }
    }

    void OnEnable()
    {
        // Notify global system
        GlobalEventHandler.TriggerMenuOpened("LanguageMenu");
        
        SelectFromPrefsOrDefault();
        StartCoroutine(ReselectWhenLocalizationReady());
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
    }

    private IEnumerator ReselectWhenLocalizationReady()
    {
        var init = LocalizationSettings.InitializationOperation;
        if (!init.IsDone)
            yield return init;

        SetSelectedButtonFromLocale(LocalizationSettings.SelectedLocale);
    }

    private void OnSelectedLocaleChanged(Locale locale)
    {
        SetSelectedButtonFromLocale(locale);
    }

    private void SelectFromPrefsOrDefault()
    {
        string code = PlayerPrefs.GetString("Language", "en");
        SelectByCode(code, fallbackToEnglish: true);
    }

    private void SetSelectedButtonFromLocale(Locale locale)
    {
        if (locale == null) return;
        SelectByCode(locale.Identifier.Code, fallbackToEnglish: true);
    }

    private void SelectByCode(string code, bool fallbackToEnglish)
    {
        int idx = languageCodes.IndexOf(code);
        if (idx < 0 && fallbackToEnglish) idx = languageCodes.IndexOf("en");
        if (idx < 0 || idx >= languageButtons.Count) return;

        var es = EventSystem.current;
        var go = languageButtons[idx].gameObject;

        if (es == null)
        {
            languageButtons[idx].Select();
            return;
        }

        if (es.currentSelectedGameObject == go) return;

        StartCoroutine(SelectNextFrame(go));
    }

    private IEnumerator SelectNextFrame(GameObject go)
    {
        yield return null;
        var es = EventSystem.current;
        if (es != null && go != null) es.SetSelectedGameObject(go);
    }

    public async void SetLanguage(string code)
    {
        try
        {
            await LocalizationSettings.InitializationOperation.Task;

            var locale = LocalizationSettings.AvailableLocales.Locales
                          .FirstOrDefault(l => l.Identifier.Code == code);

            if (locale == null)
            {
                Debug.LogWarning($"Locale '{code}' not found in AvailableLocales.");
                return;
            }

            LocalizationSettings.SelectedLocale = locale;

            PlayerPrefs.SetString("Language", code);
            PlayerPrefs.Save();
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
    }
}