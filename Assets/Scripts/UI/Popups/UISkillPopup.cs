using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wuxing.Game;
using Wuxing.Localization;

namespace Wuxing.UI
{
    public class UISkillPopup : UIPopup
    {
        private const int MaxActiveSkillSlots = 3;

        [SerializeField] private Text titleText;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform slotContentRoot;
        [SerializeField] private Button slotTemplateButton;
        [SerializeField] private RectTransform libraryContentRoot;
        [SerializeField] private Button libraryTemplateButton;
        [SerializeField] private Text detailTitleText;
        [SerializeField] private Text detailBodyText;

        private readonly List<Button> slotButtons = new List<Button>();
        private readonly List<Button> libraryButtons = new List<Button>();

        private string characterId = string.Empty;
        private int selectedSlotIndex;
        private string selectedSkillId = string.Empty;

        private void Awake()
        {
            if (closeButton != null)
            {
                UIFactory.ApplyStandardButtonChrome(closeButton);
                closeButton.onClick.AddListener(ClosePopup);
            }

            if (slotTemplateButton != null)
            {
                slotTemplateButton.gameObject.SetActive(false);
            }

            if (libraryTemplateButton != null)
            {
                libraryTemplateButton.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ClosePopup);
            }
        }

        public override void OnOpen(object data)
        {
            base.OnOpen(data);
            characterId = data as string;
            if (string.IsNullOrEmpty(characterId))
            {
                characterId = GameProgressManager.GetPrimaryCharacterId();
            }

            if (titleText != null)
            {
                titleText.text = LocalizationManager.GetText("map.skill_overview_title");
            }

            selectedSlotIndex = Mathf.Clamp(selectedSlotIndex, 0, MaxActiveSkillSlots - 1);
            selectedSkillId = GetSlotSkillId(selectedSlotIndex);
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return;
            }

            RefreshSlotButtons();
            RefreshLibraryButtons();
            RefreshDetail();
        }

        private void RefreshSlotButtons()
        {
            var cards = GameProgressManager.BuildEquippedActiveSkillCards(characterId, false);
            for (var i = 0; i < MaxActiveSkillSlots; i++)
            {
                var button = GetOrCreateSlotButton(i);
                ConfigureCardButtonRect(button, i, 0);
                ConfigureSlotCard(button, i, i < cards.Count ? cards[i] : null);
                button.gameObject.SetActive(true);
            }

            for (var i = MaxActiveSkillSlots; i < slotButtons.Count; i++)
            {
                if (slotButtons[i] != null)
                {
                    slotButtons[i].gameObject.SetActive(false);
                }
            }

            if (slotContentRoot != null)
            {
                slotContentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, UICardChromeUtility.StandardCardHeight);
            }
        }

        private void RefreshLibraryButtons()
        {
            var cards = BuildLibraryCards();
            for (var i = 0; i < cards.Count; i++)
            {
                var button = GetOrCreateLibraryButton(i);
                ConfigureCardButtonRect(button, i % 4, i / 4);
                ConfigureLibraryCard(button, cards[i]);
                button.gameObject.SetActive(true);
            }

            for (var i = cards.Count; i < libraryButtons.Count; i++)
            {
                if (libraryButtons[i] != null)
                {
                    libraryButtons[i].gameObject.SetActive(false);
                }
            }

            if (libraryContentRoot != null)
            {
                var rows = Mathf.Max(1, Mathf.CeilToInt(cards.Count / 4f));
                var totalHeight = rows * UICardChromeUtility.StandardCardHeight + Mathf.Max(0, rows - 1) * 16f;
                libraryContentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);
            }
        }

        private void RefreshDetail()
        {
            if (detailTitleText == null || detailBodyText == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(selectedSkillId))
            {
                detailTitleText.text = LocalizationManager.GetText("map.skill_overview_title");
                detailTitleText.color = Color.white;
                detailBodyText.text = "\u5f53\u524d\u69fd\u4f4d\u4e3a\u7a7a\u3002\n\u4ece\u4e0b\u65b9\u6280\u80fd\u5e93\u9009\u62e9\u4e00\u4e2a\u4e3b\u52a8\u6280\u80fd\u4e0a\u573a\u3002";
                return;
            }

            var cards = GameProgressManager.BuildSkillLibraryCards(characterId, false);
            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card != null && string.Equals(card.Id, selectedSkillId, StringComparison.OrdinalIgnoreCase))
                {
                    detailTitleText.text = card.DetailTitle;
                    detailTitleText.color = card.BorderColor;
                    detailBodyText.text = card.DetailBody;
                    return;
                }
            }

            detailTitleText.text = LocalizationManager.GetText("map.skill_overview_title");
            detailTitleText.color = Color.white;
            detailBodyText.text = "\u672a\u627e\u5230\u6280\u80fd\u8be6\u60c5\u3002";
        }

        private List<SkillCardData> BuildLibraryCards()
        {
            var result = new List<SkillCardData>();
            result.Add(new SkillCardData
            {
                Title = "\u5378\u4e0b\u6280\u80fd",
                Subtitle = GetSlotLabel(selectedSlotIndex),
                SkillId = string.Empty,
                BorderColor = new Color(0.88f, 0.82f, 0.76f, 1f),
                IsUnequip = true
            });

            var cards = GameProgressManager.BuildSkillLibraryCards(characterId, false);
            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null || string.IsNullOrEmpty(card.Id) || !GameProgressManager.CanEquipActiveSkill(characterId, card.Id))
                {
                    continue;
                }

                result.Add(new SkillCardData
                {
                    Title = card.Title,
                    Subtitle = card.Subtitle,
                    SkillId = card.Id,
                    BorderColor = card.BorderColor,
                    IsUnequip = false
                });
            }

            return result;
        }

        private void ConfigureSlotCard(Button button, int slotIndex, UICardData card)
        {
            if (button == null)
            {
                return;
            }

            var title = GetSlotLabel(slotIndex);
            var subtitle = card != null ? card.Title : "\u7a7a\u6280\u80fd\u680f";
            var borderColor = card != null ? card.BorderColor : UIElementPalette.GetBorderColor("None");
            var isSelected = selectedSlotIndex == slotIndex;

            UICardChromeUtility.Apply(button, borderColor, isSelected);
            SetCardTexts(button, title, subtitle);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate
            {
                selectedSlotIndex = slotIndex;
                selectedSkillId = GetSlotSkillId(slotIndex);
                RefreshAll();
            });
        }

        private void ConfigureLibraryCard(Button button, SkillCardData card)
        {
            if (button == null || card == null)
            {
                return;
            }

            var isSelected = card.IsUnequip
                ? string.IsNullOrEmpty(selectedSkillId)
                : string.Equals(selectedSkillId, card.SkillId, StringComparison.OrdinalIgnoreCase);
            UICardChromeUtility.Apply(button, card.BorderColor, isSelected);
            SetCardTexts(button, card.Title, card.Subtitle);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate
            {
                if (card.IsUnequip)
                {
                    GameProgressManager.UnequipActiveSkill(characterId, selectedSlotIndex);
                    selectedSkillId = string.Empty;
                }
                else if (!string.IsNullOrEmpty(card.SkillId))
                {
                    GameProgressManager.EquipActiveSkill(characterId, selectedSlotIndex, card.SkillId);
                    selectedSkillId = card.SkillId;
                }

                RefreshAll();
            });
        }

        private Button GetOrCreateSlotButton(int index)
        {
            while (slotButtons.Count <= index)
            {
                var clone = Instantiate(slotTemplateButton.gameObject, slotContentRoot, false);
                clone.name = "SkillSlot_" + slotButtons.Count;
                clone.SetActive(false);
                slotButtons.Add(clone.GetComponent<Button>());
            }

            return slotButtons[index];
        }

        private Button GetOrCreateLibraryButton(int index)
        {
            while (libraryButtons.Count <= index)
            {
                var clone = Instantiate(libraryTemplateButton.gameObject, libraryContentRoot, false);
                clone.name = "SkillLibrary_" + libraryButtons.Count;
                clone.SetActive(false);
                libraryButtons.Add(clone.GetComponent<Button>());
            }

            return libraryButtons[index];
        }

        private string GetSlotSkillId(int slotIndex)
        {
            var equipped = GameProgressManager.GetEquippedActiveSkillIds(characterId);
            return slotIndex >= 0 && slotIndex < equipped.Count ? equipped[slotIndex] ?? string.Empty : string.Empty;
        }

        private static string GetSlotLabel(int slotIndex)
        {
            return "\u6280\u80fd\u680f" + (slotIndex + 1);
        }

        private static void SetCardTexts(Button button, string title, string subtitle)
        {
            var titleText = button.transform.Find("TitleText")?.GetComponent<Text>();
            if (titleText != null)
            {
                titleText.text = title ?? string.Empty;
                titleText.supportRichText = true;
            }

            var subtitleText = button.transform.Find("SubtitleText")?.GetComponent<Text>();
            if (subtitleText != null)
            {
                subtitleText.text = subtitle ?? string.Empty;
                subtitleText.supportRichText = true;
            }

            var progressRoot = button.transform.Find("ProgressRoot");
            if (progressRoot != null)
            {
                progressRoot.gameObject.SetActive(false);
            }
        }

        private static void ConfigureCardButtonRect(Button button, int column, int row)
        {
            var rect = button.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(UICardChromeUtility.StandardCardWidth, UICardChromeUtility.StandardCardHeight);
            rect.anchoredPosition = new Vector2(column * (UICardChromeUtility.StandardCardWidth + 16f), -row * (UICardChromeUtility.StandardCardHeight + 16f));
        }

        private static void ClosePopup()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.CloseTopPopup();
            }
        }

        private class SkillCardData
        {
            public string Title;
            public string Subtitle;
            public string SkillId;
            public Color BorderColor;
            public bool IsUnequip;
        }
    }
}
