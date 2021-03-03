using TMPro;
using UnityEngine;

namespace Arway
{
    public class TimerManager : MonoBehaviour
    {
        public TextMeshProUGUI timerText;

        float theTime;
        public float speed = 1;
        bool playing;

        // Use this for initialization
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            if (playing == true)
            {
                theTime += Time.deltaTime * speed;
                string hours = Mathf.Floor((theTime % 216000) / 3600).ToString("00");
                string minutes = Mathf.Floor((theTime % 3600) / 60).ToString("00");
                string seconds = (theTime % 60).ToString("00");

                if (theTime >= 3600)
                    timerText.text = hours + ":" + minutes + ":" + seconds;
                else
                    timerText.text = minutes + ":" + seconds;
            }
        }

        public void StartTimer()
        {

            timerText.GetComponent<TMP_Text>().enabled = true;
            playing = true;
        }

        public void StopTimer()
        {

            playing = false;
            theTime = 0;
            timerText.GetComponent<TMP_Text>().enabled = false;

        }
    }
}