using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Wuxing.UI
{
    public static class UIFactory
    {
        private static Font defaultFont;

        public static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            Stretch(rect);

            var image = go.GetComponent<Image>();
            image.color = color;
            return go;
        }

        public static RectTransform CreateContainer(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        public static Text CreateText(
            Transform parent,
            string name,
            string content,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            Stretch(rect);

            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = GetDefaultFont();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label, UnityAction onClick)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            button.onClick.AddListener(onClick);

            var text = CreateText(buttonObject.transform, "Label", label, 22, TextAnchor.MiddleCenter, new Color(0.96f, 0.92f, 0.84f, 1f));
            text.rectTransform.offsetMin = new Vector2(14f, 10f);
            text.rectTransform.offsetMax = new Vector2(-14f, -10f);

            return button;
        }

        public static Button CreateListButton(Transform parent, string name, string label, UnityAction onClick)
        {
            var button = CreateButton(parent, name, label, onClick);
            var labelText = button.GetComponentInChildren<Text>();
            if (labelText != null)
            {
                labelText.alignment = TextAnchor.UpperLeft;
                labelText.fontSize = 18;
                labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
                labelText.verticalOverflow = VerticalWrapMode.Truncate;
                labelText.rectTransform.offsetMin = new Vector2(16f, 14f);
                labelText.rectTransform.offsetMax = new Vector2(-16f, -14f);
            }

            return button;
        }

        public static Image AddOutlineBox(Transform parent, string name, Color color, float thickness)
        {
            var outlineObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
            outlineObject.transform.SetParent(parent, false);

            var rect = outlineObject.GetComponent<RectTransform>();
            Stretch(rect);

            var image = outlineObject.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);

            var outline = outlineObject.GetComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(thickness, -thickness);

            return image;
        }

        public static Image CreateLine(
            Transform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float size)
        {
            var lineObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            lineObject.transform.SetParent(parent, false);

            var rect = lineObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            if (Mathf.Approximately(anchorMin.x, anchorMax.x))
            {
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
            }
            else
            {
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
            }

            var image = lineObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        public static VerticalLayoutGroup AddVerticalLayout(GameObject target, int spacing, TextAnchor alignment)
        {
            var layout = target.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static ContentSizeFitter AddContentSizeFitter(GameObject target)
        {
            var fitter = target.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            return fitter;
        }

        public static LayoutElement AddLayoutElement(GameObject target, float minHeight)
        {
            var element = target.AddComponent<LayoutElement>();
            element.minHeight = minHeight;
            return element;
        }

        public static ScrollRect CreateScrollRect(Transform parent, string name, Color backgroundColor)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            root.transform.SetParent(parent, false);

            var rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);

            var rootImage = root.GetComponent<Image>();
            rootImage.color = backgroundColor;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(root.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 0f);
            viewportRect.anchorMax = new Vector2(1f, 1f);
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(-28f, 0f);
            var viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
            viewportImage.raycastTarget = true;
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<UIScrollFollowController>();

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = new Vector2(0f, 0f);
            contentRect.offsetMax = new Vector2(0f, 0f);

            var scrollbarRoot = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarRoot.transform.SetParent(root.transform, false);
            var scrollbarRect = scrollbarRoot.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 1f);
            scrollbarRect.offsetMin = new Vector2(-18f, 10f);
            scrollbarRect.offsetMax = new Vector2(-6f, -10f);

            var scrollbarBackground = scrollbarRoot.GetComponent<Image>();
            scrollbarBackground.color = new Color(0.35f, 0.22f, 0.22f, 0.9f);

            var slidingArea = new GameObject("SlidingArea", typeof(RectTransform));
            slidingArea.transform.SetParent(scrollbarRoot.transform, false);
            var slidingAreaRect = slidingArea.GetComponent<RectTransform>();
            slidingAreaRect.anchorMin = Vector2.zero;
            slidingAreaRect.anchorMax = Vector2.one;
            slidingAreaRect.offsetMin = new Vector2(1f, 1f);
            slidingAreaRect.offsetMax = new Vector2(-1f, -1f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(slidingArea.transform, false);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;
            var handleImage = handle.GetComponent<Image>();
            handleImage.color = new Color(0.95f, 0.84f, 0.72f, 1f);

            var scrollbar = scrollbarRoot.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handleImage;
            scrollbar.handleRect = handleRect;
            scrollbar.numberOfSteps = 0;

            var scrollRect = root.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 40f;
            scrollRect.inertia = true;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.verticalScrollbarSpacing = 6f;

            return scrollRect;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Font GetDefaultFont()
        {
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return defaultFont;
        }
    }
}



