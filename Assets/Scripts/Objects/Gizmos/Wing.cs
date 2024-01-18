using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wing : MonoBehaviour
{
    public float fallSpeed = 1f;
    public bool leftWing = false;  // Changes the fall animation to the left wing
    private bool falling = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void Fall()
    {
        if (!falling) {
            falling = true;
            GetComponent<Animator>().SetTrigger(leftWing ? "FallLeft" : "FallRight");
        }
        

    }

    // Update is called once per frame
    void Update()
    {
        if (falling)
        {
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        }
    }

    private void OnBecameInvisible()
    {
        // once the wing is off screen, destroy it
        if (falling)
        {
            Destroy(gameObject);
        }
    }


}
