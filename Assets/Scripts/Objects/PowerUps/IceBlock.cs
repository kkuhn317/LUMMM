using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class IceBlock : ObjectPhysics
{

    protected AudioSource audioSource;
    public AudioClip hitWallSound;

    protected override void Start() {
        base.Start();
        //get audio source
        audioSource = GetComponent<AudioSource>();
    }

   private void OnCollisionEnter2D(Collision2D other) {
    print ("collision with " + other.gameObject.tag);
         if (other.gameObject.CompareTag("Player")) {

                Vector2 impulse = Vector2.zero;

            int contactCount = other.contactCount;
            for (int i = 0; i < contactCount; i++)
            {
                var contact = other.GetContact(i);
                impulse += contact.normal * contact.normalImpulse;
                impulse.x += contact.tangentImpulse * contact.normal.y;
                impulse.y -= contact.tangentImpulse * contact.normal.x;
            }
            //if impulse.x is greater than 0 then the player is moving right
            //if impulse.x is less than 0 then the player is moving left
            //change the velocity of the iceblock to move in the same direction as the player
            print(impulse.x);
            if (impulse.x > 0.1) {
                movingLeft = false;
                velocity.x = 10;
            } else if (impulse.x < -0.1) {
                movingLeft = true;
                velocity.x = 10;
            }
            
            

              
         }
         
   }
     private void OnTriggerEnter2D(Collider2D collision)
     {
        //if it hits an enemy then knock away the enemy
        if (collision.gameObject.CompareTag("Enemy") && velocity.x != 0){
            collision.gameObject.GetComponent<EnemyAI>().KnockAway(movingLeft);
            GameManager.Instance.AddScorePoints(100); // Gives a hundred points to the player
        }
     }
    public void BreakIce(){
        //disable the iceblock
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<BoxCollider2D>().enabled = false;
        movement = ObjectMovement.still;
        audioSource.PlayOneShot(hitWallSound);
      
        //make the iceblock shoot out iceparticles by enabling all the sprite children of the iceblock and setting their velocity to shoot out all in random directions 
        for (int i = 0; i < transform.childCount; i++)
        {
            //enable the sprite renderer and gameobject
            transform.GetChild(i).GetComponent<SpriteRenderer>().enabled = true;
            transform.GetChild(i).gameObject.SetActive(true);
        
            
        }
    
        //only set the direction if the child object has the script StarMoveOutward
        GameManager.Instance.AddScorePoints(100); // Gives a hundred points to the player
        transform.GetChild(0).GetComponent<StarMoveOutward>().direction = new Vector2(1, 1);
        transform.GetChild(1).GetComponent<StarMoveOutward>().direction = new Vector2(-1, 1);
        transform.GetChild(2).GetComponent<StarMoveOutward>().direction = new Vector2(1, -1);
        transform.GetChild(3).GetComponent<StarMoveOutward>().direction = new Vector2(-1, -1);


        Destroy(transform.GetChild(4).gameObject);
        

        Destroy(gameObject, 0.5f);
    }

    protected override void onTouchWall(GameObject other)
    {
        base.onTouchWall(other);
        if(other.GetComponent<IceBlock>() != null){
            //if it hits a wall then destroy the iceblock
            other.GetComponent<IceBlock>().BreakIce();
        }
        BreakIce();
        
        
        
        
    }

}
