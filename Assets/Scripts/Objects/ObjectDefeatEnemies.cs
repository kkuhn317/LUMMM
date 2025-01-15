using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectDefeatEnemies : MonoBehaviour
{
    public GameObject hitPrefab;

    private void OnTriggerEnter2D(Collider2D other)
    {
        ObjectPhysics objectPhysics = other.GetComponent<ObjectPhysics>();
        if (objectPhysics != null)
        {
            Debug.Log("ObjectPhysics component found!");

            SpriteRenderer spriteRenderer = other.GetComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 1;
            
            // Spawn the object at the closest point of collision
            Vector2 collisionPoint = other.ClosestPoint(transform.position);
            if (hitPrefab != null)
            {
                Instantiate(hitPrefab, collisionPoint, Quaternion.identity);
            }

            if (!objectPhysics.enabled)
            {
                Debug.Log("ObjectPhysics is disabled. Enabling now...");
                objectPhysics.enabled = true; // Enable ObjectPhysics
                objectPhysics.KnockAway(other.transform.position.x > transform.position.x);
                other.enabled = false; // Disable the collider
            }

            // Deactivate all scripts except for the Rigidbody
            MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != this && !(script is ObjectPhysics))
                {
                    script.enabled = false;
                }
            }
        }

        if (other.CompareTag("Enemy"))
        {
            EnemyAI enemyAI = other.GetComponent<EnemyAI>();
            enemyAI.KnockAway(other.transform.position.x > transform.position.x);
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.collider.CompareTag("Enemy"))
        {
            EnemyAI enemyAI = other.collider.GetComponent<EnemyAI>();
            enemyAI.KnockAway(other.transform.position.x > transform.position.x);
        }
    }
}
