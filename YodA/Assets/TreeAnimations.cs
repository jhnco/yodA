using UnityEngine;

public class TreeAnimations : MonoBehaviour
{
    

    public bool isHappy = false;
    [SerializeField] Sprite sad;
    [SerializeField] Sprite happy;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(isHappy & gameObject.GetComponent<SpriteRenderer>().sprite != happy)
        {
            makeHappy();
        }
        else if(!isHappy & gameObject.GetComponent<SpriteRenderer>().sprite != sad)
        {
            makeSad();
        }
    }

    void makeHappy()
    {
        gameObject.GetComponent<SpriteRenderer>().sprite = happy;
    }

    void makeSad()
    {
        gameObject.GetComponent<SpriteRenderer>().sprite = sad;
    }
}
