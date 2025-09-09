using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
        public string description;
        public System.Action applyCheat;
        public System.Action deactivateCheat;
        public System.Func<bool> isActive;

        public Cheat(string code, System.Action applyCheat, System.Action deactivateCheat, System.Func<bool> isActive, string description)
        {
            this.code = code;
            this.applyCheat = applyCheat;
            this.deactivateCheat = deactivateCheat;
            this.isActive = isActive;
            this.description = description;
        }

    }

    private class CheatBinding
    {
        public string code;
        public string description;
        public System.Func<bool> getter;
        public System.Action<bool> setter;

        public CheatBinding(string code, string description, System.Func<bool> getter, System.Action<bool> setter)
        {
            this.code = code;
            this.description = description;
            this.getter = getter;
            this.setter = setter;
        }
    }

    private static readonly CheatBinding[] cheatBindings = new CheatBinding[]
    {
        new CheatBinding("club", "Unlock plushies", () => GlobalVariables.cheatPlushies, v => GlobalVariables.cheatPlushies = v),
        new CheatBinding("supersecretbeta", "Enable beta mode", () => GlobalVariables.cheatBetaMode, v => GlobalVariables.cheatBetaMode = v),
        new CheatBinding("immortal", "Enable invincibility", () => GlobalVariables.cheatInvincibility, v => GlobalVariables.cheatInvincibility = v),
        new CheatBinding("abilityfreak", "Unlock all abilities", () => GlobalVariables.cheatAllAbilities, v => GlobalVariables.cheatAllAbilities = v),
        new CheatBinding("minimushroom", "Start as Tiny Mario", () => GlobalVariables.cheatStartTiny, v => GlobalVariables.cheatStartTiny = v),
        new CheatBinding("iceflower", "Start as Ice Mario", () => GlobalVariables.cheatStartIce, v => GlobalVariables.cheatStartIce = v),
        new CheatBinding("flamethrower", "Fire Mario, Inf Fireballs", () => GlobalVariables.cheatFlamethrower, v => GlobalVariables.cheatFlamethrower = v),
        new CheatBinding("midnight", "Darkness Effect", () => GlobalVariables.cheatDarkness, v => GlobalVariables.cheatDarkness = v),
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
                binding.getter,
                binding.description
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
                item.GetComponentInChildren<TMP_Text>().text = cheat.description;
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
            infoText.text = "Invalid cheat code!";
            cheatInputField.Select();
        }
    }

    private void ActivateCheat(Cheat cheat)
    {
        cheat.applyCheat.Invoke();
        audioSource.Play();
        infoText.text = $"Cheat activated!";
        // Add to the list if not already present
        if (!activeCheatObjects.Exists(co => co.cheat.code == cheat.code))
        {
            var item = Instantiate(cheatListItemPrefab, cheatList.transform);
            item.GetComponentInChildren<TMP_Text>().text = cheat.description;
            item.GetComponentInChildren<Button>().onClick.AddListener(() => DeactivateCheat(cheat));
            activeCheatObjects.Add(new CheatObject(cheat, item));
        }
    }

    private void DeactivateCheat(Cheat cheat)
    {
        cheat.deactivateCheat.Invoke();
        audioSource.Play();
        infoText.text = $"Cheat deactivated!";
        // Remove from the list
        var cheatObject = activeCheatObjects.Find(co => co.cheat.code == cheat.code);
        if (cheatObject != null)
        {
            activeCheatObjects.Remove(cheatObject);
            Destroy(cheatObject.listItem);
        }
    }
}
