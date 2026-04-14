using System;
using UnityEngine;
using UnityEngine.UI;

namespace Wuxing.UI
{
    public class UICardTipsView : MonoBehaviour
    {
        public const string ResourcePath = "Prefabs/UI/CardTips";

        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private Image panelImage;
        [SerializeField] private Button dismissButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button actionButton;
        [SerializeField] private Text actionButtonText;

        private RectTransform hostRoot;
        private RectTransform dismissLayerRoot;
        private RectTransform[] dismissBelowRoots = Array.Empty<RectTransform>();
        private RectTransform dismissAboveRoot;
        private Sprite backgroundSprite;
        private Sprite buttonSprite;
        private Font titleFont;
        private Font bodyFont;
        private Action pendingAction;

        public void Bind(RectTransform host, Sprite background, Sprite buttonBackground, Font title, Font body, RectTransform aboveRoot, params RectTransform[] contentRoots)
        {
            hostRoot = host != null ? host : transform.parent as RectTransform;
            backgroundSprite = background;
            buttonSprite = buttonBackground;
            titleFont = title != null ? title : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bodyFont = body != null ? body : titleFont;
            dismissAboveRoot = aboveRoot;
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
            transform.SetAsLastSibling();
            panelRoot.SetAsLastSibling();
            UpdateDismissLayerOrder();

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

        }

        private void Awake()
        {
            EnsureBuilt();
            ApplyAssets();
            Hide();
        }

        private void OnDestroy()
        {
            if (dismissButton != null)
            {
                dismissButton.onClick.RemoveListener(Hide);
                if (dismissLayerRoot != null && dismissButton.transform.parent == dismissLayerRoot)
                {
                    Destroy(dismissButton.gameObject);
                }
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

            AutoWireFromPrefab();

            if (dismissButton == null || panelRoot == null || panelImage == null
                || titleText == null || bodyText == null || actionButton == null || actionButtonText == null)
            {
                EnsureFallbackObjects();
                AutoWireFromPrefab();
            }

            if (dismissButton == null || panelRoot == null || panelImage == null
                || titleText == null || bodyText == null || actionButton == null || actionButtonText == null)
            {
                Debug.LogWarning("UICardTipsView missing required references.", this);
                return;
            }

            EnsureDismissUnderHostRoot();

            dismissButton.onClick.RemoveListener(Hide);
            dismissButton.onClick.AddListener(Hide);
            actionButton.onClick.RemoveListener(HandleActionButtonClicked);
            actionButton.onClick.AddListener(HandleActionButtonClicked);
        }

        private void AutoWireFromPrefab()
        {
            if (dismissButton == null)
            {
                dismissButton = transform.Find("TipsDismiss")?.GetComponent<Button>();
                if (dismissButton == null && dismissLayerRoot != null)
                {
                    dismissButton = dismissLayerRoot.Find("TipsDismiss")?.GetComponent<Button>();
                }

                if (dismissButton == null && hostRoot != null)
                {
                    dismissButton = hostRoot.Find("TipsDismiss")?.GetComponent<Button>();
                }
            }

            if (panelRoot == null)
            {
                panelRoot = transform.Find("TipsPanel") as RectTransform;
            }

            if (panelImage == null && panelRoot != null)
            {
                panelImage = panelRoot.GetComponent<Image>();
            }

            if (titleText == null && panelRoot != null)
            {
                titleText = panelRoot.Find("Title")?.GetComponent<Text>();
            }

            if (bodyText == null && panelRoot != null)
            {
                bodyText = panelRoot.Find("Body")?.GetComponent<Text>();
            }

            if (actionButton == null && panelRoot != null)
            {
                actionButton = panelRoot.Find("ActionButton")?.GetComponent<Button>();
            }

            if (actionButtonText == null && actionButton != null)
            {
                actionButtonText = actionButton.transform.Find("Label")?.GetComponent<Text>();
            }
        }

        private void EnsureDismissUnderHostRoot()
        {
            if (dismissButton == null || hostRoot == null)
            {
                return;
            }

            var dismissRect = dismissButton.transform as RectTransform;
            if (dismissRect == null)
            {
                return;
            }

            dismissLayerRoot = ResolveDismissLayerRoot();
            if (dismissLayerRoot == null)
            {
                dismissLayerRoot = hostRoot;
            }

            if (dismissRect.parent != dismissLayerRoot)
            {
                dismissRect.SetParent(dismissLayerRoot, false);
            }

            dismissRect.anchorMin = Vector2.zero;
            dismissRect.anchorMax = Vector2.one;
            dismissRect.pivot = new Vector2(0.5f, 0.5f);
            dismissRect.offsetMin = Vector2.zero;
            dismissRect.offsetMax = Vector2.zero;
        }

        private void EnsureFallbackObjects()
        {
            if (dismissButton == null)
            {
                var dismissObject = new GameObject("TipsDismiss", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                dismissObject.transform.SetParent(transform, false);
                var dismissRect = dismissObject.GetComponent<RectTransform>();
                dismissRect.anchorMin = Vector2.zero;
                dismissRect.anchorMax = Vector2.one;
                dismissRect.offsetMin = Vector2.zero;
                dismissRect.offsetMax = Vector2.zero;
                dismissObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);
                dismissButton = dismissObject.GetComponent<Button>();
            }

            if (panelRoot == null)
            {
                var panelObject = new GameObject("TipsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panelObject.transform.SetParent(transform, false);
                var rect = panelObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(380f, 280f);
            }

            if (titleText == null && panelRoot != null)
            {
                CreateText("Title", panelRoot, 28, FontStyle.Bold);
            }

            if (bodyText == null && panelRoot != null)
            {
                var body = CreateText("Body", panelRoot, 22, FontStyle.Normal);
                body.alignment = TextAnchor.UpperLeft;
                body.horizontalOverflow = HorizontalWrapMode.Wrap;
                body.verticalOverflow = VerticalWrapMode.Overflow;
                body.lineSpacing = 1.15f;
            }

            if (actionButton == null && panelRoot != null)
            {
                var actionObject = new GameObject("ActionButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                actionObject.transform.SetParent(panelRoot, false);
                var actionRect = actionObject.GetComponent<RectTransform>();
                actionRect.anchorMin = new Vector2(0.5f, 0f);
                actionRect.anchorMax = new Vector2(0.5f, 0f);
                actionRect.pivot = new Vector2(0.5f, 0f);
                actionRect.sizeDelta = new Vector2(144f, 44f);
                actionButton = actionObject.GetComponent<Button>();

                var label = CreateText("Label", actionRect, 24, FontStyle.Bold);
                label.alignment = TextAnchor.MiddleCenter;
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(18f, 10f);
                label.rectTransform.offsetMax = new Vector2(-18f, -10f);
                actionButtonText = label;
            }
        }

        private void ApplyAssets()
        {
            if (panelImage != null)
            {
                panelImage.sprite = backgroundSprite;
                panelImage.type = backgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;
                panelImage.color = new Color(1f, 1f, 1f, 0.98f);
                panelImage.raycastTarget = true;
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

                var colors = actionButton.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 0.98f, 0.92f, 1f);
                colors.pressedColor = new Color(0.88f, 0.82f, 0.74f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
                actionButton.colors = colors;
            }

            if (titleText != null)
            {
                titleText.font = titleFont;
                titleText.color = new Color(0.2f, 0.14f, 0.1f, 1f);
                titleText.alignment = TextAnchor.MiddleLeft;
                EnsureOutline(titleText, new Color(1f, 1f, 1f, 0.2f), new Vector2(1f, -1f));
            }

            if (bodyText != null)
            {
                bodyText.font = bodyFont;
                bodyText.color = new Color(0.24f, 0.18f, 0.12f, 0.96f);
                bodyText.alignment = TextAnchor.UpperLeft;
                bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
                bodyText.verticalOverflow = VerticalWrapMode.Overflow;
                bodyText.lineSpacing = 1.15f;
            }

            if (actionButtonText != null)
            {
                actionButtonText.font = titleFont;
                actionButtonText.color = new Color(0.22f, 0.14f, 0.08f, 1f);
                actionButtonText.alignment = TextAnchor.MiddleCenter;
                EnsureOutline(actionButtonText, new Color(1f, 1f, 1f, 0.18f), new Vector2(1f, -1f));
            }
        }

        private void Layout(RectTransform sourceCard, bool hasAction)
        {
            if (hostRoot == null || sourceCard == null || panelRoot == null || titleText == null || bodyText == null || actionButton == null)
            {
                return;
            }

            const float bubbleWidth = 380f;
            const float minBubbleHeight = 180f;
            const float maxBubbleHeight = 520f;
            const float horizontalGap = 8f;
            const float screenPadding = 18f;
            const float topPadding = 26f;
            const float sidePadding = 38f;
            const float buttonSpacing = 18f;
            const float bottomPadding = 26f;
            const float titleHeight = 34f;
            const float buttonHeight = 44f;
            const float tailAnchorX = 20f;
            const float tailAnchorYFromBottom = 28f;

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
            panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            panelRoot.pivot = new Vector2(0f, 0f);

            actionButton.gameObject.SetActive(hasAction);
            if (hasAction)
            {
                actionButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 18f);
            }

            var preferRight = true;
            var candidatePivotX = localTopRight.x + horizontalGap - tailAnchorX;
            if (candidatePivotX + bubbleWidth > rootRect.xMax - screenPadding)
            {
                preferRight = false;
                candidatePivotX = localBottomLeft.x - horizontalGap + tailAnchorX;
            }

            if (preferRight)
            {
                candidatePivotX = Mathf.Clamp(candidatePivotX, rootRect.xMin + screenPadding, rootRect.xMax - screenPadding - bubbleWidth);
            }
            else
            {
                candidatePivotX = Mathf.Clamp(candidatePivotX, rootRect.xMin + screenPadding + bubbleWidth, rootRect.xMax - screenPadding);
            }

            var candidatePivotY = Mathf.Clamp(
                cardCenterY - tailAnchorYFromBottom,
                rootRect.yMin + screenPadding,
                rootRect.yMax - screenPadding - bubbleHeight);

            panelRoot.anchoredPosition = new Vector2(candidatePivotX, candidatePivotY);
            panelRoot.localScale = new Vector3(preferRight ? 1f : -1f, 1f, 1f);

            if (titleText != null)
            {
                titleText.rectTransform.localScale = new Vector3(preferRight ? 1f : -1f, 1f, 1f);
            }

            if (bodyText != null)
            {
                bodyText.rectTransform.localScale = new Vector3(preferRight ? 1f : -1f, 1f, 1f);
            }

            if (actionButton != null)
            {
                actionButton.GetComponent<RectTransform>().localScale = new Vector3(preferRight ? 1f : -1f, 1f, 1f);
            }

            if (actionButtonText != null)
            {
                actionButtonText.rectTransform.localScale = new Vector3(preferRight ? 1f : -1f, 1f, 1f);
            }
        }

        private void UpdateDismissLayerOrder()
        {
            if (dismissButton == null)
            {
                return;
            }

            dismissLayerRoot = ResolveDismissLayerRoot();
            if (dismissLayerRoot == null)
            {
                return;
            }

            var dismissRect = dismissButton.transform as RectTransform;
            if (dismissRect != null && dismissRect.parent != dismissLayerRoot)
            {
                dismissRect.SetParent(dismissLayerRoot, false);
                dismissRect.anchorMin = Vector2.zero;
                dismissRect.anchorMax = Vector2.one;
                dismissRect.offsetMin = Vector2.zero;
                dismissRect.offsetMax = Vector2.zero;
            }

            var slotRoot = dismissBelowRoots.Length > 0 ? ResolveChildUnderLayerRoot(dismissBelowRoots[0]) : null;
            var closeRoot = ResolveChildUnderLayerRoot(dismissAboveRoot);
            if (slotRoot != null && closeRoot != null)
            {
                var slotIndex = slotRoot.GetSiblingIndex();
                var closeIndex = closeRoot.GetSiblingIndex();
                var siblingIndex = Mathf.Min(slotIndex, closeIndex) + 1;
                dismissButton.transform.SetSiblingIndex(siblingIndex);
                return;
            }

            if (slotRoot != null)
            {
                dismissButton.transform.SetSiblingIndex(slotRoot.GetSiblingIndex());
                return;
            }

            if (closeRoot != null)
            {
                dismissButton.transform.SetSiblingIndex(closeRoot.GetSiblingIndex());
            }
        }

        private RectTransform ResolveTopLevelChild(RectTransform node)
        {
            if (node == null || hostRoot == null)
            {
                return null;
            }

            var current = node;
            while (current.parent is RectTransform parent && parent != hostRoot)
            {
                current = parent;
            }

            return current;
        }

        private RectTransform ResolveChildUnderLayerRoot(RectTransform node)
        {
            if (node == null || dismissLayerRoot == null)
            {
                return null;
            }

            var current = node;
            while (current.parent is RectTransform parent && parent != dismissLayerRoot)
            {
                current = parent;
            }

            return current.parent == dismissLayerRoot ? current : null;
        }

        private RectTransform ResolveDismissLayerRoot()
        {
            var slotRoot = dismissBelowRoots.Length > 0 ? dismissBelowRoots[0] : null;
            if (slotRoot != null && dismissAboveRoot != null)
            {
                var common = FindCommonRectParent(slotRoot, dismissAboveRoot);
                if (common != null)
                {
                    return common;
                }
            }

            return hostRoot;
        }

        private static RectTransform FindCommonRectParent(RectTransform a, RectTransform b)
        {
            var currentA = a;
            while (currentA != null)
            {
                var currentB = b;
                while (currentB != null)
                {
                    if (currentA == currentB)
                    {
                        return currentA;
                    }

                    currentB = currentB.parent as RectTransform;
                }

                currentA = currentA.parent as RectTransform;
            }

            return null;
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
