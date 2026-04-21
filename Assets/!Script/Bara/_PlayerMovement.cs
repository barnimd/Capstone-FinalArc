using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    private Vector2 movement;
    private float xPostLastFrame;

    private void Awake()
    {

    }

    private void Start()
    {
        
    }

    void Update()
    {
        HandleMovement();
        FlipCharacterX();
    }
    private void FlipCharacterX()
    {
        float input = Input.GetAxis("Horizontal");
        // moving right
        if (input > 0 && (transform.position.x > xPostLastFrame))
        {
            spriteRenderer.flipX = false;
        }
        //moving left
        else if (input < 0 && (transform.position.x < xPostLastFrame))
        {
            spriteRenderer.flipX = true;
        }

        xPostLastFrame = transform.position.x;
    }
    private void HandleMovement ()
    {
        float input = Input.GetAxis("Horizontal");
        movement.x = input * speed * Time.deltaTime;
        transform.Translate(movement);

        if(input != 0)
        {
            animator.SetBool("isMoving", true);
        }
        else
        {
            animator.SetBool("isMoving", false);
        }
    }
}