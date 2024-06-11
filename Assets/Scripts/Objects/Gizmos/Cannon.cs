using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cannon : MonoBehaviour
{
    // angle in degrees
    public float angle = 0f;

    public float initialDelay = 0f;
    public float rateOfFire = 3f; // 0 or less means no auto fire

    public GameObject projectilePrefab;
    public GameObject projectileEffectPrefab;

    // movement speed for bullet bills, force for any other objects
    public float projectileSpeed = 10f;

    public bool isShooting = true;
    bool isVisible = false;

    public float shootOffset = 0f;

    private AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (rateOfFire > 0f) {
            InvokeRepeating(nameof(AutoFire), initialDelay, rateOfFire);
        }
    }

    void AutoFire()
    {
        if (isShooting)
        {
            Shoot();
        }
    }

    public void Shoot() {
        if (projectilePrefab == null || !isVisible)
        {
            return;
        }

        // offset the position of the projectile so it looks better
        Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * shootOffset;

        GameObject projectile = Instantiate(projectilePrefab, transform.position + offset, Quaternion.identity);

        if (projectileEffectPrefab != null) {
            Instantiate(projectileEffectPrefab, transform.position + offset, Quaternion.identity);
        }

        // assume it has ObjectPhysics
        ObjectPhysics physics2 = projectile.GetComponent<ObjectPhysics>();

        // set velocity
        Vector2 vel = new(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

        physics2.realVelocity = vel * projectileSpeed;

        if (audioSource != null) {
            audioSource.Play();
        }
    }

    public void ChangeprojectileSpeed(float projectilespeed)
    {
        projectileSpeed = projectilespeed;
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
