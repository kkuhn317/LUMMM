using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallDestroy : MonoBehaviour
{
    // destroy when it goes off screen
    void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}
