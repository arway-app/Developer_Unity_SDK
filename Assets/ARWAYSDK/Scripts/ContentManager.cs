/*===============================================================================
Copyright (C) 2020 ARWAY Ltd. All Rights Reserved.

This file is part of ARwayKit AR SDK

The ARwayKit SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of ARWAY Ltd.

===============================================================================*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Siccity.GLTFUtility;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Arway
{
    public class ContentManager : MonoBehaviour
    {
        public ArwaySDK m_Sdk = null;

        public GameObject wayPoint;
        public GameObject destination;
        public GameObject imagesPOI;
        public GameObject textPOI;

        private NavController navController;
        private Node[] Map = new Node[0];
        public List<GameObject> nodeList;

        private string filePath = "";

        [Header("Content Manager")]
        [SerializeField]
        private GameObject m_ARSpace;
        [SerializeField]
        private GameObject destinationDropdown;
        private TMP_Dropdown dropdown;

        [Header("Localization")]
        public string map_id;
        public string mapped_Cloud_Id;

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

        private int counts = 0;
        private int requestCount = 0;

        private Texture2D m_Texture;

        public List<int> cloudMaps;
        Utils utils = new Utils();
        string sessionCookieString = "";

        private bool isReadyToLocalize;

        public Camera ARCamera;

        [Header("Show/Hide Content")]
        [SerializeField]
        private bool showContentBeforeLocalization;

        private GameObject m_WaypointsAndDestinations, m_Texts, m_Images, m_3DModels;

        void Start()
        {
            sessionCookieString = PlayerPrefs.GetString("COOKIE");

            dropdown = destinationDropdown.GetComponent<TMP_Dropdown>();
            dropdown.options.Clear();
            dropdown.options.Add(new TMP_Dropdown.OptionData("Select Destination"));

            filePath = $"{Application.persistentDataPath}/Files/test.glb";

            navController = this.GetComponent<NavController>();

            cloudMaps = new List<int>();

            if (mapped_Cloud_Id.Length > 0)
                cloudMaps.Add(int.Parse(mapped_Cloud_Id));

            m_Sdk = ArwaySDK.Instance;

            if (m_Sdk.developerToken != null && m_Sdk.developerToken.Length > 0)
            {
                isReadyToLocalize = CheckMapIdDetails();

                if (isReadyToLocalize)
                    StartCoroutine(GetMapData(map_id));

                m_ARSpace.SetActive(showContentBeforeLocalization);
                destinationDropdown.SetActive(showContentBeforeLocalization);
            }
            else
            {
                Debug.Log("***********\tDeveloper Token not valid!\t***********");
                NotificationManager.Instance.GenerateWarning("Error: " + "Invalid Developer Token!!");
            }

            if (m_ARSpace == null)
            {
                m_ARSpace = new GameObject("ARSpace");

                if (m_ARSpace == null)
                {
                    Debug.Log("No AR Space found");
                }
            }

            // Create WaypointsAndDestinations Group with an ARAnchor
            m_WaypointsAndDestinations = new GameObject("Waypoints & Destinations");
            m_WaypointsAndDestinations.transform.parent = m_ARSpace.transform;

            // Create 3D Models Group with an ARAnchor
            m_3DModels = new GameObject("3D Models");
            m_3DModels.transform.parent = m_ARSpace.transform;

            // Create Images Group with an ARAnchor
            m_Images = new GameObject("Images");
            m_Images.transform.parent = m_ARSpace.transform;

            // Create Text Group with an ARAnchor
            m_Texts = new GameObject("Texts");
            m_Texts.transform.parent = m_ARSpace.transform;
        }

        public void AddARAnchors()
        {
            RemoveARAnchors();

            Debug.Log("Adding AR Anchors");

            m_WaypointsAndDestinations.AddComponent<ARAnchor>();
            m_3DModels.AddComponent<ARAnchor>();
            m_Images.AddComponent<ARAnchor>();
            m_Texts.AddComponent<ARAnchor>();
        }

        public void RemoveARAnchors()
        {
            Debug.Log("Removing AR Anchors");

            Destroy(m_WaypointsAndDestinations.GetComponent<ARAnchor>());
            Destroy(m_3DModels.GetComponent<ARAnchor>());
            Destroy(m_Images.GetComponent<ARAnchor>());
            Destroy(m_Texts.GetComponent<ARAnchor>());
        }

        private bool CheckMapIdDetails()
        {
            if (map_id.Length > 0)
            {
                if (mapped_Cloud_Id.Length == 0)
                {
                    NotificationManager.Instance.GenerateWarning("Error: " + "Mapped Cloud Id is null!!");
                    Debug.Log("***********\tMapped Cloud Id is null !!\t***********");
                    return false;
                }
                return true;
            }
            NotificationManager.Instance.GenerateWarning("Error: " + "Map Id is null!!");
            Debug.Log("***********\tMap Id is null !!\t***********");
            return false;
        }

        /// <summary>
        /// Gets the map data.
        /// </summary>
        /// <returns>The map data.</returns>
        /// <param name="map_id">Map identifier.</param>
        IEnumerator GetMapData(string map_id)
        {
            NotificationManager.Instance.GenerateNotification("Getting Map data..");

            using (UnityWebRequest www = UnityWebRequest.Get(m_Sdk.ContentServer + EndPoint.MAP_DATA + "index.php?map_id=" + map_id))
            {
                www.SetRequestHeader("dev-token", m_Sdk.developerToken);

                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.Log("****************\t" + www.error + "\t****************");
                    NotificationManager.Instance.GenerateWarning("Error: " + www.error);
                }
                else
                {
                    try
                    {
                        string jsonResult = System.Text.Encoding.UTF8.GetString(www.downloadHandler.data);
                        MapAssetData mapAssetData = JsonUtility.FromJson<MapAssetData>(jsonResult);

                        if (mapAssetData != null)
                        {
                            //---------------------   waypoints  -------------------------//

                            if (mapAssetData.Waypoints != null)
                            {   //   Debug.Log("Total waypoints: " + mapAssetData.Waypoints.Length);
                                for (int i = 0; i < mapAssetData.Waypoints.Length; i++)
                                {
                                    Vector3 pos = utils.getPose(mapAssetData.Waypoints[i].Position);
                                    Vector3 rot = utils.getRot(mapAssetData.Waypoints[i].Rotation);

                                    StartCoroutine(CreatePrefab(wayPoint, mapAssetData.Waypoints[i].name, pos, rot));
                                }
                            }

                            //---------------------   Destinations  -------------------------//

                            if (mapAssetData.Destinations != null)
                            {
                                Debug.Log("Total Destinations: " + mapAssetData.Destinations.Length);
                                for (int i = 0; i < mapAssetData.Destinations.Length; i++)
                                {
                                    Vector3 pos = utils.getPose(mapAssetData.Destinations[i].Position);
                                    Vector3 rot = utils.getRot(mapAssetData.Destinations[i].Rotation);

                                    StartCoroutine(CreatePrefab(destination, mapAssetData.Destinations[i].name, pos, rot));

                                    dropdown.options.Add(new TMP_Dropdown.OptionData(mapAssetData.Destinations[i].name));
                                }
                            }

                            //---------------------   GlbModels  -------------------------//

                            if (mapAssetData.GlbModels != null)
                            {
                                for (int i = 0; i < mapAssetData.GlbModels.Length; i++)
                                {
                                    Vector3 pos = utils.getPose(mapAssetData.GlbModels[i].Position);
                                    Vector3 rot = utils.getRot(mapAssetData.GlbModels[i].Rotation);
                                    Vector3 scale = utils.getScale(mapAssetData.GlbModels[i].Scale);

                                    CreateGlbPrefab(mapAssetData.GlbModels[i].name, pos, rot, scale, mapAssetData.GlbModels[i].link);
                                }
                            }

                            //---------------------   FloorImages  -------------------------//

                            if (mapAssetData.FloorImages != null)
                            {
                                for (int i = 0; i < mapAssetData.FloorImages.Length; i++)
                                {
                                    Vector3 pos = utils.getPose(mapAssetData.FloorImages[i].Position);
                                    Vector3 rot = utils.getRot(mapAssetData.FloorImages[i].Rotation);
                                    Vector3 scale = utils.getScale(mapAssetData.FloorImages[i].Scale);

                                    string imageUrl = mapAssetData.FloorImages[i].link;

                                    if (!string.IsNullOrEmpty(imageUrl))
                                        StartCoroutine(loadPoiImage(imageUrl, pos, rot, scale, mapAssetData.FloorImages[i].name));
                                    else
                                        Debug.Log("Image URL is empty!!");
                                }
                            }

                            //---------------------   Texts  -------------------------//

                            if (mapAssetData.Texts != null)
                            {
                                for (int i = 0; i < mapAssetData.Texts.Length; i++)
                                {
                                    Vector3 pos = utils.getPose(mapAssetData.Texts[i].Position);
                                    Vector3 rot = utils.getRot(mapAssetData.Texts[i].Rotation);
                                    Vector3 scale = utils.getScale(mapAssetData.Texts[i].Scale);

                                    string text = mapAssetData.Texts[i].name;

                                    if (text != null)
                                        loadPoiText(text, pos, rot, scale, text);
                                    else
                                        Debug.Log("Text is empty!!");
                                }
                            }
                        }

                        RunButton.SetActive(true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, this);
                    }
                }
            }
        }

        public unsafe void RequestLocalization()
        {
            if (!isReadyToLocalize)
            {
                NotificationManager.Instance.GenerateError("Error: " + "NOT READY TO LOCALIZE !!");
                Debug.Log("********  NOT READY TO LOCALIZE !!   *******");
                return;
            }

            RemoveARAnchors();

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
                    www.SetRequestHeader("Cookie", sessionCookieString);
                }

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

                        AddARAnchors();
                    }

                    loc_attempts_txt.text = "Localization attempts:  " + counts + " / " + requestCount;

                    // show ARSpace GameObject if counts > 0 
                    if (counts > 0)
                    {
                        m_ARSpace.SetActive(true);
                        destinationDropdown.SetActive(true);
                    }
                }
            }
        }

        // update drop down for destinations... 
        public void HangleDestinationSelection(int val)
        {
            if (val > 0)
            {
                Debug.Log("selection >> " + val + " " + dropdown.options[val].text);
            }
        }

        IEnumerator CreatePrefab(GameObject gameObject, string name, Vector3 pos, Vector3 rot)
        {
            var temp = Instantiate(gameObject);
            temp.name = name;
            temp.transform.parent = m_WaypointsAndDestinations.transform;
            temp.transform.localPosition = pos;
            temp.transform.localEulerAngles = rot;

            nodeList.Add(temp);

            Map = new Node[nodeList.Count];
            int i = 0;
            foreach (var node in nodeList)
            {
                Map[i] = node.GetComponent<Node>();
                i = i + 1;
            }
            navController.allnodes = Map;
            yield return name;
        }

        public IEnumerator loadPoiImage(String url, Vector3 pos, Vector3 rot, Vector3 scale, String name)
        {
            // Debug.Log("URL>>>" + url);

            var imgpoi = Instantiate(imagesPOI);
            imgpoi.transform.SetParent(m_Images.transform);
            imgpoi.transform.localPosition = pos;
            imgpoi.transform.localEulerAngles = rot;
            imgpoi.transform.localScale = scale;
            imgpoi.name = name;

            UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                Texture myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                imgpoi.GetComponentInChildren<RawImage>().texture = myTexture;
            }
        }

        public void loadPoiText(String textcon, Vector3 pos, Vector3 rot, Vector3 scale, String name)
        {
            var temp = Instantiate(textPOI);
            temp.transform.SetParent(m_Texts.transform);
            temp.transform.localPosition = pos;
            temp.transform.localEulerAngles = rot;
            temp.transform.localScale = scale;
            temp.name = name;
            temp.GetComponentInChildren<TMP_Text>().text = textcon;
        }

        // --------------- >>> START <<< GLB Model Loading ---------------//
        private void CreateGlbPrefab(string glb_name, Vector3 pos, Vector3 rot, Vector3 scale, string url)
        {
            string path = GetFilePath(url);
            //Debug.Log("GLB_URL >> " + url+"\n path>> "+path);
            if (File.Exists(path))
            {
                Debug.Log("Found file locally, loading...");
                LoadModel(path, glb_name, pos, rot, scale);
                return;
            }

            StartCoroutine(GetFileRequest(url, (UnityWebRequest req) =>
            {
                if (req.isNetworkError || req.isHttpError)
                {
                    // Log any errors that may happen
                    Debug.Log($"{req.error} : {req.downloadHandler.text}");
                }
                else
                {
                    // Save the model into a new wrapper
                    LoadModel(path, glb_name, pos, rot, scale);
                }
            }));
        }

        string GetFilePath(string url)
        {
            string[] pieces = url.Split('/');
            string filename = pieces[pieces.Length - 1];

            return $"{filePath}{filename}";
        }

        void LoadModel(string path, string glb_name, Vector3 pos, Vector3 rot, Vector3 scale)
        {
            GameObject model = Importer.LoadFromFile(path);

            model.name = glb_name;
            model.transform.parent = m_3DModels.transform;
            model.transform.localPosition = pos;
            model.transform.localEulerAngles = rot;
            model.transform.localScale = scale;
        }

        IEnumerator GetFileRequest(string url, Action<UnityWebRequest> callback)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.downloadHandler = new DownloadHandlerFile(GetFilePath(url));
                yield return req.SendWebRequest();
                callback(req);
            }
        }
        // --------------- >>> END <<<  GLB Model Loading ---------------//

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
