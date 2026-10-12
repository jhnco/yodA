using TMPro;
using UnityEngine;

public class TurnTriggerToCollide : MonoBehaviour
{
    [SerializeField] GameObject Object;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.gameObject.tag == "Player")
        {
            if (Object)
            {
                Object.GetComponent<Collider2D>().isTrigger = false;
            }
            else
            {
                gameObject.GetComponent<Collider2D>().isTrigger = false;
            }
        }    
    }
}
