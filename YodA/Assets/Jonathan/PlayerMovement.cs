using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 8f;

    [Header("Ground Check (Raycast)")]
    [Tooltip("Layer(s) considered ground. Set this to your Floor layer, not the Floor tag.")]
    public LayerMask groundLayer;
    public LayerMask cloudLayer;


    [Tooltip("How far below the player's feet to check for ground.")]
    public float groundCheckDistance = 0.15f;

    [Tooltip("Optional: assign an empty child GameObject placed at the player's feet. If left empty, the collider's bottom edge is used instead.")]
    public Transform groundCheckPoint;

    [Tooltip("Small grace window (seconds) after walking off a ledge where jumping is still allowed.")]
    public float coyoteTime = 0.1f;

    private Rigidbody2D rb;
    private Collider2D col;
    private bool isGrounded;
    private float coyoteTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        CheckGrounded();

        // Walking
        float moveInput = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // Coyote time: keep a short window where jumping is still allowed
        // right after leaving the ground (e.g. walking off a ledge).
        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        // Jumping
        if (Input.GetButtonDown("Jump") && coyoteTimer > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            coyoteTimer = 0f; // prevent double-jumping during the same grace window
        }
    }

    void CheckGrounded()
    {
        Vector2 origin;

        if (groundCheckPoint != null)
        {
            origin = groundCheckPoint.position;
        }
        else if (col != null)
        {
            // Fall back to the bottom-center of the collider's bounds if no groundCheckPoint is set.
            origin = new Vector2(col.bounds.center.x, col.bounds.min.y);
        }
        else
        {
            origin = transform.position;
        }

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer | cloudLayer);
        isGrounded = hit.collider != null;

        // Uncomment to visualize the ray in the Scene view:
        // Debug.DrawRay(origin, Vector2.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.gameObject.tag == "Cloud")
        {
            Physics2D.gravity = new Vector2(0f, -3f);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.tag == "Cloud")
        {
            Invoke("ChangeToDefaultGravity", 0.2f);
        }
    }

    void ChangeToDefaultGravity()
    {
        Physics2D.gravity = new Vector2(0f, -9.81f);
    }
}