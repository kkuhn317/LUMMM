using UnityEngine;
using System.Collections.Generic;

public class ObjectPool : MonoBehaviour
{
    [Header("Pool Settings")]
    public GameObject prefab;
    public int initialSize = 10;
    public bool expandable = true;

    private readonly Queue<GameObject> _objects = new();

    private void Awake()
    {
        if (prefab == null)
        {
            Debug.LogError($"ObjectPool on {name}: Prefab is not assigned.");
            return;
        }

        Prewarm(initialSize);
    }

    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
            CreateNewObject();
    }

    private GameObject CreateNewObject()
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);

        var po = obj.GetComponent<PooledObject>();
        if (po == null)
            po = obj.AddComponent<PooledObject>();

        po.parentPool = this;

        _objects.Enqueue(obj);
        return obj;
    }

    public GameObject Get()
    {
        if (_objects.Count == 0)
        {
            if (expandable)
                CreateNewObject();
            else
                return null;
        }

        GameObject obj = _objects.Dequeue();
        obj.SetActive(true);
        return obj;
    }

    public void Release(GameObject obj)
    {
        obj.SetActive(false);
        _objects.Enqueue(obj);
    }
}