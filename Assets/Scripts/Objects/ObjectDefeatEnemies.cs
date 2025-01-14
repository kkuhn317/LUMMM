using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectDefeatEnemies : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
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
