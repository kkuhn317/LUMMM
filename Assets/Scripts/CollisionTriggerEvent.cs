using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CollisionTriggerEvent : MonoBehaviour
{
    [SerializeField] UnityEvent onPlayerEnter;
    [SerializeField] bool autoDeactivate = false;

    // check if player enters the trigger
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            onPlayerEnter.Invoke();

            if (autoDeactivate)
            {
                // Deactivate the GameObject after the event is triggered
                gameObject.SetActive(false);
            }
        }
    }
}
