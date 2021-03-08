
/*===============================================================================
Copyright (C) 2020 ARWAY Ltd. All Rights Reserved.

This file is part of ARwayKit AR SDK

The ARwayKit SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of ARWAY Ltd.

===============================================================================*/
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections.LowLevel.Unsafe;
using System;
using TMPro;
using System.Collections.Generic;
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using Microsoft.Azure.SpatialAnchors.Unity.Examples;
using System.Threading.Tasks;

namespace Arway
{
    public class AzureAnchorLocalizer : DemoScriptBase
    {
        public ArwaySDK m_Sdk = null;

        public Text debugpose;

        [SerializeField]
        private GameObject loaderPanel;
        [SerializeField]
        private Text loaderText;
        [SerializeField]
        private GameObject localizeButton;

        public List<string> cloudMaps = new List<string>();

        [Header("Content Manager")]
        [SerializeField]
        private GameObject destinationDropdown;

        [Header("Show/Hide Content")]
        [SerializeField]
        private bool showContentBeforeLocalization;

        internal enum AppState
        {
            DemoStepCreateSessionForQuery = 0,
            DemoStepStartSessionForQuery,
            DemoStepLookForAnchor,
            DemoStepLookingForAnchor,
            DemoStepDeleteFoundAnchor,
            DemoStepStopSessionForQuery,
            DemoStepComplete,
            DemoStepBusy
        }

        private readonly Dictionary<AppState, DemoStepParams> stateParams = new Dictionary<AppState, DemoStepParams>
        {
            { AppState.DemoStepCreateSessionForQuery,new DemoStepParams() { StepMessage = "Next: Create Azure Spatial Anchors Session for query"}},
            { AppState.DemoStepStartSessionForQuery,new DemoStepParams() { StepMessage = "Next: Start Azure Spatial Anchors Session for query" }},
            { AppState.DemoStepLookForAnchor,new DemoStepParams() { StepMessage = "Next: Look for Anchor" }},
            { AppState.DemoStepLookingForAnchor,new DemoStepParams() { StepMessage = "Looking for Anchor..." }},
            { AppState.DemoStepDeleteFoundAnchor,new DemoStepParams() { StepMessage = "Next: Delete Anchor"  }},
            { AppState.DemoStepStopSessionForQuery,new DemoStepParams() { StepMessage = "Next: Stop Azure Spatial Anchors Session for query" }},
            { AppState.DemoStepComplete,new DemoStepParams() { StepMessage = "Next: Restart demo" }},
            { AppState.DemoStepBusy,new DemoStepParams() { StepMessage = "Processing..." }}
        };

        private AppState _currentAppState = AppState.DemoStepCreateSessionForQuery;

        AppState currentAppState
        {
            get
            {
                return _currentAppState;
            }
            set
            {
                if (_currentAppState != value)
                {
                    Debug.LogFormat("State from {0} to {1}", _currentAppState, value);
                    _currentAppState = value;

                    if (!isErrorActive)
                    {
                        feedbackBox.text = stateParams[_currentAppState].StepMessage;
                    }
                }
            }
        }
        /// <summary>
        /// Start this instance.
        /// </summary>
        public override void Start()
        {
            m_Sdk = ArwaySDK.Instance;

            if (string.IsNullOrEmpty(m_Sdk.developerToken))
            {
                NotificationManager.Instance.GenerateError("Invalid Developer Token!");
            }

            ARSpace.SetActive(showContentBeforeLocalization);
            destinationDropdown.SetActive(showContentBeforeLocalization);

            base.Start();

            if (!SanityCheckAccessConfiguration())
            {
                return;
            }
            feedbackBox.text = stateParams[currentAppState].StepMessage;
        }

        protected override void OnCloudAnchorLocated(AnchorLocatedEventArgs args)
        {
            base.OnCloudAnchorLocated(args);

            if (args.Status == LocateAnchorStatus.Located)
            {
                currentCloudAnchor = args.Anchor;

                UnityDispatcher.InvokeOnAppThread(() =>
                {
                    Pose anchorPose = Pose.identity;

#if UNITY_ANDROID || UNITY_IOS
                    anchorPose = currentCloudAnchor.GetPose();
#endif
                    if (AzureAssetImporter.mapIdToOffset.ContainsKey(args.Identifier))
                    {
                        Vector3 Mapoffsetposition = AzureAssetImporter.mapIdToOffset[args.Identifier].position;
                        Quaternion Mapoffsetrotation = AzureAssetImporter.mapIdToOffset[args.Identifier].rotation;

                        Matrix4x4 cloudMapOffset = Matrix4x4.TRS(Mapoffsetposition, Mapoffsetrotation, Vector3.one);

                        Matrix4x4 anchorPoseOg = Matrix4x4.TRS(anchorPose.position, anchorPose.rotation, Vector3.one);

                        Matrix4x4 resultpose = anchorPoseOg * cloudMapOffset.inverse;

                        loaderPanel.SetActive(false);
                        localizeButton.SetActive(false);
                        ARSpace.SetActive(true);
                        destinationDropdown.SetActive(true);

                        NotificationManager.Instance.GenerateSuccess("Offset found , setting global pose" + args.Identifier);

                        ARSpace.transform.rotation = resultpose.rotation;
                        ARSpace.transform.position = new Vector3(resultpose[0, 3], resultpose[1, 3], resultpose[2, 3]);
                    }
                    else
                    {
                        ARSpace.SetActive(true);
                        destinationDropdown.SetActive(true);
                        NotificationManager.Instance.GenerateSuccess("No Offset , setting default pose" + args.Identifier);
                        SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);
                    }
                });

                Debug.Log("Yay, anchor located!");
            }
        }

        private void ConfigureSession()
        {
            List<string> anchorsToFind = new List<string>();
            if (currentAppState == AppState.DemoStepCreateSessionForQuery)
            {
                foreach (string item in cloudMaps)
                {
                    anchorsToFind.Add(item);
                }
            }

            SetAnchorIdsToLocate(anchorsToFind);
        }

        public override void Update()
        {
            base.Update();
        }

        public async override Task AdvanceDemoAsync()
        {
            switch (currentAppState)
            {
                case AppState.DemoStepCreateSessionForQuery:
                    CloudManager.StopSession();
                    currentWatcher = null;

                    ConfigureSession();
                    // currentAppState = AppState.DemoStepStartSessionForQuery;
                    currentAppState = AppState.DemoStepBusy;
                    await CloudManager.StartSessionAsync();

                    loaderText.text = "Localizing...";
                    loaderPanel.SetActive(true);

                    currentAppState = AppState.DemoStepLookingForAnchor;
                    if (currentWatcher != null)
                    {
                        currentWatcher.Stop();
                        currentWatcher = null;
                    }
                    currentWatcher = CreateWatcher();
                    if (currentWatcher == null)
                    {
                        Debug.Log("Either cloudmanager or session is null, should not be here!");
                        feedbackBox.text = "YIKES - couldn't create watcher!";
                        currentAppState = AppState.DemoStepLookingForAnchor;
                    }
                    currentAppState = AppState.DemoStepCreateSessionForQuery;
                    break;
                case AppState.DemoStepStartSessionForQuery:
                    currentAppState = AppState.DemoStepBusy;
                    await CloudManager.StartSessionAsync();
                    currentAppState = AppState.DemoStepLookForAnchor;
                    break;
                case AppState.DemoStepLookForAnchor:
                    currentAppState = AppState.DemoStepLookingForAnchor;
                    if (currentWatcher != null)
                    {
                        currentWatcher.Stop();
                        currentWatcher = null;
                    }
                    currentWatcher = CreateWatcher();
                    if (currentWatcher == null)
                    {
                        Debug.Log("Either cloudmanager or session is null, should not be here!");
                        feedbackBox.text = "YIKES - couldn't create watcher!";
                        currentAppState = AppState.DemoStepLookForAnchor;
                    }
                    break;
                case AppState.DemoStepLookingForAnchor:
                    break;
                case AppState.DemoStepDeleteFoundAnchor:
                    currentAppState = AppState.DemoStepBusy;
                    await CloudManager.DeleteAnchorAsync(currentCloudAnchor);
                    CleanupSpawnedObjects();
                    currentAppState = AppState.DemoStepStopSessionForQuery;
                    break;
                case AppState.DemoStepStopSessionForQuery:
                    currentAppState = AppState.DemoStepBusy;
                    CloudManager.StopSession();
                    currentWatcher = null;
                    currentAppState = AppState.DemoStepComplete;
                    break;
                case AppState.DemoStepComplete:
                    currentAppState = AppState.DemoStepBusy;
                    currentCloudAnchor = null;
                    CleanupSpawnedObjects();
                    currentAppState = AppState.DemoStepCreateSessionForQuery;
                    break;
                case AppState.DemoStepBusy:
                    break;
                default:
                    Debug.Log("Shouldn't get here for app state " + currentAppState.ToString());
                    break;
            }
        }

        protected override bool IsPlacingObject()
        {
            return false;
        }
    }
}
