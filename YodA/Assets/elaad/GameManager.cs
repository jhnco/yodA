using UnityEngine;

/// <summary>
/// Drives the two-phase flow:
///   1) DROP  - an arrow indicator moves left/right along the top of the screen;
///              pressing Space drops a box straight down.
///   2) MOVE  - once "dropsAllowed" boxes have landed, control switches to the
///              player character (handled by PlayerController).
///
/// Setup:
///  - Put this on an empty "GameManager" object.
///  - Assign arrowTransform (a sprite you place near the top of the screen).
///  - Assign boxPrefab: a 2D sprite with Rigidbody2D (Dynamic) + BoxCollider2D.
///  - Assign playerObject: your player GameObject, with PlayerController on it.
///    Leave playerObject's PlayerController disabled in the Inspector to start,
///    or leave the whole object inactive - GameManager will handle enabling it.
///  - Set groundLevelY to the world Y position boxes/arrow should be clamped above.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum Phase { Drop, Move }

    [Header("Phase")]
    public Phase currentPhase = Phase.Drop;

    [Header("Arrow (drop indicator)")]
    public Transform arrowTransform;
    public float arrowSpeed = 6f;
    public float arrowMinX = -7f;
    public float arrowMaxX = 7f;
    public float arrowSpawnY = 4.5f; // height the arrow sits at, top-ish of screen

    [Header("Boxes")]
    public GameObject boxPrefab;
    public int dropsAllowed = 3;
    private int dropsUsed = 0;
    private FallingBox activeFallingBox; // only one box in flight at a time

    [Header("Player")]
    public GameObject playerObject;
    public float switchDelay = 0.3f; // small pause after the 3rd box lands

    void Start()
    {
        if (arrowTransform != null)
        {
            Vector3 pos = arrowTransform.position;
            pos.y = arrowSpawnY;
            arrowTransform.position = pos;
        }

        // Player starts disabled/inactive until the drop phase finishes.
        if (playerObject != null)
        {
            playerObject.SetActive(false);
        }
    }

    void Update()
    {
        if (currentPhase == Phase.Drop)
        {
            HandleDropPhase();
        }
        // Move phase input is handled entirely inside PlayerController.
    }

    void HandleDropPhase()
    {
        if (arrowTransform == null) return;

        // Move the arrow left/right.
        float h = Input.GetAxisRaw("Horizontal"); // or use Input.GetKey(KeyCode.LeftArrow/RightArrow)
        Vector3 pos = arrowTransform.position;
        pos.x += h * arrowSpeed * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, arrowMinX, arrowMaxX);
        arrowTransform.position = pos;

        // Drop a box on Space, only if none is currently falling and we have drops left.
        if (Input.GetKeyDown(KeyCode.Space) && activeFallingBox == null && dropsUsed < dropsAllowed)
        {
            SpawnBox(pos.x);
        }
    }

    void SpawnBox(float x)
    {
        Vector3 spawnPos = new Vector3(x, arrowSpawnY, 0f);
        GameObject boxObj = Instantiate(boxPrefab, spawnPos, Quaternion.identity);

        FallingBox fb = boxObj.GetComponent<FallingBox>();
        if (fb == null) fb = boxObj.AddComponent<FallingBox>();

        fb.manager = this;
        activeFallingBox = fb;
    }

    /// <summary>Called by FallingBox once it comes to rest on the ground or another box.</summary>
    public void NotifyBoxLanded(FallingBox box)
    {
        if (activeFallingBox == box) activeFallingBox = null;

        dropsUsed++;

        if (dropsUsed >= dropsAllowed)
        {
            Invoke(nameof(SwitchToMovePhase), switchDelay);
        }
    }

    void SwitchToMovePhase()
    {
        currentPhase = Phase.Move;

        if (arrowTransform != null)
            arrowTransform.gameObject.SetActive(false);

        if (playerObject != null)
        {
            playerObject.SetActive(true);
            var pc = playerObject.GetComponent<PlayerController>();
            if (pc != null) pc.enabled = true;
        }
    }
}
