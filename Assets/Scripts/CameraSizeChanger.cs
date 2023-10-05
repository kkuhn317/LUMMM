using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSizeChanger : MonoBehaviour
{
    [SerializeField] float newSize = 5.0f; // Set the new size you want here.
    [SerializeField] float returnDelay = 3.0f; // Delay before returning to original size.
    private float originalSize;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        if (mainCamera != null)
        {
            originalSize = mainCamera.orthographicSize;
            ChangeCameraSize(newSize);
        }
    }

    private void ChangeCameraSize(float size)
    {
        if (mainCamera != null)
        {
            mainCamera.orthographicSize = size;

            if (returnDelay > 0)
            {
                StartCoroutine(ReturnToOriginalSizeAfterDelay(returnDelay));
            }
        }
    }

    IEnumerator ReturnToOriginalSizeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (mainCamera != null)
        {
            mainCamera.orthographicSize = originalSize;
        }
    }
}
