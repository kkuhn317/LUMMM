using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cannon : MonoBehaviour
{
    // angle in degrees
    public float angle = 0f;

    public float initialDelay = 0f;
    public float rateOfFire = 3f;

    public GameObject projectilePrefab;

    // movement speed for bullet bills, force for any other objects
    public float projectileSpeed = 10f;

    public bool isShooting = true;
    bool isVisible = false;

    private AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        InvokeRepeating(nameof(Fire), initialDelay, rateOfFire);
    }

    void Fire()
    {
        if (projectilePrefab == null || !isVisible || !isShooting)
        {
            return;
        }

        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

        // assume it has ObjectPhysics
        ObjectPhysics physics2 = projectile.GetComponent<ObjectPhysics>();

        // set velocity
        Vector2 vel = new(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

        physics2.realVelocity = vel * projectileSpeed;

        if (audioSource != null)
        {
            audioSource.Play();
        }
    }

    void OnBecameVisible()
    {
        isVisible = true;
    }

    void OnBecameInvisible()
    {
        isVisible = false;
    }
}
