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

    private void OnDrawGizmos()
    {
        // Draw reset position
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(resetPosition, transform.position.y - 5, transform.position.z),
                        new Vector3(resetPosition, transform.position.y + 5, transform.position.z));

        // Draw start position
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(startPosition, transform.position.y - 5, transform.position.z),
                        new Vector3(startPosition, transform.position.y + 5, transform.position.z));

        // Draw cloud's current position
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position, 0.3f);
    }
}
