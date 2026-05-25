using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownPlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private Animator animator;

    public bool CanMove { get; set; } = true;

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
            if (animator != null) animator.SetBool("isMoving", false);
            return;
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // Normalize diagonal movement so it's not faster than axis-aligned
        if (movement.sqrMagnitude > 1f)
            movement = movement.normalized;

        bool isMoving = movement.sqrMagnitude > 0.01f;
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
        rb.velocity = CanMove ? movement * speed : Vector2.zero;
    }
}
