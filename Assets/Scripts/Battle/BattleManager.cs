using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Wuxing.Config;
using Wuxing.Game;
using Wuxing.Localization;

namespace Wuxing.Battle
{
    public static class BattleManager
    {
        private static readonly string[] DefaultPlayerIds = { "C001" };
        private static readonly string[] DefaultEnemyIds = { "E001", "E004" };
        private static readonly Dictionary<string, string[]> PlayerEquipmentPresetA = new Dictionary<string, string[]>
        {
            { "C001", new[] { "EQ001", "EQ003", "EQ006" } }
        };
        private static readonly Dictionary<string, string[]> PlayerEquipmentPresetB = new Dictionary<string, string[]>
        {
            { "C001", new[] { "EQ002", "EQ004", "EQ005" } }
        };
        private static readonly Dictionary<string, string[]> DefaultEnemyEquipmentIds = new Dictionary<string, string[]>
        {
            { "E001", new[] { "EQ001", "EQ003" } },
            { "E004", new[] { "EQ002", "EQ004" } }
        };
        private static readonly string[] EquipmentSlots = { "Weapon", "Armor", "Accessory" };
        private const string EquipmentPresetPrefKey = "battle.equipment.preset";
        private const string EquipmentOverridePrefKey = "battle.equipment.overrides";
        private static int playerEquipmentPresetIndex;
        private static readonly Dictionary<string, Dictionary<string, string>> PlayerEquipmentOverrides = new Dictionary<string, Dictionary<string, string>>();
        private static bool equipmentSettingsLoaded;

        public static void CyclePlayerEquipmentPreset()
        {
            EnsureEquipmentSettingsLoaded();
            playerEquipmentPresetIndex = (playerEquipmentPresetIndex + 1) % 2;
            PlayerEquipmentOverrides.Clear();
            SaveEquipmentSettings();
        }

        public static void ResetPlayerEquipmentOverrides()
        {
            EnsureEquipmentSettingsLoaded();
            PlayerEquipmentOverrides.Clear();
            SaveEquipmentSettings();
        }

        public static string GetCurrentPlayerEquipmentPresetName()
        {
            EnsureEquipmentSettingsLoaded();
            return playerEquipmentPresetIndex == 0
                ? LocalizationManager.GetText("battle.equipment_preset_balanced")
                : LocalizationManager.GetText("battle.equipment_preset_burst");
        }

        public static int GetPlayerEquipmentUnitCount()
        {
            var database = CharacterDatabaseLoader.Load();
            if (database == null)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < DefaultPlayerIds.Length; i++)
            {
                if (database.GetById(DefaultPlayerIds[i]) != null)
                {
                    count++;
                }
            }

            return count;
        }

        public static string GetPlayerEquipmentUnitName(int unitIndex)
        {
            var config = GetPlayerCharacterConfig(unitIndex);
            return config != null ? config.Name : LocalizationManager.GetText("battle.summary_no_units");
        }

        public static string GetPlayerEquipmentName(int unitIndex, string slot)
        {
            EnsureEquipmentSettingsLoaded();
            var config = GetPlayerCharacterConfig(unitIndex);
            if (config == null)
            {
                return LocalizationManager.GetText("battle.equipment_none");
            }

            var equipmentDatabase = EquipmentDatabaseLoader.Load();
            if (equipmentDatabase == null)
            {
                return LocalizationManager.GetText("battle.equipment_none");
            }

            var equipmentInstanceId = GetEquippedPlayerEquipmentInstanceId(config.Id, slot, equipmentDatabase);
            if (string.IsNullOrEmpty(equipmentInstanceId))
            {
                return LocalizationManager.GetText("battle.equipment_none");
            }

            var equipment = GetEquipmentConfigByInstanceId(equipmentDatabase, equipmentInstanceId);
            return equipment != null ? equipment.Name : LocalizationManager.GetText("battle.equipment_none");
        }

        public static string GetEquippedPlayerEquipmentInstanceId(int unitIndex, string slot)
        {
            var config = GetPlayerCharacterConfig(unitIndex);
            if (config == null)
            {
                return string.Empty;
            }

            return GetEquippedPlayerEquipmentInstanceId(config.Id, slot, EquipmentDatabaseLoader.Load()) ?? string.Empty;
        }

        public static void CyclePlayerEquipmentForUnitSlot(string unitId, string slot)
        {
            EnsureEquipmentSettingsLoaded();
            if (string.IsNullOrEmpty(unitId) || string.IsNullOrEmpty(slot))
            {
                return;
            }

            var equipmentDatabase = EquipmentDatabaseLoader.Load();
            if (equipmentDatabase == null)
            {
                return;
            }

            var candidates = GetOwnedEquipmentInstanceCandidates(equipmentDatabase, slot);
            if (candidates.Count == 0)
            {
                return;
            }

            var currentEquipmentId = GetEquippedPlayerEquipmentInstanceId(unitId, slot, equipmentDatabase);
            var currentIndex = -1;
            for (var i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] != null && candidates[i].InstanceId == currentEquipmentId)
                {
                    currentIndex = i;
                    break;
                }
            }

            var nextEquipment = candidates[(currentIndex + 1 + candidates.Count) % candidates.Count];
            SetPlayerEquipmentOverride(unitId, slot, nextEquipment != null ? nextEquipment.InstanceId : string.Empty);
            SaveEquipmentSettings();
        }

        public static void CyclePlayerEquipmentForUnitIndexSlot(int unitIndex, string slot)
        {
            var config = GetPlayerCharacterConfig(unitIndex);
            if (config == null)
            {
                return;
            }

            CyclePlayerEquipmentForUnitSlot(config.Id, slot);
        }

        public static void EquipOwnedItemForUnitIndex(int unitIndex, string equipmentInstanceId)
        {
            EnsureEquipmentSettingsLoaded();

            var config = GetPlayerCharacterConfig(unitIndex);
            if (config == null || string.IsNullOrEmpty(equipmentInstanceId) || !GameProgressManager.OwnsEquipmentInstance(equipmentInstanceId))
            {
                return;
            }

            var equipmentDatabase = EquipmentDatabaseLoader.Load();
            if (equipmentDatabase == null)
            {
                return;
            }

            var equipment = GetEquipmentConfigByInstanceId(equipmentDatabase, equipmentInstanceId);
            if (equipment == null || string.IsNullOrEmpty(equipment.Slot))
            {
                return;
            }

            SetPlayerEquipmentOverride(config.Id, equipment.Slot, equipmentInstanceId);
            SaveEquipmentSettings();
        }

        public static void UnequipPlayerEquipmentForUnitIndexSlot(int unitIndex, string slot)
        {
            EnsureEquipmentSettingsLoaded();

            var config = GetPlayerCharacterConfig(unitIndex);
            if (config == null || string.IsNullOrEmpty(slot))
            {
                return;
            }

            SetPlayerEquipmentOverride(config.Id, slot, string.Empty);
            SaveEquipmentSettings();
        }

        public static List<EquipmentConfig> GetOwnedEquipmentsForSlot(string slot)
        {
            var equipmentDatabase = EquipmentDatabaseLoader.Load();
            if (equipmentDatabase == null || string.IsNullOrEmpty(slot))
            {
                return new List<EquipmentConfig>();
            }

            var instances = GetOwnedEquipmentInstanceCandidates(equipmentDatabase, slot);
            var results = new List<EquipmentConfig>();
            for (var i = 0; i < instances.Count; i++)
            {
                var config = equipmentDatabase.GetById(instances[i].EquipmentId);
                if (config != null)
                {
                    results.Add(config);
                }
            }

            return results;
        }

        public static List<EquipmentInstanceData> GetOwnedEquipmentInstancesForSlot(string slot)
        {
            var equipmentDatabase = EquipmentDatabaseLoader.Load();
            if (equipmentDatabase == null || string.IsNullOrEmpty(slot))
            {
                return new List<EquipmentInstanceData>();
            }

            return GetOwnedEquipmentInstanceCandidates(equipmentDatabase, slot);
        }

        public static EquipmentConfig GetOwnedEquipmentConfigByInstance(string equipmentInstanceId)
        {
            return GetEquipmentConfigByInstanceId(EquipmentDatabaseLoader.Load(), equipmentInstanceId);
        }

        public static void AutoEquipPlayerUnitOffense(int unitIndex)
        {
            AutoEquipPlayerUnitByFocus(unitIndex, true);
        }

        public static void AutoEquipPlayerUnitDefense(int unitIndex)
        {
            AutoEquipPlayerUnitByFocus(unitIndex, false);
        }

        public static BattlePlaybackResult RunSampleBattlePlayback()
        {
            EnsureEquipmentSettingsLoaded();
            var characterDatabase = CharacterDatabaseLoader.Load();
            var enemyDatabase = EnemyDatabaseLoader.Load();
            var skillDatabase = SkillDatabaseLoader.Load();
            var equipmentDatabase = EquipmentDatabaseLoader.Load();

            if (characterDatabase == null || enemyDatabase == null || skillDatabase == null || equipmentDatabase == null)
            {
                var failedResult = new BattlePlaybackResult
                {
                    IsVictory = false,
                    WinnerSide = "None",
                    TotalRounds = 0,
                    FinalPlayerTeamSummary = LocalizationManager.GetText("battle.summary_no_data"),
                    FinalEnemyTeamSummary = LocalizationManager.GetText("battle.summary_no_data")
                };

                failedResult.Events.Add(new BattleEvent
                {
                    Type = BattleEventType.BattleEnd,
                    Log = LocalizationManager.GetText("battle.log_config_missing"),
                    PlayerTeamSummary = failedResult.FinalPlayerTeamSummary,
                    EnemyTeamSummary = failedResult.FinalEnemyTeamSummary,
                    IsBattleFinished = true,
                    IsVictory = false
                });

                return failedResult;
            }

            var playerTeam = BuildPlayerTeam(characterDatabase, equipmentDatabase);
            var enemyTeam = BuildEnemyTeam(enemyDatabase, equipmentDatabase);
            return SimulatePlayback(playerTeam, enemyTeam, skillDatabase);
        }

        public static BattleResult RunSampleBattle()
        {
            var playback = RunSampleBattlePlayback();
            var result = new BattleResult
            {
                IsVictory = playback.IsVictory,
                WinnerSide = playback.WinnerSide,
                TotalRounds = playback.TotalRounds,
                PlayerTeamSummary = playback.FinalPlayerTeamSummary,
                EnemyTeamSummary = playback.FinalEnemyTeamSummary
            };

            for (var i = 0; i < playback.Events.Count; i++)
            {
                if (!string.IsNullOrEmpty(playback.Events[i].Log))
                {
                    result.Logs.Add(playback.Events[i].Log);
                }
            }

            return result;
        }

        public static string BuildBattlePreparationStatus()
        {
            var stage = Mathf.Max(1, GameProgressManager.GetCurrentStage());
            return LocalizationManager.GetText("battle.prep_status_prefix")
                + stage
                + LocalizationManager.GetText("battle.prep_status_suffix");
        }

        public static string BuildBattlePreparationSummary()
        {
            var playback = RunSampleBattlePlayback();
            var enemyNames = BuildCurrentEnemyNameList();
            var stage = Mathf.Max(1, GameProgressManager.GetCurrentStage());
            var isEnglish = GetCurrentLanguageSafe() == GameLanguage.English;

            var builder = new StringBuilder();
            builder.Append(LocalizationManager.GetText("battle.prep_briefing_prefix"))
                .Append(stage)
                .Append(LocalizationManager.GetText("battle.prep_briefing_suffix"))
                .Append('\n')
                .Append(LocalizationManager.GetText("battle.prep_objective"))
                .Append('\n')
                .Append(LocalizationManager.GetText("battle.prep_enemies"))
                .Append(enemyNames)
                .Append('\n')
                .Append(LocalizationManager.GetText("battle.prep_threat"))
                .Append(GetStageThreatLabel(stage, isEnglish))
                .Append('\n')
                .Append(LocalizationManager.GetText("battle.prep_preview"))
                .Append(playback.IsVictory
                    ? LocalizationManager.GetText("battle.prep_likely_victory")
                    : LocalizationManager.GetText("battle.prep_likely_defeat"))
                .Append(LocalizationManager.GetText("battle.prep_rounds_prefix"))
                .Append(playback.TotalRounds)
                .Append(LocalizationManager.GetText("battle.prep_rounds_suffix"))
                .Append('\n')
                .Append(LocalizationManager.GetText("battle.prep_tip"));
            return builder.ToString();
        }

        public static string FormatTeam(BattleTeamRuntime team)
        {
            if (team == null || team.Units.Count == 0)
            {
                return LocalizationManager.GetText("battle.summary_no_units");
            }

            var builder = new StringBuilder();
            for (var i = 0; i < team.Units.Count; i++)
            {
                var unit = team.Units[i];
                if (i > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(unit.Name)
                    .Append("  HP ")
                    .Append(unit.CurrentHP)
                    .Append('/')
                    .Append(unit.MaxHP)
                    .Append("  MP ")
                    .Append(unit.CurrentMP)
                    .Append('/')
                    .Append(unit.MaxMP);

                if (unit.IsDead)
                {
                    builder.Append("  [")
                        .Append(LocalizationManager.GetText("battle.summary_dead"))
                        .Append(']');
                }
            }

            return builder.ToString();
        }

        private static string BuildCurrentEnemyNameList()
        {
            var database = EnemyDatabaseLoader.Load();
            if (database == null)
            {
                return LocalizationManager.GetText("battle.summary_no_data");
            }

            var enemyIds = GetConfiguredEnemyIds();
            var builder = new StringBuilder();
            for (var i = 0; i < enemyIds.Length; i++)
            {
                var config = database.GetById(enemyIds[i]);
                if (config == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(LocalizationManager.GetText("common.separator_names"));
                }

                builder.Append(config.Name);
            }

            return builder.Length > 0 ? builder.ToString() : LocalizationManager.GetText("battle.summary_no_data");
        }

        public static string FormatEquipmentTeam(BattleTeamRuntime team)
        {
            if (team == null || team.Units.Count == 0)
            {
                return LocalizationManager.GetText("battle.equipment_none");
            }

            var equipmentDatabase = EquipmentDatabaseLoader.Load();
            var builder = new StringBuilder();

            for (var i = 0; i < team.Units.Count; i++)
            {
                var unit = team.Units[i];
                if (i > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(unit.Name)
                    .Append(": ");

                if (unit.EquippedItemIds == null || unit.EquippedItemIds.Count == 0 || equipmentDatabase == null)
                {
                    builder.Append(LocalizationManager.GetText("battle.equipment_none"));
                    continue;
                }

                for (var j = 0; j < unit.EquippedItemIds.Count; j++)
                {
                    if (j > 0)
                    {
                        builder.Append(" / ");
                    }

                    var equipment = equipmentDatabase.GetById(unit.EquippedItemIds[j]);
                    builder.Append(equipment != null ? equipment.Name : unit.EquippedItemIds[j]);
                }
            }

            return builder.ToString();
        }

        public static string BuildDefaultEquipmentDetailText()
        {
            var characterDatabase = CharacterDatabaseLoader.Load();
            var enemyDatabase = EnemyDatabaseLoader.Load();
            var equipmentDatabase = EquipmentDatabaseLoader.Load();

            if (characterDatabase == null || enemyDatabase == null || equipmentDatabase == null)
            {
                return LocalizationManager.GetText("battle.log_config_missing");
            }

            var playerTeam = BuildPlayerTeam(characterDatabase, equipmentDatabase);
            var enemyTeam = BuildEnemyTeam(enemyDatabase, equipmentDatabase);

            var builder = new StringBuilder();
            builder.Append(LocalizationManager.GetText("battle.player_team"))
                .Append(" [")
                .Append(GetCurrentPlayerEquipmentPresetName())
                .Append(']')
                .Append('\n')
                .Append(FormatEquipmentDetailTeam(playerTeam, equipmentDatabase))
                .Append("\n\n")
                .Append(LocalizationManager.GetText("battle.enemy_team"))
                .Append('\n')
                .Append(FormatEquipmentDetailTeam(enemyTeam, equipmentDatabase));
            return builder.ToString();
        }

        public static string BuildPlayerEquipmentEditorText(int selectedUnitIndex)
        {
            EnsureEquipmentSettingsLoaded();
            var characterDatabase = CharacterDatabaseLoader.Load();
            var equipmentDatabase = EquipmentDatabaseLoader.Load();
            if (characterDatabase == null || equipmentDatabase == null)
            {
                return LocalizationManager.GetText("battle.log_config_missing");
            }

            var playerTeam = BuildPlayerTeam(characterDatabase, equipmentDatabase);
            if (playerTeam.Units.Count == 0)
            {
                return LocalizationManager.GetText("battle.summary_no_units");
            }

            var clampedIndex = Clamp(selectedUnitIndex, 0, playerTeam.Units.Count - 1);
            var selectedUnit = playerTeam.Units[clampedIndex];
            var builder = new StringBuilder();
            builder.Append(GetEquipmentEditTargetLabel())
                .Append(": ")
                .Append(selectedUnit.Name)
                .Append("\n\n")
                .Append(GetEquipmentStatsLabel())
                .Append(": ")
                .Append(FormatUnitStats(selectedUnit))
                .Append("\n")
                .Append(GetEquipmentSkillsLabel())
                .Append(": ")
                .Append(FormatUnitSkills(selectedUnit))
                .Append("\n")
                .Append(LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English
                    ? "Learned Skills (included above): "
                    : "已学功法（已包含在上方）: ")
                .Append(FormatUnitSkills(selectedUnit))
                .Append("\n\n")
                .Append(FormatSingleUnitEquipmentDetail(selectedUnit, equipmentDatabase))
                .Append("\n\n")
                .Append(GetTeamTotalStatsLabel())
                .Append(": ")
                .Append(FormatTeamTotalStats(playerTeam))
                .Append("\n")
                .Append(GetBattlePreviewLabel())
                .Append(": ")
                .Append(BuildBattlePreviewSummary())
                .Append("\n\n")
                .Append(GetOwnedEquipmentLabel())
                .Append(":\n")
                .Append(FormatOwnedEquipmentList(equipmentDatabase))
                .Append("\n\n")
                .Append(LocalizationManager.GetText("battle.player_team"))
                .Append(" [")
                .Append(GetCurrentPlayerEquipmentPresetName())
                .Append(']')
                .Append('\n')
                .Append(FormatEquipmentDetailTeam(playerTeam, equipmentDatabase));
            return builder.ToString();
        }

        private static string FormatEquipmentDetailTeam(BattleTeamRuntime team, EquipmentDatabase equipmentDatabase)
        {
            if (team == null || team.Units.Count == 0)
            {
                return LocalizationManager.GetText("battle.equipment_none");
            }

            var builder = new StringBuilder();
            for (var i = 0; i < team.Units.Count; i++)
            {
                var unit = team.Units[i];
                if (i > 0)
                {
                    builder.Append("\n\n");
                }

                builder.Append(unit.Name);

                if (unit.EquippedItemIds == null || unit.EquippedItemIds.Count == 0)
                {
                    builder.Append('\n').Append(LocalizationManager.GetText("battle.equipment_none"));
                    continue;
                }

                for (var j = 0; j < unit.EquippedItemIds.Count; j++)
                {
                    var equipment = equipmentDatabase.GetById(unit.EquippedItemIds[j]);
                    if (equipment == null)
                    {
                        continue;
                    }

                    builder.Append('\n')
                        .Append("- ")
                        .Append(GetEquipmentSlotLabel(equipment.Slot))
                        .Append(": ")
                        .Append(equipment.Name)
                        .Append("  ")
                        .Append(FormatEquipmentBonus(equipment));
                }
            }

            return builder.ToString();
        }

        private static string FormatSingleUnitEquipmentDetail(BattleUnitRuntime unit, EquipmentDatabase equipmentDatabase)
        {
            if (unit == null)
            {
                return LocalizationManager.GetText("battle.equipment_none");
            }

            var builder = new StringBuilder();
            if (unit.EquippedItemIds == null || unit.EquippedItemIds.Count == 0)
            {
                return LocalizationManager.GetText("battle.equipment_none");
            }

            for (var i = 0; i < unit.EquippedItemIds.Count; i++)
            {
                var equipment = equipmentDatabase.GetById(unit.EquippedItemIds[i]);
                if (equipment == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append("- ")
                    .Append(GetEquipmentSlotLabel(equipment.Slot))
                    .Append(": ")
                    .Append(equipment.Name)
                    .Append("  ")
                    .Append(FormatEquipmentBonus(equipment));
            }

            return builder.Length > 0 ? builder.ToString() : LocalizationManager.GetText("battle.equipment_none");
        }

        private static string FormatUnitStats(BattleUnitRuntime unit)
        {
            if (unit == null)
            {
                return LocalizationManager.GetText("battle.summary_no_data");
            }

            return "HP " + unit.MaxHP + "  ATK " + unit.ATK + "  DEF " + unit.DEF + "  MP " + unit.MaxMP;
        }

        private static string FormatUnitSkills(BattleUnitRuntime unit)
        {
            if (unit == null || unit.SkillIds == null || unit.SkillIds.Count == 0)
            {
                return LocalizationManager.GetText("battle.summary_no_data");
            }

            var skillDatabase = SkillDatabaseLoader.Load();
            if (skillDatabase == null)
            {
                return LocalizationManager.GetText("battle.summary_no_data");
            }

            var builder = new StringBuilder();
            for (var i = 0; i < unit.SkillIds.Count; i++)
            {
                var skill = skillDatabase.GetById(unit.SkillIds[i]);
                if (skill == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(" / ");
                }

                builder.Append(skill.Name)
                    .Append(" Lv.")
                    .Append(unit.GetSkillLevel(skill.Id));
            }

            return builder.Length > 0 ? builder.ToString() : LocalizationManager.GetText("battle.summary_no_data");
        }

        private static string FormatTeamTotalStats(BattleTeamRuntime team)
        {
            if (team == null || team.Units.Count == 0)
            {
                return LocalizationManager.GetText("battle.summary_no_data");
            }

            var totalHp = 0;
            var totalAtk = 0;
            var totalDef = 0;
            var totalMp = 0;
            for (var i = 0; i < team.Units.Count; i++)
            {
                var unit = team.Units[i];
                totalHp += unit.MaxHP;
                totalAtk += unit.ATK;
                totalDef += unit.DEF;
                totalMp += unit.MaxMP;
            }

            return "HP " + totalHp + "  ATK " + totalAtk + "  DEF " + totalDef + "  MP " + totalMp;
        }

        private static string BuildBattlePreviewSummary()
        {
            var playback = RunSampleBattlePlayback();
            var resultText = playback.IsVictory ? GetVictoryLabel() : GetDefeatLabel();
            return resultText + " / " + GetRoundCountLabel() + " " + playback.TotalRounds;
        }

        private static string FormatEquipmentBonus(EquipmentConfig equipment)
        {
            var builder = new StringBuilder();
            AppendBonus(builder, "HP", equipment.HP);
            AppendBonus(builder, "ATK", equipment.ATK);
            AppendBonus(builder, "DEF", equipment.DEF);
            AppendBonus(builder, "MP", equipment.MP);
            return builder.ToString().Trim();
        }

        private static string FormatOwnedEquipmentList(EquipmentDatabase equipmentDatabase)
        {
            if (equipmentDatabase == null)
            {
                return LocalizationManager.GetText("battle.summary_no_data");
            }

            var ownedIds = GameProgressManager.GetOwnedEquipmentIds();
            if (ownedIds.Count == 0)
            {
                return LocalizationManager.GetText("battle.equipment_none");
            }

            var builder = new StringBuilder();
            for (var i = 0; i < ownedIds.Count; i++)
            {
                var equipment = equipmentDatabase.GetById(ownedIds[i]);
                if (equipment == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append("- ")
                    .Append(GetEquipmentSlotLabel(equipment.Slot))
                    .Append(": ")
                    .Append(equipment.Name)
                    .Append("  ")
                    .Append(FormatEquipmentBonus(equipment));
            }

            return builder.Length > 0 ? builder.ToString() : LocalizationManager.GetText("battle.equipment_none");
        }

        private static void AppendBonus(StringBuilder builder, string statName, int value)
        {
            if (value == 0)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(statName)
                .Append('+')
                .Append(value);
        }

        private static BattleTeamRuntime BuildPlayerTeam(CharacterDatabase database, EquipmentDatabase equipmentDatabase)
        {
            var team = new BattleTeamRuntime();
            var stage = Mathf.Max(1, GameProgressManager.GetCurrentStage());
            for (var i = 0; i < DefaultPlayerIds.Length; i++)
            {
                var config = database.GetById(DefaultPlayerIds[i]);
                if (config != null)
                {
                    var unit = BattleUnitRuntime.FromCharacter(config);
                    ApplyRunLearnedSkills(unit);
                    ApplyDefaultEquipment(unit, equipmentDatabase);
                    ApplyCultivationScaling(unit);
                    team.Units.Add(unit);
                }
            }

            return team;
        }

        private static CharacterConfig GetPlayerCharacterConfig(int unitIndex)
        {
            var database = CharacterDatabaseLoader.Load();
            if (database == null || unitIndex < 0 || unitIndex >= DefaultPlayerIds.Length)
            {
                return null;
            }

            return database.GetById(DefaultPlayerIds[unitIndex]);
        }

        private static BattleTeamRuntime BuildEnemyTeam(EnemyDatabase database, EquipmentDatabase equipmentDatabase)
        {
            var team = new BattleTeamRuntime();
            var stage = Mathf.Max(1, GameProgressManager.GetCurrentStage());
            var encounter = GetCurrentEnemyEncounter();
            var enemyIds = GetConfiguredEnemyIds(encounter);
            for (var i = 0; i < enemyIds.Length; i++)
            {
                var config = database.GetById(enemyIds[i]);
                if (config != null)
                {
                    var unit = BattleUnitRuntime.FromEnemy(config);
                    if (encounter != null && !string.IsNullOrEmpty(encounter.OverrideElement))
                    {
                        unit.BattleElement = NormalizeBattleElement(encounter.OverrideElement);
                    }

                    ApplyEnemyEquipmentByStage(unit, equipmentDatabase, stage);
                    ApplyStageScaling(unit, stage);
                    team.Units.Add(unit);
                }
            }

            return team;
        }

        private static string[] GetConfiguredEnemyIds()
        {
            return GetConfiguredEnemyIds(GetCurrentEnemyEncounter());
        }

        private static string[] GetConfiguredEnemyIds(EnemyEncounterConfig encounter)
        {
            if (encounter == null || string.IsNullOrWhiteSpace(encounter.EnemyIds))
            {
                return DefaultEnemyIds;
            }

            var tokens = encounter.EnemyIds.Split('|');
            var ids = new List<string>();
            for (var i = 0; i < tokens.Length; i++)
            {
                var enemyId = tokens[i].Trim();
                if (!string.IsNullOrEmpty(enemyId))
                {
                    ids.Add(enemyId);
                }
            }

            return ids.Count > 0 ? ids.ToArray() : DefaultEnemyIds;
        }

        private static EnemyEncounterConfig GetCurrentEnemyEncounter()
        {
            var stage = Mathf.Max(1, GameProgressManager.GetCurrentStage());
            var stageNodeDatabase = StageNodeDatabaseLoader.Load();
            var encounterDatabase = EnemyEncounterDatabaseLoader.Load();
            if (stageNodeDatabase == null || encounterDatabase == null || encounterDatabase.Encounters == null)
            {
                return null;
            }

            var stageNode = stageNodeDatabase.GetByStage(stage);
            var nodeType = stageNode != null ? stageNode.NodeType : "Battle";
            var matches = new List<EnemyEncounterConfig>();
            var totalWeight = 0;
            for (var i = 0; i < encounterDatabase.Encounters.Count; i++)
            {
                var encounter = encounterDatabase.Encounters[i];
                if (encounter == null
                    || stage < encounter.StageFrom
                    || stage > encounter.StageTo
                    || !string.Equals(encounter.NodeType, nodeType, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                matches.Add(encounter);
                totalWeight += Mathf.Max(1, encounter.Weight);
            }

            if (matches.Count <= 0)
            {
                return null;
            }

            if (matches.Count == 1)
            {
                return matches[0];
            }

            var roll = UnityEngine.Random.Range(0, Mathf.Max(1, totalWeight));
            var cursor = 0;
            for (var i = 0; i < matches.Count; i++)
            {
                cursor += Mathf.Max(1, matches[i].Weight);
                if (roll < cursor)
                {
                    return matches[i];
                }
            }

            return matches[0];
        }

        private static void ApplyDefaultEquipment(
            BattleUnitRuntime unit,
            EquipmentDatabase equipmentDatabase)
        {
            if (unit == null || equipmentDatabase == null)
            {
                return;
            }

            for (var i = 0; i < EquipmentSlots.Length; i++)
            {
                var instanceId = GetEquippedPlayerEquipmentInstanceId(unit.Id, EquipmentSlots[i], equipmentDatabase);
                var equipment = GetEquipmentConfigByInstanceId(equipmentDatabase, instanceId);
                if (equipment != null)
                {
                    unit.ApplyEquipment(equipment);
                }
            }
        }

        private static void ApplyRunLearnedSkills(BattleUnitRuntime unit)
        {
            if (unit == null || string.IsNullOrEmpty(unit.Id))
            {
                return;
            }

            for (var i = 0; i < unit.SkillIds.Count; i++)
            {
                var existingSkillId = unit.SkillIds[i];
                unit.SetSkillLevel(existingSkillId, GameProgressManager.GetSkillLevel(unit.Id, existingSkillId));
            }

            var learnedSkills = GameProgressManager.GetLearnedSkillIds(unit.Id);
            for (var i = 0; i < learnedSkills.Count; i++)
            {
                var skillId = learnedSkills[i];
                if (string.IsNullOrEmpty(skillId))
                {
                    continue;
                }

                unit.SetSkillLevel(skillId, GameProgressManager.GetSkillLevel(unit.Id, skillId));
            }
        }

        private static void ApplyCultivationScaling(BattleUnitRuntime unit)
        {
            if (unit == null)
            {
                return;
            }

            var level = Mathf.Max(1, GameProgressManager.GetCultivationLevel());
            var bonusLevels = Mathf.Max(0, level - 1);
            if (bonusLevels <= 0)
            {
                return;
            }

            unit.MaxHP += bonusLevels * 12;
            unit.CurrentHP += bonusLevels * 12;
            unit.ATK += bonusLevels * 2;
            unit.DEF += bonusLevels;
            unit.MaxMP += bonusLevels * 3;
            unit.CurrentMP += bonusLevels * 3;
        }

        private static void ApplyStageScaling(BattleUnitRuntime unit, int stage)
        {
            if (unit == null || stage <= 1)
            {
                return;
            }

            var config = GetStageBalance(stage);
            if (config != null)
            {
                ApplyStatBonus(unit, config.EnemyHPBonus, config.EnemyATKBonus, config.EnemyDEFBonus, config.EnemyMPBonus);
                return;
            }

            var extraStages = stage - 3;
            ApplyStatBonus(unit, 16 + extraStages * 10, 4 + extraStages * 2, 1 + extraStages, 2 + extraStages * 2);
        }
        private static void ApplyEnemyEquipmentByStage(BattleUnitRuntime unit, EquipmentDatabase equipmentDatabase, int stage)
        {
            if (unit == null || stage <= 1 || equipmentDatabase == null || DefaultEnemyEquipmentIds == null)
            {
                return;
            }

            string[] equipmentIds;
            if (!DefaultEnemyEquipmentIds.TryGetValue(unit.Id, out equipmentIds) || equipmentIds == null || equipmentIds.Length == 0)
            {
                return;
            }

            var config = GetStageBalance(stage);
            var equipmentCount = config != null
                ? Mathf.Max(0, config.EnemyEquipmentCount)
                : (stage <= 3 ? 1 : equipmentIds.Length);
            if (equipmentCount <= 0)
            {
                return;
            }

            var appliedCount = 0;
            for (var i = 0; i < equipmentIds.Length; i++)
            {
                var equipment = equipmentDatabase.GetById(equipmentIds[i]);
                if (equipment == null)
                {
                    continue;
                }

                unit.ApplyEquipment(equipment);
                appliedCount++;
                if (appliedCount >= equipmentCount)
                {
                    break;
                }
            }
        }
        private static StageBalanceConfig GetStageBalance(int stage)
        {
            var database = StageBalanceDatabaseLoader.Load();
            return database != null ? database.GetByStage(stage) : null;
        }

        private static void ApplyStatBonus(BattleUnitRuntime unit, int hpBonus, int atkBonus, int defBonus, int mpBonus)
        {
            if (unit == null)
            {
                return;
            }

            unit.MaxHP += hpBonus;
            unit.CurrentHP += hpBonus;
            unit.ATK += atkBonus;
            unit.DEF += defBonus;
            unit.MaxMP += mpBonus;
            unit.CurrentMP += mpBonus;
        }
        private static string GetEquippedPlayerEquipmentInstanceId(string unitId, string slot, EquipmentDatabase equipmentDatabase)
        {
            EnsureEquipmentSettingsLoaded();
            if (string.IsNullOrEmpty(unitId) || string.IsNullOrEmpty(slot) || equipmentDatabase == null)
            {
                return null;
            }

            Dictionary<string, string> slotOverrides;
            if (PlayerEquipmentOverrides.TryGetValue(unitId, out slotOverrides))
            {
                string overrideInstanceId;
                if (slotOverrides.TryGetValue(slot, out overrideInstanceId)
                    && !string.IsNullOrEmpty(overrideInstanceId)
                    && GameProgressManager.OwnsEquipmentInstance(overrideInstanceId))
                {
                    var overrideEquipment = GetEquipmentConfigByInstanceId(equipmentDatabase, overrideInstanceId);
                    if (overrideEquipment != null && string.Equals(overrideEquipment.Slot, slot, StringComparison.OrdinalIgnoreCase))
                    {
                        return overrideInstanceId;
                    }
                }
            }

            var presetConfigId = GetPresetEquipmentConfigId(unitId, slot, equipmentDatabase);
            if (string.IsNullOrEmpty(presetConfigId))
            {
                return null;
            }

            return GameProgressManager.GetFirstOwnedEquipmentInstanceIdByEquipmentId(presetConfigId);
        }

        private static string GetPresetEquipmentConfigId(string unitId, string slot, EquipmentDatabase equipmentDatabase)
        {
            var basePreset = playerEquipmentPresetIndex == 0 ? PlayerEquipmentPresetA : PlayerEquipmentPresetB;
            string[] presetIds;
            if (!basePreset.TryGetValue(unitId, out presetIds) || presetIds == null)
            {
                return null;
            }

            for (var i = 0; i < presetIds.Length; i++)
            {
                var equipment = equipmentDatabase.GetById(presetIds[i]);
                if (equipment != null && string.Equals(equipment.Slot, slot, StringComparison.OrdinalIgnoreCase))
                {
                    return equipment.Id;
                }
            }

            return null;
        }

        private static void SetPlayerEquipmentOverride(string unitId, string slot, string equipmentId)
        {
            Dictionary<string, string> slotOverrides;
            if (!PlayerEquipmentOverrides.TryGetValue(unitId, out slotOverrides))
            {
                slotOverrides = new Dictionary<string, string>();
                PlayerEquipmentOverrides[unitId] = slotOverrides;
            }

            slotOverrides[slot] = equipmentId;
        }

        private static void AutoEquipPlayerUnitByFocus(int unitIndex, bool offenseFocus)
        {
            EnsureEquipmentSettingsLoaded();

            var config = GetPlayerCharacterConfig(unitIndex);
            var equipmentDatabase = EquipmentDatabaseLoader.Load();
            if (config == null || equipmentDatabase == null)
            {
                return;
            }

            for (var i = 0; i < EquipmentSlots.Length; i++)
            {
                var slot = EquipmentSlots[i];
                var bestEquipment = GetBestEquipmentForSlot(equipmentDatabase, slot, offenseFocus);
                if (bestEquipment == null)
                {
                    continue;
                }

                SetPlayerEquipmentOverride(config.Id, slot, bestEquipment.InstanceId);
            }

            SaveEquipmentSettings();
        }

        private static EquipmentInstanceData GetBestEquipmentForSlot(EquipmentDatabase equipmentDatabase, string slot, bool offenseFocus)
        {
            if (equipmentDatabase == null || string.IsNullOrEmpty(slot))
            {
                return null;
            }

            var candidates = GetOwnedEquipmentInstanceCandidates(equipmentDatabase, slot);
            EquipmentInstanceData bestEquipment = null;
            var bestScore = int.MinValue;

            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate == null)
                {
                    continue;
                }

                var score = CalculateEquipmentScore(equipmentDatabase.GetById(candidate.EquipmentId), offenseFocus);
                if (bestEquipment == null || score > bestScore)
                {
                    bestEquipment = candidate;
                    bestScore = score;
                }
            }

            return bestEquipment;
        }

        private static List<EquipmentInstanceData> GetOwnedEquipmentInstanceCandidates(EquipmentDatabase equipmentDatabase, string slot)
        {
            var results = new List<EquipmentInstanceData>();
            var ownedEntries = GameProgressManager.GetOwnedEquipmentInstances();
            for (var i = 0; i < ownedEntries.Count; i++)
            {
                var entry = ownedEntries[i];
                if (entry == null)
                {
                    continue;
                }

                var equipment = equipmentDatabase.GetById(entry.EquipmentId);
                if (equipment != null && equipment.Slot == slot)
                {
                    results.Add(entry);
                }
            }

            return results;
        }

        private static int CalculateEquipmentScore(EquipmentConfig equipment, bool offenseFocus)
        {
            if (equipment == null)
            {
                return int.MinValue;
            }

            if (offenseFocus)
            {
                return equipment.ATK * 100 + equipment.MP * 10 + equipment.HP + equipment.DEF * 5;
            }

            return equipment.HP * 4 + equipment.DEF * 50 + equipment.MP * 8 + equipment.ATK * 5;
        }

        private static void EnsureEquipmentSettingsLoaded()
        {
            if (equipmentSettingsLoaded)
            {
                return;
            }

            equipmentSettingsLoaded = true;
            playerEquipmentPresetIndex = Clamp(PlayerPrefs.GetInt(EquipmentPresetPrefKey, 0), 0, 1);
            PlayerEquipmentOverrides.Clear();

            var raw = PlayerPrefs.GetString(EquipmentOverridePrefKey, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            var entries = raw.Split(';');
            for (var i = 0; i < entries.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(entries[i]))
                {
                    continue;
                }

                var parts = entries[i].Split('|');
                if (parts.Length != 3)
                {
                    continue;
                }

                var overrideValue = parts[2];
                if (!GameProgressManager.OwnsEquipmentInstance(overrideValue))
                {
                    overrideValue = GameProgressManager.GetFirstOwnedEquipmentInstanceIdByEquipmentId(parts[2]);
                }

                if (!string.IsNullOrEmpty(overrideValue))
                {
                    SetPlayerEquipmentOverride(parts[0], parts[1], overrideValue);
                }
            }
        }

        private static EquipmentConfig GetEquipmentConfigByInstanceId(EquipmentDatabase equipmentDatabase, string equipmentInstanceId)
        {
            if (equipmentDatabase == null || string.IsNullOrWhiteSpace(equipmentInstanceId))
            {
                return null;
            }

            var instance = GameProgressManager.GetOwnedEquipmentInstance(equipmentInstanceId);
            return instance != null ? equipmentDatabase.GetById(instance.EquipmentId) : null;
        }

        private static void SaveEquipmentSettings()
        {
            PlayerPrefs.SetInt(EquipmentPresetPrefKey, playerEquipmentPresetIndex);
            PlayerPrefs.SetString(EquipmentOverridePrefKey, SerializeEquipmentOverrides());
            PlayerPrefs.Save();
        }

        private static string SerializeEquipmentOverrides()
        {
            var builder = new StringBuilder();
            foreach (var unitPair in PlayerEquipmentOverrides)
            {
                foreach (var slotPair in unitPair.Value)
                {
                    if (string.IsNullOrEmpty(slotPair.Value))
                    {
                        continue;
                    }

                    if (builder.Length > 0)
                    {
                        builder.Append(';');
                    }

                    builder.Append(unitPair.Key)
                        .Append('|')
                        .Append(slotPair.Key)
                        .Append('|')
                        .Append(slotPair.Value);
                }
            }

            return builder.ToString();
        }

        private static string GetEquipmentSlotLabel(string slot)
        {
            switch (slot)
            {
                case "Weapon":
                    return LocalizationManager.GetText("battle.slot_weapon");
                case "Armor":
                    return LocalizationManager.GetText("battle.slot_armor");
                case "Accessory":
                    return LocalizationManager.GetText("battle.slot_accessory");
                default:
                    return slot;
            }
        }

        private static string GetEquipmentEditTargetLabel()
        {
            return LocalizationManager.GetText("battle.editor_target");
        }

        private static string GetEquipmentStatsLabel()
        {
            return LocalizationManager.GetText("battle.editor_stats");
        }

        private static string GetEquipmentSkillsLabel()
        {
            return LocalizationManager.GetText("battle.editor_skills");
        }

        private static string GetTeamTotalStatsLabel()
        {
            return LocalizationManager.GetText("battle.editor_team_totals");
        }

        private static string GetBattlePreviewLabel()
        {
            return LocalizationManager.GetText("battle.editor_preview");
        }

        private static string GetOwnedEquipmentLabel()
        {
            return LocalizationManager.GetText("battle.editor_owned_equipment");
        }

        private static string GetStageThreatLabel(int stage, bool english)
        {
            if (stage <= 1)
            {
                return LocalizationManager.GetText("battle.threat_low");
            }

            if (stage == 2)
            {
                return LocalizationManager.GetText("battle.threat_medium");
            }

            if (stage == 3)
            {
                return LocalizationManager.GetText("battle.threat_medium_high");
            }

            return LocalizationManager.GetText("battle.threat_high");
        }

        private static string GetVictoryLabel()
        {
            return LocalizationManager.GetText("battle.preview_victory");
        }

        private static string GetDefeatLabel()
        {
            return LocalizationManager.GetText("battle.preview_defeat");
        }

        private static string GetRoundCountLabel()
        {
            return LocalizationManager.GetText("battle.preview_rounds");
        }

        private static GameLanguage GetCurrentLanguageSafe()
        {
            return LocalizationManager.Instance != null
                ? LocalizationManager.Instance.CurrentLanguage
                : GameLanguage.ChineseSimplified;
        }

        private static BattlePlaybackResult SimulatePlayback(BattleTeamRuntime playerTeam, BattleTeamRuntime enemyTeam, SkillDatabase skillDatabase)
        {
            var result = new BattlePlaybackResult();
            const int maxRounds = 20;

            for (var round = 1; round <= maxRounds; round++)
            {
                result.TotalRounds = round;
                AddEvent(
                    result.Events,
                    BattleEventType.RoundStart,
                    LocalizationManager.GetText("battle.log_round") + " " + round,
                    playerTeam,
                    enemyTeam);

                ResolveTeamTurn(playerTeam, enemyTeam, playerTeam, enemyTeam, skillDatabase, result.Events);
                if (enemyTeam.IsAllDead)
                {
                    result.IsVictory = true;
                    result.WinnerSide = "Player";
                    result.FinalPlayerTeamSummary = FormatTeam(playerTeam);
                    result.FinalEnemyTeamSummary = FormatTeam(enemyTeam);
                    AddBattleEndEvents(result, playerTeam, enemyTeam, LocalizationManager.GetText("battle.log_enemy_defeated"));
                    return result;
                }

                ResolveTeamTurn(enemyTeam, playerTeam, playerTeam, enemyTeam, skillDatabase, result.Events);
                if (playerTeam.IsAllDead)
                {
                    result.IsVictory = false;
                    result.WinnerSide = "Enemy";
                    result.FinalPlayerTeamSummary = FormatTeam(playerTeam);
                    result.FinalEnemyTeamSummary = FormatTeam(enemyTeam);
                    AddBattleEndEvents(result, playerTeam, enemyTeam, LocalizationManager.GetText("battle.log_player_defeated"));
                    return result;
                }
            }

            result.IsVictory = !playerTeam.IsAllDead;
            result.WinnerSide = result.IsVictory ? "Player" : "Enemy";
            result.FinalPlayerTeamSummary = FormatTeam(playerTeam);
            result.FinalEnemyTeamSummary = FormatTeam(enemyTeam);
            AddBattleEndEvents(result, playerTeam, enemyTeam, LocalizationManager.GetText("battle.log_round_limit"));
            return result;
        }

        private static void ResolveTeamTurn(
            BattleTeamRuntime actingTeam,
            BattleTeamRuntime targetTeam,
            BattleTeamRuntime playerTeam,
            BattleTeamRuntime enemyTeam,
            SkillDatabase skillDatabase,
            List<BattleEvent> events)
        {
            for (var i = 0; i < actingTeam.Units.Count; i++)
            {
                var actor = actingTeam.Units[i];
                if (actor.IsDead)
                {
                    continue;
                }

                ResolvePeriodicStatusEffects(actor, playerTeam, enemyTeam, events);
                if (actor.IsDead)
                {
                    continue;
                }

                var target = targetTeam.GetFirstAlive();
                if (target == null)
                {
                    return;
                }

                if (actor.ConsumeControlTurn())
                {
                    AddEvent(events, BattleEventType.Action, actor.Name + " 本回合被控制，无法行动。", playerTeam, enemyTeam);
                    actor.TickDurationStatusEffects();
                    actor.CleanupExpiredStatusEffects();
                    continue;
                }

                var skill = GetFirstCastableSkill(actor, skillDatabase);
                if (skill != null)
                {
                    ResolveSkill(actor, actingTeam, targetTeam, playerTeam, enemyTeam, target, skill, events);
                }
                else
                {
                    ResolveBasicAttack(actor, target, playerTeam, enemyTeam, events);
                }

                actor.TickDurationStatusEffects();
                actor.CleanupExpiredStatusEffects();
            }
        }

        private static void ResolvePeriodicStatusEffects(
            BattleUnitRuntime actor,
            BattleTeamRuntime playerTeam,
            BattleTeamRuntime enemyTeam,
            List<BattleEvent> events)
        {
            if (actor == null || actor.StatusEffects == null || actor.StatusEffects.Count == 0)
            {
                return;
            }

            for (var i = actor.StatusEffects.Count - 1; i >= 0; i--)
            {
                var effect = actor.StatusEffects[i];
                if (effect == null || effect.RemainingRounds <= 0)
                {
                    continue;
                }

                if (string.Equals(effect.EffectType, "Dot", StringComparison.OrdinalIgnoreCase))
                {
                    var damage = CalculateFinalDamage(null, actor, effect.GetTotalValue());
                    ApplyDamage(actor, damage);
                    AddEvent(events, BattleEventType.Action, actor.Name + " 受到持续伤害 " + damage + "。", playerTeam, enemyTeam);
                    AddDeathEventIfNeeded(events, actor, playerTeam, enemyTeam);
                }
                else if (string.Equals(effect.EffectType, "Hot", StringComparison.OrdinalIgnoreCase))
                {
                    var heal = Mathf.Max(1, effect.GetTotalValue());
                    actor.CurrentHP = Clamp(actor.CurrentHP + heal, 0, actor.MaxHP);
                    AddEvent(events, BattleEventType.Action, actor.Name + " 恢复生命 " + heal + "。", playerTeam, enemyTeam);
                }
            }

            actor.TickPeriodicStatusEffects("TurnStart");
            actor.CleanupExpiredStatusEffects();
        }
        private static SkillConfig GetFirstCastableSkill(BattleUnitRuntime actor, SkillDatabase skillDatabase)
        {
            for (var i = 0; i < actor.SkillIds.Count; i++)
            {
                var skill = skillDatabase.GetById(actor.SkillIds[i]);
                if (skill != null && actor.CurrentMP >= skill.MPCost && !IsPassiveSkill(skill))
                {
                    return skill;
                }
            }

            return null;
        }

        private static bool IsPassiveSkill(SkillConfig skill)
        {
            if (skill == null)
            {
                return false;
            }

            return string.Equals(skill.Category, "被动", StringComparison.OrdinalIgnoreCase)
                || string.Equals(skill.Category, "Passive", StringComparison.OrdinalIgnoreCase)
                || GetSkillEffects(skill).Exists(effect => string.Equals(effect.EffectType, "DotBoost", StringComparison.OrdinalIgnoreCase));
        }

        private static void ResolveSkill(
            BattleUnitRuntime actor,
            BattleTeamRuntime actingTeam,
            BattleTeamRuntime targetTeam,
            BattleTeamRuntime playerTeam,
            BattleTeamRuntime enemyTeam,
            BattleUnitRuntime defaultTarget,
            SkillConfig skill,
            List<BattleEvent> events)
        {
            actor.CurrentMP = Clamp(actor.CurrentMP - skill.MPCost, 0, actor.MaxMP);
            AddEvent(
                events,
                BattleEventType.Action,
                actor.Name + " " + LocalizationManager.GetText("battle.log_casts") + " " + skill.Name + ".",
                playerTeam,
                enemyTeam);

            var effects = GetSkillEffects(skill);
            for (var i = 0; i < effects.Count; i++)
            {
                ApplyConfiguredEffect(actor, actingTeam, targetTeam, playerTeam, enemyTeam, defaultTarget, skill, effects[i], events);
            }
        }

        private static void ResolveBasicAttack(
            BattleUnitRuntime actor,
            BattleUnitRuntime target,
            BattleTeamRuntime playerTeam,
            BattleTeamRuntime enemyTeam,
            List<BattleEvent> events)
        {
            var damage = CalculateBasicAttackDamage(actor, target);
            ApplyDamage(target, damage);
            actor.CurrentMP = Clamp(actor.CurrentMP + 5, 0, actor.MaxMP);
            AddEvent(
                events,
                BattleEventType.Action,
                actor.Name + " " + LocalizationManager.GetText("battle.log_attacks") + " " + target.Name + " " +
                LocalizationManager.GetText("battle.log_for") + " " + damage + " " +
                LocalizationManager.GetText("battle.log_damage") + ".",
                playerTeam,
                enemyTeam);
            AddDeathEventIfNeeded(events, target, playerTeam, enemyTeam);
        }

        private static int CalculateBasicAttackDamage(BattleUnitRuntime actor, BattleUnitRuntime target)
        {
            var rawDamage = GetEffectiveAttack(actor) - GetEffectiveDefense(target);
            return CalculateFinalDamage(actor, target, rawDamage);
        }

        private static int CalculateEffectDamage(BattleUnitRuntime actor, BattleUnitRuntime target, SkillConfig skill, SkillEffectConfig effect)
        {
            var rawDamage = GetEffectiveAttack(actor) + GetScaledEffectValue(actor, skill, effect) - GetEffectiveDefense(target);
            return CalculateFinalDamage(actor, target, rawDamage);
        }

        private static int CalculateFinalDamage(BattleUnitRuntime actor, BattleUnitRuntime target, int rawDamage)
        {
            var scaledDamage = ScaleDamage(rawDamage);
            var vulnerableBonus = target != null ? Mathf.Max(0, target.GetStatusTotalValue("Vulnerable")) : 0;
            var formula = GetBattleFormula();
            var vulnerableMultiplier = 1f + vulnerableBonus * formula.VulnerablePerPoint;
            var elementMultiplier = GetElementMultiplier(actor, target);
            return Mathf.Max(Mathf.RoundToInt(formula.MinDamage), Mathf.RoundToInt(scaledDamage * vulnerableMultiplier * elementMultiplier));
        }

        private static int GetScaledEffectValue(BattleUnitRuntime actor, SkillConfig skill, SkillEffectConfig effect)
        {
            if (skill == null || effect == null)
            {
                return 0;
            }

            var level = actor != null ? actor.GetSkillLevel(skill.Id) : 1;
            return Mathf.Max(0, effect.Value + Mathf.Max(0, level - 1) * effect.ValuePerLevel);
        }

        private static int GetEffectiveAttack(BattleUnitRuntime unit)
        {
            if (unit == null)
            {
                return 0;
            }

            return Mathf.Max(1, unit.ATK + unit.GetStatusTotalValue("AttackUp"));
        }

        private static int GetEffectiveDefense(BattleUnitRuntime unit)
        {
            if (unit == null)
            {
                return 0;
            }

            var defense = unit.DEF + unit.GetStatusTotalValue("DefenseUp") - unit.GetStatusTotalValue("ArmorBreak");
            var formula = GetBattleFormula();
            return Mathf.Max(0, Mathf.RoundToInt(defense * formula.DefenseMitigationFactor));
        }
        private static List<SkillEffectConfig> GetSkillEffects(SkillConfig skill)
        {
            if (skill == null)
            {
                return new List<SkillEffectConfig>();
            }

            if (skill.Effects != null && skill.Effects.Count > 0)
            {
                return skill.Effects;
            }

            return BuildLegacySkillEffects(skill);
        }

        private static List<SkillEffectConfig> BuildLegacySkillEffects(SkillConfig skill)
        {
            var results = new List<SkillEffectConfig>();
            if (skill == null || string.IsNullOrEmpty(skill.EffectType))
            {
                return results;
            }

            results.Add(new SkillEffectConfig
            {
                EffectIndex = 1,
                EffectType = skill.EffectType,
                TargetScope = string.IsNullOrEmpty(skill.TargetType) ? "Default" : skill.TargetType,
                Value = skill.Power,
                ValuePerLevel = Mathf.Max(1, Mathf.RoundToInt(skill.Power * 0.35f)),
                DurationRounds = skill.Duration,
                MaxStacks = 1,
                StackRule = "Refresh",
                TriggerTiming = ResolveLegacyTriggerTiming(skill.EffectType)
            });
            return results;
        }

        private static string ResolveLegacyTriggerTiming(string effectType)
        {
            if (string.Equals(effectType, "Dot", StringComparison.OrdinalIgnoreCase)
                || string.Equals(effectType, "Hot", StringComparison.OrdinalIgnoreCase))
            {
                return "TurnStart";
            }

            if (string.Equals(effectType, "DotBoost", StringComparison.OrdinalIgnoreCase))
            {
                return "Passive";
            }

            if (string.Equals(effectType, "Control", StringComparison.OrdinalIgnoreCase))
            {
                return "Control";
            }

            if (string.Equals(effectType, "ArmorBreak", StringComparison.OrdinalIgnoreCase)
                || string.Equals(effectType, "Vulnerable", StringComparison.OrdinalIgnoreCase)
                || string.Equals(effectType, "AttackUp", StringComparison.OrdinalIgnoreCase)
                || string.Equals(effectType, "DefenseUp", StringComparison.OrdinalIgnoreCase))
            {
                return "Duration";
            }

            return "Instant";
        }

        private static void ApplyConfiguredEffect(
            BattleUnitRuntime actor,
            BattleTeamRuntime actingTeam,
            BattleTeamRuntime targetTeam,
            BattleTeamRuntime playerTeam,
            BattleTeamRuntime enemyTeam,
            BattleUnitRuntime defaultTarget,
            SkillConfig skill,
            SkillEffectConfig effect,
            List<BattleEvent> events)
        {
            if (effect == null)
            {
                return;
            }

            var targets = ResolveTargets(actor, actingTeam, targetTeam, defaultTarget, effect);
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null || target.IsDead)
                {
                    continue;
                }

                ApplyConfiguredEffectToTarget(actor, playerTeam, enemyTeam, skill, effect, target, events);
            }
        }

        private static void ApplyConfiguredEffectToTarget(
            BattleUnitRuntime actor,
            BattleTeamRuntime playerTeam,
            BattleTeamRuntime enemyTeam,
            SkillConfig skill,
            SkillEffectConfig effect,
            BattleUnitRuntime target,
            List<BattleEvent> events)
        {
            var effectType = effect.EffectType ?? string.Empty;
            var scaledValue = GetScaledEffectValue(actor, skill, effect);

            if (string.Equals(effectType, "Damage", StringComparison.OrdinalIgnoreCase))
            {
                var damage = CalculateEffectDamage(actor, target, skill, effect);
                ApplyDamage(target, damage);
                AddEvent(events, BattleEventType.Action, target.Name + " " + LocalizationManager.GetText("battle.log_takes") + " " + damage + " " + LocalizationManager.GetText("battle.log_damage") + ".", playerTeam, enemyTeam);
                AddDeathEventIfNeeded(events, target, playerTeam, enemyTeam);
                return;
            }

            if (string.Equals(effectType, "Heal", StringComparison.OrdinalIgnoreCase))
            {
                var healValue = Mathf.Max(1, scaledValue);
                target.CurrentHP = Clamp(target.CurrentHP + healValue, 0, target.MaxHP);
                AddEvent(events, BattleEventType.Action, LocalizationManager.GetText("battle.log_heals") + " " + target.Name + " " + LocalizationManager.GetText("battle.log_for") + " " + healValue + ".", playerTeam, enemyTeam);
                return;
            }

            if (string.Equals(effectType, "Shield", StringComparison.OrdinalIgnoreCase))
            {
                var shieldGain = Mathf.Max(1, scaledValue);
                target.ApplyShield(shieldGain);
                AddEvent(events, BattleEventType.Action, target.Name + " " + LocalizationManager.GetText("battle.log_steadies") + " " + LocalizationManager.GetText("battle.log_for") + " " + shieldGain + ".", playerTeam, enemyTeam);
                return;
            }

            if (string.Equals(effectType, "ManaGain", StringComparison.OrdinalIgnoreCase))
            {
                var gain = Mathf.Max(1, scaledValue);
                target.CurrentMP = Clamp(target.CurrentMP + gain, 0, target.MaxMP);
                AddEvent(events, BattleEventType.Action, target.Name + " MP +" + gain + ".", playerTeam, enemyTeam);
                return;
            }

            if (string.Equals(effectType, "Dot", StringComparison.OrdinalIgnoreCase))
            {
                scaledValue = ApplyDotPassiveBonus(actor, scaledValue);
            }

            target.AddStatusEffect(new BattleStatusEffectRuntime
            {
                EffectType = effectType,
                Value = Mathf.Max(1, scaledValue),
                RemainingRounds = Mathf.Max(1, effect.DurationRounds),
                MaxStacks = Mathf.Max(1, effect.MaxStacks),
                StackCount = 1,
                StackRule = string.IsNullOrEmpty(effect.StackRule) ? "Refresh" : effect.StackRule,
                TriggerTiming = string.IsNullOrEmpty(effect.TriggerTiming) ? ResolveLegacyTriggerTiming(effectType) : effect.TriggerTiming,
                SourceSkillId = skill != null ? skill.Id : string.Empty
            });

            AddEvent(events, BattleEventType.Action, BuildStatusLog(target, effectType, Mathf.Max(1, scaledValue), Mathf.Max(1, effect.DurationRounds)), playerTeam, enemyTeam);
        }

        private static int ApplyDotPassiveBonus(BattleUnitRuntime actor, int value)
        {
            if (actor == null)
            {
                return Mathf.Max(1, value);
            }

            var skillDatabase = SkillDatabaseLoader.Load();
            if (skillDatabase == null)
            {
                return Mathf.Max(1, value);
            }

            var bonus = 0;
            for (var i = 0; i < actor.SkillIds.Count; i++)
            {
                var skill = skillDatabase.GetById(actor.SkillIds[i]);
                if (!IsPassiveSkill(skill))
                {
                    continue;
                }

                var passiveEffects = GetSkillEffects(skill);
                for (var j = 0; j < passiveEffects.Count; j++)
                {
                    var effect = passiveEffects[j];
                    if (effect != null && string.Equals(effect.EffectType, "DotBoost", StringComparison.OrdinalIgnoreCase))
                    {
                        bonus += GetScaledEffectValue(actor, skill, effect);
                    }
                }
            }

            return Mathf.Max(1, Mathf.RoundToInt(value * (1f + bonus / 100f)));
        }

        private static string BuildStatusLog(BattleUnitRuntime target, string effectType, int value, int duration)
        {
            if (string.Equals(effectType, "Dot", StringComparison.OrdinalIgnoreCase))
            {
                return target.Name + " 陷入持续伤害，持续 " + duration + " 回合。";
            }

            if (string.Equals(effectType, "Hot", StringComparison.OrdinalIgnoreCase))
            {
                return target.Name + " 获得持续恢复，持续 " + duration + " 回合。";
            }

            if (string.Equals(effectType, "Control", StringComparison.OrdinalIgnoreCase))
            {
                return target.Name + " 被控制，接下来将失去行动。";
            }

            if (string.Equals(effectType, "ArmorBreak", StringComparison.OrdinalIgnoreCase))
            {
                return target.Name + " 防御下降 " + value + "，持续 " + duration + " 回合。";
            }

            return target.Name + " 获得状态效果。";
        }

        private static List<BattleUnitRuntime> ResolveTargets(
            BattleUnitRuntime actor,
            BattleTeamRuntime actingTeam,
            BattleTeamRuntime targetTeam,
            BattleUnitRuntime defaultTarget,
            SkillEffectConfig effect)
        {
            var results = new List<BattleUnitRuntime>();
            var scope = effect != null && !string.IsNullOrEmpty(effect.TargetScope) ? effect.TargetScope : "Default";

            switch (scope)
            {
                case "Self":
                    results.Add(actor);
                    break;
                case "SingleAlly":
                    results.Add(actingTeam.GetFirstAlive());
                    break;
                case "AllAllies":
                    results.AddRange(GetAliveUnits(actingTeam));
                    break;
                case "AllEnemies":
                    results.AddRange(GetAliveUnits(targetTeam));
                    break;
                default:
                    results.Add(defaultTarget ?? targetTeam.GetFirstAlive());
                    break;
            }

            return results;
        }

        private static List<BattleUnitRuntime> GetAliveUnits(BattleTeamRuntime team)
        {
            var results = new List<BattleUnitRuntime>();
            if (team == null || team.Units == null)
            {
                return results;
            }

            for (var i = 0; i < team.Units.Count; i++)
            {
                if (team.Units[i] != null && !team.Units[i].IsDead)
                {
                    results.Add(team.Units[i]);
                }
            }

            return results;
        }

        private static BattleFormulaConfig GetBattleFormula()
        {
            var database = BattleFormulaDatabaseLoader.Load();
            if (database != null && database.Formula != null)
            {
                return database.Formula;
            }

            return new BattleFormulaConfig
            {
                DamageMultiplier = 1f,
                FlatDamageBonus = 0,
                VulnerablePerPoint = 0.01f,
                DefenseMitigationFactor = 1f,
                MinDamage = 1f
            };
        }

        private static float GetElementMultiplier(BattleUnitRuntime actor, BattleUnitRuntime target)
        {
            var database = ElementRelationDatabaseLoader.Load();
            if (database == null)
            {
                return 1f;
            }

            var attackerElement = NormalizeBattleElement(actor != null ? actor.BattleElement : string.Empty);
            var defenderElement = NormalizeBattleElement(target != null ? target.BattleElement : string.Empty);
            return database.GetMultiplier(attackerElement, defenderElement);
        }

        private static string NormalizeBattleElement(string element)
        {
            if (string.IsNullOrWhiteSpace(element))
            {
                return "Earth";
            }

            switch (element.Trim().ToLowerInvariant())
            {
                case "金":
                case "metal":
                    return "Metal";
                case "木":
                case "wood":
                    return "Wood";
                case "水":
                case "water":
                    return "Water";
                case "火":
                case "fire":
                    return "Fire";
                case "圣":
                case "holy":
                    return "Holy";
                default:
                    return "Earth";
            }
        }
        private static void ApplyDamage(BattleUnitRuntime target, int damage)
        {
            if (target == null)
            {
                return;
            }

            var remainingDamage = target.ConsumeShield(Mathf.Max(0, damage));
            target.CurrentHP = Clamp(target.CurrentHP - remainingDamage, 0, target.MaxHP);
        }
        private static int ScaleDamage(int rawDamage)
        {
            var formula = GetBattleFormula();
            var scaledDamage = Mathf.RoundToInt(rawDamage * formula.DamageMultiplier) + formula.FlatDamageBonus;
            return Clamp(scaledDamage, Mathf.RoundToInt(formula.MinDamage), 9999);
        }

        private static void AddBattleEndEvents(
            BattlePlaybackResult result,
            BattleTeamRuntime playerTeam,
            BattleTeamRuntime enemyTeam,
            string summaryLog)
        {
            AddEvent(result.Events, BattleEventType.BattleEnd, summaryLog, playerTeam, enemyTeam, true, result.IsVictory);
            AddEvent(
                result.Events,
                BattleEventType.BattleEnd,
                LocalizationManager.GetText("battle.log_finished"),
                playerTeam,
                enemyTeam,
                true,
                result.IsVictory);
        }

        private static void AddDeathEventIfNeeded(
            List<BattleEvent> events,
            BattleUnitRuntime target,
            BattleTeamRuntime playerTeam,
            BattleTeamRuntime enemyTeam)
        {
            if (target == null || !target.IsDead)
            {
                return;
            }

            AddEvent(
                events,
                BattleEventType.Action,
                target.Name + " " + LocalizationManager.GetText("battle.log_fallen"),
                playerTeam,
                enemyTeam);
        }

        private static void AddEvent(
            List<BattleEvent> events,
            BattleEventType type,
            string log,
            BattleTeamRuntime playerTeam,
            BattleTeamRuntime enemyTeam,
            bool isBattleFinished = false,
            bool? isVictory = null)
        {
            events.Add(new BattleEvent
            {
                Type = type,
                Log = log,
                PlayerTeamSummary = FormatTeam(playerTeam),
                EnemyTeamSummary = FormatTeam(enemyTeam),
                PlayerEquipmentSummary = FormatEquipmentTeam(playerTeam),
                EnemyEquipmentSummary = FormatEquipmentTeam(enemyTeam),
                IsBattleFinished = isBattleFinished,
                IsVictory = isVictory
            });
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}
























