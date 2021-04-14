/*===============================================================================
Copyright (C) 2020 ARWAY Ltd. All Rights Reserved.

This file is part of ARwayKit AR SDK

The ARwayKit SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of ARWAY Ltd.

===============================================================================*/
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections.LowLevel.Unsafe;
using System;
using TMPro;
using System.Collections.Generic;
using System.Collections;

namespace Arway
{
    public class MultiLocalizationWS : MonoBehaviour
    {
        private WebSocket websocket;

        // Server IP address
        [SerializeField]
        private string hostIP;

        // Server port
        private int port = 5000;

        // Flag to use localhost
        [SerializeField]
        private bool useLocalhost = true;

        // Address used in code
        private string host => useLocalhost ? "localhost" : hostIP;
        // Final server address
        private string server;

        public ArwaySDK m_Sdk = null;

        [SerializeField]
        private GameObject poseSetterGO;

        public bool vibrateOnLocalize;

        public TMP_Text loc_attempts_txt;
        public TMP_Text loc_map_txt;

        private int counts = 0;
        private int requestCount = 0;
        private int sentrequestCount = 0;
        private Texture2D m_Texture;

        CloudListItem cloudListItem;

        public List<int> cloudMaps = new List<int>();

        //AR Camera in the scene
        public Camera ARCamera;

        [Header("UI")]
        [SerializeField]
        private GameObject loaderPanel;

        [SerializeField]
        private Text loaderText;

        [SerializeField]
        private GameObject connectionLoaderPanel;

        [Header("Animaiton")]
        [SerializeField]
        private GameObject findPlaneGO;

        [Header("Content Manager")]
        [SerializeField]
        private GameObject ArSpace;
        [SerializeField]
        private GameObject destinationDropdown;

        [Header("Show/Hide Content")]
        [SerializeField]
        private bool showContentBeforeLocalization;

        [Header("Localization Frequency")]
        [SerializeField]
        private int localizationFrequency;

        [Header("Maximum Request in one Session")]
        [SerializeField]
        private int maximumRequestinSession;

        private bool isFirstRequest = true;

        Queue<ArCameraOffset> ArCameraPoseQueue = new Queue<ArCameraOffset>();

        /// <summary>
        /// Unity method called on initialization
        /// </summary>
        private void Awake()
        {
            server = "ws://" + hostIP + "/req_pose";
        }

        public void Start()
        {
            Debug.Log("server: " + server);
            websocket = new WebSocket(server);

            m_Sdk = ArwaySDK.Instance;
            //Debug.Log("Cookies: " + sessionCookieString);

            if (string.IsNullOrEmpty(m_Sdk.developerToken))
            {
                NotificationManager.Instance.GenerateError("Invalid Developer Token!");
            }

            ArSpace.SetActive(showContentBeforeLocalization);
            destinationDropdown.SetActive(showContentBeforeLocalization);
            ConnectWS();
        }

        /// <summary>
        /// Checks the tracking.
        /// </summary>
        /// <returns>The tracking.</returns>
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

        public async void ConnectWS()
        {
            websocket.OnOpen += () =>
            {
                Debug.Log("Connection open!");
                Invoke("RequestLocalization", 1f);

                if (isFirstRequest)
                {
                    connectionLoaderPanel.SetActive(true);
                }
            };

            websocket.OnError += (e) =>
            {
                Debug.Log("Error! " + e);
                //hide loader panel..
                loaderPanel.SetActive(false);

                if (isFirstRequest)
                    connectionLoaderPanel.SetActive(false);
            };

            websocket.OnClose += (e) =>
            {
                Debug.Log("Connection closed!");
                //hide loader panel..
                loaderPanel.SetActive(false);

                if (isFirstRequest)
                    connectionLoaderPanel.SetActive(false);
            };

            websocket.OnMessage += (bytes) =>
            {
                Debug.Log("OnMessage!");

                Vector3 camPos;
                Quaternion camRot;

                if (isFirstRequest)
                {
                    connectionLoaderPanel.SetActive(false);
                    isFirstRequest = false;
                    InvokeRepeating("RequestLocalization", 1f, localizationFrequency);
                    StartCoroutine(showInstructions());
                }

                if (ArCameraPoseQueue.Count > 0)
                {
                    ArCameraOffset arCameraOffset = new ArCameraOffset();
                    arCameraOffset = ArCameraPoseQueue.Dequeue();

                    camPos = arCameraOffset.position;
                    camRot = arCameraOffset.rotation;

                    //hide loader panel..
                    loaderPanel.SetActive(false);

                    // getting the message as a string
                    var Response = System.Text.Encoding.UTF8.GetString(bytes);

                    requestCount++;

                    LocalizationResponse localization = JsonUtility.FromJson<LocalizationResponse>(Response);
                    Debug.Log(localization);

                    if (localization.poseAvailable == true)
                    {
                        counts += 1;
                        poseSetterGO.GetComponent<PoseSetter>().poseHandlerMultiMap(localization, camPos, camRot);

                        if (vibrateOnLocalize)
                            Handheld.Vibrate();
                        CancelInvoke("RequestLocalization");

                        m_Sdk.GetComponent<MultiMapAssetImporter>().AddARAnchors();
                    }

                    loc_attempts_txt.text = "Localization attempts:  " + counts + " / " + requestCount;

                    // show ARSpace GameObject if counts > 0 
                    if (counts > 0)
                    {
                        ArSpace.SetActive(true);
                        destinationDropdown.SetActive(true);
                    }
                }
            };

            // waiting for messages
            await websocket.Connect();
        }

        public unsafe void RequestLocalization()
        {
            if (sentrequestCount < maximumRequestinSession)
            {
                Debug.Log(" >>>>>>   RequestLocalization  <<<<<<< " + cloudMaps.Count);
                m_Sdk.GetComponent<MultiMapAssetImporter>().RemoveARAnchors();

                loaderText.text = "Localizing...";

                XRCameraIntrinsics intr;
                ARCameraManager cameraManager = m_Sdk.cameraManager;
                var cameraSubsystem = cameraManager.subsystem;

                if (cameraSubsystem != null && cameraSubsystem.TryGetIntrinsics(out intr) && cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
                {
                    //Debug.Log("Cloud ID >>>>>>>>>>>>>>> : " + cloud_id);

                    var format = TextureFormat.RGB24;

                    if (m_Texture == null || m_Texture.width != image.width || m_Texture.height != image.height)
                    {
                        m_Texture = new Texture2D(image.width, image.height, format, false);
                    }

                    // Convert the image to format, flipping the image across the Y axis.
                    // We can also get a sub rectangle, but we'll get the full image here.
                    var conversionParams = new XRCpuImage.ConversionParams(image, format, XRCpuImage.Transformation.MirrorX);

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

                    //show requeset counts..
                    loc_attempts_txt.GetComponent<TMP_Text>().enabled = true;
                    loc_map_txt.GetComponent<TMP_Text>().enabled = true;

                    byte[] _bytesjpg = m_Texture.EncodeToJPG();

                    loc_map_txt.text = "";

                    Debug.Log("TotalMaps: " + cloudMaps.Count);

                    LocalizationRequestwithToken lr = new LocalizationRequestwithToken();
                    lr.developer_token = m_Sdk.developerToken;
                    lr.cloud_Ids = cloudMaps;
                    lr.width = image.width;
                    lr.height = image.height;
                    lr.channel = 3;
                    lr.Camera_fx = intr.focalLength.x;
                    lr.Camera_fy = intr.focalLength.y;
                    lr.Camera_px = intr.principalPoint.x;
                    lr.Camera_py = intr.principalPoint.y;
                    lr.version = m_Sdk.arwaysdkversion;
                    lr.image = Convert.ToBase64String(_bytesjpg);
                    lr.timestamp = image.timestamp;

                    ArCameraOffset arCameraOffset = new ArCameraOffset
                    {
                        position = ARCamera.transform.position,
                        rotation = ARCamera.transform.rotation
                    };

                    ArCameraPoseQueue.Enqueue(arCameraOffset);

                    string loc_request_data = JsonUtility.ToJson(lr);

                    SendCameraImages(loc_request_data);
                    sentrequestCount++;
                }
            }
            else
            {
                CancelInvoke("RequestLocalization");
            }
        }

        public void SendCameraImages(string rawdata)
        {
            //Debug.Log(">>>>>>>  SendMessage  <<<<<<<" + rawdata);
            SendWebSocketMessage(rawdata);
        }

        void Update()
        {

#if !UNITY_WEBGL || UNITY_EDITOR
            if (websocket != null)
                websocket.DispatchMessageQueue();
#endif
        }

        async void SendWebSocketMessage(string rawData)
        {
            if (websocket.State == WSState.Open)
            {
                // show loader panel
                loaderPanel.SetActive(true);

                await websocket.SendText(rawData);
            }
        }

        public async void CloseWS()
        {
            if (websocket != null)
                await websocket.Close();
        }

        private async void OnApplicationQuit()
        {
            if (websocket != null)
                await websocket.Close();
        }

        async void OnDisable()
        {
            if (websocket != null)
                await websocket.Close();
        }

        IEnumerator showInstructions()
        {
            findPlaneGO.SetActive(true);
            //Wait for 6 seconds
            yield return new WaitForSeconds(6);
            findPlaneGO.SetActive(false);
        }
    }
}