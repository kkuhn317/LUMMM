using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FirePower : MarioAbility
{
    public GameObject fireballObj;
    public int fireballs = 0;
    public int fireballsMax = 2;
    public Vector2 shootOffset;
    public AudioClip shootSound;

    private void ShootProjectile(bool movingLeft = false)
    {
        if (fireballs < fireballsMax || GlobalVariables.cheatFlamethrower)
        {
            // Use parameter for direction, fall back to Mario's facing direction if not specified
            if (!marioMovement) return;

            bool isLeft = movingLeft;

            // If no direction specified, use Mario's facing direction
            if (!movingLeft && !marioMovement.facingRight)
            {
                isLeft = true;
            }

            int directionInt = isLeft ? -1 : 1;

            // instantiate fireball
            Vector3 spawnPosition = transform.position + new Vector3(shootOffset.x * directionInt, shootOffset.y, 0);
            GameObject newFireball = Instantiate(fireballObj, spawnPosition, transform.rotation);
            Fireball fireballScript = newFireball.GetComponent<Fireball>();
            ObjectPhysics fireballPhysics = newFireball.GetComponent<ObjectPhysics>();
            fireballScript.firePowerScript = this;
            fireballPhysics.movingLeft = isLeft;

            // increment fireball count
            fireballs++;

            // play shooting animation
            GetComponent<Animator>().SetTrigger("shoot");

            // play fireball sound
            GetComponent<AudioSource>().PlayOneShot(shootSound);
        }
    }
    
    private void ShootSpinningFireballs()
    {        
        // Shoot two fireballs simultaneously - one left, one right
        ShootProjectile(true);   // Left fireball
        ShootProjectile(false);  // Right fireball
    }

    public void onFireballDestroyed()
    {
        fireballs--;
    }

    // Shoot Action
    public override void onShootPressed()
    {
        if (!marioMovement.carrying && !marioMovement.groundPounding)
        {
            ShootProjectile();
        }
    }

    public override void onSpinPressed()
    {
        ShootSpinningFireballs();
    }
}
