
/*===============================================================================
Copyright (C) 2020 ARWAY Ltd. All Rights Reserved.

This file is part of ARwayKit AR SDK

The ARwayKit SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of ARWAY Ltd.

===============================================================================*/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.UI;
using System;
using UnityEngine.XR.ARSubsystems;
using System.Collections;
using Microsoft.Azure.SpatialAnchors.Unity.Examples;

namespace Arway
{
    public class PointCloudToPCD : MonoBehaviour
    {
        private ARPointCloudManager pointCloudManager;

        List<Vector3> updatedPoints = new List<Vector3>();

        private bool isRecording = false;

        public GameObject StartButton;
        public GameObject StopButton;
        public Sprite startSprite;
        public Sprite stopSprite;

        public ArwaySDK m_Sdk = null;

        public TimerManager timerManager;
        public CreateAnchor createAnchor;

        [Header("UI Elements")]
        [SerializeField]
        private GameObject newMapPanel;

        [SerializeField]
        private GameObject cloudListButton;

        [SerializeField]
        private GameObject navSceneButton;

        [SerializeField]
        private UploadManager uploadManager;

        private void Start()
        {
            if (m_Sdk == null)
            {
                m_Sdk = ArwaySDK.Instance;
            }
        }

        private void Init()
        {
            updatedPoints = new List<Vector3>();
            updatedPoints.Clear();

            Debug.Log("Initializing point cloud manager....");

            // Look for ARPointCloudManager if not assigned
            if (pointCloudManager == null)
            {
                Debug.Log("pointCloudManager is null!!");
                pointCloudManager = Camera.main.gameObject.GetComponentInParent<ARPointCloudManager>();
            }

            pointCloudManager.pointCloudsChanged += OnPointCloudsChanged;
            timerManager = timerManager.GetComponent<TimerManager>();
        }

        private void OnPointCloudsChanged(ARPointCloudChangedEventArgs eventArgs)
        {
            if (isRecording)
            {
                foreach (var pointCloud in eventArgs.updated) // for updated point cloud
                //foreach (var pointCloud in eventArgs.added) // for every added point cloud
                {
                    Parallel.Invoke(
                        () => CalculateConfidance(pointCloud),
                        () => AddPointCloudToList(pointCloud)
                        );
                }
            }
        }

        float threshold = 0.0f;

        private void CalculateConfidance(ARPointCloud pointCloud)
        {
#if UNITY_EDITOR || UNITY_ANDROID
            foreach (float confidance in pointCloud.confidenceValues.Value)
            {
                threshold = confidance;
            }
#elif UNITY_IOS 
            threshold = 0.26f; // since we do not get confidencValue in iOS
#endif
        }

        int currIndex = 0;

        private void AddPointCloudToList(ARPointCloud pointCloud)
        {
            currIndex = 0;

            foreach (var pos in pointCloud.positions.Value)
            {
                currIndex++;

                if (threshold > 0.25f)
                {
#if UNITY_EDITOR || UNITY_ANDROID
                    updatedPoints.Add(pos);
#elif UNITY_IOS
                    // add alternate point values
                    if (currIndex % 2 == 0)
                        updatedPoints.Add(pos);
#endif
                }
            }
        }

        public void StartMapping()
        {
            if (!isRecording)
            {
                InitiateMapping();
            }
        }

        public void InitiateMapping()
        {
            // reset the AR Foundation Origin first... 
            m_Sdk.arSession.Reset();

            // check if tracking is good to start mapping..
            StartCoroutine(CheckTrackingState());
        }

        IEnumerator CheckTrackingState()
        {
            //loaderText.SetText("Waiting for tracking state...");
            yield return new WaitForSeconds(1f);
            if (checkTracking() > 3)
            {
                NotificationManager.Instance.GenerateNotification("Mapping started...");
                ReadyForMapping();
            }
            else
            {
                NotificationManager.Instance.GenerateNotification("Tracking state is poor");
                StartCoroutine(CheckTrackingState());
                Debug.Log("Tracking state is poor");
            }
        }

        private void ReadyForMapping()
        {
            // Get the current location of the device
            Debug.Log("Fetching Location...");
            uploadManager.GetMapCoordinates();

            Init();
            Debug.Log("moves next statement");
            pointCloudManager.enabled = true;
            createAnchor.AdvanceDemoAsync();
            updateUI(false);

            Debug.Log(">>>>>>>>>>>>>>   Mapping Started...  <<<<<<<<<<<<");

            isRecording = true;
            timerManager.StartTimer();

            StartButton.SetActive(false);
            StopButton.SetActive(true);
        }

        public int checkTracking()
        {
            int quality = 0;
            if (m_Sdk.arSession == null)
                return 0;
            var arSubsystem = m_Sdk.arSession.subsystem;
            if (arSubsystem != null && arSubsystem.running)
            {
                switch (arSubsystem.trackingState)
                {
                    case TrackingState.Tracking:
                        quality = 4;
                        break;
                    case TrackingState.Limited:
                        quality = 1;
                        break;
                    case TrackingState.None:
                        quality = 0;
                        break;
                }
            }
            return quality;
        }

        public void StopMapping()
        {
            createAnchor.AdvanceDemoAsync();

            pointCloudManager.enabled = false;
            pointCloudManager.SetTrackablesActive(false);

            isRecording = false;
            Debug.Log(">>>>>>>>>>>>  Writing PCD File.  <<<<<<<<<<<<");

            timerManager.StopTimer();

            StopButton.SetActive(false);
            StartButton.SetActive(true);

            CreatePCDAsync();
        }

        private async Task CreatePCDAsync()
        {
            FileInfo file = new FileInfo(Application.persistentDataPath + "/map/");
            file.Directory.Create();
            Debug.Log("Creating map directory.");

            // read lines in the JSON file
            string pcdFile = "map.pcd";
            string pcdPath = Path.Combine(Application.persistentDataPath + "/map/", pcdFile);

            int totalPoints = updatedPoints.Count;
            int totalLine = totalPoints + 11;

            Debug.Log("************\t Total points:" + totalPoints + " \t***************");

            string[] lines = new string[totalLine];

            lines[0] = "# .PCD v.7 - Point Cloud Data file format";
            lines[1] = "VERSION .7";
            lines[2] = "FIELDS x y z";
            lines[3] = "SIZE 4 4 4";
            lines[4] = "TYPE F F F";
            lines[5] = "COUNT 1 1 1";
            lines[6] = "WIDTH " + totalPoints;
            lines[7] = "HEIGHT 1";
            lines[8] = "VIEWPOINT 0 0 0 1 0 0 0";
            lines[9] = "POINTS " + totalPoints;
            lines[10] = "DATA ascii";

            int i = 11;
            foreach (Vector3 pos in updatedPoints)
            {
                lines[i] = pos.x * (-1) + " " + pos.y + " " + pos.z;
                i++;
            }

            Debug.Log("************\t WriteLinesToPCD \t ***************");

            await WriteLinesToPCD(pcdPath, lines);
        }

        public async Task WriteLinesToPCD(string pcdPath, string[] lines)
        {
            try
            {
                //Debug.Log("Path: " + pcdPath + " points count: " + lines.Length);
                File.WriteAllLines(pcdPath, lines);
                Debug.Log("************\t PCD file created.\t***************");

                // show new map panel and upload the pcd file
                updateUI(true);

                if (File.Exists(pcdPath))
                {
                    newMapPanel.SetActive(true);
                }
            } catch (Exception e)
            {
                Debug.Log("Exception in writing file!!" + e.ToString());
            }
        }

        private void updateUI(bool setActive)
        {
            cloudListButton.SetActive(setActive);
            navSceneButton.SetActive(setActive);
        }
    }
}