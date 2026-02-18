using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    private Animator anim;
    private Rigidbody2D rb;
    public float speed = 5f;
    private float jumpForce = 3f;
    
    private float xInput;

    private bool isGrounded;
    private bool facingRight = true;

    private void Awake()
    {
        rb= GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
    }


    private void Update()
    {
        HandleInput();
        HandleMovement();
        HandleAnimations();
        HandleFlip();
    }

    private void HandleInput()
    {
        xInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            Jump();
    }



    private void HandleMovement()
    {
        rb.velocity = new Vector2(xInput * speed, rb.velocity.y);
    }

    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        isGrounded = false;
    }


    private void HandleAnimations()
    {
        bool isMoving = rb.velocity.x != 0;

        anim.SetBool("isMoving", isMoving);
    }

    private void HandleFlip()
    {
        if (rb.velocity.x > 0 && !facingRight)
            flip();
        else if (rb.velocity.x < 0 && facingRight)
            flip();
    }

    private void  flip()
    {
        transform.Rotate(0f, 180f, 0f);
        facingRight = !facingRight;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }


}
