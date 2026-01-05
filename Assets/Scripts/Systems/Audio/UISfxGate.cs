public static class UISfxGate
{
    public static bool SuppressNextSelectSfx;

    private static int suppressSelectDepth;

    public static void PushSuppressSelectSfx()
    {
        suppressSelectDepth++;
    }

    public static void PopSuppressSelectSfx()
    {
        suppressSelectDepth = UnityEngine.Mathf.Max(0, suppressSelectDepth - 1);
    }

    public static bool IsSelectSfxSuppressed => suppressSelectDepth > 0;

    public static bool ConsumeSuppressNextSelectSfx()
    {
        if (!SuppressNextSelectSfx) return false;
        SuppressNextSelectSfx = false;
        return true;
    }
}