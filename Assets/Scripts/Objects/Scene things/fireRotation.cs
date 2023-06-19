using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class fireRotation : MonoBehaviour
{
    public float rotationSpeed = 500f;
    public bool clockwise;

    void Update()
    {
        if (!clockwise) {
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        } else {
            transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
        } 
    }
}
