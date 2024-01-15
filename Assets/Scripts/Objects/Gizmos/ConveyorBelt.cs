using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ConveyorDirection
{
    Left,
    Right
}

public class ConveyorBelt : MonoBehaviour
{
    public GameObject left;
    public GameObject middle;
    public GameObject right;

    [HideInInspector]
    public int length;
    public float speed;
    public ConveyorDirection direction;

    private Rigidbody2D rb;
    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        animator.SetBool("direction", direction == ConveyorDirection.Right);
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;
        if (player.transform.parent == transform)
        {
            Vector3 pos = transform.position;
            float moveAmount = speed * GetDirectionMultiplier() * Time.deltaTime;
            transform.position += new Vector3(moveAmount, 0);
            player.transform.parent = null;
            transform.position = pos;
            player.transform.parent = transform;
        }
    }

    private int GetDirectionMultiplier()
    {
        return direction == ConveyorDirection.Right ? 1 : -1;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.parent = transform;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.parent = null;
        }
    }

    public void ChangeLength(int length)
    {
        this.length = length;
        left.transform.localPosition = new Vector3(-(((float)length - 1) / 2), 0, 0);
        middle.GetComponent<SpriteRenderer>().size = new Vector2(length - 2, 1);
        right.transform.localPosition = new Vector3((((float)length - 1) / 2), 0, 0);

        GetComponent<BoxCollider2D>().size = new Vector2(length, 1);
    }
}
