using UnityEngine;
using UnityEngine.UI;
using Wuxing.Localization;

namespace Wuxing.UI
{
    public class UIStartPage : UIPage
    {
        [SerializeField] private Button enterButton;
        [SerializeField] private Button languageButton;
        [SerializeField] private Text languageStateText;

        public override void OnOpen(object data)
        {
            RefreshLanguageState();
        }

        private void Awake()
        {
            if (enterButton != null)
            {
                enterButton.onClick.AddListener(OnClickEnter);
            }

            if (languageButton != null)
            {
                languageButton.onClick.AddListener(OnClickLanguage);
            }
        }

        private void OnEnable()
        {
            LocalizationManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
        }

        private void OnDestroy()
        {
            if (enterButton != null)
            {
                enterButton.onClick.RemoveListener(OnClickEnter);
            }

            if (languageButton != null)
            {
                languageButton.onClick.RemoveListener(OnClickLanguage);
            }
        }

        private void OnClickEnter()
        {
            UIManager.Instance.ShowPage("MainMenu");
        }

        private void OnClickLanguage()
        {
            LocalizationManager.ToggleLanguage();
            RefreshLanguageState();
            UIManager.Instance.ShowToastByKey("toast.language_changed");
        }

        private void OnLanguageChanged()
        {
            RefreshLanguageState();
        }

        private void RefreshLanguageState()
        {
            if (languageStateText == null)
            {
                return;
            }

            var isEnglish = LocalizationManager.Instance != null
                && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
            languageStateText.text = isEnglish ? "Current Language: English" : "\u5f53\u524d\u8bed\u8a00\uff1a\u7b80\u4f53\u4e2d\u6587";
        }
    }
}
