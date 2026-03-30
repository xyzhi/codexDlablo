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
        private const float MinPanelHeight = 420f;
        private const float TopPadding = 48f;
        private const float BottomPadding = 48f;
        private const float TitleSpacing = 20f;
        private const float ButtonAreaHeight = 120f;
        private const float ButtonMessageSpacing = 28f;
        private const float HorizontalPadding = 48f;

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
            Setup(titleOrKey, messageOrKey, localized, onConfirm, onCancel, null, null);
        }

        public void Setup(
            string titleOrKey,
            string messageOrKey,
            bool localized,
            Action onConfirm,
            Action onCancel,
            string confirmButtonLabel,
            string cancelButtonLabel)
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
            ApplyButtonLabels(confirmButtonLabel, cancelButtonLabel);
            RefreshButtonLayout();
            RefreshPopupLayout();
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

        private void ApplyButtonLabels(string confirmButtonLabel, string cancelButtonLabel)
        {
            if (confirmButtonLabel == "Next Stage"
                || confirmButtonLabel == "Retry"
                || confirmButtonLabel == "下一关"
                || confirmButtonLabel == "重试")
            {
                cancelButtonLabel = LocalizationManager.Instance != null
                    && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English
                    ? "Close"
                    : "关闭";
            }

            if (confirmButton != null)
            {
                var confirmText = confirmButton.GetComponentInChildren<Text>();
                if (confirmText != null)
                {
                    confirmText.text = string.IsNullOrEmpty(confirmButtonLabel)
                        ? LocalizationManager.GetText("popup.confirm_button")
                        : confirmButtonLabel;
                }
            }

            if (cancelButton != null)
            {
                var cancelText = cancelButton.GetComponentInChildren<Text>();
                if (cancelText != null)
                {
                    cancelText.text = string.IsNullOrEmpty(cancelButtonLabel)
                        ? LocalizationManager.GetText("popup.cancel_button")
                        : cancelButtonLabel;
                }
            }
        }

        private void RefreshPopupLayout()
        {
            var panelRect = GetPanelRect();
            if (panelRect == null || titleText == null || messageText == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            var parentRect = panelRect.parent as RectTransform;
            var parentHeight = parentRect != null ? parentRect.rect.height : Screen.height;
            if (parentHeight <= 0f)
            {
                parentHeight = 1920f;
            }

            var messageWidth = GetMessageWidth(panelRect);
            if (messageWidth > 0f)
            {
                messageText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, messageWidth);
            }

            Canvas.ForceUpdateCanvases();

            var titleHeight = Mathf.Max(52f, titleText.preferredHeight);
            var messageHeight = Mathf.Max(96f, messageText.preferredHeight);
            var desiredHeight = TopPadding
                + titleHeight
                + TitleSpacing
                + messageHeight
                + ButtonMessageSpacing
                + ButtonAreaHeight
                + BottomPadding;

            var maxHeight = parentHeight * 0.82f;
            var clampedHeight = Mathf.Clamp(desiredHeight, MinPanelHeight, maxHeight);
            ApplyPanelHeight(panelRect, clampedHeight, parentHeight);
            ApplyChildLayout(panelRect, titleHeight, messageHeight);
        }

        private RectTransform GetPanelRect()
        {
            if (titleText == null)
            {
                return null;
            }

            return titleText.transform.parent as RectTransform;
        }

        private float GetMessageWidth(RectTransform panelRect)
        {
            var panelWidth = panelRect.rect.width;
            if (panelWidth <= 0f)
            {
                var parentRect = panelRect.parent as RectTransform;
                if (parentRect != null)
                {
                    panelWidth = parentRect.rect.width * (panelRect.anchorMax.x - panelRect.anchorMin.x);
                }
            }

            return Mathf.Max(0f, panelWidth - 64f);
        }

        private void ApplyPanelHeight(RectTransform panelRect, float desiredHeight, float parentHeight)
        {
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, desiredHeight);
        }

        private void ApplyChildLayout(RectTransform panelRect, float titleHeight, float messageHeight)
        {
            if (panelRect == null || titleText == null || messageText == null)
            {
                return;
            }

            var titleRect = titleText.rectTransform;
            var messageRect = messageText.rectTransform;

            ConfigureTopStretchRect(titleRect, TopPadding, titleHeight);
            ConfigureTopStretchRect(messageRect, TopPadding + titleHeight + TitleSpacing, messageHeight);

            titleText.alignment = TextAnchor.UpperCenter;
            messageText.alignment = TextAnchor.UpperCenter;
            messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
            messageText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static void ConfigureTopStretchRect(RectTransform rect, float topOffset, float height)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(HorizontalPadding, -(topOffset + height));
            rect.offsetMax = new Vector2(-HorizontalPadding, -topOffset);
        }
    }
}

