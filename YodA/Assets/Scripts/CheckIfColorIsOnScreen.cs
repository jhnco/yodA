using System.Collections;
using UnityEngine;

public class CheckIfColorIsOnScreen : MonoBehaviour
{
    [SerializeField] private Color targetColor = Color.red;
    [SerializeField] private float checkInterval = 3f;

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= checkInterval)
        {
            timer = 0f;
            StartCoroutine(CheckColorRoutine());
        }
    }

    private IEnumerator CheckColorRoutine()
    {
        yield return new WaitForEndOfFrame();

        bool found = IsColorOnScreen();
        if (found)
        {
            Debug.Log("Color detected!");
        }
    }

    /// <summary>
    /// Only call this AFTER WaitForEndOfFrame (see CheckColorRoutine above).
    /// Calling it directly from Update/Start will fail.
    /// </summary>
    public bool IsColorOnScreen()
    {
        Texture2D screenTex = ScreenCapture.CaptureScreenshotAsTexture();

        if (screenTex == null)
        {
            Debug.LogWarning("Screenshot capture failed — texture was null.");
            return false;
        }

        Color[] pixels = screenTex.GetPixels();

        bool found = false;
        foreach (Color pixel in pixels)
        {
            if (pixel == targetColor)
            {
                found = true;
                break;
            }
        }

        Destroy(screenTex);
        return found;
    }
}