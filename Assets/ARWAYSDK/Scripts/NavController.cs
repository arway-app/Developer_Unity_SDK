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

        void Start()
        {
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

        public void HangleDestinationSelection(int val)
        {
            //Debug.Log("selection >> " + val + " " + dropdown.options[val].text);
            dropdown.Hide();

            if (val > 0)
            {
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

            // Debug.Log(closestNode);
            //Debug.Log(target);

            path = this.gameObject.GetComponent<AStar>().FindPath(closestNode, target, allnodes);

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
    }
}