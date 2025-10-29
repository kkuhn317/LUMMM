using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.Collections.Generic;

public class LanguageMenu : MenuBase
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
        // Instant visual selection from saved preference (doesn't depend on Localization init)
        SelectFromPrefsOrDefault();

        // When Localization finishes initializing, reconcile selection with the actual runtime locale
        StartCoroutine(ReselectWhenLocalizationReady());

        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
    }

    public override void Open()
    {
        base.Open();
        SelectFromPrefsOrDefault();
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

        // Already selected? Do nothing to avoid the “while already selecting” warning.
        if (es.currentSelectedGameObject == go) return;

        // Defer to the next frame to avoid race with other SetSelectedGameObject calls
        StartCoroutine(SelectNextFrame(go));
    }

    private IEnumerator SelectNextFrame(GameObject go)
    {
        yield return null; // wait one frame
        var es = EventSystem.current;
        if (es != null && go != null) es.SetSelectedGameObject(go);
    }

    public async void SetLanguage(string code)
    {
        try
        {
            // Ensure Localization is ready before touching AvailableLocales/SelectedLocale
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