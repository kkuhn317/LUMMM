using UnityEngine;
using UnityEngine.Events;

public class CollisionTriggerEvent : MonoBehaviour
{
    [SerializeField] UnityEvent onPlayerEnter;
    [SerializeField] UnityEvent onPlayerExit;
    [SerializeField] bool autoDeactivate = false;
    [SerializeField] LayerMask layersToCheck;
    [SerializeField] UnityEvent onLayerEnter;
    [SerializeField] UnityEvent onLayerExit;

    bool active = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!active) return;

        if (other.CompareTag("Player"))
        {
            onPlayerEnter?.Invoke();
            if (autoDeactivate) active = false;
        }

        if (IsInLayerMask(other.gameObject, layersToCheck))
        {
            onLayerEnter?.Invoke();
            if (autoDeactivate) active = false;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!active) return;

        if (other.CompareTag("Player"))
        {
            onPlayerExit?.Invoke();
            if (autoDeactivate) active = false;
        }

        if (IsInLayerMask(other.gameObject, layersToCheck))
        {
            onLayerExit?.Invoke();
            if (autoDeactivate) active = false;
        }
    }

    private bool IsInLayerMask(GameObject obj, LayerMask mask)
    {
        return (mask.value & (1 << obj.layer)) != 0;
    }
}