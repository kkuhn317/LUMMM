using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Transform[] points; // array of points for the platform to move between
    public float speed; // speed of the platform movement
    public float waitTime = 0f; // time to wait at each point

    int nextPointNum; // index of the next point in the array
    Vector3 nextPoint; // position of the next point
    bool isWaiting; // indicate if the platform is waiting

    public Vector2 velocity => (nextPoint - transform.position).normalized * speed;

    // Start is called before the first frame update
    void Start()
    {
        nextPointNum = 0;
        isWaiting = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isWaiting && transform.position == nextPoint) // if it's waiting and it's already on the next point
        {
            StartCoroutine(WaitBeforeNextPoint()); // updated to the new target point after the wait is over, which takes a few seconds
        }

        if (!isWaiting) // if the platform is not currently waiting
        {
            nextPoint = points[nextPointNum].position; // update the next target point
            transform.position = Vector3.MoveTowards(transform.position, nextPoint, speed * Time.deltaTime); // move the platform towards the next point
        }
    }

    private IEnumerator WaitBeforeNextPoint()
    {
        isWaiting = true; // set the waiting flag to true
        yield return new WaitForSeconds(waitTime); // wait for the specified time

        nextPointNum++; // increment the next point index
        if (nextPointNum >= points.Length) // wrap around if the index exceeds the array length
        {
            nextPointNum = 0; // reset the index to the first point
        }
        nextPoint = points[nextPointNum].position; // update the next point position
        isWaiting = false; // set the waiting flag to false to resume movement
    }

    private void OnDrawGizmos()
    {
        // draw lines between points
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] != null)
            {
                if (i < points.Length - 1)
                {
                    Gizmos.DrawLine(points[i].position, points[i + 1].position);
                }
                else
                {
                    Gizmos.DrawLine(points[i].position, points[0].position);
                }
            }
        }
    }
}
