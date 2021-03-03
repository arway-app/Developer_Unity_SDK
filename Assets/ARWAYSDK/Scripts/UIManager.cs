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
    public class UIManager : MonoBehaviour
    {
        [SerializeField]
        private ArwaySDK m_Sdk = null;
      
        [SerializeField]
        private GameObject deletePrompt = null;

        private string deleteMapURL = "";
        private string cloudListURL = "";
        private string devToken = "";
        private string cloud_id = "";

        private List<GameObject> items = new List<GameObject>();

        [SerializeField]
        [Tooltip("Cloud Map List Holder.")]
        private GameObject listHolder;

        [SerializeField]
        private TMP_Text deleteMessage;

        [SerializeField]
        private TMP_Text created_maps;

        [Header("UI Components")]

        [SerializeField]
        private GameObject mapListPanel;

        [SerializeField]
        private GameObject mLoader;

        [SerializeField]
        private Text loaderText;

        [SerializeField]
        private HProgressBar m_ProgressBar = null;


        //-------------------------- UI Manager ---------------------------------

        void Start()
        {
            m_Sdk = ArwaySDK.Instance;

            deleteMapURL = m_Sdk.ContentServer + EndPoint.DELETE_CLOUD_MAP;

            devToken = m_Sdk.developerToken;

            cloudListURL = m_Sdk.ContentServer + EndPoint.CLOUD_LIST;

            if (m_Sdk == null)
                m_Sdk = GetComponentInParent<ArwaySDK>();
        }

        public void ToggleDeletePrompt(bool on, string id, string cloud_map_name)
        {
            cloud_id = id;
            deleteMessage.text = "Would you like to delete \"" + cloud_map_name + "\" Map ?";
            deletePrompt.SetActive(on);
        }
       
        public void DeteleMapCall()
        {
            loaderText.text = "Deleting map...";
            mLoader.SetActive(true);

            StartCoroutine(DeleteCoudMap(cloud_id));
        }

       
        IEnumerator DeleteCoudMap(string cloud_ID)
        {
            if (cloud_ID.Length > 0)
            {
                Debug.Log("ID >>> " + cloud_ID);

                WWWForm form = new WWWForm();
                form.AddField("cloud_id", cloud_ID);

                UnityWebRequest req = UnityWebRequest.Post(deleteMapURL, form);
                req.SetRequestHeader("dev-token", devToken);
                yield return req.SendWebRequest();

                if (req.isHttpError || req.isNetworkError)
                {
                    mLoader.SetActive(false);
                    Debug.Log(req.error);
                    NotificationManager.Instance.GenerateError("Map Delete Failed!!");
                    deletePrompt.SetActive(false);
                }
                else
                {
                    mLoader.SetActive(false);

                    Debug.Log("upload status : " + req.downloadHandler.text);
                    NotificationManager.Instance.GenerateSuccess("Map Deleted.");
                    deletePrompt.SetActive(false);

                    getCloudList();
                }
            }
            else
            {
                mLoader.SetActive(false);
                Debug.Log("************\tCloudID Null !!!!!!!! \t***************");
                NotificationManager.Instance.GenerateWarning("Cloud-Id Missing!!");
                deletePrompt.SetActive(false);
            }
        }


        public void getCloudList()
        {
            Debug.Log("getting cloud list..." + devToken);
            //CHECK YOUR INTERNET CONNECTION
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.Log("No Internet Connection ");
                NotificationManager.Instance.GenerateError("No Internet Connection!!");
            }
            else
            {
                if (devToken != "")
                {
                    mapListPanel.SetActive(true);

                    if (items.Count > 0)
                    {
                        DestroyItems();
                    }

                    loaderText.text = "Loading List...";
                    mLoader.SetActive(true);

                    StartCoroutine(GetMapList());
                }
               
            }
        }

        IEnumerator GetMapList()
        {
            using (UnityWebRequest www = UnityWebRequest.Get(cloudListURL))
            {
                www.SetRequestHeader("dev-token", devToken);
                yield return www.SendWebRequest();
                if (www.isNetworkError || www.isHttpError)
                {
                    mLoader.SetActive(false);
                    Debug.Log("****************\t" + www.error + "\t****************");
                }
                else
                {
                    try
                    {
                        mLoader.SetActive(false);
                        string jsonResult = System.Text.Encoding.UTF8.GetString(www.downloadHandler.data);
                        Debug.Log("JSON_OUTPUT:" + jsonResult);

                        CloudListItem cloudListItem = JsonUtility.FromJson<CloudListItem>(jsonResult);
                        int totalMaps = cloudListItem.cloudMapList.Length;

                        Debug.Log(">>>>   Total maps: " + totalMaps + "   <<<<");
                        created_maps.text = "Created (" + totalMaps + ")";


                        for (int i = 0; i < totalMaps; i++)
                        {

                            GameObject item = Instantiate(Resources.Load("prefabs/CloudItem")) as GameObject;
                            items.Add(item);
                            item.SetActive(true);
                            item.transform.SetParent(listHolder.transform, false);
                            item.transform.localScale = Vector3.one;

                            string map_name = UnityWebRequest.UnEscapeURL(cloudListItem.cloudMapList[totalMaps - (i + 1)].map_name);

                            item.transform.Find("MapName").GetComponent<TextMeshProUGUI>().text = map_name + " (" + cloudListItem.cloudMapList[totalMaps - (i + 1)].id + ")";
                            item.transform.Find("Updated").GetComponent<TextMeshProUGUI>().text = cloudListItem.cloudMapList[totalMaps - (i + 1)].uploaded;

                        }
                    }
                    catch (Exception e)
                    {
                        mLoader.SetActive(false);
                        Debug.LogException(e, this);
                    }
                }
            }
        }

        public void DestroyItems()
        {
            foreach (GameObject item in items)
            {
                Destroy(item);
            }
            items.Clear();
        }

        IEnumerator GoToScene(string targetScene)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);

            asyncLoad.allowSceneActivation = false;

            while (asyncLoad.progress < 0.9F)
            {
                yield return null;
            }

            Debug.Log(asyncLoad.progress);

            yield return new WaitForSeconds(0.5f);

            asyncLoad.allowSceneActivation = true;
        }


        private void InitProgressBar()
        {
            m_ProgressBar.minValue = 0;
            m_ProgressBar.maxValue = 100;
            m_ProgressBar.currentValue = 0;
        }

        public void ShowProgressBar()
        {
            m_ProgressBar.transform.GetComponent<Fader>().FadeIn();
        }

        public void HideProgressBar()
        {
            m_ProgressBar.transform.GetComponent<Fader>().FadeOut();
        }

        public void SetProgress(int value)
        {
            m_ProgressBar.currentValue = value;
        }

    }
}
