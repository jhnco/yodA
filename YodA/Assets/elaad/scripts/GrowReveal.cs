using UnityEngine;
using System.Collections;

public class GrowReveal : MonoBehaviour
{
    [Tooltip("Seconds to stay invisible before growing")]
    public float delay = 3f;

    [Tooltip("Seconds the growth takes")]
    public float duration = 5f;

    [Tooltip("Softness of the growing edge, as a fraction of the object's height")]
    [Range(0.01f, 1f)] public float softness = 0.3f;

    public bool playOnStart = true;

    Renderer rend;
    Material mat;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        mat = rend.material;
        SetupBounds();
        mat.SetFloat("_Reveal", 0f); // fully invisible from the first frame
    }

    void SetupBounds()
    {
        float minY = rend.bounds.min.y;
        float maxY = rend.bounds.max.y;
        mat.SetFloat("_MinY", minY);
        mat.SetFloat("_MaxY", maxY);
        mat.SetFloat("_Soft", Mathf.Max(0.0001f, (maxY - minY) * softness));
    }

    void Start()
    {
        if (playOnStart) Play();
    }

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(Grow());
    }

    IEnumerator Grow()
    {
        SetupBounds();
        mat.SetFloat("_Reveal", 0f);

        yield return new WaitForSeconds(delay);

        SetupBounds(); // re-measure in case the object moved during the delay

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            mat.SetFloat("_Reveal", t / duration);
            yield return null;
        }
        mat.SetFloat("_Reveal", 1f);
    }
}