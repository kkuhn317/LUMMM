using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public string playerTag = "Player";

    private GameObject target;

    private GameObject[] Players;
    public float leftBounds;
    public float rightBounds;

    public float smoothDampTime = 0.15f;
    private Vector3 smoothDampVelocity = Vector3.zero;

    private float camWidth, camHeight, levelMinX, levelMaxX;

    // Start is called before the first frame update
    void Start()
    {
        camHeight = Camera.main.orthographicSize * 2;
        camWidth = camHeight * Camera.main.aspect;

        levelMinX = leftBounds + (camWidth / 2);
        levelMaxX = rightBounds - (camWidth / 2);

    }

    // Update is called once per frame
    void Update()
    {
        Players = GameObject.FindGameObjectsWithTag("Player");
    
        if (Players.Length > 0)
        {
            target = Players[0];

            float targetX = Mathf.Max(levelMinX, Mathf.Min(levelMaxX, target.transform.position.x));

            float x = Mathf.SmoothDamp(transform.position.x, targetX, ref smoothDampVelocity.x, smoothDampTime);

            transform.position = new Vector3(x, transform.position.y, transform.position.z);
        }
    }

}
