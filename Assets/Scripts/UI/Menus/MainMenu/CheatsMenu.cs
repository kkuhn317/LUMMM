using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;
using UnityEngine.InputSystem;
using TMPro;

public class CheatsMenu : MenuBase
{
    public TMP_InputField cheatInputField;
    public TMP_Text infoText;
    public GameObject cheatList;
    public GameObject cheatListItemPrefab;
    [SerializeField] private InputActionReference submitAction;

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

    public class CheatBinding
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
    
    public static readonly CheatBinding[] cheatBindings = new CheatBinding[]
    {
        new CheatBinding("club", () => CheatFlags.Plushies, v => CheatFlags.Plushies = v),
        new CheatBinding("supersecretbeta", () => CheatFlags.BetaMode, v => CheatFlags.BetaMode = v),
        new CheatBinding("immortal", () => CheatFlags.Invincibility, v => CheatFlags.Invincibility = v),
        new CheatBinding("abilityfreak", () => CheatFlags.AllAbilities, v => CheatFlags.AllAbilities = v),
        new CheatBinding("minimushroom", () => CheatFlags.StartPowerup == StartPowerupMode.Tiny, 
            v => CheatFlags.StartPowerup = v ? StartPowerupMode.Tiny : StartPowerupMode.None),
        new CheatBinding("iceflower", () => CheatFlags.StartPowerup == StartPowerupMode.Ice,
            v => CheatFlags.StartPowerup = v ? StartPowerupMode.Ice : StartPowerupMode.None),
        new CheatBinding("flamethrower", () => CheatFlags.StartPowerup == StartPowerupMode.Flamethrower,
            v => CheatFlags.StartPowerup = v ? StartPowerupMode.Flamethrower : StartPowerupMode.None),
        new CheatBinding("midnight", () => CheatFlags.Darkness, v => CheatFlags.Darkness = v),
        new CheatBinding("corruption", () => CheatFlags.Randomizer, v => CheatFlags.Randomizer = v)
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
        if (cheats == null || cheats.Count == 0)
        {
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

        foreach (var cheatObject in activeCheatObjects)
        {
            cheatObject.listItem.GetComponentInChildren<TMP_Text>().text = GetCheatDescription(cheatObject.cheat.code);
        }

        if (submitAction != null && submitAction.action != null)
        {
            submitAction.action.started += OnSubmitAction;
            if (!submitAction.action.enabled) submitAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (submitAction != null && submitAction.action != null)
            submitAction.action.started -= OnSubmitAction;
    }

    private void OnSubmitAction(InputAction.CallbackContext ctx)
    {
        if (cheatInputField != null && cheatInputField.isFocused)
        {
            OnEnterButtonPressed();
        }
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        foreach (var cheat in cheats)
        {
            if (cheat.isActive())
            {
                var item = Instantiate(cheatListItemPrefab, cheatList.transform);
                item.GetComponentInChildren<TMP_Text>().text = GetCheatDescription(cheat.code);
                item.GetComponentInChildren<Button>().onClick.AddListener(() => DeactivateCheat(cheat));
                activeCheatObjects.Add(new CheatObject(cheat, item));
            }
        }
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
        
        if (!activeCheatObjects.Exists(co => co.cheat.code == cheat.code))
        {
            var item = Instantiate(cheatListItemPrefab, cheatList.transform);
            item.GetComponentInChildren<TMP_Text>().text = GetCheatDescription(cheat.code);
            item.GetComponentInChildren<Button>().onClick.AddListener(() => DeactivateCheat(cheat));
            activeCheatObjects.Add(new CheatObject(cheat, item));
        }
    }

    private void DeactivateCheat(Cheat cheat)
    {
        cheat.deactivateCheat.Invoke();
        audioSource.Play();
        infoText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Menu_CheatInfoDeactivated");
        
        var cheatObject = activeCheatObjects.Find(co => co.cheat.code == cheat.code);
        if (cheatObject != null)
        {
            activeCheatObjects.Remove(cheatObject);
            Destroy(cheatObject.listItem);
        }
    }

    public static string GetCheatDescription(string code)
    {
        return LocalizationSettings.StringDatabase.GetLocalizedString($"Cheat_{code}");
    }
}