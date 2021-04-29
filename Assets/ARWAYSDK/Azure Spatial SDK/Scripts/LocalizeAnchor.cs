using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using Microsoft.Azure.SpatialAnchors.Unity.Examples;
using Arway;
using TMPro;
using UnityEngine.SceneManagement;

public class LocalizeAnchor : DemoScriptBase
{
    [SerializeField]
    private GameObject loaderPanel;
    [SerializeField]
    private Text loaderText;

    [SerializeField]
    private GameObject localizeButton;

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

    private string currentAnchorId = "";

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

        // Get Current Spatial Anchor ID from PlayerPrefs
        currentAnchorId = PlayerPrefs.GetString("CURR_ANCHOR_ID");

        SetBypassCache(true);
        
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

                NotificationManager.Instance.GenerateSuccess("Anchor found : " + args.Identifier);
                loaderPanel.SetActive(false);
                // localizeButton.SetActive(false);

                // HoloLens: The position will be set based on the unityARUserAnchor that was located.
                SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);
                // currentAppState = AppState.DemoStepDeleteFoundAnchor;
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
        return false;
    }

    protected override async Task OnSaveCloudAnchorSuccessfulAsync()
    {
        await base.OnSaveCloudAnchorSuccessfulAsync();

        Debug.Log("Anchor created, yay!");

        currentAnchorId = currentCloudAnchor.Identifier;

        // Save Currrent Spatial Anchor ID to PlayerPrefs for future reference
        PlayerPrefs.SetString("CURR_ANCHOR_ID", currentAnchorId);

        // Sanity check that the object is still where we expect
        Pose anchorPose = Pose.identity;

#if UNITY_ANDROID || UNITY_IOS
        anchorPose = currentCloudAnchor.GetPose();
#endif
        // HoloLens: The position will be set based on the unityARUserAnchor that was located.

        SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);

        // currentAppState = AppState.DemoStepStopSession;
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

    private void ConfigureSession()
    {
        List<string> anchorsToFind = new List<string>();
        if (currentAppState == AppState.DemoStepCreateSessionForQuery)
        {
            currentAnchorId = PlayerPrefs.GetString("CURR_ANCHOR_ID");
            anchorsToFind.Add(currentAnchorId);
        }

        SetAnchorIdsToLocate(anchorsToFind);
    }

    public void ReloadScene()
    {
        // Reload Mapping Scene
        Debug.Log("Re-Load Mapping Scene.");
        StartCoroutine(ReloadCurrentScene());
    }

    IEnumerator ReloadCurrentScene()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9F)
        {
            yield return null;
        }

        Debug.Log(asyncLoad.progress);
        yield return new WaitForSeconds(0.8f);

        asyncLoad.allowSceneActivation = true;
    }
}
