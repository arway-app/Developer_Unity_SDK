using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Arway
{
    public class SDKInfoManager : MonoBehaviour
    {

        [SerializeField]
        private string sdkLink = "https://github.com/arway-app/Developer_Unity_SDK";

        [SerializeField]
        private string webStudioLink = "https://developer.arway.app/";

        [SerializeField]
        private string shareLinkAndroid = "";

        [SerializeField]
        private string shareLinkIos = "";


        // Start is called before the first frame update
        void Start()
        {

        }

        public void DownloadSDK()
        {
            Application.OpenURL(sdkLink);
        }

        public void GoToWebStudio()
        {
            Application.OpenURL(webStudioLink);
        }

    }
}