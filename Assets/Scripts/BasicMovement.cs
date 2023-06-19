using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    public float speed;
    private bool facingRight = true;
    private float horizontalMove;
    public Animator animator;

    private float JumpTime;
    public float JumpStartTime;
    public float jumpForce;
    private bool isJumping;

    private bool isGrounded; //When touch the ground
    public Transform groundCheck;
    public float checkRadius;
    public LayerMask WhatisGround;

    private int extraJumps; 
    public int extraJumpsValue;

    void Jump()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, WhatisGround);

        if (isGrounded == true && Input.GetButtonDown("Jump"))
        {
            isJumping = true;
            JumpTime = JumpStartTime;
            rb.velocity = Vector2.up * jumpForce;
        }

        if (Input.GetButton("Jump") && isJumping == true)
        {
            if (JumpTime > 0)
            {
                rb.velocity = Vector2.up * jumpForce;
                JumpTime -= Time.deltaTime;
            }
            else
            {
                isJumping = false;
            }
        }
        
        if (Input.GetButtonUp("Jump"))
        {
            isJumping = false;
        }

        if (isGrounded == true)
        {
            extraJumps = extraJumpsValue;
        }

        if (Input.GetButtonDown("Jump") && extraJumps > 0)
        {
            isJumping = true;
            extraJumps--;
            rb.velocity = Vector2.up * jumpForce;
        }

        if (Input.GetButtonUp("Jump"))
        {
            isJumping = false;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        extraJumps = extraJumpsValue;
    }

    void FixedUpdate()
    {
        rb.velocity = new Vector2(horizontalMove * speed, rb.velocity.y);
        animator.SetFloat("Horizontal", Mathf.Abs(horizontalMove));
    }

    // Update is called once per frame
    void Update()
    {
        horizontalMove = Input.GetAxisRaw("Horizontal");
        if (horizontalMove < 0.0f && facingRight)
        {
            FlipPlayer();
        }

        if (horizontalMove > 0.0f && !facingRight)
        {
            FlipPlayer();
        }

        Jump();    

    }

    void FlipPlayer()
    {
        facingRight = !facingRight;
        Vector2 playerScale = gameObject.transform.localScale;
        playerScale.x *= -1;
        transform.localScale = playerScale;
    }
}
