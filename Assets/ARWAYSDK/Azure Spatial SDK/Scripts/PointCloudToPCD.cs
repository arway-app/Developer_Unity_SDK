
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
using System;
using UnityEngine.XR.ARSubsystems;
using System.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.UI;

namespace Arway
{
    public class PointCloudToPCD : MonoBehaviour
    {
        private ARPointCloudManager pointCloudManager;

        List<Vector3> updatedPoints = new List<Vector3>();
        public List<Color32> updatedColors = new List<Color32>();

        private bool isRecording = false;

        public GameObject StartButton;
        public GameObject StopButton;
        public Sprite startSprite;
        public Sprite stopSprite;

        public ArwaySDK m_Sdk = null;

        public TimerManager timerManager;
        public CreateAnchor createAnchor;

        private Texture2D m_Texture;

        [Header("UI Elements")]
        [SerializeField]
        private GameObject newMapPanel;

        [SerializeField]
        private GameObject cloudListButton;

        [SerializeField]
        private GameObject navSceneButton;

        [SerializeField]
        private UploadManager uploadManager;

        private float screenH;
        private float screenW;
        private float widthRatio;
        private float heightRatio;


        private void Start()
        {
            if (m_Sdk == null)
            {
                m_Sdk = ArwaySDK.Instance;
            }

        }

        void setupRatio()
        {
            ARCameraManager cameraManager = m_Sdk.cameraManager;
            if (cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {

                screenH = (float)Screen.height;
                screenW = (float)(screenH * (float)image.height / (float)image.width);//1800

                // ration should be same for both width and height >> scaling should be done in same proportion
                widthRatio = (float)screenW / (float)image.height;
                heightRatio = (float)screenH / (float)image.width;

                image.Dispose();
            }
        }

        private void Update()
        {
            GetFrames();
        }


        private unsafe void GetFrames()
        {

            ARCameraManager cameraManager = m_Sdk.cameraManager;


            if (cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                //Debug.Log("Cloud ID >>>>>>>>>>>>>>> : " + cloud_id);

                var format = TextureFormat.RGB24;

                if (m_Texture == null || m_Texture.width != image.width || m_Texture.height != image.height)
                {
                    m_Texture = new Texture2D(image.width, image.height, format, false);
                }

                // Convert the image to format, flipping the image across the Y axis.
                // We can also get a sub rectangle, but we'll get the full image here.
                var conversionParams = new XRCpuImage.ConversionParams(image, format, XRCpuImage.Transformation.MirrorY);

                // Texture2D allows us write directly to the raw texture data
                // This allows us to do the conversion in-place without making any copies.
                var rawTextureData = m_Texture.GetRawTextureData<byte>();
                try
                {
                    image.Convert(conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);
                }
                finally
                {
                    // We must dispose of the XRCameraImage after we're finished
                    // with it to avoid leaking native resources.
                    image.Dispose();
                }

                // Apply the updated texture data to our texture
                m_Texture.Apply();

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
            setupRatio();
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

                if (threshold > 0.2f)
                {
#if UNITY_EDITOR || UNITY_ANDROID
                    updatedPoints.Add(pos);
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(pos);

                    Color color = m_Texture.GetPixel((int)(screenPos.y / heightRatio), (int)(screenPos.x / widthRatio));
                    updatedColors.Add(color);
#elif UNITY_IOS
                    // add alternate point values
                    if (currIndex % 2 == 0){
                        updatedPoints.Add(pos);
                        Vector3 screenPos = Camera.main.WorldToScreenPoint(pos);

                        Color color = m_Texture.GetPixel((int)(screenPos.y / heightRatio), (int)(screenPos.x / widthRatio));
                        updatedColors.Add(color);
                    }
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

            CreatePLYAsync();
        }



        private async Task CreatePLYAsync()
        {
            FileInfo file = new FileInfo(Application.persistentDataPath + "/map/");
            file.Directory.Create();
            Debug.Log("Creating map directory.");

            // read lines in the JSON file
            string plyFile = "map.ply";
            string plyPath = Path.Combine(Application.persistentDataPath + "/map/", plyFile);

            int totalPoints = updatedPoints.Count;
            int totalLine = totalPoints + 20;

            Debug.Log("************\t Total points:" + totalPoints + " \t***************");

            string[] lines = new string[totalLine];

            lines[0] = "ply";
            lines[1] = "format ascii 1.0";
            lines[2] = "comment author: ARWAY";
            lines[3] = "comment object: Point Cloud Map";
            lines[4] = "element vertex " + totalPoints;
            lines[5] = "property float x";
            lines[6] = "property float y";
            lines[7] = "property float z";
            lines[8] = "property uchar red";
            lines[9] = "property uchar green";
            lines[10] = "property uchar blue";
            lines[11] = "element face 0";
            lines[12] = "property list uchar int vertex_index";
            lines[13] = "element edge 0";
            lines[14] = "property int vertex1";
            lines[15] = "property int vertex2";
            lines[16] = "property uchar red";
            lines[17] = "property uchar green";
            lines[18] = "property uchar blue";
            lines[19] = "end_header";

            int i = 20;
            for (int counter = 0; counter < updatedPoints.Count; counter++)
            {
                lines[i] = updatedPoints[counter].x * -1f + " " + updatedPoints[counter].y + " " + updatedPoints[counter].z + " " + updatedColors[counter].r + " " + updatedColors[counter].g + " " + updatedColors[counter].b;
                i++;
            }

            Debug.Log("************\t WriteLinesToPLY \t ***************");

            await WriteLinesToPLY(plyPath, lines);
        }

        public Task WriteLinesToPLY(string plyPath, string[] lines)
        {
            try
            {
                Debug.Log("Path: " + plyPath + " points count: " + lines.Length);
                File.WriteAllLines(plyPath, lines);
                Debug.Log("************\t PLY file created.\t***************");

                // show new map panel and upload the pcd file
                updateUI(true);

                if (File.Exists(plyPath))
                {
                    newMapPanel.SetActive(true);
                }
            }
            catch (Exception e)
            {
                Debug.Log("Exception in writing file!!" + e.ToString());
            }

            return null;
        }

        private void updateUI(bool setActive)
        {
            cloudListButton.SetActive(setActive);
            navSceneButton.SetActive(setActive);
        }
    }
}