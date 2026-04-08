using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Wuxing.Battle;
using Wuxing.Config;
using Wuxing.Game;
using Wuxing.Localization;

namespace Wuxing.UI
{
    public class UIEquipmentPopup : UIPopup
    {
        private const int PlayerUnitIndex = 0;
        private static readonly string[] SlotOrder = { "Weapon", "Armor", "Accessory" };

        [SerializeField] private Text titleText;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform slotContentRoot;
        [SerializeField] private Button slotTemplateButton;
        [SerializeField] private RectTransform inventoryContentRoot;
        [SerializeField] private Button inventoryTemplateButton;
        [SerializeField] private Text detailTitleText;
        [SerializeField] private Text detailBodyText;

        private readonly List<Button> slotButtons = new List<Button>();
        private readonly List<Button> inventoryButtons = new List<Button>();

        private string selectedSlot = "Weapon";
        private string selectedEquipmentInstanceId = string.Empty;

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

            if (inventoryTemplateButton != null)
            {
                inventoryTemplateButton.gameObject.SetActive(false);
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
            if (titleText != null)
            {
                titleText.text = LocalizationManager.GetText("battle.equipment_title");
            }

            RefreshAll();
        }

        private void RefreshAll()
        {
            selectedEquipmentInstanceId = BattleManager.GetEquippedPlayerEquipmentInstanceId(PlayerUnitIndex, selectedSlot) ?? string.Empty;
            RefreshSlotButtons();
            RefreshInventoryButtons();
            RefreshDetail();
        }

        private void RefreshSlotButtons()
        {
            for (var i = 0; i < SlotOrder.Length; i++)
            {
                var slot = SlotOrder[i];
                var button = GetOrCreateSlotButton(i);
                ConfigureCardButtonRect(button, i, 0, 3);
                ConfigureSlotCard(button, slot);
                button.gameObject.SetActive(true);
            }

            for (var i = SlotOrder.Length; i < slotButtons.Count; i++)
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

        private void RefreshInventoryButtons()
        {
            var cards = BuildInventoryCardsForSelectedSlot();
            for (var i = 0; i < cards.Count; i++)
            {
                var button = GetOrCreateInventoryButton(i);
                ConfigureCardButtonRect(button, i % 4, i / 4, 4);
                ConfigureInventoryCard(button, cards[i]);
                button.gameObject.SetActive(true);
            }

            for (var i = cards.Count; i < inventoryButtons.Count; i++)
            {
                if (inventoryButtons[i] != null)
                {
                    inventoryButtons[i].gameObject.SetActive(false);
                }
            }

            if (inventoryContentRoot != null)
            {
                var rows = Mathf.Max(1, Mathf.CeilToInt(cards.Count / 4f));
                var totalHeight = rows * UICardChromeUtility.StandardCardHeight + Mathf.Max(0, rows - 1) * 16f;
                inventoryContentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);
            }
        }

        private void RefreshDetail()
        {
            if (detailTitleText == null || detailBodyText == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(selectedEquipmentInstanceId))
            {
                detailTitleText.text = LocalizationManager.GetText("battle.equipment_none");
                detailTitleText.color = Color.white;
                detailBodyText.text = BuildEmptySlotDetail();
                return;
            }

            var equipment = BattleManager.GetOwnedEquipmentConfigByInstance(selectedEquipmentInstanceId);
            if (equipment == null)
            {
                detailTitleText.text = LocalizationManager.GetText("battle.equipment_none");
                detailTitleText.color = Color.white;
                detailBodyText.text = BuildEmptySlotDetail();
                return;
            }

            detailTitleText.text = equipment.Name;
            detailTitleText.color = UIElementPalette.GetBorderColor(GetSlotElement(equipment.Slot));
            detailBodyText.text = BuildEquipmentDetail(equipment);
        }

        private Button GetOrCreateSlotButton(int index)
        {
            while (slotButtons.Count <= index)
            {
                var clone = Instantiate(slotTemplateButton.gameObject, slotContentRoot, false);
                clone.name = "Slot_" + slotButtons.Count;
                clone.SetActive(false);
                slotButtons.Add(clone.GetComponent<Button>());
            }

            return slotButtons[index];
        }

        private Button GetOrCreateInventoryButton(int index)
        {
            while (inventoryButtons.Count <= index)
            {
                var clone = Instantiate(inventoryTemplateButton.gameObject, inventoryContentRoot, false);
                clone.name = "Equip_" + inventoryButtons.Count;
                clone.SetActive(false);
                inventoryButtons.Add(clone.GetComponent<Button>());
            }

            return inventoryButtons[index];
        }

        private void ConfigureSlotCard(Button button, string slot)
        {
            if (button == null)
            {
                return;
            }

            var isSelected = string.Equals(selectedSlot, slot, StringComparison.OrdinalIgnoreCase);
            UICardChromeUtility.Apply(button, UIElementPalette.GetBorderColor(GetSlotElement(slot)), isSelected);
            SetCardTexts(button, GetSlotDisplayName(slot), BattleManager.GetPlayerEquipmentName(PlayerUnitIndex, slot));

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate
            {
                selectedSlot = slot;
                selectedEquipmentInstanceId = BattleManager.GetEquippedPlayerEquipmentInstanceId(PlayerUnitIndex, slot) ?? string.Empty;
                RefreshAll();
            });
        }

        private void ConfigureInventoryCard(Button button, EquipmentCardData card)
        {
            if (button == null || card == null)
            {
                return;
            }

            var isSelected = card.IsUnequip
                ? string.IsNullOrEmpty(selectedEquipmentInstanceId)
                : string.Equals(selectedEquipmentInstanceId, card.EquipmentInstanceId, StringComparison.OrdinalIgnoreCase);
            UICardChromeUtility.Apply(button, card.BorderColor, isSelected);
            SetCardTexts(button, card.Title, card.Subtitle);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate
            {
                if (card.IsUnequip)
                {
                    BattleManager.UnequipPlayerEquipmentForUnitIndexSlot(PlayerUnitIndex, selectedSlot);
                    selectedEquipmentInstanceId = string.Empty;
                }
                else if (!string.IsNullOrEmpty(card.EquipmentInstanceId))
                {
                    BattleManager.EquipOwnedItemForUnitIndex(PlayerUnitIndex, card.EquipmentInstanceId);
                    selectedEquipmentInstanceId = card.EquipmentInstanceId;
                }

                RefreshAll();
            });
        }

        private List<EquipmentCardData> BuildInventoryCardsForSelectedSlot()
        {
            var result = new List<EquipmentCardData>();
            result.Add(new EquipmentCardData
            {
                Title = "\u5378\u4e0b\u88c5\u5907",
                Subtitle = GetSlotDisplayName(selectedSlot),
                EquipmentInstanceId = string.Empty,
                BorderColor = new Color(0.88f, 0.82f, 0.76f, 1f),
                IsUnequip = true
            });

            var instances = BattleManager.GetOwnedEquipmentInstancesForSlot(selectedSlot);
            var database = EquipmentDatabaseLoader.Load();
            for (var i = 0; i < instances.Count; i++)
            {
                var instance = instances[i];
                if (instance == null || database == null)
                {
                    continue;
                }

                var equipment = database.GetById(instance.EquipmentId);
                if (equipment == null)
                {
                    continue;
                }

                result.Add(new EquipmentCardData
                {
                    Title = equipment.Name,
                    Subtitle = GetSlotDisplayName(equipment.Slot),
                    EquipmentInstanceId = instance.InstanceId,
                    BorderColor = UIElementPalette.GetBorderColor(GetSlotElement(equipment.Slot)),
                    IsUnequip = false
                });
            }

            return result;
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

        private static void ConfigureCardButtonRect(Button button, int column, int row, int maxColumns)
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

        private string BuildEmptySlotDetail()
        {
            var builder = new StringBuilder();
            builder.Append("\u90e8\u4f4d\uff1a")
                .Append(GetSlotDisplayName(selectedSlot))
                .Append('\n')
                .Append(LocalizationManager.GetText("battle.equipment_none"));
            return builder.ToString();
        }

        private static string BuildEquipmentDetail(EquipmentConfig equipment)
        {
            var builder = new StringBuilder();
            builder.Append("\u90e8\u4f4d\uff1a")
                .Append(GetSlotDisplayName(equipment.Slot));

            if (equipment.HP != 0)
            {
                builder.Append('\n').Append("\u751f\u547d ").Append(FormatSignedValue(equipment.HP));
            }

            if (equipment.ATK != 0)
            {
                builder.Append('\n').Append("\u653b\u51fb ").Append(FormatSignedValue(equipment.ATK));
            }

            if (equipment.DEF != 0)
            {
                builder.Append('\n').Append("\u9632\u5fa1 ").Append(FormatSignedValue(equipment.DEF));
            }

            if (equipment.MP != 0)
            {
                builder.Append('\n').Append("\u6cd5\u529b ").Append(FormatSignedValue(equipment.MP));
            }

            if (!string.IsNullOrEmpty(equipment.Notes))
            {
                builder.Append('\n').Append("\u8bf4\u660e\uff1a").Append(equipment.Notes);
            }

            return builder.ToString();
        }

        private static string GetSlotDisplayName(string slot)
        {
            switch ((slot ?? string.Empty).Trim())
            {
                case "Weapon":
                    return "\u6b66\u5668";
                case "Armor":
                    return "\u62a4\u7532";
                case "Accessory":
                    return "\u9970\u54c1";
                default:
                    return "\u672a\u5206\u7c7b";
            }
        }

        private static string GetSlotElement(string slot)
        {
            switch ((slot ?? string.Empty).Trim())
            {
                case "Weapon":
                    return "Metal";
                case "Armor":
                    return "Earth";
                case "Accessory":
                    return "Water";
                default:
                    return "None";
            }
        }

        private static string FormatSignedValue(int value)
        {
            return value > 0 ? "+" + value : value.ToString();
        }

        private static void ClosePopup()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.CloseTopPopup();
            }
        }

        private class EquipmentCardData
        {
            public string Title;
            public string Subtitle;
            public string EquipmentInstanceId;
            public Color BorderColor;
            public bool IsUnequip;
        }
    }
}
