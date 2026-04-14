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
        private const float CardIllustrationMaskInset = 12f;

        [SerializeField] private Text titleText;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform slotContentRoot;
        [SerializeField] private Button slotTemplateButton;
        [SerializeField] private RectTransform libraryContentRoot;
        [SerializeField] private Button libraryTemplateButton;
        [SerializeField] private Text detailTitleText;
        [SerializeField] private Text detailBodyText;
        [SerializeField] private Sprite skillCardSprite;
        [SerializeField] private Sprite tipsBackgroundSprite;
        [SerializeField] private Sprite commonButtonSprite;

        private readonly List<Button> slotButtons = new List<Button>();
        private readonly List<Button> libraryButtons = new List<Button>();

        private string characterId = string.Empty;
        private int selectedSlotIndex;
        private string selectedSkillId = string.Empty;

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
            HideLegacyDetailPanel();
            HideTips();
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return;
            }

            selectedSkillId = GetSlotSkillId(selectedSlotIndex);
            RefreshSlotButtons();
            RefreshLibraryButtons();
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

        private List<SkillCardData> BuildLibraryCards()
        {
            var result = new List<SkillCardData>();
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
                    DetailTitle = card.DetailTitle,
                    DetailBody = card.DetailBody,
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
            var subtitle = card != null ? card.Title : "空技能栏";
            var borderColor = card != null ? card.BorderColor : UIElementPalette.GetBorderColor("None");

            UICardChromeUtility.Apply(button, borderColor, false);
            ApplyCardIllustration(button, skillCardSprite);
            SetCardTexts(button, title, subtitle);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate
            {
                selectedSlotIndex = slotIndex;
                selectedSkillId = GetSlotSkillId(slotIndex);
                RefreshAll();

                ShowTips(
                    button.GetComponent<RectTransform>(),
                    card != null && !string.IsNullOrEmpty(card.DetailTitle) ? card.DetailTitle : title,
                    card != null ? card.DetailBody : BuildEmptySlotDetail(slotIndex),
                    card != null ? "卸下" : null,
                    card != null ? (Action)delegate
                    {
                        GameProgressManager.UnequipActiveSkill(characterId, selectedSlotIndex);
                        HideTips();
                        RefreshAll();
                    } : null);
            });
        }

        private void ConfigureLibraryCard(Button button, SkillCardData card)
        {
            if (button == null || card == null)
            {
                return;
            }

            UICardChromeUtility.Apply(button, card.BorderColor, false);
            ApplyCardIllustration(button, skillCardSprite);
            SetCardTexts(button, card.Title, card.Subtitle);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate
            {
                var actionLabel = card.IsUnequip ? "卸下" : "装备";
                var action = card.IsUnequip
                    ? (Action)delegate
                    {
                        GameProgressManager.UnequipActiveSkill(characterId, selectedSlotIndex);
                        HideTips();
                        RefreshAll();
                    }
                    : (Action)delegate
                    {
                        GameProgressManager.EquipActiveSkill(characterId, selectedSlotIndex, card.SkillId);
                        HideTips();
                        RefreshAll();
                    };

                ShowTips(
                    button.GetComponent<RectTransform>(),
                    string.IsNullOrEmpty(card.DetailTitle) ? card.Title : card.DetailTitle,
                    BuildLibraryTipBody(card),
                    card.IsUnequip && string.IsNullOrEmpty(selectedSkillId) ? null : actionLabel,
                    card.IsUnequip && string.IsNullOrEmpty(selectedSkillId) ? null : action);
            });
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
                libraryContentRoot);
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
            const float maxBubbleHeight = 520f;
            const float horizontalGap = 28f;
            const float screenPadding = 18f;
            const float topPadding = 26f;
            const float sidePadding = 38f;
            const float buttonSpacing = 18f;
            const float bottomPadding = 26f;
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
            var bodyHeight = Mathf.Clamp(tipsBodyText.preferredHeight, 72f, 340f);
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

            if (libraryContentRoot != null)
            {
                siblingIndex = Mathf.Min(siblingIndex, libraryContentRoot.GetSiblingIndex());
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

        private string BuildLibraryTipBody(SkillCardData card)
        {
            if (card == null)
            {
                return string.Empty;
            }

            if (card.IsUnequip)
            {
                return BuildUnequipDetail();
            }

            return "目标槽位：" + GetSlotLabel(selectedSlotIndex) + "\n\n" + (card.DetailBody ?? string.Empty);
        }

        private string BuildUnequipDetail()
        {
            if (string.IsNullOrEmpty(selectedSkillId))
            {
                return "当前槽位为空，没有可卸下的功法。";
            }

            var cards = GameProgressManager.BuildEquippedActiveSkillCards(characterId, false);
            if (selectedSlotIndex >= 0 && selectedSlotIndex < cards.Count && cards[selectedSlotIndex] != null)
            {
                return "当前槽位：" + GetSlotLabel(selectedSlotIndex) + "\n\n" + cards[selectedSlotIndex].DetailBody;
            }

            return "当前槽位：" + GetSlotLabel(selectedSlotIndex);
        }

        private static string BuildEmptySlotDetail(int slotIndex)
        {
            return "当前槽位：" + GetSlotLabel(slotIndex) + "\n\n尚未装备功法。点击下方功法卡片后，可在 tips 中执行装备。";
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
            return "技能栏" + (slotIndex + 1);
        }

        private static void SetCardTexts(Button button, string title, string subtitle)
        {
            var titleText = button.transform.Find("TitleText")?.GetComponent<Text>();
            if (titleText != null)
            {
                titleText.text = title ?? string.Empty;
                titleText.supportRichText = true;
                ConfigureCardTitleText(titleText);
            }

            var subtitleText = button.transform.Find("SubtitleText")?.GetComponent<Text>();
            if (subtitleText != null)
            {
                subtitleText.text = subtitle ?? string.Empty;
                subtitleText.supportRichText = true;
                ConfigureCardSubtitleText(subtitleText);
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

        private static void ConfigureCardTitleText(Text text)
        {
            if (text == null)
            {
                return;
            }

            EnsureTextOutline(text);
            text.color = new Color(0.98f, 0.96f, 0.9f, 1f);
        }

        private static void ConfigureCardSubtitleText(Text text)
        {
            if (text == null)
            {
                return;
            }

            EnsureTextOutline(text);
            text.color = new Color(0.96f, 0.94f, 0.88f, 1f);

            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.08f, 0.62f);
            rect.anchorMax = new Vector2(0.92f, 0.8f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureTextOutline(Text text)
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

            outline.effectColor = new Color(0.16f, 0.11f, 0.08f, 0.82f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            outline.useGraphicAlpha = true;
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
            public string DetailTitle;
            public string DetailBody;
            public Color BorderColor;
            public bool IsUnequip;
        }
    }
}
