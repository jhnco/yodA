using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    float accel = 0.1f;

    [SerializeField] float tapJumpSpeed;
    [SerializeField] float minJumpTime;
    [SerializeField] float checkGroundRayLength;
    [SerializeField] LayerMask Floor;

    [Tooltip("Layer(s) of the clouds. Also counts as ground.")]
    [SerializeField] LayerMask cloudLayer;

    [SerializeField] float moveSpeed;
    [SerializeField] float maxSpeed;
    public float wantedSpeed;
    int directon;
    public float movement;

    [Header("Walking / Facing")]
    public bool currentlyWalking = false;
    public bool facingRight = true;

    [Tooltip("SpriteRenderer to flip. Assign in Inspector or find automatically.")]
    [SerializeField] SpriteRenderer spriteRenderer;

    [SerializeField] float jumpSpeed;
    [SerializeField] float jumpDuration;
    [SerializeField] float maxJump;
    public bool jumping;
    public bool grounded;
    float jumpingStartTime;
    float jumpVel;

    float lastTimeGrounded;
    bool wasGrounded;

    [SerializeField] float delayedJumpTimeAllowance;

    public bool allowedMovement = true;

    Vector2 groundBoxSize = new Vector2(0.4f, 0.5f);

    [Header("Cloud Triangles")]
    public Color triangleColor = new Color(0.6f, 0.85f, 1f, 1f);
    public float triangleSize = 0.3f;
    public float spawnInterval = 0.08f;
    public float triangleLifetime = 0.6f;
    public float triangleFallDistance = 0.8f;
    public float wiggleAmount = 0.08f;
    public float wiggleSpeed = 40f;
    public float spawnSpread = 0.3f;

    Collider2D col;
    bool touchingCloud;
    ContactFilter2D cloudFilter;
    readonly Collider2D[] overlapResults = new Collider2D[16];

    Coroutine triangleRoutine;
    static Sprite triangleSprite;

    void Start()
    {
        col = GetComponent<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Set the initial facing direction without rotating the player.
        if (spriteRenderer != null)
            spriteRenderer.flipX = !facingRight;

        cloudFilter = new ContactFilter2D();
        cloudFilter.NoFilter();
        cloudFilter.useTriggers = true;
    }

    void Update()
    {
        CheckCloudContact();
        CheckDirection();

        if (allowedMovement)
            Jump();
    }

    void FixedUpdate()
    {
        checkColl();

        if (jumping)
            HandleJump();

        if (allowedMovement)
            movePlayer(directon);
    }

    void Jump()
    {
        if ((Input.GetKeyDown(KeyCode.Space) && grounded) ||
            (Input.GetKeyDown(KeyCode.Space) &&
            Time.timeSinceLevelLoad - lastTimeGrounded < delayedJumpTimeAllowance))
        {
            jumping = true;
            jumpingStartTime = Time.timeSinceLevelLoad;

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                tapJumpSpeed
            );
        }
    }

    void CheckDirection()
    {
        if (Input.GetKey(KeyCode.A))
        {
            directon = -1;
            facingRight = false;
            currentlyWalking = true;

            // Flip only the sprite horizontally.
            if (spriteRenderer != null)
                spriteRenderer.flipX = true;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            directon = 1;
            facingRight = true;
            currentlyWalking = true;

            // Restore the sprite's original horizontal direction.
            if (spriteRenderer != null)
                spriteRenderer.flipX = false;
        }
        else
        {
            directon = 0;
            currentlyWalking = false;
        }
    }

    void movePlayer(int direction)
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        wantedSpeed = moveSpeed * direction;

        movement = Mathf.Lerp(
            rb.linearVelocity.x,
            wantedSpeed,
            0.1f
        );

        if (Mathf.Abs(movement) < 0.1f &&
            !Input.GetKey(KeyCode.D) &&
            !Input.GetKey(KeyCode.A))
        {
            movement = 0;
        }

        rb.linearVelocity = new Vector2(
            movement,
            rb.linearVelocity.y
        );
    }

    void HandleJump()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (Time.timeSinceLevelLoad - jumpingStartTime < jumpDuration)
        {
            jumpVel = Mathf.Lerp(
                rb.linearVelocity.y,
                jumpSpeed,
                accel
            );

            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpVel
            );

            if (Time.timeSinceLevelLoad - jumpingStartTime >= minJumpTime)
            {
                if (!Input.GetKey(KeyCode.Space))
                    jumping = false;
            }
        }
        else
        {
            jumping = false;
        }
    }

    void checkColl()
    {
        RaycastHit2D boxDown = Physics2D.BoxCast(
            transform.position,
            groundBoxSize,
            0f,
            Vector2.down,
            checkGroundRayLength,
            Floor | cloudLayer
        );

        if (boxDown.collider != null || touchingCloud)
        {
            grounded = true;
            wasGrounded = true;
        }
        else
        {
            grounded = false;
        }

        if (!grounded && wasGrounded)
        {
            lastTimeGrounded = Time.timeSinceLevelLoad;
            wasGrounded = false;
        }
    }

    // ---------------- Cloud detection ----------------

    Vector2 GetFeetPosition()
    {
        if (col != null)
            return new Vector2(col.bounds.center.x, col.bounds.min.y);

        return transform.position;
    }

    void CheckCloudContact()
    {
        if (col == null)
            return;

        int count = col.Overlap(cloudFilter, overlapResults);

        bool nowTouching = false;
        Collider2D cloudCollider = null;

        for (int i = 0; i < count; i++)
        {
            if (overlapResults[i] != null &&
                overlapResults[i].CompareTag("Cloud"))
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
        CancelInvoke(nameof(ChangeToDefaultGravity));
        Physics2D.gravity = new Vector2(0f, -3f);

        if (triangleRoutine == null)
        {
            SpriteRenderer cloudSR =
                cloud.GetComponentInChildren<SpriteRenderer>();

            Material cloudMat =
                cloudSR != null ? cloudSR.sharedMaterial : null;

            int layerID =
                cloudSR != null ? cloudSR.sortingLayerID : 0;

            int order =
                cloudSR != null ? cloudSR.sortingOrder : 0;

            triangleRoutine = StartCoroutine(
                SpawnTriangles(cloudMat, layerID, order)
            );
        }
    }

    void OnCloudExit()
    {
        Invoke(nameof(ChangeToDefaultGravity), 0.2f);

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

    IEnumerator SpawnTriangles(
        Material mat,
        int sortingLayerID,
        int sortingOrder)
    {
        while (true)
        {
            SpawnTriangle(mat, sortingLayerID, sortingOrder);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnTriangle(
        Material mat,
        int sortingLayerID,
        int sortingOrder)
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

        if (mat != null)
            sr.sharedMaterial = mat;

        sr.color = triangleColor;
        sr.sortingLayerID = sortingLayerID;
        sr.sortingOrder = sortingOrder + 1;

        go.transform.localScale = Vector3.one * triangleSize;

        StartCoroutine(AnimateTriangle(go, sr, spawnPos));
    }

    IEnumerator AnimateTriangle(
        GameObject go,
        SpriteRenderer sr,
        Vector2 startPos)
    {
        float phase = Random.Range(0f, Mathf.PI * 2f);
        float t = 0f;

        while (t < triangleLifetime && go != null)
        {
            float normalized = t / triangleLifetime;

            float wiggleX =
                Mathf.Sin(t * wiggleSpeed + phase) * wiggleAmount;

            float fallY = -triangleFallDistance * normalized;

            go.transform.position = new Vector3(
                startPos.x + wiggleX,
                startPos.y + fallY,
                0f
            );

            Color c = triangleColor;
            c.a = Mathf.Lerp(triangleColor.a, 0f, normalized);
            sr.color = c;

            t += Time.deltaTime;
            yield return null;
        }

        if (go != null)
            Destroy(go);
    }

    static Sprite GetTriangleSprite()
    {
        if (triangleSprite != null)
            return triangleSprite;

        const int size = 64;

        Texture2D tex = new Texture2D(
            size,
            size,
            TextureFormat.RGBA32,
            false
        );

        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color clear = new Color(1f, 1f, 1f, 0f);
        float center = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            float halfWidth =
                (y / (float)(size - 1)) * (size * 0.5f);

            for (int x = 0; x < size; x++)
            {
                bool inside = Mathf.Abs(x - center) <= halfWidth;

                tex.SetPixel(
                    x,
                    y,
                    inside ? Color.white : clear
                );
            }
        }

        tex.Apply();

        triangleSprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size
        );

        return triangleSprite;
    }

    void OnDrawGizmos()
    {
        Vector3 start = transform.position;

        Vector3 end =
            start + Vector3.down * checkGroundRayLength;

        Vector3 size = new Vector3(
            groundBoxSize.x,
            groundBoxSize.y,
            0f
        );

        Gizmos.color = grounded ? Color.green : Color.red;

        Gizmos.DrawWireCube(start, size);
        Gizmos.DrawWireCube(end, size);
        Gizmos.DrawLine(start, end);
    }
}