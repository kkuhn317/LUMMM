using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fireball : ObjectPhysics
{

    public firePower firePowerScript;
    public AudioClip hitWallSound;
    public GameObject wallHitPrefab; // Prefab to instantiate when the fireball hits a wall

    bool hitEnemy = false;

    void OnTriggerEnter2D(Collider2D other)
    {

        if (other.gameObject.GetComponent<EnemyAI>() && !hitEnemy)
        {
            if (other.gameObject.GetComponent<EnemyAI>().canBeFireballed) {
                hitEnemy = true;
                GameManager.Instance.AddScorePoints(100); // Gives a hundred points to the player
                other.gameObject.GetComponent<EnemyAI>().KnockAway(movingLeft);
                deleteFireball();
            } else {
                hitWall();
            }
        }
    }

    public override bool CheckWalls(Vector3 pos, float direction)
    {
        bool hit = base.CheckWalls(pos, direction);
        if (hit) {
            hitWall();
        }
        return hit;
    }

    public void hitWall()
    {
        GetComponent<AudioSource>().PlayOneShot(hitWallSound);
        Instantiate(wallHitPrefab, transform.position, Quaternion.identity); // Instantiate the prefab at the fireball's position
        deleteFireball();
    }

    public void deleteFireball()
    {
        // TODO: make explosion animation
        if (firePowerScript)
            firePowerScript.onFireballDestroyed();
        
        // let sounds play before deleting
        GetComponent<Collider2D>().enabled = false;
        GetComponent<SpriteRenderer>().enabled = false;
        movement = ObjectMovement.still;
        Destroy(gameObject, 2);
    }

}
