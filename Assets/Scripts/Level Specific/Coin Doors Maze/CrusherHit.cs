using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CrusherHit : MonoBehaviour
{
    public GameObject hitEffect;
    public UnityEvent onObjectHit;

    private Rigidbody2D rb;
    private ObjectPhysics objectPhysics;
    private HashSet<Collider2D> triggeredObjects = new HashSet<Collider2D>();

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        objectPhysics = GetComponent<ObjectPhysics>();
        objectPhysics.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {

        // Check if the object has already triggered the effect
        if (triggeredObjects.Contains(other))
        {
            return; // Exit if the effect has already been triggered for this object
        }

        if (other.CompareTag("OnlyCutsceneUse"))
        {
            Debug.Log("Triggered by OnlyCutsceneUse");
            onObjectHit.Invoke();

            // Instantiate the "Hit" effect at the trigger point
            if (hitEffect)
            {
                Instantiate(hitEffect, other.ClosestPoint(transform.position), Quaternion.identity);
            }

            objectPhysics.enabled = true;
            objectPhysics.KnockAway(other.transform.position.x > transform.position.x);

            // Deactivate all scripts except for the Rigidbody
            MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != this && !(script is ObjectPhysics))
                {
                    script.enabled = false;
                }
            }

            // Add the object to the triggered set
            triggeredObjects.Add(other);
        }
    }
}
