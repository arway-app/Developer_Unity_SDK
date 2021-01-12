using UnityEditor;

namespace Arway
{
    [CustomEditor(typeof(ArwaySDK))]
    public class ArwaySDKInfo : Editor
    {
        private ArwaySDK sdk
        {
            get { return target as ArwaySDK; }
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("ARWAY SDK v" + ArwaySDK.sdkVersion, MessageType.Info);
            base.OnInspectorGUI();
        }
    }
}
