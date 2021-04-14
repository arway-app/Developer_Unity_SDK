/*===============================================================================
Copyright (C) 2020 ARWAY Ltd. All Rights Reserved.

This file is part of ARwayKit AR SDK

The ARwayKit SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of ARWAY Ltd.

===============================================================================*/
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System;
using Unity.Collections;

namespace Arway
{
	public class ArwaySDK : MonoBehaviour
	{
        public static string sdkVersion = "0.1.0";

        private static ArwaySDK instance = null;

		public enum CameraResolution { Default, SD, HD, FullHD };
		[SerializeField]
		[Tooltip("Android Resolution - minmmum is better for network")]
		private CameraResolution m_AndroidResolution = CameraResolution.SD;

		[SerializeField]
		[Tooltip("iOS Resolution - minmmum is better for network")]
		private CameraResolution m_iOSResolution = CameraResolution.SD;

		[Tooltip("Enter SDK developer token")]
		public string developerToken;

		[SerializeField]
		[Tooltip("Application target frame rate")]
		private int m_TargetFrameRate = 60;

		private string m_LocalizationServer = "https://localization.arway.app/";

        private string m_contentServer = "https://api.arway.app/";

		private ARCameraManager m_CameraManager;
		private ARSession m_ARSession;

		private bool m_ConfigDone = false;


		public static ArwaySDK Instance
		{
			get
			{
#if UNITY_EDITOR
				if (instance == null && !Application.isPlaying)
				{
					instance = UnityEngine.Object.FindObjectOfType<ArwaySDK>();
				}
#endif
				if (instance == null)
				{
					Debug.LogError("No ArwaySDK instance");
				}
				return instance;
			}
		}


		public CameraResolution androidResolution
		{
			get { return m_AndroidResolution; }
		}

		public CameraResolution iOSResolution
		{
			get { return m_iOSResolution; }
		}

		public string localizationServer
		{
			get { return m_LocalizationServer; }
		}

		public string arwaysdkversion
		{
			get { return sdkVersion; }
		}

		public string ContentServer
		{
			get { return m_contentServer; }
		}


		public ARCameraManager cameraManager
		{
			get
			{
				if (m_CameraManager == null)
				{
					m_CameraManager = UnityEngine.Object.FindObjectOfType<ARCameraManager>();

				}
				return m_CameraManager;
			}
		}

		public ARSession arSession
		{
			get
			{
				if (m_ARSession == null)
				{
					m_ARSession = UnityEngine.Object.FindObjectOfType<ARSession>();
					if (m_ARSession == null)
						Debug.Log("No ARSession found");
				}
				return m_ARSession;
			}
		}

		public int targetFrameRate
		{
			get { return m_TargetFrameRate; }
			set
			{
				m_TargetFrameRate = value;
				SetFrameRate();
			}
		}

		void Awake()
		{
			if (instance == null)
			{
				instance = this;
			}
			if (instance != this)
			{
				Debug.LogError("There must be only one ArwaySDK object in a scene.");
				UnityEngine.Object.DestroyImmediate(this);
				return;
			}

			if (developerToken != null && developerToken.Length > 0)
			{
				PlayerPrefs.SetString("token", developerToken);
			}
		}

		void Start()
		{
			SetFrameRate();
		}

		private void SetFrameRate()
		{
			Application.targetFrameRate = 60;
		}

		private void Update()
		{

			if (!m_ConfigDone && cameraManager != null)
				ConfigureCamera();
		}


		private void ConfigureCamera()
		{
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
			var cameraSubsystem = cameraManager.subsystem;
			if (cameraSubsystem == null || !cameraSubsystem.running)
            {
				return;
            }
			var configurations = cameraSubsystem.GetConfigurations(Allocator.Temp);
			if (!configurations.IsCreated || (configurations.Length <= 0))
            {
				return;
            }
			int bestError = int.MaxValue;
			var currentConfig = cameraSubsystem.currentConfiguration;
			int w = (int)currentConfig?.width;
			int h = (int)currentConfig?.height;

			if (w == 0 && h == 0)
            {
				return;
            }

#if UNITY_ANDROID
			CameraResolution reso = androidResolution;
#else
			CameraResolution reso = iOSResolution;
#endif
			switch (reso)
			{
				case CameraResolution.Default:
					w = (int)currentConfig?.width;
					h = (int)currentConfig?.height;
					break;
				case CameraResolution.SD:
					w = 640;
					h = 480;
					break;
				case CameraResolution.HD:
					w = 1280;
					h = 720;
					break;
				case CameraResolution.FullHD:
					w = 1920;
					h = 1080;
					break;
			}

			foreach (var config in configurations)
			{
				int perror = config.width * config.height - w * h;
				if (Math.Abs(perror) < bestError)
				{
					bestError = Math.Abs(perror);
					currentConfig = config;
				}
			}

			if (reso != CameraResolution.Default) {
				Debug.Log("resolution = " + (int)currentConfig?.width + "x" + (int)currentConfig?.height);
				cameraSubsystem.currentConfiguration = currentConfig;
			}
#endif
			m_ConfigDone = true;
		}
	}
}