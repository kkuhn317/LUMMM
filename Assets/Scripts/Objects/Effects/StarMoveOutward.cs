using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Used for the stars that appear when you get a key or enter a door or get a checkpoint etc.
public class StarMoveOutward : MonoBehaviour
{
    public Vector3 direction;
    public float speed;

    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, 1);
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }
}
