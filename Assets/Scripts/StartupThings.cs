using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartupThings : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (Application.isMobilePlatform) {
            Application.targetFrameRate = 60;   // Default is 30 (ew)
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
