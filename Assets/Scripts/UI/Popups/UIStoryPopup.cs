using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wuxing.Config;
using Wuxing.Game;
using Wuxing.Localization;

namespace Wuxing.UI
{
    public class UIStoryPopup : UIPopup, IPointerClickHandler
    {
        private Image rootImage;
        private Text titleText;
        private Text contentText;
        private Text hintText;
        private string fullContent;
        private float typingSpeed;
        private float skipHintDelay;
        private float revealProgress;
        private float openElapsed;
        private bool typingCompleted;
        private bool canSkipTyping;

        private void Awake()
        {
            EnsureUi();
        }

        public override void OnOpen(object data)
        {
            EnsureUi();

            var node = data as StoryNodeConfig;
            if (node == null)
            {
                StoryManager.AdvanceCurrentNode();
                return;
            }

            var title = ResolveText(node.TitleKey);
            fullContent = ResolveText(node.ContentKey);
            typingSpeed = Mathf.Max(1f, node.TypingCharsPerSecond > 0 ? node.TypingCharsPerSecond : 24f);
            skipHintDelay = Mathf.Max(0.2f, node.SkipHintDelay > 0f ? node.SkipHintDelay : 1.8f);
            revealProgress = 0f;
            openElapsed = 0f;
            typingCompleted = string.IsNullOrEmpty(fullContent);
            canSkipTyping = node.Skippable;

            titleText.text = string.IsNullOrEmpty(title) ? string.Empty : title;
            titleText.gameObject.SetActive(!string.IsNullOrEmpty(title));
            contentText.text = typingCompleted ? fullContent : string.Empty;
            hintText.gameObject.SetActive(false);
            hintText.text = ResolveUiText("story.skip_hint", "Tap anywhere to skip");
        }

        public override void OnClose()
        {
            fullContent = string.Empty;
            contentText.text = string.Empty;
            hintText.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(fullContent) || typingCompleted)
            {
                openElapsed += Time.unscaledDeltaTime;
                if (canSkipTyping && !hintText.gameObject.activeSelf && openElapsed >= skipHintDelay)
                {
                    hintText.gameObject.SetActive(true);
                }

                return;
            }

            openElapsed += Time.unscaledDeltaTime;
            revealProgress += typingSpeed * Time.unscaledDeltaTime;
            var visibleCount = Mathf.Clamp(Mathf.FloorToInt(revealProgress), 0, fullContent.Length);
            contentText.text = fullContent.Substring(0, visibleCount);
            if (visibleCount >= fullContent.Length)
            {
                typingCompleted = true;
            }

            if (canSkipTyping && !hintText.gameObject.activeSelf && openElapsed >= skipHintDelay)
            {
                hintText.gameObject.SetActive(true);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!typingCompleted)
            {
                if (!canSkipTyping)
                {
                    return;
                }

                typingCompleted = true;
                contentText.text = fullContent;
                hintText.gameObject.SetActive(true);
                return;
            }

            StoryManager.AdvanceCurrentNode();
        }

        private void EnsureUi()
        {
            if (TryBindExistingUi())
            {
                return;
            }

            gameObject.name = "StoryPopup";
            rootImage = gameObject.GetComponent<Image>();
            if (rootImage == null)
            {
                rootImage = gameObject.AddComponent<Image>();
            }

            rootImage.color = new Color(0.03f, 0.03f, 0.05f, 0.95f);
            rootImage.raycastTarget = true;

            var rootRect = GetComponent<RectTransform>();
            if (rootRect != null)
            {
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
            }

            var topShade = UIFactory.CreatePanel(transform, "TopShade", new Color(0f, 0f, 0f, 0.18f));
            var topShadeRect = topShade.GetComponent<RectTransform>();
            topShadeRect.anchorMin = new Vector2(0f, 0.58f);
            topShadeRect.anchorMax = new Vector2(1f, 1f);
            topShadeRect.offsetMin = Vector2.zero;
            topShadeRect.offsetMax = Vector2.zero;

            var bottomShade = UIFactory.CreatePanel(transform, "BottomShade", new Color(0f, 0f, 0f, 0.34f));
            var bottomShadeRect = bottomShade.GetComponent<RectTransform>();
            bottomShadeRect.anchorMin = new Vector2(0f, 0f);
            bottomShadeRect.anchorMax = new Vector2(1f, 0.42f);
            bottomShadeRect.offsetMin = Vector2.zero;
            bottomShadeRect.offsetMax = Vector2.zero;

            var contentRoot = UIFactory.CreateContainer(transform, "ContentRoot", new Vector2(0.14f, 0.18f), new Vector2(0.86f, 0.8f), Vector2.zero, Vector2.zero);
            titleText = UIFactory.CreateText(contentRoot, "TitleText", string.Empty, 30, TextAnchor.MiddleCenter, new Color(0.96f, 0.93f, 0.86f, 1f));
            titleText.rectTransform.anchorMin = new Vector2(0f, 0.74f);
            titleText.rectTransform.anchorMax = new Vector2(1f, 0.88f);
            titleText.rectTransform.offsetMin = Vector2.zero;
            titleText.rectTransform.offsetMax = Vector2.zero;

            contentText = UIFactory.CreateText(contentRoot, "ContentText", string.Empty, 28, TextAnchor.UpperLeft, new Color(0.96f, 0.94f, 0.88f, 1f));
            contentText.supportRichText = true;
            contentText.lineSpacing = 1.35f;
            contentText.rectTransform.anchorMin = new Vector2(0f, 0.08f);
            contentText.rectTransform.anchorMax = new Vector2(1f, 0.72f);
            contentText.rectTransform.offsetMin = new Vector2(24f, 0f);
            contentText.rectTransform.offsetMax = new Vector2(-24f, 0f);

            hintText = UIFactory.CreateText(transform, "HintText", string.Empty, 18, TextAnchor.LowerCenter, new Color(0.78f, 0.75f, 0.68f, 0.92f));
            hintText.rectTransform.anchorMin = new Vector2(0f, 0.04f);
            hintText.rectTransform.anchorMax = new Vector2(1f, 0.12f);
            hintText.rectTransform.offsetMin = Vector2.zero;
            hintText.rectTransform.offsetMax = Vector2.zero;
            hintText.gameObject.SetActive(false);
        }

        private bool TryBindExistingUi()
        {
            rootImage = GetComponent<Image>();
            titleText = FindText("ContentRoot/TitleText");
            contentText = FindText("ContentRoot/ContentText");
            hintText = FindText("HintText");
            return rootImage != null && titleText != null && contentText != null && hintText != null;
        }

        private Text FindText(string path)
        {
            var child = transform.Find(path);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private static string ResolveText(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            return LocalizationManager.GetText(key);
        }

        private static string ResolveUiText(string key, string fallback)
        {
            var resolved = LocalizationManager.GetText(key);
            if (!string.Equals(resolved, key))
            {
                return resolved;
            }

            return fallback;
        }
    }
}
