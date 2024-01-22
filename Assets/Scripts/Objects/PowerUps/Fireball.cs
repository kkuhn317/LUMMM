using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fireball : ObjectPhysics
{

    public FirePower firePowerScript;
    public AudioClip hitWallSound;
    public GameObject wallHitPrefab; // Prefab to instantiate when the fireball hits a wall

    protected bool hitEnemy = false;

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {

        if (other.gameObject.GetComponent<EnemyAI>() && !hitEnemy)
        {
            if (other.gameObject.GetComponent<EnemyAI>().canBeFireballed) {
                OnHitEnemy(other.gameObject);
            } else {
                hitWall();
            }
        }
    }

    protected virtual void OnHitEnemy(GameObject enemy)
    {
        hitEnemy = true;
        GameManager.Instance.AddScorePoints(100); // Gives a hundred points to the player
        enemy.GetComponent<EnemyAI>().KnockAway(movingLeft);
        deleteFireball();
    }

    public override bool CheckWalls(Vector3 pos, float direction)
    {
        bool hit = base.CheckWalls(pos, direction);
        if (hit) {
            hitWall();
        }
        return hit;
    }

    public virtual void hitWall()
    {
        GetComponent<AudioSource>().PlayOneShot(hitWallSound);
        Instantiate(wallHitPrefab, transform.position, Quaternion.identity); // Instantiate the prefab at the fireball's position
        deleteFireball();
    }

    public virtual void deleteFireball()
    {
        // TODO: make explosion animation
        if (firePowerScript)
            firePowerScript.onFireballDestroyed();
        
        // let sounds play before deleting
        GetComponent<Collider2D>().enabled = false;

        
        //GetComponent<SpriteRenderer>().enabled = false;

        // turn off all sprite renderers (including children)
        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            spriteRenderer.enabled = false;
        }
        
        movement = ObjectMovement.still;
        Destroy(gameObject, 2);
    }

}
