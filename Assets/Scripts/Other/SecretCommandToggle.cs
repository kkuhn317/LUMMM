using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script does absolutely nothing. I promise. Don't look at it.
public class SecretCommandToggle : MonoBehaviour
{
    private string inputBuffer = "";
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (Input.anyKeyDown)
        {
            inputBuffer += Input.inputString;

            // Check if the input buffer contains the secret code.
            if (inputBuffer.Contains(secretCode))
            {
                GlobalVariables.enablePlushies = true;
                audioSource.Play();
                inputBuffer = ""; // Clear the input buffer.
            }

            // Trim the input buffer to the length of the secret code.
            if (inputBuffer.Length > secretCode.Length)
            {
                inputBuffer = inputBuffer.Substring(inputBuffer.Length - secretCode.Length);
            }
        }
    }
















































    private string secretCode = "club";
}
