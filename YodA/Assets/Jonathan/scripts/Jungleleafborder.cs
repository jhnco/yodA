using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drop this on any 2D object. It grows jungle leaves out of the object's sides.
/// Leaves are built from simple procedural shapes (a pointed ellipse + a midrib line),
/// so no art assets are needed.
///
/// When an object tagged "Player" (with a trigger collider) touches a leaf, that leaf
/// sways faster and fades to a custom color. It calms down again after the player leaves.
///
/// Requires LeafTouchRelay.cs in the project too.
/// Right-click the component header > "Regenerate Leaves" to rebuild in the editor.
/// </summary>
[DisallowMultipleComponent]
public class JungleLeafBorder : MonoBehaviour
{
    [Header("Sides")]
    public bool top = true;
    public bool bottom = true;
    public bool left = true;
    public bool right = true;

    [Header("Leaves")]
    [Tooltip("Leaves per world unit of edge length")]
    public float density = 4f;
    public Vector2 leafLength = new Vector2(0.35f, 0.7f);
    [Tooltip("Width as a fraction of length")]
    public Vector2 leafWidthRatio = new Vector2(0.35f, 0.55f);
    [Tooltip("Random tilt away from straight-out, in degrees")]
    public float angleSpread = 40f;
    [Tooltip("Push leaves slightly inside the edge so they look attached")]
    public float inset = 0.05f;
    [Tooltip("Where along each side leaves may spawn, from 0 (start) to 1 (end). Example: 0.15 to 0.85 keeps the corners clear.")]
    public Vector2 spawnRange = new Vector2(0.15f, 0.85f);

    [Header("Material")]
    [Tooltip("Optional. Any 2D material (e.g. Sprite-Lit-Default for URP 2D lights, or your own shader graph). Leave empty for the default sprite material.")]
    public Material leafMaterial;

    [Header("Colors")]
    public Color darkGreen = new Color(0.05f, 0.35f, 0.12f);
    public Color lightGreen = new Color(0.35f, 0.75f, 0.2f);
    [Range(0f, 1f)] public float veinDarkness = 0.45f;

    [Header("Sorting")]
    public bool drawBehindObject = false;
    public int sortingOffset = 1;

    [Header("Sway (idle)")]
    public bool sway = true;
    public float swayAngle = 6f;
    public float swaySpeed = 1.5f;

    [Header("Player Touch Reaction")]
    public bool reactToPlayer = true;
    [Tooltip("Tag of the object whose trigger collider should excite the leaves")]
    public string playerTag = "Player";
    [Tooltip("Sway speed while touched")]
    public float touchedSwaySpeed = 8f;
    [Tooltip("Sway angle (degrees) while touched")]
    public float touchedSwayAngle = 18f;
    [Tooltip("Color the leaves fade to while touched")]
    public Color touchedColor = new Color(1f, 0.3f, 0.5f);
    [Tooltip("How fast leaves ramp into / out of the excited state (higher = snappier)")]
    public float reactSpeed = 6f;
    [Tooltip("Seconds the leaf stays excited after the player stops touching it")]
    public float calmDownTime = 0.6f;
    [Tooltip("Adds a kinematic Rigidbody2D to each leaf so trigger events always fire, even if the player has no Rigidbody2D")]
    public bool addKinematicRigidbody = true;

    [Header("Random")]
    public int seed = 12345;

    const string ContainerName = "__JungleLeaves";

    class Leaf
    {
        public Transform t;
        public SpriteRenderer sr;
        public Color baseColor;
        public float baseAngle;
        public float phase;
        public float speed;

        public int touchCount;
        public float calmTimer;
        public float excite; // 0 = idle, 1 = fully excited
    }

    readonly List<Leaf> leaves = new List<Leaf>();
    static Sprite cachedSprite;

    bool dirty;
#if UNITY_EDITOR
    bool editRebuildQueued;
#endif

    void Start()
    {
        dirty = false;
        Generate();
    }

    // Runs whenever a value changes in the Inspector (on each instance separately)
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            dirty = true; // rebuild at the start of the next Update
            return;
        }

#if UNITY_EDITOR
        // Can't create/destroy objects inside OnValidate in edit mode, so defer it
        if (editRebuildQueued) return;
        editRebuildQueued = true;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            editRebuildQueued = false;
            if (this == null) return;
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this)) return; // never build inside the asset itself
            Generate();
        };
#endif
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        if (dirty)
        {
            dirty = false;
            Generate();
        }

        float dt = Time.deltaTime;

        for (int i = 0; i < leaves.Count; i++)
        {
            Leaf l = leaves[i];
            if (l.t == null) continue;

            // Excitement state
            bool touched = l.touchCount > 0;
            if (touched) l.calmTimer = calmDownTime;
            else l.calmTimer -= dt;

            float target = (touched || l.calmTimer > 0f) ? 1f : 0f;
            l.excite = Mathf.MoveTowards(l.excite, target, dt * reactSpeed);

            // Color
            if (l.sr != null)
                l.sr.color = Color.Lerp(l.baseColor, touchedColor, l.excite);

            // Sway (phase is accumulated so changing speed never causes a jump)
            if (sway)
            {
                float speed = Mathf.Lerp(swaySpeed, touchedSwaySpeed, l.excite);
                float amp = Mathf.Lerp(swayAngle, touchedSwayAngle, l.excite);
                l.phase += dt * speed * l.speed;
                float a = l.baseAngle + Mathf.Sin(l.phase) * amp;
                l.t.localRotation = Quaternion.Euler(0f, 0f, a);
            }
        }
    }

    /// <summary>Called by LeafTouchRelay when something enters/exits a leaf's trigger.</summary>
    public void NotifyTouch(int index, Collider2D other, bool entered)
    {
        if (!reactToPlayer) return;
        if (index < 0 || index >= leaves.Count) return;
        if (!IsPlayer(other)) return;

        Leaf l = leaves[index];
        l.touchCount = Mathf.Max(0, l.touchCount + (entered ? 1 : -1));
    }

    bool IsPlayer(Collider2D c)
    {
        if (c.CompareTag(playerTag)) return true;
        // Collider may be on a child while the tag is on the root / rigidbody object
        return c.attachedRigidbody != null && c.attachedRigidbody.CompareTag(playerTag);
    }

    [ContextMenu("Regenerate Leaves")]
    public void Generate()
    {
        Clear();
        leaves.Clear();

        Rect r = GetLocalRect();
        Vector3 ls = transform.lossyScale;
        float sx = Mathf.Approximately(ls.x, 0f) ? 1f : Mathf.Abs(ls.x);
        float sy = Mathf.Approximately(ls.y, 0f) ? 1f : Mathf.Abs(ls.y);

        Transform container = new GameObject(ContainerName).transform;
        container.SetParent(transform, false);
        // Cancel the object's (possibly non-uniform) scale so leaves never get stretched or skewed
        container.localScale = new Vector3(1f / sx, 1f / sy, 1f);

        Random.State oldState = Random.state;
        Random.InitState(seed);

        Sprite leafSprite = GetLeafSprite();

        SpriteRenderer parentSr = GetComponent<SpriteRenderer>();
        int baseOrder = parentSr != null ? parentSr.sortingOrder : 0;
        int layerId = parentSr != null ? parentSr.sortingLayerID : 0;
        int order = drawBehindObject ? baseOrder - sortingOffset : baseOrder + sortingOffset;

        // Each side: start point, end point (local space), outward angle (0 = up, +CCW)
        if (top) BuildSide(container, leafSprite, new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax), 0f, sx, sy, order, layerId);
        if (bottom) BuildSide(container, leafSprite, new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), 180f, sx, sy, order, layerId);
        if (left) BuildSide(container, leafSprite, new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax), 90f, sx, sy, order, layerId);
        if (right) BuildSide(container, leafSprite, new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), -90f, sx, sy, order, layerId);

        Random.state = oldState;
    }

    void BuildSide(Transform parent, Sprite sprite, Vector2 a, Vector2 b, float outAngle,
                   float sx, float sy, int order, int layerId)
    {
        // Convert the edge into world-sized units (the container cancels the object's scale)
        a = new Vector2(a.x * sx, a.y * sy);
        b = new Vector2(b.x * sx, b.y * sy);

        // Only use the chosen portion of the side (keeps leaves off the corners)
        float rMin = Mathf.Clamp01(Mathf.Min(spawnRange.x, spawnRange.y));
        float rMax = Mathf.Clamp01(Mathf.Max(spawnRange.x, spawnRange.y));
        Vector2 start = Vector2.Lerp(a, b, rMin);
        Vector2 end = Vector2.Lerp(a, b, rMax);
        a = start;
        b = end;

        float edgeLength = Vector2.Distance(a, b);
        int count = Mathf.Max(1, Mathf.RoundToInt(edgeLength * density));

        // Direction pointing outward, used for the inset
        Vector2 outDir = Quaternion.Euler(0, 0, outAngle) * Vector2.up;

        for (int i = 0; i < count; i++)
        {
            float t = (i + Random.Range(0.1f, 0.9f)) / count;
            Vector2 p = Vector2.Lerp(a, b, t);
            p -= outDir * inset;

            float angle = outAngle + Random.Range(-angleSpread, angleSpread);
            float length = Random.Range(leafLength.x, leafLength.y);
            float width = length * Random.Range(leafWidthRatio.x, leafWidthRatio.y);

            GameObject go = new GameObject("Leaf");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(p.x, p.y, 0f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            // Sprite is 0.5 units wide and 1 unit tall at scale 1
            go.transform.localScale = new Vector3(width * 2f, length, 1f);

            Color baseColor = Color.Lerp(darkGreen, lightGreen, Random.value);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = baseColor;
            sr.sortingLayerID = layerId;
            sr.sortingOrder = order + Random.Range(0, 3);
            if (leafMaterial != null) sr.sharedMaterial = leafMaterial;

            int index = leaves.Count;

            if (reactToPlayer)
            {
                // Trigger collider matching the leaf shape (sprite is 0.5 x 1, pivot at the base)
                BoxCollider2D col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(0.4f, 0.9f);
                col.offset = new Vector2(0f, 0.5f);

                if (addKinematicRigidbody)
                {
                    Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
                    rb.bodyType = RigidbodyType2D.Kinematic;
                }

                LeafTouchRelay relay = go.AddComponent<LeafTouchRelay>();
                relay.owner = this;
                relay.index = index;
            }

            leaves.Add(new Leaf
            {
                t = go.transform,
                sr = sr,
                baseColor = baseColor,
                baseAngle = angle,
                phase = Random.Range(0f, Mathf.PI * 2f),
                speed = Random.Range(0.7f, 1.3f)
            });
        }
    }

    Rect GetLocalRect()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            Bounds b = sr.sprite.bounds;
            return new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            // Convert world bounds into local space (ignores rotation, good enough for borders)
            Bounds wb = col.bounds;
            Vector3 min = transform.InverseTransformPoint(wb.min);
            Vector3 max = transform.InverseTransformPoint(wb.max);
            return Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y),
                                   Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
        }

        return new Rect(-0.5f, -0.5f, 1f, 1f);
    }

    void Clear()
    {
        Transform old;
        while ((old = transform.Find(ContainerName)) != null)
        {
            // Rename first: Destroy is deferred in play mode, so Find would keep returning it
            old.name = ContainerName + "_old";
            if (Application.isPlaying) Destroy(old.gameObject);
            else DestroyImmediate(old.gameObject);
        }
    }

    /// <summary>
    /// Builds a leaf shape from simple geometry: a pointed ellipse with a darker midrib.
    /// Pivot is at the bottom-center so the leaf grows from the edge.
    /// </summary>
    Sprite GetLeafSprite()
    {
        if (cachedSprite != null) return cachedSprite;

        const int w = 64;
        const int h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float v = (y + 0.5f) / h;                       // 0 base -> 1 tip
            // Pointed at both ends, widest slightly toward the base
            float halfWidth = 0.5f * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(v, 0.8f)), 0.85f);

            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w - 0.5f;            // -0.5 .. 0.5
                float d = Mathf.Abs(u);

                // Anti-aliased edge
                float alpha = Mathf.Clamp01((halfWidth - d) * w * 0.6f);
                if (alpha <= 0f) { pixels[y * w + x] = Color.clear; continue; }

                float c = 1f;
                // Midrib (a thin line down the center)
                if (d < 0.03f) c = 1f - veinDarkness;

                // Side veins (diagonal lines)
                float vein = Mathf.Abs(Mathf.Repeat(v * 6f - d * 3.5f, 1f) - 0.5f);
                if (vein < 0.035f && d > 0.03f) c = Mathf.Min(c, 1f - veinDarkness * 0.45f);

                // Slight shading toward the edges
                c *= Mathf.Lerp(1f, 0.85f, d / Mathf.Max(halfWidth, 0.001f));

                pixels[y * w + x] = new Color(c, c, c, alpha); // white-ish, tinted by SpriteRenderer.color
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        // Pixels-per-unit = height -> sprite is 1 unit tall; pivot at base center
        cachedSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), h);
        return cachedSprite;
    }
}