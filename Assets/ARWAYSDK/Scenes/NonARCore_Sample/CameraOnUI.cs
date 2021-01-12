using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Arway
{
    public class CameraOnUI : MonoBehaviour
    {
        [SerializeField]
        private RawImage rawImage;

        private float screenH;
        private float screenW;

        WebCamTexture tex;

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

        private int counts = 0;
        private int requestCount = 0;

        private Texture2D m_Texture;

        public List<int> cloudMaps = new List<int>();

        string sessionCookieString = "";


        void Start()
        {

            renderCameraFeed();

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

        }

        private void renderCameraFeed()
        {
            screenH = Screen.height;
            screenW = Screen.width;

            Debug.Log("H: " + screenH + " W: " + screenW);

            WebCamDevice[] devices = WebCamTexture.devices;
            tex = new WebCamTexture(devices[0].name);

            rawImage.texture = tex;

            Debug.Log("H1: " + tex.requestedHeight + " W1: " + tex.requestedWidth);


            rawImage.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, screenH);
            rawImage.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, screenW);

            tex.Play();
        }

        public unsafe void RequestLocalization()
        {

            LocalizationRequest lr = new LocalizationRequest();
            lr.cloud_Ids = cloudMaps;
            lr.width = 640;
            lr.height = 480;
            lr.channel = 3;
            lr.Camera_fx = 482.33990478515627f;
            lr.Camera_fy = 482.3245544433594f;
            lr.Camera_px = 322.75787353515627f;
            lr.Camera_py = 237.1666717529297f;
            lr.version = m_Sdk.arwaysdkversion;

            m_Texture = Convert_WebCamTexture_To_Texture2d(tex);
            byte[] _bytesjpg = m_Texture.EncodeToJPG();

            lr.image = Convert.ToBase64String(_bytesjpg);
            lr.timestamp = 0.1;

            //show requeset counts..

            loc_attempts_txt.GetComponent<TMP_Text>().enabled = true;

            string output = JsonUtility.ToJson(lr);
            StartCoroutine(sendCameraImages(output));

        }

        IEnumerator sendCameraImages(string rawdata)
        {
            Debug.Log("RawString: " + rawdata);

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
                            if (sessionCookieString.Length == 0)
                            {
                                sessionCookieString = result;
                                PlayerPrefs.SetString("COOKIE", sessionCookieString);
                            }
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
                        Debug.Log("Calling POSE Setter...");
                        poseSetterGO.GetComponent<PoseSetter>().poseHandler(localization);

                        if (vibrateOnLocalize)
                            Handheld.Vibrate();
                    }

                    loc_attempts_txt.text = "Localization attempts:  " + counts + " / " + requestCount;
                }
            }
        }


        public Texture2D Convert_WebCamTexture_To_Texture2d(WebCamTexture _webCamTexture)
        {
            Texture2D _texture2D = new Texture2D(_webCamTexture.width, _webCamTexture.height);
            _texture2D.SetPixels32(_webCamTexture.GetPixels32());

            return _texture2D;
        }




        /// <summary>
        /// Handles the cloud list selection.
        /// </summary>
        /// <param name="val">Value.</param>
        public void HandleCloudListSelection(int val)
        {
            if (val > 0)
            {
                counts = 0;
                requestCount = 0;
                loc_attempts_txt.text = "Localization attempts:  " + counts + " / " + requestCount;

                string dataString = cloudDropdown.options[val].text;
                var dataArray = dataString.Trim().Split(' ');
                string cloud_id = dataArray[dataArray.Length - 1];

                cloudMaps.Clear();

                cloudMaps.Add(int.Parse(cloud_id));

                Debug.Log("Cloud ID: " + cloud_id);

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
                        string map_name = UnityWebRequest.UnEscapeURL(cloudListItem.cloudMapList[cloudListItem.cloudMapList.Length - (i + 1)].map_name);
                        cloudDropdown.options.Add(new TMP_Dropdown.OptionData(map_name + "  " + cloudListItem.cloudMapList[cloudListItem.cloudMapList.Length - (i + 1)].id));
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