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
        List<Vector3> corners = new List<Vector3>();
        [Obsolete]

        public TMP_Dropdown dropdown;

        public bool showPath = true;

        public TMP_Text m_DistanceLeft, m_TimeLeft, m_Direction;
        public GameObject m_LeftDirectionArrow, m_RightDirectionArrow;

        private GameObject m_MainCamera;
        private GameObject navigationPrefab, leftArrow, rightArrow;

        private bool destinationUpdated = false, isNavigating = false;
        private float distanceLeft = 0f;
        private float walkingSpeed = 1.0f;
        private Vector3 lastPos;

        void Start()
        {
            m_MainCamera = Camera.main.gameObject;

            m_DistanceLeft.gameObject.SetActive(false);
            m_TimeLeft.gameObject.SetActive(false);

            navigationPrefab = m_DistanceLeft.transform.parent.gameObject;
            navigationPrefab.SetActive(false);

            leftArrow = m_Direction.transform.GetChild(0).gameObject;
            rightArrow = m_Direction.transform.GetChild(1).gameObject;

            m_LeftDirectionArrow.SetActive(false);
            m_RightDirectionArrow.SetActive(false);

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

                navigationPrefab.SetActive(true);

                distanceLeft += Vector3.Distance(path[0].gameObject.transform.position, m_MainCamera.transform.position);

                if (path.Count >= 2)
                {
                    for (int currNode = 0; currNode < path.Count - 1; currNode += 1)
                    {
                        distanceLeft += Vector3.Distance(path[currNode].gameObject.transform.position, path[currNode + 1].gameObject.transform.position);
                    }
                }

                if (path.Count >= 4)
                {
                    StartCoroutine(CheckForTurns(path[0].gameObject, path[1].gameObject, path[2].gameObject));
                }
                else
                {
                    m_Direction.text = "";

                    leftArrow.SetActive(false);
                    rightArrow.SetActive(false);

                    m_LeftDirectionArrow.SetActive(false);
                    m_RightDirectionArrow.SetActive(false);
                }

                if (distanceLeft < 0.25f)
                {
                    m_DistanceLeft.text = "<size=64>0</size> m";
                    m_TimeLeft.text = "<size=64>0</size> sec";
                    m_Direction.text = "";

                    isNavigating = false;
                }
                else
                {
                    m_DistanceLeft.text = "<size=64>" + distanceLeft.ToString("F1") + "</size> m";

                    var timeLeft = Mathf.RoundToInt(distanceLeft / walkingSpeed);

                    if (timeLeft >= 60)
                    {
                        m_TimeLeft.text = "<size=64>" + timeLeft / 60 + "</size>  min <size=64>" + timeLeft % 60 + "</size> sec";
                    }
                    else
                    {
                        m_TimeLeft.text = "<size=64>" + timeLeft % 60 + "</size> sec";
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

        // Calculate the user's walking speed per 1 sec
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

                yield return new WaitForSeconds(1f);
                StartCoroutine(CalculateWalkingSpeed());
            }

            yield return null;
        }

        // Check for Turns and Main Camera Deviations from Path
        IEnumerator CheckForTurns(GameObject a, GameObject b, GameObject c)
        {
            Vector3 A = a.transform.position;
            Vector3 B = b.transform.position;
            Vector3 C = c.transform.position;

            // Check Angle of turn
            Vector3 v1 = new Vector3(C.x - A.x, C.y - A.y, C.z - A.z);
            Vector3 v2 = new Vector3(B.x - A.x, B.y - A.y, B.z - A.z);

            float v1mag = (float)Math.Sqrt(v1.x * v1.x + v1.y * v1.y + v1.z * v1.z);
            Vector3 v1norm = new Vector3(v1.x / v1mag, v1.y / v1mag, v1.z / v1mag);

            float v2mag = (float)Math.Sqrt(v2.x * v2.x + v2.y * v2.y + v2.z * v2.z);
            Vector3 v2norm = new Vector3(v2.x / v2mag, v2.y / v2mag, v2.z / v2mag);

            var res = v1norm.x * v2norm.x + v1norm.y * v2norm.y + v1norm.z * v2norm.z;

            var angle = Math.Acos(res);
            angle = (float)(angle * 180 / Math.PI);

            if (angle >= 180)
                angle -= 180;

            // Check Angle of Main Camera in relative to the path
            var targetWaypoint = new Vector3(A.x, m_MainCamera.transform.position.y, A.z);
            Vector3 targetDir = targetWaypoint - m_MainCamera.transform.position;
            float angleMainCamera = Vector3.Angle(targetDir, m_MainCamera.transform.forward);

            // Check Left or Right Direction of Main Camera
            Vector3 camHeading = m_MainCamera.transform.position - A;
            var camDir = AngleDir(m_MainCamera.transform.forward, camHeading, m_MainCamera.transform.up);

            // If user is not facing the given path
            if (angleMainCamera >= 30)
            {
                m_Direction.text = "";

                leftArrow.SetActive(false);
                rightArrow.SetActive(false);

                if (camDir <= -0.001f)
                {
                    m_LeftDirectionArrow.SetActive(false);
                    m_RightDirectionArrow.SetActive(true);
                }
                else if (camDir >= 0.001f)
                {
                    m_LeftDirectionArrow.SetActive(true);
                    m_RightDirectionArrow.SetActive(false);
                }
                else
                {
                    m_LeftDirectionArrow.SetActive(false);
                    m_RightDirectionArrow.SetActive(false);
                }
            }
            else
            {
                m_LeftDirectionArrow.SetActive(false);
                m_RightDirectionArrow.SetActive(false);

                // Check Left or Right Direction of the Turn
                Vector3 heading = C - A;

                var dir = AngleDir(m_MainCamera.transform.forward, heading, m_MainCamera.transform.up);

                if (angle >= 10)
                {
                    if (dir <= -0.001f)
                    {
                        m_Direction.text = "Turn Left";

                        leftArrow.SetActive(true);
                        rightArrow.SetActive(false);
                    }
                    else if (dir >= 0.001f)
                    {
                        m_Direction.text = "Turn Right";

                        leftArrow.SetActive(false);
                        rightArrow.SetActive(true);
                    }
                    else
                    {
                        m_Direction.text = "";

                        leftArrow.SetActive(false);
                        rightArrow.SetActive(false);
                    }
                }
                else
                {
                    m_Direction.text = "";

                    leftArrow.SetActive(false);
                    rightArrow.SetActive(false);
                }
            }
            yield return null;
        }

        // Get the relative direction (Left / Right) between two vectors
        private float AngleDir(Vector3 fwd, Vector3 targetDir, Vector3 up)
        {
            Vector3 perp = Vector3.Cross(fwd, targetDir);
            float dir = Vector3.Dot(perp, up);

            return dir;
        }
    }
}