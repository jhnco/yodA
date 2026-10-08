using UnityEngine;

/// <summary>
/// Attach to the box prefab (needs Rigidbody2D + Collider2D already on it).
/// Watches its own vertical velocity and reports back to GameManager once
/// it has settled on the ground or on top of another box, so the player
/// can then walk on it as a platform.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class FallingBox : MonoBehaviour
{
    public GameManager manager;

    private Rigidbody2D rb;
    private bool hasLanded = false;
    private float settleCheckDelay = 0.1f; // ignore the first instant of freefall
    private float timer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (hasLanded) return;

        timer += Time.fixedDeltaTime;
        if (timer < settleCheckDelay) return;

        // "Landed" = essentially stopped moving vertically.
        if (Mathf.Abs(rb.linearVelocity.y) < 0.05f)
        {
            hasLanded = true;
            manager?.NotifyBoxLanded(this);
        }
    }
}
