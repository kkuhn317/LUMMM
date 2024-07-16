using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionHandler : MonoBehaviour
{
    public GameObject objectToSpawn;
    public AudioClip hit;
    private AudioSource audioSource;
    private Collider2D col;
    private EnemyAI enemyAI;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        col = GetComponentInChildren<Collider2D>();
        enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.onPlayerDamaged.AddListener(onPlayerHit);
        }
    }

    private void onPlayerHit(GameObject player)
    {
        onPlayerHitPosition(player.transform.position);
    }

    private void onPlayerHitPosition(Vector2 hitPoint)
    {
        Instantiate(objectToSpawn, hitPoint, Quaternion.identity);

        if (audioSource != null)
        {
            audioSource.PlayOneShot(hit);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (enemyAI != null) {
            return;
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            onPlayerHitPosition(collision.GetContact(0).point);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (enemyAI != null) {
            return;
        }

        if (other.gameObject.CompareTag("Player"))
        {
            onPlayerHitPosition(col.ClosestPoint(other.transform.position));
        }
    }
}
