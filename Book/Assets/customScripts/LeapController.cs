﻿using UnityEngine;
using Leap;
using System.Timers;


public class LeapController : MonoBehaviour
{

    private Leap.Controller controller;
    private LeapProvider leapProvider;
    //   public GameObject picture;
    public MegaBookBuilder book;
    public AudioSource pageTurnSoundSlow;
    public GameObject prefabPage;
    public Texture2D frontTexture;
    public Texture2D backTexture;
    public AudioSource pageTurnSoundFast;
    private Vector3 picturePosition;
    bool pictureCurrentlyDragged;
   //private LeapPageTurner leapPageTurner;
    private LeapDragAndDrop leapDragAndDrop;
    private LeapPageTurnerV2 leapPageTurnerV2;
    private bool notTapping = true;
    private bool notTappingNTimer = true;
    private Timer tapTimer;

    void Start()
    {
        pictureCurrentlyDragged = false;
        controller = new Controller();
        leapProvider = FindObjectOfType<LeapProvider>() as LeapProvider;
        if (!controller.IsConnected)
        {
            Debug.LogError("no LeapMotion Device detected...");

        }
        else {
            Debug.Log("Device connected, continue");

        }
     //   leapPageTurner = new LeapPageTurner(book, controller, pageTurnSoundSlow, pageTurnSoundFast);
        leapDragAndDrop = new LeapDragAndDrop(leapProvider);
        leapPageTurnerV2 = new LeapPageTurnerV2(book, controller, pageTurnSoundSlow);
    }

    public LeapDragAndDrop GetLeapDragAndDrop() {
        return leapDragAndDrop;
    }

    void Update()
    {
        Frame frame = leapProvider.CurrentFrame;

        if (!pictureCurrentlyDragged)
        {
            //leapPageTurner.CheckPageTurnGesture(frame.Hands);
            leapPageTurnerV2.CheckPageTurnGesture(frame.Hands);
        }

        foreach (Hand hand in frame.Hands)
        {
            if (hand.IsRight)
            {
                DragAndDropChecker(hand);
                TapChecker(hand);
                //leapDragAndDrop.CheckDragAndDropGesture(hand);
            }
            if (!pictureCurrentlyDragged && hand.GrabStrength > 0.7)
            {
                Vector3 unityLeap = hand.PalmPosition.ToUnity();
                foreach (GameObject pageCollider in GameObject.FindGameObjectsWithTag("page collider"))
                {
                    Vector3 distance = unityLeap - pageCollider.transform.position;
                    if (distance.magnitude < 0.5)
                    {
                        if (!pageCollider.GetComponent<PageCollider>().next)
                        {
                            makeOutOfBookPicture(false, new Vector3(-1.5f, 0, 0) + pageCollider.transform.position, backTexture);
                        }
                        if (pageCollider.GetComponent<PageCollider>().next)
                        {
                            makeOutOfBookPicture(true, new Vector3(1.5f, 0, 0) + pageCollider.transform.position, frontTexture);
                        }
                    }
                }
            }
        }
    }

    private void TapChecker(Hand hand)
    {
        if (hand.GrabStrength < 0.4 && hand.PalmNormal.y < -0.7) // && hand.)
        {
            // Debug.Log(hand.PalmNormal);
            if (hand.Fingers[1].TipVelocity.y - hand.Fingers[3].TipVelocity.y < -0.4f && notTappingNTimer)
            {
                Ray ray;
                RaycastHit hit;
                ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out hit))
                {
                        if (hit.collider.tag == "pic")
                        {
                        hit.transform.GetComponent<PictureDrag>().selectNDeselPic();
                        }
                }
                notTapping = false;
                notTappingNTimer = false;
                initializeSpeedDecrease();
            }
            else if (hand.Fingers[1].TipVelocity.y - hand.Fingers[3].TipVelocity.y > 0.3f && !notTappingNTimer)
            {
                notTapping = true;
            }
        }
    }

    private void initializeSpeedDecrease()
    {
        tapTimer = new Timer(1000); //Set Timer intervall 
        tapTimer.Elapsed += tapTimerReset; // Hook up the method to the timer
        tapTimer.Enabled = true;
    }

    void tapTimerReset(object sender, ElapsedEventArgs e)
    {
        if(notTapping)
        {
            notTappingNTimer = true;
            tapTimer.Enabled = false;
        }
    }

    private void makeOutOfBookPicture(bool front, Vector3 pushDirection, Texture2D texture)
    {
        try
        {
            int pageNum = book.GetCurrentPage();
            if (!front)
                pageNum = pageNum - 1;

            Texture2D pageTexture = book.GetPageTexture(pageNum, front) as Texture2D;

            if (texture == pageTexture)
            {
                return;
            }

            Material material = new Material(Shader.Find("Standard"));
            material.SetTexture("_MainTex", pageTexture);


            book.SetPageTexture(texture, pageNum, front);
            material.SetTexture("_BumpMap", Resources.Load("Textures/MegaBook_Mask_Map") as Texture2D);

            prefabPage.GetComponent<Renderer>().material = material;
            Instantiate(prefabPage, pushDirection, new Quaternion(1, 0, 0, 1));
        }
        catch (System.Exception e)
        {
            Debug.Log(e.Data);
        }
    }

    //ToDo: put function below into a own class

    private void DragAndDropChecker(Hand rightHand)
    {
        pictureCurrentlyDragged = false;
        float grabStrength = rightHand.GrabStrength;
        if (rightHand.PalmNormal.y < -0.7)
        {
            if (grabStrength > 0.6)
            {
                //Debug.Log("Grab detected... strength is:" + grabStrength);
                Vector3 unityLeap = rightHand.PalmPosition.ToUnity();
                try
                {
                    foreach (GameObject pic in GameObject.FindGameObjectWithTag("pic").GetComponent<PictureDrag>().getCreatedObjects())
                    {
                        Vector3 distance = unityLeap - pic.transform.position;
                        //Debug.Log("distance is " + distance.magnitude);
                        if (distance.magnitude < 0.7)
                        {
                            pictureCurrentlyDragged = true;
                            picturePosition.x = rightHand.PalmPosition.x;
                            picturePosition.z = rightHand.PalmPosition.z;
                            picturePosition.y = rightHand.PalmPosition.y - 0.2f;
                            pic.transform.position = picturePosition;
                            pic.GetComponent<PictureDrag>().setAsFirstInList(pic);
                            return;
                        }
                    }
                }
                catch
                {

                }
            }
        }
    }
}
