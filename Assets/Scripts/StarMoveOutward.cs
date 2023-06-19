using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
