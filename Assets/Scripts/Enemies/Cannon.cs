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

    private AudioSource audioSource;

    // using the angle, we use these internally
    private float shootAngle;
    private bool shootDirection = false; // true = left, false = right

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        InvokeRepeating(nameof(Fire), initialDelay, rateOfFire);
    }

    void Fire()
    {
        if (projectilePrefab == null)
        {
            return;
        }

        shootAngle = angle;

        angle %= 360;

        if (angle > 90 && angle < 270)
        {
            // flip angle on y axis
            shootAngle = 180 - angle;
            shootAngle %= 360;
            shootDirection = true;
        }

        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

        // if this object has BulletBill, set the angle
        if (projectile.TryGetComponent(out BulletBill physics))
        {   
            projectile.transform.rotation = Quaternion.Euler(0, 0, angle);

            physics.velocity.x = projectileSpeed;

        } else {
            // assume it has ObjectPhysics
            ObjectPhysics physics2 = projectile.GetComponent<ObjectPhysics>();

            // set velocity
            Vector2 vel = new(Mathf.Cos(shootAngle * Mathf.Deg2Rad), Mathf.Sin(shootAngle * Mathf.Deg2Rad));

            physics2.velocity = vel * projectileSpeed;
            physics2.movingLeft = shootDirection;
        }

        if (audioSource != null)
        {
            audioSource.Play();
        }
    }
}
