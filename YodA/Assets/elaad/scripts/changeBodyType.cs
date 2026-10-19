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

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.tag == "Floor")
        {
            GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        }
    }
    
}
