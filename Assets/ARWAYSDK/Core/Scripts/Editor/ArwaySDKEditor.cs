using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Networking;

namespace Arway
{
    [CustomEditor(typeof(ArwaySDK))]
    public class ArwaySDKEditor : EditorWindow
    {

        private string myEmail = "";
        private string myPassword = "";
        private string myToken = "";
        private ArwaySDK sdk = null;

        private string alertText = "";

        private static UnityWebRequest request;

        [MenuItem("Arway SDK/Open Settings")]
        static void Init()
        {
            ArwaySDKEditor window = (ArwaySDKEditor)EditorWindow.GetWindow(typeof(ArwaySDKEditor));
            //window.position = new Rect(Screen.width / 2, Screen.height / 2, 450, 100);
            window.Show();

        }

        //Add a menu item to link to slack.
        [MenuItem("Arway SDK/Help")]
        static void HelpOutput()
        {
            Application.OpenURL("https://docs.arway.app/arway-sdk/quickstart-in-unity");
        }

        [MenuItem("Arway SDK/Support")]
        static void Support()
        {
            Application.OpenURL("https://discord.com/invite/5mG9ewM?utm_source=unity&utm_medium=button&utm_campaign=customers");
        }

        void OnGUI()
        {
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Arway SDK Version " + ArwaySDK.sdkVersion + "\n", EditorStyles.wordWrappedLabel);

            GUILayout.Label("Credentials", EditorStyles.boldLabel);
            EditorGUILayout.Separator();

            myEmail = EditorGUILayout.TextField("Email", myEmail);
            myPassword = EditorGUILayout.PasswordField("Password", myPassword);

            EditorGUILayout.Separator();
            if (GUILayout.Button("Login", GUILayout.Width(120), GUILayout.Height(28)))
            {
                alertText = "";
                Login(myEmail, myPassword);

                EditorApplication.update += EditorUpdate;
            }

            EditorGUILayout.Separator();

            EditorGUILayout.LabelField(alertText, EditorStyles.boldLabel);

            EditorGUILayout.Separator();

            myToken = EditorGUILayout.TextField("Developer Token", myToken);
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            GuiLine(2);
            EditorGUILayout.Separator();

            GUILayout.Label("Not a registered user?");
            if (GUILayout.Button("Register Now", GUILayout.Width(120), GUILayout.Height(30)))
            {
                Application.OpenURL("https://developer.arway.io/register.php");
            }

            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("(C) 2020 ARWAY Ltd. All Right Reserved.");

        }

        //=================== DRAW LINE ==============================

        void GuiLine(int i_height = 1)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, i_height);
            rect.height = i_height;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }


        void OnInspectorUpdate()
        {
            Repaint();
        }

        void Login(string email, string password)
        {
            WWWForm form = new WWWForm();
            form.AddField("user", email);
            form.AddField("pass", password);

            sdk = ArwaySDK.Instance;

            request = UnityWebRequest.Post(sdk.ContentServer + EndPoint.AUTH, form);
            request.useHttpContinue = false;
            request.SendWebRequest();
        }

        private void EditorUpdate()
        {
            while (!request.isDone)
                return;

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.Log(request.error);
            }
            else
            {
                string response = request.downloadHandler.text;

                if (response.Equals("INVALID PASSWORD!"))
                {
                    alertText = "INVALID PASSWORD!";
                    Debug.Log(" **********\tLOGIN FAILED!!!!!\t*********");
                }
                else if (response.Length < 20)
                {
                    Debug.Log(" **********\tInvalid Credentials!!!!!\t*********");
                    alertText = "Invalid Credentials!!";
                }
                else
                {
                    Debug.Log(" **********\tLOGIN SUCESS\t*********");
                    alertText = "LOGIN SUCESS";
                    myToken = response;
                    sdk.developerToken = myToken;
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

                }
            }

            EditorApplication.update -= EditorUpdate;
        }
    }
}
