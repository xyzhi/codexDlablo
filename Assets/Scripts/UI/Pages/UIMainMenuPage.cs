using UnityEngine;
using UnityEngine.UI;
using Wuxing.Localization;

namespace Wuxing.UI
{
    public class UIMainMenuPage : UIPage
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button popupButton;
        [SerializeField] private Button toastButton;
        [SerializeField] private Button languageButton;

        private void Awake()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnClickStart);
            }

            if (popupButton != null)
            {
                popupButton.onClick.AddListener(OnClickPopup);
            }

            if (toastButton != null)
            {
                toastButton.onClick.AddListener(OnClickToast);
            }

            if (languageButton != null)
            {
                languageButton.onClick.AddListener(OnClickLanguage);
            }
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnClickStart);
            }

            if (popupButton != null)
            {
                popupButton.onClick.RemoveListener(OnClickPopup);
            }

            if (toastButton != null)
            {
                toastButton.onClick.RemoveListener(OnClickToast);
            }

            if (languageButton != null)
            {
                languageButton.onClick.RemoveListener(OnClickLanguage);
            }
        }

        private void OnClickStart()
        {
            UIManager.Instance.ShowPage("Battle");
        }

        private void OnClickPopup()
        {
            var popup = UIManager.Instance.ShowPopup<UIConfirmPopup>("Confirm");
            if (popup == null)
            {
                return;
            }

            popup.Setup(
                "popup.confirm.title",
                "popup.confirm.message",
                true,
                delegate { UIManager.Instance.ShowToastByKey("toast.confirmed"); },
                delegate { UIManager.Instance.ShowToastByKey("toast.cancelled"); });
        }

        private void OnClickToast()
        {
            UIManager.Instance.ShowToastByKey("toast.framework_ready");
        }

        private void OnClickLanguage()
        {
            LocalizationManager.ToggleLanguage();
            UIManager.Instance.ShowToastByKey("toast.language_changed");
        }
    }
}
