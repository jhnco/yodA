using UnityEngine;

public class changeBodyType : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Floor"))
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
        }
    }
}
