using UnityEngine;
using UnityEngine.InputSystem; // For Touchscreen (if you ever want to use it again)

[System.Flags]
public enum PlatformMask
{
    None        = 0,
    Editor      = 1 << 0,
    Standalone  = 1 << 1,  // Windows/Mac/Linux players
    Mobile      = 1 << 2,  // Android / iOS (+ WebGL on phone via extra check)
    WebGL       = 1 << 3,
    Console     = 1 << 4,  // PS / Xbox / Switch, etc.
}

public class PlatformFilterEnabler : MonoBehaviour
{
    [Header("Platforms where this should be ENABLED")]
    public PlatformMask activeOn =
        PlatformMask.Editor |
        PlatformMask.Standalone |
        PlatformMask.WebGL;

    [Tooltip("If true, ALL bits in activeOn must be present in currentMask (AND logic). " +
             "If false, ANY bit is enough (OR logic, default).")]
    public bool requireAllFlags = false;

    [Header("Platforms where this should be DISABLED (even if activeOn matches)")]
    [Tooltip("If any of these bits are present in currentMask, the object will be disabled.")]
    public PlatformMask excludeOn = PlatformMask.None;

    [Header("Targets")]
    [Tooltip("If true, this GameObject will be enabled/disabled.")]
    public bool affectSelf = true;

    [Tooltip("Optional extra GameObjects to toggle with the same rule.")]
    public GameObject[] extraTargets;

#if UNITY_EDITOR
    [Header("Editor Simulation (only in Play Mode)")]
    [Tooltip("If true, overrides the detected platform mask while running in the Editor.")]
    public bool simulateInEditor = false;

    [Tooltip("Mask to use when simulateInEditor is true. For example:\n" +
             "- WebGL                → simulate WebGL PC\n" +
             "- Mobile               → simulate native mobile\n" +
             "- WebGL | Mobile       → simulate WebGL opened on a phone")]
    public PlatformMask editorSimulatedMask = PlatformMask.Standalone;
#endif

    private void Awake()
    {
        bool shouldBeActive = IsCurrentPlatformAllowed();
        Apply(shouldBeActive);
    }

    private bool IsCurrentPlatformAllowed()
    {
        PlatformMask currentMask = PlatformMask.None;

#if UNITY_EDITOR
        if (simulateInEditor)
        {
            // Use the simulated mask in Editor
            currentMask = editorSimulatedMask | PlatformMask.Editor;
        }
        else
        {
            // Normal Editor behaviour: just mark Editor + infer runtime group if needed
            currentMask |= PlatformMask.Editor;

            // (Optional) If you want, you can also try to infer Standalone here,
            // but normalmente no hace falta para el preview.
        }
#else
        // RUNTIME (no Editor)
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.LinuxPlayer:
                currentMask |= PlatformMask.Standalone;
                break;

            case RuntimePlatform.Android:
            case RuntimePlatform.IPhonePlayer:
                currentMask |= PlatformMask.Mobile;
                break;

            case RuntimePlatform.WebGLPlayer:
                currentMask |= PlatformMask.WebGL;

                // If this WebGL build is running on a *real* mobile device, also mark it as Mobile.
                if (IsWebGLMobileLike())
                    currentMask |= PlatformMask.Mobile;
                break;

            case RuntimePlatform.PS4:
            case RuntimePlatform.PS5:
            case RuntimePlatform.XboxOne:
            case RuntimePlatform.GameCoreXboxSeries:
            case RuntimePlatform.Switch:
                currentMask |= PlatformMask.Console;
                break;
        }
#endif

        bool allowed;
        if (requireAllFlags)
        {
            // ALL bits in activeOn must be present in currentMask
            allowed = (currentMask & activeOn) == activeOn;
        }
        else
        {
            // ANY bit in activeOn is enough
            allowed = (currentMask & activeOn) != 0;
        }

        // Force-disable if any excluded bits are present
        if (allowed && (currentMask & excludeOn) != 0)
        {
            allowed = false;
        }

        Debug.Log(
            $"[PlatformFilterEnabler] '{name}' " +
            $"platform={Application.platform}, " +
            $"mask={currentMask}, activeOn={activeOn}, excludeOn={excludeOn}, requireAllFlags={requireAllFlags}, " +
#if UNITY_EDITOR
            $"simulateInEditor={simulateInEditor}, editorSimulatedMask={editorSimulatedMask}, " +
#endif
            $"=> allowed={allowed}"
        );

        return allowed;
    }

    private void Apply(bool enable)
    {
        if (affectSelf)
            gameObject.SetActive(enable);

        if (extraTargets == null) return;

        for (int i = 0; i < extraTargets.Length; i++)
        {
            if (extraTargets[i] != null)
                extraTargets[i].SetActive(enable);
        }
    }

    /// <summary>
    /// Returns true when running as WebGL on a *mobile* device (phone/tablet).
    /// Here NO resolution heuristic, just Unity's mobile detection.
    /// </summary>
    private bool IsWebGLMobileLike()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (Application.isMobilePlatform)
            return true;

        if (SystemInfo.deviceType == DeviceType.Handheld)
            return true;

        return false;
#else
        return false;
#endif
    }
}