using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Runs after CameraZoom each frame, so the text always uses the camera's final position.
[DefaultExecutionOrder(100)]
public class TreeCollManager : MonoBehaviour
{
    PlayerMovement PlayerMovement;
    CameraZoom CameraZoom;
    TreeAnimations TreeAnimations;
    Camera gameCam;
    public TMP_Text speechText;

    [Header("Text Zoom")]
    [Tooltip("How many times bigger the text gets when enlarged (1 = no change).")]
    [SerializeField] float zoomedTextMultiplier = 1.5f;

    [Tooltip("Z of the plane in the game world the text is attached to (the plane your characters stand on).")]
    [SerializeField] float worldPlaneZ = 0f;

    public bool firstTalkHappened = false;
    public bool hasRiddenRide = false;
    public bool treeIsTalking = false;

    string[] oneMoreRideStart = new string[4];
    string[] oneMoreRideEnd = new string[3];
    string[] specificRideEnd = new string[3];

    float textScale = 1f;       // current size multiplier of the text
    float textTargetScale = 1f; // multiplier we are heading toward
    float textVelocity;

    // World anchoring: the text stays on the same spot in the game world while the camera zooms
    bool anchored;
    Vector3 anchorWorld;
    Vector3 startLocalPos;
    Vector3 startLocalScale;

    public Dictionary<string, int> RideAmount = new Dictionary<string, int>();
    public int rideAmountSum = 0;

    bool getLeaf = true;
    public int numDestroyed = 0;
    int destroyedTalkNum = 0;

    void Start()
    {
        PlayerMovement = FindFirstObjectByType<PlayerMovement>();
        CameraZoom = FindFirstObjectByType<CameraZoom>();
        TreeAnimations = FindFirstObjectByType<TreeAnimations>();

        gameCam = CameraZoom != null ? CameraZoom.GetComponent<Camera>() : Camera.main;

        oneMoreRideStart[0] = "Oh the leaf? ";
        oneMoreRideStart[1] = "About the leaf, ";
        oneMoreRideStart[2] = "I will give you the leaf soon, ";
        oneMoreRideStart[3] = "Don't worry you'll get the leaf soon, ";

        oneMoreRideEnd[0] = "just one more ride.";
        oneMoreRideEnd[1] = "another ride and its yours.";
        oneMoreRideEnd[2] = "just go on one more ride.";

        specificRideEnd[0] = "go slide and you'll get it.";
        specificRideEnd[1] = "go swing and you'll get it.";
        specificRideEnd[2] = "go on the carousel and you'll get it.";


        RideAmount.Add("Slide", 0);
        RideAmount.Add("Swing", 0);
        RideAmount.Add("Carousel", 0);


    }

    void Update()
    {
        if (speechText == null)
            return;

        if (numDestroyed == 1 && destroyedTalkNum != 1)
        {
            StartCoroutine(FirstDestroyed());
            destroyedTalkNum++;
        }
        else if (numDestroyed == 2 && destroyedTalkNum != 2)
        {
            StartCoroutine(SecondDestroyed());
            destroyedTalkNum++;
        }
    }

    void LateUpdate()
    {
        if (speechText == null || gameCam == null)
            return;

        // Same smoothing time as the camera so the text and the zoom move together.
        float smooth = CameraZoom != null ? CameraZoom.SmoothTime : 0.4f;
        textScale = Mathf.SmoothDamp(textScale, textTargetScale, ref textVelocity, smooth);

        if (!anchored)
            return;

        RectTransform rt = speechText.rectTransform;

        // Finished shrinking: put the text back exactly as it was.
        if (textTargetScale == 1f && Mathf.Abs(textScale - 1f) < 0.001f)
        {
            textScale = 1f;
            rt.localScale = startLocalScale;
            rt.localPosition = startLocalPos;
            anchored = false;
            return;
        }

        // Scale the whole text box (not just the font), so the text never re-wraps onto another line.
        rt.localScale = startLocalScale * textScale;

        // Keep the center of the text on the same world point, wherever the camera is.
        Vector2 screenPoint = gameCam.WorldToScreenPoint(anchorWorld);
        RectTransform parent = rt.parent as RectTransform;

        if (parent != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, GetUICamera(), out Vector2 local))
        {
            // Move the pivot so that the text's center (not its pivot) lands on the anchor point.
            Vector2 centerOffset = Vector2.Scale(rt.rect.center, rt.localScale);
            rt.localPosition = new Vector3(local.x - centerOffset.x, local.y - centerOffset.y, startLocalPos.z);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            foreach (var ride in RideAmount)
            {
                if (ride.Value < 3)
                {
                    getLeaf = false;
                }
            }

            if (getLeaf && numDestroyed == 0)
            {
                StartCoroutine(GiveLeaf());
            }
            else
            {
                if (numDestroyed == 0)
                {
                    if (!firstTalkHappened)
                    {
                        firstTalkHappened = true;
                        StartCoroutine(FirstTalk());
                    }

                    else if (RideAmount["Slide"] != RideAmount["Carousel"] && RideAmount["Carousel"] == RideAmount["Swing"] && RideAmount["Slide"] < RideAmount["Carousel"])
                    {
                        StartCoroutine(SpecificTalk(0));
                    }
                    else if (RideAmount["Swing"] != RideAmount["Carousel"] && RideAmount["Carousel"] == RideAmount["Slide"] && RideAmount["Swing"] < RideAmount["Carousel"])
                    {
                        StartCoroutine(SpecificTalk(1));
                    }
                    else if (RideAmount["Carousel"] != RideAmount["Slide"] && RideAmount["Swing"] == RideAmount["Slide"] && RideAmount["Carousel"] < RideAmount["Slide"])
                    {
                        StartCoroutine(SpecificTalk(2));
                    }
                    else if (hasRiddenRide)
                    {
                        StartCoroutine(OtherTalk());
                    }
                }

            }
            getLeaf = true;

        }
    }

    IEnumerator FirstTalk()
    {
        treeIsTalking = true;
        TreeAnimations.isHappy = true;
        PlayerMovement.SetCanMove(false);
        CameraZoom.ZoomIn();
        InlargeTextSize();
        speechText.text = "My goodness a person! No one has come here in ages!";
        yield return new WaitForSeconds(4);
        speechText.text = "Please, Go on a ride, I'll give you my most special leaf!";
        yield return new WaitForSeconds(4);
        speechText.text = "Im only happy when someone rides the rides...";
        yield return new WaitForSeconds(3);
        speechText.text = "";
        TreeAnimations.isHappy = false;
        ReduceTextSize();
        PlayerMovement.SetCanMove(true);
        CameraZoom.ZoomOut();
        treeIsTalking = false;
    }

    IEnumerator OtherTalk()
    {
        treeIsTalking = true;
        PlayerMovement.SetCanMove(false);
        CameraZoom.ZoomIn();
        InlargeTextSize();
        speechText.text = $"{oneMoreRideStart[Random.Range(0, 4)]}{oneMoreRideEnd[Random.Range(0, 3)]}";
        yield return new WaitForSeconds(4);
        speechText.text = "";
        ReduceTextSize();
        PlayerMovement.SetCanMove(true);
        CameraZoom.ZoomOut();
        treeIsTalking = false;
    }

    IEnumerator SpecificTalk(int ride)
    {
        treeIsTalking = true;
        PlayerMovement.SetCanMove(false);
        CameraZoom.ZoomIn();
        InlargeTextSize();
        speechText.text = $"{oneMoreRideStart[Random.Range(0, 4)]}{specificRideEnd[ride]}";
        yield return new WaitForSeconds(4);
        speechText.text = "";
        ReduceTextSize();
        PlayerMovement.SetCanMove(true);
        CameraZoom.ZoomOut();
        treeIsTalking = false;
    }

    IEnumerator GiveLeaf()
    {
        treeIsTalking = true;
        PlayerMovement.SetCanMove(false);
        CameraZoom.ZoomIn();
        InlargeTextSize();
        speechText.text = $"*Gives Leaf*";
        yield return new WaitForSeconds(4);
        speechText.text = "";
        ReduceTextSize();
        PlayerMovement.SetCanMove(true);
        CameraZoom.ZoomOut();
        treeIsTalking = false;
    }

    IEnumerator FirstDestroyed()
    {
        treeIsTalking = true;
        PlayerMovement.SetCanMove(false);
        CameraZoom.ZoomIn();
        InlargeTextSize();
        speechText.text = $"Nooo!! Why would you do that?!";
        yield return new WaitForSeconds(4);
        speechText.text = "";
        ReduceTextSize();
        PlayerMovement.SetCanMove(true);
        CameraZoom.ZoomOut();
        treeIsTalking = false;
    }

    IEnumerator SecondDestroyed()
    {
        treeIsTalking = true;
        PlayerMovement.SetCanMove(false);
        CameraZoom.ZoomIn();
        InlargeTextSize();
        speechText.text = $"Stop it! I'll give you the leaf! Just leave... Please.";
        yield return new WaitForSeconds(4);
        speechText.text = "";
        ReduceTextSize();
        PlayerMovement.SetCanMove(true);
        CameraZoom.ZoomOut();
        treeIsTalking = false;
    }


    // ---------------- Text zoom ----------------

    /// <summary>Smoothly enlarges the text and pins it to its current spot in the game world.
    /// Call it together with CameraZoom.ZoomIn().</summary>
    public void InlargeTextSize()
    {
        BeginAnchor();
        textTargetScale = zoomedTextMultiplier;
    }

    /// <summary>Smoothly shrinks the text back to normal. Call it together with CameraZoom.ZoomOut().</summary>
    public void ReduceTextSize()
    {
        textTargetScale = 1f;
    }

    /// <summary>Remembers which point in the game world the text is currently over.</summary>
    void BeginAnchor()
    {
        if (anchored || speechText == null || gameCam == null)
            return;

        Canvas canvas = speechText.canvas != null ? speechText.canvas.rootCanvas : null;

        // Text on a World Space canvas already moves with the camera by itself.
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            return;

        RectTransform rt = speechText.rectTransform;

        // Where the middle of the text is on screen right now (before the zoom starts).
        Vector3 centerPoint = rt.TransformPoint(rt.rect.center);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(GetUICamera(), centerPoint);

        // Which point in the game world is under that spot on screen.
        Ray ray = gameCam.ScreenPointToRay(screenPoint);
        Plane plane = new Plane(Vector3.back, new Vector3(0f, 0f, worldPlaneZ));

        if (!plane.Raycast(ray, out float enter))
            return;

        anchorWorld = ray.GetPoint(enter);
        startLocalPos = rt.localPosition;
        startLocalScale = rt.localScale;
        anchored = true;
    }

    /// <summary>The camera that renders the canvas (null for Screen Space - Overlay).</summary>
    Camera GetUICamera()
    {
        Canvas canvas = speechText.canvas != null ? speechText.canvas.rootCanvas : null;

        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera != null ? canvas.worldCamera : gameCam;
    }


}