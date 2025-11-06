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

    private void ShootProjectile(bool isLeft, bool explicitDirection)
    {
        if (fireballs < fireballsMax || GlobalVariables.cheatFlamethrower)
        {
            if (!explicitDirection)
            {
                // Use Mario's facing direction
                isLeft = !marioMovement.facingRight;
            }

            int directionInt = isLeft ? -1 : 1;

            Vector3 spawnPos = transform.position + new Vector3(shootOffset.x * directionInt, shootOffset.y, 0);
            GameObject newFireball = Instantiate(fireballObj, spawnPos, Quaternion.identity);

            Fireball fireballScript = newFireball.GetComponent<Fireball>();
            ObjectPhysics fireballPhysics = newFireball.GetComponent<ObjectPhysics>();

            fireballScript.firePowerScript = this;
            fireballPhysics.movingLeft = isLeft;

            fireballs++;
            GetComponent<Animator>().SetTrigger("shoot");
            GetComponent<AudioSource>().PlayOneShot(shootSound);
        }
    }
    
    private void ShootSpinningFireballs()
    {
        bool facingLeft = !marioMovement.facingRight;

        // First fireball: in the direction Mario is facing
        ShootProjectile(facingLeft, true);

        // Second fireball: the opposite direction
        ShootProjectile(!facingLeft, true);
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
            bool isLeft = !marioMovement.facingRight;
            ShootProjectile(isLeft, false);
        }
    }

    public override void onSpinPressed()
    {
        ShootSpinningFireballs();
    }
}
