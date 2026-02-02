using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class InputModuleWatchdog : MonoBehaviour
{
    private InputSystemUIInputModule module;
    private bool lastState;

    void Awake()
    {
        module = FindObjectOfType<InputSystemUIInputModule>();
        if (module != null)
        {
            lastState = module.enabled;
            Debug.Log("[WATCHDOG] Found InputSystemUIInputModule. Starting monitor.");
        }
        else
        {
            Debug.LogError("[WATCHDOG] No InputSystemUIInputModule found in scene!");
        }
    }

    void Update()
    {
        if (module == null) return;

        // Detect enable / disable
        if (module.enabled != lastState)
        {
            string stack = StackTraceUtility.ExtractStackTrace();

            Debug.LogError(
                $"[WATCHDOG] InputSystemUIInputModule.enabled changed to {module.enabled}\n" +
                $"FRAME={Time.frameCount}\n" +
                $"--- STACK TRACE ---\n{stack}"
            );

            lastState = module.enabled;
        }
    }
}