using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// Extra functionality for the Game Manager in the test level in the rebind menu
public class OptionsGameManager : MonoBehaviour, IOptionsPauseHandler, IPauseToggleGate
{
    public CanvasGroup rebindCanvasGroup;
    public GameObject[] mobileButtons;
    public RebindSettings rebindSettings;

    [SerializeField] private InputSystemUIInputModule uiInputModule;

    // Gate PauseMenuController input while rebind window is open
    public bool CanTogglePause => rebindSettings == null || rebindSettings.CanTogglePause;

    [SerializeField] UnityEvent onGameResumed;
    [SerializeField] UnityEvent onGamePaused;

    public void OnPause()
    {
        Debug.Log($"OnPause called - rebindCanvasGroup: {rebindCanvasGroup != null}, interactable was: {rebindCanvasGroup?.interactable}");
        rebindCanvasGroup.interactable = true;
        rebindCanvasGroup.blocksRaycasts = false;
        Debug.Log($"rebindCanvasGroup.interactable is now: {rebindCanvasGroup.interactable}");

        if (EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = true;

        if (uiInputModule != null)
            uiInputModule.enabled = true;

        onGamePaused?.Invoke();

        foreach (GameObject button in mobileButtons)
            button.SetActive(false);
    }

    public void OnResume()
    {
        rebindCanvasGroup.interactable = false;
        rebindCanvasGroup.blocksRaycasts = true;

        if (EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = false;

        if (uiInputModule != null)
            uiInputModule.enabled = false;

        onGameResumed?.Invoke();

        foreach (GameObject button in mobileButtons)
            button.SetActive(true);
    }
}