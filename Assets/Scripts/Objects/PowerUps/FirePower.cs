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

    private void ShootProjectile() {
        if (fireballs < fireballsMax)
            {
                bool facingRight = GetComponent<MarioMovement>().facingRight;
                int directionint = facingRight ? 1 : -1;

                // instantiate fireball
                GameObject newFireball = Instantiate(fireballObj, transform.position + (Vector3)shootOffset * directionint, transform.rotation);
                Fireball fireballScript = newFireball.GetComponent<Fireball>();
                ObjectPhysics fireballPhysics = newFireball.GetComponent<ObjectPhysics>();
                fireballScript.firePowerScript = this;
                fireballPhysics.movingLeft = !facingRight;

                // increment fireball count
                fireballs++;

                // play shooting animation
                GetComponent<Animator>().SetTrigger("shoot");

                // play fireball sound
                GetComponent<AudioSource>().PlayOneShot(shootSound);
            }
    }

    public void onFireballDestroyed()
    {
        fireballs--;
    }

    // Shoot Action
    public override void onShootPressed()
    {
        if (!marioMovement.carrying)
        {
            ShootProjectile();
        }
    }
}
