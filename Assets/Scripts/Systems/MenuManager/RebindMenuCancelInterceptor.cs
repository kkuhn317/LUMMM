using UnityEngine;
using UnityEngine.InputSystem;

public class RebindMenuCancelInterceptor : MonoBehaviour, ICancelHandler
{
    [SerializeField] private int cancelPriority = 10;
    [SerializeField] private GUIManager guiManager;
    [SerializeField] private PauseMenuController pauseController;

    public int CancelPriority => cancelPriority;

    public bool OnCancel()
    {
        if (guiManager.GetTopMenuObject().Equals(this.gameObject)) 
        {
            print("stopped cancel in rebind menu, toggling pause manually");
            
            // We are at the root menu. Toggle the pause!
            // Passing null is safe, PauseMenuController will auto-find the PlayerInput
            if (pauseController != null) pauseController.RequestTogglePause(null);
            
            return true;
        }
        
        return false;
    }
}