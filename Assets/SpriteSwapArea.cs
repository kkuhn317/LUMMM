using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class SpriteSwapArea : MonoBehaviour
{

    public SpriteLibraryAsset newSpriteLibrary;
    public AudioClip newAudioClip;

    private bool soundPlayed = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player")
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
        if (other.gameObject.tag == "Player")
        {
            other.gameObject.GetComponent<MarioMovement>().resetSpriteLibrary();
        }
    }
}
