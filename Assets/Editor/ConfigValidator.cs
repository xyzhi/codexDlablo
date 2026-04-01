
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ConfigValidator
{
    private const string CharacterCsvPath = "Docs/Character.csv";
    private const string EnemyCsvPath = "Docs/Enemy.csv";
    private const string SkillCsvPath = "Docs/Skill.csv";
    private const string EquipmentCsvPath = "Docs/Equipment.csv";
    private const string SpiritStoneCsvPath = "Docs/SpiritStone.csv";
    private const string StageBalanceCsvPath = "Docs/StageBalance.csv";
    private const string StageNodeCsvPath = "Docs/StageNode.csv";
    private const string EventOptionCsvPath = "Docs/EventOption.csv";
    private const string LocalizationCsvPath = "Docs/Localization.csv";

    private static readonly HashSet<string> ValidSkillQualities = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "普通", "稀有", "绝品", "common", "rare", "epic", "legendary"
    };

    private static readonly HashSet<string> ValidElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "金", "木", "水", "火", "土", "metal", "wood", "water", "fire", "earth"
    };

    private static readonly HashSet<string> ValidEquipmentSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Weapon", "Armor", "Accessory"
    };

    private static readonly HashSet<string> ValidNodeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Battle", "Elite", "Rest", "Boss"
    };

    private static readonly HashSet<string> ValidEventRewardModes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Progress", "Equipment", "Skill"
    };

    private static readonly HashSet<string> ValidSkillRewardNodeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Battle", "Elite", "Boss"
    };

    private static readonly HashSet<string> BuiltinStageEventProfiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "DefaultBattle", "DefaultElite", "DefaultBoss"
    };

    private static readonly Dictionary<string, string> SpiritStoneColorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "金", "#E5C36A" }, { "木", "#5BC16A" }, { "水", "#59A7FF" }, { "火", "#FF6B5D" }, { "土", "#E6D25A" },
        { "metal", "#E5C36A" }, { "wood", "#5BC16A" }, { "water", "#59A7FF" }, { "fire", "#FF6B5D" }, { "earth", "#E6D25A" }
    };

    [MenuItem("工具/配置/校验全部配置")]
    public static void ValidateAll()
    {
        var items = new List<ValidationItem>();

        try
        {
            var characterTable = LoadTable(CharacterCsvPath, "角色表");
            var enemyTable = LoadTable(EnemyCsvPath, "敌人表");
            var skillTable = LoadTable(SkillCsvPath, "技能表");
            var equipmentTable = LoadTable(EquipmentCsvPath, "装备表");
            var spiritStoneTable = LoadTable(SpiritStoneCsvPath, "灵石表");
            var stageBalanceTable = LoadTable(StageBalanceCsvPath, "关卡成长表");
            var stageNodeTable = LoadTable(StageNodeCsvPath, "关卡节点表");
            var eventOptionTable = LoadTable(EventOptionCsvPath, "事件选项表");
            var localizationTable = LoadTable(LocalizationCsvPath, "语言表");

            var localizationKeys = ValidateLocalizationTable(localizationTable, items);
            var skillIds = ValidateSkillTable(skillTable, items);
            ValidateCharacterTable(characterTable, skillIds, items);
            ValidateEnemyTable(enemyTable, skillIds, items);
            ValidateEquipmentTable(equipmentTable, items);
            ValidateSpiritStoneTable(spiritStoneTable, items);
            ValidateStageBalanceTable(stageBalanceTable, items);
            var eventProfiles = ValidateEventOptionTable(eventOptionTable, localizationKeys, items);
            ValidateStageNodeTable(stageNodeTable, eventProfiles, items);

            LogValidationItems(items);
        }
        catch (Exception exception)
        {
            Debug.LogError("配置校验执行失败：" + exception.Message);
        }
    }

    private static HashSet<string> ValidateLocalizationTable(CsvTable table, List<ValidationItem> items)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            RequireFields(row, items, "Key", "ZhHans", "En");

            var key = row.Get("Key").Trim();
            if (!string.IsNullOrEmpty(key))
            {
                int firstLine;
                if (seen.TryGetValue(key, out firstLine))
                {
                    AddError(items, row, "Key", $"发现重复 key：'{key}'，首次出现在第 {firstLine} 行。");
                }
                else
                {
                    seen[key] = row.LineNumber;
                    keys.Add(key);
                }
            }

            ValidateMultilineText(row, items, "ZhHans");
            ValidateMultilineText(row, items, "En");
            DetectMojibake(row, items, "ZhHans");
            DetectMojibake(row, items, "En");
        }

        return keys;
    }

    private static HashSet<string> ValidateSkillTable(CsvTable table, List<ValidationItem> items)
    {
        ValidateDuplicateIds(table, items);
        var skillIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            RequireFields(row, items, "ID", "Name", "Element", "Quality", "Category", "TargetType", "EffectType");
            ValidateIntegerField(row, items, "MPCost");
            ValidateIntegerField(row, items, "Power");
            ValidateIntegerField(row, items, "Duration");

            var skillId = row.Get("ID").Trim();
            if (!string.IsNullOrEmpty(skillId))
            {
                skillIds.Add(skillId);
            }

            var quality = row.Get("Quality");
            if (!string.IsNullOrWhiteSpace(quality) && !ValidSkillQualities.Contains(quality.Trim()))
            {
                AddError(items, row, "Quality", $"技能品质不合法：'{quality}'。当前仅支持 普通/稀有/绝品（或 common/rare/epic/legendary）。");
            }

            var element = row.Get("Element");
            if (!string.IsNullOrWhiteSpace(element) && !IsValidElementToken(element))
            {
                AddError(items, row, "Element", $"技能五行不合法：'{element}'。");
            }

            DetectMojibake(row, items, "Name");
            DetectMojibake(row, items, "Description");
            DetectMojibake(row, items, "Notes");
        }

        return skillIds;
    }

    private static void ValidateCharacterTable(CsvTable table, HashSet<string> skillIds, List<ValidationItem> items)
    {
        ValidateDuplicateIds(table, items);

        for (var i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            RequireFields(row, items, "ID", "Name", "ElementRoots", "ClassRole", "Position", "CombatStyle", "InitialSkills");
            ValidateIntegerField(row, items, "HP");
            ValidateIntegerField(row, items, "ATK");
            ValidateIntegerField(row, items, "DEF");
            ValidateIntegerField(row, items, "MP");
            ValidateElementList(row, items, "ElementRoots");
            ValidateSkillReferenceList(row, "InitialSkills", skillIds, items);
        }
    }

    private static void ValidateEnemyTable(CsvTable table, HashSet<string> skillIds, List<ValidationItem> items)
    {
        ValidateDuplicateIds(table, items);

        for (var i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            RequireFields(row, items, "ID", "Name", "Element", "Role", "Position", "CombatStyle", "Skills");
            ValidateIntegerField(row, items, "HP");
            ValidateIntegerField(row, items, "ATK");
            ValidateIntegerField(row, items, "DEF");
            ValidateIntegerField(row, items, "MP");
            ValidateElementList(row, items, "Element");
            ValidateSkillReferenceList(row, "Skills", skillIds, items);
        }
    }

    private static void ValidateEquipmentTable(CsvTable table, List<ValidationItem> items)
    {
        ValidateDuplicateIds(table, items);

        for (var i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            RequireFields(row, items, "ID", "Name", "Slot");
            ValidateIntegerField(row, items, "HP");
            ValidateIntegerField(row, items, "ATK");
            ValidateIntegerField(row, items, "DEF");
            ValidateIntegerField(row, items, "MP");

            var slot = row.Get("Slot");
            if (!string.IsNullOrWhiteSpace(slot) && !ValidEquipmentSlots.Contains(slot.Trim()))
            {
                AddError(items, row, "Slot", $"装备槽位缺少运行时映射：'{slot}'。当前仅支持 Weapon/Armor/Accessory。");
            }

            DetectMojibake(row, items, "Name");
            DetectMojibake(row, items, "Notes");
        }
    }
    private static void ValidateSpiritStoneTable(CsvTable table, List<ValidationItem> items)
    {
        ValidateDuplicateIds(table, items);

        for (var i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            RequireFields(row, items, "ID", "Name", "Element", "ColorHex");

            var element = row.Get("Element");
            if (!string.IsNullOrWhiteSpace(element) && !IsValidElementToken(element))
            {
                AddError(items, row, "Element", $"灵石五行不合法：'{element}'。");
            }

            var colorHex = row.Get("ColorHex");
            if (!string.IsNullOrWhiteSpace(colorHex))
            {
                var normalizedColor = colorHex.Trim().ToUpperInvariant();
                if (!IsHexColor(normalizedColor))
                {
                    AddError(items, row, "ColorHex", $"灵石颜色格式不合法：'{colorHex}'。应为 #RRGGBB。");
                }
                else if (!string.IsNullOrWhiteSpace(element))
                {
                    string expectedColor;
                    if (SpiritStoneColorMap.TryGetValue(element.Trim(), out expectedColor)
                        && !string.Equals(expectedColor, normalizedColor, StringComparison.OrdinalIgnoreCase))
                    {
                        AddError(items, row, "ColorHex", $"灵石颜色与当前运行时映射不一致：'{element}' 期望 {expectedColor}，实际为 {normalizedColor}。");
                    }
                }
            }

            DetectMojibake(row, items, "Name");
            DetectMojibake(row, items, "Notes");
        }
    }

    private static void ValidateStageBalanceTable(CsvTable table, List<ValidationItem> items)
    {
        ValidateSequentialStageRows(table, items, true);

        for (var i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            ValidateIntegerField(row, items, "Stage");
            ValidateIntegerField(row, items, "EnemyHPBonus");
            ValidateIntegerField(row, items, "EnemyATKBonus");
            ValidateIntegerField(row, items, "EnemyDEFBonus");
            ValidateIntegerField(row, items, "EnemyMPBonus");
            ValidateIntegerField(row, items, "EnemyEquipmentCount");
            ValidateIntegerField(row, items, "PlayerHPBonus");
            ValidateIntegerField(row, items, "PlayerATKBonus");
            ValidateIntegerField(row, items, "PlayerDEFBonus");
            ValidateIntegerField(row, items, "PlayerMPBonus");

            var equipmentCount = ParseIntOrNull(row.Get("EnemyEquipmentCount"));
            if (equipmentCount.HasValue)
            {
                if (equipmentCount.Value < 0)
                {
                    AddError(items, row, "EnemyEquipmentCount", "敌人装备数量不能为负数。");
                }
                else if (equipmentCount.Value > 3)
                {
                    AddWarning(items, row, "EnemyEquipmentCount", $"敌人装备数量偏高：{equipmentCount.Value}。当前建议范围 0-3。");
                }
            }

            WarnOnOutlier(row, items, "EnemyHPBonus", 200);
            WarnOnOutlier(row, items, "EnemyATKBonus", 50);
            WarnOnOutlier(row, items, "EnemyDEFBonus", 50);
            WarnOnOutlier(row, items, "EnemyMPBonus", 80);
            WarnOnOutlier(row, items, "PlayerHPBonus", 200);
            WarnOnOutlier(row, items, "PlayerATKBonus", 50);
            WarnOnOutlier(row, items, "PlayerDEFBonus", 50);
            WarnOnOutlier(row, items, "PlayerMPBonus", 80);
        }
    }

    private static void ValidateStageNodeTable(CsvTable table, HashSet<string> eventProfiles, List<ValidationItem> items)
    {
        ValidateSequentialStageRows(table, items, true);

        for (var i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            RequireFields(row, items, "Stage", "NodeType", "EventProfile", "ThemeZh", "ThemeEn", "DetailZh", "DetailEn");
            ValidateIntegerField(row, items, "Stage");

            var nodeType = row.Get("NodeType");
            if (!string.IsNullOrWhiteSpace(nodeType) && !ValidNodeTypes.Contains(nodeType.Trim()))
            {
                AddError(items, row, "NodeType", $"节点类型不合法：'{nodeType}'。当前仅支持 Battle / Elite / Rest / Boss。");
            }

            var profile = row.Get("EventProfile").Trim();
            if (!string.IsNullOrEmpty(profile)
                && eventProfiles != null
                && eventProfiles.Count > 0
                && !eventProfiles.Contains(profile)
                && !BuiltinStageEventProfiles.Contains(profile))
            {
                AddError(items, row, "EventProfile", $"引用的事件配置不存在：'{profile}'。若这是主流程内置节点，请加入内置白名单或改为事件表已有 Profile。");
            }
        }
    }

    private static HashSet<string> ValidateEventOptionTable(CsvTable table, HashSet<string> localizationKeys, List<ValidationItem> items)
    {
        var profileToOptions = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);
        var profileOptionSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var profiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            RequireFields(row, items, "Profile", "OptionIndex", "TitleKey", "RewardMode", "SpiritStoneElement", "ExpBase", "ExpPerStage", "SpiritStoneBase", "SpiritStonePerStage", "SpiritStoneCostBase", "SpiritStoneCostPerStage", "ResultTitleKey", "ResultIntroKey");
            ValidateIntegerField(row, items, "OptionIndex");
            ValidateIntegerField(row, items, "ExpBase");
            ValidateIntegerField(row, items, "ExpPerStage");
            ValidateIntegerField(row, items, "SpiritStoneBase");
            ValidateIntegerField(row, items, "SpiritStonePerStage");
            ValidateIntegerField(row, items, "SpiritStoneCostBase");
            ValidateIntegerField(row, items, "SpiritStoneCostPerStage");

            var profile = row.Get("Profile").Trim();
            var optionIndex = ParseIntOrNull(row.Get("OptionIndex"));
            if (!string.IsNullOrEmpty(profile))
            {
                profiles.Add(profile);
            }

            if (!string.IsNullOrEmpty(profile) && optionIndex.HasValue)
            {
                var pairKey = profile + "#" + optionIndex.Value;
                if (!profileOptionSeen.Add(pairKey))
                {
                    AddError(items, row, "OptionIndex", $"Profile + OptionIndex 重复：'{profile}' / {optionIndex.Value}。");
                }

                HashSet<int> optionSet;
                if (!profileToOptions.TryGetValue(profile, out optionSet))
                {
                    optionSet = new HashSet<int>();
                    profileToOptions[profile] = optionSet;
                }

                optionSet.Add(optionIndex.Value);
                if (optionIndex.Value < 1 || optionIndex.Value > 3)
                {
                    AddError(items, row, "OptionIndex", $"OptionIndex 超出范围：{optionIndex.Value}。当前应为 1 到 3。");
                }
            }

            var rewardMode = row.Get("RewardMode");
            if (!string.IsNullOrWhiteSpace(rewardMode) && !ValidEventRewardModes.Contains(rewardMode.Trim()))
            {
                AddError(items, row, "RewardMode", $"RewardMode 不合法：'{rewardMode}'。当前仅支持 Progress / Equipment / Skill。");
            }

            var rewardNodeType = row.Get("SkillRewardNodeType");
            if (!string.IsNullOrWhiteSpace(rewardNodeType) && !ValidSkillRewardNodeTypes.Contains(rewardNodeType.Trim()))
            {
                AddError(items, row, "SkillRewardNodeType", $"SkillRewardNodeType 不合法：'{rewardNodeType}'。当前仅支持 Battle / Elite / Boss 或留空。");
            }

            ValidateNonNegativeIntegerField(row, items, "ExpBase");
            ValidateNonNegativeIntegerField(row, items, "ExpPerStage");
            ValidateNonNegativeIntegerField(row, items, "SpiritStoneBase");
            ValidateNonNegativeIntegerField(row, items, "SpiritStonePerStage");
            ValidateNonNegativeIntegerField(row, items, "SpiritStoneCostBase");
            ValidateNonNegativeIntegerField(row, items, "SpiritStoneCostPerStage");

            ValidateEventSpiritStoneElement(row, items);
            ValidateLocalizationKeyReference(row, items, localizationKeys, "TitleKey", true);
            ValidateLocalizationKeyReference(row, items, localizationKeys, "ResultTitleKey", true);
            ValidateLocalizationKeyReference(row, items, localizationKeys, "ResultIntroKey", true);

            var isSkillMode = string.Equals(rewardMode.Trim(), "Skill", StringComparison.OrdinalIgnoreCase);
            ValidateLocalizationKeyReference(row, items, localizationKeys, "SelectionTitleKey", isSkillMode);
            ValidateLocalizationKeyReference(row, items, localizationKeys, "SelectionMessageKey", isSkillMode);
            ValidateLocalizationKeyReference(row, items, localizationKeys, "EmptyResultKey", isSkillMode);
        }

        foreach (var pair in profileToOptions)
        {
            for (var option = 1; option <= 3; option++)
            {
                if (!pair.Value.Contains(option))
                {
                    AddError(items, table.DisplayName, 0, "Profile", $"Profile '{pair.Key}' 缺少 OptionIndex {option}，每个 Profile 必须完整拥有 3 个选项。");
                }
            }
        }

        return profiles;
    }
    private static void ValidateDuplicateIds(CsvTable table, List<ValidationItem> items)
    {
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            var id = row.Get("ID").Trim();
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            int firstLine;
            if (seen.TryGetValue(id, out firstLine))
            {
                AddError(items, row, "ID", $"发现重复 ID：'{id}'，首次出现在第 {firstLine} 行。");
            }
            else
            {
                seen[id] = row.LineNumber;
            }
        }
    }

    private static void ValidateSequentialStageRows(CsvTable table, List<ValidationItem> items, bool startAtOne)
    {
        var seenStages = new HashSet<int>();
        var stages = new List<int>();

        for (var i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            var stage = ParseIntOrNull(row.Get("Stage"));
            if (!stage.HasValue)
            {
                continue;
            }

            if (!seenStages.Add(stage.Value))
            {
                AddError(items, row, "Stage", $"发现重复 Stage：{stage.Value}。");
            }
            else
            {
                stages.Add(stage.Value);
            }
        }

        stages.Sort();
        if (stages.Count == 0)
        {
            return;
        }

        var expected = startAtOne ? 1 : stages[0];
        for (var i = 0; i < stages.Count; i++)
        {
            if (stages[i] != expected)
            {
                AddError(items, table.DisplayName, 0, "Stage", $"Stage 不连续：期望 {expected}，实际出现 {stages[i]}。请补齐缺失关卡。");
                expected = stages[i];
            }

            expected++;
        }
    }

    private static void ValidateSkillReferenceList(CsvRow row, string fieldName, HashSet<string> skillIds, List<ValidationItem> items)
    {
        var raw = row.Get(fieldName);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        var parts = raw.Split('|');
        for (var i = 0; i < parts.Length; i++)
        {
            var token = parts[i].Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            if (skillIds.Contains(token))
            {
                continue;
            }

            if (!LooksLikeSkillId(token))
            {
                AddWarning(items, row, fieldName, $"技能引用 '{token}' 不是技能 ID 格式。当前运行时按技能 ID 读取，建议改成 SKxxx。");
            }

            AddError(items, row, fieldName, $"技能引用无效：'{token}'，在 Skill.csv 中找不到对应 ID。");
        }
    }

    private static void ValidateElementList(CsvRow row, List<ValidationItem> items, string fieldName)
    {
        var raw = row.Get(fieldName);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        var parts = raw.Split('|');
        for (var i = 0; i < parts.Length; i++)
        {
            var token = parts[i].Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            if (!IsValidElementToken(token))
            {
                AddError(items, row, fieldName, $"五行字段不合法：'{token}'。");
            }
        }
    }

    private static void ValidateEventSpiritStoneElement(CsvRow row, List<ValidationItem> items)
    {
        var value = row.Get("SpiritStoneElement").Trim();
        if (string.IsNullOrEmpty(value) || string.Equals(value, "Stage", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!IsValidElementToken(value))
        {
            AddError(items, row, "SpiritStoneElement", $"SpiritStoneElement 不合法：'{value}'。应为 Stage 或五行元素。");
        }
    }

    private static void ValidateLocalizationKeyReference(CsvRow row, List<ValidationItem> items, HashSet<string> localizationKeys, string fieldName, bool required)
    {
        var key = row.Get(fieldName).Trim();
        if (string.IsNullOrEmpty(key))
        {
            if (required)
            {
                AddError(items, row, fieldName, "关键多语言 key 为空。");
            }

            return;
        }

        if (localizationKeys != null && !localizationKeys.Contains(key))
        {
            AddError(items, row, fieldName, $"多语言 key 不存在：'{key}'。");
        }
    }

    private static void RequireFields(CsvRow row, List<ValidationItem> items, params string[] fieldNames)
    {
        for (var i = 0; i < fieldNames.Length; i++)
        {
            var fieldName = fieldNames[i];
            if (string.IsNullOrWhiteSpace(row.Get(fieldName)))
            {
                AddError(items, row, fieldName, "必填字段为空。");
            }
        }
    }

    private static void ValidateIntegerField(CsvRow row, List<ValidationItem> items, string fieldName)
    {
        var value = row.Get(fieldName);
        if (string.IsNullOrWhiteSpace(value))
        {
            AddError(items, row, fieldName, "数值字段为空。");
            return;
        }

        int parsedValue;
        if (!int.TryParse(value.Trim(), out parsedValue))
        {
            AddError(items, row, fieldName, $"数值字段无法解析为整数：'{value}'。");
        }
    }

    private static void ValidateNonNegativeIntegerField(CsvRow row, List<ValidationItem> items, string fieldName)
    {
        var parsed = ParseIntOrNull(row.Get(fieldName));
        if (parsed.HasValue && parsed.Value < 0)
        {
            AddError(items, row, fieldName, $"数值不能为负数：{parsed.Value}。");
        }
    }

    private static void WarnOnOutlier(CsvRow row, List<ValidationItem> items, string fieldName, int threshold)
    {
        var parsed = ParseIntOrNull(row.Get(fieldName));
        if (parsed.HasValue && Mathf.Abs(parsed.Value) > threshold)
        {
            AddWarning(items, row, fieldName, $"数值偏离常规范围：{parsed.Value}。请确认是否为策划预期。");
        }
    }

    private static void ValidateMultilineText(CsvRow row, List<ValidationItem> items, string fieldName)
    {
        var value = row.Get(fieldName);
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (value.Contains("\r") && !value.Contains("\r\n"))
        {
            AddWarning(items, row, fieldName, "文本中存在异常换行符。建议统一使用标准换行。");
            return;
        }

        var lineBreakCount = CountLineBreaks(value);
        if (lineBreakCount >= 4)
        {
            AddWarning(items, row, fieldName, $"多行文本换行较多（{lineBreakCount} 处）。请确认是否为预期文案。");
        }
    }

    private static void DetectMojibake(CsvRow row, List<ValidationItem> items, string fieldName)
    {
        var value = row.Get(fieldName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (value.IndexOf('�') >= 0
            || value.IndexOf("鍙", StringComparison.Ordinal) >= 0
            || value.IndexOf("閲", StringComparison.Ordinal) >= 0
            || value.IndexOf("鐏", StringComparison.Ordinal) >= 0
            || value.IndexOf("姘", StringComparison.Ordinal) >= 0)
        {
            AddWarning(items, row, fieldName, $"疑似存在编码异常：'{value}'。");
        }
    }
    private static int CountLineBreaks(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        var count = 0;
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '\n')
            {
                count++;
            }
        }

        return count;
    }

    private static int? ParseIntOrNull(string value)
    {
        int result;
        return int.TryParse((value ?? string.Empty).Trim(), out result) ? result : (int?)null;
    }

    private static bool LooksLikeSkillId(string token)
    {
        return !string.IsNullOrEmpty(token) && token.Length >= 3 && token.StartsWith("SK", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidElementToken(string token)
    {
        return ValidElements.Contains((token ?? string.Empty).Trim());
    }

    private static bool IsHexColor(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length != 7 || value[0] != '#')
        {
            return false;
        }

        for (var i = 1; i < value.Length; i++)
        {
            var ch = value[i];
            var isDigit = ch >= '0' && ch <= '9';
            var isUpperHex = ch >= 'A' && ch <= 'F';
            var isLowerHex = ch >= 'a' && ch <= 'f';
            if (!isDigit && !isUpperHex && !isLowerHex)
            {
                return false;
            }
        }

        return true;
    }

    private static void AddError(List<ValidationItem> items, CsvRow row, string fieldName, string message)
    {
        items.Add(new ValidationItem("错误", row.Table.DisplayName, row.LineNumber, fieldName, message));
    }

    private static void AddWarning(List<ValidationItem> items, CsvRow row, string fieldName, string message)
    {
        items.Add(new ValidationItem("警告", row.Table.DisplayName, row.LineNumber, fieldName, message));
    }

    private static void AddError(List<ValidationItem> items, string tableName, int lineNumber, string fieldName, string message)
    {
        items.Add(new ValidationItem("错误", tableName, lineNumber, fieldName, message));
    }

    private static void LogValidationItems(List<ValidationItem> items)
    {
        var errors = new List<ValidationItem>();
        var warnings = new List<ValidationItem>();
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].Severity == "错误")
            {
                errors.Add(items[i]);
            }
            else
            {
                warnings.Add(items[i]);
            }
        }

        LogGroupedItems("错误", errors, true);
        LogGroupedItems("警告", warnings, false);

        if (errors.Count > 0)
        {
            Debug.LogError($"配置校验失败：共发现 {errors.Count} 个错误，{warnings.Count} 个警告。");
            return;
        }

        Debug.Log($"配置校验通过：未发现错误。警告 {warnings.Count} 个。");
    }

    private static void LogGroupedItems(string severity, List<ValidationItem> items, bool isError)
    {
        if (items.Count == 0)
        {
            return;
        }

        var grouped = new Dictionary<string, List<ValidationItem>>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < items.Count; i++)
        {
            List<ValidationItem> group;
            if (!grouped.TryGetValue(items[i].TableName, out group))
            {
                group = new List<ValidationItem>();
                grouped[items[i].TableName] = group;
            }

            group.Add(items[i]);
        }

        foreach (var pair in grouped)
        {
            if (isError)
            {
                Debug.LogError($"[{severity}][{pair.Key}] 共 {pair.Value.Count} 条");
            }
            else
            {
                Debug.LogWarning($"[{severity}][{pair.Key}] 共 {pair.Value.Count} 条");
            }

            for (var i = 0; i < pair.Value.Count; i++)
            {
                var item = pair.Value[i];
                var lineInfo = item.LineNumber > 0 ? $"第 {item.LineNumber} 行 / {item.FieldName}" : item.FieldName;
                var content = $"[{severity}][{item.TableName}] {lineInfo}：{item.Message}";
                if (isError)
                {
                    Debug.LogError(content);
                }
                else
                {
                    Debug.LogWarning(content);
                }
            }
        }
    }

    private static CsvTable LoadTable(string relativePath, string displayName)
    {
        var absolutePath = GetProjectFilePath(relativePath);
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException(displayName + "不存在。", absolutePath);
        }

        var rows = ReadCsvRows(absolutePath);
        if (rows.Count <= 1)
        {
            throw new InvalidOperationException(displayName + "为空。");
        }

        var headers = rows[0];
        var table = new CsvTable(relativePath, displayName, headers);
        for (var i = 1; i < rows.Count; i++)
        {
            var columns = rows[i];
            if (!IsRowEmpty(columns))
            {
                table.Rows.Add(new CsvRow(table, i + 1, headers, columns));
            }
        }

        return table;
    }

    private static string GetProjectFilePath(string relativePath)
    {
        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.Combine(projectRoot, relativePath);
    }

    private static List<List<string>> ReadCsvRows(string path)
    {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(stream, Encoding.UTF8, true))
        {
            return ParseCsv(reader.ReadToEnd());
        }
    }

    private static List<List<string>> ParseCsv(string content)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var value = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < content.Length; i++)
        {
            var ch = content[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < content.Length && content[i + 1] == '"')
                {
                    value.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                row.Add(value.ToString());
                value.Clear();
                continue;
            }

            if ((ch == '\n' || ch == '\r') && !inQuotes)
            {
                if (ch == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                {
                    i++;
                }

                row.Add(value.ToString());
                value.Clear();
                rows.Add(row);
                row = new List<string>();
                continue;
            }

            value.Append(ch);
        }

        if (value.Length > 0 || row.Count > 0)
        {
            row.Add(value.ToString());
            rows.Add(row);
        }

        return rows;
    }
    private static bool IsRowEmpty(List<string> columns)
    {
        for (var i = 0; i < columns.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(columns[i]))
            {
                return false;
            }
        }

        return true;
    }

    private sealed class CsvTable
    {
        public CsvTable(string relativePath, string displayName, List<string> headers)
        {
            RelativePath = relativePath;
            DisplayName = displayName;
            Headers = headers;
            Rows = new List<CsvRow>();
        }

        public string RelativePath { get; private set; }
        public string DisplayName { get; private set; }
        public List<string> Headers { get; private set; }
        public List<CsvRow> Rows { get; private set; }
    }

    private sealed class CsvRow
    {
        private readonly Dictionary<string, string> values;

        public CsvRow(CsvTable table, int lineNumber, List<string> headers, List<string> columns)
        {
            Table = table;
            LineNumber = lineNumber;
            values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < headers.Count; i++)
            {
                var header = headers[i];
                var value = i < columns.Count ? columns[i] : string.Empty;
                if (!values.ContainsKey(header))
                {
                    values.Add(header, value);
                }
            }
        }

        public CsvTable Table { get; private set; }
        public int LineNumber { get; private set; }

        public string Get(string fieldName)
        {
            string value;
            return values.TryGetValue(fieldName, out value) ? value : string.Empty;
        }
    }

    private sealed class ValidationItem
    {
        public ValidationItem(string severity, string tableName, int lineNumber, string fieldName, string message)
        {
            Severity = severity;
            TableName = tableName;
            LineNumber = lineNumber;
            FieldName = fieldName;
            Message = message;
        }

        public string Severity { get; private set; }
        public string TableName { get; private set; }
        public int LineNumber { get; private set; }
        public string FieldName { get; private set; }
        public string Message { get; private set; }
    }
}

