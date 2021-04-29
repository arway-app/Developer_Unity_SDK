using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using Microsoft.Azure.SpatialAnchors.Unity.Examples;
using Arway;

public class CreateAnchor : DemoScriptBase
{
    internal enum AppState
    {
        DemoStepCreateSession = 0,
        DemoStepStopSession,
        DemoStepBusy
    }

    private readonly Dictionary<AppState, DemoStepParams> stateParams = new Dictionary<AppState, DemoStepParams>
        {
            { AppState.DemoStepCreateSession,new DemoStepParams() { StepMessage = "Next: Create Azure Spatial Anchors Session" }},
            { AppState.DemoStepStopSession,new DemoStepParams() { StepMessage = "Next: Stop Azure Spatial Anchors Session" }},
            { AppState.DemoStepBusy,new DemoStepParams() { StepMessage = "Processing..." }}
        };

    private AppState _currentAppState = AppState.DemoStepCreateSession;

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

    [HideInInspector]
    public static string currentAnchorId = "";
    public static Vector3 ARCameraPos = Vector3.zero;
    public Quaternion ARCameraRot = Quaternion.identity;

    /// <summary>
    /// Start is called on the frame when a script is enabled just before any
    /// of the Update methods are called the first time.
    /// </summary>
    public override void Start()
    {
        Debug.Log(">>Azure Spatial Anchors Create And Host Anchor");

        base.Start();

        if (!SanityCheckAccessConfiguration())
        {
            return;
        }
        feedbackBox.text = stateParams[currentAppState].StepMessage;

        Debug.Log("Azure Spatial Anchors script started");
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
                // HoloLens: The position will be set based on the unityARUserAnchor that was located.
                SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);
            });
        }
    }

    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    public override void Update()
    {
        base.Update();
    }

    protected override bool IsPlacingObject()
    {
        return currentAppState == AppState.DemoStepCreateSession;
    }

    protected override async Task OnSaveCloudAnchorSuccessfulAsync()
    {
        await base.OnSaveCloudAnchorSuccessfulAsync();

        Debug.Log("Anchor created, yay!");

        currentAnchorId = currentCloudAnchor.Identifier;

        PlayerPrefs.SetString("CURR_ANCHOR_ID", currentAnchorId);

        Debug.Log("ANCHOR ID :- " + currentAnchorId);
        NotificationManager.Instance.GenerateSuccess("ANCHOR ID :- " + currentAnchorId);
        // Sanity check that the object is still where we expect
        Pose anchorPose = Pose.identity;

#if UNITY_ANDROID || UNITY_IOS
        anchorPose = currentCloudAnchor.GetPose();
#endif
        // HoloLens: The position will be set based on the unityARUserAnchor that was located.

        SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);

        currentAppState = AppState.DemoStepStopSession;
    }

    protected override void OnSaveCloudAnchorFailed(Exception exception)
    {
        base.OnSaveCloudAnchorFailed(exception);

        currentAnchorId = string.Empty;
    }

    public async override Task AdvanceDemoAsync()
    {

        switch (currentAppState)
        {
            case AppState.DemoStepCreateSession:
                currentAppState = AppState.DemoStepBusy;

                if (CloudManager.Session == null)
                {
                    await CloudManager.CreateSessionAsync();
                }

                currentAnchorId = "";
                currentCloudAnchor = null;

                await CloudManager.StartSessionAsync();
                ARCameraPos = Camera.main.transform.position;
                ARCameraRot = Quaternion.identity;

                // Create Anchor At Camera Transform
                SpawnOrMoveCurrentAnchoredObject(ARCameraPos, ARCameraRot);

                currentAppState = AppState.DemoStepStopSession;
                break;
            case AppState.DemoStepStopSession:
                currentAppState = AppState.DemoStepBusy;

                if (spawnedObject != null)
                {
                    await SaveCurrentObjectAnchorToCloudAsync();
                }
                else
                {
                    SpawnOrMoveCurrentAnchoredObject(ARCameraPos, ARCameraRot);
                    await SaveCurrentObjectAnchorToCloudAsync();
                }

                CloudManager.StopSession();
                CleanupSpawnedObjects();
                await CloudManager.ResetSessionAsync();

                currentAppState = AppState.DemoStepCreateSession;

                break;
            case AppState.DemoStepBusy:
                break;
            default:
                Debug.Log("Shouldn't get here for app state " + currentAppState.ToString());
                break;
        }
    }

        public static string getCurrentAnchorId()
    {
        return currentAnchorId;
    }
}
