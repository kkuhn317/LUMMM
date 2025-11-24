using UnityEngine;

public class PooledObject : MonoBehaviour
{
    [HideInInspector] public ObjectPool parentPool;

    public void Release()
    {
        if (parentPool != null)
            parentPool.Release(gameObject);
        else
            Destroy(gameObject); // Fallback if something went wrong
    }
}