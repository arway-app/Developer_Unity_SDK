/*===============================================================================
Copyright (C) 2020 ARWAY Ltd. All Rights Reserved.

This file is part of ARwayKit AR SDK

The ARwayKit SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of ARWAY Ltd.

===============================================================================*/
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections.LowLevel.Unsafe;
using System;
using TMPro;
using System.Collections.Generic;

namespace Arway
{
    public class MultiMapLocalizer : MonoBehaviour
    {
        public ArwaySDK m_Sdk = null;

        [SerializeField]
        private GameObject poseSetterGO;

        [SerializeField]
        private GameObject loaderPanel;

        [SerializeField]
        private Text loaderText;

        public bool vibrateOnLocalize;

        public TMP_Text loc_attempts_txt;
        public TMP_Text loc_map_txt;

        private int counts = 0;
        private int requestCount = 0;

        private Texture2D m_Texture;

        CloudListItem cloudListItem;

        public List<int> cloudMaps = new List<int>();

        string sessionCookieString = "";

        [Header("Content Manager")]
        [SerializeField]
        private GameObject ArSpace;
        [SerializeField]
        private GameObject destinationDropdown;

        [Header("Show/Hide Content")]
        [SerializeField]
        private bool showContentBeforeLocalization;

        //AR Camera in the scene
        public Camera ARCamera;

        /// <summary>
        /// Start this instance.
        /// </summary>
        void Start()
        {
            m_Sdk = ArwaySDK.Instance;
            sessionCookieString = PlayerPrefs.GetString("COOKIE");
            //Debug.Log("Cookies: " + sessionCookieString);

            if (string.IsNullOrEmpty(m_Sdk.developerToken))
            {
                NotificationManager.Instance.GenerateError("Invalid Developer Token!");
            }

            ArSpace.SetActive(showContentBeforeLocalization);
            destinationDropdown.SetActive(showContentBeforeLocalization);
        }

        /// <summary>
        /// Requests the localization.
        /// </summary>
        public unsafe void RequestLocalization()
        {
            Debug.Log(" >>>>>>   RequestLocalization  <<<<<<<" + cloudMaps.Count);
            m_Sdk.GetComponent<MultiMapAssetImporter>().RemoveARAnchors();

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

                LocalizationRequest lr = new LocalizationRequest();
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

                Vector3 camPos = ARCamera.transform.position;
                Quaternion camRot = ARCamera.transform.rotation;

                string loc_request_data = JsonUtility.ToJson(lr);

                StartCoroutine(sendCameraImages(loc_request_data, camPos, camRot));
            }
        }

        /// <summary>
        /// Sends the camera images.
        /// </summary>
        /// <returns>The camera images.</returns>
        /// <param name="rawdata">Rawdata.</param>
        /// <param name="cloud_id">Cloud identifier.</param>
        IEnumerator sendCameraImages(string rawdata, Vector3 camPos, Quaternion camRot)
        {
            loaderText.text = "Localizing...";
            loaderPanel.SetActive(true);

            using (UnityWebRequest www = UnityWebRequest.Put(m_Sdk.localizationServer + m_Sdk.developerToken + "/" + EndPoint.REQ_POSE, rawdata))
            {
                www.method = UnityWebRequest.kHttpVerbPOST;
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Accept", "application/json");

                if (sessionCookieString.Length > 0)
                {
                    //Debug.Log("Saved Cookie >> " + sessionCookieString);               
                    www.SetRequestHeader("Cookie", sessionCookieString);
                }

                yield return www.SendWebRequest();
                Debug.Log("***************");

                if (www.error != null)
                {
                    loaderPanel.SetActive(false);
                }
                else
                {
                    //Try to get a cookie and set in next API calls
                    if (www.GetResponseHeaders().ContainsKey("SET-COOKIE"))
                    {
                        if (www.GetResponseHeaders().TryGetValue("SET-COOKIE", out string result))
                        {
                            sessionCookieString = result;
                            PlayerPrefs.SetString("COOKIE", sessionCookieString);
                        }
                    }

                    loaderPanel.SetActive(false);

                    Debug.Log("All OK");
                    Debug.Log("Localize Response: " + www.downloadHandler.text);

                    requestCount++;

                    LocalizationResponse localization = JsonUtility.FromJson<LocalizationResponse>(www.downloadHandler.text);
                    Debug.Log(localization);

                    if (localization.poseAvailable == true)
                    {
                        counts += 1;
                        poseSetterGO.GetComponent<PoseSetter>().poseHandlerMultiMap(localization, camPos, camRot);

                        if (vibrateOnLocalize)
                            Handheld.Vibrate();

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
            }
        }
    }
}