using UnityEngine;

public static class Pool
{
    public static GameObject Spawn(GameObject prefab, Vector3 pos)
    {
        return ObjectPoolManager.Instance.Spawn(prefab, pos, Quaternion.identity);
    }

    public static GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        return ObjectPoolManager.Instance.Spawn(prefab, pos, rot);
    }

    public static void Release(GameObject obj)
    {
        ObjectPoolManager.Instance.Release(obj);
    }
}