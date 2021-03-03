using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Arway
{
    public class ItemClickController : MonoBehaviour
    {
        private GameObject m_ArwaySDK;
        public GameObject m_MapList;

        public AssetImporter assetImporter;
        public MultiMapAssetImporter multiMapAssetImporter;
        public AzureAnchorLocalizer azureAnchorLocalizer;
        public AzureAssetImporter azureAssetImporter;


        void Start()
        {
            if (m_ArwaySDK == null)
            {
                m_ArwaySDK = GameObject.FindGameObjectWithTag("ArwaySDK");
            }

            if (GameObject.FindGameObjectWithTag("MapList") != null)
            {
                m_MapList = GameObject.FindGameObjectWithTag("MapList");
            }

            if (m_ArwaySDK.GetComponent<AssetImporter>())
            {
                assetImporter = m_ArwaySDK.GetComponent<AssetImporter>();
                assetImporter.enabled = false;
            }

            if (m_ArwaySDK.GetComponent<MultiMapAssetImporter>())
            {
                multiMapAssetImporter = m_ArwaySDK.GetComponent<MultiMapAssetImporter>();
            }

            if (m_ArwaySDK.GetComponent<AzureAnchorLocalizer>())
            {
                azureAnchorLocalizer = m_ArwaySDK.GetComponent<AzureAnchorLocalizer>();
            }

            if (m_ArwaySDK.GetComponent<AzureAssetImporter>())
            {
                azureAssetImporter = m_ArwaySDK.GetComponent<AzureAssetImporter>();
            }
        }

        public void GetMapId()
        {
            string map_id = gameObject.transform.name;
            string map_name = gameObject.transform.Find("name").GetComponent<Text>().text;

            StartCoroutine(UpdateMapDetails(map_id, map_name));
        }

        IEnumerator UpdateMapDetails(string id, string map_name)
        {
            Debug.Log("Selected Map Details: \n" + " mapId: " + id + ", MapName: " + map_name);

            PlayerPrefs.SetString("MAP_ID", id);
            PlayerPrefs.SetString("MAP_NAME", map_name);

            if (m_ArwaySDK.GetComponent<AssetImporter>())
            {
                assetImporter.enabled = true;
            }

            if (m_ArwaySDK.GetComponent<MultiMapAssetImporter>())
            {
                multiMapAssetImporter.enabled = true;
            }

            if (m_ArwaySDK.GetComponent<AzureAnchorLocalizer>())
            {
                azureAnchorLocalizer.enabled = true;
            }

            if (m_ArwaySDK.GetComponent<AzureAssetImporter>())
            {
                azureAssetImporter.enabled = true;
            }

            m_MapList.SetActive(false);

            yield return 0;
        }
    }
}