using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CollisionTriggerEvent : MonoBehaviour
{
    [SerializeField] UnityEvent onPlayerEnter;
    [SerializeField] UnityEvent onPlayerExit;
    [SerializeField] bool autoDeactivate = false;

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
    }


}
