using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wuxing.UI
{
    public class UICardTipsView : MonoBehaviour
    {
        public const string ResourcePath = "Prefabs/UI/CardTips";

        private RectTransform hostRoot;
        private RectTransform[] dismissBelowRoots = Array.Empty<RectTransform>();
        private Sprite backgroundSprite;
        private Sprite buttonSprite;
        private Font titleFont;
        private Font bodyFont;

        private RectTransform panelRoot;
        private RectTransform arrowRoot;
        private Button dismissButton;
        private Text titleText;
        private Text bodyText;
        private Button actionButton;
        private Text actionButtonText;
        private Action pendingAction;

        public void Bind(RectTransform host, Sprite background, Sprite buttonBackground, Font title, Font body, params RectTransform[] contentRoots)
        {
            hostRoot = host != null ? host : transform.parent as RectTransform;
            backgroundSprite = background;
            buttonSprite = buttonBackground;
            titleFont = title != null ? title : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bodyFont = body != null ? body : titleFont;
            dismissBelowRoots = contentRoots ?? Array.Empty<RectTransform>();
            EnsureBuilt();
            ApplyAssets();
            UpdateDismissLayerOrder();
            Hide();
        }

        public void Show(RectTransform sourceCard, string title, string body, string actionLabel, Action action)
        {
            EnsureBuilt();
            ApplyAssets();
            UpdateDismissLayerOrder();

            pendingAction = action;
            titleText.text = title ?? string.Empty;
            bodyText.text = body ?? string.Empty;

            var hasAction = !string.IsNullOrEmpty(actionLabel) && action != null;
            actionButton.gameObject.SetActive(hasAction);
            actionButton.interactable = hasAction;
            actionButtonText.text = actionLabel ?? string.Empty;

            dismissButton.gameObject.SetActive(true);
            panelRoot.gameObject.SetActive(true);
            arrowRoot.gameObject.SetActive(true);
            panelRoot.SetAsLastSibling();
            arrowRoot.SetAsLastSibling();

            Layout(sourceCard, hasAction);
        }

        public void Hide()
        {
            pendingAction = null;

            if (dismissButton != null)
            {
                dismissButton.gameObject.SetActive(false);
            }

            if (panelRoot != null)
            {
                panelRoot.gameObject.SetActive(false);
            }

            if (arrowRoot != null)
            {
                arrowRoot.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (dismissButton != null)
            {
                dismissButton.onClick.RemoveListener(Hide);
            }

            if (actionButton != null)
            {
                actionButton.onClick.RemoveListener(HandleActionButtonClicked);
            }
        }

        private void HandleActionButtonClicked()
        {
            pendingAction?.Invoke();
        }

        private void EnsureBuilt()
        {
            if (hostRoot == null)
            {
                hostRoot = transform.parent as RectTransform;
            }

            if (hostRoot == null)
            {
                return;
            }

            if (dismissButton == null)
            {
                var dismissObject = new GameObject("TipsDismiss", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                dismissObject.transform.SetParent(hostRoot, false);
                var dismissRect = dismissObject.GetComponent<RectTransform>();
                dismissRect.anchorMin = Vector2.zero;
                dismissRect.anchorMax = Vector2.one;
                dismissRect.offsetMin = Vector2.zero;
                dismissRect.offsetMax = Vector2.zero;

                var dismissImage = dismissObject.GetComponent<Image>();
                dismissImage.color = new Color(0f, 0f, 0f, 0.001f);

                dismissButton = dismissObject.GetComponent<Button>();
                dismissButton.onClick.AddListener(Hide);
            }

            if (panelRoot == null)
            {
                var panelObject = new GameObject("TipsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panelObject.transform.SetParent(hostRoot, false);
                panelRoot = panelObject.GetComponent<RectTransform>();
                panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
                panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
                panelRoot.pivot = new Vector2(0.5f, 0.5f);
                panelRoot.sizeDelta = new Vector2(380f, 280f);
            }

            if (arrowRoot == null)
            {
                var arrowObject = new GameObject("TipsArrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                arrowObject.transform.SetParent(hostRoot, false);
                arrowRoot = arrowObject.GetComponent<RectTransform>();
                arrowRoot.anchorMin = new Vector2(0.5f, 0.5f);
                arrowRoot.anchorMax = new Vector2(0.5f, 0.5f);
                arrowRoot.pivot = new Vector2(0.5f, 0.5f);
                arrowRoot.sizeDelta = new Vector2(24f, 24f);

                var arrowImage = arrowObject.GetComponent<Image>();
                arrowImage.color = new Color(0.97f, 0.96f, 0.94f, 1f);
                arrowImage.raycastTarget = false;
            }

            if (titleText == null)
            {
                titleText = CreateText("Title", panelRoot, 28, FontStyle.Bold);
                titleText.alignment = TextAnchor.MiddleLeft;
                titleText.color = new Color(0.2f, 0.14f, 0.1f, 1f);
                EnsureOutline(titleText, new Color(1f, 1f, 1f, 0.2f), new Vector2(1f, -1f));
            }

            if (bodyText == null)
            {
                bodyText = CreateText("Body", panelRoot, 22, FontStyle.Normal);
                bodyText.alignment = TextAnchor.UpperLeft;
                bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
                bodyText.verticalOverflow = VerticalWrapMode.Overflow;
                bodyText.lineSpacing = 1.15f;
                bodyText.color = new Color(0.24f, 0.18f, 0.12f, 0.96f);
            }

            if (actionButton == null)
            {
                var actionObject = new GameObject("ActionButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                actionObject.transform.SetParent(panelRoot, false);

                var actionRect = actionObject.GetComponent<RectTransform>();
                actionRect.anchorMin = new Vector2(0.5f, 0f);
                actionRect.anchorMax = new Vector2(0.5f, 0f);
                actionRect.pivot = new Vector2(0.5f, 0f);
                actionRect.sizeDelta = new Vector2(144f, 44f);

                actionButton = actionObject.GetComponent<Button>();
                var colors = actionButton.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 0.98f, 0.92f, 1f);
                colors.pressedColor = new Color(0.88f, 0.82f, 0.74f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
                actionButton.colors = colors;
                actionButton.onClick.AddListener(HandleActionButtonClicked);

                actionButtonText = CreateText("Label", actionRect, 24, FontStyle.Bold);
                actionButtonText.alignment = TextAnchor.MiddleCenter;
                actionButtonText.rectTransform.anchorMin = Vector2.zero;
                actionButtonText.rectTransform.anchorMax = Vector2.one;
                actionButtonText.rectTransform.offsetMin = new Vector2(18f, 10f);
                actionButtonText.rectTransform.offsetMax = new Vector2(-18f, -10f);
                actionButtonText.color = new Color(0.22f, 0.14f, 0.08f, 1f);
                EnsureOutline(actionButtonText, new Color(1f, 1f, 1f, 0.18f), new Vector2(1f, -1f));
            }
        }

        private void ApplyAssets()
        {
            if (panelRoot != null)
            {
                var image = panelRoot.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = backgroundSprite;
                    image.type = backgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;
                    image.color = new Color(1f, 1f, 1f, 0.98f);
                    image.raycastTarget = true;
                }
            }

            if (actionButton != null)
            {
                var image = actionButton.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = buttonSprite;
                    image.type = buttonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
                    image.color = new Color(0.96f, 0.93f, 0.86f, 1f);
                }
            }

            if (titleText != null)
            {
                titleText.font = titleFont;
            }

            if (bodyText != null)
            {
                bodyText.font = bodyFont;
            }

            if (actionButtonText != null)
            {
                actionButtonText.font = titleFont;
            }
        }

        private void Layout(RectTransform sourceCard, bool hasAction)
        {
            if (hostRoot == null || sourceCard == null || panelRoot == null || titleText == null || bodyText == null || actionButton == null || arrowRoot == null)
            {
                return;
            }

            const float bubbleWidth = 380f;
            const float minBubbleHeight = 180f;
            const float maxBubbleHeight = 520f;
            const float horizontalGap = 28f;
            const float screenPadding = 18f;
            const float topPadding = 26f;
            const float sidePadding = 38f;
            const float buttonSpacing = 18f;
            const float bottomPadding = 26f;
            const float titleHeight = 34f;
            const float buttonHeight = 44f;
            const float arrowSize = 24f;

            var rootRect = hostRoot.rect;
            var corners = new Vector3[4];
            sourceCard.GetWorldCorners(corners);
            var localBottomLeft = hostRoot.InverseTransformPoint(corners[0]);
            var localTopRight = hostRoot.InverseTransformPoint(corners[2]);
            var cardCenterY = (localBottomLeft.y + localTopRight.y) * 0.5f;

            var bodyWidth = bubbleWidth - sidePadding * 2f;
            titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            titleText.rectTransform.anchorMax = new Vector2(0f, 1f);
            titleText.rectTransform.pivot = new Vector2(0f, 1f);
            titleText.rectTransform.sizeDelta = new Vector2(bodyWidth, titleHeight);
            titleText.rectTransform.anchoredPosition = new Vector2(sidePadding, -topPadding);

            bodyText.rectTransform.anchorMin = new Vector2(0f, 1f);
            bodyText.rectTransform.anchorMax = new Vector2(0f, 1f);
            bodyText.rectTransform.pivot = new Vector2(0f, 1f);
            bodyText.rectTransform.sizeDelta = new Vector2(bodyWidth, 100f);
            bodyText.rectTransform.anchoredPosition = new Vector2(sidePadding, -(topPadding + titleHeight + 10f));
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;

            Canvas.ForceUpdateCanvases();
            var bodyHeight = Mathf.Clamp(bodyText.preferredHeight, 72f, 340f);
            bodyText.rectTransform.sizeDelta = new Vector2(bodyWidth, bodyHeight);

            var actionHeight = hasAction ? buttonHeight + buttonSpacing : 0f;
            var bubbleHeight = Mathf.Clamp(topPadding + titleHeight + 10f + bodyHeight + actionHeight + bottomPadding, minBubbleHeight, maxBubbleHeight);
            panelRoot.sizeDelta = new Vector2(bubbleWidth, bubbleHeight);

            actionButton.gameObject.SetActive(hasAction);
            if (hasAction)
            {
                actionButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 18f);
            }

            var preferRight = true;
            var candidateCenterX = localTopRight.x + horizontalGap + bubbleWidth * 0.5f;
            if (candidateCenterX + bubbleWidth * 0.5f > rootRect.xMax - screenPadding)
            {
                preferRight = false;
                candidateCenterX = localBottomLeft.x - horizontalGap - bubbleWidth * 0.5f;
            }

            candidateCenterX = Mathf.Clamp(candidateCenterX, rootRect.xMin + screenPadding + bubbleWidth * 0.5f, rootRect.xMax - screenPadding - bubbleWidth * 0.5f);
            var candidateCenterY = Mathf.Clamp(cardCenterY, rootRect.yMin + screenPadding + bubbleHeight * 0.5f, rootRect.yMax - screenPadding - bubbleHeight * 0.5f);
            panelRoot.anchoredPosition = new Vector2(candidateCenterX, candidateCenterY);

            var arrowX = preferRight
                ? candidateCenterX - bubbleWidth * 0.5f - arrowSize * 0.42f
                : candidateCenterX + bubbleWidth * 0.5f + arrowSize * 0.42f;
            var arrowY = Mathf.Clamp(cardCenterY, candidateCenterY - bubbleHeight * 0.5f + 28f, candidateCenterY + bubbleHeight * 0.5f - 28f);
            arrowRoot.anchoredPosition = new Vector2(arrowX, arrowY);
            arrowRoot.localRotation = Quaternion.Euler(0f, 0f, preferRight ? 45f : 225f);
        }

        private void UpdateDismissLayerOrder()
        {
            if (dismissButton == null || hostRoot == null)
            {
                return;
            }

            var siblingIndex = Mathf.Max(0, hostRoot.childCount - 1);
            for (var i = 0; i < dismissBelowRoots.Length; i++)
            {
                var root = dismissBelowRoots[i];
                if (root == null)
                {
                    continue;
                }

                siblingIndex = Mathf.Min(siblingIndex, root.GetSiblingIndex());
            }

            dismissButton.transform.SetSiblingIndex(siblingIndex);
        }

        private static Text CreateText(string name, RectTransform parent, int fontSize, FontStyle fontStyle)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.supportRichText = true;
            text.raycastTarget = false;
            return text;
        }

        private static void EnsureOutline(Text text, Color color, Vector2 distance)
        {
            if (text == null)
            {
                return;
            }

            var outline = text.GetComponent<Outline>();
            if (outline == null)
            {
                outline = text.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }
    }
}
