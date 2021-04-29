using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Arway
{
    public class GetCloudMaps : MonoBehaviour
    {
        public ArwaySDK m_Sdk = null;

        [SerializeField]
        private Text loaderText;

        [SerializeField]
        private TMP_Dropdown cloudDropdown;

        public List<int> cloudMaps = new List<int>();

        private string jsonResult;

        void Start()
        {
            m_Sdk = ArwaySDK.Instance;

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
        }

        /// <summary>
        /// Handles the cloud list selection.
        /// </summary>
        /// <param name="val">Value.</param>
        public void HandleCloudListSelection(int val)
        {
            if (val > 0)
            {
                string dataString = cloudDropdown.options[val].text;
                var dataArray = dataString.Trim().Split(' ');
                string cloud_id = dataArray[dataArray.Length - 1];

                string anchorId = GetAnchorId(cloud_id);
                PlayerPrefs.SetString("CURR_ANCHOR_ID", anchorId);
                Debug.Log("Anchor ID = " + anchorId);

                cloudMaps.Clear();
                cloudMaps.Add(int.Parse(cloud_id));
            }
        }

        private string GetAnchorId(string cloud_id)
        {
            CloudListItem cloudListItem = JsonUtility.FromJson<CloudListItem>(jsonResult);

            for (int i = 0; i < cloudListItem.cloudMapList.Length; i++)
            {
                if (cloudListItem.cloudMapList[cloudListItem.cloudMapList.Length - (i + 1)].id == cloud_id)
                {
                    return cloudListItem.cloudMapList[cloudListItem.cloudMapList.Length - (i + 1)].AnchorId;
                }
            }

            return "";
        }

        /// <summary>
        /// Gets the cloud list.
        /// </summary>
        /// <returns>The cloud list.</returns>
        IEnumerator GetCloudList()
        {
            loaderText.text = "Loading Maps...";
            Debug.Log("\n\n\n\n**********");
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
                    jsonResult = System.Text.Encoding.UTF8.GetString(www.downloadHandler.data);

                    CloudListItem cloudListItem = JsonUtility.FromJson<CloudListItem>(jsonResult);

                    for (int i = 0; i < cloudListItem.cloudMapList.Length; i++)
                    {
                        if (cloudListItem.cloudMapList[cloudListItem.cloudMapList.Length - (i + 1)].AnchorId != "NULL")
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
    }
}