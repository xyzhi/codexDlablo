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
        private Text amountValueText;
        private Text levelLabelText;
        private Text levelValueText;
        private Text stageLabelText;
        private Text stageValueText;
        private Text equipmentText;
        private Text characterText;
        private Text skillText;
        private Text skillLevelLabelText;
        private Text skillLevelValueText;
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
            runButtonText = GetButtonLabel(CreateAccentButton(panel.transform, "RunButton", string.Empty, OnRun, 0.05f, 0.77f, 0.95f, 0.82f));

            CreateSectionLine(panel.transform, 0.73f);
            stonesSectionText = CreateSectionTitle(panel.transform, 0.695f);
            CreateMiniButton(panel.transform, "ElementPrev", "<", delegate { elementIndex--; RefreshAll(); }, 0.05f, 0.625f, 0.13f, 0.68f);
            elementText = CreateValueChip(panel.transform, "ElementValue", 22, 0.15f, 0.625f, 0.47f, 0.68f);
            CreateMiniButton(panel.transform, "ElementNext", ">", delegate { elementIndex++; RefreshAll(); }, 0.49f, 0.625f, 0.57f, 0.68f);
            amountLabelText = CreateCaption(panel.transform, "AmountLabel", 0.61f, 0.68f, 0.73f, 0.71f);
            CreateMiniButton(panel.transform, "AmountMinus", "-", delegate { amount = Mathf.Max(100, amount - 100); RefreshAll(); }, 0.61f, 0.625f, 0.69f, 0.68f);
            amountValueText = CreateValueChip(panel.transform, "AmountValue", 20, 0.71f, 0.625f, 0.81f, 0.68f);
            CreateMiniButton(panel.transform, "AmountPlus", "+", delegate { amount = Mathf.Min(99999, amount + 100); RefreshAll(); }, 0.83f, 0.625f, 0.91f, 0.68f);
            stoneButtonText = GetButtonLabel(CreateAccentButton(panel.transform, "GrantStone", string.Empty, GrantCurrentStone, 0.05f, 0.555f, 0.42f, 0.61f));
            all500ButtonText = GetButtonLabel(CreateButton(panel.transform, "GrantAll500", string.Empty, delegate { GrantAllSpiritStones(500); }, 0.45f, 0.555f, 0.68f, 0.61f));
            all5000ButtonText = GetButtonLabel(CreateButton(panel.transform, "GrantAll5000", string.Empty, delegate { GrantAllSpiritStones(5000); }, 0.71f, 0.555f, 0.95f, 0.61f));

            CreateSectionLine(panel.transform, 0.515f);
            progressSectionText = CreateSectionTitle(panel.transform, 0.48f);
            levelLabelText = CreateCaption(panel.transform, "LevelLabel", 0.05f, 0.46f, 0.17f, 0.49f);
            CreateMiniButton(panel.transform, "LevelMinus", "-", delegate { targetLevel = Mathf.Max(1, targetLevel - 1); RefreshAll(); }, 0.05f, 0.395f, 0.13f, 0.45f);
            levelValueText = CreateValueChip(panel.transform, "LevelValue", 20, 0.15f, 0.395f, 0.25f, 0.45f);
            CreateMiniButton(panel.transform, "LevelPlus", "+", delegate { targetLevel = Mathf.Min(999, targetLevel + 1); RefreshAll(); }, 0.27f, 0.395f, 0.35f, 0.45f);
            applyLevelButtonText = GetButtonLabel(CreateAccentButton(panel.transform, "ApplyLevel", string.Empty, ApplyLevel, 0.37f, 0.395f, 0.59f, 0.45f));
            CreateMiniButton(panel.transform, "QuickLevel10", "10", delegate { targetLevel = 10; ApplyLevel(); }, 0.62f, 0.395f, 0.7f, 0.45f);
            CreateMiniButton(panel.transform, "QuickLevel30", "30", delegate { targetLevel = 30; ApplyLevel(); }, 0.73f, 0.395f, 0.81f, 0.45f);
            CreateMiniButton(panel.transform, "QuickLevel99", "99", delegate { targetLevel = 99; ApplyLevel(); }, 0.84f, 0.395f, 0.92f, 0.45f);

            stageLabelText = CreateCaption(panel.transform, "StageLabel", 0.05f, 0.365f, 0.17f, 0.395f);
            CreateMiniButton(panel.transform, "StageMinus", "-", delegate { targetStage = Mathf.Max(1, targetStage - 1); RefreshAll(); }, 0.05f, 0.3f, 0.13f, 0.355f);
            stageValueText = CreateValueChip(panel.transform, "StageValue", 20, 0.15f, 0.3f, 0.25f, 0.355f);
            CreateMiniButton(panel.transform, "StagePlus", "+", delegate { targetStage = Mathf.Min(GameProgressManager.GetMaxStage(), targetStage + 1); RefreshAll(); }, 0.27f, 0.3f, 0.35f, 0.355f);
            jumpStageButtonText = GetButtonLabel(CreateAccentButton(panel.transform, "JumpStage", string.Empty, JumpToStage, 0.37f, 0.3f, 0.59f, 0.355f));
            jumpLastButtonText = GetButtonLabel(CreateButton(panel.transform, "JumpLast", string.Empty, JumpToLastStage, 0.62f, 0.3f, 0.92f, 0.355f));

            CreateSectionLine(panel.transform, 0.275f);
            loadoutSectionText = CreateSectionTitle(panel.transform, 0.24f);
            CreateMiniButton(panel.transform, "EquipPrev", "<", delegate { equipmentIndex--; RefreshAll(); }, 0.05f, 0.19f, 0.13f, 0.245f);
            equipmentText = CreateValueChip(panel.transform, "EquipValue", 17, 0.15f, 0.19f, 0.79f, 0.245f);
            CreateMiniButton(panel.transform, "EquipNext", ">", delegate { equipmentIndex++; RefreshAll(); }, 0.81f, 0.19f, 0.89f, 0.245f);
            grantEquipmentButtonText = GetButtonLabel(CreateAccentButton(panel.transform, "GrantEquipment", string.Empty, GrantEquipment, 0.05f, 0.125f, 0.45f, 0.175f));
            grantAllEquipmentButtonText = GetButtonLabel(CreateButton(panel.transform, "GrantAllEquipment", string.Empty, GrantAllEquipment, 0.48f, 0.125f, 0.95f, 0.175f));

            CreateMiniButton(panel.transform, "CharacterPrev", "<", delegate { characterIndex--; RefreshAll(); }, 0.05f, 0.07f, 0.13f, 0.115f);
            characterText = CreateValueChip(panel.transform, "CharacterValue", 16, 0.15f, 0.07f, 0.45f, 0.115f);
            CreateMiniButton(panel.transform, "CharacterNext", ">", delegate { characterIndex++; RefreshAll(); }, 0.47f, 0.07f, 0.55f, 0.115f);
            CreateMiniButton(panel.transform, "SkillPrev", "<", delegate { skillIndex--; RefreshAll(); }, 0.57f, 0.07f, 0.65f, 0.115f);
            skillText = CreateValueChip(panel.transform, "SkillValue", 16, 0.67f, 0.07f, 0.95f, 0.115f);

            skillLevelLabelText = CreateCaption(panel.transform, "SkillLevelLabel", 0.05f, 0.045f, 0.2f, 0.07f);
            CreateMiniButton(panel.transform, "SkillLevelMinus", "-", delegate { skillLevel = Mathf.Max(1, skillLevel - 1); RefreshAll(); }, 0.05f, 0.005f, 0.13f, 0.05f);
            skillLevelValueText = CreateValueChip(panel.transform, "SkillLevelValue", 18, 0.15f, 0.005f, 0.25f, 0.05f);
            CreateMiniButton(panel.transform, "SkillLevelPlus", "+", delegate { skillLevel = Mathf.Min(99, skillLevel + 1); RefreshAll(); }, 0.27f, 0.005f, 0.35f, 0.05f);
            grantSkillButtonText = GetButtonLabel(CreateAccentButton(panel.transform, "GrantSkill", string.Empty, GrantSkill, 0.37f, 0.005f, 0.59f, 0.05f));
            grantAllSkillLv1ButtonText = GetButtonLabel(CreateButton(panel.transform, "GrantAllSkillLv1", string.Empty, delegate { GrantAllSkills(1); }, 0.62f, 0.005f, 0.78f, 0.05f));
            grantAllSkillLv5ButtonText = GetButtonLabel(CreateButton(panel.transform, "GrantAllSkillLv5", string.Empty, delegate { GrantAllSkills(5); }, 0.81f, 0.005f, 0.95f, 0.05f));
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
            amountValueText.text = amount.ToString();
            levelLabelText.text = T("cheat.label_level");
            levelValueText.text = targetLevel.ToString();
            stageLabelText.text = T("cheat.label_stage");
            stageValueText.text = targetStage.ToString();
            skillLevelLabelText.text = T("cheat.label_skill_level");
            skillLevelValueText.text = skillLevel.ToString();

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
    }
}
