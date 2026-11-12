using TMPro;
using UnityEngine;
using System.Collections;

public class TreeCollManager : MonoBehaviour
{
    PlayerMovement PlayerMovement;
    CameraZoom CameraZoom;

    public TMP_Text speechText;

    [Header("Text Zoom")]
    [Tooltip("How many times bigger the text gets when enlarged (1 = no change).")]
    [SerializeField] float zoomedTextMultiplier = 1.5f;

    int collCount = 0;

    float normalFontSize;
    float textScale = 1f;       // current multiplier applied to the font size
    float textTargetScale = 1f; // multiplier we are heading toward
    float textVelocity;

    void Start()
    {
        PlayerMovement = FindFirstObjectByType<PlayerMovement>();
        CameraZoom = FindFirstObjectByType<CameraZoom>();

        if (speechText != null)
            normalFontSize = speechText.fontSize;
    }

    void Update()
    {
        if (speechText == null)
            return;

        // Same smoothing time as the camera so the text and the zoom move together.
        float smooth = CameraZoom != null ? CameraZoom.SmoothTime : 0.4f;

        textScale = Mathf.SmoothDamp(textScale, textTargetScale, ref textVelocity, smooth);
        speechText.fontSize = normalFontSize * textScale;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            collCount++;
        }

        switch (collCount)
        {
            case 1:
                StartCoroutine(FirstTalk());
                break;
        }
    }

    IEnumerator FirstTalk()
    {
        PlayerMovement.SetCanMove(false);
        CameraZoom.ZoomIn();
        InlargeTextSize();
        speechText.text = "My goodness a person! No one has come here in ages!";
        yield return new WaitForSeconds(4);
        speechText.text = "Please, Go on a ride, I'll give you my most special Leaf.";
        yield return new WaitForSeconds(5);
        speechText.text = "";
        ReduceTextSize();
        PlayerMovement.SetCanMove(true);
        CameraZoom.ZoomOut();
    }

    // ---------------- Text zoom ----------------

    /// <summary>Smoothly enlarges the text. Call it together with CameraZoom.ZoomIn().</summary>
    public void InlargeTextSize()
    {
        textTargetScale = zoomedTextMultiplier;
    }

    /// <summary>Smoothly shrinks the text back to normal. Call it together with CameraZoom.ZoomOut().</summary>
    public void ReduceTextSize()
    {
        textTargetScale = 1f;
    }


}