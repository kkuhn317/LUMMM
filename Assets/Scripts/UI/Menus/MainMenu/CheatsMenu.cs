using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;
using UnityEngine.InputSystem;
using TMPro;

public class CheatsMenu : MonoBehaviour, IMenuTransition
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
        new CheatBinding("flamethrower", () => CheatFlags.StartPowerup == StartPowerupMode.Fire,
            v => CheatFlags.StartPowerup = v ? StartPowerupMode.Fire : StartPowerupMode.None),
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

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string showTrigger = "Show";
    [SerializeField] private string hideTrigger = "Hide";

    public void OnShow(System.Action onComplete)
    {
        if (animator == null)
        {
            Open();
            onComplete?.Invoke();
            return;
        }

        animator.SetTrigger(showTrigger);
        StartCoroutine(WaitForAnimation(onComplete, onDone: Open));
    }

    public void OnHide(System.Action onComplete)
    {
        if (animator == null)
        {
            onComplete?.Invoke();
            return;
        }

        animator.SetTrigger(hideTrigger);
        StartCoroutine(WaitForAnimation(onComplete));
    }

    private System.Collections.IEnumerator WaitForAnimation(System.Action onComplete, System.Action onDone = null)
    {
        // Wait one frame for animator to start transitioning
        yield return null;
        while (animator.IsInTransition(0) ||
               animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            yield return null;

        onDone?.Invoke();
        onComplete?.Invoke();
    }

    private void Awake()
    {
        // Initialize cheats if needed
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
    }

    private void OnEnable()
    {
        // Refresh active cheats display
        RefreshActiveCheatsDisplay();

        // Subscribe to input
        if (submitAction != null && submitAction.action != null)
        {
            submitAction.action.started += OnSubmitAction;
            if (!submitAction.action.enabled) submitAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from input
        if (submitAction != null && submitAction.action != null)
            submitAction.action.started -= OnSubmitAction;
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        
        // Initialize active cheats display
        RefreshActiveCheatsDisplay();
    }

    private void RefreshActiveCheatsDisplay()
    {
        // Clear existing list items
        foreach (var cheatObject in activeCheatObjects)
        {
            if (cheatObject.listItem != null)
                Destroy(cheatObject.listItem);
        }
        activeCheatObjects.Clear();

        // Rebuild list from current cheat states
        foreach (var cheat in cheats)
        {
            if (cheat.isActive())
            {
                CreateCheatListItem(cheat);
            }
        }
    }

    private void CreateCheatListItem(Cheat cheat)
    {
        if (cheatList == null || cheatListItemPrefab == null) return;

        var item = Instantiate(cheatListItemPrefab, cheatList.transform);
        var textComponent = item.GetComponentInChildren<TMP_Text>();
        if (textComponent != null)
            textComponent.text = GetCheatDescription(cheat.code);
        
        var button = item.GetComponentInChildren<Button>();
        if (button != null)
            button.onClick.AddListener(() => DeactivateCheat(cheat));
        
        activeCheatObjects.Add(new CheatObject(cheat, item));
    }

    private void OnSubmitAction(InputAction.CallbackContext ctx)
    {
        if (cheatInputField != null && cheatInputField.isFocused)
        {
            OnEnterButtonPressed();
        }
    }
    
    public void Open()
    {
        cheatInputField.Select();
        cheatInputField.text = "";
    }

    public void OnEnterButtonPressed()
    {
        string inputCode = cheatInputField.text.ToLower().Trim();

        if (string.IsNullOrEmpty(inputCode))
        {
            cheatInputField.Select();
            return;
        }

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
        if (cheat.isActive())
        {
            infoText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Menu_CheatInfoAlreadyActive");
            cheatInputField.Select();
            return;
        }

        cheat.applyCheat.Invoke();
        
        if (audioSource != null)
            audioSource.Play();
            
        infoText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Menu_CheatInfoActivated");
        
        // Add to active list if not already there
        if (!activeCheatObjects.Exists(co => co.cheat.code == cheat.code))
        {
            CreateCheatListItem(cheat);
        }

        // Notify that cheat was activated
        GlobalEventHandler.TriggerMenuOpened($"CheatActivated:{cheat.code}");
    }

    private void DeactivateCheat(Cheat cheat)
    {
        if (!cheat.isActive()) return;

        cheat.deactivateCheat.Invoke();
        
        if (audioSource != null)
            audioSource.Play();
            
        infoText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Menu_CheatInfoDeactivated");
        
        // Remove from active list
        var cheatObject = activeCheatObjects.Find(co => co.cheat.code == cheat.code);
        if (cheatObject != null)
        {
            activeCheatObjects.Remove(cheatObject);
            if (cheatObject.listItem != null)
                Destroy(cheatObject.listItem);
        }

        // Notify that cheat was deactivated
        GlobalEventHandler.TriggerMenuOpened($"CheatDeactivated:{cheat.code}");
    }

    public static string GetCheatDescription(string code)
    {
        return LocalizationSettings.StringDatabase.GetLocalizedString($"Cheat_{code}");
    }

    // Handle closing/back navigation
    public void OnBackPressed()
    {
        if (GUIManager.Instance != null)
        {
            GUIManager.Instance.Back();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}