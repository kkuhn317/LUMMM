using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonRotation : MonoBehaviour
{
    public Transform referencedObject; // Reference to another GameObject whose children should rotate
    public float rotationDuration = 0.25f; // Duration of rotation animation

    // Dictionary to keep track of active rotation coroutines for each GameObject
    private Dictionary<GameObject, Coroutine> activeRotations = new Dictionary<GameObject, Coroutine>();

    // Rotates the GameObject attached to this script and each child of the referencedObject by a given amount.
    public void Rotate(float rotationAmount)
    {
        // Rotate the GameObject this script is attached to
        RotateObject(this.gameObject, rotationAmount, rotationDuration);

        // If there's a referenced object, apply the rotation to each of its children with a slight delay
        if (referencedObject != null)
        {
            int index = 0;
            foreach (Transform child in referencedObject)
            {
                if (child != null) // Ensure child is not null
                {
                    RotateObject(child.gameObject, rotationAmount, rotationDuration, 0.1f * index);
                    index++;
                }
            }
        }
    }

    // Starts a rotation coroutine, canceling any previous rotation on the same object and resetting to 0 first
    private void RotateObject(GameObject obj, float rotationAmount, float duration, float delay = 0f)
    {
        // Cancel any existing rotation on this object
        if (activeRotations.ContainsKey(obj) && activeRotations[obj] != null)
        {
            StopCoroutine(activeRotations[obj]);
        }

        // Reset rotation to 0
        obj.transform.eulerAngles = new Vector3(obj.transform.eulerAngles.x, obj.transform.eulerAngles.y, 0);

        // Start a new rotation coroutine and store it in the dictionary
        activeRotations[obj] = StartCoroutine(RotateCoroutine(obj, rotationAmount, duration, delay));
    }

    // Coroutine to smoothly rotate an object by the specified amount over a given duration
    private IEnumerator RotateCoroutine(GameObject obj, float rotationAmount, float duration, float delay)
    {
        // Wait for the delay before starting the rotation
        yield return new WaitForSeconds(delay);

        float startRotation = 0f;
        float endRotation = rotationAmount;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float currentRotation = Mathf.Lerp(startRotation, endRotation, elapsedTime / duration);
            obj.transform.eulerAngles = new Vector3(obj.transform.eulerAngles.x, obj.transform.eulerAngles.y, currentRotation);
            yield return null;
        }

        // Ensure final rotation matches the target rotation
        obj.transform.eulerAngles = new Vector3(obj.transform.eulerAngles.x, obj.transform.eulerAngles.y, endRotation);

        // Remove the coroutine reference from the dictionary, as it has completed
        activeRotations[obj] = null;
    }
}
