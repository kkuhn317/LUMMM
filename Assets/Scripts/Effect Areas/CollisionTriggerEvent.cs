using System.Collections;
using System.Collections.Generic;
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

    // check if player enters the trigger
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!active) return;
        if (onPlayerEnter == null) return;

        if (other.gameObject.tag == "Player")
        {
            onPlayerEnter.Invoke();

            if (autoDeactivate)
            {
                // Deactivate the trigger
                active = false;
            }
        }

        if (IsInLayerMask(other.gameObject, layersToCheck))
        {
            onLayerEnter.Invoke();

            if (autoDeactivate)
            {
                // Deactivate the trigger
                active = false;
            }
        }
    }

    // check if player exits the trigger
    void OnTriggerExit2D(Collider2D other)
    {
        if (!active) return;
        if (onPlayerExit == null) return;

        if (other.gameObject.tag == "Player")
        {
            onPlayerExit.Invoke();

            if (autoDeactivate)
            {
                // Deactivate the trigger
                active = false;
            }
        }

        if (IsInLayerMask(other.gameObject, layersToCheck))
        {
            onLayerExit.Invoke();

            if (autoDeactivate)
            {
                // Deactivate the trigger
                active = false;
            }
        }
    }

    private bool IsInLayerMask(GameObject obj, LayerMask mask)
    {
        return (mask.value & (1 << obj.layer)) != 0;
    }
}
