using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public enum MovementType { Loop, PingPong, StopOnEnd }

public class movingPlatform1 : MonoBehaviour
{
    public bool connectFromFirstPoint; //This is just to draw a line
    public Transform targetPlatform; //Reference the platform gameObject
    [Header("Automatic detection")]
    public GameObject ways;
    public Transform[] points;
    private int currentPointIndex;
    [Header("Direction and speed")]
    public float speed; //platform speed
    public int startingPoint;
    private int direction = 1; //1 for forward, -1 for backward (Avoid OutofRangeException)
    [Header("Movement type")]
    public MovementType movementType; //Allows choose between each movement type
    [Header("Condition")]
    private bool reverse = false; // initialize the reverse variable to false
    public bool isMoving;

    private void Awake()
    {
        points = new Transform[ways.transform.childCount];
        for (int i = 0; i < ways.gameObject.transform.childCount; i++)
        {
            points[i] = ways.gameObject.transform.GetChild(i).gameObject.transform;
        }
    }


    private void Start()
    {
        //The platform will move to the end Position, otherwise the B position
        transform.Translate((points[currentPointIndex].position - transform.position).normalized * speed * Time.deltaTime, Space.World);
        currentPointIndex = startingPoint;
    }

    private void Update()
    {
        switch (movementType)
        {
            case MovementType.Loop:
                LoopMovement();
                break;
            case MovementType.PingPong:
                PingPongMovement();
                break;
            case MovementType.StopOnEnd:
                StopOnEndMovement();
                break;
        }
    }

    private void LoopMovement()
    {
        if (Vector2.Distance(transform.position, points[currentPointIndex].position) < 0.05f)
        {
            currentPointIndex++; //iterate the index
            //Check if the platform was on the last point after the index increase
            if (currentPointIndex == points.Length) //If the index reach the total number of elements
            {
                currentPointIndex = 0; //reset the index
            }
        }
        //Then, moving the platform to the point position with the index "currentIndex"
        transform.position = Vector3.MoveTowards(transform.position, points[currentPointIndex].position, speed * Time.fixedDeltaTime);
        /*Note that transform.Translate() uses a direction and distance to move the object, whereas transform.position sets the object's 
         * position directly. In this case, we calculate the direction using (points[currentIndex].position - transform.position).normalized, 
         * normalize it to get a direction vector of length 1, and multiply it by speed * Time.deltaTime to get the distance the platform should 
         * move in one frame. The Space.World parameter specifies that we want to move the platform in world space, rather than local space.*/
    }

    private void PingPongMovement()
    {
        if (Vector2.Distance(transform.position, points[currentPointIndex].position) < 0.05f)
        {
            currentPointIndex += direction; //iterate the index
            //Check if the platform was on the last or first point after the index increase or decrease
            if (currentPointIndex == points.Length - 1 || currentPointIndex == 0)
            {
                direction *= -1; //reverse the direction
                reverse = !reverse; // toggle the reverse variable
            }
        }
        //Then, moving the platform to the point position with the index "i"
        transform.position = Vector3.MoveTowards(transform.position, points[currentPointIndex].position, speed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Used for StoponEnd
        if (other.CompareTag("Player"))
        {
            isMoving = true;
        }
    }

    private void StopOnEndMovement()
    {
        if (isMoving)
        {
            if (Vector2.Distance(transform.position, points[currentPointIndex].position) < 0.05f)
            {
                if (currentPointIndex == points.Length - 1)
                {
                    speed = 0f;
                }
                else
                {
                    currentPointIndex++; //iterate the index
                }
            }
            //Then, moving the platform to the point position with the index (currentPointIndex)
            transform.position = Vector3.MoveTowards(transform.position, points[currentPointIndex].position, speed * Time.fixedDeltaTime);
        }
    }
    public void ChangeMovementType(MovementType newMovementType)
    {
        movementType = newMovementType;

        switch (newMovementType)
        {
            case MovementType.Loop:
                if (currentPointIndex >= points.Length)
                {
                    currentPointIndex = 0;
                }
                break;
            case MovementType.PingPong:
                if (currentPointIndex >= points.Length)
                {
                    if (reverse)
                    {
                        currentPointIndex = points.Length - 1;
                    }
                    else
                    {
                        currentPointIndex = 0;
                    }
                }
                break;
            case MovementType.StopOnEnd:
                currentPointIndex = Mathf.Clamp(currentPointIndex, 0, points.Length - 1);
                break;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (transform.position.y < collision.transform.position.y - 0.8f)
            {
                collision.transform.SetParent(transform);
            }
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
        }
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        //Draw a line between the platform and the first point
        if (targetPlatform != null)
        {
            if (connectFromFirstPoint)
            {
                Gizmos.DrawLine(ways.transform.GetChild(0).position, targetPlatform.position);
            }
            else
            {
                Gizmos.DrawLine(transform.position, targetPlatform.position);
            }
        }

        //Draw a line between each point to show the movement path
        Transform[] wayPoints = new Transform[ways.transform.childCount];
        for (int i = 0; i < wayPoints.Length; i++)
        {
            wayPoints[i] = ways.transform.GetChild(i);
        }

        for (int i = 0; i < wayPoints.Length - 1; i++)
        {
            Gizmos.DrawLine(wayPoints[i].position, wayPoints[i + 1].position);
        }

        //If the movement type is set to Loop, draw a line between the last and first points
        if (movementType == MovementType.Loop)
        {
            Gizmos.DrawLine(wayPoints[wayPoints.Length - 1].position, wayPoints[0].position);
        }

        //If the platform is looping, draw a line from the last point to the first point
        if (wayPoints.Length > 1 && wayPoints[0] != null && wayPoints[wayPoints.Length - 1] != null)
        {
            Gizmos.DrawLine(wayPoints[0].position, wayPoints[wayPoints.Length - 1].position);
        }

        if (wayPoints == null) return;

        //Draw a sphere at each point to make it more visible
        for (int i = 0; i < wayPoints.Length; i++)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(wayPoints[i].position, 0.2f);

            //Draw the index of the waypoint next to the sphere
            Vector2 guiPosition = Camera.current.WorldToScreenPoint(wayPoints[i].position);
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 14;
            Handles.Label(new Vector3(guiPosition.x, Screen.height - guiPosition.y, 0), i.ToString(), style);
        }
    }
#endif
}
