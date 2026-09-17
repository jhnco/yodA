using UnityEngine;

// Attach this to a cloud object. It just moves left or right at a constant speed.
public class CloudMover : MonoBehaviour
{
    [Tooltip("Units per second. Positive = move right, Negative = move left.")]
    public float speed = 1f;

    [Tooltip("If true, cloud wraps around to the other side of the screen when it goes off-screen.")]
    public bool wrapAround = true;

    [Tooltip("X position where cloud resets to if wrapAround is on and it moves off the right edge.")]
    public float resetXRight = 15f;

    [Tooltip("X position where cloud resets to if wrapAround is on and it moves off the left edge.")]
    public float resetXLeft = -15f;

    void Update()
    {
        transform.position += Vector3.right * speed * Time.deltaTime;

        if (!wrapAround) return;

        if (speed > 0 && transform.position.x > resetXRight)
        {
            transform.position = new Vector3(resetXLeft, transform.position.y, transform.position.z);
        }
        else if (speed < 0 && transform.position.x < resetXLeft)
        {
            transform.position = new Vector3(resetXRight, transform.position.y, transform.position.z);
        }
    }
}