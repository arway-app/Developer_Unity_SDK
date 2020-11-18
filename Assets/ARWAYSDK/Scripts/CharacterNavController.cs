/*===============================================================================
Copyright (C) 2020 ARWAY Ltd. All Rights Reserved.

This file is part of ARwayKit AR SDK

The ARwayKit SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of ARWAY Ltd.

===============================================================================*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arway
{
    public class CharacterNavController : MonoBehaviour
    {
        [SerializeField]
        public GameObject m_CharacterGuide;

        public List<Node> m_path = new List<Node>();

        private GameObject m_MainCamera;

        private Animator m_CharacterAnimator;

        private Vector3 m_Target;

        private float m_MovementSpeed = 0.3f;
        private float m_DistanceFromCamera, xDistance, zDistance;
        private float m_DistanceFromTarget, distanceToX, distanceToZ;

        private bool m_NavigationEnd = false;

        private string m_CurrDestination = " ";

        NavController navController;

        private void Start()
        {
            // Get reference to NavController Script
            navController = gameObject.GetComponent<NavController>();

            // Get Main Camera reference 
            m_MainCamera = Camera.main.gameObject;

            // Set first target to main camera position
            m_Target = new Vector3(m_MainCamera.transform.position.x, m_MainCamera.transform.position.y - 1, m_MainCamera.transform.position.z) + transform.forward;

            // Instantiate Character Guide Set it's animation to Idle
            m_CharacterGuide = Instantiate(m_CharacterGuide, m_Target, Quaternion.identity) as GameObject;
            m_CharacterAnimator = m_CharacterGuide.GetComponent<Animator>();
            m_CharacterAnimator.SetTrigger("idle");

            m_CharacterGuide.SetActive(false);

            // Get Target Vector3 coordinates
            StartCoroutine(NextVal());
        }

        private void Update()
        {
            // If Navigation Path is not empty
            if (navController.path.Count > 0)
            {
                // Check if destination has been updated
                string tmp = PlayerPrefs.GetString("DEST_NAME", "DestinationNotSelected");
                if (tmp != m_CurrDestination)
                {
                    m_CurrDestination = tmp;
                    m_NavigationEnd = false;
                }

                // Fetch Current Path
                m_path = navController.path;
            }

            // If Navigation has not ended
            if (m_NavigationEnd == false)
            {
                // Calculate distance of guide from the main camera
                xDistance = Mathf.Abs(m_MainCamera.transform.position.x - m_CharacterGuide.transform.position.x);
                zDistance = Mathf.Abs(m_MainCamera.transform.position.z - m_CharacterGuide.transform.position.z);

                m_DistanceFromCamera = xDistance > zDistance ? xDistance : zDistance;

                // Calculate character movement speed depending upon distance from camera
                m_MovementSpeed = 1.5f - m_DistanceFromCamera;

                if (m_MovementSpeed < 0.2f)
                {
                    m_MovementSpeed = 0.2f;
                }
                if (m_MovementSpeed > 1.0f)
                {
                    m_MovementSpeed = 1.0f;
                }

                // If the user is within range of camera
                if (m_DistanceFromCamera <= 1.5f)
                {
                    m_CharacterAnimator.SetTrigger("walk");

                    // Calculate distance to move
                    float step = m_MovementSpeed * Time.deltaTime;
                    m_CharacterGuide.transform.position = Vector3.MoveTowards(m_CharacterGuide.transform.position, m_Target, step);

                    // Smoothly rotate towards the target point.
                    var rotationValue = m_Target - m_CharacterGuide.transform.position;
                    if (rotationValue != Vector3.zero)
                    {
                        var targetRotation = Quaternion.LookRotation(rotationValue, Vector3.up);
                        m_CharacterGuide.transform.rotation = Quaternion.Slerp(m_CharacterGuide.transform.rotation, targetRotation, m_MovementSpeed * 2 * Time.deltaTime);
                    }
                }
                else
                {
                    m_CharacterAnimator.SetTrigger("idle");

                    // Smoothly rotate towards the Camera.
                    var cameraRotation = Quaternion.LookRotation(Camera.main.transform.position - m_CharacterGuide.transform.position, Vector3.up);
                    m_CharacterGuide.transform.rotation = Quaternion.Slerp(m_CharacterGuide.transform.rotation, cameraRotation, m_MovementSpeed * 2 * Time.deltaTime);
                }
            }
            else
            {
                m_CharacterAnimator.SetTrigger("idle");

                // Smoothly rotate towards the Camera.
                var cameraRotation = Quaternion.LookRotation(Camera.main.transform.position - m_CharacterGuide.transform.position);
                m_CharacterGuide.transform.rotation = Quaternion.Slerp(m_CharacterGuide.transform.rotation, cameraRotation, m_MovementSpeed * 2 * Time.deltaTime);
            }

            // Reset rotation of x and z axis
            m_CharacterGuide.transform.localEulerAngles = new Vector3(0, m_CharacterGuide.transform.rotation.eulerAngles.y, 0);
        }

        // Get the next coordinate to the destination
        IEnumerator NextVal()
        {
            // If the destination is not empty and the navigation has not ended
            if (m_path.Count != 0 && m_NavigationEnd == false)
            {
                // Calculate distance of guide from the main camera
                xDistance = Mathf.Abs(m_MainCamera.transform.position.x - m_CharacterGuide.transform.position.x);
                zDistance = Mathf.Abs(m_MainCamera.transform.position.z - m_CharacterGuide.transform.position.z);

                m_DistanceFromCamera = xDistance > zDistance ? xDistance : zDistance;

                // If the user is within range of camera
                if (m_DistanceFromCamera <= 1.5f)
                {
                    // Calculate the distance of the character guide from the target coordinates
                    m_Target = m_path[0].gameObject.transform.position;

                    m_CharacterGuide.SetActive(true);

                    distanceToX = Mathf.Abs(m_Target.x - m_CharacterGuide.transform.position.x);
                    distanceToZ = Mathf.Abs(m_Target.z - m_CharacterGuide.transform.position.z);

                    m_DistanceFromTarget = distanceToX > distanceToZ ? distanceToX : distanceToZ;

                    // If Character is near destination
                    if (m_path.Count <= 1)
                    {
                        m_Target = m_path[m_path.Count - 1].gameObject.transform.position;

                        distanceToX = Mathf.Abs(m_Target.x - m_CharacterGuide.transform.position.x);
                        distanceToZ = Mathf.Abs(m_Target.z - m_CharacterGuide.transform.position.z);

                        m_DistanceFromTarget = distanceToX > distanceToZ ? distanceToX : distanceToZ;

                        // If character has reached destination
                        if (m_DistanceFromTarget <= 0.1f)
                        {
                            m_NavigationEnd = true;
                            Debug.Log("Navigation Ended!");
                        }
                    }
                }
            }

            yield return new WaitForSeconds(0.1f);
            StartCoroutine(NextVal());
        }
    }
}
