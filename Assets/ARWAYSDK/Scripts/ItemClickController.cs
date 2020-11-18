﻿using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Arway
{

    public class ItemClickController : MonoBehaviour
    {

        //public string SceneName = "Content";

        public GameObject m_MapList;
        public AssetImporter assetImporter;

        void Start()
        {
            m_MapList = GameObject.FindGameObjectWithTag("MapList");
            assetImporter = GameObject.FindGameObjectWithTag("ArwaySDK").GetComponent<AssetImporter>();
            assetImporter.enabled = false;
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

            // SceneManager.LoadScene(SceneName);
            assetImporter.enabled = true;

            m_MapList.SetActive(false);

            yield return 0;
        }
    }
}