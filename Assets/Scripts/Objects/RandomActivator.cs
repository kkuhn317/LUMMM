using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Used for objects that have a certain chance to appear in the level
// For example: easter eggs like the golden goomba picture in goomba pyramid
public class RandomActivator : MonoBehaviour
{
    public float activationProbability = 0.5f; // Probability (0 to 1) of activation on scene load

    void Start()
    {
        // Generate a random number between 0 and 1
        float randomValue = Random.Range(0f, 1f);

        // Check if the random value is less than the activation probability
        if (randomValue < activationProbability)
        {
            // Activate the GameObject
            gameObject.SetActive(true);
        }
        else
        {
            // Deactivate the GameObject
            gameObject.SetActive(false);
        }
    }
}
