using System.Collections.Generic;
using DG.Tweening;
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
        private RectTransform contentRootRect;
        private Text titleText;
        private Text contentText;
        private Text hintText;
        private RectTransform choiceRoot;
        private readonly List<Button> choiceButtons = new List<Button>();
        private readonly List<string> activeChoiceIds = new List<string>();
        private string fullContent;
        private float typingSpeed;
        private float skipHintDelay;
        private float revealProgress;
        private float openElapsed;
        private bool typingCompleted;
        private bool canSkipTyping;
        private bool unfoldCompleted = true;
        private Sequence unfoldSequence;
        private Vector2 contentRootExpandedSizeDelta;
        private bool hasContentRootExpandedSize;

        [Header("Unfold Animation")]
        [SerializeField] private float unfoldDuration = 0.85f;
        [SerializeField] private float foldedHeight = 56f;
        [SerializeField] private Ease unfoldEase = Ease.OutCubic;

        private void Awake()
        {
            EnsureUi();
        }

        private void OnDestroy()
        {
            KillUnfoldAnimation(false);
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
            unfoldCompleted = false;

            titleText.text = string.IsNullOrEmpty(title) ? string.Empty : title;
            titleText.gameObject.SetActive(false);
            contentText.text = string.Empty;
            contentText.gameObject.SetActive(false);
            hintText.gameObject.SetActive(false);
            hintText.text = ResolveUiText("story.skip_hint", "Tap anywhere to skip");
            RefreshChoices();
            UpdateChoiceVisibility();
            PlayUnfoldAnimation(!string.IsNullOrEmpty(title));
        }

        public override void OnClose()
        {
            KillUnfoldAnimation(true);
            fullContent = string.Empty;
            contentText.text = string.Empty;
            contentText.gameObject.SetActive(true);
            hintText.gameObject.SetActive(false);
            if (choiceRoot != null)
            {
                choiceRoot.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!unfoldCompleted)
            {
                return;
            }

            if (string.IsNullOrEmpty(fullContent) || typingCompleted)
            {
                openElapsed += Time.unscaledDeltaTime;
                if (canSkipTyping && !hintText.gameObject.activeSelf && openElapsed >= skipHintDelay)
                {
                    UpdateChoiceVisibility();
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
                UpdateChoiceVisibility();
            }

            if (canSkipTyping && !hintText.gameObject.activeSelf && openElapsed >= skipHintDelay)
            {
                UpdateChoiceVisibility();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!unfoldCompleted)
            {
                return;
            }

            if (!typingCompleted)
            {
                if (!canSkipTyping)
                {
                    return;
                }

                typingCompleted = true;
                contentText.text = fullContent;
                UpdateChoiceVisibility();
                return;
            }

            if (activeChoiceIds.Count > 0)
            {
                return;
            }

            StoryManager.AdvanceCurrentNode();
        }

        private void EnsureUi()
        {
            if (TryBindExistingUi())
            {
                if (choiceRoot == null)
                {
                    choiceRoot = UIFactory.CreateContainer(transform, "ChoiceRoot", new Vector2(0.18f, 0.1f), new Vector2(0.82f, 0.3f), Vector2.zero, Vector2.zero);
                }
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
            contentRootRect = contentRoot.GetComponent<RectTransform>();
            titleText = UIFactory.CreateText(contentRoot, "TitleText", string.Empty, 30, TextAnchor.MiddleCenter, new Color(0.23f, 0.14f, 0.07f, 1f));
            titleText.rectTransform.anchorMin = new Vector2(0f, 0.76f);
            titleText.rectTransform.anchorMax = new Vector2(1f, 0.9f);
            titleText.rectTransform.offsetMin = Vector2.zero;
            titleText.rectTransform.offsetMax = Vector2.zero;

            contentText = UIFactory.CreateText(contentRoot, "ContentText", string.Empty, 28, TextAnchor.UpperLeft, new Color(0.28f, 0.18f, 0.1f, 1f));
            contentText.supportRichText = true;
            contentText.lineSpacing = 1.35f;
            contentText.rectTransform.anchorMin = new Vector2(0f, 0.28f);
            contentText.rectTransform.anchorMax = new Vector2(1f, 0.65f);
            contentText.rectTransform.offsetMin = new Vector2(48f, 0f);
            contentText.rectTransform.offsetMax = new Vector2(-48f, 0f);

            hintText = UIFactory.CreateText(contentRoot, "HintText", string.Empty, 18, TextAnchor.MiddleCenter, new Color(0.38f, 0.25f, 0.14f, 0.86f));
            hintText.rectTransform.anchorMin = new Vector2(0f, 0.22f);
            hintText.rectTransform.anchorMax = new Vector2(1f, 0.28f);
            hintText.rectTransform.offsetMin = new Vector2(16f, 0f);
            hintText.rectTransform.offsetMax = new Vector2(-16f, 0f);
            hintText.gameObject.SetActive(false);

            choiceRoot = UIFactory.CreateContainer(transform, "ChoiceRoot", new Vector2(0.18f, 0.1f), new Vector2(0.82f, 0.3f), Vector2.zero, Vector2.zero);
        }

        private bool TryBindExistingUi()
        {
            rootImage = GetComponent<Image>();
            var contentRoot = transform.Find("ContentRoot");
            contentRootRect = contentRoot != null ? contentRoot.GetComponent<RectTransform>() : null;
            titleText = FindText("ContentRoot/TitleText");
            contentText = FindText("ContentRoot/ContentText");
            hintText = FindText("ContentRoot/HintText");
            if (hintText == null)
            {
                hintText = FindText("HintText");
            }
            var choiceTransform = transform.Find("ChoiceRoot");
            choiceRoot = choiceTransform != null ? choiceTransform.GetComponent<RectTransform>() : null;
            return rootImage != null && contentRootRect != null && titleText != null && contentText != null && hintText != null;
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

        private void RefreshChoices()
        {
            activeChoiceIds.Clear();
            var choices = StoryManager.GetCurrentChoices();
            if (choiceRoot == null)
            {
                return;
            }

            for (var i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                if (choice == null)
                {
                    continue;
                }

                var button = GetOrCreateChoiceButton(i);
                var capturedChoiceId = choice.Id;
                button.gameObject.SetActive(true);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(delegate { StoryManager.SelectChoice(capturedChoiceId); });

                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = ResolveText(choice.TitleKey);
                    label.alignment = TextAnchor.MiddleLeft;
                    label.fontSize = 18;
                }

                var rect = button.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = new Vector2(0f, -i * 72f);
                    rect.sizeDelta = new Vector2(0f, 60f);
                }

                activeChoiceIds.Add(choice.Id);
            }

            for (var i = activeChoiceIds.Count; i < choiceButtons.Count; i++)
            {
                if (choiceButtons[i] != null)
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private Button GetOrCreateChoiceButton(int index)
        {
            while (choiceButtons.Count <= index)
            {
                var button = UIFactory.CreateButton(choiceRoot, "ChoiceButton" + choiceButtons.Count, string.Empty, delegate { });
                choiceButtons.Add(button);
            }

            return choiceButtons[index];
        }

        private void UpdateChoiceVisibility()
        {
            var hasChoices = activeChoiceIds.Count > 0;
            if (choiceRoot != null)
            {
                choiceRoot.gameObject.SetActive(unfoldCompleted && typingCompleted && hasChoices);
            }

            var choicesVisible = typingCompleted && hasChoices;
            hintText.gameObject.SetActive(unfoldCompleted && !choicesVisible && canSkipTyping && openElapsed >= skipHintDelay);
        }

        private void PlayUnfoldAnimation(bool showTitleAfterUnfold)
        {
            if (contentRootRect == null)
            {
                FinishUnfold(showTitleAfterUnfold);
                return;
            }

            KillUnfoldAnimation(false);
            Canvas.ForceUpdateCanvases();

            contentRootExpandedSizeDelta = contentRootRect.sizeDelta;
            hasContentRootExpandedSize = true;
            var expandedHeight = contentRootRect.rect.height;
            var startHeight = Mathf.Clamp(foldedHeight, 1f, expandedHeight);
            if (expandedHeight <= startHeight)
            {
                FinishUnfold(showTitleAfterUnfold);
                return;
            }

            SetContentRootHeight(startHeight);

            var currentHeight = startHeight;
            unfoldSequence = DOTween.Sequence().SetUpdate(true);
            unfoldSequence.Append(DOTween.To(
                    () => currentHeight,
                    value =>
                    {
                        currentHeight = value;
                        SetContentRootHeight(value);
                    },
                    expandedHeight,
                    Mathf.Max(0.01f, unfoldDuration))
                .SetEase(unfoldEase));
            unfoldSequence.OnComplete(() =>
            {
                contentRootRect.sizeDelta = contentRootExpandedSizeDelta;
                FinishUnfold(showTitleAfterUnfold);
            });
        }

        private void FinishUnfold(bool showTitle)
        {
            unfoldCompleted = true;
            openElapsed = skipHintDelay;
            titleText.gameObject.SetActive(showTitle);
            contentText.gameObject.SetActive(true);
            contentText.text = typingCompleted ? fullContent : string.Empty;
            UpdateChoiceVisibility();
        }

        private void KillUnfoldAnimation(bool restoreExpandedHeight)
        {
            if (unfoldSequence != null && unfoldSequence.IsActive())
            {
                unfoldSequence.Kill();
            }

            unfoldSequence = null;
            if (restoreExpandedHeight && contentRootRect != null && hasContentRootExpandedSize)
            {
                contentRootRect.sizeDelta = contentRootExpandedSizeDelta;
            }
        }

        private void SetContentRootHeight(float height)
        {
            if (contentRootRect == null)
            {
                return;
            }

            contentRootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }
    }
}
