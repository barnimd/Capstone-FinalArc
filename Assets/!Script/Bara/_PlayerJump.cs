using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _PlayerJump : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rigidBody;
    [SerializeField] private float jumpForce = 5f;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Jump") && GetIsGrounded())
        {
            Jump();
        }
    }


    private bool GetIsGrounded()
    {
        return Physics2D.Raycast(transform.position, Vector2.down, 1.5f, LayerMask.GetMask("Ground"));
    }
    private void Jump()
    {
        rigidBody.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);    
    }
}
