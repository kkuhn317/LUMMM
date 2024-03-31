using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            if (inputBuffer.Contains(secretCodes[0]))
            {
                GlobalVariables.enablePlushies = true;
                audioSource.Play();
                inputBuffer = ""; // Clear the input buffer.
            } else if (inputBuffer.Contains(secretCodes[1]))
            {
                GlobalVariables.enableBetaMode = true;
                audioSource.Play();
                inputBuffer = ""; // Clear the input buffer.
            }

            // Trim the input buffer to the length of the longest secret code.
            if (inputBuffer.Length > secretCodes.Max(x => x.Length))
            {
                //inputBuffer = inputBuffer.Substring(inputBuffer.Length - secretCode.Length);
                inputBuffer = inputBuffer.Substring(inputBuffer.Length - secretCodes.Max(x => x.Length));
            }
        }
    }

























        private string[] secretCodes = new string[]
    {
        "club",
        "supersecretbeta",
    };
}
