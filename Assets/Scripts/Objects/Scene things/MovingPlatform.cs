using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Transform[] points;
    public float speed;

    int nextPointNum;
    Vector3 nextPoint;

    // Start is called before the first frame update
    void Start()
    {
        nextPointNum = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if(transform.position == nextPoint)
        {
            if(points.Length == 1)
            {
                return;
            }
            nextPointNum++;
            if(nextPointNum >= points.Length)
            {
                nextPointNum = 0;
            }
        }
        //print(nextPoint);
        //print(nextPointNum);
        nextPoint = points[nextPointNum].position;
        transform.position = Vector3.MoveTowards(transform.position, nextPoint, speed * Time.deltaTime);
    }

    private void OnDrawGizmos()
    {
        // draw lines between points
        for(int i = 0; i < points.Length; i++)
        {
            if(points[i] != null)
            {
                if(i < points.Length - 1)
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
