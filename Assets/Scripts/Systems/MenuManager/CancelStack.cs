using System.Collections.Generic;
using UnityEngine;

public static class CancelStack
{
    private static readonly List<ICancelHandler> handlers = new();

    public static void Register(ICancelHandler handler)
    {
        if (handler == null) return;
        if (handlers.Contains(handler)) return;
        handlers.Add(handler);
        handlers.Sort((a, b) => b.CancelPriority.CompareTo(a.CancelPriority));
    }

    public static void Unregister(ICancelHandler handler)
    {
        if (handler == null) return;
        handlers.Remove(handler);
    }

    public static ICancelHandler Peek()
    {
        for (int i = handlers.Count - 1; i >= 0; i--)
        {
            var o = handlers[i] as Object;
            if (o == null) handlers.RemoveAt(i);
        }
        return handlers.Count > 0 ? handlers[0] : null;
    }
}