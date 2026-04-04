using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wuxing.UI
{
    public class UICardBrowserPopup : UIPopup
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private RectTransform cardContentRoot;
        [SerializeField] private Button cardTemplateButton;
        [SerializeField] private Text detailTitleText;
        [SerializeField] private Text detailBodyText;
        [SerializeField] private Button closeButton;

        private readonly List<Button> cardButtons = new List<Button>();
        private IList<UICardData> currentCards;
        private Button selectedButton;

        private void Awake()
        {
            if (closeButton != null)
            {
                UIFactory.ApplyStandardButtonChrome(closeButton);
                closeButton.onClick.AddListener(ClosePopup);
            }

            if (detailBodyText != null)
            {
                detailBodyText.supportRichText = true;
                detailBodyText.alignment = TextAnchor.UpperLeft;
                detailBodyText.fontSize = 16;
            }

            if (subtitleText != null)
            {
                subtitleText.gameObject.SetActive(false);
            }

            if (cardTemplateButton != null)
            {
                cardTemplateButton.gameObject.SetActive(false);
            }

            ApplyStaticLayout();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ClosePopup);
            }
        }

        public void Setup(string title, string subtitle, IList<UICardData> cards)
        {
            const float cardWidth = UICardChromeUtility.StandardCardWidth;
            const float cardHeight = UICardChromeUtility.StandardCardHeight;
            const float cardSpacingX = 16f;
            const float cardSpacingY = 16f;
            const int maxColumns = 4;

            currentCards = cards;
            selectedButton = null;
            if (titleText != null) titleText.text = title;
            if (subtitleText != null)
            {
                subtitleText.text = string.Empty;
                subtitleText.gameObject.SetActive(false);
            }

            for (var i = 0; i < cardButtons.Count; i++)
            {
                if (cardButtons[i] != null)
                {
                    cardButtons[i].gameObject.SetActive(false);
                }
            }

            if (cards == null || cards.Count == 0)
            {
                if (cardContentRoot != null)
                {
                    cardContentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);
                }

                ApplyDetail(null);
                return;
            }

            for (var i = 0; i < cards.Count; i++)
            {
                var button = GetOrCreateCardButton(i);
                BindCard(button, cards[i], i);

                var rect = button.GetComponent<RectTransform>();
                if (rect != null)
                {
                    var row = i / maxColumns;
                    var column = i % maxColumns;
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition = new Vector2(column * (cardWidth + cardSpacingX), -row * (cardHeight + cardSpacingY));
                    rect.sizeDelta = new Vector2(cardWidth, cardHeight);
                }
            }

            if (cardContentRoot != null)
            {
                cardContentRoot.anchorMin = new Vector2(0f, 1f);
                cardContentRoot.anchorMax = new Vector2(0f, 1f);
                cardContentRoot.pivot = new Vector2(0f, 1f);
                cardContentRoot.anchoredPosition = Vector2.zero;
                var columns = Mathf.Min(maxColumns, Mathf.Max(1, cards.Count));
                var rows = Mathf.CeilToInt(cards.Count / (float)maxColumns);
                cardContentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cardWidth * columns + Mathf.Max(0, columns - 1) * cardSpacingX);
                cardContentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(cardHeight, rows * cardHeight + Mathf.Max(0, rows - 1) * cardSpacingY));
            }

            ApplyDetail(cards[0]);
            ApplySelection(cardButtons[0], cards[0]);
        }

        private void ApplyStaticLayout()
        {
            if (titleText != null)
            {
                titleText.fontSize = 36;
                titleText.alignment = TextAnchor.UpperLeft;
                titleText.rectTransform.anchorMin = new Vector2(0.04f, 0.92f);
                titleText.rectTransform.anchorMax = new Vector2(0.52f, 0.98f);
                titleText.rectTransform.offsetMin = Vector2.zero;
                titleText.rectTransform.offsetMax = Vector2.zero;
            }

            if (closeButton != null)
            {
                var closeRect = closeButton.GetComponent<RectTransform>();
                if (closeRect != null)
                {
                    closeRect.anchorMin = new Vector2(0.86f, 0.915f);
                    closeRect.anchorMax = new Vector2(0.96f, 0.972f);
                    closeRect.offsetMin = Vector2.zero;
                    closeRect.offsetMax = Vector2.zero;
                }

                var closeLabel = closeButton.GetComponentInChildren<Text>();
                if (closeLabel != null)
                {
                    closeLabel.fontSize = 18;
                    closeLabel.alignment = TextAnchor.MiddleCenter;
                }
            }

            if (cardContentRoot != null && cardContentRoot.parent != null && cardContentRoot.parent.parent != null)
            {
                var leftScrollRoot = cardContentRoot.parent.parent.parent as RectTransform;
                if (leftScrollRoot != null)
                {
                    leftScrollRoot.anchorMin = new Vector2(0.04f, 0.36f);
                    leftScrollRoot.anchorMax = new Vector2(0.96f, 0.84f);
                    leftScrollRoot.offsetMin = Vector2.zero;
                    leftScrollRoot.offsetMax = Vector2.zero;
                }
            }

            if (detailTitleText != null && detailTitleText.transform.parent != null)
            {
                var detailPanel = detailTitleText.transform.parent as RectTransform;
                if (detailPanel != null)
                {
                    detailPanel.anchorMin = new Vector2(0.04f, 0.08f);
                    detailPanel.anchorMax = new Vector2(0.96f, 0.29f);
                    detailPanel.offsetMin = Vector2.zero;
                    detailPanel.offsetMax = Vector2.zero;
                }

                detailTitleText.fontSize = 26;
                detailTitleText.alignment = TextAnchor.UpperLeft;
                detailTitleText.rectTransform.anchorMin = new Vector2(0.04f, 0.66f);
                detailTitleText.rectTransform.anchorMax = new Vector2(0.96f, 0.92f);
                detailTitleText.rectTransform.offsetMin = Vector2.zero;
                detailTitleText.rectTransform.offsetMax = Vector2.zero;
            }

            if (detailBodyText != null)
            {
                detailBodyText.rectTransform.anchorMin = new Vector2(0.04f, 0.08f);
                detailBodyText.rectTransform.anchorMax = new Vector2(0.96f, 0.6f);
                detailBodyText.rectTransform.offsetMin = Vector2.zero;
                detailBodyText.rectTransform.offsetMax = Vector2.zero;
            }
        }

        private Button GetOrCreateCardButton(int index)
        {
            while (cardButtons.Count <= index)
            {
                var clone = Instantiate(cardTemplateButton.gameObject, cardContentRoot, false);
                clone.name = "Card_" + (cardButtons.Count + 1);
                clone.SetActive(false);
                cardButtons.Add(clone.GetComponent<Button>());
            }

            return cardButtons[index];
        }

        private void BindCard(Button button, UICardData card, int index)
        {
            if (button == null || card == null)
            {
                return;
            }

            button.gameObject.SetActive(true);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate
            {
                var nextCard = currentCards != null && index < currentCards.Count ? currentCards[index] : null;
                ApplyDetail(nextCard);
                ApplySelection(button, nextCard);
            });

            ApplyCardChrome(button, card.BorderColor, selectedButton == button);

            var title = button.transform.Find("TitleText")?.GetComponent<Text>();
            if (title != null)
            {
                title.text = card.Title;
                title.supportRichText = true;
            }

            var subtitle = button.transform.Find("SubtitleText")?.GetComponent<Text>();
            if (subtitle != null)
            {
                subtitle.text = card.Subtitle;
                subtitle.supportRichText = true;
            }

            var progressRoot = button.transform.Find("ProgressRoot");
            if (progressRoot != null)
            {
                progressRoot.gameObject.SetActive(card.ShowProgress);
            }

            var progressFill = button.transform.Find("ProgressRoot/Fill")?.GetComponent<Image>();
            if (progressFill != null)
            {
                var maxValue = Mathf.Max(1, card.ProgressMax);
                progressFill.fillAmount = Mathf.Clamp01(card.ProgressCurrent / (float)maxValue);
                progressFill.color = card.BorderColor;
            }

            var progressLabel = button.transform.Find("ProgressRoot/ProgressLabel")?.GetComponent<Text>();
            if (progressLabel != null)
            {
                progressLabel.text = card.ProgressLabel ?? string.Empty;
                progressLabel.gameObject.SetActive(card.ShowProgress && !string.IsNullOrEmpty(card.ProgressLabel));
            }
        }

        private void ApplySelection(Button button, UICardData card)
        {
            selectedButton = button;
            for (var i = 0; i < cardButtons.Count; i++)
            {
                var current = cardButtons[i];
                if (current == null || !current.gameObject.activeSelf)
                {
                    continue;
                }

                var currentCard = currentCards != null && i < currentCards.Count ? currentCards[i] : null;
                ApplyCardChrome(current, currentCard != null ? currentCard.BorderColor : Color.white, current == selectedButton);
            }

            if (button != null && card != null)
            {
                ApplyDetail(card);
            }
        }

        private static void ApplyCardChrome(Button button, Color borderColor, bool selected)
        {
            UICardChromeUtility.Apply(button, borderColor, selected);
        }

        private void ApplyDetail(UICardData card)
        {
            if (detailTitleText != null)
            {
                detailTitleText.text = card != null ? card.DetailTitle : string.Empty;
                detailTitleText.color = card != null ? card.BorderColor : Color.white;
            }

            if (detailBodyText != null)
            {
                detailBodyText.text = card != null ? card.DetailBody : string.Empty;
            }
        }

        private static void ClosePopup()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.CloseTopPopup();
            }
        }
    }
}

