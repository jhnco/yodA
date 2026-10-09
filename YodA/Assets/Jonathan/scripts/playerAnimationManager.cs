using UnityEngine;

public class playerAnimationManager : MonoBehaviour
{
    Animator anim;
    Rigidbody2D rb;
    PlayerMovement PlayerMovement;
    string currentAnimation;

    public string idle;
    public string walk;
    public string jump;      // optional, unused now
    public string startJump;
    public string midAir;

    enum AirState { None, StartJump, MidAir }
    AirState airState = AirState.None;

    float airStateEnterTime;

    // Grace period so we don't "land" on the same frames the player is
    // still inside the ground-check box right after leaving the floor.
    const float minAirTime = 0.1f;

    void Start()
    {
        PlayerMovement = GetComponent<PlayerMovement>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        ChangeAnimationState(idle);
    }

    void Update()
    {
        // A new jump always (re)starts the startJump animation.
        if (PlayerMovement.jumpStarted)
        {
            PlayerMovement.jumpStarted = false;
            EnterAirState(AirState.StartJump);
            ChangeAnimationState(startJump);
        }

        // Off the floor (in the air, with or without a cloud): midAir.
        // Standing on the floor while touching a cloud does NOT count.
        if (airState == AirState.None && !PlayerMovement.onFloor)
        {
            EnterAirState(AirState.MidAir);
            ChangeAnimationState(midAir);
        }

        if (airState != AirState.None)
        {
            UpdateAirAnimation();
            return;
        }

        // Ground animations
        if (PlayerMovement.currentlyWalking)
            ChangeAnimationState(walk);
        else
            ChangeAnimationState(idle);
    }

    void UpdateAirAnimation()
    {
        float timeInState = Time.time - airStateEnterTime;

        // startJump finished -> midAir
        if (airState == AirState.StartJump)
        {
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);

            // timeInState guard: the Animator needs a frame to switch states
            // after Play(), so ignore the old state's info at first.
            if (timeInState > 0.05f && info.IsName(startJump) && info.normalizedTime >= 1f)
            {
                EnterAirState(AirState.MidAir);
                ChangeAnimationState(midAir);
            }
        }

        // Landing: on the real floor (cloud contact doesn't matter), not moving upward
        if (PlayerMovement.onFloor &&
            timeInState > minAirTime &&
            rb.linearVelocity.y <= 0.1f)
        {
            airState = AirState.None;
            currentAnimation = null; // force the ground animation to play
        }
    }

    void EnterAirState(AirState state)
    {
        airState = state;
        airStateEnterTime = Time.time;
    }

    public void ChangeAnimationState(string newAnimation)
    {
        if (currentAnimation == newAnimation) return;
        anim.Play(newAnimation);
        currentAnimation = newAnimation;
    }
}