using System;
using UnityEngine;
using UnityEngine.UI;

namespace Wuxing.UI
{
    public class UICardTipsView : MonoBehaviour
    {
        public const string ResourcePath = "Prefabs/UI/CardTips";

        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private RectTransform panelVisualRoot;
        [SerializeField] private Image panelImage;
        [SerializeField] private Button dismissButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button actionButton;
        [SerializeField] private Text actionButtonText;
        [Header("Debug")]
        [SerializeField] private string debugSourceCardName;
        [SerializeField] private Vector2 debugSourceCardCenter;
        [SerializeField] private Vector2 debugTargetPoint;
        [SerializeField] private Vector2 debugBubblePivotPosition;
        [SerializeField] private Vector2 debugBubbleTailPoint;
        [SerializeField] private bool debugPreferRight;
        [SerializeField] private Rect debugPlacementRect;

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
            if (titleText != null)
            {
                titleText.gameObject.SetActive(true);
            }

            if (bodyText != null)
            {
                bodyText.gameObject.SetActive(true);
            }

            if (actionButton != null)
            {
                actionButton.gameObject.SetActive(hasAction);
            }

            transform.SetAsLastSibling();
            panelRoot.SetSiblingIndex(0);
            if (titleText != null)
            {
                titleText.transform.SetAsLastSibling();
            }

            if (bodyText != null)
            {
                bodyText.transform.SetAsLastSibling();
            }

            if (actionButton != null)
            {
                actionButton.transform.SetAsLastSibling();
            }

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

            if (titleText != null)
            {
                titleText.gameObject.SetActive(false);
            }

            if (bodyText != null)
            {
                bodyText.gameObject.SetActive(false);
            }

            if (actionButton != null)
            {
                actionButton.gameObject.SetActive(false);
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

            EnsurePanelVisualRoot();
            EnsureContentOutsidePanel();
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

            if (panelVisualRoot == null && panelImage != null)
            {
                panelVisualRoot = panelImage.rectTransform;
            }

            if (titleText == null)
            {
                titleText = transform.Find("TipsTitle")?.GetComponent<Text>();
                if (titleText == null && panelRoot != null)
                {
                    titleText = panelRoot.Find("Title")?.GetComponent<Text>();
                }
            }

            if (bodyText == null)
            {
                bodyText = transform.Find("TipsBody")?.GetComponent<Text>();
                if (bodyText == null && panelRoot != null)
                {
                    bodyText = panelRoot.Find("Body")?.GetComponent<Text>();
                }
            }

            if (actionButton == null)
            {
                actionButton = transform.Find("TipsActionButton")?.GetComponent<Button>();
                if (actionButton == null && panelRoot != null)
                {
                    actionButton = panelRoot.Find("ActionButton")?.GetComponent<Button>();
                }
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

        private void EnsureContentOutsidePanel()
        {
            if (panelRoot == null)
            {
                return;
            }

            ReparentContentRect(titleText != null ? titleText.rectTransform : null, "TipsTitle");
            ReparentContentRect(bodyText != null ? bodyText.rectTransform : null, "TipsBody");
            ReparentContentRect(actionButton != null ? actionButton.GetComponent<RectTransform>() : null, "TipsActionButton");
        }

        private void EnsurePanelVisualRoot()
        {
            if (panelRoot == null || panelImage == null)
            {
                return;
            }

            if (panelVisualRoot != null && panelVisualRoot != panelRoot)
            {
                panelVisualRoot.anchorMin = Vector2.zero;
                panelVisualRoot.anchorMax = Vector2.one;
                panelVisualRoot.pivot = new Vector2(0.5f, 0.5f);
                panelVisualRoot.offsetMin = Vector2.zero;
                panelVisualRoot.offsetMax = Vector2.zero;
                panelVisualRoot.SetAsFirstSibling();
                return;
            }

            var sourceImage = panelImage;
            var visualObject = new GameObject("TipsPanelVisual", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            visualObject.transform.SetParent(panelRoot, false);
            visualObject.transform.SetSiblingIndex(0);

            var visualRect = visualObject.GetComponent<RectTransform>();
            visualRect.anchorMin = Vector2.zero;
            visualRect.anchorMax = Vector2.one;
            visualRect.pivot = new Vector2(0.5f, 0.5f);
            visualRect.offsetMin = Vector2.zero;
            visualRect.offsetMax = Vector2.zero;

            var visualImage = visualObject.GetComponent<Image>();
            visualImage.sprite = sourceImage.sprite;
            visualImage.type = sourceImage.type;
            visualImage.color = sourceImage.color;
            visualImage.material = sourceImage.material;
            visualImage.raycastTarget = sourceImage.raycastTarget;
            visualImage.maskable = sourceImage.maskable;
            visualImage.preserveAspect = sourceImage.preserveAspect;
            visualImage.fillCenter = sourceImage.fillCenter;
            visualImage.fillMethod = sourceImage.fillMethod;
            visualImage.fillAmount = sourceImage.fillAmount;
            visualImage.fillClockwise = sourceImage.fillClockwise;
            visualImage.fillOrigin = sourceImage.fillOrigin;
            visualImage.useSpriteMesh = sourceImage.useSpriteMesh;
            visualImage.pixelsPerUnitMultiplier = sourceImage.pixelsPerUnitMultiplier;

            Destroy(sourceImage);
            panelVisualRoot = visualRect;
            panelImage = visualImage;
        }

        private void ReparentContentRect(RectTransform rect, string targetName)
        {
            if (rect == null)
            {
                return;
            }

            if (rect.parent != transform)
            {
                rect.SetParent(transform, false);
            }

            if (!string.IsNullOrEmpty(targetName))
            {
                rect.name = targetName;
            }
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

            if (titleText == null)
            {
                CreateText("TipsTitle", transform as RectTransform, 28, FontStyle.Bold);
            }

            if (bodyText == null)
            {
                var body = CreateText("TipsBody", transform as RectTransform, 22, FontStyle.Normal);
                body.alignment = TextAnchor.UpperLeft;
                body.horizontalOverflow = HorizontalWrapMode.Wrap;
                body.verticalOverflow = VerticalWrapMode.Overflow;
                body.lineSpacing = 1.15f;
            }

            if (actionButton == null)
            {
                var actionObject = new GameObject("TipsActionButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                actionObject.transform.SetParent(transform, false);
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
                if (backgroundSprite != null)
                {
                    panelImage.sprite = backgroundSprite;
                    panelImage.type = Image.Type.Sliced;
                }
                panelImage.color = new Color(1f, 1f, 1f, 0.98f);
                panelImage.raycastTarget = true;
            }

            if (actionButton != null)
            {
                var image = actionButton.GetComponent<Image>();
                if (image != null)
                {
                    if (buttonSprite != null)
                    {
                        image.sprite = buttonSprite;
                        image.type = Image.Type.Sliced;
                    }
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
                actionButtonText.color = new Color(0.95f, 0.9f, 0.78f, 1f);
                actionButtonText.alignment = TextAnchor.MiddleCenter;
                EnsureOutline(actionButtonText, new Color(0.18f, 0.12f, 0.08f, 0.75f), new Vector2(1f, -1f));
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
            var rootRect = hostRoot.rect;
            var placementRect = ResolvePlacementRect();
            debugPlacementRect = placementRect;
            var corners = new Vector3[4];
            sourceCard.GetWorldCorners(corners);
            var localBottomLeft = hostRoot.InverseTransformPoint(corners[0]);
            var localTopRight = hostRoot.InverseTransformPoint(corners[2]);
            var cardCenterX = (localBottomLeft.x + localTopRight.x) * 0.5f;
            var cardCenterY = (localBottomLeft.y + localTopRight.y) * 0.5f;
            debugSourceCardName = sourceCard.name;
            debugSourceCardCenter = new Vector2(cardCenterX, cardCenterY);
            debugTargetPoint = debugSourceCardCenter;

            var preferRight = true;
            var candidatePivotX = cardCenterX;
            if (candidatePivotX + bubbleWidth > placementRect.xMax - screenPadding)
            {
                preferRight = false;
                candidatePivotX = cardCenterX;
            }
            debugPreferRight = preferRight;

            if (preferRight)
            {
                candidatePivotX = Mathf.Clamp(candidatePivotX, placementRect.xMin + screenPadding, placementRect.xMax - screenPadding - bubbleWidth);
            }
            else
            {
                candidatePivotX = Mathf.Clamp(candidatePivotX, placementRect.xMin + screenPadding + bubbleWidth, placementRect.xMax - screenPadding);
            }

            var registrationPivot = preferRight ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
            var bodyWidth = bubbleWidth - sidePadding * 2f;
            var titleRect = titleText.rectTransform;
            var bodyRect = bodyText.rectTransform;
            var textAnchor = preferRight ? new Vector2(0f, 1f) : new Vector2(1f, 1f);

            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = textAnchor;
            titleRect.sizeDelta = new Vector2(bodyWidth, titleHeight);

            bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRect.pivot = textAnchor;
            bodyRect.sizeDelta = new Vector2(bodyWidth, 100f);
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;

            Canvas.ForceUpdateCanvases();
            var bodyHeight = Mathf.Clamp(bodyText.preferredHeight, 72f, 340f);
            bodyRect.sizeDelta = new Vector2(bodyWidth, bodyHeight);

            var actionHeight = hasAction ? buttonHeight + buttonSpacing : 0f;
            var bubbleHeight = Mathf.Clamp(topPadding + titleHeight + 10f + bodyHeight + actionHeight + bottomPadding, minBubbleHeight, maxBubbleHeight);
            panelRoot.sizeDelta = new Vector2(bubbleWidth, bubbleHeight);
            panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            panelRoot.pivot = registrationPivot;

            var candidatePivotY = Mathf.Clamp(
                cardCenterY,
                rootRect.yMin + screenPadding,
                rootRect.yMax - screenPadding - bubbleHeight);

            var panelLeft = preferRight ? candidatePivotX : candidatePivotX - bubbleWidth;
            var panelRight = panelLeft + bubbleWidth;
            var panelBottom = candidatePivotY;
            var panelTop = panelBottom + bubbleHeight;
            var textX = preferRight ? panelLeft + sidePadding : panelRight - sidePadding;

            titleRect.anchoredPosition = new Vector2(textX, panelTop - topPadding);
            bodyRect.anchoredPosition = new Vector2(textX, panelTop - (topPadding + titleHeight + 10f));

            actionButton.gameObject.SetActive(hasAction);
            if (hasAction)
            {
                var actionRect = actionButton.GetComponent<RectTransform>();
                if (actionRect != null)
                {
                    actionRect.anchorMin = new Vector2(0.5f, 0.5f);
                    actionRect.anchorMax = new Vector2(0.5f, 0.5f);
                    actionRect.pivot = new Vector2(0.5f, 0f);
                    actionRect.anchoredPosition = new Vector2(panelLeft + bubbleWidth * 0.5f, panelBottom + 18f);
                }
            }

            panelRoot.anchoredPosition = new Vector2(candidatePivotX, candidatePivotY);
            debugBubblePivotPosition = panelRoot.anchoredPosition;
            debugBubbleTailPoint = panelRoot.anchoredPosition;
            panelRoot.localScale = Vector3.one;

            if (panelVisualRoot != null)
            {
                panelVisualRoot.localScale = new Vector3(preferRight ? 1f : -1f, 1f, 1f);
            }

            if (titleText != null)
            {
                titleText.rectTransform.localScale = Vector3.one;
            }

            if (bodyText != null)
            {
                bodyText.rectTransform.localScale = Vector3.one;
            }

            if (actionButton != null)
            {
                actionButton.GetComponent<RectTransform>().localScale = Vector3.one;
            }

            if (actionButtonText != null)
            {
                actionButtonText.rectTransform.localScale = Vector3.one;
            }
        }

        private Rect ResolvePlacementRect()
        {
            if (hostRoot == null)
            {
                return new Rect(-960f, -540f, 1920f, 1080f);
            }

            var hasBounds = false;
            var min = Vector2.zero;
            var max = Vector2.zero;
            for (var i = 0; i < dismissBelowRoots.Length; i++)
            {
                var rect = dismissBelowRoots[i];
                if (rect == null)
                {
                    continue;
                }

                var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(hostRoot, rect);
                var boundsMin = new Vector2(bounds.min.x, bounds.min.y);
                var boundsMax = new Vector2(bounds.max.x, bounds.max.y);
                if (!hasBounds)
                {
                    min = boundsMin;
                    max = boundsMax;
                    hasBounds = true;
                    continue;
                }

                min = Vector2.Min(min, boundsMin);
                max = Vector2.Max(max, boundsMax);
            }

            if (!hasBounds)
            {
                return hostRoot.rect;
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
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
