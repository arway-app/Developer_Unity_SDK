using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Net.Http;
using System.Threading.Tasks;

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
        private Text loaderText;

        string pcdName = "map.pcd";
        string pcdPath;

        private string devToken = "";
        private string uploadURL = "";

        private string m_longitude = "0.0", m_latitude = "0.0", m_altitude = "0.0";

        // Start is called before the first frame update
        void Start()
        {
            GetMapCoordinates();

            pcdPath = Path.Combine(Application.persistentDataPath + "/map/", pcdName);

            m_Sdk = ArwaySDK.Instance;

            //deleteMapURL = m_Sdk.ContentServer + EndPoint.DELETE_CLOUD_MAP;
            uploadURL = m_Sdk.ContentServer + EndPoint.MAP_UPLOAD;
            devToken = m_Sdk.developerToken;
        }

        public void GetMapCoordinates()
        {
            // Get the current location of the device
            StartCoroutine(GetMapLocation());
        }

        public void uploadMapData()
        {
            if (mapNameText.text.Length > 0)
            {
                //StartCoroutine(uploadMapData(mapNameText.text));

                loaderText.text = "Getting ANCHOR_ID...";
                mLoader.SetActive(true);

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

        public async void ReadyToUploadMap(string map_name, string anchor_id)
        {
            await UploadMapDataAsync(map_name, anchor_id);
        }

        /// <summary>
        /// UploadMapDataAsync
        /// </summary>
        /// <param name="map_name"></param>
        /// <param name="anchor_id"></param>
        /// <returns></returns>
        public async Task UploadMapDataAsync(string map_name, string anchor_id)
        {

            if (!String.IsNullOrEmpty(anchor_id))
            {
                newMapPanel.SetActive(false);
                loaderText.text = "Loading...";
                mLoader.SetActive(true);

                if (File.Exists(pcdPath))
                {
                    loaderText.text = "Uploading Map...";

                    HttpClient client = new HttpClient();

                    // we need to send a request with multipart/form-data
                    var multiForm = new MultipartFormDataContent();

                    // add API method params
                    multiForm.Add(new StringContent(mapNameText.text), "map_name");
                    multiForm.Add(new StringContent(m_latitude), "Latitude");
                    multiForm.Add(new StringContent(m_longitude), "Longitude");
                    multiForm.Add(new StringContent(m_altitude), "Altitude");

                    multiForm.Add(new StringContent(anchor_id), "anchor_id");

                    FileStream pcd = File.OpenRead(pcdPath);
                    multiForm.Add(new StreamContent(pcd), "pcd", Path.GetFileName(pcdPath));

                    client.DefaultRequestHeaders.Add("dev-token", devToken);

                    uiManager.SetProgress(0);
                    uiManager.ShowProgressBar();

                    // send request to UPLOAD API
                    if (isNetworkAvailable())
                    {
                        var response = await client.PostAsync(uploadURL, multiForm);
                        Debug.Log("upload response : " + response);
                        uiManager.SetProgress(100);

                        PlayerPrefs.SetString("CURR_MAP_NAME", map_name);

                        NotificationManager.Instance.GenerateSuccess("Upload Done.");
                        mLoader.SetActive(false);

                        // Delete map files once upload done.. 
                        StartCoroutine(DeleteMapFile(pcdPath));

                    }
                    else
                    {
                        Debug.Log("upload status : No internet!! ");
                        NotificationManager.Instance.GenerateError("Upload Failed!!");
                        mLoader.SetActive(false);
                    }

                    uiManager.HideProgressBar();

                    // Reload Mapping Scene
                    Debug.Log("Re-Load Mapping Scene.");
                    StartCoroutine(ReloadCurrentScene());

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
        IEnumerator DeleteMapFile(string pcdPath)
        {
            DeleteFile(pcdPath);
            yield return null;
        }

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
    }
}