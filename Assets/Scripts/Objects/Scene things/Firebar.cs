using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Firebar : MonoBehaviour
{

    [Header("Rotation speed")]
    public float firebarRotationSpeed; //declare a public variable for the fire bar rotation speed
    //Direction of rotation
    public bool clockwise;

    [Header("Rotation angle")]
    public float firebarRotationAngle; //declare a public variable for the fire bar rotation angle

    [Header("Components")]
    //Number of fireballs in the bar
    public int numFireballs;
    //Prefab for the fireball game object
    public GameObject fireballPrefab;
    //Distance between the fireballs
    public float fireballDistance;
    //Radius of the fireball rotation circle
    public float fireballRadius;

    // Array to hold references to the fireball game objects
    private GameObject[] fireballs;
    //Center position of the fireball rotation
    private Vector3 centerPosition;


    void Start()
    {
        centerPosition = transform.position;
        fireballs = new GameObject[numFireballs];
        for (int i = 0; i < numFireballs; i++)
        {
            float angle = i * Mathf.PI * 2 / numFireballs;
            Vector3 pos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * fireballRadius;
            fireballs[i] = Instantiate(fireballPrefab, centerPosition + pos, Quaternion.identity);
            fireballs[i].transform.SetParent(transform);
        }
        transform.rotation = Quaternion.Euler(0, 0, firebarRotationAngle);
    }

    void Update()
    {
        float angleIncrement = (clockwise ? -1 : 1) * 360f / numFireballs;
        transform.Rotate(Vector3.forward, angleIncrement * Time.deltaTime * firebarRotationSpeed);

        for (int i = 0; i < numFireballs; i++)
        {
            float angle = i * Mathf.PI * 2 / numFireballs;
            Vector3 pos = new Vector3(i * fireballDistance, 0, 0);
            pos = Quaternion.AngleAxis(transform.rotation.eulerAngles.z, Vector3.forward) * pos;
            if (fireballs[i] != null)
            {
                fireballs[i].transform.position = centerPosition + pos;
            }
        }
    }

    public void SetNumFireballs(int newNumFireballs)
    {
        if (newNumFireballs == numFireballs)
        {
            return;
        }

        GameObject[] newFireballs = new GameObject[newNumFireballs];
        for (int i = 0; i < newNumFireballs; i++)
        {
            if (i < numFireballs)
            {
                newFireballs[i] = fireballs[i];
            }
            else
            {
                float angle = i * Mathf.PI * 2 / newNumFireballs;
                Vector3 pos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * fireballRadius;
                newFireballs[i] = Instantiate(fireballPrefab, centerPosition + pos, Quaternion.identity);
                newFireballs[i].transform.SetParent(transform);
            }
        }

        for (int i = newNumFireballs; i < numFireballs; i++)
        {
            Destroy(fireballs[i]);
        }

        numFireballs = newNumFireballs;
        fireballs = newFireballs;
    }
}
