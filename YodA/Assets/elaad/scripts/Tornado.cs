using UnityEngine;

// Lifts the player upward while Active, then vanishes for a while (Inactive), on repeat.
// The tornado draws ITSELF in code (a funnel plus swirling streaks of air), so no art is needed.
// Works with the existing PlayerMovement script WITHOUT modifying it:
// PlayerMovement only controls horizontal velocity while walking, so left/right steering
// keeps working while the tornado handles the vertical movement.
//
// The funnel is sized from the BoxCollider2D, so resize the COLLIDER (keep the object's Scale at 1).
[RequireComponent(typeof(BoxCollider2D))]
public class Tornado : MonoBehaviour
{
    [Header("Cycle")]
    [SerializeField] float activeDuration = 3f;    // Seconds the tornado is on
    [SerializeField] float inactiveDuration = 2f;  // Seconds the tornado is off
    [SerializeField] bool startActive = true;      // Which state the cycle begins in

    [Header("Disappear Warning (last part of Active)")]
    [SerializeField] float warningTime = 0.5f;     // How long before vanishing the warning plays
    [SerializeField] float flashSpeed = 8f;        // Flashes per second during the warning
    [SerializeField] float flashMinAlpha = 0.25f;  // Lowest opacity during a flash
    [Range(0.1f, 1f)]
    [SerializeField] float shrinkScale = 0.85f;    // Tornado shrinks (toward its base) to this fraction

    [Header("Lift")]
    [SerializeField] float liftSpeed = 6f;         // Max upward speed of the player
    [SerializeField] float liftAcceleration = 20f; // How quickly the player speeds up to liftSpeed

    [Header("Look (generated in code, no art needed)")]
    [SerializeField] Color tornadoColor = new Color(0.62f, 0.75f, 0.9f, 1f);
    [SerializeField] int streakCount = 40;         // Number of swirling air streaks
    [Range(0.05f, 1f)]
    [SerializeField] float bottomWidth = 0.2f;     // Funnel width at the tip, as a fraction of the top width
    [SerializeField] float spinSpeed = 1.2f;       // Rotations per second (faster near the tip)
    [SerializeField] float riseSpeed = 0.5f;       // How fast the air drifts upward (funnel heights per second)
    [Range(0.05f, 1f)]
    [SerializeField] float streakLength = 0.45f;   // Streak length as a fraction of the funnel width
    [SerializeField] float streakThickness = 0.08f;// Streak thickness in world units
    [Range(0f, 0.5f)]
    [SerializeField] float bodyAlpha = 0.15f;      // Opacity of the faint funnel shape behind the streaks

    [Header("Draw Order")]
    [SerializeField] int backSortingOrder = 0;     // Sorting order of streaks passing BEHIND the funnel
    [SerializeField] int frontSortingOrder = 10;   // Sorting order of streaks passing in FRONT (player sits between if their order is in between)

    // One swirling streak of air
    class Streak
    {
        public Transform t;
        public SpriteRenderer sr;
        public float height;        // 0 = bottom of funnel, 1 = top
        public float angle;         // Position around the funnel (radians)
        public float radiusFactor;  // How far out from the center (0..1)
        public float speedFactor;   // Individual speed variation
        public float lengthFactor;  // Individual length variation
    }

    // Tornado state
    BoxCollider2D triggerCollider;
    bool isActive;
    float stateTimer;

    // Visual
    Transform visualRoot;
    SpriteRenderer bodyRenderer;
    Streak[] streaks;
    float funnelWidth;
    float funnelHeight;

    static Sprite softSprite;   // Soft round sprite used for streaks (squashed into ovals)
    static Sprite funnelSprite; // Funnel-shaped body

    // Player state
    Rigidbody2D playerRb;
    float originalGravityScale;
    bool lifting;

    void Awake()
    {
        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;

        BuildVisual();
        SetTornadoActive(startActive);
    }

    // ---------------- Active / Inactive cycle ----------------

    void Update()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
            SetTornadoActive(!isActive); // Time's up: switch to the other state

        if (!isActive) return;

        // Normal look by default...
        float alpha = 1f;
        float scale = 1f;

        // ...but flash and shrink during the last warningTime seconds
        if (stateTimer <= warningTime)
        {
            // 0 at the start of the warning, 1 when the tornado disappears
            float progress = 1f - Mathf.Clamp01(stateTimer / Mathf.Max(warningTime, 0.01f));

            float flash = Mathf.PingPong(Time.time * flashSpeed * 2f, 1f);
            alpha = Mathf.Lerp(flashMinAlpha, 1f, flash);
            scale = Mathf.Lerp(1f, shrinkScale, progress);
        }

        visualRoot.localScale = Vector3.one * scale;
        AnimateVisual(alpha);
    }

    void SetTornadoActive(bool active)
    {
        isActive = active;
        stateTimer = active ? activeDuration : inactiveDuration;

        // Trigger and visuals on only while Active
        triggerCollider.enabled = active;
        visualRoot.gameObject.SetActive(active);
        visualRoot.localScale = Vector3.one;

        // If the player was being lifted, put gravity back right away
        if (!active) StopLift();
    }

    // ---------------- Lift physics ----------------

    // Runs every physics step while the player is inside (only fires while the collider is on)
    void OnTriggerStay2D(Collider2D other)
    {
        if (!isActive || !other.CompareTag("Player")) return;

        // First step inside: turn off gravity for the player
        if (!lifting)
        {
            playerRb = other.attachedRigidbody;
            if (playerRb == null) return;

            originalGravityScale = playerRb.gravityScale;
            playerRb.gravityScale = 0f;
            lifting = true;
        }

        // Smoothly speed up toward liftSpeed. Horizontal velocity is left alone,
        // so the player can still steer with A/D.
        Vector2 vel = playerRb.linearVelocity;
        vel.y = Mathf.MoveTowards(vel.y, liftSpeed, liftAcceleration * Time.fixedDeltaTime);
        playerRb.linearVelocity = vel;
    }

    // Player left the tornado
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) StopLift();
    }

    // Restore the player's normal gravity
    void StopLift()
    {
        if (lifting && playerRb != null)
            playerRb.gravityScale = originalGravityScale;

        lifting = false;
    }

    // Safety: never leave the player floating if this object is disabled or destroyed
    void OnDisable()
    {
        StopLift();
    }

    // ---------------- Generated visual ----------------

    void BuildVisual()
    {
        // Size the funnel to the collider
        funnelWidth = triggerCollider.size.x;
        funnelHeight = triggerCollider.size.y;
        Vector2 bottomCenter = triggerCollider.offset - new Vector2(0f, funnelHeight * 0.5f);

        // Root sits at the bottom-center of the collider, so shrinking pulls toward the ground
        visualRoot = new GameObject("TornadoVisual").transform;
        visualRoot.SetParent(transform, false);
        visualRoot.localPosition = bottomCenter;

        // Faint funnel shape behind the streaks
        GameObject body = new GameObject("FunnelBody");
        body.transform.SetParent(visualRoot, false);
        body.transform.localPosition = new Vector3(0f, funnelHeight * 0.5f, 0f);
        body.transform.localScale = new Vector3(funnelWidth, funnelHeight, 1f);
        bodyRenderer = body.AddComponent<SpriteRenderer>();
        bodyRenderer.sprite = GetFunnelSprite();
        bodyRenderer.sortingOrder = backSortingOrder;

        // Swirling streaks of air
        streaks = new Streak[streakCount];
        for (int i = 0; i < streakCount; i++)
        {
            GameObject go = new GameObject("Streak");
            go.transform.SetParent(visualRoot, false);

            Streak s = new Streak();
            s.t = go.transform;
            s.sr = go.AddComponent<SpriteRenderer>();
            s.sr.sprite = GetSoftSprite();
            s.height = Random.value;
            s.angle = Random.Range(0f, Mathf.PI * 2f);
            s.radiusFactor = Random.Range(0.55f, 1f);
            s.speedFactor = Random.Range(0.8f, 1.25f);
            s.lengthFactor = Random.Range(0.6f, 1.2f);
            streaks[i] = s;
        }
    }

    // Moves every streak around the funnel. alphaMul is used by the disappear warning.
    void AnimateVisual(float alphaMul)
    {
        float dt = Time.deltaTime;

        // Faint body
        Color bc = tornadoColor;
        bc.a = bodyAlpha * alphaMul;
        bodyRenderer.color = bc;

        for (int i = 0; i < streaks.Length; i++)
        {
            Streak s = streaks[i];

            // Drift upward, wrapping back to the bottom
            s.height += riseSpeed * s.speedFactor * dt;
            if (s.height > 1f) s.height -= 1f;

            // Spin around the funnel (faster near the tip)
            float spin = spinSpeed * Mathf.Lerp(1.5f, 0.8f, s.height) * s.speedFactor;
            s.angle += spin * Mathf.PI * 2f * dt;

            // The funnel is narrow at the bottom and wide at the top
            float radius = Mathf.Lerp(bottomWidth, 1f, s.height) * funnelWidth * 0.5f * s.radiusFactor;

            float x = Mathf.Cos(s.angle) * radius;
            float depth = Mathf.Sin(s.angle);   // +1 = passing in front, -1 = passing behind
            float y = s.height * funnelHeight;
            s.t.localPosition = new Vector3(x, y, 0f);

            // Streaks look longest when moving across the screen and shortest at the funnel's edges
            float stretch = Mathf.Lerp(0.3f, 1f, Mathf.Abs(depth));
            float length = funnelWidth * streakLength * s.lengthFactor * stretch;
            s.t.localScale = new Vector3(length, streakThickness, 1f);

            // Front streaks are brighter, back streaks dimmer. Fade near the top and bottom.
            float edgeFade = Mathf.Clamp01(Mathf.Min(s.height / 0.1f, (1f - s.height) / 0.15f));
            Color c = tornadoColor;
            c.a = tornadoColor.a * (depth > 0f ? 0.9f : 0.4f) * edgeFade * alphaMul;
            s.sr.color = c;
            s.sr.sortingOrder = depth > 0f ? frontSortingOrder : backSortingOrder;
        }
    }

    // Soft round blob, squashed into an oval streak by scaling
    static Sprite GetSoftSprite()
    {
        if (softSprite != null) return softSprite;

        const int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                float a = Mathf.Clamp01((1f - d) * 2f); // Soft edge
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        softSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return softSprite;
    }

    // Funnel shape: narrow at the bottom, wide at the top, soft edges
    static Sprite GetFunnelSprite()
    {
        if (funnelSprite != null) return funnelSprite;

        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            float v = y / (float)(size - 1);            // 0 = bottom, 1 = top
            float limit = Mathf.Lerp(0.2f, 1f, v);      // Half-width of the funnel at this height
            float vFade = Mathf.Clamp01(v / 0.1f) * Mathf.Clamp01((1f - v) / 0.15f);

            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - center) / center;
                float a = Mathf.Clamp01((limit - dx) / 0.25f) * vFade;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        funnelSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return funnelSprite;
    }
}