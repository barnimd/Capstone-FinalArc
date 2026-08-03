using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Run (tahan Shift)")]
    [Tooltip("Kecepatan lari = kecepatan jalan x nilai ini, lalu ditambahkan ke kecepatan jalan. " +
             "0.75 artinya lari = 175% kecepatan jalan.")]
    [SerializeField] private float runSpeedRatio = 0.75f;
    [SerializeField] private KeyCode runKey = KeyCode.LeftShift;

    /// <summary>True selama player nahan tombol lari dan lagi gerak.</summary>
    public bool IsRunning { get; private set; }

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

        // Kecepatan dasar dikali setting movement dari menu Settings (slider 0..1)
        float walkSpeed = speed * GameSettings.MoveSpeedMultiplier;
        float runSpeed  = walkSpeed * runSpeedRatio;

        // Tahan Shift: kecepatan lari ditambahkan ke kecepatan jalan
        IsRunning = Mathf.Abs(input) > 0.01f && Input.GetKey(runKey);
        float finalSpeed = IsRunning ? walkSpeed + runSpeed : walkSpeed;

        movement.x = input * finalSpeed * Time.deltaTime;
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