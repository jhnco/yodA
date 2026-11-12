using UnityEngine;
using UnityEngine.SceneManagement;

// Works with the existing PlayerMovement script WITHOUT modifying it.
// It only uses PlayerMovement's public fields (wantedSpeed, jumping, grounded)
// and the player's Rigidbody2D.
public class QuickSand : MonoBehaviour
{
    [Header("Movement Penalties (1 = normal)")]
    [SerializeField] float speedMultiplier = 0.4f;   // Horizontal speed while in quicksand
    [SerializeField] float jumpMultiplier = 0.5f;    // Jump strength while in quicksand

    [Header("Sinking")]
    [SerializeField] float sinkSpeed = 0.5f;         // Slow downward speed (units/sec)
    [SerializeField] float deathDepth = 2.2f;        // How far the player's FEET must sink below the surface to restart

    [Header("Visual Warning")]
    [Range(0f, 1f)]
    [SerializeField] float warningStart = 0.6f;      // Start tinting at this fraction of deathDepth
    [SerializeField] Color warningColor = Color.black; // Sprite tint at the moment of death

    // Runtime state
    Collider2D sandCollider;
    PlayerMovement player;
    Rigidbody2D playerRb;
    SpriteRenderer playerSprite;
    Color originalColor;
    float jumpCap;        // Max upward speed for the current jump
    bool jumpCapSet;      // Has the cap been set for the current jump?
    bool restarting;      // Prevents LoadScene being called more than once

    void Awake()
    {
        sandCollider = GetComponent<Collider2D>();
    }

    // Player ENTERS the quicksand
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        player = other.GetComponent<PlayerMovement>();
        if (player == null) return;

        playerRb = other.attachedRigidbody;
        playerSprite = other.GetComponentInChildren<SpriteRenderer>();
        if (playerSprite != null) originalColor = playerSprite.color;

        jumpCapSet = false;
    }

    // Player STAYS in the quicksand (runs every physics step, after PlayerMovement's FixedUpdate)
    void OnTriggerStay2D(Collider2D other)
    {
        if (player == null || restarting || !other.CompareTag("Player")) return;

        Vector2 vel = playerRb.linearVelocity;

        // --- Allow jumping out ---
        // With no floor under the pit, grounded would be false and the player couldn't jump.
        player.grounded = true;

        // --- Slow horizontal movement ---
        // wantedSpeed = moveSpeed * direction, so this caps speed at moveSpeed * multiplier
        float maxX = Mathf.Abs(player.wantedSpeed) * speedMultiplier;
        vel.x = Mathf.Clamp(vel.x, -maxX, maxX);

        if (vel.y > 0f)
        {
            // Player is jumping: remember this jump's starting speed and cap it
            if (player.jumping && !jumpCapSet)
            {
                jumpCap = vel.y * jumpMultiplier;
                jumpCapSet = true;
            }
            if (jumpCapSet) vel.y = Mathf.Min(vel.y, jumpCap);
        }
        else
        {
            // Not rising: slowly pull the player down
            jumpCapSet = false;
            if (!player.jumping) vel.y = -sinkSpeed;
        }

        playerRb.linearVelocity = vel;

        // --- Depth: how far the player's feet are below the quicksand surface ---
        float surfaceY = sandCollider.bounds.max.y;
        float depth = surfaceY - other.bounds.min.y; // <= 0 means feet are at/above the surface

        // --- Visual warning: tint gets stronger as the player nears deathDepth ---
        if (playerSprite != null)
        {
            float t = Mathf.InverseLerp(deathDepth * warningStart, deathDepth, depth);
            playerSprite.color = Color.Lerp(originalColor, warningColor, t);
        }

        // --- Sunk too deep: restart the level ---
        if (depth >= deathDepth)
        {
            restarting = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    // Player EXITS the quicksand
    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || player == null) return;

        // Nothing to restore for speed/jump: PlayerMovement runs normally again.
        jumpCapSet = false;
        if (playerSprite != null) playerSprite.color = originalColor;
    }
}