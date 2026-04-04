using System;
using System.Collections.Generic;
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
        private const float MultiChoiceButtonAreaHeight = 560f;
        private const float ButtonMessageSpacing = 28f;
        private const float HorizontalPadding = 48f;
        private const float ChoiceCardSpacingX = 12f;
        private const float ChoiceCardSpacingY = 16f;

        [SerializeField] private Text titleText;
        [SerializeField] private Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private LocalizedText titleLocalizedText;
        [SerializeField] private LocalizedText messageLocalizedText;

        private Action onConfirm;
        private Action onCancel;
        private readonly List<Button> choiceButtons = new List<Button>();
        private bool multiChoiceMode;

        private void Awake()
        {
            if (titleText != null)
            {
                titleText.supportRichText = true;
            }

            if (messageText != null)
            {
                messageText.supportRichText = true;
                messageText.fontSize = 16;
            }

            if (confirmButton != null)
            {
                choiceButtons.Add(confirmButton);
            }

            if (cancelButton != null)
            {
                choiceButtons.Add(cancelButton);
            }
        }

        public void Setup(string titleOrKey, string messageOrKey, bool localized, Action confirmAction, Action cancelAction)
        {
            Setup(titleOrKey, messageOrKey, localized, confirmAction, cancelAction, null, null);
        }

        public void Setup(
            string titleOrKey,
            string messageOrKey,
            bool localized,
            Action confirmAction,
            Action cancelAction,
            string confirmButtonLabel,
            string cancelButtonLabel)
        {
            multiChoiceMode = false;
            ApplyContent(titleOrKey, messageOrKey, localized);

            onConfirm = confirmAction;
            onCancel = cancelAction;
            EnsureChoiceButtonCount(2);

            ConfigureActionButton(confirmButton, string.IsNullOrEmpty(confirmButtonLabel) ? LocalizationManager.GetText("popup.confirm_button") : confirmButtonLabel, confirmAction, true);
            ConfigureActionButton(cancelButton, string.IsNullOrEmpty(cancelButtonLabel) ? LocalizationManager.GetText("popup.cancel_button") : cancelButtonLabel, cancelAction, cancelAction != null);

            ApplyDefaultButtonLayout();
            RefreshPopupLayout();
        }

        public void SetupChoices(string title, string message, IList<string> choiceLabels, IList<Action> choiceActions)
        {
            multiChoiceMode = true;
            ApplyContent(title, message, false);

            onConfirm = null;
            onCancel = null;

            var count = choiceLabels != null ? Mathf.Clamp(choiceLabels.Count, 0, 4) : 0;
            EnsureChoiceButtonCount(count);
            for (var i = 0; i < choiceButtons.Count; i++)
            {
                var shouldShow = i < count;
                var label = shouldShow ? choiceLabels[i] : string.Empty;
                var action = shouldShow && choiceActions != null && i < choiceActions.Count ? choiceActions[i] : null;
                ConfigureActionButton(choiceButtons[i], label, action, shouldShow);
            }

            ApplyMultiChoiceLayout(count);
            RefreshPopupLayout();
        }

        private void ApplyContent(string titleOrKey, string messageOrKey, bool localized)
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
        }

        private void EnsureChoiceButtonCount(int count)
        {
            if (confirmButton == null)
            {
                return;
            }

            while (choiceButtons.Count < count)
            {
                var cloneObject = Instantiate(confirmButton.gameObject, confirmButton.transform.parent, false);
                cloneObject.name = "ChoiceButton" + choiceButtons.Count;
                var button = cloneObject.GetComponent<Button>();
                if (button != null)
                {
                    choiceButtons.Add(button);
                }
            }
        }

        private void ConfigureActionButton(Button button, string label, Action action, bool visible)
        {
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(visible);
            button.onClick.RemoveAllListeners();
            if (visible)
            {
                button.onClick.AddListener(delegate
                {
                    UIManager.Instance.CloseTopPopup();
                    if (action != null)
                    {
                        action.Invoke();
                    }
                });
            }

            var buttonText = button.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.supportRichText = true;
                buttonText.text = label;
                buttonText.alignment = multiChoiceMode ? TextAnchor.UpperLeft : TextAnchor.MiddleCenter;
                buttonText.fontSize = multiChoiceMode ? 20 : 24;
                buttonText.resizeTextForBestFit = multiChoiceMode;
                buttonText.resizeTextMinSize = multiChoiceMode ? 16 : 24;
                buttonText.resizeTextMaxSize = multiChoiceMode ? 22 : 24;
                buttonText.lineSpacing = multiChoiceMode ? 1.15f : 1f;
                buttonText.horizontalOverflow = HorizontalWrapMode.Wrap;
                buttonText.verticalOverflow = VerticalWrapMode.Overflow;
                if (multiChoiceMode)
                {
                    buttonText.rectTransform.offsetMin = new Vector2(20f, 18f);
                    buttonText.rectTransform.offsetMax = new Vector2(-20f, -18f);
                }
            }

            if (multiChoiceMode)
            {
                UICardChromeUtility.Apply(button, Color.white, false);
            }
            else
            {
                UIFactory.ApplyStandardButtonChrome(button);
                var rect = button.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 64f);
                }
            }
        }

        private void ApplyDefaultButtonLayout()
        {
            if (confirmButton != null)
            {
                var confirmRect = confirmButton.GetComponent<RectTransform>();
                if (confirmRect != null)
                {
                    confirmRect.anchorMin = onCancel == null ? SingleConfirmAnchorMin : DefaultConfirmAnchorMin;
                    confirmRect.anchorMax = onCancel == null ? SingleConfirmAnchorMax : DefaultConfirmAnchorMax;
                    confirmRect.offsetMin = Vector2.zero;
                    confirmRect.offsetMax = Vector2.zero;
                }
            }

            if (cancelButton != null)
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

            for (var i = 2; i < choiceButtons.Count; i++)
            {
                if (choiceButtons[i] != null)
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void ApplyMultiChoiceLayout(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var button = choiceButtons[i];
                if (button == null)
                {
                    continue;
                }

                var rect = button.GetComponent<RectTransform>();
                if (rect == null)
                {
                    continue;
                }

                var totalRowWidth = count * UICardChromeUtility.StandardCardWidth + Mathf.Max(0, count - 1) * ChoiceCardSpacingX;
                var startX = -totalRowWidth * 0.5f + UICardChromeUtility.StandardCardWidth * 0.5f;
                var x = startX + i * (UICardChromeUtility.StandardCardWidth + ChoiceCardSpacingX);
                var y = BottomPadding;

                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.sizeDelta = new Vector2(UICardChromeUtility.StandardCardWidth, UICardChromeUtility.StandardCardHeight);
                rect.anchoredPosition = new Vector2(x, y);
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
            var parentWidth = parentRect != null ? parentRect.rect.width : Screen.width;
            if (parentHeight <= 0f)
            {
                parentHeight = 1920f;
            }
            if (parentWidth <= 0f)
            {
                parentWidth = 1080f;
            }

            if (multiChoiceMode)
            {
                panelRect.anchorMin = new Vector2(0.03f, 0.2f);
                panelRect.anchorMax = new Vector2(0.97f, 0.8f);
                panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, parentWidth * 0.94f);
            }
            else
            {
                panelRect.anchorMin = new Vector2(0.12f, 0.32f);
                panelRect.anchorMax = new Vector2(0.88f, 0.68f);
            }

            var messageWidth = GetMessageWidth(panelRect);
            if (messageWidth > 0f)
            {
                messageText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, messageWidth);
            }

            Canvas.ForceUpdateCanvases();

            var titleHeight = Mathf.Max(52f, titleText.preferredHeight);
            var messageHeight = Mathf.Max(96f, messageText.preferredHeight);
            if (multiChoiceMode)
            {
                messageHeight = Mathf.Clamp(messageHeight, 96f, 220f);
            }

            var buttonHeight = multiChoiceMode ? 320f : ButtonAreaHeight;
            var desiredHeight = TopPadding
                + titleHeight
                + TitleSpacing
                + messageHeight
                + ButtonMessageSpacing
                + buttonHeight
                + BottomPadding;

            var maxHeight = parentHeight * 0.82f;
            var clampedHeight = Mathf.Clamp(desiredHeight, MinPanelHeight, maxHeight);
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, clampedHeight);
            ApplyChildLayout(titleHeight, messageHeight);
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

        private void ApplyChildLayout(float titleHeight, float messageHeight)
        {
            if (titleText == null || messageText == null)
            {
                return;
            }

            ConfigureTopStretchRect(titleText.rectTransform, TopPadding, titleHeight);
            ConfigureTopStretchRect(messageText.rectTransform, TopPadding + titleHeight + TitleSpacing, messageHeight);

            titleText.alignment = TextAnchor.UpperCenter;
            messageText.alignment = ShouldUseDenseMessageLayout(messageText.text) ? TextAnchor.UpperLeft : TextAnchor.UpperCenter;
            messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
            messageText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static bool ShouldUseDenseMessageLayout(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return false;
            }

            var lineCount = 1;
            for (var i = 0; i < content.Length; i++)
            {
                if (content[i] == '\n')
                {
                    lineCount++;
                }
            }

            return lineCount >= 6 || content.Length >= 120;
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

