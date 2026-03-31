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
        private static readonly string[] DefaultPlayerIds = { "C001", "C002" };
        private static readonly string[] DefaultEnemyIds = { "E001", "E004" };
        private static readonly Dictionary<string, string[]> PlayerEquipmentPresetA = new Dictionary<string, string[]>
        {
            { "C001", new[] { "EQ001", "EQ003", "EQ006" } },
            { "C002", new[] { "EQ002", "EQ004", "EQ005" } }
        };
        private static readonly Dictionary<string, string[]> PlayerEquipmentPresetB = new Dictionary<string, string[]>
        {
            { "C001", new[] { "EQ002", "EQ004", "EQ005" } },
            { "C002", new[] { "EQ001", "EQ003", "EQ006" } }
        };
        private static readonly Dictionary<string, string[]> DefaultEnemyEquipmentIds = new Dictionary<string, string[]>
        {
            { "E001", new[] { "EQ001", "EQ003" } },
            { "E004", new[] { "EQ002", "EQ004" } }
        };
        private static readonly string[] EquipmentSlots = { "Weapon", "Armor", "Accessory" };
        private const string EquipmentPresetPrefKey = "battle.equipment.preset";
        private const string EquipmentOverridePrefKey = "battle.equipment.overrides";
        private const float TestDamageMultiplier = 2.4f;
        private const int TestFlatDamageBonus = 8;
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

            string[] equipmentIds;
            if (!GetCurrentPlayerEquipmentPreset().TryGetValue(config.Id, out equipmentIds))
            {
                return LocalizationManager.GetText("battle.equipment_none");
            }

            var equipmentId = GetOwnedEquipmentIdBySlot(equipmentIds, slot, equipmentDatabase);
            if (string.IsNullOrEmpty(equipmentId))
            {
                return LocalizationManager.GetText("battle.equipment_none");
            }

            var equipment = equipmentDatabase.GetById(equipmentId);
            return equipment != null ? equipment.Name : LocalizationManager.GetText("battle.equipment_none");
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

            var candidates = GetOwnedEquipmentCandidates(equipmentDatabase, slot);
            if (candidates.Count == 0)
            {
                return;
            }

            var currentLoadout = GetCurrentPlayerEquipmentPreset();
            string[] equipmentIds;
            currentLoadout.TryGetValue(unitId, out equipmentIds);

            var currentEquipmentId = GetOwnedEquipmentIdBySlot(equipmentIds, slot, equipmentDatabase);
            var currentIndex = -1;
            for (var i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] != null && candidates[i].Id == currentEquipmentId)
                {
                    currentIndex = i;
                    break;
                }
            }

            var nextEquipment = candidates[(currentIndex + 1 + candidates.Count) % candidates.Count];
            SetPlayerEquipmentOverride(unitId, slot, nextEquipment != null ? nextEquipment.Id : string.Empty);
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

        public static void EquipOwnedItemForUnitIndex(int unitIndex, string equipmentId)
        {
            EnsureEquipmentSettingsLoaded();

            var config = GetPlayerCharacterConfig(unitIndex);
            if (config == null || string.IsNullOrEmpty(equipmentId) || !GameProgressManager.OwnsEquipment(equipmentId))
            {
                return;
            }

            var equipmentDatabase = EquipmentDatabaseLoader.Load();
            if (equipmentDatabase == null)
            {
                return;
            }

            var equipment = equipmentDatabase.GetById(equipmentId);
            if (equipment == null || string.IsNullOrEmpty(equipment.Slot))
            {
                return;
            }

            SetPlayerEquipmentOverride(config.Id, equipment.Slot, equipment.Id);
            SaveEquipmentSettings();
        }

        public static List<EquipmentConfig> GetOwnedEquipmentsForSlot(string slot)
        {
            var equipmentDatabase = EquipmentDatabaseLoader.Load();
            if (equipmentDatabase == null || string.IsNullOrEmpty(slot))
            {
                return new List<EquipmentConfig>();
            }

            return GetOwnedEquipmentCandidates(equipmentDatabase, slot);
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

            var builder = new StringBuilder();
            for (var i = 0; i < DefaultEnemyIds.Length; i++)
            {
                var config = database.GetById(DefaultEnemyIds[i]);
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

                builder.Append(skill.Name);
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
            var loadout = GetCurrentPlayerEquipmentPreset();
            var stage = Mathf.Max(1, GameProgressManager.GetCurrentStage());
            for (var i = 0; i < DefaultPlayerIds.Length; i++)
            {
                var config = database.GetById(DefaultPlayerIds[i]);
                if (config != null)
                {
                    var unit = BattleUnitRuntime.FromCharacter(config);
                    ApplyRunLearnedSkills(unit);
                    ApplyDefaultEquipment(unit, equipmentDatabase, loadout);
                    ApplyCultivationScaling(unit);
                    ApplyEarlyStagePlayerSupport(unit, stage);
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
            for (var i = 0; i < DefaultEnemyIds.Length; i++)
            {
                var config = database.GetById(DefaultEnemyIds[i]);
                if (config != null)
                {
                    var unit = BattleUnitRuntime.FromEnemy(config);
                    ApplyEnemyEquipmentByStage(unit, equipmentDatabase, stage);
                    ApplyStageScaling(unit, stage);
                    team.Units.Add(unit);
                }
            }

            return team;
        }

        private static void ApplyDefaultEquipment(
            BattleUnitRuntime unit,
            EquipmentDatabase equipmentDatabase,
            Dictionary<string, string[]> loadoutMap)
        {
            if (unit == null || equipmentDatabase == null || loadoutMap == null)
            {
                return;
            }

            string[] equipmentIds;
            if (!loadoutMap.TryGetValue(unit.Id, out equipmentIds) || equipmentIds == null)
            {
                return;
            }

            for (var i = 0; i < equipmentIds.Length; i++)
            {
                var equipment = equipmentDatabase.GetById(equipmentIds[i]);
                if (equipment != null && GameProgressManager.OwnsEquipment(equipment.Id))
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

            var learnedSkills = GameProgressManager.GetLearnedSkillIds(unit.Id);
            for (var i = 0; i < learnedSkills.Count; i++)
            {
                var skillId = learnedSkills[i];
                if (string.IsNullOrEmpty(skillId) || unit.SkillIds.Contains(skillId))
                {
                    continue;
                }

                unit.SkillIds.Add(skillId);
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

            if (stage == 2)
            {
                unit.MaxHP += 8;
                unit.CurrentHP += 8;
                unit.ATK += 2;
                unit.DEF += 1;
                unit.MaxMP += 1;
                unit.CurrentMP += 1;
                return;
            }

            if (stage == 3)
            {
                unit.MaxHP += 16;
                unit.CurrentHP += 16;
                unit.ATK += 4;
                unit.DEF += 1;
                unit.MaxMP += 2;
                unit.CurrentMP += 2;
                return;
            }

            var extraStages = stage - 3;
            unit.MaxHP += 16 + extraStages * 10;
            unit.CurrentHP += 16 + extraStages * 10;
            unit.ATK += 4 + extraStages * 2;
            unit.DEF += 1 + extraStages * 1;
            unit.MaxMP += 2 + extraStages * 2;
            unit.CurrentMP += 2 + extraStages * 2;
        }

        private static void ApplyEnemyEquipmentByStage(BattleUnitRuntime unit, EquipmentDatabase equipmentDatabase, int stage)
        {
            if (unit == null)
            {
                return;
            }

            if (stage <= 1)
            {
                return;
            }

            if (stage == 2 && equipmentDatabase != null && DefaultEnemyEquipmentIds != null)
            {
                string[] equipmentIds;
                if (!DefaultEnemyEquipmentIds.TryGetValue(unit.Id, out equipmentIds) || equipmentIds == null || equipmentIds.Length == 0)
                {
                    return;
                }

                var firstEquipment = equipmentDatabase.GetById(equipmentIds[0]);
                if (firstEquipment != null)
                {
                    unit.ApplyEquipment(firstEquipment);
                }

                return;
            }

            if (stage == 3 && equipmentDatabase != null && DefaultEnemyEquipmentIds != null)
            {
                string[] equipmentIds;
                if (!DefaultEnemyEquipmentIds.TryGetValue(unit.Id, out equipmentIds) || equipmentIds == null || equipmentIds.Length == 0)
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
                    if (appliedCount >= 2)
                    {
                        break;
                    }
                }

                return;
            }

            ApplyDefaultEquipment(unit, equipmentDatabase, DefaultEnemyEquipmentIds);
        }

        private static void ApplyEarlyStagePlayerSupport(BattleUnitRuntime unit, int stage)
        {
            if (unit == null)
            {
                return;
            }

            if (stage == 1)
            {
                unit.MaxHP += 20;
                unit.CurrentHP += 20;
                unit.ATK += 3;
                unit.DEF += 2;
                return;
            }

            if (stage == 2)
            {
                unit.MaxHP += 10;
                unit.CurrentHP += 10;
                unit.ATK += 2;
                unit.DEF += 1;
                return;
            }

            if (stage == 3)
            {
                unit.MaxHP += 6;
                unit.CurrentHP += 6;
                unit.ATK += 1;
            }
        }

        private static Dictionary<string, string[]> GetCurrentPlayerEquipmentPreset()
        {
            EnsureEquipmentSettingsLoaded();
            var basePreset = playerEquipmentPresetIndex == 0 ? PlayerEquipmentPresetA : PlayerEquipmentPresetB;
            var mergedPreset = new Dictionary<string, string[]>();

            foreach (var pair in basePreset)
            {
                var values = new string[pair.Value.Length];
                for (var i = 0; i < pair.Value.Length; i++)
                {
                    values[i] = pair.Value[i];
                }

                mergedPreset[pair.Key] = values;
            }

            foreach (var overridePair in PlayerEquipmentOverrides)
            {
                string[] equipmentIds;
                if (!mergedPreset.TryGetValue(overridePair.Key, out equipmentIds))
                {
                    equipmentIds = new string[EquipmentSlots.Length];
                    mergedPreset[overridePair.Key] = equipmentIds;
                }

                for (var i = 0; i < EquipmentSlots.Length; i++)
                {
                    string overrideId;
                    if (!overridePair.Value.TryGetValue(EquipmentSlots[i], out overrideId)
                        || string.IsNullOrEmpty(overrideId)
                        || !GameProgressManager.OwnsEquipment(overrideId))
                    {
                        continue;
                    }

                    equipmentIds[i] = overrideId;
                }
            }

            return mergedPreset;
        }

        private static string GetOwnedEquipmentIdBySlot(string[] equipmentIds, string slot, EquipmentDatabase equipmentDatabase)
        {
            if (equipmentIds == null || equipmentDatabase == null)
            {
                return null;
            }

            for (var i = 0; i < equipmentIds.Length; i++)
            {
                var equipment = equipmentDatabase.GetById(equipmentIds[i]);
                if (equipment != null
                    && equipment.Slot == slot
                    && GameProgressManager.OwnsEquipment(equipment.Id))
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

                SetPlayerEquipmentOverride(config.Id, slot, bestEquipment.Id);
            }

            SaveEquipmentSettings();
        }

        private static EquipmentConfig GetBestEquipmentForSlot(EquipmentDatabase equipmentDatabase, string slot, bool offenseFocus)
        {
            if (equipmentDatabase == null || string.IsNullOrEmpty(slot))
            {
                return null;
            }

            var candidates = GetOwnedEquipmentCandidates(equipmentDatabase, slot);
            EquipmentConfig bestEquipment = null;
            var bestScore = int.MinValue;

            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate == null)
                {
                    continue;
                }

                var score = CalculateEquipmentScore(candidate, offenseFocus);
                if (bestEquipment == null || score > bestScore)
                {
                    bestEquipment = candidate;
                    bestScore = score;
                }
            }

            return bestEquipment;
        }

        private static List<EquipmentConfig> GetOwnedEquipmentCandidates(EquipmentDatabase equipmentDatabase, string slot)
        {
            var results = new List<EquipmentConfig>();
            var ownedIds = GameProgressManager.GetOwnedEquipmentIds();
            for (var i = 0; i < ownedIds.Count; i++)
            {
                var equipment = equipmentDatabase.GetById(ownedIds[i]);
                if (equipment != null && equipment.Slot == slot)
                {
                    results.Add(equipment);
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

                SetPlayerEquipmentOverride(parts[0], parts[1], parts[2]);
            }
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

                var target = targetTeam.GetFirstAlive();
                if (target == null)
                {
                    return;
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
            }
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

            return skill.Category == "Passive"
                || skill.EffectType == "DotBoost";
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
            actor.CurrentMP -= skill.MPCost;

            if (skill.EffectType == "Heal")
            {
                var ally = actingTeam.GetFirstAlive();
                if (ally == null)
                {
                    return;
                }

                var healValue = skill.Power;
                ally.CurrentHP = Clamp(ally.CurrentHP + healValue, 0, ally.MaxHP);
                AddEvent(
                    events,
                    BattleEventType.Action,
                    actor.Name + " " + LocalizationManager.GetText("battle.log_casts") + " " + skill.Name + ", " +
                    LocalizationManager.GetText("battle.log_heals") + " " + ally.Name + " " +
                    LocalizationManager.GetText("battle.log_for") + " " + healValue + ".",
                    playerTeam,
                    enemyTeam);
                return;
            }

            if (skill.TargetType == "AllEnemies")
            {
                AddEvent(
                    events,
                    BattleEventType.Action,
                    actor.Name + " " + LocalizationManager.GetText("battle.log_casts") + " " + skill.Name + ".",
                    playerTeam,
                    enemyTeam);

                for (var i = 0; i < targetTeam.Units.Count; i++)
                {
                    var target = targetTeam.Units[i];
                    if (target.IsDead)
                    {
                        continue;
                    }

                    var damage = CalculateSkillDamage(actor, target, skill);
                    target.CurrentHP = Clamp(target.CurrentHP - damage, 0, target.MaxHP);
                    AddEvent(
                        events,
                        BattleEventType.Action,
                        target.Name + " " + LocalizationManager.GetText("battle.log_takes") + " " + damage + " " +
                        LocalizationManager.GetText("battle.log_damage") + ".",
                        playerTeam,
                        enemyTeam);

                    AddDeathEventIfNeeded(events, target, playerTeam, enemyTeam);
                }

                return;
            }

            if (skill.EffectType == "Shield")
            {
                var shieldGain = skill.Power;
                actor.CurrentHP = Clamp(actor.CurrentHP + shieldGain, 0, actor.MaxHP);
                AddEvent(
                    events,
                    BattleEventType.Action,
                    actor.Name + " " + LocalizationManager.GetText("battle.log_casts") + " " + skill.Name + ", " +
                    LocalizationManager.GetText("battle.log_steadies") + " " +
                    LocalizationManager.GetText("battle.log_for") + " " + shieldGain + ".",
                    playerTeam,
                    enemyTeam);
                return;
            }

            var singleDamage = CalculateSkillDamage(actor, defaultTarget, skill);
            defaultTarget.CurrentHP = Clamp(defaultTarget.CurrentHP - singleDamage, 0, defaultTarget.MaxHP);
            AddEvent(
                events,
                BattleEventType.Action,
                actor.Name + " " + LocalizationManager.GetText("battle.log_casts") + " " + skill.Name + " " +
                LocalizationManager.GetText("battle.log_on") + " " + defaultTarget.Name + " " +
                LocalizationManager.GetText("battle.log_for") + " " + singleDamage + " " +
                LocalizationManager.GetText("battle.log_damage") + ".",
                playerTeam,
                enemyTeam);
            AddDeathEventIfNeeded(events, defaultTarget, playerTeam, enemyTeam);
        }

        private static void ResolveBasicAttack(
            BattleUnitRuntime actor,
            BattleUnitRuntime target,
            BattleTeamRuntime playerTeam,
            BattleTeamRuntime enemyTeam,
            List<BattleEvent> events)
        {
            var damage = ScaleDamage(actor.ATK - target.DEF);
            target.CurrentHP = Clamp(target.CurrentHP - damage, 0, target.MaxHP);
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

        private static int CalculateSkillDamage(BattleUnitRuntime actor, BattleUnitRuntime target, SkillConfig skill)
        {
            var rawDamage = actor.ATK + skill.Power - target.DEF;
            return ScaleDamage(rawDamage);
        }

        private static int ScaleDamage(int rawDamage)
        {
            var scaledDamage = (int)(rawDamage * TestDamageMultiplier) + TestFlatDamageBonus;
            return Clamp(scaledDamage, 1, 9999);
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






