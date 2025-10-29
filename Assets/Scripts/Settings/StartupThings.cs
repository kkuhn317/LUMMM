using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class StartupThings : MonoBehaviour
{
    private IEnumerator Start()
    {
        // Keep your existing 60 FPS cap on mobile
        if (Application.isMobilePlatform)
            Application.targetFrameRate = 60;

        // Wait until Localization is ready (avoids race/fallback to English)
        var init = LocalizationSettings.InitializationOperation;
        if (!init.IsDone)
            yield return init;

        // Apply saved locale if present (exact -> prefix -> keep current)
        string saved = PlayerPrefs.GetString("Language", string.Empty);
        var locales = LocalizationSettings.AvailableLocales.Locales;

        Locale locale = null;
        if (!string.IsNullOrWhiteSpace(saved))
        {
            locale = locales.FirstOrDefault(l => l.Identifier.Code == saved)
                  ?? locales.FirstOrDefault(l => l.Identifier.Code.StartsWith(saved));
        }

        if (locale != null)
            LocalizationSettings.SelectedLocale = locale;

        // Load control layout (currently only needed for on-screen controls, since keyboard/controler layout is loaded automatically)
        RebindSaveLoad.OnGameStart();
    }
}