using System;
using UnityEngine;
using UnityEngine.UI;
using Wuxing.Localization;

namespace Wuxing.UI
{
    public class UIConfirmPopup : UIPopup
    {
        private static readonly Vector2 DefaultConfirmAnchorMin = new Vector2(0.1f, 0.08f);
        private static readonly Vector2 DefaultConfirmAnchorMax = new Vector2(0.44f, 0.24f);
        private static readonly Vector2 DefaultCancelAnchorMin = new Vector2(0.56f, 0.08f);
        private static readonly Vector2 DefaultCancelAnchorMax = new Vector2(0.9f, 0.24f);
        private static readonly Vector2 SingleConfirmAnchorMin = new Vector2(0.28f, 0.08f);
        private static readonly Vector2 SingleConfirmAnchorMax = new Vector2(0.72f, 0.24f);

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
            RefreshButtonLayout();
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

        private void RefreshButtonLayout()
        {
            if (confirmButton != null)
            {
                var confirmRect = confirmButton.GetComponent<RectTransform>();
                if (confirmRect != null)
                {
                    confirmRect.anchorMin = _onCancel == null ? SingleConfirmAnchorMin : DefaultConfirmAnchorMin;
                    confirmRect.anchorMax = _onCancel == null ? SingleConfirmAnchorMax : DefaultConfirmAnchorMax;
                    confirmRect.offsetMin = Vector2.zero;
                    confirmRect.offsetMax = Vector2.zero;
                }
            }

            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(_onCancel != null);
                if (_onCancel != null)
                {
                    var cancelRect = cancelButton.GetComponent<RectTransform>();
                    if (cancelRect != null)
                    {
                        cancelRect.anchorMin = DefaultCancelAnchorMin;
                        cancelRect.anchorMax = DefaultCancelAnchorMax;
                        cancelRect.offsetMin = Vector2.zero;
                        cancelRect.offsetMax = Vector2.zero;
                    }
                }
            }
        }
    }
}
