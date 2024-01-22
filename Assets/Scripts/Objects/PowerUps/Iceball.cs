using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class Iceball : Fireball
{
    
    
    
    public GameObject smallIceBlock;
    public GameObject bigIceBlock;
    public GameObject longIceBlock;
    private int groundHits = 0;
    protected override void OnBounced()
    {
        groundHits++;
        if (groundHits >= 2)
        {
            GetComponent<AudioSource>().PlayOneShot(hitWallSound);
            Instantiate(wallHitPrefab, transform.position, Quaternion.identity); // Instantiate the prefab at the fireball's position
            deleteFireball();
            


        }
    }


    protected override void OnTriggerEnter2D(Collider2D other)
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
    
    protected override void OnHitEnemy(GameObject enemy)
    {
        hitEnemy = true;
        //GameManager.Instance.AddScorePoints(100); // Gives a hundred points to the player
        //enemy.GetComponent<EnemyAI>().KnockAway(movingLeft);
        //freeze enemy by turning off AI
        enemy.GetComponent<EnemyAI>().enabled = false;
        //stop animation
        if (enemy.GetComponent<Animator>()){
        enemy.GetComponent<Animator>().enabled = false;
        }else if (enemy.GetComponent<AnimatedSprite>()){
        enemy.GetComponent<AnimatedSprite>().enabled = false;
        }
        //make small ice block the parent of the enemy
        //scale the ice block to be bigger if the enemy is bigger using the width of EnemyAI
        if (enemy.GetComponent<EnemyAI>().width > 1)
        {
            GameObject newIceBlock = Instantiate(bigIceBlock, enemy.transform.position, Quaternion.identity);
            enemy.transform.parent = newIceBlock.transform;
        }
        else
        {
            GameObject newIceBlock = Instantiate(smallIceBlock, enemy.transform.position, Quaternion.identity);
            enemy.transform.parent = newIceBlock.transform;
        }
        //GameObject newIceBlock = Instantiate(smallIceBlock, enemy.transform.position, Quaternion.identity);
        //enemy.transform.parent = newIceBlock.transform;

        deleteFireball();
    }
    public override void deleteFireball()
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
