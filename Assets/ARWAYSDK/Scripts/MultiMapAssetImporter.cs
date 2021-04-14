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
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Arway
{
    public class MultiMapAssetImporter : MonoBehaviour
    {
        public ArwaySDK m_Sdk = null;
        private string map_id;
        public GameObject m_ARSpace;

        public GameObject wayPoint;
        public GameObject destination;

        public GameObject imagesPOI;
        public GameObject textPOI;

        private NavController navController;
        private Node[] Map = new Node[0];
        public List<GameObject> nodeList;

        private string filePath = "";

        public GameObject destinationDropdown;
        private TMP_Dropdown dropdown;

        private MultiLocalizationWS multiLocalizationWS;
        private MultiMapLocalizer multiMapLocalizer;
        private bool isWebsocketScene;

        public static Dictionary<int, CloudMapOffset> mapIdToOffset = new Dictionary<int, CloudMapOffset>();

        Utils utils = new Utils();
        private GameObject m_WaypointsAndDestinations, m_Texts, m_Images, m_3DModels;

        void Start()
        {
            dropdown = destinationDropdown.GetComponent<TMP_Dropdown>();
            dropdown.options.Clear();
            dropdown.options.Add(new TMP_Dropdown.OptionData("Select Destination"));

            destinationDropdown.SetActive(false);

            map_id = PlayerPrefs.GetString("MAP_ID");
            Debug.Log("MapID >> " + map_id);

            filePath = $"{Application.persistentDataPath}/Files/test.glb";

            navController = this.GetComponent<NavController>();

            if (map_id.Length > 0)
            {
                m_Sdk = ArwaySDK.Instance;

                if (m_Sdk.developerToken != null && m_Sdk.developerToken.Length > 0)
                {
                    StartCoroutine(GetMapData(map_id));
                }
                else
                {
                    Debug.Log("***********\tInvalid Developer Token!\t***********");
                    NotificationManager.Instance.GenerateError("Invalid Developer Token!!");
                }
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

            // check if its websocket scene
            if (m_Sdk.GetComponent<MultiLocalizationWS>() != null)
            {
                multiLocalizationWS = m_Sdk.GetComponent<MultiLocalizationWS>();
                isWebsocketScene = true;
            }

            if (m_Sdk.GetComponent<MultiMapLocalizer>() != null)
            {
                multiMapLocalizer = m_Sdk.GetComponent<MultiMapLocalizer>();
                isWebsocketScene = false;
            }
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

        /// <summary>
        /// Gets the map data.
        /// </summary>
        /// <returns>The map data.</returns>
        /// <param name="map_id">Map identifier.</param>
        IEnumerator GetMapData(string map_id)
        {
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

                        //---------------------   waypoints  -------------------------//

                        if (mapAssetData.Waypoints != null)
                        {
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

                            //if (mapAssetData.Destinations.Length > 0)
                            //{
                            //    destinationDropdown.SetActive(true);
                            //}

                            for (int i = 0; i < mapAssetData.Destinations.Length; i++)
                            {
                                Vector3 pos = utils.getPose(mapAssetData.Destinations[i].Position);
                                Vector3 rot = utils.getRot(mapAssetData.Destinations[i].Rotation);

                                StartCoroutine(CreatePrefab(destination, mapAssetData.Destinations[i].name, pos, rot));

                                dropdown.options.Add(new TMP_Dropdown.OptionData(mapAssetData.Destinations[i].name));
                            }
                        }

                        //---------------------   CloudMaps  -------------------------//

                        if (mapAssetData.CloudMaps != null)
                        {
                            for (int i = 0; i < mapAssetData.CloudMaps.Length; i++)
                            {
                                Debug.Log("Total Cloud_maps: " + mapAssetData.CloudMaps.Length + " _ " + mapAssetData.CloudMaps[i].pcd_id);

                                Vector3 pos = utils.getPose(mapAssetData.CloudMaps[i].Position);
                                Vector3 rot = utils.getRot(mapAssetData.CloudMaps[i].Rotation);
                                Vector3 scale = utils.getScale(mapAssetData.CloudMaps[i].Scale);

                                CloudMapOffset offset = new CloudMapOffset();
                                offset.position = pos;
                                offset.rotation = Quaternion.Euler(rot);

                                mapIdToOffset.Add(mapAssetData.CloudMaps[i].pcd_id, offset);

                                if (isWebsocketScene)
                                    multiLocalizationWS.cloudMaps.Add(mapAssetData.CloudMaps[i].pcd_id);
                                else
                                    multiMapLocalizer.cloudMaps.Add(mapAssetData.CloudMaps[i].pcd_id);
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
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, this);
                    }
                }
            }

            if (isWebsocketScene)
            {
                multiLocalizationWS.enabled = true;
            }
            else
                multiMapLocalizer.enabled = true;
        }

        // update drop down for destinations... 
        public void HandleDestinationSelection(int val)
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
