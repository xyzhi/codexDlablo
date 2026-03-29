using UnityEngine;
using UnityEngine.UI;

namespace Wuxing.Localization
{
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;

        private Text targetText;

        public string Key
        {
            get { return key; }
        }

        private void Awake()
        {
            targetText = GetComponent<Text>();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            LocalizationManager.LanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            LocalizationManager.LanguageChanged -= Refresh;
        }

        public void SetKey(string newKey)
        {
            key = newKey;
            if (Application.isPlaying)
            {
                Refresh();
            }
        }

        public void Refresh()
        {
            if (targetText == null)
            {
                targetText = GetComponent<Text>();
            }

            if (targetText == null)
            {
                return;
            }

            targetText.text = LocalizationManager.GetText(key);
        }
    }
}

