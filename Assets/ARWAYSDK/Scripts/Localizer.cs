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
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

namespace Arway
{
    public class Localizer : MonoBehaviour
    {
        public ArwaySDK m_Sdk = null;

        [SerializeField]
        private TMP_Dropdown cloudDropdown;

        [SerializeField]
        private GameObject poseSetterGO;

        [SerializeField]
        private GameObject RunButton;

        [SerializeField]
        private GameObject loaderPanel;

        [SerializeField]
        private Text loaderText;

        public bool vibrateOnLocalize;

        public TMP_Text loc_attempts_txt;

        [HideInInspector]
        public int counts = 0;
        private int requestCount = 0;

        private Texture2D m_Texture;

        public List<int> cloudMaps = new List<int>();

        string sessionCookieString = "";

        [Header("ARSpace GameObject")]
        [SerializeField]
        private GameObject ArSpace;

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

            if (m_Sdk.developerToken != null && m_Sdk.developerToken.Length > 0)
            {
                NotificationManager.Instance.GenerateNotification("Getting Cloud List..");
                StartCoroutine(GetCloudList());
            }
            else
            {
                NotificationManager.Instance.GenerateError("Invalid Developer Token!");
            }

            cloudDropdown = cloudDropdown.GetComponent<TMP_Dropdown>();
            cloudDropdown.options.Clear();
            cloudDropdown.options.Add(new TMP_Dropdown.OptionData("Select Cloud Map"));

            ArSpace.SetActive(showContentBeforeLocalization);
        }

        /// <summary>
        /// Requests the localization.
        /// </summary>
        public unsafe void RequestLocalization()
        {

            XRCameraIntrinsics intr;
            ARCameraManager cameraManager = m_Sdk.cameraManager;
            var cameraSubsystem = cameraManager.subsystem;

            if (cameraSubsystem != null && cameraSubsystem.TryGetIntrinsics(out intr) && cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                loaderText.text = "Localizing...";
                loaderPanel.SetActive(true);

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

                Vector3 camPos = ARCamera.transform.position;
                Quaternion camRot = ARCamera.transform.rotation;

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

                byte[] _bytesjpg = m_Texture.EncodeToJPG();
                lr.image = Convert.ToBase64String(_bytesjpg);
                lr.timestamp = image.timestamp;

                //show requeset counts..

                loc_attempts_txt.GetComponent<TMP_Text>().enabled = true;

                string output = JsonUtility.ToJson(lr);
                StartCoroutine(sendCameraImages(output, camPos, camRot));

            }

        }

        /// <summary>
        /// Sends the camera images.
        /// </summary>
        /// <returns>The camera images.</returns>
        /// <param name="rawdata">Rawdata.</param>
        IEnumerator sendCameraImages(string rawdata, Vector3 camPos, Quaternion camRot)
        {
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

                // Get and Set Cookies 
                www.GetResponseHeaders();

                yield return www.SendWebRequest();
                Debug.Log("***************");

                if (www.error != null)
                {
                    loaderPanel.SetActive(false);
                    Debug.Log("Error: " + www.error);
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
                    Debug.Log("Status Code: " + www.downloadHandler.text);

                    requestCount++;

                    LocalizationResponse localization = JsonUtility.FromJson<LocalizationResponse>(www.downloadHandler.text);
                    Debug.Log(localization);

                    if (localization.poseAvailable == true)
                    {
                        counts += 1;
                        poseSetterGO.GetComponent<PoseSetter>().poseHandler(localization, camPos, camRot);

                        if (vibrateOnLocalize)
                            Handheld.Vibrate();
                    }

                    loc_attempts_txt.text = "Localization attempts:  " + counts + " / " + requestCount;

                    // show ARSpace GameObject if counts > 0 
                    if (counts > 0)
                    {
                        ArSpace.SetActive(true);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the cloud list selection.
        /// </summary>
        /// <param name="val">Value.</param>
        public void HandleCloudListSelection(int val)
        {
            if (val > 0)
            {
                //remove ARSpace GO from scene if changing cloud map.
                //ArSpace.SetActive(false);

                counts = 0;
                requestCount = 0;
                loc_attempts_txt.text = "Localization attempts:  " + counts + " / " + requestCount;

                string dataString = cloudDropdown.options[val].text;
                var dataArray = dataString.Trim().Split(' ');
                string cloud_id = dataArray[dataArray.Length - 1];

                cloudMaps.Clear();

                cloudMaps.Add(int.Parse(cloud_id));

                RunButton.SetActive(true);
            }
        }


        /// <summary>
        /// Gets the cloud list.
        /// </summary>
        /// <returns>The cloud list.</returns>
        IEnumerator GetCloudList()
        {
            loaderText.text = "Loading Maps...";
            UnityWebRequest www = UnityWebRequest.Get(m_Sdk.ContentServer + EndPoint.CLOUD_LIST);
            www.SetRequestHeader("dev-token", m_Sdk.developerToken);
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log("****************\t" + www.error + "\t****************");
            }
            else
            {
                try
                {
                    string jsonResult = System.Text.Encoding.UTF8.GetString(www.downloadHandler.data);

                    CloudListItem cloudListItem = JsonUtility.FromJson<CloudListItem>(jsonResult);

                    for (int i = 0; i < cloudListItem.cloudMapList.Length; i++)
                    {
                        if (cloudListItem.cloudMapList[cloudListItem.cloudMapList.Length - (i + 1)].AnchorId == "NULL")
                        {
                            string map_name = UnityWebRequest.UnEscapeURL(cloudListItem.cloudMapList[cloudListItem.cloudMapList.Length - (i + 1)].map_name);
                            cloudDropdown.options.Add(new TMP_Dropdown.OptionData(map_name + "  " + cloudListItem.cloudMapList[cloudListItem.cloudMapList.Length - (i + 1)].id));
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
            }
        }

        /// <summary>
        /// Backs the button click.
        /// </summary>
        /// <param name="sceneName">Scene name.</param>
        public void BackButtonClick(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}