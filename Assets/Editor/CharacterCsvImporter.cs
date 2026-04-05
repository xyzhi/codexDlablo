using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Wuxing.Config;
using Wuxing.Localization;
public static class CharacterCsvImporter
{
    private const string CharacterCsvPath = "Docs/Character.csv";
    private const string EnemyCsvPath = "Docs/Enemy.csv";
    private const string SkillCsvPath = "Docs/Skill.csv";
    private const string SkillEffectCsvPath = "Docs/SkillEffect.csv";
    private const string BattleFormulaCsvPath = "Docs/BattleFormula.csv";
    private const string ElementRelationCsvPath = "Docs/ElementRelation.csv";
    private const string EnemyEncounterCsvPath = "Docs/EnemyEncounter.csv";
    private const string EquipmentCsvPath = "Docs/Equipment.csv";
    private const string SpiritStoneCsvPath = "Docs/SpiritStone.csv";
    private const string SpiritStoneConversionCsvPath = "Docs/SpiritStoneConversion.csv";
    private const string StageBalanceCsvPath = "Docs/StageBalance.csv";
    private const string StageNodeCsvPath = "Docs/StageNode.csv";
    private const string EventOptionCsvPath = "Docs/EventOption.csv";
    private const string EventProfileCsvPath = "Docs/EventProfile.csv";
    private const string StoryNodeCsvPath = "Docs/StoryNode.csv";
    private const string StoryTriggerCsvPath = "Docs/StoryTrigger.csv";
    private const string LocalizationCsvPath = "Docs/Localization.csv";
    private const string ConfigOutputFolder = "Assets/Resources/Configs";
    private const string LocalizationOutputFolder = "Assets/Resources/Localization";
    private const string CharacterJsonPath = ConfigOutputFolder + "/CharacterDatabase.json";
    private const string EnemyJsonPath = ConfigOutputFolder + "/EnemyDatabase.json";
    private const string SkillJsonPath = ConfigOutputFolder + "/SkillDatabase.json";
    private const string BattleFormulaJsonPath = ConfigOutputFolder + "/BattleFormulaDatabase.json";
    private const string ElementRelationJsonPath = ConfigOutputFolder + "/ElementRelationDatabase.json";
    private const string EnemyEncounterJsonPath = ConfigOutputFolder + "/EnemyEncounterDatabase.json";
    private const string EquipmentJsonPath = ConfigOutputFolder + "/EquipmentDatabase.json";
    private const string SpiritStoneJsonPath = ConfigOutputFolder + "/SpiritStoneDatabase.json";
    private const string SpiritStoneConversionJsonPath = ConfigOutputFolder + "/SpiritStoneConversionDatabase.json";
    private const string StageBalanceJsonPath = ConfigOutputFolder + "/StageBalanceDatabase.json";
    private const string StageNodeJsonPath = ConfigOutputFolder + "/StageNodeDatabase.json";
    private const string EventOptionJsonPath = ConfigOutputFolder + "/EventOptionDatabase.json";
    private const string EventProfileJsonPath = ConfigOutputFolder + "/EventProfileDatabase.json";
    private const string StoryNodeJsonPath = ConfigOutputFolder + "/StoryNodeDatabase.json";
    private const string StoryTriggerJsonPath = ConfigOutputFolder + "/StoryTriggerDatabase.json";
    private const string LocalizationJsonPath = LocalizationOutputFolder + "/GameText.json";
    [MenuItem("Tools/Config/Import All CSV")]
    public static void ImportAll()
    {
        try
        {
            ImportCharacters();
            ImportEnemies();
            ImportSkills();
            ImportBattleFormula();
            ImportElementRelations();
            ImportEnemyEncounters();
            ImportEquipments();
            ImportSpiritStones();
            ImportSpiritStoneConversions();
            ImportStageBalances();
            ImportStageNodes();
            ImportEventOptions();
            ImportEventProfiles();
            ImportStoryNodes();
            ImportStoryTriggers();
            ImportLocalization();
            AssetDatabase.Refresh();
            Debug.Log("Imported all config CSV files successfully.");
        }
        catch (Exception exception)
        {
            Debug.LogError(exception.Message);
        }
    }
    [MenuItem("Tools/Config/Import Characters CSV")]
    public static void ImportCharacters()
    {
        var rows = ReadRequiredRows(CharacterCsvPath, "Character CSV");
        var configs = new List<CharacterConfig>();
        for (var i = 1; i < rows.Count; i++)
        {
            var columns = rows[i];
            if (IsRowEmpty(columns))
            {
                continue;
            }
            if (columns.Count < 12)
            {
                throw new InvalidOperationException($"Invalid character row at line {i + 1}.");
            }
            configs.Add(new CharacterConfig
            {
                Id = columns[0],
                Name = columns[1],
                ElementRoots = columns[2],
                ClassRole = columns[3],
                Position = columns[4],
                HP = ParseInt(columns[5], nameof(CharacterConfig.HP), i + 1),
                ATK = ParseInt(columns[6], nameof(CharacterConfig.ATK), i + 1),
                DEF = ParseInt(columns[7], nameof(CharacterConfig.DEF), i + 1),
                MP = ParseInt(columns[8], nameof(CharacterConfig.MP), i + 1),
                CombatStyle = columns[9],
                InitialSkills = columns[10],
                GrowthNotes = columns[11]
            });
        }
        EnsureFolderExists(ConfigOutputFolder);
        var database = new CharacterDatabase();
        database.characters = configs;
        WriteJson(CharacterJsonPath, JsonUtility.ToJson(database, true));
        Debug.Log($"Imported {configs.Count} characters to {CharacterJsonPath}");
    }
    [MenuItem("Tools/Config/Import Enemies CSV")]
    public static void ImportEnemies()
    {
        var rows = ReadRequiredRows(EnemyCsvPath, "Enemy CSV");
        var configs = new List<EnemyConfig>();
        for (var i = 1; i < rows.Count; i++)
        {
            var columns = rows[i];
            if (IsRowEmpty(columns))
            {
                continue;
            }
            if (columns.Count < 12)
            {
                throw new InvalidOperationException($"Invalid enemy row at line {i + 1}.");
            }
            configs.Add(new EnemyConfig
            {
                Id = columns[0],
                Name = columns[1],
                Element = columns[2],
                Role = columns[3],
                Position = columns[4],
                HP = ParseInt(columns[5], nameof(EnemyConfig.HP), i + 1),
                ATK = ParseInt(columns[6], nameof(EnemyConfig.ATK), i + 1),
                DEF = ParseInt(columns[7], nameof(EnemyConfig.DEF), i + 1),
                MP = ParseInt(columns[8], nameof(EnemyConfig.MP), i + 1),
                CombatStyle = columns[9],
                Skills = columns[10],
                Notes = columns[11]
            });
        }
        EnsureFolderExists(ConfigOutputFolder);
        var database = new EnemyDatabase();
        database.enemies = configs;
        WriteJson(EnemyJsonPath, JsonUtility.ToJson(database, true));
        Debug.Log($"Imported {configs.Count} enemies to {EnemyJsonPath}");
    }
    [MenuItem("Tools/Config/Import Skills CSV")]
    public static void ImportSkills()
    {
        var rows = ReadRequiredRows(SkillCsvPath, "Skill CSV");
        var effectRows = ReadRequiredRows(SkillEffectCsvPath, "SkillEffect CSV");
        var effectMap = BuildSkillEffectMap(effectRows);
        var configs = new List<SkillConfig>();
        for (var i = 1; i < rows.Count; i++)
        {
            var columns = rows[i];
            if (IsRowEmpty(columns))
            {
                continue;
            }
            if (columns.Count < 19)
            {
                throw new InvalidOperationException($"Invalid skill row at line {i + 1}.");
            }

            List<SkillEffectConfig> effects;
            if (!effectMap.TryGetValue(columns[0], out effects))
            {
                effects = new List<SkillEffectConfig>();
            }

            configs.Add(new SkillConfig
            {
                Id = columns[0],
                Name = columns[1],
                Element = columns[2],
                Quality = columns[3],
                Category = columns[4],
                TargetType = columns[5],
                Priority = ParseInt(columns[6], nameof(SkillConfig.Priority), i + 1),
                Cooldown = ParseInt(columns[7], nameof(SkillConfig.Cooldown), i + 1),
                MPCost = ParseInt(columns[8], nameof(SkillConfig.MPCost), i + 1),
                TriggerType = columns[9],
                TriggerValue = columns[10],
                TargetRule = columns[11],
                CastLimit = ParseInt(columns[12], nameof(SkillConfig.CastLimit), i + 1),
                SkillTags = columns[13],
                Power = ParseInt(columns[14], nameof(SkillConfig.Power), i + 1),
                Duration = ParseInt(columns[15], nameof(SkillConfig.Duration), i + 1),
                EffectType = columns[16],
                Description = columns[17],
                Notes = columns[18],
                Effects = effects
            });
        }
        EnsureFolderExists(ConfigOutputFolder);
        var database = new SkillDatabase();
        database.skills = configs;
        WriteJson(SkillJsonPath, JsonUtility.ToJson(database, true));
        Debug.Log($"Imported {configs.Count} skills to {SkillJsonPath}");
    }
    [MenuItem("Tools/Config/Import Battle Formula CSV")]
    public static void ImportBattleFormula()
    {
        var rows = ReadRequiredRows(BattleFormulaCsvPath, "BattleFormula CSV");
        var columns = rows[1];
        if (columns.Count < 9)
        {
            throw new InvalidOperationException("Invalid battle formula row.");
        }

        var database = new BattleFormulaDatabase
        {
            Formula = new BattleFormulaConfig
            {
                DamageMultiplier = ParseFloat(columns[0], nameof(BattleFormulaConfig.DamageMultiplier), 2),
                FlatDamageBonus = ParseInt(columns[1], nameof(BattleFormulaConfig.FlatDamageBonus), 2),
                HealMultiplier = ParseFloat(columns[2], nameof(BattleFormulaConfig.HealMultiplier), 2),
                ShieldMultiplier = ParseFloat(columns[3], nameof(BattleFormulaConfig.ShieldMultiplier), 2),
                VulnerablePerPoint = ParseFloat(columns[4], nameof(BattleFormulaConfig.VulnerablePerPoint), 2),
                DefenseMitigationFactor = ParseFloat(columns[5], nameof(BattleFormulaConfig.DefenseMitigationFactor), 2),
                CritMultiplier = ParseFloat(columns[6], nameof(BattleFormulaConfig.CritMultiplier), 2),
                MinDamage = ParseFloat(columns[7], nameof(BattleFormulaConfig.MinDamage), 2),
                Notes = columns[8]
            }
        };

        EnsureFolderExists(ConfigOutputFolder);
        WriteJson(BattleFormulaJsonPath, JsonUtility.ToJson(database, true));
        Debug.Log($"Imported battle formula to {BattleFormulaJsonPath}");
    }

    [MenuItem("Tools/Config/Import Element Relation CSV")]
    public static void ImportElementRelations()
    {
        var rows = ReadRequiredRows(ElementRelationCsvPath, "ElementRelation CSV");
        var relations = new List<ElementRelationConfig>();
        for (var i = 1; i < rows.Count; i++)
        {
            var columns = rows[i];
            if (IsRowEmpty(columns))
            {
                continue;
            }
            if (columns.Count < 4)
            {
                throw new InvalidOperationException($"Invalid element relation row at line {i + 1}.");
            }

            relations.Add(new ElementRelationConfig
            {
                AttackerElement = columns[0],
                DefenderElement = columns[1],
                Multiplier = ParseFloat(columns[2], nameof(ElementRelationConfig.Multiplier), i + 1),
                Notes = columns[3]
            });
        }

        EnsureFolderExists(ConfigOutputFolder);
        var database = new ElementRelationDatabase { Relations = relations };
        WriteJson(ElementRelationJsonPath, JsonUtility.ToJson(database, true));
        Debug.Log($"Imported {relations.Count} element relations to {ElementRelationJsonPath}");
    }
    [MenuItem("Tools/Config/Import Enemy Encounter CSV")]
    public static void ImportEnemyEncounters()
    {
        var rows = ReadRequiredRows(EnemyEncounterCsvPath, "EnemyEncounter CSV");
        var configs = new List<EnemyEncounterConfig>();
        for (var i = 1; i < rows.Count; i++)
        {
            var columns = rows[i];
            if (IsRowEmpty(columns))
            {
                continue;
            }

            if (columns.Count < 8)
            {
                throw new InvalidOperationException($"Invalid enemy encounter row at line {i + 1}.");
            }

            configs.Add(new EnemyEncounterConfig
            {
                Id = columns[0],
                StageFrom = ParseInt(columns[1], nameof(EnemyEncounterConfig.StageFrom), i + 1),
                StageTo = ParseInt(columns[2], nameof(EnemyEncounterConfig.StageTo), i + 1),
                NodeType = columns[3],
                EnemyIds = columns[4],
                OverrideElement = columns[5],
                Weight = ParseInt(columns[6], nameof(EnemyEncounterConfig.Weight), i + 1),
                Notes = columns[7]
            });
        }

        EnsureFolderExists(ConfigOutputFolder);
        var database = new EnemyEncounterDatabase { Encounters = configs };
        WriteJson(EnemyEncounterJsonPath, JsonUtility.ToJson(database, true));
        Debug.Log($"Imported {configs.Count} enemy encounters to {EnemyEncounterJsonPath}");
    }
    [MenuItem("Tools/Config/Import Equipment CSV")]
    public static void ImportEquipments()
    {
        var rows = ReadRequiredRows(EquipmentCsvPath, "Equipment CSV");
        var configs = new List<EquipmentConfig>();
        for (var i = 1; i < rows.Count; i++)
        {
            var columns = rows[i];
            if (IsRowEmpty(columns))
            {
                continue;
            }
            if (columns.Count < 8)
            {
                throw new InvalidOperationException($"Invalid equipment row at line {i + 1}.");
            }
            configs.Add(new EquipmentConfig
            {
                Id = columns[0],
                Name = columns[1],
                Slot = columns[2],
                HP = ParseInt(columns[3], nameof(EquipmentConfig.HP), i + 1),
                ATK = ParseInt(columns[4], nameof(EquipmentConfig.ATK), i + 1),
                DEF = ParseInt(columns[5], nameof(EquipmentConfig.DEF), i + 1),
                MP = ParseInt(columns[6], nameof(EquipmentConfig.MP), i + 1),
                Notes = columns[7]
            });
        }
        EnsureFolderExists(ConfigOutputFolder);
        var database = new EquipmentDatabase();
        database.equipments = configs;
        WriteJson(EquipmentJsonPath, JsonUtility.ToJson(database, true));
        Debug.Log($"Imported {configs.Count} equipments to {EquipmentJsonPath}");
    }
    [MenuItem("Tools/Config/Import Spirit Stone CSV")]
    public static void ImportSpiritStones()
    {
        var rows = ReadRequiredRows(SpiritStoneCsvPath, "SpiritStone CSV");
        var configs = new List<SpiritStoneConfig>();
        for (var i = 1; i < rows.Count; i++)
        {
            var columns = rows[i];
            if (IsRowEmpty(columns))
            {
                continue;
            }
            if (columns.Count < 5)
            {
                throw new InvalidOperationException($"Invalid spirit stone row at line {i + 1}.");
            }
            configs.Add(new SpiritStoneConfig
            {
                Id = columns[0],
                Name = columns[1],
                Element = columns[2],
                ColorHex = columns[3],
                Notes = columns[4]
            });
        }
        EnsureFolderExists(ConfigOutputFolder);
        var database = new SpiritStoneDatabase();
        database.spiritStones = configs;
        WriteJson(SpiritStoneJsonPath, JsonUtility.ToJson(database, true));
        Debug.Log($"Imported {configs.Count} spirit stones to {SpiritStoneJsonPath}");
    }
    [MenuItem("Tools/Config/Import Spirit Stone Conversion CSV")]
    public static void ImportSpiritStoneConversions()
    {
        var rows = ReadRequiredRows(SpiritStoneConversionCsvPath, "SpiritStoneConversion CSV");
        var configs = new List<SpiritStoneConversionConfig>();
        for (var i = 1; i < rows.Count; i++)
        {
            var columns = rows[i];
            if (IsRowEmpty(columns))
            {
                continue;
            }
            if (columns.Count < 5)
            {
                throw new InvalidOperationException($"Invalid spirit stone conversion row at line {i + 1}.");
            }
            configs.Add(new SpiritStoneConversionConfig
            {
                SourceElement = columns[0],
                TargetElement = columns[1],
                CostAmount = ParseInt(columns[2], nameof(SpiritStoneConversionConfig.CostAmount), i + 1),
                GainAmount = ParseInt(columns[3], nameof(SpiritStoneConversionConfig.GainAmount), i + 1),
                Notes = columns[4]
            });
        }
        EnsureFolderExists(ConfigOutputFolder);
        var database = new SpiritStoneConversionDatabase();
        database.conversions = configs;
        WriteJson(SpiritStoneConversionJsonPath, JsonUtility.ToJson(database, true));
        Debug.Log($"Imported {configs.Count} spirit stone conversions to {SpiritStoneConversionJsonPath}");
    }
    [MenuItem("Tools/Config/Import Stage Balance CSV")]
    public static void ImportStageBalances()
    {
        var rows = ReadRequiredRows(StageBalanceCsvPath, "StageBalance CSV");
        var configs = new List<StageBalanceConfig>();
        for (var i = 1; i < rows.Count; i++)
        {
            var columns = rows[i];
            if (IsRowEmpty(columns))
            {
                continue;
            }
            if (columns.Count < 7)
            {
                throw new InvalidOperationException($"Invalid stage balance row at line {i + 1}.");
            }
            configs.Add(new StageBalanceConfig
            {
                Stage = ParseInt(columns[0], nameof(StageBalanceConfig.Stage), i + 1),
                EnemyHPBonus = ParseInt(columns[1], nameof(StageBalanceConfig.EnemyHPBonus), i + 1),
                EnemyATKBonus = ParseInt(columns[2], nameof(StageBalanceConfig.EnemyATKBonus), i + 1),
                EnemyDEFBonus = ParseInt(columns[3], nameof(StageBalanceConfig.EnemyDEFBonus), i + 1),
                EnemyMPBonus = ParseInt(columns[4], nameof(StageBalanceConfig.EnemyMPBonus), i + 1),
                EnemyEquipmentCount = ParseInt(columns[5], nameof(StageBalanceConfig.EnemyEquipmentCount), i + 1),
                Notes = columns[6]
            });
        }
        EnsureFolderExists(ConfigOutputFolder);
        var database = new StageBalanceDatabase();
        database.stageBalances = configs;
        if (!TryWriteStageBalanceJson(database))
        {
            Debug.Log($"Stage balances unchanged, skipped writing {StageBalanceJsonPath}");
            return;
        }

        Debug.Log($"Imported {configs.Count} stage balances to {StageBalanceJsonPath}");
    }
    [MenuItem("Tools/Config/Import Stage Nodes CSV")]
    public static void ImportStageNodes()
    {
        var rows = ReadRequiredRows(StageNodeCsvPath, "StageNode CSV");
        var configs = new List<StageNodeConfig>();
        for (var i = 1; i < rows.Count; i++)
        {
            var columns = rows[i];
            if (IsRowEmpty(columns))
            {
                continue;
            }
            if (columns.Count < 15)
            {
                throw new InvalidOperationException($"Invalid stage node row at line {i + 1}.");
            }
            configs.Add(new StageNodeConfig
            {
                Stage = ParseInt(columns[0], nameof(StageNodeConfig.Stage), i + 1),
                NodeType = columns[1],
                EventProfile = columns[2],
                SpiritStoneElement = columns[3],
                ThemeZh = columns[4],
                ThemeEn = columns[5],
                DetailZh = columns[6],
                DetailEn = columns[7],
                EventMode = columns[8],
                CooldownMonths = ParseInt(columns[9], nameof(StageNodeConfig.CooldownMonths), i + 1),
                BattleExpReward = ParseInt(columns[10], nameof(StageNodeConfig.BattleExpReward), i + 1),
                BattleSpiritStoneReward = ParseInt(columns[11], nameof(StageNodeConfig.BattleSpiritStoneReward), i + 1),
                NonBattleExpReward = ParseInt(columns[12], nameof(StageNodeConfig.NonBattleExpReward), i + 1),
                NonBattleSpiritStoneReward = ParseInt(columns[13], nameof(StageNodeConfig.NonBattleSpiritStoneReward), i + 1),
                Notes = columns[14]
            });
        }
        EnsureFolderExists(ConfigOutputFolder);
        var database = new StageNodeDatabase();
        database.stageNodes = configs;
        WriteJson(StageNodeJsonPath, JsonUtility.ToJson(database, true));
        Debug.Log($"Imported {configs.Count} stage nodes to {StageNodeJsonPath}");
    }
    [MenuItem("Tools/Config/Import Event Options CSV")]
    public static void ImportEventOptions()
    {
        var rows = ReadRequiredRows(EventOptionCsvPath, "EventOption CSV");
        var configs = new List<EventOptionConfig>();
        for (var i = 1; i < rows.Count; i++)
        {
            var columns = rows[i];
            if (IsRowEmpty(columns))
            {
                continue;
            }
            if (columns.Count < 25)
            {
                throw new InvalidOperationException($"Invalid event option row at line {i + 1}.");
            }
            configs.Add(new EventOptionConfig
            {
                Profile = columns[0],
                OptionIndex = ParseInt(columns[1], nameof(EventOptionConfig.OptionIndex), i + 1),
                TitleKey = columns[2],
                RewardMode = columns[3],
                UtilityAction = columns[4],
                SpiritStoneElement = columns[5],
                ExpBase = ParseInt(columns[6], nameof(EventOptionConfig.ExpBase), i + 1),
                ExpPerStage = ParseInt(columns[7], nameof(EventOptionConfig.ExpPerStage), i + 1),
                SpiritStoneBase = ParseInt(columns[8], nameof(EventOptionConfig.SpiritStoneBase), i + 1),
                SpiritStonePerStage = ParseInt(columns[9], nameof(EventOptionConfig.SpiritStonePerStage), i + 1),
                SpiritStoneCostBase = ParseInt(columns[10], nameof(EventOptionConfig.SpiritStoneCostBase), i + 1),
                SpiritStoneCostPerStage = ParseInt(columns[11], nameof(EventOptionConfig.SpiritStoneCostPerStage), i + 1),
                SkillRewardNodeType = columns[12],
                SelectionTitleKey = columns[13],
                SelectionMessageKey = columns[14],
                ResultTitleKey = columns[15],
                ResultIntroKey = columns[16],
                EmptyResultKey = columns[17],
                BuffType = columns[18],
                BuffValueBase = ParseInt(columns[19], nameof(EventOptionConfig.BuffValueBase), i + 1),
                BuffValuePerStage = ParseInt(columns[20], nameof(EventOptionConfig.BuffValuePerStage), i + 1),
                BuffDurationMonths = ParseInt(columns[21], nameof(EventOptionConfig.BuffDurationMonths), i + 1),
                BuffTitleKey = columns[22],
                BuffDescriptionKey = columns[23],
                Notes = columns[24]
            });
        }
        EnsureFolderExists(ConfigOutputFolder);
        var database = new EventOptionDatabase();
        database.eventOptions = configs;
        WriteJson(EventOptionJsonPath, JsonUtility.ToJson(database, true));
        Debug.Log($"Imported {configs.Count} event options to {EventOptionJsonPath}");
    }
    [MenuItem("Tools/Config/Import Event Profiles CSV")]
    public static void ImportEventProfiles()
    {
        var rows = ReadRequiredRows(EventProfileCsvPath, "EventProfile CSV");
        var configs = new List<EventProfileConfig>();
        for (var i = 1; i < rows.Count; i++)
        {
            var columns = rows[i];
            if (IsRowEmpty(columns))
            {
                continue;
            }
            if (columns.Count < 4)
            {
                throw new InvalidOperationException($"Invalid event profile row at line {i + 1}.");
            }
            configs.Add(new EventProfileConfig
            {
                Profile = columns[0],
                TitleKey = columns[1],
                MessageKey = columns[2],
                Notes = columns[3]
            });
        }
        EnsureFolderExists(ConfigOutputFolder);
        var database = new EventProfileDatabase();
        database.eventProfiles = configs;
        WriteJson(EventProfileJsonPath, JsonUtility.ToJson(database, true));
        Debug.Log($"Imported {configs.Count} event profiles to {EventProfileJsonPath}");
    }
    [MenuItem("Tools/Config/Import Localization CSV")]
    public static void ImportLocalization()
    {
        var rows = ReadRequiredRows(LocalizationCsvPath, "Localization CSV");
        var entries = new List<LocalizationEntry>();
        for (var i = 1; i < rows.Count; i++)
        {
            var columns = rows[i];
            if (IsRowEmpty(columns))
            {
                continue;
            }
            if (columns.Count < 3)
            {
                throw new InvalidOperationException($"Invalid localization row at line {i + 1}.");
            }
            entries.Add(new LocalizationEntry
            {
                key = columns[0],
                zhHans = NormalizeLocalizationText(columns[1]),
                en = NormalizeLocalizationText(columns[2])
            });
        }
        EnsureFolderExists(LocalizationOutputFolder);
        var table = new LocalizationTable();
        table.entries = entries.ToArray();
        WriteJson(LocalizationJsonPath, JsonUtility.ToJson(table, true));
        Debug.Log($"Imported {entries.Count} localization rows to {LocalizationJsonPath}");
    }
    [MenuItem("Tools/Config/Import Story Nodes CSV")]
    public static void ImportStoryNodes()
    {
        var rows = ReadRequiredRows(StoryNodeCsvPath, "StoryNode CSV");
        var configs = new List<StoryNodeConfig>();
        for (var i = 1; i < rows.Count; i++)
        {
            var columns = rows[i];
            if (IsRowEmpty(columns))
            {
                continue;
            }

            if (columns.Count < 12)
            {
                throw new InvalidOperationException($"Invalid story node row at line {i + 1}.");
            }

            var speakerKey = columns[2];
            var leftSpeakerKey = columns.Count >= 15 ? columns[3] : speakerKey;
            var rightSpeakerKey = columns.Count >= 15 ? columns[4] : string.Empty;
            var activeSpeakerSide = columns.Count >= 15 ? columns[5] : "Left";
            var titleKey = columns.Count >= 15 ? columns[6] : columns[3];
            var contentKey = columns.Count >= 15 ? columns[7] : columns[4];
            var typingCharsPerSecond = columns.Count >= 15 ? ParseInt(columns[8], nameof(StoryNodeConfig.TypingCharsPerSecond), i + 1) : ParseInt(columns[5], nameof(StoryNodeConfig.TypingCharsPerSecond), i + 1);
            var skipHintDelay = columns.Count >= 15 ? ParseFloat(columns[9], nameof(StoryNodeConfig.SkipHintDelay), i + 1) : ParseFloat(columns[6], nameof(StoryNodeConfig.SkipHintDelay), i + 1);
            var skippable = columns.Count >= 15 ? ParseBool(columns[10]) : ParseBool(columns[7]);
            var nextNodeId = columns.Count >= 15 ? columns[11] : columns[8];
            var callbackKey = columns.Count >= 15 ? columns[12] : columns[9];
            var callbackParam = columns.Count >= 15 ? columns[13] : columns[10];
            var notes = columns.Count >= 15 ? columns[14] : columns[11];

            configs.Add(new StoryNodeConfig
            {
                Id = columns[0],
                Type = columns[1],
                SpeakerKey = speakerKey,
                LeftSpeakerKey = leftSpeakerKey,
                RightSpeakerKey = rightSpeakerKey,
                ActiveSpeakerSide = activeSpeakerSide,
                TitleKey = titleKey,
                ContentKey = contentKey,
                TypingCharsPerSecond = typingCharsPerSecond,
                SkipHintDelay = skipHintDelay,
                Skippable = skippable,
                NextNodeId = nextNodeId,
                CallbackKey = callbackKey,
                CallbackParam = callbackParam,
                Notes = notes
            });
        }

        EnsureFolderExists(ConfigOutputFolder);
        var database = new StoryNodeDatabase { storyNodes = configs };
        WriteJson(StoryNodeJsonPath, JsonUtility.ToJson(database, true));
        StoryNodeDatabaseLoader.ClearCache();
        Debug.Log($"Imported {configs.Count} story nodes to {StoryNodeJsonPath}");
    }

    [MenuItem("Tools/Config/Import Story Triggers CSV")]
    public static void ImportStoryTriggers()
    {
        var rows = ReadRequiredRows(StoryTriggerCsvPath, "StoryTrigger CSV");
        var configs = new List<StoryTriggerConfig>();
        for (var i = 1; i < rows.Count; i++)
        {
            var columns = rows[i];
            if (IsRowEmpty(columns))
            {
                continue;
            }

            if (columns.Count < 7)
            {
                throw new InvalidOperationException($"Invalid story trigger row at line {i + 1}.");
            }

            configs.Add(new StoryTriggerConfig
            {
                Id = columns[0],
                TriggerKey = columns[1],
                Stage = ParseInt(columns[2], nameof(StoryTriggerConfig.Stage), i + 1),
                NodeId = columns[3],
                OncePerRun = ParseBool(columns[4]),
                Enabled = ParseBool(columns[5]),
                Notes = columns[6]
            });
        }

        EnsureFolderExists(ConfigOutputFolder);
        var database = new StoryTriggerDatabase { storyTriggers = configs };
        WriteJson(StoryTriggerJsonPath, JsonUtility.ToJson(database, true));
        StoryTriggerDatabaseLoader.ClearCache();
        Debug.Log($"Imported {configs.Count} story triggers to {StoryTriggerJsonPath}");
    }
    private static Dictionary<string, List<SkillEffectConfig>> BuildSkillEffectMap(List<List<string>> rows)
    {
        var effectMap = new Dictionary<string, List<SkillEffectConfig>>(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i < rows.Count; i++)
        {
            var columns = rows[i];
            if (IsRowEmpty(columns))
            {
                continue;
            }

            if (columns.Count < 7)
            {
                throw new InvalidOperationException($"Invalid skill effect row at line {i + 1}.");
            }

            var skillId = columns[0];
            if (string.IsNullOrWhiteSpace(skillId))
            {
                throw new InvalidOperationException($"Skill effect row at line {i + 1} is missing SkillId.");
            }

            List<SkillEffectConfig> list;
            if (!effectMap.TryGetValue(skillId, out list))
            {
                list = new List<SkillEffectConfig>();
                effectMap[skillId] = list;
            }

            list.Add(new SkillEffectConfig
            {
                EffectIndex = ParseInt(columns[1], nameof(SkillEffectConfig.EffectIndex), i + 1),
                EffectType = columns[2],
                TargetScope = columns[3],
                Value = ParseInt(columns[4], nameof(SkillEffectConfig.Value), i + 1),
                ValuePerLevel = ParseInt(columns[5], nameof(SkillEffectConfig.ValuePerLevel), i + 1),
                DurationRounds = ParseInt(columns[6], nameof(SkillEffectConfig.DurationRounds), i + 1),
                MaxStacks = ParseInt(columns[7], nameof(SkillEffectConfig.MaxStacks), i + 1),
                StackRule = columns[8],
                TriggerTiming = columns[9],
                Notes = columns[10]
            });
        }

        foreach (var pair in effectMap)
        {
            pair.Value.Sort((left, right) => left.EffectIndex.CompareTo(right.EffectIndex));
        }

        return effectMap;
    }
    private static string NormalizeLocalizationText(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("\r\n", "\n").Replace("\r", "\n");
    }
    private static List<List<string>> ReadRequiredRows(string relativePath, string displayName)
    {
        var csvPath = GetProjectFilePath(relativePath);
        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException(displayName + " not found.", csvPath);
        }
        var rows = ReadCsvRows(csvPath);
        if (rows.Count <= 1)
        {
            throw new InvalidOperationException(displayName + " is empty.");
        }
        return rows;
    }
    private static string GetProjectFilePath(string relativePath)
    {
        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.Combine(projectRoot, relativePath);
    }
    private static void WriteJson(string assetRelativePath, string json)
    {
        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var absolutePath = Path.Combine(projectRoot, assetRelativePath);
        if (File.Exists(absolutePath))
        {
            var existingJson = File.ReadAllText(absolutePath, Encoding.UTF8);
            if (AreJsonContentsEquivalent(existingJson, json))
            {
                return;
            }
        }

        File.WriteAllText(absolutePath, json, new UTF8Encoding(true));
    }

    private static bool TryWriteStageBalanceJson(StageBalanceDatabase database)
    {
        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var absolutePath = Path.Combine(projectRoot, StageBalanceJsonPath);
        if (File.Exists(absolutePath))
        {
            var existingJson = File.ReadAllText(absolutePath, Encoding.UTF8);
            var existingDatabase = JsonUtility.FromJson<StageBalanceDatabase>(existingJson);
            if (AreStageBalanceDatabasesEquivalent(existingDatabase, database))
            {
                return false;
            }
        }

        File.WriteAllText(absolutePath, JsonUtility.ToJson(database, true), new UTF8Encoding(true));
        return true;
    }

    private static bool AreStageBalanceDatabasesEquivalent(StageBalanceDatabase left, StageBalanceDatabase right)
    {
        var leftItems = left != null && left.stageBalances != null ? left.stageBalances : new List<StageBalanceConfig>();
        var rightItems = right != null && right.stageBalances != null ? right.stageBalances : new List<StageBalanceConfig>();
        if (leftItems.Count != rightItems.Count)
        {
            return false;
        }

        for (var i = 0; i < leftItems.Count; i++)
        {
            var leftItem = leftItems[i];
            var rightItem = rightItems[i];
            if (leftItem == null || rightItem == null)
            {
                if (leftItem != rightItem)
                {
                    return false;
                }

                continue;
            }

            if (leftItem.Stage != rightItem.Stage
                || leftItem.EnemyHPBonus != rightItem.EnemyHPBonus
                || leftItem.EnemyATKBonus != rightItem.EnemyATKBonus
                || leftItem.EnemyDEFBonus != rightItem.EnemyDEFBonus
                || leftItem.EnemyMPBonus != rightItem.EnemyMPBonus
                || leftItem.EnemyEquipmentCount != rightItem.EnemyEquipmentCount
                || !string.Equals(leftItem.Notes ?? string.Empty, rightItem.Notes ?? string.Empty, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AreJsonContentsEquivalent(string left, string right)
    {
        return string.Equals(
            CompactJsonForComparison(left),
            CompactJsonForComparison(right),
            StringComparison.Ordinal);
    }

    private static string CompactJsonForComparison(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(json.Length);
        var inString = false;
        var escaping = false;

        for (var i = 0; i < json.Length; i++)
        {
            var ch = json[i];
            if (inString)
            {
                builder.Append(ch);
                if (escaping)
                {
                    escaping = false;
                }
                else if (ch == '\\')
                {
                    escaping = true;
                }
                else if (ch == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (ch == '"')
            {
                inString = true;
                builder.Append(ch);
                continue;
            }

            if (!char.IsWhiteSpace(ch))
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }

    private static float ParseFloat(string value, string fieldName, int lineNumber)
    {
        float result;
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
        {
            return result;
        }
        throw new FormatException($"Failed to parse {fieldName} at line {lineNumber}: {value}");
    }
    private static int ParseInt(string value, string fieldName, int lineNumber)
    {
        int result;
        if (int.TryParse(value, out result))
        {
            return result;
        }
        throw new FormatException($"Failed to parse {fieldName} at line {lineNumber}: {value}");
    }
    private static bool ParseBool(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        return string.Equals(normalized, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "yes", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "y", StringComparison.OrdinalIgnoreCase);
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
    private static void EnsureFolderExists(string assetFolderPath)
    {
        var parts = assetFolderPath.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}





