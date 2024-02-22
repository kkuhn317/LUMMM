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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Instantiate(objectToSpawn, collision.transform.position, Quaternion.identity);

            if (audioSource != null)
            {
                audioSource.PlayOneShot(hit);
            }
        }
    }
}
