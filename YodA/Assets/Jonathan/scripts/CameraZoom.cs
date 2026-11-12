using UnityEngine;

/// <summary>
/// Put this on the camera. Two states:
///   1. Normal  - the camera's original position (and size, if orthographic).
///   2. Zoomed  - the camera moves so the edges of the box GameObject fit the screen.
/// Works with both perspective cameras (the camera moves closer/farther)
/// and orthographic cameras (the camera's Size changes).
/// The box can be anything with a Renderer (sprite, mesh) or a Collider/Collider2D.
/// If it has none of those, its transform scale is used as the box size.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraZoom : MonoBehaviour
{
    [Header("Zoom Box")]
    [Tooltip("The box GameObject to zoom in on. Its edges fill the screen when zoomed.")]
    [SerializeField] GameObject zoomBox;

    [Tooltip("1 = box edges touch the screen edges. Higher = more empty space around the box.")]
    [SerializeField] float padding = 1.05f;

    [Header("Smoothing")]
    [Tooltip("Roughly how many seconds the move takes. Smaller = faster.")]
    [SerializeField] float smoothTime = 0.4f;

    public bool IsZoomedIn { get; private set; }

    /// <summary>How long the zoom takes. Other scripts can use this to stay in sync with the camera.</summary>
    public float SmoothTime => smoothTime;

    Camera cam;
    Vector3 normalPosition;
    Quaternion normalRotation;
    float normalOrthoSize;

    Vector3 velocity;
    float sizeVelocity;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        // Remember the camera's starting view as the "normal" state.
        normalPosition = transform.position;
        normalRotation = transform.rotation;
        normalOrthoSize = cam.orthographicSize;

        if (zoomBox == null)
            Debug.LogWarning("CameraZoom: Zoom Box is not assigned, so the camera has nothing to zoom in on.", this);
    }

    void LateUpdate()
    {
        Vector3 wantedPos = normalPosition;
        float wantedSize = normalOrthoSize;

        if (IsZoomedIn && zoomBox != null)
            ComputeZoom(GetBoxBounds(zoomBox), out wantedPos, out wantedSize);

        transform.position = Vector3.SmoothDamp(transform.position, wantedPos, ref velocity, smoothTime);
        transform.rotation = normalRotation;

        if (cam.orthographic)
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, wantedSize, ref sizeVelocity, smoothTime);
    }

    // ---------------- Public API ----------------

    /// <summary>State 2: zoom in on the box.</summary>
    public void ZoomIn()
    {
        IsZoomedIn = true;
    }

    /// <summary>Zoom in on a different box (also switches to the zoomed state).</summary>
    public void ZoomIn(GameObject newBox)
    {
        zoomBox = newBox;
        IsZoomedIn = true;
    }

    /// <summary>State 1: go back to the normal view.</summary>
    public void ZoomOut()
    {
        IsZoomedIn = false;
    }

    /// <summary>true = zoomed in, false = normal.</summary>
    public void SetZoom(bool zoomedIn)
    {
        IsZoomedIn = zoomedIn;
    }

    public void ToggleZoom()
    {
        IsZoomedIn = !IsZoomedIn;
    }

    /// <summary>Change which box the camera zooms in on, without changing the state.</summary>
    public void SetZoomBox(GameObject newBox)
    {
        zoomBox = newBox;
    }

    /// <summary>Update the "normal" view, e.g. if the level moves the camera elsewhere.</summary>
    public void SetNormalView(Vector3 position, Quaternion rotation)
    {
        normalPosition = position;
        normalRotation = rotation;
    }

    // ---------------- Box -> camera position / size ----------------

    /// <summary>
    /// Works out where the camera has to be (keeping its rotation) so the whole box fits on screen.
    /// Perspective: moves along the camera's forward axis. Orthographic: changes the camera size.
    /// </summary>
    void ComputeZoom(Bounds b, out Vector3 pos, out float orthoSize)
    {
        Vector3 right = normalRotation * Vector3.right;
        Vector3 up = normalRotation * Vector3.up;
        Vector3 fwd = normalRotation * Vector3.forward;

        float tanV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float tanH = tanV * cam.aspect;

        Vector3 e = b.extents;

        float maxX = 0f;      // widest the box gets, as seen by the camera (half-width)
        float maxY = 0f;      // tallest the box gets, as seen by the camera (half-height)
        float frontDepth = 0f; // how far the front of the box sticks out toward the camera
        float dist = 0f;      // perspective: distance from the box center that fits every corner

        // Check all 8 corners of the box.
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    Vector3 offset = new Vector3(e.x * sx, e.y * sy, e.z * sz);

                    float x = Mathf.Abs(Vector3.Dot(offset, right));
                    float y = Mathf.Abs(Vector3.Dot(offset, up));
                    float z = Vector3.Dot(offset, fwd); // positive = farther from the camera than the center

                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                    frontDepth = Mathf.Max(frontDepth, -z);

                    dist = Mathf.Max(dist, x / tanH - z, y / tanV - z);
                }

        if (cam.orthographic)
        {
            // Orthographic: distance doesn't change what you see, the Size does.
            orthoSize = Mathf.Max(maxY, maxX / cam.aspect) * padding;

            // Center on the box, keeping the camera at its normal depth.
            float normalDepth = Vector3.Dot(normalPosition - b.center, fwd);
            pos = b.center + fwd * normalDepth;
        }
        else
        {
            // Perspective: move closer/farther until the box fits.
            dist *= padding;

            // Never end up inside the box / in front of the near plane.
            dist = Mathf.Max(dist, cam.nearClipPlane + frontDepth + 0.01f);

            orthoSize = normalOrthoSize;
            pos = b.center - fwd * dist;
        }
    }

    /// <summary>
    /// Gets the box's edges: Renderers first, then Collider2Ds, then Colliders, then the transform scale.
    /// </summary>
    static Bounds GetBoxBounds(GameObject go)
    {
        bool has = false;
        Bounds b = new Bounds(go.transform.position, Vector3.zero);

        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }
        if (has) return b;

        foreach (Collider2D c in go.GetComponentsInChildren<Collider2D>())
        {
            if (!has) { b = c.bounds; has = true; }
            else b.Encapsulate(c.bounds);
        }
        if (has) return b;

        foreach (Collider c in go.GetComponentsInChildren<Collider>())
        {
            if (!has) { b = c.bounds; has = true; }
            else b.Encapsulate(c.bounds);
        }
        if (has) return b;

        return new Bounds(go.transform.position, go.transform.lossyScale);
    }

    // ---------------- Editor helper ----------------

    void OnDrawGizmosSelected()
    {
        if (zoomBox == null)
            return;

        // Outline of the edges the camera will fit to the screen.
        Bounds b = GetBoxBounds(zoomBox);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(b.center, b.size);
    }
}