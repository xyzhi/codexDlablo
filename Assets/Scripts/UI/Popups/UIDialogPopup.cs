using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wuxing.Config;
using Wuxing.Game;
using Wuxing.Localization;

namespace Wuxing.UI
{
    public class UIDialogPopup : UIPopup, IPointerClickHandler
    {
        private static readonly Color ActiveSpeakerColor = new Color(0.9f, 0.82f, 0.66f, 1f);
        private static readonly Color InactiveSpeakerColor = new Color(0.52f, 0.5f, 0.48f, 1f);

        private Image rootImage;
        private Text leftSpeakerText;
        private Text rightSpeakerText;
        private Text contentText;
        private Text hintText;
        private RectTransform choiceRoot;
        private Button choiceTemplateButton;
        private readonly List<Button> choiceButtons = new List<Button>();
        private readonly List<string> activeChoiceIds = new List<string>();
        private string fullContent;
        private float typingSpeed;
        private float revealProgress;
        private bool typingCompleted;

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

            var leftSpeaker = ResolveText(string.IsNullOrEmpty(node.LeftSpeakerKey) ? node.SpeakerKey : node.LeftSpeakerKey);
            var rightSpeaker = ResolveText(node.RightSpeakerKey);
            var activeSide = ResolveSpeakerSide(node.ActiveSpeakerSide, leftSpeaker, rightSpeaker);

            leftSpeakerText.text = leftSpeaker;
            rightSpeakerText.text = rightSpeaker;
            bool showLeft = !string.IsNullOrEmpty(leftSpeaker) && activeSide != StorySpeakerSide.Right;
            bool showRight = !string.IsNullOrEmpty(rightSpeaker) && activeSide == StorySpeakerSide.Right;
            leftSpeakerText.gameObject.SetActive(showLeft);
            rightSpeakerText.gameObject.SetActive(showRight);
            leftSpeakerText.color = activeSide == StorySpeakerSide.Right ? InactiveSpeakerColor : ActiveSpeakerColor;
            rightSpeakerText.color = activeSide == StorySpeakerSide.Right ? ActiveSpeakerColor : InactiveSpeakerColor;
            fullContent = ResolveText(node.ContentKey);
            typingSpeed = Mathf.Max(1f, node.TypingCharsPerSecond > 0 ? node.TypingCharsPerSecond : 30f);
            revealProgress = 0f;
            typingCompleted = string.IsNullOrEmpty(fullContent);
            contentText.text = typingCompleted ? fullContent : string.Empty;
            hintText.text = ResolveUiText("dialog.continue_hint", "Tap anywhere to continue");
            RefreshChoices();
            UpdateChoiceVisibility();
        }

        private void Update()
        {
            if (typingCompleted || string.IsNullOrEmpty(fullContent))
            {
                return;
            }

            revealProgress += typingSpeed * Time.unscaledDeltaTime;
            var visibleCount = Mathf.Clamp(Mathf.FloorToInt(revealProgress), 0, fullContent.Length);
            contentText.text = fullContent.Substring(0, visibleCount);
            if (visibleCount >= fullContent.Length)
            {
                typingCompleted = true;
                UpdateChoiceVisibility();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!typingCompleted)
            {
                typingCompleted = true;
                contentText.text = fullContent;
                typingCompleted = true;
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
                if (choiceTemplateButton != null)
                {
                    choiceTemplateButton.gameObject.SetActive(false);
                }

                if (choiceRoot == null)
                {
                    var dialogBox = transform.Find("DialogBox");
                    if (dialogBox != null)
                    {
                        choiceRoot = UIFactory.CreateContainer(dialogBox, "ChoiceRoot", new Vector2(0f, 0.02f), new Vector2(1f, 0.28f), new Vector2(24f, 0f), new Vector2(-24f, 0f));
                    }
                }
                return;
            }

            gameObject.name = "DialogPopup";
            rootImage = gameObject.GetComponent<Image>();
            if (rootImage == null)
            {
                rootImage = gameObject.AddComponent<Image>();
            }

            rootImage.color = new Color(0f, 0f, 0f, 0.18f);
            rootImage.raycastTarget = true;

            var rootRect = GetComponent<RectTransform>();
            if (rootRect != null)
            {
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
            }

            var box = UIFactory.CreatePanel(transform, "DialogBox", new Color(0.05f, 0.05f, 0.07f, 0.94f));
            var boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.08f, 0.06f);
            boxRect.anchorMax = new Vector2(0.92f, 0.34f);
            boxRect.offsetMin = Vector2.zero;
            boxRect.offsetMax = Vector2.zero;

            var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(box.transform, false);
            var borderRect = border.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-2f, -2f);
            borderRect.offsetMax = new Vector2(2f, 2f);
            var borderImage = border.GetComponent<Image>();
            borderImage.color = new Color(0.54f, 0.4f, 0.25f, 1f);
            border.transform.SetAsFirstSibling();

            leftSpeakerText = UIFactory.CreateText(box.transform, "LeftSpeakerText", string.Empty, 22, TextAnchor.UpperLeft, ActiveSpeakerColor);
            leftSpeakerText.rectTransform.anchorMin = new Vector2(0f, 0.78f);
            leftSpeakerText.rectTransform.anchorMax = new Vector2(0.45f, 0.96f);
            leftSpeakerText.rectTransform.offsetMin = new Vector2(24f, 0f);
            leftSpeakerText.rectTransform.offsetMax = new Vector2(-12f, 0f);

            rightSpeakerText = UIFactory.CreateText(box.transform, "RightSpeakerText", string.Empty, 22, TextAnchor.UpperRight, InactiveSpeakerColor);
            rightSpeakerText.rectTransform.anchorMin = new Vector2(0.55f, 0.78f);
            rightSpeakerText.rectTransform.anchorMax = new Vector2(1f, 0.96f);
            rightSpeakerText.rectTransform.offsetMin = new Vector2(12f, 0f);
            rightSpeakerText.rectTransform.offsetMax = new Vector2(-24f, 0f);

            contentText = UIFactory.CreateText(box.transform, "ContentText", string.Empty, 24, TextAnchor.UpperLeft, new Color(0.95f, 0.94f, 0.88f, 1f));
            contentText.lineSpacing = 1.25f;
            contentText.rectTransform.anchorMin = new Vector2(0f, 0.18f);
            contentText.rectTransform.anchorMax = new Vector2(1f, 0.74f);
            contentText.rectTransform.offsetMin = new Vector2(24f, 0f);
            contentText.rectTransform.offsetMax = new Vector2(-24f, 0f);

            hintText = UIFactory.CreateText(box.transform, "HintText", string.Empty, 16, TextAnchor.LowerRight, new Color(0.76f, 0.74f, 0.7f, 0.92f));
            hintText.rectTransform.anchorMin = new Vector2(0.5f, 0.02f);
            hintText.rectTransform.anchorMax = new Vector2(1f, 0.16f);
            hintText.rectTransform.offsetMin = new Vector2(12f, 0f);
            hintText.rectTransform.offsetMax = new Vector2(-24f, 0f);
            hintText.gameObject.SetActive(false);

            choiceRoot = UIFactory.CreateContainer(box.transform, "ChoiceRoot", new Vector2(0f, 0.02f), new Vector2(1f, 0.28f), new Vector2(24f, 0f), new Vector2(-24f, 0f));
            choiceTemplateButton = UIFactory.CreateButton(choiceRoot, "TemplateButton", string.Empty, delegate { });
            choiceTemplateButton.gameObject.SetActive(false);
        }

        private bool TryBindExistingUi()
        {
            rootImage = GetComponent<Image>();
            leftSpeakerText = FindText("DialogBox/LeftSpeakerText") ?? FindText("DialogBox/SpeakerText");
            rightSpeakerText = FindText("DialogBox/RightSpeakerText") ?? FindText("DialogBox/TitleText");
            contentText = FindText("DialogBox/ContentText");
            hintText = FindText("DialogBox/HintText");
            var choiceTransform = transform.Find("DialogBox/ChoiceRoot");
            choiceRoot = choiceTransform != null ? choiceTransform.GetComponent<RectTransform>() : null;
            choiceTemplateButton = FindButton("DialogBox/ChoiceRoot/TemplateButton")
                ?? FindButton("DialogBox/TemplateButton")
                ?? FindButton("TemplateButton");
            return rootImage != null && leftSpeakerText != null && rightSpeakerText != null && contentText != null && hintText != null;
        }

        private Text FindText(string path)
        {
            var child = transform.Find(path);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private Button FindButton(string path)
        {
            var child = transform.Find(path);
            return child != null ? child.GetComponent<Button>() : null;
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

        private static StorySpeakerSide ResolveSpeakerSide(string rawValue, string leftSpeaker, string rightSpeaker)
        {
            if (string.Equals(rawValue, "Right", System.StringComparison.OrdinalIgnoreCase))
            {
                return StorySpeakerSide.Right;
            }

            if (string.Equals(rawValue, "Left", System.StringComparison.OrdinalIgnoreCase))
            {
                return StorySpeakerSide.Left;
            }

            if (string.IsNullOrEmpty(leftSpeaker) && !string.IsNullOrEmpty(rightSpeaker))
            {
                return StorySpeakerSide.Right;
            }

            return StorySpeakerSide.Left;
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
                    var localizedLabel = label.GetComponent<LocalizedText>();
                    if (localizedLabel != null)
                    {
                        localizedLabel.SetKey(choice.TitleKey);
                    }
                    else
                    {
                        label.text = ResolveText(choice.TitleKey);
                    }

                    label.supportRichText = true;
                }

                var rect = button.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 1f);
                    rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    var buttonHeight = Mathf.Max(60f, rect.sizeDelta.y);
                    rect.anchoredPosition = new Vector2(0f, -i * (buttonHeight + 8f));
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
                Button button;
                if (choiceTemplateButton != null)
                {
                    var clone = Instantiate(choiceTemplateButton.gameObject, choiceRoot, false);
                    clone.name = "ChoiceButton" + choiceButtons.Count;
                    button = clone.GetComponent<Button>();
                }
                else
                {
                    button = UIFactory.CreateButton(choiceRoot, "ChoiceButton" + choiceButtons.Count, string.Empty, delegate { });
                }

                choiceButtons.Add(button);
            }

            return choiceButtons[index];
        }

        private void UpdateChoiceVisibility()
        {
            var hasChoices = activeChoiceIds.Count > 0;
            if (choiceRoot != null)
            {
                choiceRoot.gameObject.SetActive(typingCompleted && hasChoices);
            }

            hintText.gameObject.SetActive(typingCompleted && !hasChoices);
        }

        private enum StorySpeakerSide
        {
            Left,
            Right
        }
    }
}
