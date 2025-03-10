using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using Newtonsoft.Json;

public class StartupThings : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // Set the target frame rate to 60 on mobile instead of the default 30
        if (Application.isMobilePlatform) {
            Application.targetFrameRate = 60;   // Default is 30 (ew)
        }

        // Get the current language
        string currentLanguage = PlayerPrefs.GetString("Language", "en");
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(currentLanguage);

        // Load control layout (currently only needed for on-screen controls, since keyboard/controler layout is loaded automatically)
        RebindSaveLoad.OnGameStart();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
