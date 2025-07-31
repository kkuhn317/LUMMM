using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waterfall : MonoBehaviour
{
    public float pushForce = 20;

    void OnTEnter2D(Collider2D collision)
    {
        OnTriggerStay2D(collision);
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            // Add force to the player
            GameObject player = collision.gameObject;

            player.GetComponent<Rigidbody2D>().AddForce(new Vector2(0, -pushForce));
        }
    }

}
