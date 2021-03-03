using TMPro;
using UnityEngine;

namespace Arway
{
    public class CloudMapListItem : MonoBehaviour
    { 
        [SerializeField]
        private UIManager uiManager = null;

        [SerializeField]
        private TextMeshProUGUI nameField = null;

        [SerializeField]
        private TextMeshProUGUI dateField = null;

        private string cloud_id = "";


        // Start is called before the first frame update
        void Start()
        {           
            if (uiManager == null)
            {
                uiManager = GetComponentInParent<UIManager>();
            }
        }


        public void DeleteCloudMap()
        {

            string dataString = nameField.text;
            var dataArray = dataString.Trim().Split(' ');

            cloud_id = dataArray[dataArray.Length - 1];

            cloud_id = cloud_id.Replace("(", "").Replace(")", "");

            string cloud_map_name = "";

            for (int i = 0; i < dataArray.Length - 1; i++)
            {
                cloud_map_name += dataArray[i] + " ";
            }


            if (uiManager != null)
            {
                uiManager.ToggleDeletePrompt(true, cloud_id, cloud_map_name.Trim());
            }

        }


    }
}