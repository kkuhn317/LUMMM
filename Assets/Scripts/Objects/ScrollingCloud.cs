using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollingCloud : MonoBehaviour
{
    public float moveSpeed = -0.5f;
    public float resetPosition = -20f;
    public float startPosition = 20f;

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);

        if ((transform.position.x <= resetPosition && moveSpeed < 0) || (transform.position.x >= resetPosition && moveSpeed > 0))
        {
            float xOffset = transform.position.x - resetPosition;
            transform.position = new Vector3(startPosition + xOffset, transform.position.y, transform.position.z);
        }
    }
}
