using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This object will swing from the center of the object back and forth in a smooth motion
public class ObjectSwing : MonoBehaviour
{
    public float swingSpeed = 1.0f; // How fast the object will swing
    public float swingRotation = 45.0f; // How far the object will swing


    private void Update()
    {
        // Calculate the rotation of the object
        float rotation = Mathf.Sin(Time.time * swingSpeed) * swingRotation;

        // Set the rotation of the object
        transform.rotation = Quaternion.Euler(0, 0, rotation);
    }
}
