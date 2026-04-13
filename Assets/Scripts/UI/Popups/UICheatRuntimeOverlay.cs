﻿using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Wuxing.Config;
using Wuxing.Game;
using Wuxing.Localization;

namespace Wuxing.UI
{
    public class UICheatRuntimeOverlay : MonoBehaviour
    {
        private static readonly string[] Elements = { "Metal", "Wood", "Water", "Fire", "Earth" };

        private GameObject popupRoot;
        private Text titleText;
        private Text summaryText;
        private Text stonesSectionText;
        private Text progressSectionText;
        private Text loadoutSectionText;
        private Text elementText;
        private Text amountLabelText;
        private InputField amountInputField;
        private Text levelLabelText;
        private InputField levelInputField;
        private Text stageLabelText;
        private InputField stageInputField;
        private Text equipmentText;
        private Text characterText;
        private Text skillText;
        private Text skillLevelLabelText;
        private InputField skillLevelInputField;
        private Text runButtonText;
        private Text stoneButtonText;
        private Text all500ButtonText;
        private Text all5000ButtonText;
        private Text applyLevelButtonText;
        private Text jumpStageButtonText;
        private Text jumpLastButtonText;
        private Text grantEquipmentButtonText;
        private Text grantAllEquipmentButtonText;
        private Text grantSkillButtonText;
        private Text grantAllSkillLv1ButtonText;
        private Text grantAllSkillLv5ButtonText;
        private Text closeButtonText;

        private int elementIndex;
        private int amount = 100;
        private int targetLevel = 10;
        private int targetStage = 3;
        private int equipmentIndex;
        private int characterIndex;
        private int skillIndex;
        private int skillLevel = 1;

        public void Initialize(Transform popupLayer)
        {
            if (popupRoot != null)
            {
                return;
            }

            BuildEntryButton();
            BuildPopup(popupLayer);
            RefreshAll();
        }

        private void OnEnable()
        {
            GameProgressManager.ProgressChanged += RefreshAll;
            LocalizationManager.LanguageChanged += RefreshAll;
        }

        private void OnDisable()
        {
            GameProgressManager.ProgressChanged -= RefreshAll;
            LocalizationManager.LanguageChanged -= RefreshAll;
        }

        private void BuildEntryButton()
        {
            var entryButton = new GameObject("CheatEntryButton", typeof(RectTransform), typeof(Image), typeof(Button));
            entryButton.transform.SetParent(transform, false);

            var rect = entryButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-14f, -84f);
            rect.sizeDelta = new Vector2(56f, 56f);

            var image = entryButton.GetComponent<Image>();
            image.color = new Color(0.12f, 0.09f, 0.08f, 0.24f);
            UIFactory.AddOutlineBox(entryButton.transform, "Outline", new Color(0.95f, 0.88f, 0.72f, 0.78f), 2f);

            var shadow = entryButton.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
            shadow.effectDistance = new Vector2(0f, -2f);
            shadow.useGraphicAlpha = true;

            var glow = entryButton.AddComponent<Outline>();
            glow.effectColor = new Color(0.95f, 0.88f, 0.72f, 0.28f);
            glow.effectDistance = new Vector2(1.5f, -1.5f);
            glow.useGraphicAlpha = true;

            var dot = UIFactory.CreatePanel(entryButton.transform, "Dot", new Color(0.95f, 0.88f, 0.72f, 0.8f));
            var dotRect = dot.GetComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.sizeDelta = new Vector2(9f, 9f);
            dotRect.localEulerAngles = new Vector3(0f, 0f, 45f);

            entryButton.GetComponent<Button>().onClick.AddListener(Toggle);
        }

        private void BuildPopup(Transform parent)
        {
            popupRoot = UIFactory.CreatePanel(parent, "CheatRuntimePopup", new Color(0f, 0f, 0f, 0.72f));
            popupRoot.SetActive(false);

            var panel = UIFactory.CreatePanel(popupRoot.transform, "Panel", new Color(0.08f, 0.09f, 0.12f, 0.985f));
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.08f, 0.03f);
            panelRect.anchorMax = new Vector2(0.92f, 0.97f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            UIFactory.AddOutlineBox(panel.transform, "Outline", new Color(0.93f, 0.86f, 0.72f, 0.55f), 1f);

            titleText = CreateText(panel.transform, "Title", 28, TextAnchor.MiddleLeft, new Color(1f, 0.96f, 0.86f, 1f), 0.05f, 0.93f, 0.7f, 0.985f);
            summaryText = CreateText(panel.transform, "Summary", 16, TextAnchor.UpperLeft, new Color(0.9f, 0.93f, 0.98f, 0.92f), 0.05f, 0.84f, 0.95f, 0.915f);
            summaryText.horizontalOverflow = HorizontalWrapMode.Wrap;
            summaryText.verticalOverflow = VerticalWrapMode.Overflow;

            closeButtonText = GetButtonLabel(CreateButton(panel.transform, "CloseButton", string.Empty, Close, 0.78f, 0.93f, 0.95f, 0.985f));
            var scrollRect = UIFactory.CreateScrollRect(panel.transform, "ContentScroll", new Color(0f, 0f, 0f, 0f));
            var scrollRectRect = scrollRect.GetComponent<RectTransform>();
            scrollRectRect.anchorMin = new Vector2(0.05f, 0.03f);
            scrollRectRect.anchorMax = new Vector2(0.95f, 0.8f);
            scrollRectRect.offsetMin = Vector2.zero;
            scrollRectRect.offsetMax = Vector2.zero;
            scrollRect.content.sizeDelta = new Vector2(0f, 1480f);
            var content = scrollRect.content;

            runButtonText = GetButtonLabel(CreateAccentButton(content, "RunButton", string.Empty, OnRun, 0f, 0.93f, 1f, 0.99f));

            CreateSectionLine(content, 0.88f);
            stonesSectionText = CreateSectionTitle(content, 0.845f);
            CreateMiniButton(content, "ElementPrev", "<", delegate { elementIndex--; RefreshAll(); }, 0f, 0.775f, 0.08f, 0.83f);
            elementText = CreateValueChip(content, "ElementValue", 22, 0.1f, 0.775f, 0.46f, 0.83f);
            CreateMiniButton(content, "ElementNext", ">", delegate { elementIndex++; RefreshAll(); }, 0.48f, 0.775f, 0.56f, 0.83f);
            amountLabelText = CreateCaption(content, "AmountLabel", 0.6f, 0.83f, 0.72f, 0.86f);
            amountInputField = CreateNumberInput(content, "AmountValue", 20, 0.6f, 0.775f, 0.9f, 0.83f, OnAmountChanged);
            stoneButtonText = GetButtonLabel(CreateAccentButton(content, "GrantStone", string.Empty, GrantCurrentStone, 0f, 0.705f, 0.42f, 0.76f));
            all500ButtonText = GetButtonLabel(CreateButton(content, "GrantAll500", string.Empty, delegate { GrantAllSpiritStones(500); }, 0.45f, 0.705f, 0.68f, 0.76f));
            all5000ButtonText = GetButtonLabel(CreateButton(content, "GrantAll5000", string.Empty, delegate { GrantAllSpiritStones(5000); }, 0.71f, 0.705f, 1f, 0.76f));

            CreateSectionLine(content, 0.655f);
            progressSectionText = CreateSectionTitle(content, 0.62f);
            levelLabelText = CreateCaption(content, "LevelLabel", 0f, 0.6f, 0.15f, 0.63f);
            levelInputField = CreateNumberInput(content, "LevelValue", 20, 0f, 0.535f, 0.18f, 0.59f, OnTargetLevelChanged);
            applyLevelButtonText = GetButtonLabel(CreateAccentButton(content, "ApplyLevel", string.Empty, ApplyLevel, 0.22f, 0.535f, 0.52f, 0.59f));
            CreateMiniButton(content, "QuickLevel10", "10", delegate { targetLevel = 10; ApplyLevel(); }, 0.56f, 0.535f, 0.66f, 0.59f);
            CreateMiniButton(content, "QuickLevel30", "30", delegate { targetLevel = 30; ApplyLevel(); }, 0.69f, 0.535f, 0.79f, 0.59f);
            CreateMiniButton(content, "QuickLevel99", "99", delegate { targetLevel = 99; ApplyLevel(); }, 0.82f, 0.535f, 0.92f, 0.59f);

            stageLabelText = CreateCaption(content, "StageLabel", 0f, 0.49f, 0.15f, 0.52f);
            stageInputField = CreateNumberInput(content, "StageValue", 20, 0f, 0.425f, 0.18f, 0.48f, OnTargetStageChanged);
            jumpStageButtonText = GetButtonLabel(CreateAccentButton(content, "JumpStage", string.Empty, JumpToStage, 0.22f, 0.425f, 0.52f, 0.48f));
            jumpLastButtonText = GetButtonLabel(CreateButton(content, "JumpLast", string.Empty, JumpToLastStage, 0.56f, 0.425f, 0.96f, 0.48f));

            CreateSectionLine(content, 0.39f);
            loadoutSectionText = CreateSectionTitle(content, 0.355f);
            CreateMiniButton(content, "EquipPrev", "<", delegate { equipmentIndex--; RefreshAll(); }, 0f, 0.305f, 0.08f, 0.36f);
            equipmentText = CreateValueChip(content, "EquipValue", 17, 0.1f, 0.305f, 0.84f, 0.36f);
            CreateMiniButton(content, "EquipNext", ">", delegate { equipmentIndex++; RefreshAll(); }, 0.86f, 0.305f, 0.94f, 0.36f);
            grantEquipmentButtonText = GetButtonLabel(CreateAccentButton(content, "GrantEquipment", string.Empty, GrantEquipment, 0f, 0.24f, 0.45f, 0.29f));
            grantAllEquipmentButtonText = GetButtonLabel(CreateButton(content, "GrantAllEquipment", string.Empty, GrantAllEquipment, 0.48f, 0.24f, 1f, 0.29f));

            CreateMiniButton(content, "CharacterPrev", "<", delegate { characterIndex--; RefreshAll(); }, 0f, 0.18f, 0.08f, 0.225f);
            characterText = CreateValueChip(content, "CharacterValue", 16, 0.1f, 0.18f, 0.48f, 0.225f);
            CreateMiniButton(content, "CharacterNext", ">", delegate { characterIndex++; RefreshAll(); }, 0.5f, 0.18f, 0.58f, 0.225f);
            CreateMiniButton(content, "SkillPrev", "<", delegate { skillIndex--; RefreshAll(); }, 0.62f, 0.18f, 0.7f, 0.225f);
            skillText = CreateValueChip(content, "SkillValue", 16, 0.72f, 0.18f, 1f, 0.225f);

            skillLevelLabelText = CreateCaption(content, "SkillLevelLabel", 0f, 0.145f, 0.2f, 0.17f);
            skillLevelInputField = CreateNumberInput(content, "SkillLevelValue", 18, 0f, 0.095f, 0.18f, 0.145f, OnSkillLevelChanged);
            grantSkillButtonText = GetButtonLabel(CreateAccentButton(content, "GrantSkill", string.Empty, GrantSkill, 0.22f, 0.095f, 0.5f, 0.145f));
            grantAllSkillLv1ButtonText = GetButtonLabel(CreateButton(content, "GrantAllSkillLv1", string.Empty, delegate { GrantAllSkills(1); }, 0.54f, 0.095f, 0.76f, 0.145f));
            grantAllSkillLv5ButtonText = GetButtonLabel(CreateButton(content, "GrantAllSkillLv5", string.Empty, delegate { GrantAllSkills(5); }, 0.8f, 0.095f, 1f, 0.145f));
        }

        private void Toggle()
        {
            if (popupRoot == null)
            {
                return;
            }

            popupRoot.SetActive(!popupRoot.activeSelf);
            if (popupRoot.activeSelf)
            {
                RefreshAll();
            }
        }

        private void Close()
        {
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }
        }

        private void OnRun()
        {
            if (GameProgressManager.HasActiveRun())
            {
                GameProgressManager.ResetRun();
            }
            else
            {
                GameProgressManager.StartRun();
            }
        }

        private void GrantCurrentStone()
        {
            GameProgressManager.DebugGrantSpiritStones(Elements[elementIndex], amount);
        }

        private void GrantAllSpiritStones(int value)
        {
            for (var i = 0; i < Elements.Length; i++)
            {
                GameProgressManager.DebugGrantSpiritStones(Elements[i], value);
            }
        }

        private void ApplyLevel()
        {
            GameProgressManager.DebugSetCultivationLevel(targetLevel);
        }

        private void JumpToStage()
        {
            GameProgressManager.DebugJumpToStage(targetStage);
        }

        private void JumpToLastStage()
        {
            GameProgressManager.DebugJumpToStage(GameProgressManager.GetMaxStage());
        }

        private void GrantEquipment()
        {
            var database = EquipmentDatabaseLoader.Load();
            if (database == null || database.Equipments.Count == 0)
            {
                return;
            }

            equipmentIndex = Wrap(equipmentIndex, database.Equipments.Count);
            var config = database.Equipments[equipmentIndex];
            if (config != null)
            {
                GameProgressManager.DebugGrantEquipment(config.Id);
            }
        }

        private void GrantAllEquipment()
        {
            var database = EquipmentDatabaseLoader.Load();
            if (database == null)
            {
                return;
            }

            for (var i = 0; i < database.Equipments.Count; i++)
            {
                var config = database.Equipments[i];
                if (config != null)
                {
                    GameProgressManager.DebugGrantEquipment(config.Id);
                }
            }
        }

        private void GrantSkill()
        {
            var character = GetSelectedCharacter();
            var skill = GetSelectedSkill();
            if (character != null && skill != null)
            {
                GameProgressManager.DebugGrantSkill(character.Id, skill.Id, skillLevel);
            }
        }

        private void GrantAllSkills(int level)
        {
            var characterDatabase = CharacterDatabaseLoader.Load();
            var skillDatabase = SkillDatabaseLoader.Load();
            if (characterDatabase == null || skillDatabase == null)
            {
                return;
            }

            for (var i = 0; i < characterDatabase.Characters.Count; i++)
            {
                var character = characterDatabase.Characters[i];
                if (character == null)
                {
                    continue;
                }

                for (var j = 0; j < skillDatabase.Skills.Count; j++)
                {
                    var skill = skillDatabase.Skills[j];
                    if (skill != null)
                    {
                        GameProgressManager.DebugGrantSkill(character.Id, skill.Id, level);
                    }
                }
            }
        }

        private void RefreshAll()
        {
            if (popupRoot == null)
            {
                return;
            }

            elementIndex = Wrap(elementIndex, Elements.Length);
            targetStage = Mathf.Clamp(targetStage, 1, Mathf.Max(1, GameProgressManager.GetMaxStage()));
            targetLevel = Mathf.Clamp(targetLevel, 1, 999);
            skillLevel = Mathf.Clamp(skillLevel, 1, 99);

            titleText.text = T("cheat.title");
            stonesSectionText.text = T("cheat.section_stones");
            progressSectionText.text = T("cheat.section_progress");
            loadoutSectionText.text = T("cheat.section_loadout");
            summaryText.text = string.Format(
                T("cheat.summary"),
                GameProgressManager.GetCurrentStage(),
                GameProgressManager.GetHighestClearedStage(),
                GameProgressManager.GetCultivationLevel(),
                GameProgressManager.GetCultivationExp(),
                GameProgressManager.GetRequiredExpForNextLevel(),
                GameProgressManager.GetSpiritStones());

            closeButtonText.text = LocalizationManager.GetText("common.button_close");
            runButtonText.text = GameProgressManager.HasActiveRun() ? T("cheat.button_reset_run") : T("cheat.button_start_run");
            elementText.text = GameProgressManager.GetSpiritStoneName(Elements[elementIndex], false);
            amountLabelText.text = T("cheat.label_amount");
            SetInputValue(amountInputField, amount);
            levelLabelText.text = T("cheat.label_level");
            SetInputValue(levelInputField, targetLevel);
            stageLabelText.text = T("cheat.label_stage");
            SetInputValue(stageInputField, targetStage);
            skillLevelLabelText.text = T("cheat.label_skill_level");
            SetInputValue(skillLevelInputField, skillLevel);

            stoneButtonText.text = T("cheat.button_grant_current_stone");
            all500ButtonText.text = T("cheat.button_grant_all_500");
            all5000ButtonText.text = T("cheat.button_grant_all_5000");
            applyLevelButtonText.text = T("cheat.button_apply_level");
            jumpStageButtonText.text = T("cheat.button_jump_stage");
            jumpLastButtonText.text = T("cheat.button_jump_last");
            grantEquipmentButtonText.text = T("cheat.button_grant_equipment");
            grantAllEquipmentButtonText.text = T("cheat.button_grant_all_equipment");
            grantSkillButtonText.text = T("cheat.button_grant_skill");
            grantAllSkillLv1ButtonText.text = T("cheat.button_grant_all_skill_lv1");
            grantAllSkillLv5ButtonText.text = T("cheat.button_grant_all_skill_lv5");

            equipmentText.text = BuildEquipmentLabel();
            characterText.text = BuildCharacterLabel();
            skillText.text = BuildSkillLabel();
        }

        private CharacterConfig GetSelectedCharacter()
        {
            var database = CharacterDatabaseLoader.Load();
            if (database == null || database.Characters.Count == 0)
            {
                return null;
            }

            characterIndex = Wrap(characterIndex, database.Characters.Count);
            return database.Characters[characterIndex];
        }

        private SkillConfig GetSelectedSkill()
        {
            var database = SkillDatabaseLoader.Load();
            if (database == null || database.Skills.Count == 0)
            {
                return null;
            }

            skillIndex = Wrap(skillIndex, database.Skills.Count);
            return database.Skills[skillIndex];
        }

        private string BuildEquipmentLabel()
        {
            var database = EquipmentDatabaseLoader.Load();
            if (database == null || database.Equipments.Count == 0)
            {
                return T("cheat.empty_equipment");
            }

            equipmentIndex = Wrap(equipmentIndex, database.Equipments.Count);
            var config = database.Equipments[equipmentIndex];
            return config != null ? config.Id + " / " + config.Name : T("cheat.empty_equipment");
        }

        private string BuildCharacterLabel()
        {
            var database = CharacterDatabaseLoader.Load();
            if (database == null || database.Characters.Count == 0)
            {
                return T("cheat.empty_character");
            }

            characterIndex = Wrap(characterIndex, database.Characters.Count);
            var config = database.Characters[characterIndex];
            return config != null ? config.Id + " / " + config.Name : T("cheat.empty_character");
        }

        private string BuildSkillLabel()
        {
            var database = SkillDatabaseLoader.Load();
            if (database == null || database.Skills.Count == 0)
            {
                return T("cheat.empty_skill");
            }

            skillIndex = Wrap(skillIndex, database.Skills.Count);
            var config = database.Skills[skillIndex];
            return config != null ? config.Id + " / " + config.Name : T("cheat.empty_skill");
        }

        private static int Wrap(int index, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            index %= count;
            return index < 0 ? index + count : index;
        }

        private static string T(string key)
        {
            return LocalizationManager.GetText(key);
        }

        private static Text CreateText(Transform parent, string name, int fontSize, TextAnchor anchor, Color color, float x1, float y1, float x2, float y2)
        {
            var text = UIFactory.CreateText(parent, name, string.Empty, fontSize, anchor, color);
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(x1, y1);
            rect.anchorMax = new Vector2(x2, y2);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return text;
        }

        private static Text CreateCaption(Transform parent, string name, float x1, float y1, float x2, float y2)
        {
            return CreateText(parent, name, 14, TextAnchor.LowerLeft, new Color(0.8f, 0.84f, 0.9f, 0.7f), x1, y1, x2, y2);
        }

        private static Text CreateValueChip(Transform parent, string name, int fontSize, float x1, float y1, float x2, float y2)
        {
            var background = UIFactory.CreatePanel(parent, name + "Bg", new Color(0.16f, 0.19f, 0.25f, 0.85f));
            var rect = background.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(x1, y1);
            rect.anchorMax = new Vector2(x2, y2);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            UIFactory.AddOutlineBox(background.transform, "Outline", new Color(0.95f, 0.88f, 0.72f, 0.22f), 1f);
            return UIFactory.CreateText(background.transform, name, string.Empty, fontSize, TextAnchor.MiddleCenter, Color.white);
        }

        private static InputField CreateNumberInput(Transform parent, string name, int fontSize, float x1, float y1, float x2, float y2, UnityAction<string> onEndEdit)
        {
            var background = UIFactory.CreatePanel(parent, name + "Bg", new Color(0.16f, 0.19f, 0.25f, 0.92f));
            var rect = background.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(x1, y1);
            rect.anchorMax = new Vector2(x2, y2);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            UIFactory.AddOutlineBox(background.transform, "Outline", new Color(0.95f, 0.88f, 0.72f, 0.22f), 1f);

            var inputObject = new GameObject(name, typeof(RectTransform), typeof(InputField));
            inputObject.transform.SetParent(background.transform, false);

            var inputRect = inputObject.GetComponent<RectTransform>();
            inputRect.anchorMin = Vector2.zero;
            inputRect.anchorMax = Vector2.one;
            inputRect.offsetMin = new Vector2(12f, 6f);
            inputRect.offsetMax = new Vector2(-12f, -6f);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var text = UIFactory.CreateText(inputObject.transform, "Text", string.Empty, fontSize, TextAnchor.MiddleCenter, Color.white);
            text.font = font;
            text.supportRichText = false;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;

            var placeholder = UIFactory.CreateText(inputObject.transform, "Placeholder", "0", fontSize, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.26f));
            placeholder.font = font;
            placeholder.rectTransform.offsetMin = Vector2.zero;
            placeholder.rectTransform.offsetMax = Vector2.zero;

            var input = inputObject.GetComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.contentType = InputField.ContentType.IntegerNumber;
            input.lineType = InputField.LineType.SingleLine;
            input.characterLimit = 6;
            input.caretColor = new Color(1f, 0.95f, 0.85f, 1f);
            input.selectionColor = new Color(0.95f, 0.88f, 0.72f, 0.2f);
            input.onEndEdit.AddListener(onEndEdit);
            return input;
        }

        private static Button CreateButton(Transform parent, string name, string label, UnityAction onClick, float x1, float y1, float x2, float y2)
        {
            var button = UIFactory.CreateButton(parent, name, label, onClick);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(x1, y1);
            rect.anchorMax = new Vector2(x2, y2);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return button;
        }

        private static Button CreateAccentButton(Transform parent, string name, string label, UnityAction onClick, float x1, float y1, float x2, float y2)
        {
            var button = CreateButton(parent, name, label, onClick, x1, y1, x2, y2);
            button.GetComponent<Image>().color = new Color(0.46f, 0.22f, 0.12f, 0.96f);
            return button;
        }

        private static void CreateMiniButton(Transform parent, string name, string label, UnityAction onClick, float x1, float y1, float x2, float y2)
        {
            var button = CreateButton(parent, name, label, onClick, x1, y1, x2, y2);
            var text = GetButtonLabel(button);
            if (text != null)
            {
                text.fontSize = 20;
            }
        }

        private static Text GetButtonLabel(Button button)
        {
            var label = button.transform.Find("Label");
            return label != null ? label.GetComponent<Text>() : null;
        }

        private static void CreateSectionLine(Transform parent, float y)
        {
            UIFactory.CreateLine(parent, "Line" + y, new Color(0.95f, 0.88f, 0.72f, 0.18f), new Vector2(0.05f, y), new Vector2(0.95f, y), 1f);
        }

        private static Text CreateSectionTitle(Transform parent, float y)
        {
            var title = UIFactory.CreateText(parent, "Section" + y, string.Empty, 15, TextAnchor.MiddleLeft, new Color(1f, 0.93f, 0.82f, 0.78f));
            var rect = title.rectTransform;
            rect.anchorMin = new Vector2(0.05f, y);
            rect.anchorMax = new Vector2(0.35f, y + 0.03f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return title;
        }

        private static void SetInputValue(InputField input, int value)
        {
            if (input == null)
            {
                return;
            }

            var textValue = value.ToString();
            if (input.text != textValue)
            {
                input.SetTextWithoutNotify(textValue);
            }
        }

        private void OnAmountChanged(string value)
        {
            amount = ParseInteger(value, amount, 1, 99999);
            SetInputValue(amountInputField, amount);
        }

        private void OnTargetLevelChanged(string value)
        {
            targetLevel = ParseInteger(value, targetLevel, 1, 999);
            SetInputValue(levelInputField, targetLevel);
        }

        private void OnTargetStageChanged(string value)
        {
            targetStage = ParseInteger(value, targetStage, 1, Mathf.Max(1, GameProgressManager.GetMaxStage()));
            SetInputValue(stageInputField, targetStage);
        }

        private void OnSkillLevelChanged(string value)
        {
            skillLevel = ParseInteger(value, skillLevel, 1, 99);
            SetInputValue(skillLevelInputField, skillLevel);
        }

        private static int ParseInteger(string value, int fallback, int min, int max)
        {
            if (!int.TryParse(value, out var parsed))
            {
                return Mathf.Clamp(fallback, min, max);
            }

            return Mathf.Clamp(parsed, min, max);
        }
    }
}
