using UnityEngine;

public class playerAnimationManager : MonoBehaviour
{
    Animator anim;
    PlayerMovement PlayerMovement;
    string currentAnimation;
    public string idle;
    public string walk;
    public string jump;
   // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PlayerMovement = gameObject.GetComponent<PlayerMovement>();
        anim = GetComponent<Animator>();

        ChangeAnimationState(idle);
    }

    // Update is called once per frame
    void Update()
    {
       if(PlayerMovement.currentlyWalking)
       {
            ChangeAnimationState(walk);
       }
       else 
       {
            ChangeAnimationState(idle);
       }
    }

    public void ChangeAnimationState(string newAnimation)
    {
        if (currentAnimation == newAnimation) return;
        anim.Play(newAnimation);
        currentAnimation = newAnimation;
    }
}
