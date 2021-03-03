using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Arway
{
    public class HProgressBar : MonoBehaviour
    {
        [SerializeField]
        private Image m_ForegroundImage = null;
        [SerializeField]
        private TextMeshProUGUI m_LabelText = null;
        private int m_CurrentValue = 0;

        public int minValue = 0;
        public int maxValue = 100;

        public int currentValue
        {
            get => m_CurrentValue;
            set
            {
                m_CurrentValue = value;
                m_ForegroundImage.fillAmount = (float)value / (float)maxValue;
                m_LabelText.text = string.Format("{0}%", value);
            }
        }

        void Start()
        {
            Reset();
        }

        public void Reset()
        {
            currentValue = 0;
        }
    }
}