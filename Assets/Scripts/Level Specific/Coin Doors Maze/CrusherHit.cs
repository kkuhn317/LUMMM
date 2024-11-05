using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrusherHit : MonoBehaviour
{
    public GameObject hitEffect;
    public float rotationForce = 10f;

    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("OnlyCutsceneUse"))
        {
            Debug.Log("Triggered by OnlyCutsceneUse");

            // Deactivate all scripts except for the Rigidbody
            MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != this)
                {
                    script.enabled = false;
                }
            }

            // Instantiate the "Hit" effect at the trigger point
            if (hitEffect)
            {
                Instantiate(hitEffect, other.ClosestPoint(transform.position), Quaternion.identity);
            }

            // Change Rigidbody2D to dynamic so gravity affects it
            rb.bodyType = RigidbodyType2D.Dynamic;

            // Determine the direction of the force to apply for rotation
            Vector2 hitDirection = (other.transform.position - transform.position).normalized;

            // Apply rotational force based on the direction
            float torque = hitDirection.x > 0 ? -rotationForce : rotationForce;
            rb.AddTorque(torque, ForceMode2D.Impulse);
        }
    }
}
