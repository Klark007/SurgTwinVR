using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
//using UnityEditor.Rendering.LookDev;

public class Setup : MonoBehaviour
{
    int qty = 0;
    float currentAvgFPS = 0;

    float minFPS = 500;
    float maxFPS = 0;

    public Camera center_camera;
    UnityEngine.Rendering.Universal.UniversalAdditionalCameraData additionalCameraData;

    GameObject orx;
    GameObject playerController;
    GameObject plane;
    UnityEngine.Video.VideoPlayer videoPlayer;

    public enum VideoState
    {
        Paused,
        Playing,
        Finished,
        Unloaded
    }
    public VideoState videoState = VideoState.Playing;

    // Start is called before the first frame update
    void Start()
    {
        OVRPlugin.systemDisplayFrequency = 90.0f;
        Debug.LogWarning(SystemInfo.graphicsDeviceType);
        Debug.LogWarning(OVRPlugin.systemDisplayFrequency);

        playerController = gameObject.transform.Find("PlayerController").gameObject;
        playerController.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;

        orx = gameObject.transform.Find("ORX").gameObject;
        orx.SetActive(false);

        additionalCameraData = center_camera.transform.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        additionalCameraData.SetRenderer(1);

        videoPlayer = FindObjectsOfType<UnityEngine.Video.VideoPlayer>()[0];
        videoPlayer.loopPointReached += EndReached;

        plane = gameObject.transform.Find("Plane").gameObject;
    }
    void EndReached(UnityEngine.Video.VideoPlayer vp)
    {
        videoState = VideoState.Finished;
    }


    // Update is called once per frame
    void Update()
    {
        if (videoState == VideoState.Finished)
        {
            videoPlayer.enabled = false;
            additionalCameraData.SetRenderer(0);

            orx.SetActive(true);

            playerController.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;

            videoState = VideoState.Unloaded;

            plane.SetActive(false);
        }

        if (videoState != VideoState.Unloaded && OVRInput.GetDown(OVRInput.Button.One))
        {
            if (videoState == VideoState.Paused)
            {
                videoState = VideoState.Playing;
                videoPlayer.Play();
            } else if (videoState == VideoState.Playing)
            {
                videoState = VideoState.Paused;
                videoPlayer.Pause();
            }
        }

        if (videoState != VideoState.Unloaded && OVRInput.GetDown(OVRInput.Button.Two))
        {
            videoPlayer.Pause();
            videoState = VideoState.Finished;
        }

        float FPS = 1 / (Time.deltaTime / Time.timeScale);

        currentAvgFPS += (FPS - currentAvgFPS) / qty;

        minFPS = Math.Min(FPS, minFPS);
        maxFPS = Math.Max(FPS, maxFPS);

        if (qty % 30 == 0)
        {
            Debug.Log("AVG:" + currentAvgFPS.ToString());
            Debug.Log("MIN:" + minFPS.ToString());
            Debug.Log("MAX:" + maxFPS.ToString());
        }
    }
}
