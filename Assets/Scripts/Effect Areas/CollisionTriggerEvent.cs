using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CollisionTriggerEvent : MonoBehaviour
{
    [SerializeField] UnityEvent onPlayerEnter;
    [SerializeField] bool autoDeactivate = false;

    bool active = true;

    // check if player enters the trigger
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!active) return;

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
}
