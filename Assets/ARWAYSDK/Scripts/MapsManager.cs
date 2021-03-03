/*===============================================================================
Copyright (C) 2020 ARWAY Ltd. All Rights Reserved.

This file is part of ARwayKit AR SDK

The ARwayKit SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of ARWAY Ltd.

===============================================================================*/
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

namespace Arway
{
    public class MapsManager : MonoBehaviour
    {
        private GameObject ItemTemplate;
        public ArwaySDK m_Sdk = null;

        [SerializeField]
        [Tooltip("List Item Holder.")]
        private GameObject listHolder;

        [Header("Loader UI")]
        [SerializeField]
        private GameObject loaderPanel;

        [SerializeField]
        private Text loaderText;

        void Start()
        {
            m_Sdk = ArwaySDK.Instance;
            if (m_Sdk.developerToken != null && m_Sdk.developerToken.Length > 0)
            {
                Invoke("CallGetMapList", 1.0f);
            }
            else
            {
                Debug.Log("***********\tDeveloper Token not valid!\t***********");
                NotificationManager.Instance.GenerateError("Invalid Developer Token!!");
            }
        }

        void CallGetMapList()
        {
            NotificationManager.Instance.GenerateNotification("Getting Map List..");

            if (loaderPanel != null)
            {
                loaderText.text = "Loading Maps...";
                loaderPanel.SetActive(true);
            }
            StartCoroutine(GetMapList());
        }

        IEnumerator GetMapList()
        {
            using (UnityWebRequest www = UnityWebRequest.Get(m_Sdk.ContentServer + EndPoint.MAP_LIST))
            {
                www.SetRequestHeader("dev-token", m_Sdk.developerToken);

                yield return www.SendWebRequest();
                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.Log("****************\t" + www.error + "\t****************");
                    NotificationManager.Instance.GenerateWarning("Error: " + www.error);
                    if (loaderPanel != null)
                        loaderPanel.SetActive(false);
                }
                else
                {
                    if (loaderPanel != null)
                        loaderPanel.SetActive(false);

                    try
                    {
                        string jsonResult = System.Text.Encoding.UTF8.GetString(www.downloadHandler.data);

                        MapListItem mapListItem = JsonUtility.FromJson<MapListItem>(jsonResult);

                        Debug.Log("Total Maps: " + mapListItem.map_list.Length.ToString());

                        for (int i = 0; i < mapListItem.map_list.Length; i++)
                        {
                            ItemTemplate = Instantiate(Resources.Load("prefabs/MapItem")) as GameObject;

                            Transform mapItemTransform = ItemTemplate.transform;
                            mapItemTransform.SetParent(listHolder.transform);
                            mapItemTransform.localScale = Vector3.one;

                            mapItemTransform.name = mapListItem.map_list[i].map_id;

                            mapItemTransform.Find("name").GetComponent<Text>().text = mapListItem.map_list[i].map_name;

                            string addressString = UnityWebRequest.UnEscapeURL(mapListItem.map_list[i].map_address);
                            mapItemTransform.Find("address").GetComponent<Text>().text = addressString;

                            if (mapListItem.map_list[i].map_image != null && mapListItem.map_list[i].map_image.Length > 0)
                            {
                                StartCoroutine(StoreImageFromUrl(mapListItem.map_list[i].map_image, mapItemTransform.Find("image").GetComponent<Image>()));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, this);
                    }
                }
            }
        }

        IEnumerator StoreImageFromUrl(string mapImageUrl, Image img)
        {
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(mapImageUrl);
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                Sprite sprite = Sprite.Create(texture,
                   new Rect(0, 0, texture.width, texture.height), Vector2.zero);

                img.sprite = sprite;
            }
        }
    }
}