using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class NoteBlockController : MonoBehaviour
{
    public float jumpForce = 10f;
    public float animationDuration = 0.2f;
    public AudioClip[] noteSounds; //Allows you to add a sound clips in an array
    public float[] noteSoundProbabilities; //Allows you to add percentage occurrence for each note sound

    private Vector2 startPosition;
    private bool isAnimating = false;

    void Start()
    {
        startPosition = transform.position;
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player") && !isAnimating)
        {
            //Choose random note sound effect based on probabilities
            float totalProbability = 0f;
            for (int i = 0; i < noteSoundProbabilities.Length; i++)
            {
                totalProbability += noteSoundProbabilities[i];
            }
            float randomValue = Random.Range(0f, totalProbability);
            int soundIndex = 0;
            float probabilitySum = 0f;
            for (int i = 0; i < noteSoundProbabilities.Length; i++)
            {
                probabilitySum += noteSoundProbabilities[i];
                if (randomValue <= probabilitySum)
                {
                    soundIndex = i;
                    break;
                }
            }
            AudioClip noteSound = noteSounds[soundIndex];

            //Play note sound effect
            AudioSource audioSource = GetComponent<AudioSource>();
            audioSource.clip = noteSound;
            audioSource.Play();

            //Add jump boost to player (still needs work)
            MarioMovement playerController = other.gameObject.GetComponent<MarioMovement>();
            //playerController.Jump(InputAction.CallbackContext);

            //Trigger animation based on player position
            Vector2 playerPosition = other.gameObject.transform.position;
            Vector2 blockPosition = transform.position;
            if (playerPosition.y < blockPosition.y) //If player's position when collide with block is lower than the block position, the block animation when goes up starts
            {
                StartCoroutine(AnimateBlock(transform.position, transform.position + Vector3.up * 0.5f, animationDuration));
            }
            else //If player's position is higher, then go down note block animation starts
            {
                StartCoroutine(AnimateBlock(transform.position, transform.position + Vector3.down * 0.5f, animationDuration));
            }
        }
    }

    IEnumerator AnimateBlock(Vector3 startPosition, Vector3 endPosition, float duration)
    {
        isAnimating = true;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = endPosition;
        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            transform.position = Vector3.Lerp(endPosition, startPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = startPosition;
        isAnimating = false;
    }
}
