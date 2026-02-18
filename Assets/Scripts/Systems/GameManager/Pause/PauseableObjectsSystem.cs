using System.Collections.Generic;
using UnityEngine;

public class PauseableObjectsController : MonoBehaviour
{
    private readonly HashSet<PauseableObject> pauseables = new HashSet<PauseableObject>();

    public void Register(PauseableObject obj)
    {
        if (obj == null) return;
        pauseables.Add(obj);
    }

    public void Unregister(PauseableObject obj)
    {
        if (obj == null) return;
        pauseables.Remove(obj);
    }

    public void PauseAll()
    {
        foreach (var p in pauseables)
        {
            if (p != null) p.Pause();
        }
    }

    public void ResumeAll()
    {
        foreach (var p in pauseables)
        {
            if (p != null) p.Resume();
        }
    }

    public void FallAll()
    {
        foreach (var p in pauseables)
        {
            if (p != null) p.FallStraightDown();
        }
    }
}