using UnityEngine;
using System.Collections.Generic;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [Tooltip("Default initial size for pools created automatically.")]
    public int defaultInitialSize = 10;

    private readonly Dictionary<GameObject, ObjectPool> _pools = new();

    private void Awake()
    {
        // Scene-local singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // NOTE: NO DontDestroyOnLoad → scene-local
    }

    /// <summary>
    /// Spawn an object using the pool for this prefab.
    /// If no pool exists, one will be created automatically.
    /// </summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var pool = GetOrCreatePool(prefab);

        GameObject obj = pool.Get();
        obj.transform.SetPositionAndRotation(position, rotation);

        return obj;
    }

    /// <summary>
    /// Release an object back to its pool via PooledObject.
    /// </summary>
    public void Release(GameObject obj)
    {
        if (obj == null)
            return;

        var po = obj.GetComponent<PooledObject>();
        if (po == null)
        {
            Destroy(obj); // Not from a pool
            return;
        }

        if (po.parentPool != null)
        {
            po.parentPool.Release(obj);
        }
        else
        {
            Destroy(obj); // Fallback safety
        }
    }

    private ObjectPool GetOrCreatePool(GameObject prefab)
    {
        if (_pools.TryGetValue(prefab, out var existingPool))
            return existingPool;

        // Create a new pool in this scene
        var poolGO = new GameObject(prefab.name + "_Pool");
        poolGO.transform.SetParent(transform);

        var pool = poolGO.AddComponent<ObjectPool>();
        pool.prefab = prefab;
        pool.initialSize = defaultInitialSize;
        pool.expandable = true;

        _pools.Add(prefab, pool);
        return pool;
    }
}