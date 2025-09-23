using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;
using TMPro;

public class CheatsMenu : MenuBase
{
    public TMP_InputField cheatInputField;
    public TMP_Text infoText;
    public GameObject cheatList;
    public GameObject cheatListItemPrefab;

    [System.Serializable]
    public class Cheat
    {
        public string code;
        public System.Action applyCheat;
        public System.Action deactivateCheat;
        public System.Func<bool> isActive;

        public Cheat(string code, System.Action applyCheat, System.Action deactivateCheat, System.Func<bool> isActive)
        {
            this.code = code;
            this.applyCheat = applyCheat;
            this.deactivateCheat = deactivateCheat;
            this.isActive = isActive;
        }

    }

    private class CheatBinding
    {
        public string code;
        public System.Func<bool> getter;
        public System.Action<bool> setter;

        public CheatBinding(string code, System.Func<bool> getter, System.Action<bool> setter)
        {
            this.code = code;
            this.getter = getter;
            this.setter = setter;
        }
    }

    private static readonly CheatBinding[] cheatBindings = new CheatBinding[]
    {
        new CheatBinding("club", () => GlobalVariables.cheatPlushies, v => GlobalVariables.cheatPlushies = v),
        new CheatBinding("supersecretbeta", () => GlobalVariables.cheatBetaMode, v => GlobalVariables.cheatBetaMode = v),
        new CheatBinding("immortal", () => GlobalVariables.cheatInvincibility, v => GlobalVariables.cheatInvincibility = v),
        new CheatBinding("abilityfreak", () => GlobalVariables.cheatAllAbilities, v => GlobalVariables.cheatAllAbilities = v),
        new CheatBinding("minimushroom", () => GlobalVariables.cheatStartTiny, v => GlobalVariables.cheatStartTiny = v),
        new CheatBinding("iceflower", () => GlobalVariables.cheatStartIce, v => GlobalVariables.cheatStartIce = v),
        new CheatBinding("flamethrower", () => GlobalVariables.cheatFlamethrower, v => GlobalVariables.cheatFlamethrower = v),
        new CheatBinding("midnight", () => GlobalVariables.cheatDarkness, v => GlobalVariables.cheatDarkness = v),
    };

    private class CheatObject
    {
        public Cheat cheat;
        public GameObject listItem;

        public CheatObject(Cheat cheat, GameObject listItem)
        {
            this.cheat = cheat;
            this.listItem = listItem;
        }
    }

    private List<Cheat> cheats;
    private List<CheatObject> activeCheatObjects = new();
    private AudioSource audioSource;

    private void OnEnable()
    {
        // Build cheats list from bindings
        cheats = new List<Cheat>();
        foreach (var binding in cheatBindings)
        {
            cheats.Add(new Cheat(
                binding.code,
                () => binding.setter(true),
                () => binding.setter(false),
                binding.getter
            ));
        }


    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Populate active cheats list
        foreach (var cheat in cheats)
        {
            if (cheat.isActive())
            {
                var item = Instantiate(cheatListItemPrefab, cheatList.transform);
                item.GetComponentInChildren<TMP_Text>().text = LocalizationSettings.StringDatabase.GetLocalizedString($"Cheat_{cheat.code}");
                item.GetComponentInChildren<Button>().onClick.AddListener(() => DeactivateCheat(cheat));
                activeCheatObjects.Add(new CheatObject(cheat, item));
            }
        }

        cheatInputField.onSubmit.AddListener((string text) =>
        {
            // Only work on Enter key
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnEnterButtonPressed();
            }
        });
    }
    
    public override void Open()
    {
        base.Open();
        cheatInputField.Select();
    }

    public void OnEnterButtonPressed()
    {
        string inputCode = cheatInputField.text.ToLower().Trim();

        Cheat cheat = cheats.Find(c => c.code == inputCode);
        if (cheat != null)
        {
            ActivateCheat(cheat);
            cheatInputField.text = "";
        }
        else
        {
            infoText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Menu_CheatInfoInvalid");
            cheatInputField.Select();
        }
    }

    private void ActivateCheat(Cheat cheat)
    {
        cheat.applyCheat.Invoke();
        audioSource.Play();
        infoText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Menu_CheatInfoActivated");
        // Add to the list if not already present
        if (!activeCheatObjects.Exists(co => co.cheat.code == cheat.code))
        {
            var item = Instantiate(cheatListItemPrefab, cheatList.transform);
            item.GetComponentInChildren<TMP_Text>().text = LocalizationSettings.StringDatabase.GetLocalizedString($"Cheat_{cheat.code}");
            item.GetComponentInChildren<Button>().onClick.AddListener(() => DeactivateCheat(cheat));
            activeCheatObjects.Add(new CheatObject(cheat, item));
        }
    }

    private void DeactivateCheat(Cheat cheat)
    {
        cheat.deactivateCheat.Invoke();
        audioSource.Play();
        infoText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Menu_CheatInfoDeactivated");
        // Remove from the list
        var cheatObject = activeCheatObjects.Find(co => co.cheat.code == cheat.code);
        if (cheatObject != null)
        {
            activeCheatObjects.Remove(cheatObject);
            Destroy(cheatObject.listItem);
        }
    }
}
