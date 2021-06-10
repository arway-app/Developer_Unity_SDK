/*===============================================================================
Copyright (C) 2020 ARWAY Ltd. All Rights Reserved.

This file is part of ARwayKit AR SDK

The ARwayKit SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of ARWAY Ltd.

===============================================================================*/
using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

namespace Arway
{
    public class UploadManager : MonoBehaviour
    {
        [SerializeField]
        private ArwaySDK m_Sdk = null;

        [SerializeField]
        private UIManager uiManager;

        [SerializeField]
        private GameObject newMapPanel;

        [SerializeField]
        private TMP_InputField mapNameText;

        [Header("UI Components")]
        [SerializeField]
        private GameObject mLoader;

        [SerializeField]
        private GameObject moveDeviceAnim;

        [SerializeField]
        private Text loaderText;

        string plyName = "map.ply";
        string plyPath;

        private string devToken = "";
        private string uploadURL = "";

        private string m_longitude = "0.0", m_latitude = "0.0", m_altitude = "0.0";

        //-------------------------------------------------------------------------------------

        protected List<Task> m_Jobs = new List<Task>();
        private int m_JobLock = 0;

        public static HttpClient mapperClient;

        void Awake()
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Automatic;
            mapperClient = new HttpClient(handler);
            mapperClient.DefaultRequestHeaders.ExpectContinue = false;
        }
        //-------------------------------------------------------------------------------------

        // Start is called before the first frame update
        void Start()
        {
            moveDeviceAnim.SetActive(false);

            GetMapCoordinates();

            plyPath = Path.Combine(Application.persistentDataPath + "/map/", plyName);

            m_Sdk = ArwaySDK.Instance;

            //deleteMapURL = m_Sdk.ContentServer + EndPoint.DELETE_CLOUD_MAP;
            uploadURL = m_Sdk.ContentServer + EndPoint.MAP_UPLOAD;
            devToken = m_Sdk.developerToken;

            if (devToken != null)
                mapperClient.DefaultRequestHeaders.Add("dev-token", devToken);
            else
                NotificationManager.Instance.GenerateWarning("Please Enter Your Developer Token!!");
        }

        public void GetMapCoordinates()
        {
            // Get the current location of the device
            StartCoroutine(GetMapLocation());
        }

        public void CancelMapUpload()
        {
            // Reload Mapping Scene
            Debug.Log("Re-Load Mapping Scene.");
            StartCoroutine(ReloadCurrentScene());
        }

        public void uploadMapData()
        {
            if (mapNameText.text.Length > 0)
            {
                //StartCoroutine(uploadMapData(mapNameText.text));

                loaderText.text = "Getting ANCHOR_ID...";
                mLoader.SetActive(true);
                moveDeviceAnim.SetActive(false);

                StartCoroutine(checkForAnchorId(mapNameText.text));
            }
            else
            {
                NotificationManager.Instance.GenerateWarning("Map name required!!");
            }
        }

        int attempts = 0;
        int attemptLimit = 10;

        IEnumerator checkForAnchorId(String map_name)
        {
            yield return new WaitForSeconds(1f);

            string anchor_id = CreateAnchor.getCurrentAnchorId();
            Debug.Log("anchor_id  " + anchor_id);
            attempts++;

            if (attempts < attemptLimit)
            {
                if (anchor_id == "")
                {
                    mLoader.SetActive(false);
                    StartCoroutine(checkForAnchorId(map_name));
                    Debug.Log("Anchor Id is null!!");
                }
                else
                {
                    Debug.Log("Anchor Id exist.");
                    //StartCoroutine(uploadMapData(map_name, anchor_id));

                    ReadyToUploadMap(map_name, anchor_id);

                    attempts = 0;
                }
            }
            else
            {
                mLoader.SetActive(false);

                Debug.Log("************\tError in getting Anchor ID !!!!!!!! \t***************");
                NotificationManager.Instance.GenerateError("Error in getting Anchor ID. Try agin..");

                attempts = 0;
            }
        }


        //-------------------------------------------------------------------------------------
        private void Update()
        {
            JobsUpdate();
        }

        public void JobsUpdate()
        {
            if (m_JobLock == 1)
                return;
            if (m_Jobs.Count > 0)
            {
                m_JobLock = 1;
                RunJob(m_Jobs[0]);
            }
        }

        private async void RunJob(Task t)
        {
            await t;
            if (m_Jobs.Count > 0)
                m_Jobs.RemoveAt(0);
            m_JobLock = 0;
        }

        //-------------------------------------------------------------------------------------


        public async void ReadyToUploadMap(string map_name, string anchor_id)
        {
            await UploadMapDataAsync(map_name, anchor_id);
        }

        protected async Task UploadMapDataAsync(string map_name, string anchor_id)
        {
            await Task.Delay(250);

            if (!String.IsNullOrEmpty(anchor_id))
            {
                newMapPanel.SetActive(false);
                mLoader.SetActive(true);
                moveDeviceAnim.SetActive(false);

                if (File.Exists(plyPath))
                {
                    loaderText.text = "Uploading Map...";

                    float uploadStartTime = Time.realtimeSinceStartup;
                    //-------------------------------------------------------------------------------------
                    JobMapUploadAsync mapUploadJob = new JobMapUploadAsync
                    {
                        mapName = map_name,
                        devToken = devToken,

                        latitude = m_latitude,
                        longitude = m_longitude,
                        altitude = m_altitude,

                        plyPath = plyPath,
                        version = ArwaySDK.sdkVersion,
                        anchorId = anchor_id
                    };
                    //-------------------------------------------------------------------------------------
                    mapUploadJob.OnStart += () =>
                    {
                        Debug.Log("mapUpload  >>>> OnStart ");

                        uiManager.SetProgress(0);
                        uiManager.ShowProgressBar();
                    };
                    //-------------------------------------------------------------------------------------
                    mapUploadJob.OnResult += (string result) =>
                    {
                        float eta = Time.realtimeSinceStartup - uploadStartTime;

                        Debug.Log(string.Format("Map Data uploaded successfully in {0} seconds", eta + " response: " + result));

                        if (uiManager != null)
                            uiManager.HideProgressBar();
                    };
                    //-------------------------------------------------------------------------------------
                    mapUploadJob.Progress.ProgressChanged += (s, progress) =>
                    {
                        int value = (int)(100f * progress);
                        uiManager.SetProgress(value);
                        //Debug.Log("Upload Progress: " + value);
                        if (value >= 100)
                        {
                            Debug.Log("Upload Progress: " + value);

                            NotificationManager.Instance.GenerateSuccess("Upload Done.");
                            mLoader.SetActive(false);
                            uiManager.HideProgressBar();

                            PlayerPrefs.SetString("CURR_MAP_NAME", map_name);

                            // Delete map files once upload done.. 
                            StartCoroutine(DeleteMapFile(plyPath));

                            mapNameText.text = "";

                            // Reload Mapping Scene
                            Debug.Log("Re-Load Mapping Scene.");
                            StartCoroutine(ReloadCurrentScene());
                        }
                    };
                    //-------------------------------------------------------------------------------------
                    mapUploadJob.OnError += (e) =>
                    {
                        Debug.Log("Upload OnError!");

                        NotificationManager.Instance.GenerateError("Map Upload Error!!");

                        uiManager.HideProgressBar();
                        mLoader.SetActive(false);

                        // Reload Mapping Scene
                        Debug.Log("Re-Load Mapping Scene.");
                        StartCoroutine(ReloadCurrentScene());
                    };
                    //-------------------------------------------------------------------------------------
                    m_Jobs.Add(mapUploadJob.RunJobAsync());
                    //-------------------------------------------------------------------------------------

                }
                else
                {
                    Debug.Log("************\tNo Map files !!!!!!!! \t***************");
                    NotificationManager.Instance.GenerateWarning("Map files missing!!");
                }
            }
            else
            {
                Debug.Log("************\tNo Anchor ID !!!!!!!! \t***************");
                NotificationManager.Instance.GenerateError("NO Anchor Id, Try mapping bigger area with more features");
            }

        }


        //-------------------------------------------------------------------------------------
        IEnumerator DeleteMapFile(string plyPath)
        {
            DeleteFile(plyPath);
            yield return null;
        }
        //-------------------------------------------------------------------------------------
        void DeleteFile(string filePath)
        {
            // check if file exists
            if (!File.Exists(filePath))
            {
                Debug.Log("No " + filePath + " filePath exists");
            }
            else
            {
                Debug.Log(filePath + " filePath exists, deleting...");
                File.Delete(filePath);
            }

        }
        //-------------------------------------------------------------------------------------

        // check for internet connectivity
        private bool isNetworkAvailable()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
                return false;
            else
                return true;
        }
        //-------------------------------------------------------------------------------------
        IEnumerator GetMapLocation()
        {
#if PLATFORM_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                // Ask for permission or proceed without the functionality enabled.
                Permission.RequestUserPermission(Permission.FineLocation);
            }
#endif

            // First, check if user has location service enabled
            if (Input.location.isEnabledByUser)
            {
                // Start service before querying location
                Input.location.Start(0.001f, 0.001f);
            }
            else
            {
                NotificationManager.Instance.GenerateWarning("Location not found. Enable GPS.");
            }

            // Wait until service initializes
            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            // Service didn't initialize in 20 seconds
            if (maxWait < 1)
            {
                Debug.Log("Timed out");
                // yield break;
            }

            // Connection has failed
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                Debug.Log("Unable to determine device location");
                // yield break;
            }
            else
            {
                // Access granted and location value could be retrieved
                Debug.Log("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);

                // Save location data when mapping starts
                m_longitude = "" + Input.location.lastData.longitude;
                m_latitude = "" + Input.location.lastData.latitude;
                m_altitude = "" + Input.location.lastData.altitude;
            }

            if (m_longitude != "0" && m_latitude != "0")
            {
                // Stop service if there is no need to query location updates continuously
                Input.location.Stop();
            }
        }
        //-------------------------------------------------------------------------------------
        IEnumerator ReloadCurrentScene()
        {
            yield return new WaitForSeconds(1f);

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
            asyncLoad.allowSceneActivation = false;

            while (asyncLoad.progress < 0.9F)
            {
                yield return null;
            }

            Debug.Log(asyncLoad.progress);
            yield return new WaitForSeconds(0.8f);

            asyncLoad.allowSceneActivation = true;
        }
        //-------------------------------------------------------------------------------------
    }
}