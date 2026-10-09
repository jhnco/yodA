using UnityEngine;

public class ChildOnTrigger : MonoBehaviour
{
    [Tooltip("The parent object (where Object B will be attached).")]
    public GameObject objectA;

    [Tooltip("The specific object that must enter the trigger to become parented.")]
    public GameObject objectB;

    [Tooltip("Speed of the camera transition.")]
    public float smoothSpeed = 5f;

    private bool isTransitioning = false;
    private readonly Vector3 targetLocalPosition = new Vector3(0, 0, -10);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == objectA)
        {
            Debug.Log("Object A entered trigger");

            // Parent Object B to Object A immediately
            objectB.transform.SetParent(objectA.transform);

            // Begin smooth transition in Update()
            isTransitioning = true;
        }
    }

    private void Update()
    {
        if (isTransitioning && objectB != null)
        {
            // Smoothly move towards local (0, 0, -10) relative to Object A
            objectB.transform.localPosition = Vector3.Lerp(
                objectB.transform.localPosition,
                targetLocalPosition,
                Time.deltaTime * smoothSpeed
            );

            // Stop calculating once close enough to save performance
            if (Vector3.Distance(objectB.transform.localPosition, targetLocalPosition) < 0.01f)
            {
                objectB.transform.localPosition = targetLocalPosition;
                isTransitioning = false;
            }
        }
    }
}