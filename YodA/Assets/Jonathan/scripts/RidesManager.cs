using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RidesManager : MonoBehaviour
{
    [SerializeField] string name;
    PlayerMovement PlayerMovement;
    TreeCollManager TreeCollManager;
    TreeAnimations TreeAnimations;
    [SerializeField] TMP_Text pressESlide;
    [SerializeField] TMP_Text pressESwing;
    [SerializeField] TMP_Text pressECarousel;
    Dictionary<string, TMP_Text> RidesToText = new Dictionary<string, TMP_Text>();
    Dictionary<string, TMP_Text> DestRidesToText = new Dictionary<string, TMP_Text>();
    [SerializeField] TMP_Text pressESlideDest;
    [SerializeField] TMP_Text pressESwingDest;
    [SerializeField] TMP_Text pressECarouselDest;
    bool inUsageRange = false;
    bool isRidingRide = false;
    float lastRideTime;
    [SerializeField] float delayBetweenRides;
    string useText = "Press E to ride";
    string destroyText = "Press F to destroy";
    bool rodeAllRidesMoreThanOnce = true;
    bool isDestroyed = false;
    [SerializeField] GameObject Parent;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PlayerMovement = FindFirstObjectByType<PlayerMovement>();
        TreeCollManager = FindFirstObjectByType<TreeCollManager>();
        TreeAnimations = FindFirstObjectByType<TreeAnimations>();

        RidesToText.Add("Slide", pressESlide);
        RidesToText.Add("Swing", pressESwing);
        RidesToText.Add("Carousel", pressECarousel);

        DestRidesToText.Add("Slide", pressESlideDest);
        DestRidesToText.Add("Swing", pressESwingDest);
        DestRidesToText.Add("Carousel", pressECarouselDest);

        RidesToText[name].text = "";
        DestRidesToText[name].text = "";
    }

    // Update is called once per frame
    void Update()
    {
        if(inUsageRange && !isRidingRide)
        {
            if(Input.GetKeyDown(KeyCode.E) && !isDestroyed && Time.timeSinceLevelLoad >= lastRideTime + delayBetweenRides)
            {
                if(!TreeCollManager.hasRiddenRide)
                {
                    TreeCollManager.hasRiddenRide = true;
                }
                isRidingRide = true;
                TreeAnimations.isHappy = true;
                PlayerMovement.SetCanMove(false);
                TreeCollManager.RideAmount[name]++;
                TreeCollManager.rideAmountSum++;

                RidesToText[name].text = $"Riding {name}";

                Invoke("StopRiding", 3f);
            }

            else if(Input.GetKeyDown(KeyCode.F) && !isDestroyed && rodeAllRidesMoreThanOnce && Time.timeSinceLevelLoad >= lastRideTime + delayBetweenRides)
            {
                Parent.GetComponent<SpriteRenderer>().color = Color.black;
                TreeCollManager.numDestroyed++;
                RidesToText[name].text = "";
                DestRidesToText[name].text = "";
                isDestroyed = true;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(!TreeCollManager.treeIsTalking && TreeCollManager.firstTalkHappened && other.gameObject.tag == "Player")
        {
            if(!isDestroyed)
            {
                RidesToText[name].text = useText + " " + name;
                inUsageRange = true;
                rodeAllRidesMoreThanOnce = true;
                foreach (var ride in TreeCollManager.RideAmount)
                {
                    if (ride.Value < 1)
                    {
                        rodeAllRidesMoreThanOnce = false;
                    }
                }
                if (rodeAllRidesMoreThanOnce)
                {
                    DestRidesToText[name].text = destroyText + " " + name;
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            RidesToText[name].text = "";
            DestRidesToText[name].text = "";
            inUsageRange = false;
        }
    }

    void StopRiding()
    {
        TreeAnimations.isHappy = false;
        PlayerMovement.SetCanMove(true);
        lastRideTime = Time.timeSinceLevelLoad;
        RidesToText[name].text = useText + " " + name;
        isRidingRide = false;

    }
}
