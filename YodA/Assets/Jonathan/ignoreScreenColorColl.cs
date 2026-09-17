using UnityEngine;

public class ignoreScreenColorColl : MonoBehaviour
{
    [SerializeField] GameObject otherBoxCollider;
    Collider2D selfCollider;

    void Start()
    {
        selfCollider = GetComponent<Collider2D>();
        Physics2D.IgnoreCollision(selfCollider, otherBoxCollider.GetComponent<Collider2D>(), true);
    }
}
