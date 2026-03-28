using System;
using UnityEngine;
using UnityEngine.UI;
using Wuxing.Localization;

namespace Wuxing.UI
{
    public class UIConfirmPopup : UIPopup
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private LocalizedText titleLocalizedText;
        [SerializeField] private LocalizedText messageLocalizedText;

        private Action _onConfirm;
        private Action _onCancel;

        private void Awake()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(Confirm);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(Cancel);
            }
        }

        private void OnDestroy()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(Confirm);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(Cancel);
            }
        }

        public void Setup(string titleOrKey, string messageOrKey, bool localized, Action onConfirm, Action onCancel)
        {
            if (localized)
            {
                if (titleLocalizedText != null)
                {
                    titleLocalizedText.SetKey(titleOrKey);
                }
                else if (titleText != null)
                {
                    titleText.text = LocalizationManager.GetText(titleOrKey);
                }

                if (messageLocalizedText != null)
                {
                    messageLocalizedText.SetKey(messageOrKey);
                }
                else if (messageText != null)
                {
                    messageText.text = LocalizationManager.GetText(messageOrKey);
                }
            }
            else
            {
                if (titleText != null)
                {
                    titleText.text = titleOrKey;
                }

                if (messageText != null)
                {
                    messageText.text = messageOrKey;
                }
            }

            _onConfirm = onConfirm;
            _onCancel = onCancel;
        }

        private void Confirm()
        {
            if (_onConfirm != null)
            {
                _onConfirm.Invoke();
            }

            UIManager.Instance.CloseTopPopup();
        }

        private void Cancel()
        {
            if (_onCancel != null)
            {
                _onCancel.Invoke();
            }

            UIManager.Instance.CloseTopPopup();
        }
    }
}
