using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class SpriteSwapArea : MonoBehaviour
{
    public SpriteLibraryAsset newSpriteLibrary;
    public AudioClip newAudioClip;

    public bool allowChangeOnEnter = true;
    public bool allowResetOnExit = true;

    private bool soundPlayed = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player" && allowChangeOnEnter)
        {
            other.gameObject.GetComponent<SpriteLibrary>().spriteLibraryAsset = newSpriteLibrary;

            // Play MaaaamaMiaAudioClip only if it has not been played before
            if (!soundPlayed)
            {
                soundPlayed = true;
                
                if (newAudioClip != null)
                    other.gameObject.GetComponent<AudioSource>().PlayOneShot(newAudioClip);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player" && allowResetOnExit)
        {
            other.gameObject.GetComponent<MarioMovement>().resetSpriteLibrary();
        }
    }

    public void ChangeSpriteLibrary(GameObject player)
    {
        if (player != null)
        {
            SpriteLibrary spriteLibrary = player.GetComponent<SpriteLibrary>();
            if (spriteLibrary != null)
            {
                spriteLibrary.spriteLibraryAsset = newSpriteLibrary;
            }
        }
    }

    public void ResetSpriteLibrary(GameObject player)
    {
        if (player != null) // Check if the player GameObject is still valid
        {
            MarioMovement marioMovement = player.GetComponent<MarioMovement>();
            if (marioMovement != null)
            {
                marioMovement.resetSpriteLibrary();
            }
        }
    }
}
