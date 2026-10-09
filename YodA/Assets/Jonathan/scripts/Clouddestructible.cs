using UnityEngine;

// Attach to a cloud (needs a Collider2D set to "Is Trigger" on the same object, or a child).
// Destroys the cloud when it enters a trigger with an object tagged "CloudDestroyer".
public class CloudDestructible : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("CloudDestroyer"))
        {
            Destroy(gameObject);
        }
    }
}