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
        private const float CardIllustrationMaskInset = 12f;
        private static readonly string[] SlotOrder = { "Weapon", "Armor", "Accessory" };

        [SerializeField] private Text titleText;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform slotContentRoot;
        [SerializeField] private Button slotTemplateButton;
        [SerializeField] private RectTransform inventoryContentRoot;
        [SerializeField] private Button inventoryTemplateButton;
        [SerializeField] private Text detailTitleText;
        [SerializeField] private Text detailBodyText;
        [SerializeField] private Sprite equipmentCardSprite;
        [SerializeField] private Sprite tipsBackgroundSprite;
        [SerializeField] private Sprite commonButtonSprite;

        private readonly List<Button> slotButtons = new List<Button>();
        private readonly List<Button> inventoryButtons = new List<Button>();

        private string selectedSlot = "Weapon";
        private string selectedEquipmentInstanceId = string.Empty;
        private RectTransform tipsPanel;
        private RectTransform tipsArrow;
        private Button tipsDismissButton;
        private Text tipsTitleText;
        private Text tipsBodyText;
        private Button tipsActionButton;
        private Text tipsActionButtonText;
        private Action pendingTipAction;
        private UICardTipsView sharedTipsView;

        private void Awake()
        {
            if (closeButton != null)
            {
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

            HideLegacyDetailPanel();
            HideTips();
            RefreshAll();
        }

        private void RefreshAll()
        {
            selectedEquipmentInstanceId = BattleManager.GetEquippedPlayerEquipmentInstanceId(PlayerUnitIndex, selectedSlot) ?? string.Empty;
            RefreshSlotButtons();
            RefreshInventoryButtons();
        }

        private void RefreshSlotButtons()
        {
            for (var i = 0; i < SlotOrder.Length; i++)
            {
                var slot = SlotOrder[i];
                var button = GetOrCreateSlotButton(i);
                ConfigureCardButtonRect(button, i, 0);
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
                ConfigureCardButtonRect(button, i % 4, i / 4);
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

            var equippedInstanceId = BattleManager.GetEquippedPlayerEquipmentInstanceId(PlayerUnitIndex, slot);
            var equippedConfig = BattleManager.GetOwnedEquipmentConfigByInstance(equippedInstanceId);
            var accentColor = equippedConfig != null ? UIElementPalette.GetQualityColor(equippedConfig.Quality) : Color.white;
            UICardChromeUtility.Apply(button, accentColor, false);
            ApplyCardIllustration(button, equipmentCardSprite);
            SetCardTexts(button, GetSlotDisplayName(slot), BattleManager.GetPlayerEquipmentName(PlayerUnitIndex, slot));

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate
            {
                selectedSlot = slot;
                selectedEquipmentInstanceId = BattleManager.GetEquippedPlayerEquipmentInstanceId(PlayerUnitIndex, slot) ?? string.Empty;
                RefreshAll();

                ShowTips(
                    button.GetComponent<RectTransform>(),
                    equippedConfig != null ? equippedConfig.Name : GetSlotDisplayName(slot),
                    equippedConfig != null ? BuildEquipmentDetail(equippedConfig) : BuildEmptySlotDetail(),
                    equippedConfig != null ? "卸下" : null,
                    equippedConfig != null ? (Action)delegate
                    {
                        BattleManager.UnequipPlayerEquipmentForUnitIndexSlot(PlayerUnitIndex, selectedSlot);
                        HideTips();
                        RefreshAll();
                    } : null);
            });
        }

        private void ConfigureInventoryCard(Button button, EquipmentCardData card)
        {
            if (button == null || card == null)
            {
                return;
            }

            UICardChromeUtility.Apply(button, card.BorderColor, false);
            ApplyCardIllustration(button, equipmentCardSprite);
            SetCardTexts(button, card.Title, card.Subtitle);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate
            {
                var actionLabel = card.IsUnequip ? "卸下" : "装备";
                var action = card.IsUnequip
                    ? (Action)delegate
                    {
                        BattleManager.UnequipPlayerEquipmentForUnitIndexSlot(PlayerUnitIndex, selectedSlot);
                        HideTips();
                        RefreshAll();
                    }
                    : (Action)delegate
                    {
                        BattleManager.EquipOwnedItemForUnitIndex(PlayerUnitIndex, card.EquipmentInstanceId);
                        HideTips();
                        RefreshAll();
                    };

                ShowTips(
                    button.GetComponent<RectTransform>(),
                    string.IsNullOrEmpty(card.DetailTitle) ? card.Title : card.DetailTitle,
                    BuildInventoryTipBody(card),
                    card.IsUnequip && string.IsNullOrEmpty(selectedEquipmentInstanceId) ? null : actionLabel,
                    card.IsUnequip && string.IsNullOrEmpty(selectedEquipmentInstanceId) ? null : action);
            });
        }

        private List<EquipmentCardData> BuildInventoryCardsForSelectedSlot()
        {
            var result = new List<EquipmentCardData>();
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
                    Subtitle = BuildEquipmentCardSubtitle(equipment),
                    EquipmentInstanceId = instance.InstanceId,
                    BorderColor = UIElementPalette.GetQualityColor(equipment.Quality),
                    DetailTitle = equipment.Name,
                    DetailBody = BuildEquipmentDetail(equipment),
                    IsUnequip = false
                });
            }

            return result;
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
            else
            {
                if (detailTitleText != null)
                {
                    detailTitleText.gameObject.SetActive(false);
                }

                if (detailBodyText != null)
                {
                    detailBodyText.gameObject.SetActive(false);
                }
            }
        }

        private void ShowTips(RectTransform sourceCard, string title, string body, string actionLabel, Action action)
        {
            EnsureTipsUI();
            if (sharedTipsView == null || sourceCard == null)
            {
                return;
            }

            sharedTipsView.Show(sourceCard, title, body, actionLabel, action);
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
            GameObject instance;
            if (prefab != null)
            {
                instance = Instantiate(prefab, root, false);
            }
            else
            {
                instance = new GameObject("CardTips", typeof(RectTransform), typeof(UICardTipsView));
                instance.transform.SetParent(root, false);
            }

            sharedTipsView = instance.GetComponent<UICardTipsView>();
            if (sharedTipsView == null)
            {
                sharedTipsView = instance.AddComponent<UICardTipsView>();
            }

            sharedTipsView.Bind(
                root,
                tipsBackgroundSprite,
                commonButtonSprite,
                titleText != null ? titleText.font : null,
                detailBodyText != null ? detailBodyText.font : titleText != null ? titleText.font : null,
                closeButton != null ? closeButton.GetComponent<RectTransform>() : null,
                slotContentRoot,
                inventoryContentRoot);
        }

        private void LayoutTips(RectTransform sourceCard, bool hasAction)
        {
            var root = transform as RectTransform;
            if (root == null || tipsPanel == null || sourceCard == null)
            {
                return;
            }

            const float bubbleWidth = 380f;
            const float minBubbleHeight = 180f;
            const float maxBubbleHeight = 420f;
            const float horizontalGap = 28f;
            const float screenPadding = 18f;
            const float topPadding = 26f;
            const float sidePadding = 38f;
            const float buttonSpacing = 16f;
            const float bottomPadding = 22f;
            const float titleHeight = 34f;
            const float buttonHeight = 44f;
            const float arrowSize = 24f;

            var rootRect = root.rect;
            var corners = new Vector3[4];
            sourceCard.GetWorldCorners(corners);
            var localBottomLeft = root.InverseTransformPoint(corners[0]);
            var localTopRight = root.InverseTransformPoint(corners[2]);
            var cardCenterX = (localBottomLeft.x + localTopRight.x) * 0.5f;
            var cardCenterY = (localBottomLeft.y + localTopRight.y) * 0.5f;

            var bodyWidth = bubbleWidth - sidePadding * 2f;
            tipsTitleText.rectTransform.sizeDelta = new Vector2(bodyWidth, titleHeight);
            tipsTitleText.rectTransform.anchoredPosition = new Vector2(sidePadding, -topPadding);

            tipsBodyText.rectTransform.sizeDelta = new Vector2(bodyWidth, 100f);
            tipsBodyText.rectTransform.anchoredPosition = new Vector2(sidePadding, -(topPadding + titleHeight + 10f));
            tipsBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;

            Canvas.ForceUpdateCanvases();
            var bodyHeight = Mathf.Clamp(tipsBodyText.preferredHeight, 72f, 250f);
            tipsBodyText.rectTransform.sizeDelta = new Vector2(bodyWidth, bodyHeight);

            var actionHeight = hasAction ? buttonHeight + buttonSpacing : 0f;
            var bubbleHeight = Mathf.Clamp(topPadding + titleHeight + 10f + bodyHeight + actionHeight + bottomPadding, minBubbleHeight, maxBubbleHeight);
            tipsPanel.sizeDelta = new Vector2(bubbleWidth, bubbleHeight);

            tipsActionButton.gameObject.SetActive(hasAction);
            if (hasAction)
            {
                tipsActionButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 18f);
            }

            var preferRight = true;
            var candidateCenterX = localTopRight.x + horizontalGap + bubbleWidth * 0.5f;
            if (candidateCenterX + bubbleWidth * 0.5f > rootRect.xMax - screenPadding)
            {
                preferRight = false;
                candidateCenterX = localBottomLeft.x - horizontalGap - bubbleWidth * 0.5f;
            }

            candidateCenterX = Mathf.Clamp(candidateCenterX, rootRect.xMin + screenPadding + bubbleWidth * 0.5f, rootRect.xMax - screenPadding - bubbleWidth * 0.5f);
            var candidateCenterY = Mathf.Clamp(cardCenterY, rootRect.yMin + screenPadding + bubbleHeight * 0.5f, rootRect.yMax - screenPadding - bubbleHeight * 0.5f);
            tipsPanel.anchoredPosition = new Vector2(candidateCenterX, candidateCenterY);

            var arrowX = preferRight
                ? candidateCenterX - bubbleWidth * 0.5f - arrowSize * 0.42f
                : candidateCenterX + bubbleWidth * 0.5f + arrowSize * 0.42f;
            var arrowY = Mathf.Clamp(cardCenterY, candidateCenterY - bubbleHeight * 0.5f + 28f, candidateCenterY + bubbleHeight * 0.5f - 28f);
            tipsArrow.anchoredPosition = new Vector2(arrowX, arrowY);
            tipsArrow.localRotation = Quaternion.Euler(0f, 0f, preferRight ? 45f : 225f);
        }

        private void UpdateDismissLayerOrder()
        {
            if (tipsDismissButton == null)
            {
                return;
            }

            var dismissTransform = tipsDismissButton.transform;
            var siblingIndex = Mathf.Max(0, transform.childCount - 1);

            if (slotContentRoot != null)
            {
                siblingIndex = Mathf.Min(siblingIndex, slotContentRoot.GetSiblingIndex());
            }

            if (inventoryContentRoot != null)
            {
                siblingIndex = Mathf.Min(siblingIndex, inventoryContentRoot.GetSiblingIndex());
            }

            dismissTransform.SetSiblingIndex(siblingIndex);
        }

        private void HandleTipsActionButtonClicked()
        {
            if (pendingTipAction == null)
            {
                return;
            }

            pendingTipAction.Invoke();
        }

        private static Text CreateTipsText(string name, RectTransform parent, Font font, int fontSize, FontStyle fontStyle)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);

            var text = textObject.GetComponent<Text>();
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.supportRichText = true;
            text.raycastTarget = false;
            return text;
        }

        private static void EnsureTipsOutline(Text text, Color color, Vector2 distance)
        {
            if (text == null)
            {
                return;
            }

            var outline = text.GetComponent<Outline>();
            if (outline == null)
            {
                outline = text.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private string BuildInventoryTipBody(EquipmentCardData card)
        {
            if (card == null)
            {
                return string.Empty;
            }

            if (card.IsUnequip)
            {
                return BuildUnequipDetail();
            }

            return LocalizationManager.GetText("equipment.detail.slot")
                + GetSlotDisplayName(selectedSlot)
                + "\n\n"
                + (card.DetailBody ?? string.Empty);
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
            rect.offsetMin = new Vector2(CardIllustrationMaskInset, CardIllustrationMaskInset);
            rect.offsetMax = new Vector2(-CardIllustrationMaskInset, -CardIllustrationMaskInset);
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
            imageObject.transform.SetSiblingIndex(0);

            var rect = imageObject.GetComponent<RectTransform>();
            ConfigureCardIllustrationImageRect(rect);

            var image = imageObject.GetComponent<Image>();
            image.preserveAspect = false;
            image.raycastTarget = false;
            return image;
        }

        private static void ConfigureCardIllustrationImageRect(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(-0.08f, -0.08f);
            rect.anchorMax = new Vector2(1.08f, 1.08f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private string BuildEmptySlotDetail()
        {
            var builder = new StringBuilder();
            builder.Append(LocalizationManager.GetText("equipment.detail.slot"))
                .Append(GetSlotDisplayName(selectedSlot))
                .Append('\n')
                .Append(LocalizationManager.GetText("battle.equipment_none"))
                .Append("\n\n点击下方装备卡片后，可在 tips 中执行装备。");
            return builder.ToString();
        }

        private string BuildUnequipDetail()
        {
            if (string.IsNullOrEmpty(selectedEquipmentInstanceId))
            {
                return BuildEmptySlotDetail();
            }

            var equipment = BattleManager.GetOwnedEquipmentConfigByInstance(selectedEquipmentInstanceId);
            return equipment != null ? BuildEquipmentDetail(equipment) : BuildEmptySlotDetail();
        }

        private static string BuildEquipmentDetail(EquipmentConfig equipment)
        {
            var builder = new StringBuilder();
            builder.Append(LocalizationManager.GetText("equipment.detail.quality"))
                .Append(GetEquipmentQualityLabel(equipment.Quality))
                .Append('\n')
                .Append(LocalizationManager.GetText("equipment.detail.level"))
                .Append(GetEquipmentLevelLabel(equipment.Level))
                .Append('\n')
                .Append(LocalizationManager.GetText("equipment.detail.slot"))
                .Append(GetSlotDisplayName(equipment.Slot));

            if (equipment.HP != 0)
            {
                builder.Append('\n').Append(LocalizationManager.GetText("equipment.stat.hp")).Append(' ').Append(FormatSignedValue(equipment.HP));
            }

            if (equipment.ATK != 0)
            {
                builder.Append('\n').Append(LocalizationManager.GetText("equipment.stat.atk")).Append(' ').Append(FormatSignedValue(equipment.ATK));
            }

            if (equipment.DEF != 0)
            {
                builder.Append('\n').Append(LocalizationManager.GetText("equipment.stat.def")).Append(' ').Append(FormatSignedValue(equipment.DEF));
            }

            if (equipment.MP != 0)
            {
                builder.Append('\n').Append(LocalizationManager.GetText("equipment.stat.mp")).Append(' ').Append(FormatSignedValue(equipment.MP));
            }

            if (!string.IsNullOrEmpty(equipment.Notes))
            {
                builder.Append('\n').Append(LocalizationManager.GetText("equipment.detail.notes")).Append(equipment.Notes);
            }

            return builder.ToString();
        }

        private static string GetSlotDisplayName(string slot)
        {
            switch ((slot ?? string.Empty).Trim())
            {
                case "Weapon":
                    return LocalizationManager.GetText("equipment.slot.weapon");
                case "Armor":
                    return LocalizationManager.GetText("equipment.slot.armor");
                case "Accessory":
                    return LocalizationManager.GetText("equipment.slot.accessory");
                default:
                    return LocalizationManager.GetText("equipment.slot.unknown");
            }
        }

        private static string BuildEquipmentCardSubtitle(EquipmentConfig equipment)
        {
            return GetEquipmentQualityLabel(equipment != null ? equipment.Quality : string.Empty)
                + " / "
                + GetEquipmentLevelLabel(equipment != null ? equipment.Level : 1);
        }

        private static string GetEquipmentQualityLabel(string quality)
        {
            switch (NormalizeEquipmentQuality(quality))
            {
                case "green":
                    return LocalizationManager.GetText("equipment.quality.green");
                case "blue":
                    return LocalizationManager.GetText("equipment.quality.blue");
                case "purple":
                    return LocalizationManager.GetText("equipment.quality.purple");
                case "gold":
                    return LocalizationManager.GetText("equipment.quality.gold");
                case "white":
                default:
                    return LocalizationManager.GetText("equipment.quality.white");
            }
        }

        private static string GetEquipmentLevelLabel(int level)
        {
            switch (Mathf.Clamp(level, 1, 5))
            {
                case 1:
                    return LocalizationManager.GetText("equipment.level.1");
                case 2:
                    return LocalizationManager.GetText("equipment.level.2");
                case 3:
                    return LocalizationManager.GetText("equipment.level.3");
                case 4:
                    return LocalizationManager.GetText("equipment.level.4");
                case 5:
                    return LocalizationManager.GetText("equipment.level.5");
                default:
                    return LocalizationManager.GetText("equipment.level.1");
            }
        }

        private static string NormalizeEquipmentQuality(string quality)
        {
            switch ((quality ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "白":
                case "white":
                    return "white";
                case "绿":
                case "green":
                    return "green";
                case "蓝":
                case "blue":
                    return "blue";
                case "紫":
                case "purple":
                    return "purple";
                case "金":
                case "gold":
                    return "gold";
                default:
                    return "white";
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
            public string DetailTitle;
            public string DetailBody;
            public Color BorderColor;
            public bool IsUnequip;
        }
    }
}
