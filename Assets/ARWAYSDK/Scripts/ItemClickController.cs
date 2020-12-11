using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Arway
{

    public class ItemClickController : MonoBehaviour
    {
        public GameObject m_MapList;
        public AssetImporter assetImporter;
        public MultiMapAssetImporter multiMapAssetImporter;

        void Start()
        {
            if (GameObject.FindGameObjectWithTag("MapList") != null)
                m_MapList = GameObject.FindGameObjectWithTag("MapList");

            if (GameObject.FindGameObjectWithTag("ArwaySDK").GetComponent<AssetImporter>())
            {
                assetImporter = GameObject.FindGameObjectWithTag("ArwaySDK").GetComponent<AssetImporter>();
                assetImporter.enabled = false;
            }

            if (GameObject.FindGameObjectWithTag("ArwaySDK").GetComponent<MultiMapAssetImporter>())
                multiMapAssetImporter = GameObject.FindGameObjectWithTag("ArwaySDK").GetComponent<MultiMapAssetImporter>();

           
        }

        public void GetMapId()
        {
            string map_id = gameObject.transform.name;
            string map_name = gameObject.transform.Find("name").GetComponent<Text>().text;

            StartCoroutine(UpdateMapDetails(map_id, map_name));
        }

        IEnumerator UpdateMapDetails(string id, string map_name)
        {
            Debug.Log("Selected Map Details: \n" + " mapId: " + id + " MapName: " + map_name);

            PlayerPrefs.SetString("MAP_ID", id);
            PlayerPrefs.SetString("MAP_NAME", map_name);

            if (GameObject.FindGameObjectWithTag("ArwaySDK").GetComponent<AssetImporter>())
                assetImporter.enabled = true;

            if (GameObject.FindGameObjectWithTag("ArwaySDK").GetComponent<MultiMapAssetImporter>())
                multiMapAssetImporter.enabled = true;

            m_MapList.SetActive(false);

            yield return 0;
        }
    }
}