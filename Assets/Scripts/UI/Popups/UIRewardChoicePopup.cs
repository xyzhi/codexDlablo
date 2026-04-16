using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wuxing.Game;

namespace Wuxing.UI
{
    public class UIRewardChoicePopup : UIPopup
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private RectTransform cardContentRoot;
        [SerializeField] private Button cardTemplateButton;
        [SerializeField] private Text detailTitleText;
        [SerializeField] private Text detailBodyText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Sprite skillCardSprite;
        [SerializeField] private Sprite tipsBackgroundSprite;
        [SerializeField] private Sprite commonButtonSprite;

        private readonly List<RectTransform> cardSlots = new List<RectTransform>();
        private readonly List<Button> slotButtons = new List<Button>();
        private IList<SkillRewardOption> currentOptions;
        private IList<Action> currentActions;
        private bool currentEnglish;
        private Action pendingSecondaryAction;
        private UICardTipsView sharedTipsView;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HandleSecondaryButtonClicked);
            }

            if (cardTemplateButton != null)
            {
                cardTemplateButton.gameObject.SetActive(false);
            }

            if (subtitleText != null)
            {
                subtitleText.supportRichText = true;
            }

            CollectCardSlots();
            HideLegacyDetailPanel();
            DisableScrollBehavior();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleSecondaryButtonClicked);
            }
        }

        public void SetupSkillRewards(
            string title,
            string message,
            IList<SkillRewardOption> options,
            IList<Action> actions,
            string secondaryLabel,
            Action secondaryAction,
            bool isEnglish)
        {
            currentOptions = options;
            currentActions = actions;
            currentEnglish = isEnglish;
            pendingSecondaryAction = secondaryAction;

            if (titleText != null)
            {
                titleText.text = title ?? string.Empty;
            }

            if (subtitleText != null)
            {
                var prompt = isEnglish
                    ? "Choose the skill you want to obtain."
                    : "\u8bf7\u9009\u62e9\u4f60\u83b7\u5f97\u7684\u6280\u80fd";
                subtitleText.text = prompt;
                subtitleText.gameObject.SetActive(true);
            }

            var visibleCount = Mathf.Min(cardSlots.Count, 3);
            var count = options != null ? Mathf.Min(options.Count, visibleCount) : 0;
            for (var i = 0; i < slotButtons.Count; i++)
            {
                if (slotButtons[i] != null)
                {
                    slotButtons[i].gameObject.SetActive(false);
                }
            }

            for (var i = 0; i < count; i++)
            {
                var button = GetOrCreateSlotButton(i);
                BindCard(button, options[i], i);
            }

            ConfigureSecondaryButton(secondaryLabel);
            HideTips();
        }

        private void BindCard(Button button, SkillRewardOption option, int index)
        {
            if (button == null || option == null)
            {
                return;
            }

            var card = BuildCard(option, currentEnglish);
            button.gameObject.SetActive(true);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate
            {
                ShowTips(
                    button.GetComponent<RectTransform>(),
                    card.DetailTitle,
                    card.DetailBody,
                    currentEnglish ? "Select" : "\u9009\u53d6",
                    delegate
                    {
                        HideTips();
                        if (currentActions != null && index >= 0 && index < currentActions.Count)
                        {
                            CloseSelf();
                            currentActions[index]?.Invoke();
                        }
                    });
            });

            UICardChromeUtility.Apply(button, card.BorderColor, false);
            ApplyCardIllustration(button, skillCardSprite);
            SetCardTexts(button, card.Title, card.Subtitle);

        }

        private static UICardData BuildCard(SkillRewardOption option, bool english)
        {
            var choiceText = GameProgressManager.BuildSkillRewardChoiceText(option, english) ?? string.Empty;
            var lines = choiceText.Split(new[] { '\n' }, StringSplitOptions.None);
            var title = lines.Length > 0 ? lines[0] : choiceText;
            var subtitle = lines.Length > 1 ? lines[1] : string.Empty;

            return new UICardData
            {
                Title = title,
                Subtitle = subtitle,
                DetailTitle = option != null ? option.SkillName + "  Lv." + option.ResultLevel : string.Empty,
                DetailBody = GameProgressManager.BuildSkillRewardDetail(option, english),
                BorderColor = option != null ? UIElementPalette.GetQualityColor(option.SkillQuality) : Color.white
            };
        }

        private void ShowTips(RectTransform sourceCard, string title, string body, string actionLabel, Action action)
        {
            EnsureTipsUI();
            if (sharedTipsView == null || sourceCard == null)
            {
                return;
            }

            var resolvedActionLabel = currentEnglish ? actionLabel : "\u9009\u53d6";
            sharedTipsView.Show(sourceCard, title, body, resolvedActionLabel, action);
        }

        private void HideTips()
        {
            if (sharedTipsView != null)
            {
                sharedTipsView.Hide();
            }
        }

        private void EnsureTipsUI()
        {
            if (sharedTipsView != null)
            {
                return;
            }

            var root = transform as RectTransform;
            if (root == null)
            {
                return;
            }

            var prefab = Resources.Load<GameObject>(UICardTipsView.ResourcePath);
            if (prefab == null)
            {
                return;
            }

            var instance = Instantiate(prefab, root, false);
            sharedTipsView = instance.GetComponent<UICardTipsView>();
            if (sharedTipsView == null)
            {
                return;
            }

            sharedTipsView.Bind(
                root,
                tipsBackgroundSprite,
                commonButtonSprite,
                titleText != null ? titleText.font : null,
                subtitleText != null ? subtitleText.font : titleText != null ? titleText.font : null,
                closeButton != null ? closeButton.GetComponent<RectTransform>() : null,
                cardContentRoot);
        }

        private void HideLegacyDetailPanel()
        {
            var detailRoot = detailTitleText != null
                ? detailTitleText.rectTransform.parent as RectTransform
                : detailBodyText != null ? detailBodyText.rectTransform.parent as RectTransform : null;
            if (detailRoot != null)
            {
                detailRoot.gameObject.SetActive(false);
            }
        }

        private void DisableScrollBehavior()
        {
            var scrollRect = cardContentRoot != null ? cardContentRoot.GetComponentInParent<ScrollRect>(true) : null;
            if (scrollRect != null)
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = false;
                scrollRect.horizontalScrollbar = null;
                scrollRect.verticalScrollbar = null;
                scrollRect.enabled = false;
            }

            if (cardContentRoot == null)
            {
                return;
            }

            var scrollbars = cardContentRoot.GetComponentsInParent<Scrollbar>(true);
            for (var i = 0; i < scrollbars.Length; i++)
            {
                if (scrollbars[i] != null)
                {
                    scrollbars[i].gameObject.SetActive(false);
                }
            }
        }

        private void ConfigureSecondaryButton(string label)
        {
            if (closeButton == null)
            {
                return;
            }

            var text = closeButton.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label ?? string.Empty;
                text.supportRichText = true;
            }

            closeButton.gameObject.SetActive(!string.IsNullOrEmpty(label));
        }

        private void HandleSecondaryButtonClicked()
        {
            HideTips();
            var action = pendingSecondaryAction;
            CloseSelf();
            action?.Invoke();
        }

        private void CollectCardSlots()
        {
            cardSlots.Clear();
            slotButtons.Clear();

            if (cardContentRoot == null)
            {
                return;
            }

            for (var i = 0; i < cardContentRoot.childCount; i++)
            {
                var child = cardContentRoot.GetChild(i) as RectTransform;
                if (child == null || !child.name.StartsWith("RewardSlot", StringComparison.Ordinal))
                {
                    continue;
                }

                cardSlots.Add(child);
                slotButtons.Add(child.GetComponentInChildren<Button>(true));
            }

            cardSlots.Sort(delegate(RectTransform left, RectTransform right)
            {
                return string.Compare(left != null ? left.name : string.Empty, right != null ? right.name : string.Empty, StringComparison.Ordinal);
            });

            slotButtons.Clear();
            for (var i = 0; i < cardSlots.Count; i++)
            {
                slotButtons.Add(cardSlots[i] != null ? cardSlots[i].GetComponentInChildren<Button>(true) : null);
            }
        }

        private Button GetOrCreateSlotButton(int index)
        {
            if (index < 0 || index >= cardSlots.Count)
            {
                return null;
            }

            var existing = index < slotButtons.Count ? slotButtons[index] : null;
            if (existing != null)
            {
                return existing;
            }

            if (cardTemplateButton == null)
            {
                return null;
            }

            var slot = cardSlots[index];
            var clone = Instantiate(cardTemplateButton.gameObject, slot, false);
            clone.name = "RewardCard_" + (index + 1);
            clone.SetActive(false);

            var rect = clone.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
            }

            var button = clone.GetComponent<Button>();
            while (slotButtons.Count <= index)
            {
                slotButtons.Add(null);
            }

            slotButtons[index] = button;
            return button;
        }

        private static void SetCardTexts(Button button, string title, string subtitle)
        {
            var titleLabel = button.transform.Find("TitleText")?.GetComponent<Text>();
            if (titleLabel != null)
            {
                titleLabel.text = title ?? string.Empty;
                titleLabel.supportRichText = true;
            }

            var subtitleLabel = button.transform.Find("SubtitleText")?.GetComponent<Text>();
            if (subtitleLabel != null)
            {
                subtitleLabel.text = subtitle ?? string.Empty;
                subtitleLabel.supportRichText = true;
            }

            var progressRoot = button.transform.Find("ProgressRoot");
            if (progressRoot != null)
            {
                progressRoot.gameObject.SetActive(false);
            }
        }

        private static void ApplyCardIllustration(Button button, Sprite sprite)
        {
            if (button == null)
            {
                return;
            }

            var maskRoot = EnsureCardIllustrationMask(button);
            if (maskRoot == null)
            {
                return;
            }

            var image = EnsureCardIllustrationImage(maskRoot);
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.enabled = sprite != null;
            image.color = Color.white;
        }

        private static Transform EnsureCardIllustrationMask(Button button)
        {
            var existing = button.transform.Find("CardIllustrationMask");
            if (existing != null)
            {
                ConfigureCardIllustrationMaskRect(existing as RectTransform);
                existing.SetSiblingIndex(Mathf.Min(3, button.transform.childCount - 1));

                var existingImage = existing.GetComponent<Image>();
                if (existingImage != null)
                {
                    existingImage.color = new Color(1f, 1f, 1f, 0.01f);
                    existingImage.raycastTarget = false;
                }

                if (existing.GetComponent<RectMask2D>() == null)
                {
                    existing.gameObject.AddComponent<RectMask2D>();
                }

                return existing;
            }

            var maskObject = new GameObject("CardIllustrationMask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            maskObject.transform.SetParent(button.transform, false);
            maskObject.transform.SetSiblingIndex(Mathf.Min(3, button.transform.childCount));

            ConfigureCardIllustrationMaskRect(maskObject.GetComponent<RectTransform>());

            var maskImage = maskObject.GetComponent<Image>();
            maskImage.color = new Color(1f, 1f, 1f, 0.01f);
            maskImage.raycastTarget = false;
            return maskObject.transform;
        }

        private static void ConfigureCardIllustrationMaskRect(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(13f, 13f);
            rect.offsetMax = new Vector2(-13f, -13f);
        }

        private static Image EnsureCardIllustrationImage(Transform maskRoot)
        {
            if (maskRoot == null)
            {
                return null;
            }

            var existing = maskRoot.Find("CardIllustration")?.GetComponent<Image>();
            if (existing != null)
            {
                ConfigureCardIllustrationImageRect(existing.rectTransform);
                existing.preserveAspect = false;
                existing.raycastTarget = false;
                return existing;
            }

            var imageObject = new GameObject("CardIllustration", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(maskRoot, false);
            var image = imageObject.GetComponent<Image>();
            image.preserveAspect = false;
            image.raycastTarget = false;
            ConfigureCardIllustrationImageRect(image.rectTransform);
            return image;
        }

        private static void ConfigureCardIllustrationImageRect(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(UICardChromeUtility.StandardCardWidth - 26f, UICardChromeUtility.StandardCardHeight - 26f);
            rect.anchoredPosition = Vector2.zero;
        }

        private static void CloseSelf()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.CloseTopPopup();
            }
        }
    }
}
