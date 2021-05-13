/*===============================================================================
Copyright (C) 2020 ARWAY Ltd. All Rights Reserved.

This file is part of ARwayKit AR SDK

The ARwayKit SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of ARWAY Ltd.

===============================================================================*/
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Arway
{
    public class ContentStorageManager : MonoBehaviour
    {
        [HideInInspector]
        public List<MovableContent> contentList = new List<MovableContent>();

        [SerializeField]
        private GameObject m_ContentPrefab = null;

        [SerializeField]
        private GameObject m_ARSpace;

        [SerializeField]
        private string m_Filename = "content.json";

        private Savefile m_Savefile;

        private List<Vector3> m_Positions = new List<Vector3>();

        [SerializeField]
        private LocalizeAnchor localizeAnchor;

        [System.Serializable]
        public struct Savefile
        {
            public List<Vector3> positions;
        }

        public static ContentStorageManager Instance
        {
            get
            {
#if UNITY_EDITOR
                if (instance == null && !Application.isPlaying)
                {
                    instance = FindObjectOfType<ContentStorageManager>();
                }
#endif
                if (instance == null)
                {
                    Debug.LogError("No ContentStorageManager instance found.");
                }
                return instance;
            }
        }

        private static ContentStorageManager instance = null;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            if (instance != this)
            {
                Debug.LogError("There must be only one ContentStorageManager object in a scene.");
                DestroyImmediate(this);
                return;
            }
        }

        private void Start()
        {
            contentList.Clear();
            LoadContents();
        }


        /// <summary>
        /// Adds the content.
        /// </summary>
        public void AddContent()
        {
           
                //check if atleast localize once.
                if (localizeAnchor.isLocalized)
                {
                    Transform cameraTransform = Camera.main.transform;
                    GameObject go = Instantiate(m_ContentPrefab, cameraTransform.position + cameraTransform.forward, Quaternion.identity, m_ARSpace.transform);
                }
                else
                {
                    NotificationManager.Instance.GenerateError("Scene not localized!! Failed to Add.");
                }
            
           
        }


        /// <summary>
        /// Deletes the content of the all.
        /// </summary>
        public void DeleteAllContent()
        {
            List<MovableContent> copy = new List<MovableContent>();

            foreach (MovableContent content in contentList)
            {
                copy.Add(content);
            }

            foreach(MovableContent content in copy)
            {
                content.RemoveContent();
            }


            NotificationManager.Instance.GenerateError("All Content Deleted!");
        }

        /// <summary>
        /// Saves the contents.
        /// </summary>
        public void SaveContents()
        {
            m_Positions.Clear();
            foreach (MovableContent content in contentList)
            {
                m_Positions.Add(content.transform.localPosition);
            }
            m_Savefile.positions = m_Positions;

            string jsonstring = JsonUtility.ToJson(m_Savefile, true);
            string dataPath = Path.Combine(Application.persistentDataPath, m_Filename);
            File.WriteAllText(dataPath, jsonstring);
        }

        /// <summary>
        /// Loads the contents.
        /// </summary>
        public void LoadContents()
        {
            string dataPath = Path.Combine(Application.persistentDataPath, m_Filename);

            try
            {
                if (File.Exists(dataPath))
                {
                    Debug.Log(" ***************  content.json file exists   ***************  ");

                    Savefile loadFile = JsonUtility.FromJson<Savefile>(File.ReadAllText(dataPath));
                    foreach (Vector3 pos in loadFile.positions)
                    {
                        GameObject go = Instantiate(m_ContentPrefab, m_ARSpace.transform);
                        go.transform.localPosition = pos;
                    }
                }
                else
                {
                    Debug.Log(" ***************  content.json file doesn't exists !!!!   ***************  ");
                }
            }
            catch (FileNotFoundException e)
            {
                Debug.LogError(dataPath + " not found\nNo objects loaded: " + e.Message);
            }
        }
    }
}