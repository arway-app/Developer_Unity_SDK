/*===============================================================================
Copyright (C) 2020 ARWAY Ltd. All Rights Reserved.

This file is part of ARwayKit AR SDK

The ARwayKit SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of ARWAY Ltd.

===============================================================================*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Arway
{
    public class Node : MonoBehaviour
    {
        public Vector3 pos;
        public static Node currentclosetnode;

        [Header("A*")]
        public List<Node> neighbors = new List<Node>();
        public float FCost { get { return GCost + HCost; } }
        public float HCost { get; set; }
        public float GCost { get; set; }
        public float Cost { get; set; }
        public Node Parent { get; set; }

        //next node in navigation list
        public Node NextInList { get; set; }

        private void Start()
        {
            pos = this.gameObject.transform.position;
        }
        void OnTriggerEnter(Collider other)
        {
            currentclosetnode = this;
            // Debug.Log(this);
        }

        public void Activate(bool active)
        {
            //transform.GetChild (0).gameObject.SetActive (active);
            if (NextInList != null)
            {
                transform.LookAt(NextInList.transform);
            }
        }

        public void FindNeighbors(float Thresold)
        {
            //  Debug.Log(Thresold);
            foreach (Node node in FindObjectsOfType<Node>())
            {
                // If node is not in neighbours list, add it to the list
                var isDuplicate = false;

                if (Vector3.Distance(node.pos, pos) < Thresold)
                {
                    foreach (var neighbor in neighbors)
                    {
                        if (node == neighbor)
                        {
                            isDuplicate = true;
                            break;
                        }
                    }

                    if (!isDuplicate)
                    {
                        neighbors.Add(node);
                    }
                }
            }
        }
    }
}