using UnityEngine;
using System.Collections.Generic;

public class starSpawner : MonoBehaviour
{
    [Header("Spawning")]
    [Tooltip("Maximum number of stars alive at once.")]
    public int maxStars = 40;
    [Tooltip("Stars spawned per second.")]
    public float spawnRate = 12f;
    [Tooltip("Distance the star floats above the surface to avoid z-fighting.")]
    public float surfaceOffset = 0.01f;

    [Header("Look")]
    public Vector2 sizeRange = new Vector2(0.05f, 0.15f);
    public Color[] colors = { Color.white, new Color(1f, 0.95f, 0.7f), new Color(0.7f, 0.85f, 1f) };
    [Tooltip("Peak brightness multiplier (HDR) - values > 1 look like glow with Bloom.")]
    public float glowIntensity = 3f;
    [Tooltip("Optional star sprite. Leave empty to use a generated soft star.")]
    public Sprite starSprite;

    [Header("Lifetime")]
    public Vector2 lifetimeRange = new Vector2(1.0f, 2.5f);
    [Range(0f, 1f)] public float fadeInPortion = 0.3f;
    [Range(0f, 1f)] public float fadeOutPortion = 0.4f;
    public float twinkleSpeed = 8f;
    [Range(0f, 1f)] public float twinkleAmount = 0.25f;

    class Star
    {
        public Transform t;
        public SpriteRenderer sr;
        public float age, life, baseSize, phase;
        public Color color;
    }

    readonly List<Star> active = new List<Star>();
    readonly Stack<Star> pool = new Stack<Star>();
    Mesh mesh;
    Transform holder;
    float spawnAccum;
    Sprite generatedSprite;

    void Awake()
    {
        var mf = GetComponent<MeshFilter>();
        mesh = mf != null ? mf.sharedMesh : null;

        // Make the object black
        var rend = GetComponent<Renderer>();
        if (rend != null)
        {
            var mat = rend.material;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.black); // URP/HDRP
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.black);         // Built-in
        }

        holder = new GameObject("Stars").transform;
        holder.SetParent(transform, false);

        if (starSprite == null) starSprite = generatedSprite = MakeStarSprite(64);
    }

    void Update()
    {
        // Spawn
        spawnAccum += spawnRate * Time.deltaTime;
        while (spawnAccum >= 1f)
        {
            spawnAccum -= 1f;
            if (active.Count < maxStars) SpawnStar();
        }

        // Update
        for (int i = active.Count - 1; i >= 0; i--)
        {
            var s = active[i];
            s.age += Time.deltaTime;
            float n = s.age / s.life;
            if (n >= 1f)
            {
                s.sr.enabled = false;
                pool.Push(s);
                active.RemoveAt(i);
                continue;
            }

            float alpha;
            if (n < fadeInPortion) alpha = n / Mathf.Max(fadeInPortion, 1e-4f);
            else if (n > 1f - fadeOutPortion) alpha = (1f - n) / Mathf.Max(fadeOutPortion, 1e-4f);
            else alpha = 1f;
            alpha = Mathf.SmoothStep(0f, 1f, alpha);

            float twinkle = 1f - twinkleAmount + twinkleAmount * Mathf.Sin(Time.time * twinkleSpeed + s.phase);
            float glow = Mathf.Lerp(1f, glowIntensity, alpha) * twinkle;

            Color c = s.color * glow;
            c.a = alpha;
            s.sr.color = c;

            float scale = s.baseSize * Mathf.Lerp(0.5f, 1f, alpha);
            s.t.localScale = Vector3.one * scale;
        }
    }

    void SpawnStar()
    {
        if (!TryGetSurfacePoint(out Vector3 localPos, out Vector3 localNormal)) return;

        Star s = pool.Count > 0 ? pool.Pop() : CreateStar();
        s.age = 0f;
        s.life = Random.Range(lifetimeRange.x, lifetimeRange.y);
        s.baseSize = Random.Range(sizeRange.x, sizeRange.y);
        s.phase = Random.value * Mathf.PI * 2f;
        s.color = colors.Length > 0 ? colors[Random.Range(0, colors.Length)] : Color.white;

        s.t.localPosition = localPos + localNormal * surfaceOffset;
        // Quad faces along its -Z for sprites, so look opposite the normal to face outward
        s.t.localRotation = Quaternion.LookRotation(-localNormal) * Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        s.t.localScale = Vector3.zero;
        s.sr.color = new Color(0, 0, 0, 0);
        s.sr.enabled = true;
        active.Add(s);
    }

    Star CreateStar()
    {
        var go = new GameObject("Star");
        go.transform.SetParent(holder, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = starSprite;
        // Additive-looking glow: use the default sprite material with HDR colors.
        return new Star { t = go.transform, sr = sr };
    }

    // Picks a random point on the mesh surface (local space). Falls back to a unit cube if no mesh.
    bool TryGetSurfacePoint(out Vector3 pos, out Vector3 normal)
    {
        if (mesh != null && mesh.vertexCount > 0)
        {
            var tris = mesh.triangles;
            var verts = mesh.vertices;
            int tri = Random.Range(0, tris.Length / 3) * 3;
            Vector3 a = verts[tris[tri]], b = verts[tris[tri + 1]], c = verts[tris[tri + 2]];

            float r1 = Mathf.Sqrt(Random.value), r2 = Random.value;
            pos = (1 - r1) * a + r1 * (1 - r2) * b + r1 * r2 * c;
            normal = Vector3.Cross(b - a, c - a).normalized;
            return true;
        }

        // Fallback: random face of a unit cube
        int face = Random.Range(0, 6);
        float u = Random.Range(-0.5f, 0.5f), v = Random.Range(-0.5f, 0.5f);
        switch (face)
        {
            case 0: pos = new Vector3(0.5f, u, v); normal = Vector3.right; break;
            case 1: pos = new Vector3(-0.5f, u, v); normal = Vector3.left; break;
            case 2: pos = new Vector3(u, 0.5f, v); normal = Vector3.up; break;
            case 3: pos = new Vector3(u, -0.5f, v); normal = Vector3.down; break;
            case 4: pos = new Vector3(u, v, 0.5f); normal = Vector3.forward; break;
            default: pos = new Vector3(u, v, -0.5f); normal = Vector3.back; break;
        }
        return true;
    }

    // Generates a soft 4-point star texture with a bright center, so no assets are needed.
    static Sprite MakeStarSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half, dy = (y - half) / half;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float core = Mathf.Clamp01(1f - d * 2.2f);                 // bright center
                core *= core;
                float rays = Mathf.Clamp01(1f - Mathf.Abs(dx) * 8f) * Mathf.Clamp01(1f - Mathf.Abs(dy)) +
                             Mathf.Clamp01(1f - Mathf.Abs(dy) * 8f) * Mathf.Clamp01(1f - Mathf.Abs(dx));
                float halo = Mathf.Clamp01(1f - d) * 0.25f;
                float a = Mathf.Clamp01(core + rays * 0.8f + halo);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    void OnDestroy()
    {
        if (generatedSprite != null)
        {
            Destroy(generatedSprite.texture);
            Destroy(generatedSprite);
        }
    }   
}
