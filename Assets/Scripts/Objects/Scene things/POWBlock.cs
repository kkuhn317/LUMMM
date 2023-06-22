using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class POWBlock : MonoBehaviour
{
    public bool changeVisibleEnemiesToKnockedAway = false;
    public bool changeAllEnemiesToKnockedAway = false;
    public AudioClip powblockSound;
    private AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            Vector2 impulse = Vector2.zero;

            int contactCount = other.contactCount;
            for (int i = 0; i < contactCount; i++)
            {
                var contact = other.GetContact(i);
                impulse += contact.normal * contact.normalImpulse;
                impulse.x += contact.tangentImpulse * contact.normal.y;
                impulse.y -= contact.tangentImpulse * contact.normal.x;
            }

            // position comparison is to stop a weird bug where the player can hit the top corner of the block and activate it
            if (impulse.y <= 0 || other.transform.position.y > transform.position.y)
            {
                return;
            }

            ActivatePOWBlock();
        }
    }

    private void ActivatePOWBlock()
    {
        // Check if the option to change visible enemies to Knocked Away is enabled
        if (changeVisibleEnemiesToKnockedAway)
        {
            ChangeVisibleEnemiesToKnockedAway();
        }

        // Check if the option to change all enemies to Knocked Away is enabled
        if (changeAllEnemiesToKnockedAway)
        {
            ChangeAllEnemiesToKnockedAway();
        }

        // Play POW Block effect 
        audioSource.PlayOneShot(powblockSound, 0.5f);

        // Destroy POW Block
        Destroy(gameObject);
    }

    private void ChangeVisibleEnemiesToKnockedAway()
    {
        // Find all visible enemies in the camera's view
        Collider2D[] colliders = Physics2D.OverlapAreaAll(Camera.main.ViewportToWorldPoint(Vector3.zero), Camera.main.ViewportToWorldPoint(Vector3.one), LayerMask.GetMask("Enemy"));

        foreach (Collider2D collider in colliders)
        {
            EnemyAI enemy = collider.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                // Change enemy's state to Knocked Away
                enemy.KnockAway(false);
            }
        }
    }

    private void ChangeAllEnemiesToKnockedAway()
    {
        // Find all enemies in the scene
        EnemyAI[] allEnemies = FindObjectsOfType<EnemyAI>();

        foreach (EnemyAI enemy in allEnemies)
        {
            //Generate a random direction between 1 and -1
            float knockDirection = Random.Range(-1f, 1f); // -1 is false, 1 is true on boolean

            // Change enemy's state to Knocked Away with the random direction
            enemy.KnockAway(knockDirection > 0); // Pass true for right direction, false for left direction
        }
    }
}
