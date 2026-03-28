using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Wuxing.UI
{
    public static class UIFactory
    {
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

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.16f, 0.18f, 0.24f, 0.95f);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            AddOutlineBox(buttonObject.transform, "Outline", new Color(0.85f, 0.88f, 0.93f, 0.8f), 1f);

            var text = CreateText(buttonObject.transform, "Label", label, 24, TextAnchor.MiddleCenter, Color.white);
            text.rectTransform.offsetMin = new Vector2(12f, 8f);
            text.rectTransform.offsetMax = new Vector2(-12f, -8f);

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

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
