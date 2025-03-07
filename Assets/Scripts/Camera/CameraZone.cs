using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZone : MonoBehaviour
{
    public Vector2 topLeft;
    public Vector2 bottomRight;
    public bool lockToHorizontal = false;
    public bool lockToVertical = false;
    public Vector2 lockOffset = Vector2.zero;
    public bool snapToBounds = false; // if true, it will never display anything outside of the bounds
    private CameraFollow cameraFollow;

    float horizontalMiddle => (topLeft.x + bottomRight.x) / 2;
    float verticalMiddle => (topLeft.y + bottomRight.y) / 2;

    public int priority = 0;    // If Mario is in multiple CameraZones, the camera uses the one with the highest priority

    // Start is called before the first frame update
    void Start()
    {
        cameraFollow = GetComponent<CameraFollow>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public float cameraMinX => lockToVertical ? horizontalMiddle + lockOffset.x : topLeft.x + (cameraFollow.camWidth / 2);
    public float cameraMaxX => lockToVertical ? horizontalMiddle + lockOffset.x : bottomRight.x - (cameraFollow.camWidth / 2);
    public float cameraMinY => lockToHorizontal ? verticalMiddle + lockOffset.y : bottomRight.y + (cameraFollow.camHeight / 2);
    public float cameraMaxY => lockToHorizontal ? verticalMiddle + lockOffset.y : topLeft.y - (cameraFollow.camHeight / 2);

    private void OnDrawGizmos ()
    {
        // draw a square around the camera bounds
        Gizmos.color = Color.red;
        Gizmos.DrawLine(topLeft, new Vector2(bottomRight.x, topLeft.y));
        Gizmos.DrawLine(topLeft, new Vector2(topLeft.x, bottomRight.y));
        Gizmos.DrawLine(bottomRight, new Vector2(bottomRight.x, topLeft.y));
        Gizmos.DrawLine(bottomRight, new Vector2(topLeft.x, bottomRight.y));

        // draw a line to show the horizontal lock position
        if (lockToHorizontal && !lockToVertical)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector2(topLeft.x, verticalMiddle + lockOffset.y), new Vector2(bottomRight.x, verticalMiddle + lockOffset.y));
        } else if (lockToVertical && !lockToHorizontal)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector2(horizontalMiddle + lockOffset.x, topLeft.y), new Vector2(horizontalMiddle + lockOffset.x, bottomRight.y));
        } else if (lockToHorizontal && lockToVertical)
        {
            Gizmos.color = Color.green;
            // Draw dot at the lock position
            Gizmos.DrawSphere(new Vector2(horizontalMiddle + lockOffset.x, verticalMiddle + lockOffset.y), 0.5f);
        }
    }
}
