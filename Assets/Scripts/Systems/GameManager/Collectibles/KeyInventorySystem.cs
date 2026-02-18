using System.Collections.Generic;
using UnityEngine;

public class KeyInventorySystem : MonoBehaviour
{
    private readonly List<GameObject> keys = new List<GameObject>();

    public int Count => keys.Count;

    public void AddKey(GameObject keyObj)
    {
        if (keyObj == null) return;
        keys.Add(keyObj);
    }

    public bool HasKey() => keys.Count > 0;

    public GameObject ConsumeKey()
    {
        if (keys.Count == 0) return null;

        var key = keys[0];
        keys.RemoveAt(0);
        return key;
    }

    public void Clear()
    {
        keys.Clear();
    }
}