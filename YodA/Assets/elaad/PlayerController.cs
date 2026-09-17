using UnityEngine;

/// <summary>
/// Standard 2D platformer movement, enabled by GameManager once the
/// required number of boxes have been dropped. Walks left/right and
/// jumps with Space; a ground check lets it stand on the floor AND
/// on top of the dropped boxes (put boxes + floor on the "Ground" layer).
///
/// Setup:
///  - Requires Rigidbody2D (Dynamic, Freeze Rotation Z) + Collider2D.
///  - Assign groundCheck: an empty child Transform placed at the player's feet.
///  - Set groundLayer to whatever layer your floor and boxes are on.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 9f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        isGrounded = groundCheck != null &&
            Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal"); // or Input.GetKey(KeyCode.LeftArrow/RightArrow)
        rb.linearVelocity = new Vector2(h * moveSpeed, rb.linearVelocity.y);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
