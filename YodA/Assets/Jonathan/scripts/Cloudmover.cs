using UnityEngine;

// Attach this to a cloud object. It just moves left or right at a constant speed.
public class CloudMover : MonoBehaviour
{
    [Tooltip("Units per second. Positive = move right, Negative = move left.")]
    public float speed = 1f;


    void Update()
    {
        transform.position += Vector3.right * speed * Time.deltaTime;
    }
}