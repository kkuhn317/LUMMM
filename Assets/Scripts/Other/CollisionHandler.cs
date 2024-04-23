using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionHandler : MonoBehaviour
{
    public GameObject objectToSpawn;
    public AudioClip hit;

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void onPlayerHit(Vector2 hitPoint)
    {
        Instantiate(objectToSpawn, hitPoint, Quaternion.identity);

        if (audioSource != null)
        {
            audioSource.PlayOneShot(hit);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            onPlayerHit(collision.GetContact(0).point);
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.CompareTag("Player"))
        {
            onPlayerHit(collider.ClosestPoint(transform.position));
        }
    }
}
