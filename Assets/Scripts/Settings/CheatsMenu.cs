using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CheatsMenu : MenuBase
{
    public TMP_InputField cheatInputField;
    public TMP_Text infoText;

    [System.Serializable]
    public class Cheat
    {
        public string code;
        public string description;
        public System.Action applyCheat;

        public Cheat(string code, System.Action applyCheat, string description)
        {
            this.code = code;
            this.applyCheat = applyCheat;
            this.description = description;
        }
    }

    private List<Cheat> cheats = new List<Cheat>
        {
            new Cheat("club", () => GlobalVariables.cheatPlushies = true, "Unlock plushies"),
            new Cheat("supersecretbeta", () => GlobalVariables.cheatBetaMode = true, "Enable beta mode"),
            new Cheat("immortal", () => GlobalVariables.cheatInvincibility = true, "Enable invincibility"),
            new Cheat("abilityfreak", () => GlobalVariables.cheatAllAbilities = true, "Unlock all abilities"),
            new Cheat("minimushroom", () => GlobalVariables.cheatStartTiny = true, "Start as Tiny Mario"),
            new Cheat("iceflower", () => GlobalVariables.cheatStartIce = true, "Start as Ice Mario"),
            new Cheat("flamethrower", () => GlobalVariables.cheatFlamethrower = true, "Fire Mario, Inf Fireballs"),
            new Cheat("midnight", () => GlobalVariables.cheatDarkness = true, "Darkness Effect"),
        };

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public override void Open()
    {
        base.Open();
    }

    public void OnEnterButtonPressed()
    {
        string inputCode = cheatInputField.text.ToLower().Trim();

        Cheat cheat = cheats.Find(c => c.code == inputCode);
        if (cheat != null)
        {
            cheat.applyCheat.Invoke();
            Debug.Log("Cheat activated: " + inputCode);
            audioSource.Play();
            infoText.text = "Cheat activated: " + cheat.description;
        }
        else
        {
            Debug.Log("Invalid cheat code: " + inputCode);
            infoText.text = "Invalid cheat code!";
        }
    }
}