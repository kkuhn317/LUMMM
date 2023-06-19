using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityMovement : MonoBehaviour
{
    private new Rigidbody2D rigidbody;
    private Vector2 velocity;
    public Vector2 direction = Vector2.left;
    public float speed = 1f;
    public LayerMask layerMask;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        enabled = false;
    }

    private void OnBecameVisible()
    {
        enabled = true;
    }

    private void OnBecameInvisible()
    {
        enabled = false;
    }

    private void OnEnable()
    {
        rigidbody.WakeUp();        
    }

    private void OnDisable()
    {
        rigidbody.velocity = Vector2.zero;
        rigidbody.Sleep();
    }

    private void FixedUpdate()
    {
        velocity.x = direction.x * speed;
        velocity.y += Physics2D.gravity.y * Time.fixedDeltaTime;
        rigidbody.MovePosition(rigidbody.position + velocity * Time.fixedDeltaTime);

        if (Physics2D.Raycast(transform.position, direction, 0.5f, layerMask))
        {
            direction = -direction;
        }

        if (Physics2D.Raycast(transform.position, Vector2.down, 0.5f, layerMask))
        {
            velocity.y = Mathf.Max(velocity.y, 0f);
        }

        Debug.DrawRay(transform.position, direction, Color.green, 0.1f);
    }
}
