using UnityEngine;

// Extra functionality for the Game Manager in the test level in the rebind menu
public class OptionsGameManager : MonoBehaviour, IOptionsPauseHandler, IPauseToggleGate
{
    public CanvasGroup rebindCanvasGroup;
    public GameObject[] mobileButtons;
    public RebindSettings rebindSettings;

    // Gate PauseMenuController input while rebind window is open
    public bool CanTogglePause => rebindSettings == null || rebindSettings.CanTogglePause;

    public void OnPause()
    {
        Debug.Log($"OnPause called - rebindCanvasGroup: {rebindCanvasGroup != null}, interactable was: {rebindCanvasGroup?.interactable}");
        rebindCanvasGroup.interactable = true;
        rebindCanvasGroup.blocksRaycasts = true;
        Debug.Log($"rebindCanvasGroup.interactable is now: {rebindCanvasGroup.interactable}");

        foreach (GameObject button in mobileButtons)
            button.SetActive(false);
    }

    public void OnResume()
    {
        rebindCanvasGroup.interactable = false;
        rebindCanvasGroup.blocksRaycasts = false;

        foreach (GameObject button in mobileButtons)
            button.SetActive(true);
    }
}