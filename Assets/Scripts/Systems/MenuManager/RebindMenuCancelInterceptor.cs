using UnityEngine;
using UnityEngine.InputSystem;

public class RebindMenuCancelInterceptor : MonoBehaviour, ICancelHandler
{
    [SerializeField] private PauseMenuController testLevelPauseController;
    [SerializeField] private int cancelPriority = 10;

    public int CancelPriority => cancelPriority;

    public bool OnCancel()
    {
        // Consume the event so Escape never closes the Rebind Menu
        return true; 
    }
}