/*===============================================================================
Copyright (C) 2020 ARWAY Ltd. All Rights Reserved.

This file is part of ARwayKit AR SDK

The ARwayKit SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of ARWAY Ltd.

===============================================================================*/
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Arway
{

    public class NavController : MonoBehaviour
    {

        [Header("Visualization")]
        [SerializeField]
        private GameObject m_navigationPathPrefab = null;

        private GameObject m_navigationPathObject = null;
        private NavigationPath m_navigationPath = null;

        public Node[] allnodes;
        private Node target;
        public List<Node> path = new List<Node>();
        public Vector3[] positionpoint;
        List<Vector3> corners = new List<Vector3>();
        [Obsolete]

        public TMP_Dropdown dropdown;

        public bool showPath = true;

        public TMP_Text m_DistanceLeft, m_TimeLeft;

        private GameObject m_MainCamera;
        private bool destinationUpdated = false, isNavigating = false;
        private float distanceLeft = 0f;
        private float walkingSpeed = 1.0f;
        private Vector3 lastPos;

        void Start()
        {
            m_MainCamera = Camera.main.gameObject;
            m_DistanceLeft.gameObject.SetActive(false);
            m_TimeLeft.gameObject.SetActive(false);

            dropdown = dropdown.GetComponent<TMP_Dropdown>();

            if (m_navigationPathPrefab != null)
            {
                if (m_navigationPathObject == null)
                {
                    m_navigationPathObject = Instantiate(m_navigationPathPrefab);
                    m_navigationPathObject.SetActive(false);
                    m_navigationPath = m_navigationPathObject.GetComponent<NavigationPath>();
                }

                if (m_navigationPath == null)
                {
                    Debug.LogWarning("NavigationManager: NavigationPath component in Navigation path is missing.");
                    return;
                }
            }
        }

        public void HandleDestinationSelection(int val)
        {
            //Debug.Log("selection >> " + val + " " + dropdown.options[val].text);
            dropdown.Hide();

            if (val > 0)
            {
                destinationUpdated = true;
                PlayerPrefs.SetString("DEST_NAME", dropdown.options[val].text);
                InvokeRepeating("StartNavigation", 1.0f, 0.5f);
            }

        }

        public void StartNavigation()
        {
            string DestName = PlayerPrefs.GetString("DEST_NAME");
            corners.Clear();

            if (allnodes != null)
            {
                foreach (Node node in allnodes)
                {
                    node.gameObject.GetComponentInChildren<MeshRenderer>().enabled = false;
                }
            }

            if (DestName != null)
            {
                target = GameObject.Find(DestName).GetComponent<Node>();
                target.gameObject.GetComponentInChildren<MeshRenderer>().enabled = true;
            }

            Node closestNode = Node.currentclosetnode;

            foreach (Node node in allnodes)
            {
                node.FindNeighbors(1.5f);
            }

            //get path from A* algorithm

            path = this.gameObject.GetComponent<AStar>().FindPath(closestNode, target, allnodes);

            if (path != null)
            {
                //Debug.Log(path);
                foreach (Node obj in path)
                {
                    corners.Add(new Vector3(obj.gameObject.transform.position.x, obj.gameObject.transform.position.y, obj.gameObject.transform.position.z));

                    if (showPath)
                    {
                        m_navigationPath.GeneratePath(corners, Vector3.up);
                        m_navigationPath.pathWidth = 0.3f;
                        m_navigationPathObject.SetActive(true);
                    }
                }
            }
            else
            {
                Debug.Log("Waypoints missing for the selected Destination!!");
                //NotificationManager.Instance.GenerateWarning("Waypoints missing for the selected Destination!!");
            }
        }

        private void Update()
        {
            distanceLeft = 0f;

            if (isNavigating && path.Count >= 1)
            {
                m_DistanceLeft.gameObject.SetActive(true);
                m_TimeLeft.gameObject.SetActive(true);

                distanceLeft += Vector3.Distance(path[0].gameObject.transform.position, m_MainCamera.transform.position);

                if (path.Count >= 2)
                {
                    for (int currNode = 0; currNode < path.Count - 1; currNode += 1)
                    {
                        distanceLeft += Vector3.Distance(path[currNode].gameObject.transform.position, path[currNode + 1].gameObject.transform.position);
                    }
                }

                if (distanceLeft < 0.25f)
                {
                    m_DistanceLeft.text = "<u>Distance:</u> 0 m";
                    m_TimeLeft.text = "<u>ETA:</u> 0 sec";

                    isNavigating = false;
                }
                else
                {
                    m_DistanceLeft.text = "<u>Distance:</u> " + distanceLeft.ToString("F1") + " m";

                    var timeLeft = Mathf.RoundToInt(distanceLeft / walkingSpeed);

                    if (timeLeft >= 60)
                    {
                        m_TimeLeft.text = "<u>ETA:</u> " + timeLeft / 60 + " min " + timeLeft % 60 + " sec";
                    }
                    else
                    {
                        m_TimeLeft.text = "<u>ETA:</u> " + timeLeft % 60 + " sec";
                    }
                }
            }

            if (destinationUpdated)
            {
                isNavigating = true;
                destinationUpdated = false;

                StartCoroutine(CalculateWalkingSpeed());
            }
        }

        IEnumerator CalculateWalkingSpeed()
        {
            if (isNavigating && path.Count >= 1)
            {
                var currSpeed = Vector3.Distance(m_MainCamera.transform.position, lastPos);

                lastPos = m_MainCamera.transform.position;

                if (currSpeed < 0.5f)
                {
                    walkingSpeed = 0.5f;
                }
                else
                {
                    walkingSpeed = currSpeed;
                }
            }

            yield return new WaitForSeconds(1f);
            StartCoroutine(CalculateWalkingSpeed());
        }
    }
}