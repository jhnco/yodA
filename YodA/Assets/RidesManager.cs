using System;
using System.Collections.Generic;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

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
    bool inUsageRange = false;
    bool isRidingRide = false;

    string text = "Press E to ride";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PlayerMovement = FindFirstObjectByType<PlayerMovement>();
        TreeCollManager = FindFirstObjectByType<TreeCollManager>();
        TreeAnimations = FindFirstObjectByType<TreeAnimations>();

        RidesToText.Add("Slide", pressESlide);
        RidesToText.Add("Swing", pressESwing);
        RidesToText.Add("Carousel", pressECarousel);
        RidesToText[name].text = "";
    }

    // Update is called once per frame
    void Update()
    {
        if(inUsageRange)
        {
            if(Input.GetKeyDown(KeyCode.E))
            {
                isRidingRide = true;
                TreeAnimations.isHappy = true;
                PlayerMovement.SetCanMove(false);
                Debug.Log("Riding ride");
                Invoke("StopRiding", 3f);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(TreeCollManager.firstTalkHappened && other.gameObject.tag == "Player")
        {
            RidesToText[name].text = text + " " + name;
            inUsageRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            RidesToText[name].text = "";
            inUsageRange = false;
        }
    }

    void StopRiding()
    {
        isRidingRide = false;
        TreeAnimations.isHappy = false;
        PlayerMovement.SetCanMove(true);
        Debug.Log("Stopped riding ride");
    }
}
