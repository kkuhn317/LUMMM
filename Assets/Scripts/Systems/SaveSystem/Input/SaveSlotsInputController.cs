using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SaveSlotsInputController : MonoBehaviour
{
    [SerializeField] private SaveSlotManager saveSlotManager;
    [SerializeField] private string levelSelectSceneName = "MainMenu";
    [SerializeField] private AudioClip transitionSound;

    private bool isLeavingScene = false;

    private void Awake()
    {
        // If not set from the Inspector, try to find it automatically in the scene
        if (saveSlotManager == null)
            saveSlotManager = FindObjectOfType<SaveSlotManager>();
    }

    /// <summary>
    /// This is called by the Input System (Cancel action).
    /// The method name must match the action callback set in PlayerInput.
    /// </summary>
    public void OnCancel(InputAction.CallbackContext context)
    {
        // Only react when the action is actually performed (button/key released/pressed depending on binding)
        if (!context.performed)
            return;

        TryHandleCancel();
    }

    /// <summary>
    /// This is used by UI Buttons (OnClick) or other scripts.
    /// No InputAction context is needed here.
    /// </summary>
    public void HandleCancel()
    {
        TryHandleCancel();
    }

    /// <summary>
    /// Centralized cancel logic shared by both Input System and UI button.
    /// </summary>
    private void TryHandleCancel()
    {
        // If we don't have a SaveSlotManager, there is nothing we can do
        if (saveSlotManager == null)
            return;

        // If we already started leaving the scene, ignore extra presses (anti-spam)
        if (isLeavingScene)
            return;

        // If we are in a special mode (Delete / Copy / Import / Export, etc.)
        // pressing Cancel should exit that mode instead of leaving the screen.
        if (saveSlotManager.CurrentMode != SaveSlotManager.InteractionMode.Normal)
        {
            saveSlotManager.CancelCurrentMode();
            return;
        }

        // From here, we are truly leaving the scene, so block any additional input
        isLeavingScene = true;

        // Play transition sound, if available
        if (transitionSound != null && AudioManager.Instance != null)
            AudioManager.Instance.Play(transitionSound, SoundCategory.SFX);

        // Use fade transition if the component exists, otherwise do a direct scene load
        if (FadeInOutScene.Instance != null)
            FadeInOutScene.Instance.LoadSceneWithFade(levelSelectSceneName);
        else
            SceneManager.LoadScene(levelSelectSceneName);
    }
}