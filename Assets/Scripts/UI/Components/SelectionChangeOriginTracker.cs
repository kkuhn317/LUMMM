using UnityEngine;
using UnityEngine.EventSystems;
using System.Diagnostics;
using System.Linq;

public class SelectionChangeOriginTracker : MonoBehaviour
{
    private GameObject _lastSelected;

    void LateUpdate()
    {
        var current = EventSystem.current.currentSelectedGameObject;

        if (current != _lastSelected)
        {
            _lastSelected = current;
            LogSelectionChangeOrigin(current);
        }
    }

    void LogSelectionChangeOrigin(GameObject selected)
    {
        // Capture stack trace
        StackTrace stack = new StackTrace(true);

        // Filter out anything outside your project's Assets folder
        var frames = stack.GetFrames()
            .Where(f => f.GetFileName() != null &&
                        f.GetFileName().Replace("\\", "/").Contains("/Assets/"))
            .ToList();

        string source = frames.Count == 0
            ? "(No project scripts detected — likely Unity system)"
            : string.Join("\n", frames.Select(f =>
                $"{System.IO.Path.GetFileName(f.GetFileName())}:{f.GetFileLineNumber()}  —  {f.GetMethod().DeclaringType.Name}.{f.GetMethod().Name}()"));

        UnityEngine.Debug.Log(
            $"[SELECTION ORIGIN]\n" +
            $"Selected: {selected?.name}\n" +
            $"Triggered by:\n{source}"
        );
    }
}