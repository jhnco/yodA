using UnityEngine;

public class UnChildOnTrigger : MonoBehaviour
{
    [Tooltip("The object that must enter the trigger to cause the detach (e.g. the player).")]
    public GameObject objectA;

    [Tooltip("The child object that will be detached and become independent.")]
    public GameObject objectB;

    [Tooltip("Vertical offset from the middle of the trigger. Negative = down, positive = up (e.g. -5 goes 5 units below the middle).")]
    public float yOffset = 0f;

    private bool hasDetached = false;
    private Collider2D triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasDetached || other.gameObject != objectA) return;

        Debug.Log("Object A entered trigger - detaching Object B");

        // Unparent Object B, keeping its world position for now.
        objectB.transform.SetParent(null, true);

        // Find the middle of the trigger (falls back to this object's position).
        Vector3 middle = triggerCollider != null
            ? triggerCollider.bounds.center
            : transform.position;

        // Move Object B to the middle, applying the Y offset. Keep its original Z.
        objectB.transform.position = new Vector3(
            middle.x,
            middle.y + yOffset,
            objectB.transform.position.z
        );

        hasDetached = true;
    }
}