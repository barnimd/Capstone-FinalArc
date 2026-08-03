using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownPlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private Animator animator;

    [Header("Run (tahan Shift)")]
    [Tooltip("Kecepatan lari = kecepatan jalan x nilai ini, lalu ditambahkan ke kecepatan jalan. " +
             "0.75 artinya lari = 175% kecepatan jalan.")]
    [SerializeField] private float runSpeedRatio = 0.75f;
    [SerializeField] private KeyCode runKey = KeyCode.LeftShift;

    public bool CanMove { get; set; } = true;

    /// <summary>True selama player nahan tombol lari dan lagi gerak.</summary>
    public bool IsRunning { get; private set; }

    private Rigidbody2D rb;
    private Vector2 movement;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!CanMove)
        {
            movement = Vector2.zero;
            IsRunning = false;
            if (animator != null) animator.SetBool("isMoving", false);
            return;
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // Normalize diagonal movement so it's not faster than axis-aligned
        if (movement.sqrMagnitude > 1f)
            movement = movement.normalized;

        bool isMoving = movement.sqrMagnitude > 0.01f;
        IsRunning = isMoving && Input.GetKey(runKey);
        animator.SetBool("isMoving", isMoving);

        if (isMoving)
        {
            animator.SetFloat("moveX", movement.x);
            animator.SetFloat("moveY", movement.y);
        }
        // When idle: leave moveX/moveY at last values so blend tree holds last facing direction
    }

    private void FixedUpdate()
    {
        // Kecepatan dasar dikali setting movement dari menu Settings (slider 0..1)
        float walkSpeed = speed * GameSettings.MoveSpeedMultiplier;
        float runSpeed  = walkSpeed * runSpeedRatio;

        // Tahan Shift: kecepatan lari ditambahkan ke kecepatan jalan
        float finalSpeed = IsRunning ? walkSpeed + runSpeed : walkSpeed;

        rb.velocity = CanMove ? movement * finalSpeed : Vector2.zero;
    }
}
