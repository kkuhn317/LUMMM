using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContinuousRotation : MonoBehaviour
{
    public bool rotateOnX = true;
    public bool rotateOnY = false;
    public bool rotateOnZ = false;

    public float rotationSpeed = 50f;
    public bool anticlockwise = true;

    // FixedUpdate is used for physics-related updates
    void FixedUpdate()
    {
        float direction = anticlockwise ? 1f : -1f;
        float rotationAmount = rotationSpeed * direction * Time.deltaTime;

        if (rotateOnX)
        {
            RotateAroundAxis(Vector3.right, rotationAmount);
        }

        if (rotateOnY)
        {
            RotateAroundAxis(Vector3.up, rotationAmount);
        }

        if (rotateOnZ)
        {
            RotateAroundAxis(Vector3.forward, rotationAmount);
        }
    }

    private void RotateAroundAxis(Vector3 axis, float angle)
    {
        Quaternion deltaRotation = Quaternion.Euler(axis * angle);
        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.MoveRotation(rb.rotation * deltaRotation);
        }
        else
        {
            transform.rotation *= deltaRotation;
        }
    }
}
