using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// Extra functionality for the Game Manager in the test level in the rebind menu
public class OptionsGameManager : MonoBehaviour, IOptionsPauseHandler, IPauseToggleGate
{
    /// <summary>
    /// The options-menu gameplay area is an ability test, so every Mario form
    /// should expose every move while this manager is active.
    /// </summary>
    public static bool GrantsAllAbilities { get; private set; }

    public CanvasGroup rebindCanvasGroup;
    public GameObject[] mobileButtons;
    public RebindSettings rebindSettings;

    [SerializeField] private InputSystemUIInputModule uiInputModule;

    // Gate PauseMenuController input while rebind window is open
    public bool CanTogglePause => rebindSettings == null || rebindSettings.CanTogglePause;

    [SerializeField] UnityEvent onGameResumed;
    [SerializeField] UnityEvent onGamePaused;

    private void OnEnable() => GrantsAllAbilities = true;

    private void OnDisable() => GrantsAllAbilities = false;

    public void OnPause()
    {
        Debug.Log($"OnPause called - rebindCanvasGroup: {rebindCanvasGroup != null}, interactable was: {rebindCanvasGroup?.interactable}");
        rebindCanvasGroup.interactable = true;
        rebindCanvasGroup.blocksRaycasts = true;
        Debug.Log($"rebindCanvasGroup.interactable is now: {rebindCanvasGroup.interactable}");

        if (EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = true;

        onGamePaused?.Invoke();

        foreach (GameObject button in mobileButtons)
            button.SetActive(false);
    }

    public void OnResume()
    {
        Debug.Log($"OnResume called!\n{System.Environment.StackTrace}");
        
        rebindCanvasGroup.interactable = false;
        rebindCanvasGroup.blocksRaycasts = false;

        if (EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = false;

        onGameResumed?.Invoke();

        foreach (GameObject button in mobileButtons)
            button.SetActive(true);
    }
}
