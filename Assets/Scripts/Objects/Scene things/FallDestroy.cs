using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallDestroy : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
        if (transform.position.y < -2) {
            Destroy(gameObject);
        }
    }
}
