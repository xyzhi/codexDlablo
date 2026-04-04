using UnityEngine;
using UnityEngine.UI;

namespace Wuxing.UI
{
    public static class UICardChromeUtility
    {
        public const float StandardCardWidth = 180f;
        public const float StandardCardHeight = 260f;
        private const float CardSurfaceInset = 4f;
        private const float CardInnerBorderInset = 10f;
        private const float CardInnerSurfaceInset = 13f;
        private const float ButtonSurfaceInset = 2f;

        public static void Apply(Button button, Color borderColor, bool selected)
        {
            if (button == null)
            {
                return;
            }

            var transform = button.transform;
            CleanupCardChrome(transform);

            var rootImage = button.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.color = borderColor;
                rootImage.raycastTarget = true;
            }

            // Card chrome is always a clear double frame: outer border + inner border.
            var surface = EnsureLayer(transform, "CardSurface", CardSurfaceInset, new Color(0.035f, 0.035f, 0.04f, 0.995f), 0);
            var innerBorder = EnsureLayer(transform, "InnerBorder", CardInnerBorderInset, new Color(borderColor.r, borderColor.g, borderColor.b, 0.9f), 1);
            var innerSurface = EnsureLayer(transform, "InnerSurface", CardInnerSurfaceInset, new Color(0.05f, 0.05f, 0.06f, 0.995f), 2);

            if (surface != null)
            {
                surface.gameObject.SetActive(!selected);
            }

            if (surface != null) surface.raycastTarget = false;
            if (innerBorder != null) innerBorder.raycastTarget = false;
            if (innerSurface != null) innerSurface.raycastTarget = false;
        }

        public static void ApplySimple(Button button, Color borderColor, bool selected)
        {
            if (button == null)
            {
                return;
            }

            var transform = button.transform;
            CleanupButtonChrome(transform);

            var rootImage = button.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.color = new Color(borderColor.r, borderColor.g, borderColor.b, 0.95f);
                rootImage.raycastTarget = true;
            }

            var surface = EnsureLayer(transform, "ButtonSurface", ButtonSurfaceInset, new Color(0.035f, 0.035f, 0.04f, 0.995f), 0);
            if (surface != null)
            {
                surface.gameObject.SetActive(!selected);
                surface.raycastTarget = false;
            }
        }

        private static void CleanupLegacyFrames(Transform parent)
        {
            RemoveLayer(parent, "Outline");
            RemoveLayer(parent, "OuterFrame");
            RemoveLayer(parent, "InnerFrame");
            RemoveLayer(parent, "SelectedFrame");
            RemoveLayer(parent, "SelectedBorder");
        }

        private static void CleanupCardChrome(Transform parent)
        {
            CleanupLegacyFrames(parent);
            HideLayer(parent, "ButtonSurface");
        }

        private static void CleanupButtonChrome(Transform parent)
        {
            CleanupLegacyFrames(parent);
            RemoveLayer(parent, "CardSurface");
            RemoveLayer(parent, "InnerBorder");
            RemoveLayer(parent, "InnerSurface");
        }

        private static void HideLayer(Transform parent, string name)
        {
            var node = parent.Find(name);
            if (node != null)
            {
                node.gameObject.SetActive(false);
            }
        }

        private static void RemoveLayer(Transform parent, string name)
        {
            var node = parent.Find(name);
            if (node != null)
            {
                Object.Destroy(node.gameObject);
            }
        }

        private static Image EnsureLayer(Transform parent, string name, float inset, Color color, int siblingIndex)
        {
            var existing = parent.Find(name) as RectTransform;
            if (existing != null)
            {
                existing.anchorMin = Vector2.zero;
                existing.anchorMax = Vector2.one;
                existing.offsetMin = new Vector2(inset, inset);
                existing.offsetMax = new Vector2(-inset, -inset);
                var existingImage = existing.GetComponent<Image>();
                if (existingImage != null)
                {
                    existingImage.color = color;
                }

                existing.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount - 1));
                return existingImage;
            }

            var layerObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            layerObject.transform.SetParent(parent, false);
            layerObject.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount));

            var rect = layerObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);

            var image = layerObject.GetComponent<Image>();
            image.color = color;
            return image;
        }
    }
}
