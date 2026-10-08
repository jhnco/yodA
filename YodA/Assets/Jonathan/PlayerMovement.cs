using System.Collections;
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

    [Header("Cloud Triangles")]
    public Color triangleColor = new Color(0.6f, 0.85f, 1f, 1f);   // light blue
    [Tooltip("World size of each triangle.")]
    public float triangleSize = 0.3f;
    [Tooltip("Seconds between spawns while touching a cloud.")]
    public float spawnInterval = 0.08f;
    [Tooltip("How long each triangle lives (seconds).")]
    public float triangleLifetime = 0.6f;
    [Tooltip("How far each triangle drifts downward over its lifetime.")]
    public float triangleFallDistance = 0.8f;
    [Tooltip("Left/right wiggle distance.")]
    public float wiggleAmount = 0.08f;
    [Tooltip("Wiggle speed. Higher = faster shake.")]
    public float wiggleSpeed = 40f;
    [Tooltip("Random horizontal spread around the player's center.")]
    public float spawnSpread = 0.3f;

    private Rigidbody2D rb;
    private Collider2D col;
    private bool isGrounded;
    private float coyoteTimer;

    // Cloud detection (uses only the main collider, so the Leaf Collider child is ignored)
    private bool touchingCloud;
    private ContactFilter2D cloudFilter;
    private readonly Collider2D[] overlapResults = new Collider2D[16];

    private Coroutine triangleRoutine;
    private static Sprite triangleSprite;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        cloudFilter = new ContactFilter2D();
        cloudFilter.NoFilter();
        cloudFilter.useTriggers = true; // clouds are triggers, so include them
    }

    void Update()
    {
        CheckGrounded();
        CheckCloudContact();

        // Walking
        float moveInput = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // Coyote time
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
            coyoteTimer = 0f;
        }
    }

    Vector2 GetFeetPosition()
    {
        if (groundCheckPoint != null)
            return groundCheckPoint.position;
        if (col != null)
            return new Vector2(col.bounds.center.x, col.bounds.min.y);
        return transform.position;
    }

    void CheckGrounded()
    {
        Vector2 origin = GetFeetPosition();

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer | cloudLayer);
        isGrounded = hit.collider != null;

        // Debug.DrawRay(origin, Vector2.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
    }

    // ---------------- Cloud detection ----------------

    void CheckCloudContact()
    {
        // Only the player's main collider is tested, so the Leaf Collider child is ignored.
        int count = col.Overlap(cloudFilter, overlapResults);

        bool nowTouching = false;
        Collider2D cloudCollider = null;

        for (int i = 0; i < count; i++)
        {
            if (overlapResults[i].CompareTag("Cloud"))
            {
                nowTouching = true;
                cloudCollider = overlapResults[i];
                break;
            }
        }

        if (nowTouching && !touchingCloud)
            OnCloudEnter(cloudCollider);
        else if (!nowTouching && touchingCloud)
            OnCloudExit();

        touchingCloud = nowTouching;
    }

    void OnCloudEnter(Collider2D cloud)
    {
        CancelInvoke("ChangeToDefaultGravity");
        Physics2D.gravity = new Vector2(0f, -3f);

        if (triangleRoutine == null)
        {
            SpriteRenderer cloudSR = cloud.GetComponentInChildren<SpriteRenderer>();
            Material cloudMat = cloudSR != null ? cloudSR.sharedMaterial : null;
            int layerID = cloudSR != null ? cloudSR.sortingLayerID : 0;
            int order = cloudSR != null ? cloudSR.sortingOrder : 0;

            triangleRoutine = StartCoroutine(SpawnTriangles(cloudMat, layerID, order));
        }
    }

    void OnCloudExit()
    {
        Invoke("ChangeToDefaultGravity", 0.2f);

        if (triangleRoutine != null)
        {
            StopCoroutine(triangleRoutine);
            triangleRoutine = null;
        }
    }

    void ChangeToDefaultGravity()
    {
        Physics2D.gravity = new Vector2(0f, -9.81f);
    }

    // ---------------- Cloud triangles ----------------

    IEnumerator SpawnTriangles(Material mat, int sortingLayerID, int sortingOrder)
    {
        while (true)
        {
            SpawnTriangle(mat, sortingLayerID, sortingOrder);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnTriangle(Material mat, int sortingLayerID, int sortingOrder)
    {
        Vector2 feet = GetFeetPosition();
        Vector2 spawnPos = new Vector2(
            feet.x + Random.Range(-spawnSpread, spawnSpread),
            feet.y - 0.1f
        );

        GameObject go = new GameObject("CloudTriangle");
        go.transform.position = spawnPos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetTriangleSprite();
        if (mat != null) sr.sharedMaterial = mat;   // same material as the cloud
        sr.color = triangleColor;
        sr.sortingLayerID = sortingLayerID;
        sr.sortingOrder = sortingOrder + 1;

        // Sprite is 1 unit wide at scale 1, so scale it to the desired size
        go.transform.localScale = Vector3.one * triangleSize;

        // Run on a separate host-independent coroutine owned by this script
        StartCoroutine(AnimateTriangle(go, sr, spawnPos));
    }

    IEnumerator AnimateTriangle(GameObject go, SpriteRenderer sr, Vector2 startPos)
    {
        float phase = Random.Range(0f, Mathf.PI * 2f);
        float t = 0f;

        while (t < triangleLifetime && go != null)
        {
            float normalized = t / triangleLifetime;

            float wiggleX = Mathf.Sin(t * wiggleSpeed + phase) * wiggleAmount;
            float fallY = -triangleFallDistance * normalized;

            go.transform.position = new Vector3(startPos.x + wiggleX, startPos.y + fallY, 0f);

            Color c = triangleColor;
            c.a = Mathf.Lerp(triangleColor.a, 0f, normalized);
            sr.color = c;

            t += Time.deltaTime;
            yield return null;
        }

        if (go != null) Destroy(go);
    }

    // Builds a downward-pointing triangle sprite (apex at the bottom, like an "arrow down")
    static Sprite GetTriangleSprite()
    {
        if (triangleSprite != null) return triangleSprite;

        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color clear = new Color(1f, 1f, 1f, 0f);
        float center = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            // y = 0 is the bottom row (apex), y = size-1 is the top row (widest)
            float halfWidth = (y / (float)(size - 1)) * (size * 0.5f);

            for (int x = 0; x < size; x++)
            {
                bool inside = Mathf.Abs(x - center) <= halfWidth;
                tex.SetPixel(x, y, inside ? Color.white : clear);
            }
        }

        tex.Apply();

        // pixelsPerUnit = size, so the sprite is exactly 1 unit wide at scale 1
        triangleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return triangleSprite;
    }
}