using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverScript : MonoBehaviour
{
    // Reference to the player or game controller script
    public MarioMovement playerScript;

    // Name of the Game Over scene
    public string gameOverSceneName;

    // Reference to the audio source
    public AudioSource audioSource;

    void Start()
    {
        // Play the audio clip
        audioSource.Play();
    }

    void Update()
    {
        // Check if the game over condition is met and the song is over
        /*if (playerScript.isGameOver && !audioSource.isPlaying)
        {
            // Load the main scene
            SceneManager.LoadScene("MainMenu");
        }*/
    }
}
