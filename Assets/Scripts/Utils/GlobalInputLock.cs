using UnityEngine;

public static class GlobalInputLock
{
    // Allows nested locks: if multiple systems block, all must release.
    private static int _lockCount = 0;

    public static bool IsLocked => _lockCount > 0;

    public static void PushLock()
    {
        _lockCount++;
    }

    public static void PopLock()
    {
        _lockCount = Mathf.Max(0, _lockCount - 1);
    }
    
    public static void Clear()
    {
        _lockCount = 0;
    }
}