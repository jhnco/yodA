using UnityEngine;
using System.Collections.Generic;

// Spawns clouds made out of several overlapping circles (using CircleCollider-free
// simple circle sprites drawn at runtime via a small mesh, or you can swap in your own sprite).
// Attach this to an empty GameObject in the scene.
public class Cloudspawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("How many clouds to spawn.")]
    public int cloudCount = 5;

    [Tooltip("Time between spawns in seconds (0 = spawn all at start).")]
    public float spawnInterval = 2f;

    [Tooltip("How far clouds can spawn from this GameObject's position, on each axis (+/-).")]
    public Vector2 spawnRange = new Vector2(10f, 3f);

    [Header("Cloud Shape Settings")]
    [Tooltip("Min/Max number of circles per cloud.")]
    public int minCirclesPerCloud = 4;
    public int maxCirclesPerCloud = 7;

    [Tooltip("Min/Max radius of each circle.")]
    public float minCircleRadius = 0.5f;
    public float maxCircleRadius = 1.2f;

    [Tooltip("How spread out the circles are within a cloud.")]
    public float spreadX = 1.5f;
    public float spreadY = 0.6f;

    [Header("Movement (optional)")]
    [Tooltip("If true, adds a CloudMover component to each spawned cloud.")]
    public bool addMover = true;
    public float moveSpeedMin = 0.3f;
    public float moveSpeedMax = 1f;

    [Header("Appearance")]
    public Color cloudColor = Color.white;
    [Tooltip("Sorting order for the circle sprites.")]
    public int sortingOrder = 0;

    [Tooltip("Optional material (with whatever shader you want, e.g. a Particle/Additive shader) to apply to the cloud circles. Leave empty to use the default Sprite material.")]
    public Material cloudMaterial;

    public LayerMask cloudLayer;

    private float timer;
    private List<GameObject> activeClouds = new List<GameObject>();

    void Start()
    {
        if (spawnInterval <= 0f)
        {
            for (int i = 0; i < cloudCount; i++)
                SpawnCloud(); // SpawnCloud() already adds each cloud to activeClouds
        }
    }

    void Update()
    {
        if (spawnInterval <= 0f) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;

            // Clear out any clouds that were destroyed by colliding with a CloudDestroyer
            activeClouds.RemoveAll(c => c == null);

            // Only spawn into a free slot. Clouds are no longer force-destroyed by time/cap -
            // they only go away when CloudDestructible detects a "CloudDestroyer" collision.
            if (activeClouds.Count < cloudCount)
            {
                SpawnCloud();
            }
        }
    }

    void SpawnCloud()
    {
        Vector3 spawnPos = transform.position + new Vector3(
            Random.Range(-spawnRange.x, spawnRange.x),
            Random.Range(-spawnRange.y, spawnRange.y),
            0f
        );

        GameObject cloud = new GameObject("Cloud");
        cloud.tag = "Cloud";
        cloud.layer = 7;
        cloud.transform.position = spawnPos;
        activeClouds.Add(cloud);

        int circleCount = Random.Range(minCirclesPerCloud, maxCirclesPerCloud + 1);

        for (int i = 0; i < circleCount; i++)
        {
            CreateCirclePart(cloud.transform, i);
        }

        // Kinematic Rigidbody2D so trigger events fire reliably even if the
        // CloudDestroyer-tagged object doesn't have its own Rigidbody2D.
        Rigidbody2D rb = cloud.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        // Rough trigger radius covering the spread of circles that make up this cloud.
        CircleCollider2D triggerCollider = cloud.AddComponent<CircleCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = Mathf.Max(spreadX, spreadY) + maxCircleRadius;

        cloud.AddComponent<CloudDestructible>();

        if (addMover)
        {
            CloudMover mover = cloud.AddComponent<CloudMover>();
            mover.speed = Random.Range(moveSpeedMin, moveSpeedMax);
            mover.resetXLeft = transform.position.x - spawnRange.x - 5f;
            mover.resetXRight = transform.position.x + spawnRange.x + 5f;
        }
    }

    void CreateCirclePart(Transform parent, int index)
    {
        GameObject part = new GameObject("CirclePart_" + index);
        part.transform.parent = parent;

        // Random offset within the cloud's spread area
        float offsetX = Random.Range(-spreadX, spreadX);
        float offsetY = Random.Range(-spreadY, spreadY);
        part.transform.localPosition = new Vector3(offsetX, offsetY, 0f);

        float radius = Random.Range(minCircleRadius, maxCircleRadius);
        part.transform.localScale = Vector3.one * radius * 2f;

        SpriteRenderer sr = part.AddComponent<SpriteRenderer>();
        sr.sprite = GetCircleSprite();
        sr.color = cloudColor;
        sr.sortingOrder = sortingOrder;

        if (cloudMaterial != null)
        {
            // Creates its own material instance (copy of the assigned material) so each
            // cloud can be tweaked independently at runtime without affecting the shared asset.
            sr.material = new Material(cloudMaterial);
        }
    }

    // Generates (and caches) a simple white circle sprite at runtime,
    // so you don't need to import a circle texture yourself.
    private static Sprite cachedCircleSprite;
    private Sprite GetCircleSprite()
    {
        if (cachedCircleSprite != null) return cachedCircleSprite;

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                // simple anti-aliased edge
                float alpha = Mathf.Clamp01(radius - dist);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha > 0.5f ? 255 : (dist < radius ? 255 : 0)));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / 2f);
        return cachedCircleSprite;
    }

    // Draws the spawn range as a wireframe box in the Scene view when this object is selected.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnRange.x * 2f, spawnRange.y * 2f, 0f));
    }
}