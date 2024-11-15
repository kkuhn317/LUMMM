using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class DualPresenceTrigger : MonoBehaviour
{
    public SpriteLibraryAsset newSpriteLibrary;
    public AudioClip newAudioClip;

    private bool playerInside = false; // Flag to check if the player is inside
    private bool enemyInside = false;  // Flag to check if an enemy is inside
    private bool soundPlayed = false;
    private BoxCollider2D boxCollider;

    private void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Checks if the object is the player or an enemy
        if (other.CompareTag("Player"))
        {
            playerInside = true;
        }
        else if (other.CompareTag("Enemy"))
        {
            enemyInside = true;
        }

        // Executes the action if both are inside
        if (playerInside && enemyInside)
        {
            ApplySpriteLibraryChange();

            // Plays the sound only if it hasn't been played before
            if (!soundPlayed && newAudioClip != null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    AudioSource audioSource = player.GetComponent<AudioSource>();
                    if (audioSource != null)
                    {
                        audioSource.PlayOneShot(newAudioClip);
                        soundPlayed = true;
                    }
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Detects if the player or the enemy has left the area
        if (other.CompareTag("Player"))
        {
            playerInside = false;
        }
        else if (other.CompareTag("Enemy"))
        {
            enemyInside = false;
        }

        // Resets the sound and sprite library, and deactivates the collider if either leaves
        if (!playerInside || !enemyInside)
        {
            ResetSpriteLibrary();
            soundPlayed = false;

            // Disable the BoxCollider2D so it no longer triggers
            if (boxCollider != null)
            {
                boxCollider.enabled = false;
            }
        }
    }

    private void ApplySpriteLibraryChange()
    {
        // Finds the player and changes their Sprite Library
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            SpriteLibrary spriteLibrary = player.GetComponent<SpriteLibrary>();
            if (spriteLibrary != null)
            {
                spriteLibrary.spriteLibraryAsset = newSpriteLibrary;
            }
        }
    }

    private void ResetSpriteLibrary()
    {
        // Finds the player and resets their Sprite Library
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            MarioMovement marioMovement = player.GetComponent<MarioMovement>();
            if (marioMovement != null)
            {
                marioMovement.resetSpriteLibrary();
            }
        }
    }
}
