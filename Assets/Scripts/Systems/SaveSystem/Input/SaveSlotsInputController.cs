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
        if (saveSlotManager == null)
            saveSlotManager = FindObjectOfType<SaveSlotManager>();
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (saveSlotManager == null)
            return;

        if (isLeavingScene)
            return;
        
        if (saveSlotManager.CurrentMode != SaveSlotManager.InteractionMode.Normal)
        {
            saveSlotManager.CancelCurrentMode();
            return;
        }

        isLeavingScene = true;

        if (transitionSound != null && AudioManager.Instance != null)
            AudioManager.Instance.Play(transitionSound, SoundCategory.SFX);

        if (FadeInOutScene.Instance != null)
            FadeInOutScene.Instance.LoadSceneWithFade(levelSelectSceneName);
        else
            SceneManager.LoadScene(levelSelectSceneName);
    }
}