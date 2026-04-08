using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Wuxing.Config;
using Wuxing.Localization;
using Wuxing.UI;

namespace Wuxing.Game
{
    public class GameProgressManager : MonoBehaviour
    {
        private const string CurrentStagePrefKey = "game.progress.current_stage";
        private const string HighestClearedStagePrefKey = "game.progress.highest_cleared_stage";
        private const string LastBattleStagePrefKey = "game.progress.last_battle_stage";
        private const string LastBattleRoundsPrefKey = "game.progress.last_battle_rounds";
        private const string LastBattleVictoryPrefKey = "game.progress.last_battle_victory";
        private const string HasLastBattlePrefKey = "game.progress.has_last_battle";
        private const string ElapsedMonthsPrefKey = "game.progress.elapsed_months";
        private const string CultivationLevelPrefKey = "game.progress.cultivation_level";
        private const string CultivationExpPrefKey = "game.progress.cultivation_exp";
        private const string SpiritStonesPrefKey = "game.progress.spirit_stones";
        private const string MetalSpiritStonesPrefKey = "game.progress.spirit_stones_metal";
        private const string WoodSpiritStonesPrefKey = "game.progress.spirit_stones_wood";
        private const string WaterSpiritStonesPrefKey = "game.progress.spirit_stones_water";
        private const string FireSpiritStonesPrefKey = "game.progress.spirit_stones_fire";
        private const string EarthSpiritStonesPrefKey = "game.progress.spirit_stones_earth";
        private const string OwnedEquipmentPrefKey = "game.progress.owned_equipment";
        private const string LearnedSkillsPrefKey = "game.progress.learned_skills";
        private const string PendingSkillRewardsPrefKey = "game.progress.pending_skill_rewards";
        private const string FixedStageEventsPrefKey = "game.progress.fixed_stage_events";
        private const string RandomStageEventStatesPrefKey = "game.progress.random_stage_events";
        private const string ObjectiveCountersPrefKey = "game.progress.objective_counters";
        private const string ActiveEffectsPrefKey = "game.progress.active_effects";
        private const string RunDataSnapshotPrefKey = "game.progress.run_data_snapshot";
        private const string EquipmentInstanceCounterPrefKey = "game.progress.equipment_instance_counter";
        private const int DefaultMaxStage = 100;
        private const int LifetimeMonths = 360;

        private static readonly string[] BaseOwnedEquipmentIds =
        {
            "EQ001", "EQ002", "EQ003", "EQ004", "EQ005", "EQ006"
        };

        private static readonly string[] BasePlayerCharacterIds =
        {
            "C001"
        };

        public static GameProgressManager Instance { get; private set; }

        public static event Action ProgressChanged;

        public int CurrentStage { get; private set; }
        public int HighestClearedStage { get; private set; }
        public bool HasLastBattle { get; private set; }
        public int LastBattleStage { get; private set; }
        public int LastBattleRounds { get; private set; }
        public bool LastBattleVictory { get; private set; }
        public int ElapsedMonths { get; private set; }
        public int CultivationLevel { get; private set; }
        public int CultivationExp { get; private set; }
        public int SpiritStones { get; private set; }
        public int MetalSpiritStones { get; private set; }
        public int WoodSpiritStones { get; private set; }
        public int WaterSpiritStones { get; private set; }
        public int FireSpiritStones { get; private set; }
        public int EarthSpiritStones { get; private set; }

        private readonly List<EquipmentInstanceData> ownedEquipments = new List<EquipmentInstanceData>();
        private readonly List<CharacterRunData> characterRunData = new List<CharacterRunData>();
        private readonly List<SkillRewardOption> pendingSkillRewards = new List<SkillRewardOption>();
        private readonly List<int> completedFixedEventStages = new List<int>();
        private readonly List<StageRandomEventState> randomStageEventStates = new List<StageRandomEventState>();
        private readonly List<ObjectiveCounterData> objectiveCounters = new List<ObjectiveCounterData>();
        private readonly List<RunEffectData> activeEffects = new List<RunEffectData>();
        private int equipmentInstanceCounter;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
        }

        public static int GetCurrentStage()
        {
            EnsureInstance();
            return Instance != null ? Instance.CurrentStage : 0;
        }

        public static int GetHighestClearedStage()
        {
            EnsureInstance();
            return Instance != null ? Instance.HighestClearedStage : 0;
        }

        public static void StartRun()
        {
            EnsureInstance();
            if (Instance == null)
            {
                return;
            }

            if (Instance.CurrentStage <= 0)
            {
                StoryManager.ClearRunTriggerStates();
                Instance.CurrentStage = 1;
                Instance.ElapsedMonths = 0;
                Instance.CultivationLevel = 1;
                Instance.CultivationExp = 0;
                Instance.ResetSpiritStones();
                Instance.ResetOwnedEquipmentToBase();
                Instance.characterRunData.Clear();
                Instance.pendingSkillRewards.Clear();
                Instance.completedFixedEventStages.Clear();
                Instance.randomStageEventStates.Clear();
                Instance.objectiveCounters.Clear();
                Instance.activeEffects.Clear();
                Instance.SaveProgress();
                ProgressChanged?.Invoke();
            }
        }

        public static RunAdvanceResult AdvanceAfterVictory()
        {
            EnsureInstance();
            if (Instance == null)
            {
                return RunAdvanceResult.ContinueMap;
            }

            MarkCurrentStageCleared();
            Instance.SaveProgress();
            ProgressChanged?.Invoke();
            return RunAdvanceResult.ContinueMap;
        }

        public static void RetryCurrentStage()
        {
            EnsureInstance();
            if (Instance == null)
            {
                return;
            }

            if (Instance.CurrentStage <= 0)
            {
                Instance.CurrentStage = 1;
                Instance.SaveProgress();
            }

            ProgressChanged?.Invoke();
        }

        public static void RetreatToPreviousStage()
        {
            EnsureInstance();
            if (Instance == null)
            {
                return;
            }

            if (Instance.CurrentStage <= 1)
            {
                Instance.CurrentStage = 1;
            }
            else
            {
                Instance.CurrentStage -= 1;
            }

            Instance.SaveProgress();
            ProgressChanged?.Invoke();
        }

        public static void ResetRun()
        {
            EnsureInstance();
            if (Instance == null)
            {
                return;
            }

            StoryManager.ClearRunTriggerStates();
            Instance.CurrentStage = 0;
            Instance.ElapsedMonths = 0;
            Instance.CultivationLevel = 1;
            Instance.CultivationExp = 0;
            Instance.ResetSpiritStones();
            Instance.ResetOwnedEquipmentToBase();
            Instance.characterRunData.Clear();
            Instance.pendingSkillRewards.Clear();
            Instance.completedFixedEventStages.Clear();
            Instance.randomStageEventStates.Clear();
            Instance.objectiveCounters.Clear();
            Instance.activeEffects.Clear();
            Instance.SaveProgress();
            ProgressChanged?.Invoke();
        }

        public static BattleRewardResult GrantBattleRewards(int stage)
        {
            EnsureInstance();
            var reward = new BattleRewardResult();
            if (Instance == null)
            {
                return reward;
            }

            var resolvedStage = Mathf.Max(1, stage);
            reward.ExpGained = Instance.ApplyExpGainModifiers(GetConfiguredBattleExpReward(resolvedStage));
            reward.SpiritStonesGained = Instance.ApplySpiritStoneGainModifiers(GetConfiguredBattleSpiritStoneReward(resolvedStage));
            reward.SpiritStoneElement = GetSpiritStoneElementByStage(resolvedStage);
            reward.SpiritStoneName = GetSpiritStoneName(reward.SpiritStoneElement, false);

            Instance.CultivationExp += reward.ExpGained;
            Instance.AddSpiritStones(reward.SpiritStoneElement, reward.SpiritStonesGained);

            while (Instance.CultivationExp >= Instance.GetRequiredExpInternal())
            {
                Instance.CultivationExp -= Instance.GetRequiredExpInternal();
                Instance.CultivationLevel += 1;
                reward.LevelsGained += 1;
            }

            var equipmentDatabase = EquipmentDatabaseLoader.Load();
            if (equipmentDatabase != null)
            {
                var drop = Instance.RollEquipmentDrop(equipmentDatabase, resolvedStage);
                if (drop != null)
                {
                    var instance = Instance.AddOwnedEquipmentInstance(drop.Id);
                    reward.DroppedEquipmentId = drop.Id;
                    reward.DroppedEquipmentInstanceId = instance != null ? instance.InstanceId : string.Empty;
                    reward.DroppedEquipmentName = drop.Name;
                    Instance.RegisterObjectiveEventInternal("ObtainEquipment", drop.Slot, 1);
                    Instance.RegisterObjectiveEventInternal("ObtainEquipment", string.Empty, 1);
                }
            }

            Instance.SaveProgress();
            ProgressChanged?.Invoke();
            return reward;
        }

        public static BattleRewardResult GrantNonBattleRewards(int stage, MapNodeType nodeType)
        {
            EnsureInstance();
            var reward = new BattleRewardResult();
            if (Instance == null)
            {
                return reward;
            }

            var resolvedStage = Mathf.Max(1, stage);
            reward.ExpGained = Instance.ApplyExpGainModifiers(GetConfiguredNonBattleExpReward(resolvedStage, nodeType));
            reward.SpiritStonesGained = Instance.ApplySpiritStoneGainModifiers(GetConfiguredNonBattleSpiritStoneReward(resolvedStage, nodeType));
            reward.SpiritStoneElement = GetSpiritStoneElementByStage(resolvedStage);
            reward.SpiritStoneName = GetSpiritStoneName(reward.SpiritStoneElement, false);

            Instance.CultivationExp += reward.ExpGained;
            Instance.AddSpiritStones(reward.SpiritStoneElement, reward.SpiritStonesGained);

            while (Instance.CultivationExp >= Instance.GetRequiredExpInternal())
            {
                Instance.CultivationExp -= Instance.GetRequiredExpInternal();
                Instance.CultivationLevel += 1;
                reward.LevelsGained += 1;
            }

            Instance.SaveProgress();
            ProgressChanged?.Invoke();
            return reward;
        }

        public static BattleRewardResult GrantProgressReward(int expGained, string spiritStoneElement, int spiritStoneCount)
        {
            EnsureInstance();
            var reward = new BattleRewardResult();
            if (Instance == null)
            {
                return reward;
            }

            reward.ExpGained = Instance.ApplyExpGainModifiers(expGained);
            reward.SpiritStonesGained = Instance.ApplySpiritStoneGainModifiers(spiritStoneCount);
            reward.SpiritStoneElement = string.IsNullOrEmpty(spiritStoneElement) ? "Earth" : spiritStoneElement;
            reward.SpiritStoneName = GetSpiritStoneName(reward.SpiritStoneElement, false);

            Instance.CultivationExp += reward.ExpGained;
            Instance.AddSpiritStones(reward.SpiritStoneElement, reward.SpiritStonesGained);

            while (Instance.CultivationExp >= Instance.GetRequiredExpInternal())
            {
                Instance.CultivationExp -= Instance.GetRequiredExpInternal();
                Instance.CultivationLevel += 1;
                reward.LevelsGained += 1;
            }

            Instance.SaveProgress();
            ProgressChanged?.Invoke();
            return reward;
        }

        public static void DebugGrantSpiritStones(string element, int amount)
        {
            EnsureInstance();
            if (Instance == null)
            {
                return;
            }

            var resolvedAmount = Mathf.Max(0, amount);
            if (resolvedAmount <= 0)
            {
                return;
            }

            Instance.AddSpiritStones(element, resolvedAmount);
            Instance.SaveProgress();
            ProgressChanged?.Invoke();
        }

        public static void DebugSetCultivationLevel(int targetLevel)
        {
            EnsureInstance();
            if (Instance == null)
            {
                return;
            }

            Instance.CultivationLevel = Mathf.Max(1, targetLevel);
            Instance.CultivationExp = 0;
            Instance.SaveProgress();
            ProgressChanged?.Invoke();
        }

        public static void DebugGrantEquipment(string equipmentId)
        {
            EnsureInstance();
            if (Instance == null || string.IsNullOrEmpty(equipmentId))
            {
                return;
            }

            var equipmentDatabase = EquipmentDatabaseLoader.Load();
            if (equipmentDatabase == null || equipmentDatabase.GetById(equipmentId) == null)
            {
                return;
            }

            Instance.AddOwnedEquipmentInstance(equipmentId);
            Instance.SaveProgress();
            ProgressChanged?.Invoke();
        }

        public static void DebugGrantSkill(string characterId, string skillId, int level)
        {
            EnsureInstance();
            if (Instance == null || string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(skillId))
            {
                return;
            }

            var characterDatabase = CharacterDatabaseLoader.Load();
            var skillDatabase = SkillDatabaseLoader.Load();
            var character = characterDatabase != null ? characterDatabase.GetById(characterId) : null;
            var skill = skillDatabase != null ? skillDatabase.GetById(skillId) : null;
            if (character == null || skill == null)
            {
                return;
            }

            var entry = Instance.GetOrCreateCharacterRunData(characterId);
            if (!CharacterHasInitialSkill(character, skillId)
                && !entry.LearnedSkillIds.Exists(id => string.Equals(id, skillId, StringComparison.OrdinalIgnoreCase)))
            {
                entry.LearnedSkillIds.Add(skillId);
            }

            Instance.SetSkillLevelInternal(entry, skillId, Mathf.Max(1, level));
            Instance.SaveProgress();
            ProgressChanged?.Invoke();
        }

        public static void DebugJumpToStage(int targetStage)
        {
            EnsureInstance();
            if (Instance == null)
            {
                return;
            }

            var clampedStage = Mathf.Clamp(targetStage, 1, GetMaxStage());
            if (Instance.CurrentStage <= 0)
            {
                Instance.CurrentStage = 1;
            }

            Instance.CurrentStage = clampedStage;
            Instance.HighestClearedStage = Mathf.Max(Instance.HighestClearedStage, Mathf.Max(0, clampedStage - 1));
            Instance.SaveProgress();
            ProgressChanged?.Invoke();
        }

        public static int GetCultivationLevel()
        {
            EnsureInstance();
            return Instance != null ? Instance.CultivationLevel : 1;
        }

        public static int GetCultivationExp()
        {
            EnsureInstance();
            return Instance != null ? Instance.CultivationExp : 0;
        }

        public static int GetSpiritStones()
        {
            EnsureInstance();
            return Instance != null ? Instance.GetTotalSpiritStones() : 0;
        }

        public static int GetSpiritStoneCount(string element)
        {
            EnsureInstance();
            return Instance != null ? Instance.GetSpiritStoneCountInternal(element) : 0;
        }

        public static bool TrySpendSpiritStones(string element, int amount)
        {
            EnsureInstance();
            if (Instance == null)
            {
                return false;
            }

            var cost = Mathf.Max(0, amount);
            if (cost <= 0)
            {
                return true;
            }

            var normalized = NormalizeSpiritStoneElement(element);
            if (Instance.GetSpiritStoneCountInternal(normalized) < cost)
            {
                return false;
            }

            switch (normalized)
            {
                case "metal":
                    Instance.MetalSpiritStones -= cost;
                    break;
                case "wood":
                    Instance.WoodSpiritStones -= cost;
                    break;
                case "water":
                    Instance.WaterSpiritStones -= cost;
                    break;
                case "fire":
                    Instance.FireSpiritStones -= cost;
                    break;
                default:
                    Instance.EarthSpiritStones -= cost;
                    break;
            }

            Instance.SyncSpiritStoneTotal();
            Instance.SaveProgress();
            ProgressChanged?.Invoke();
            return true;
        }

        public static bool TryConvertSpiritStones(string sourceElement, string targetElement, int costAmount, int gainAmount, int convertCount)
        {
            EnsureInstance();
            if (Instance == null)
            {
                return false;
            }

            var resolvedCount = Mathf.Max(0, convertCount);
            if (resolvedCount <= 0)
            {
                return false;
            }

            var normalizedSource = NormalizeSpiritStoneElement(sourceElement);
            var normalizedTarget = NormalizeSpiritStoneElement(targetElement);
            if (string.Equals(normalizedSource, normalizedTarget, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var totalCost = Mathf.Max(1, costAmount) * resolvedCount;
            var totalGain = Mathf.Max(1, gainAmount) * resolvedCount;
            if (Instance.GetSpiritStoneCountInternal(normalizedSource) < totalCost)
            {
                return false;
            }

            switch (normalizedSource)
            {
                case "metal":
                    Instance.MetalSpiritStones -= totalCost;
                    break;
                case "wood":
                    Instance.WoodSpiritStones -= totalCost;
                    break;
                case "water":
                    Instance.WaterSpiritStones -= totalCost;
                    break;
                case "fire":
                    Instance.FireSpiritStones -= totalCost;
                    break;
                default:
                    Instance.EarthSpiritStones -= totalCost;
                    break;
            }

            Instance.AddSpiritStones(normalizedTarget, totalGain);
            Instance.SaveProgress();
            ProgressChanged?.Invoke();
            return true;
        }

        public static string GetSpiritStoneColorHex(string element)
        {
            return GetSpiritStoneColorHexInternal(element);
        }

        public static string BuildSpiritStoneSummary(bool english, bool richText = false)
        {
            EnsureInstance();
            if (Instance == null)
            {
                return LocalizationManager.GetText("progress.spirit_stones_zero");
            }

            var parts = new List<string>
            {
                Instance.BuildSpiritStoneEntry("Metal", english, richText),
                Instance.BuildSpiritStoneEntry("Wood", english, richText),
                Instance.BuildSpiritStoneEntry("Water", english, richText),
                Instance.BuildSpiritStoneEntry("Fire", english, richText),
                Instance.BuildSpiritStoneEntry("Earth", english, richText)
            };

            return LocalizationManager.GetText("progress.spirit_stones_prefix") + string.Join("  ", parts.ToArray());
        }

        public static void RegisterObjectiveEvent(string eventType, string conditionParam = "", int amount = 1)
        {
            EnsureInstance();
            if (Instance == null || string.IsNullOrEmpty(eventType) || amount <= 0)
            {
                return;
            }

            Instance.RegisterObjectiveEventInternal(eventType, conditionParam, amount);
            Instance.SaveProgress();
            ProgressChanged?.Invoke();
        }

        public static string BuildObjectiveSummary(bool english)
        {
            EnsureInstance();
            return Instance != null ? Instance.BuildObjectiveSummaryInternal(english) : string.Empty;
        }

        public static string BuildSpiritStoneGainText(BattleRewardResult reward, bool english, bool richText = false)
        {
            if (reward == null || reward.SpiritStonesGained <= 0)
            {
                return LocalizationManager.GetText("progress.spirit_stones_gain_zero");
            }

            var content = GetSpiritStoneName(reward.SpiritStoneElement, english) + " +" + reward.SpiritStonesGained;
            return richText ? WrapSpiritStoneColor(reward.SpiritStoneElement, content) : content;
        }

        public static int GetRequiredExpForNextLevel()
        {
            EnsureInstance();
            return Instance != null ? Instance.GetRequiredExpInternal() : 60;
        }

        public static string GetSpiritStoneElementForStage(int stage)
        {
            return GetSpiritStoneElementByStage(stage);
        }

        public static bool OwnsEquipment(string equipmentId)
        {
            EnsureInstance();
            return Instance != null && Instance.ownedEquipments.Exists(delegate(EquipmentInstanceData entry)
            {
                return entry != null
                    && string.Equals(entry.EquipmentId, equipmentId, StringComparison.OrdinalIgnoreCase);
            });
        }

        public static List<string> GetOwnedEquipmentIds()
        {
            EnsureInstance();
            if (Instance == null)
            {
                return new List<string>();
            }

            var results = new List<string>();
            for (var i = 0; i < Instance.ownedEquipments.Count; i++)
            {
                var entry = Instance.ownedEquipments[i];
                if (entry != null && !string.IsNullOrWhiteSpace(entry.EquipmentId))
                {
                    results.Add(entry.EquipmentId);
                }
            }
            results.Sort(StringComparer.OrdinalIgnoreCase);
            return results;
        }

        public static bool OwnsEquipmentInstance(string instanceId)
        {
            EnsureInstance();
            return Instance != null && Instance.ownedEquipments.Exists(delegate(EquipmentInstanceData entry)
            {
                return entry != null
                    && string.Equals(entry.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase);
            });
        }

        public static EquipmentInstanceData GetOwnedEquipmentInstance(string instanceId)
        {
            EnsureInstance();
            if (Instance == null || string.IsNullOrWhiteSpace(instanceId))
            {
                return null;
            }

            for (var i = 0; i < Instance.ownedEquipments.Count; i++)
            {
                var entry = Instance.ownedEquipments[i];
                if (entry != null && string.Equals(entry.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase))
                {
                    return CloneEquipmentInstance(entry);
                }
            }

            return null;
        }

        public static List<EquipmentInstanceData> GetOwnedEquipmentInstances()
        {
            EnsureInstance();
            var results = new List<EquipmentInstanceData>();
            if (Instance == null)
            {
                return results;
            }

            for (var i = 0; i < Instance.ownedEquipments.Count; i++)
            {
                var entry = Instance.ownedEquipments[i];
                if (entry != null)
                {
                    results.Add(CloneEquipmentInstance(entry));
                }
            }

            return results;
        }

        public static string GetFirstOwnedEquipmentInstanceIdByEquipmentId(string equipmentId)
        {
            EnsureInstance();
            if (Instance == null || string.IsNullOrWhiteSpace(equipmentId))
            {
                return string.Empty;
            }

            for (var i = 0; i < Instance.ownedEquipments.Count; i++)
            {
                var entry = Instance.ownedEquipments[i];
                if (entry != null && string.Equals(entry.EquipmentId, equipmentId, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.InstanceId ?? string.Empty;
                }
            }

            return string.Empty;
        }

        public static List<string> GetLearnedSkillIds(string characterId)
        {
            EnsureInstance();
            if (Instance == null || string.IsNullOrEmpty(characterId))
            {
                return new List<string>();
            }

            return Instance.GetLearnedSkillIdsInternal(characterId);
        }

        public static int GetSkillLevel(string characterId, string skillId)
        {
            EnsureInstance();
            if (Instance == null || string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(skillId))
            {
                return 1;
            }

            return Instance.GetSkillLevelInternal(characterId, skillId);
        }

        public static List<string> GetEquippedActiveSkillIds(string characterId)
        {
            EnsureInstance();
            if (Instance == null || string.IsNullOrEmpty(characterId))
            {
                return new List<string>();
            }

            return Instance.GetEquippedActiveSkillIdsInternal(characterId);
        }

        public static void EquipActiveSkill(string characterId, int slotIndex, string skillId)
        {
            EnsureInstance();
            if (Instance == null || string.IsNullOrEmpty(characterId))
            {
                return;
            }

            Instance.EquipActiveSkillInternal(characterId, slotIndex, skillId);
            Instance.SaveProgress();
            ProgressChanged?.Invoke();
        }

        public static void UnequipActiveSkill(string characterId, int slotIndex)
        {
            EnsureInstance();
            if (Instance == null || string.IsNullOrEmpty(characterId))
            {
                return;
            }

            Instance.UnequipActiveSkillInternal(characterId, slotIndex);
            Instance.SaveProgress();
            ProgressChanged?.Invoke();
        }

        public static bool CanEquipActiveSkill(string characterId, string skillId)
        {
            EnsureInstance();
            return Instance != null
                && !string.IsNullOrEmpty(characterId)
                && Instance.CharacterCanEquipActiveSkill(characterId, skillId);
        }

        public static string BuildLearnedSkillsOverview(bool english)
        {
            EnsureInstance();

            var characterDatabase = CharacterDatabaseLoader.Load();
            var skillDatabase = SkillDatabaseLoader.Load();
            if (characterDatabase == null || skillDatabase == null)
            {
                return LocalizationManager.GetText("progress.skills_unavailable");
            }

            var builder = new StringBuilder();
            builder.Append(LocalizationManager.GetText("progress.run_skills_title"));

            for (var i = 0; i < BasePlayerCharacterIds.Length; i++)
            {
                var characterId = BasePlayerCharacterIds[i];
                var character = characterDatabase.GetById(characterId);
                if (character == null)
                {
                    continue;
                }

                var learnedSkillIds = GetLearnedSkillIds(characterId);
                var currentSkillNames = new List<string>();
                var learnedSkillNames = new List<string>();
                var knownSkillIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                AppendSkillNames(character.Id, character.InitialSkills, currentSkillNames, knownSkillIds, skillDatabase, english);
                for (var j = 0; j < learnedSkillIds.Count; j++)
                {
                    learnedSkillNames.Add(GetSkillLabel(skillDatabase, character.Id, learnedSkillIds[j], english));
                    if (knownSkillIds.Add(learnedSkillIds[j]))
                    {
                        currentSkillNames.Add(GetSkillLabel(skillDatabase, character.Id, learnedSkillIds[j], english));
                    }
                }

                builder.Append("\n\n")
                    .Append(LocalizationManager.GetText("progress.skills_character"))
                    .Append(character.Name)
                    .Append('\n')
                    .Append(LocalizationManager.GetText("progress.skills_current"))
                    .Append(currentSkillNames.Count > 0
                        ? "\n" + string.Join("\n\n", currentSkillNames.ToArray())
                        : LocalizationManager.GetText("progress.skills_none"))
                    .Append('\n')
                    .Append(LocalizationManager.GetText("progress.skills_new"))
                    .Append(learnedSkillNames.Count > 0
                        ? "\n" + string.Join("\n\n", learnedSkillNames.ToArray())
                        : LocalizationManager.GetText("progress.skills_none"));
            }

            return builder.ToString();
        }

        public static List<UICardData> BuildLearnedSkillCards(bool english)
        {
            EnsureInstance();

            var result = new List<UICardData>();
            var characterDatabase = CharacterDatabaseLoader.Load();
            var skillDatabase = SkillDatabaseLoader.Load();
            if (characterDatabase == null || skillDatabase == null)
            {
                return result;
            }

            for (var i = 0; i < BasePlayerCharacterIds.Length; i++)
            {
                var characterId = BasePlayerCharacterIds[i];
                var character = characterDatabase.GetById(characterId);
                if (character == null)
                {
                    continue;
                }

                var knownSkillIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                CollectKnownSkillIds(character.InitialSkills, knownSkillIds);
                var learnedSkillIds = GetLearnedSkillIds(characterId);
                for (var j = 0; j < learnedSkillIds.Count; j++)
                {
                    if (!string.IsNullOrWhiteSpace(learnedSkillIds[j]))
                    {
                        knownSkillIds.Add(learnedSkillIds[j].Trim());
                    }
                }

                foreach (var skillId in knownSkillIds)
                {
                    var skill = skillDatabase.GetById(skillId);
                    if (skill == null)
                    {
                        continue;
                    }

                    var level = GetSkillLevel(characterId, skillId);
                    var detail = new StringBuilder();
                    detail.Append(english ? "Character: " : "角色：")
                        .Append(character.Name)
                        .Append('\n')
                        .Append(english ? "Element: " : "五行：")
                        .Append(skill.Element)
                        .Append('\n')
                        .Append(english ? "Effect: " : "效果：")
                        .Append(BuildSkillEffectSummary(skill, level, english));

                    result.Add(new UICardData
                    {
                        Id = characterId + ":" + skillId,
                        Title = WrapSkillNameWithQualityColor(skill.Name, skill.Quality),
                        Subtitle = LocalizationManager.GetText("common.level_prefix") + level,
                        DetailTitle = skill.Name + "  " + LocalizationManager.GetText("common.level_prefix") + level,
                        DetailBody = detail.ToString(),
                        BorderColor = UIElementPalette.GetQualityColor(skill.Quality)
                    });
                }
            }

            return result;
        }

        public static string GetPrimaryCharacterId()
        {
            return BasePlayerCharacterIds.Length > 0 ? BasePlayerCharacterIds[0] : string.Empty;
        }

        public static List<UICardData> BuildEquippedActiveSkillCards(string characterId, bool english)
        {
            EnsureInstance();

            var result = new List<UICardData>();
            var skillDatabase = SkillDatabaseLoader.Load();
            if (skillDatabase == null || string.IsNullOrEmpty(characterId))
            {
                return result;
            }

            var equippedSkillIds = GetEquippedActiveSkillIds(characterId);
            for (var i = 0; i < 3; i++)
            {
                var equippedSkillId = i < equippedSkillIds.Count ? equippedSkillIds[i] : string.Empty;
                var skill = !string.IsNullOrEmpty(equippedSkillId) ? skillDatabase.GetById(equippedSkillId) : null;
                result.Add(new UICardData
                {
                    Id = "slot:" + i,
                    Title = skill != null ? WrapSkillNameWithQualityColor(skill.Name, skill.Quality) : (english ? "Empty Slot" : "空技能栏"),
                    Subtitle = skill != null ? LocalizationManager.GetText("common.level_prefix") + GetSkillLevel(characterId, equippedSkillId) : (english ? "Choose active skill" : "选择主动技能"),
                    DetailTitle = skill != null ? skill.Name : (english ? "Skill Slot" : "技能栏"),
                    DetailBody = skill != null ? BuildSkillDetail(characterId, skill, english) : (english ? "Select an active skill from the library below." : "从下方技能库中选择一个主动技能上场。"),
                    BorderColor = skill != null ? UIElementPalette.GetQualityColor(skill.Quality) : UIElementPalette.GetBorderColor("None")
                });
            }

            return result;
        }

        public static List<UICardData> BuildSkillLibraryCards(string characterId, bool english)
        {
            EnsureInstance();

            var result = new List<UICardData>();
            var skillDatabase = SkillDatabaseLoader.Load();
            if (skillDatabase == null || string.IsNullOrEmpty(characterId))
            {
                return result;
            }

            var entry = Instance != null ? Instance.GetOrCreateCharacterRunData(characterId) : null;
            var knownSkillIds = Instance != null ? Instance.GetKnownSkillIds(characterId, entry) : new List<string>();
            for (var i = 0; i < knownSkillIds.Count; i++)
            {
                var skillId = knownSkillIds[i];
                var skill = skillDatabase.GetById(skillId);
                if (skill == null)
                {
                    continue;
                }

                result.Add(new UICardData
                {
                    Id = skillId,
                    Title = WrapSkillNameWithQualityColor(skill.Name, skill.Quality),
                    Subtitle = LocalizationManager.GetText("common.level_prefix") + GetSkillLevel(characterId, skillId),
                    DetailTitle = skill.Name + " " + LocalizationManager.GetText("common.level_prefix") + GetSkillLevel(characterId, skillId),
                    DetailBody = BuildSkillDetail(characterId, skill, english),
                    BorderColor = UIElementPalette.GetQualityColor(skill.Quality)
                });
            }

            return result;
        }

        public static List<UICardData> BuildOwnedEquipmentCards(bool english)
        {
            EnsureInstance();

            var result = new List<UICardData>();
            var equipmentDatabase = EquipmentDatabaseLoader.Load();
            if (equipmentDatabase == null)
            {
                return result;
            }

            var ownedEntries = GetOwnedEquipmentInstances();
            for (var i = 0; i < ownedEntries.Count; i++)
            {
                var entry = ownedEntries[i];
                var equipment = entry != null ? equipmentDatabase.GetById(entry.EquipmentId) : null;
                if (equipment == null)
                {
                    continue;
                }

                var detail = new StringBuilder();
                detail.Append(english ? "Quality: " : "品级：")
                    .Append(GetEquipmentQualityLabel(equipment.Quality, english))
                    .Append('\n')
                    .Append(english ? "Level: " : "等级：")
                    .Append(GetEquipmentLevelLabel(equipment.Level, english))
                    .Append('\n')
                    .Append(english ? "Slot: " : "部位：")
                    .Append(GetEquipmentSlotLabel(equipment.Slot, english));

                var stats = BuildEquipmentStatCardLines(equipment, english);
                if (!string.IsNullOrEmpty(stats))
                {
                    detail.Append('\n').Append(stats);
                }

                if (!string.IsNullOrEmpty(equipment.Notes))
                {
                    detail.Append('\n')
                        .Append(english ? "Notes: " : "说明：")
                        .Append(equipment.Notes);
                }

                result.Add(new UICardData
                {
                    Id = entry.InstanceId,
                    Title = equipment.Name,
                    Subtitle = BuildEquipmentCardSubtitle(equipment, english),
                    DetailTitle = equipment.Name,
                    DetailBody = detail.ToString(),
                    BorderColor = UIElementPalette.GetQualityColor(equipment.Quality)
                });
            }

            return result;
        }
        public static void PrepareSkillRewardOptions()
        {
            PrepareSkillRewardOptions(GetNodeType(GetCurrentStage()));
        }

        public static void PrepareSkillRewardOptions(MapNodeType rewardNodeType)
        {
            EnsureInstance();
            if (Instance == null)
            {
                return;
            }

            Instance.pendingSkillRewards.Clear();

            var characterDatabase = CharacterDatabaseLoader.Load();
            var skillDatabase = SkillDatabaseLoader.Load();
            if (characterDatabase == null || skillDatabase == null || skillDatabase.Skills == null)
            {
                Instance.SaveProgress();
                ProgressChanged?.Invoke();
                return;
            }

            var weightedCandidates = new List<SkillRewardOption>();
            for (var i = 0; i < BasePlayerCharacterIds.Length; i++)
            {
                var characterId = BasePlayerCharacterIds[i];
                var character = characterDatabase.GetById(characterId);
                if (character == null)
                {
                    continue;
                }

                for (var j = 0; j < skillDatabase.Skills.Count; j++)
                {
                    var skill = skillDatabase.Skills[j];
                    if (skill == null || string.IsNullOrEmpty(skill.Id) || IsPassiveSkillReward(skill))
                    {
                        continue;
                    }

                    var currentLevel = Instance.GetSkillLevelInternal(character.Id, skill.Id);
                    var isUpgrade = Instance.CharacterHasSkillInternal(character, skill.Id);
                    var option = new SkillRewardOption
                    {
                        CharacterId = character.Id,
                        CharacterName = character.Name,
                        SkillId = skill.Id,
                        SkillName = skill.Name,
                        SkillElement = skill.Element,
                        SkillQuality = skill.Quality,
                        SkillDescription = skill.Description,
                        CurrentLevel = currentLevel,
                        ResultLevel = isUpgrade ? currentLevel + 1 : 1,
                        IsUpgrade = isUpgrade
                    };

                    var weight = GetSkillRewardWeight(skill, rewardNodeType, isUpgrade);
                    for (var copy = 0; copy < weight; copy++)
                    {
                        weightedCandidates.Add(option);
                    }
                }
            }

            Shuffle(weightedCandidates);
            for (var i = 0; i < weightedCandidates.Count && Instance.pendingSkillRewards.Count < 3; i++)
            {
                var candidate = weightedCandidates[i];
                if (candidate == null || Instance.pendingSkillRewards.Exists(existing =>
                    existing != null
                    && string.Equals(existing.CharacterId, candidate.CharacterId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(existing.SkillId, candidate.SkillId, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                Instance.pendingSkillRewards.Add(candidate);
            }

            Instance.SaveProgress();
            ProgressChanged?.Invoke();
        }

        public static BattleRewardResult GrantEquipmentReward(int stage, int expGained, string spiritStoneElement, int spiritStoneCount)
        {
            EnsureInstance();
            var reward = new BattleRewardResult();
            if (Instance == null)
            {
                return reward;
            }

            var resolvedStage = Mathf.Max(1, stage);
            reward.ExpGained = Instance.ApplyExpGainModifiers(expGained);
            reward.SpiritStoneElement = string.IsNullOrEmpty(spiritStoneElement)
                ? GetSpiritStoneElementByStage(resolvedStage)
                : spiritStoneElement;
            reward.SpiritStoneName = GetSpiritStoneName(reward.SpiritStoneElement, false);
            reward.SpiritStonesGained = Instance.ApplySpiritStoneGainModifiers(spiritStoneCount);

            if (reward.ExpGained > 0)
            {
                Instance.CultivationExp += reward.ExpGained;
            }

            if (reward.SpiritStonesGained > 0)
            {
                Instance.AddSpiritStones(reward.SpiritStoneElement, reward.SpiritStonesGained);
            }

            while (Instance.CultivationExp >= Instance.GetRequiredExpInternal())
            {
                Instance.CultivationExp -= Instance.GetRequiredExpInternal();
                Instance.CultivationLevel += 1;
                reward.LevelsGained += 1;
            }

            var equipmentDatabase = EquipmentDatabaseLoader.Load();
            if (equipmentDatabase != null)
            {
                var drop = Instance.RollEquipmentDrop(equipmentDatabase, resolvedStage);
                if (drop != null)
                {
                    var instance = Instance.AddOwnedEquipmentInstance(drop.Id);
                    reward.DroppedEquipmentId = drop.Id;
                    reward.DroppedEquipmentInstanceId = instance != null ? instance.InstanceId : string.Empty;
                    reward.DroppedEquipmentName = drop.Name;
                    Instance.RegisterObjectiveEventInternal("ObtainEquipment", drop.Slot, 1);
                    Instance.RegisterObjectiveEventInternal("ObtainEquipment", string.Empty, 1);
                }
            }

            Instance.SaveProgress();
            ProgressChanged?.Invoke();
            return reward;
        }

        public static List<SkillRewardOption> GetPendingSkillRewardOptions()
        {
            EnsureInstance();
            return Instance != null ? new List<SkillRewardOption>(Instance.pendingSkillRewards) : new List<SkillRewardOption>();
        }

        public static bool HasPendingSkillRewardOptions()
        {
            EnsureInstance();
            return Instance != null && Instance.pendingSkillRewards.Count > 0;
        }

        public static SkillRewardOption ApplyPendingSkillReward(int optionIndex)
        {
            EnsureInstance();
            if (Instance == null || optionIndex < 0 || optionIndex >= Instance.pendingSkillRewards.Count)
            {
                return null;
            }

            var option = Instance.pendingSkillRewards[optionIndex];
            if (option == null || string.IsNullOrEmpty(option.CharacterId) || string.IsNullOrEmpty(option.SkillId))
            {
                return null;
            }

            var entry = Instance.GetOrCreateCharacterRunData(option.CharacterId);
            var characterDatabase = CharacterDatabaseLoader.Load();
            var character = characterDatabase != null ? characterDatabase.GetById(option.CharacterId) : null;
            var alreadyKnown = Instance.CharacterHasSkillInternal(character, option.SkillId);
            var previousLevel = Instance.GetSkillLevelInternal(entry, character, option.SkillId);

            if (!alreadyKnown && !entry.LearnedSkillIds.Exists(delegate(string skillId)
            {
                return string.Equals(skillId, option.SkillId, StringComparison.OrdinalIgnoreCase);
            }))
            {
                entry.LearnedSkillIds.Add(option.SkillId);
            }

            var nextLevel = Mathf.Max(1, option.ResultLevel);
            Instance.SetSkillLevelInternal(entry, option.SkillId, nextLevel);
            Instance.TryAutoEquipActiveSkill(entry, option.SkillId);
            option.CurrentLevel = previousLevel;
            option.ResultLevel = nextLevel;
            option.IsUpgrade = alreadyKnown;
            Instance.RegisterObjectiveEventInternal("GainSkillReward", option.SkillId, 1);
            Instance.RegisterObjectiveEventInternal("GainSkillReward", string.Empty, 1);

            Instance.pendingSkillRewards.Clear();
            Instance.SaveProgress();
            ProgressChanged?.Invoke();
            return option;
        }

        public static RunData GetRunDataSnapshot()
        {
            EnsureInstance();
            return Instance != null ? Instance.BuildRunDataSnapshot() : new RunData();
        }

        public static IReadOnlyList<RunEffectData> GetActiveEffects()
        {
            EnsureInstance();
            return Instance != null ? Instance.activeEffects.AsReadOnly() : Array.Empty<RunEffectData>();
        }

        public static RunEffectData AddTimedEffect(string effectType, int value, int durationMonths, string titleKey, string descriptionKey)
        {
            EnsureInstance();
            if (Instance == null || string.IsNullOrEmpty(effectType) || value <= 0 || durationMonths <= 0)
            {
                return null;
            }

            var effect = new RunEffectData
            {
                EffectType = effectType,
                Value = Mathf.Max(1, value),
                RemainingMonths = Mathf.Max(1, durationMonths),
                TitleKey = titleKey ?? string.Empty,
                DescriptionKey = descriptionKey ?? string.Empty
            };

            Instance.activeEffects.Add(effect);
            Instance.SaveProgress();
            ProgressChanged?.Invoke();
            return CloneRunEffect(effect);
        }

        public static string BuildActiveEffectsSummary(bool english)
        {
            EnsureInstance();
            if (Instance == null || Instance.activeEffects.Count == 0)
            {
                return LocalizationManager.GetText("map.profile_active_effects") + LocalizationManager.GetText("map.profile_no_effects");
            }

            var builder = new StringBuilder();
            builder.Append(LocalizationManager.GetText("map.profile_active_effects"));
            var hasAny = false;
            for (var i = 0; i < Instance.activeEffects.Count; i++)
            {
                var effect = Instance.activeEffects[i];
                if (effect == null || effect.RemainingMonths <= 0)
                {
                    continue;
                }

                if (hasAny)
                {
                    builder.Append("\n");
                }
                else
                {
                    hasAny = true;
                }

                builder.Append("- ");
                if (!string.IsNullOrEmpty(effect.TitleKey))
                {
                    builder.Append(LocalizationManager.GetText(effect.TitleKey)).Append("：");
                }
                builder.Append(FormatEffectDescription(effect.DescriptionKey, effect.RemainingMonths, effect.Value));
            }

            if (!hasAny)
            {
                builder.Append(LocalizationManager.GetText("map.profile_no_effects"));
            }

            return builder.ToString();
        }

        public static void RecordBattleResult(bool isVictory, int stage, int rounds)
        {
            EnsureInstance();
            if (Instance == null)
            {
                return;
            }

            Instance.HasLastBattle = true;
            Instance.LastBattleVictory = isVictory;
            Instance.LastBattleStage = Mathf.Max(1, stage);
            Instance.LastBattleRounds = Mathf.Max(0, rounds);
            if (isVictory)
            {
                Instance.RegisterObjectiveEventInternal("WinBattle", string.Empty, 1);
                Instance.RegisterObjectiveEventInternal("WinNodeTypeBattle", GetNodeType(stage).ToString(), 1);
            }
            Instance.SaveProgress();
            ProgressChanged?.Invoke();
        }

        public static bool HasActiveRun()
        {
            return GetCurrentStage() > 0;
        }

        public static int GetMaxStage()
        {
            var database = StageNodeDatabaseLoader.Load();
            var configuredMaxStage = database != null ? database.GetMaxStage() : 0;
            return configuredMaxStage > 0
                ? configuredMaxStage
                : DefaultMaxStage;
        }

        public static int GetElapsedMonths()
        {
            EnsureInstance();
            return Instance != null ? Instance.ElapsedMonths : 0;
        }

        public static int GetMaxReachableStage()
        {
            EnsureInstance();
            if (Instance == null)
            {
                return 1;
            }

            var currentStage = Instance.CurrentStage > 0 ? Instance.CurrentStage : 1;
            var maxStage = GetMaxStage();
            var frontierStage = Mathf.Clamp(Instance.HighestClearedStage + 1, 1, maxStage);
            return Mathf.Clamp(Mathf.Max(currentStage, frontierStage), 1, maxStage);
        }

        public static bool CanTravelToStage(int stage)
        {
            return stage >= 1 && stage <= GetMaxReachableStage();
        }

        public static int GetRemainingMonths()
        {
            return Mathf.Max(0, LifetimeMonths - GetElapsedMonths());
        }

        public static string GetStageEventProfile(int stage)
        {
            var config = GetStageNodeConfig(stage);
            if (config != null && !string.IsNullOrEmpty(config.EventProfile))
            {
                return config.EventProfile;
            }

            return stage <= 1 ? "VillageStart" : "DefaultBattle";
        }

        public static string GetStageEventMode(int stage)
        {
            var config = GetStageNodeConfig(stage);
            if (config != null && !string.IsNullOrEmpty(config.EventMode))
            {
                var normalized = config.EventMode.Trim().ToLowerInvariant();
                switch (normalized)
                {
                    case "random":
                    case "\u968f\u673a":
                        return "Random";
                    case "fixed":
                    case "\u56fa\u5b9a":
                        return "Fixed";
                    case "none":
                    case "battle":
                    default:
                        return "None";
                }
            }

            return IsBattleNode(GetNodeType(stage)) ? "None" : "Fixed";
        }

        public static int GetStageEventCooldownMonths(int stage)
        {
            var config = GetStageNodeConfig(stage);
            return config != null ? Mathf.Max(0, config.CooldownMonths) : 0;
        }

        public static bool IsFixedStageEventConsumed(int stage)
        {
            EnsureInstance();
            return Instance != null && Instance.completedFixedEventStages.Contains(Mathf.Max(1, stage));
        }

        public static bool ConsumeFixedStageEvent(int stage)
        {
            EnsureInstance();
            if (Instance == null)
            {
                return false;
            }

            var resolvedStage = Mathf.Max(1, stage);
            if (Instance.completedFixedEventStages.Contains(resolvedStage))
            {
                return false;
            }

            Instance.completedFixedEventStages.Add(resolvedStage);
            Instance.SaveProgress();
            ProgressChanged?.Invoke();
            return true;
        }

        public static int GetRandomStageCooldownRemainingMonths(int stage)
        {
            EnsureInstance();
            if (Instance == null)
            {
                return 0;
            }

            var cooldownMonths = GetStageEventCooldownMonths(stage);
            if (cooldownMonths <= 0)
            {
                return 0;
            }

            var state = Instance.GetRandomStageEventState(stage);
            if (state == null)
            {
                return 0;
            }

            var monthsPassed = Mathf.Max(0, Instance.ElapsedMonths - state.LastTriggeredMonth);
            return Mathf.Max(0, cooldownMonths - monthsPassed);
        }

        public static bool MarkRandomStageEventTriggered(int stage)
        {
            EnsureInstance();
            if (Instance == null)
            {
                return false;
            }

            var remaining = GetRandomStageCooldownRemainingMonths(stage);
            if (remaining > 0)
            {
                return false;
            }

            var resolvedStage = Mathf.Max(1, stage);
            var state = Instance.GetRandomStageEventState(resolvedStage);
            if (state == null)
            {
                state = new StageRandomEventState
                {
                    Stage = resolvedStage,
                    LastTriggeredMonth = Instance.ElapsedMonths
                };
                Instance.randomStageEventStates.Add(state);
            }
            else
            {
                state.LastTriggeredMonth = Instance.ElapsedMonths;
            }

            Instance.SaveProgress();
            ProgressChanged?.Invoke();
            return true;
        }

        public static bool HasEnoughSpiritStones(string element, int amount)
        {
            EnsureInstance();
            if (Instance == null)
            {
                return false;
            }

            if (amount <= 0)
            {
                return true;
            }

            switch (NormalizeSpiritStoneElement(element))
            {
                case "metal":
                    return Instance.MetalSpiritStones >= amount;
                case "wood":
                    return Instance.WoodSpiritStones >= amount;
                case "water":
                    return Instance.WaterSpiritStones >= amount;
                case "fire":
                    return Instance.FireSpiritStones >= amount;
                case "earth":
                default:
                    return Instance.EarthSpiritStones >= amount;
            }
        }

        public static MapNodeType GetNodeType(int stage)
        {
            if (stage <= 0)
            {
                return MapNodeType.Battle;
            }

            var config = GetStageNodeConfig(stage);
            if (config != null)
            {
                return ParseNodeType(config.NodeType);
            }

            return MapNodeType.Battle;
        }

        private static StageNodeConfig GetStageNodeConfig(int stage)
        {
            var database = StageNodeDatabaseLoader.Load();
            return database != null ? database.GetByStage(Mathf.Max(1, stage)) : null;
        }

        private static int GetConfiguredBattleExpReward(int stage)
        {
            var config = GetStageNodeConfig(stage);
            return config != null && config.BattleExpReward > 0
                ? config.BattleExpReward
                : 18 + Mathf.Max(1, stage) * 8;
        }

        private static int GetConfiguredBattleSpiritStoneReward(int stage)
        {
            var config = GetStageNodeConfig(stage);
            return config != null && config.BattleSpiritStoneReward > 0
                ? config.BattleSpiritStoneReward
                : 12 + Mathf.Max(1, stage) * 5;
        }

        private static int GetConfiguredNonBattleExpReward(int stage, MapNodeType nodeType)
        {
            var config = GetStageNodeConfig(stage);
            if (config != null && config.NonBattleExpReward > 0)
            {
                return config.NonBattleExpReward;
            }

            var resolvedStage = Mathf.Max(1, stage);
            return nodeType == MapNodeType.Rest
                ? (resolvedStage == 1 ? 12 : 10 + resolvedStage * 4)
                : 6 + resolvedStage * 2;
        }

        private static int GetConfiguredNonBattleSpiritStoneReward(int stage, MapNodeType nodeType)
        {
            var config = GetStageNodeConfig(stage);
            if (config != null && config.NonBattleSpiritStoneReward > 0)
            {
                return config.NonBattleSpiritStoneReward;
            }

            var resolvedStage = Mathf.Max(1, stage);
            return nodeType == MapNodeType.Rest
                ? (resolvedStage == 1 ? 10 : 6 + resolvedStage * 3)
                : 4 + resolvedStage * 2;
        }

        private static MapNodeType ParseNodeType(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue))
            {
                return MapNodeType.Battle;
            }

            switch (rawValue.Trim().ToLowerInvariant())
            {
                case "elite":
                case "\u7cbe\u82f1":
                    return MapNodeType.Elite;
                case "rest":
                case "\u4f11\u6574":
                    return MapNodeType.Rest;
                case "boss":
                case "\u9996\u9886":
                    return MapNodeType.Boss;
                case "battle":
                case "\u6218\u6597":
                default:
                    return MapNodeType.Battle;
            }
        }

        public static bool IsBattleNode(MapNodeType nodeType)
        {
            return nodeType == MapNodeType.Battle
                || nodeType == MapNodeType.Elite
                || nodeType == MapNodeType.Boss;
        }

        public static RunAdvanceResult AdvanceNonBattleNode(int months)
        {
            EnsureInstance();
            if (Instance == null)
            {
                return RunAdvanceResult.ContinueMap;
            }

            if (Instance.CurrentStage <= 0)
            {
                Instance.CurrentStage = 1;
            }

            var monthCost = Mathf.Max(1, months);
            Instance.ElapsedMonths += monthCost;
            Instance.TickTimedEffects(monthCost);
            if (Instance.ElapsedMonths >= LifetimeMonths)
            {
                Instance.CurrentStage = 0;
                Instance.SaveProgress();
                ProgressChanged?.Invoke();
                return RunAdvanceResult.LifespanEnded;
            }

            if (Instance.CurrentStage >= GetMaxStage())
            {
                Instance.CurrentStage = 0;
                Instance.SaveProgress();
                ProgressChanged?.Invoke();
                return RunAdvanceResult.ChapterComplete;
            }

            Instance.CurrentStage += 1;
            Instance.SaveProgress();
            ProgressChanged?.Invoke();
            return RunAdvanceResult.ContinueMap;
        }

        public static RunAdvanceResult TravelToStage(int targetStage)
        {
            EnsureInstance();
            if (Instance == null)
            {
                return RunAdvanceResult.ContinueMap;
            }

            if (Instance.CurrentStage <= 0)
            {
                Instance.CurrentStage = 1;
            }

            var clampedStage = Mathf.Clamp(targetStage, 1, GetMaxReachableStage());
            var monthCost = Mathf.Abs(clampedStage - Instance.CurrentStage);
            Instance.ElapsedMonths += monthCost;
            Instance.TickTimedEffects(monthCost);
            Instance.CurrentStage = clampedStage;

            if (Instance.ElapsedMonths >= LifetimeMonths)
            {
                Instance.CurrentStage = 0;
                Instance.SaveProgress();
                ProgressChanged?.Invoke();
                return RunAdvanceResult.LifespanEnded;
            }

            Instance.SaveProgress();
            ProgressChanged?.Invoke();
            return RunAdvanceResult.ContinueMap;
        }

        public static void MarkCurrentStageCleared()
        {
            MarkStageCleared(GetCurrentStage());
        }

        public static void MarkStageCleared(int stage)
        {
            EnsureInstance();
            if (Instance == null)
            {
                return;
            }

            var resolvedStage = Mathf.Clamp(stage, 0, GetMaxStage());
            if (resolvedStage > Instance.HighestClearedStage)
            {
                Instance.HighestClearedStage = resolvedStage;
                Instance.SaveProgress();
                ProgressChanged?.Invoke();
            }
        }

        public static int GetLastBattleStage()
        {
            EnsureInstance();
            return Instance != null ? Instance.LastBattleStage : 0;
        }

        public static int GetLastBattleRounds()
        {
            EnsureInstance();
            return Instance != null ? Instance.LastBattleRounds : 0;
        }

        public static bool GetLastBattleVictory()
        {
            EnsureInstance();
            return Instance != null && Instance.LastBattleVictory;
        }

        public static string GetStageTheme(bool english, int stage)
        {
            var config = GetStageNodeConfig(stage);
            if (config != null)
            {
                var configuredTheme = english ? config.ThemeEn : config.ThemeZh;
                if (!string.IsNullOrEmpty(configuredTheme))
                {
                    return configuredTheme;
                }
            }

            return LocalizationManager.GetText("map.stage_theme_default");
        }

        public static string BuildCurrentObjective(bool english)
        {
            EnsureInstance();
            if (Instance == null)
            {
                return LocalizationManager.GetText("progress.current_objective_start");
            }

            var database = ObjectiveDatabaseLoader.Load();
            if (database != null && database.Objectives != null && database.Objectives.Count > 0)
            {
                ObjectiveConfig mainObjective;
                ObjectiveConfig hintObjective;
                List<ObjectiveConfig> stageObjectives;
                Instance.GetActiveObjectives(database, out mainObjective, out stageObjectives, out hintObjective);

                if (mainObjective != null)
                {
                    return GetObjectiveLocalizedText(mainObjective.TitleKey);
                }

                if (stageObjectives != null && stageObjectives.Count > 0)
                {
                    return GetObjectiveLocalizedText(stageObjectives[0].TitleKey);
                }

                if (hintObjective != null)
                {
                    return GetObjectiveLocalizedText(hintObjective.TitleKey);
                }
            }

            var currentStage = GetCurrentStage();
            return currentStage <= 0
                ? LocalizationManager.GetText("progress.current_objective_start")
                : string.Format(LocalizationManager.GetText("progress.current_objective_stage"), currentStage);
        }

        public static int GetNodeMonthCost(int stage)
        {
            return 1;
        }

        public static string BuildCurrentNodeDetail(bool english)
        {
            return BuildNodeDetail(english, Mathf.Max(1, GetCurrentStage()));
        }

        public static string BuildNodeDetail(bool english, int stage)
        {
            var resolvedStage = Mathf.Max(1, stage);
            var config = GetStageNodeConfig(resolvedStage);
            if (config != null)
            {
                var configuredDetail = english ? config.DetailEn : config.DetailZh;
                if (!string.IsNullOrEmpty(configuredDetail))
                {
                    return configuredDetail;
                }
            }

            switch (GetNodeType(resolvedStage))
            {
                case MapNodeType.Elite:
                    return LocalizationManager.GetText("map.node_detail_elite");
                case MapNodeType.Rest:
                    return LocalizationManager.GetText("map.node_detail_rest");
                case MapNodeType.Boss:
                    return LocalizationManager.GetText("map.node_detail_boss");
                case MapNodeType.Battle:
                default:
                    return LocalizationManager.GetText("map.node_detail_battle");
            }
        }

        public static string BuildLastBattleSummary(bool english)
        {
            if (GetLastBattleStage() <= 0)
            {
                return LocalizationManager.GetText("progress.last_battle_none");
            }

            return string.Format(
                LocalizationManager.GetText("progress.last_battle_summary"),
                GetLastBattleStage(),
                LocalizationManager.GetText(GetLastBattleVictory() ? "battle.status_victory" : "battle.status_defeat"),
                GetLastBattleRounds());
        }

        public static string BuildPostBattleNextStep(bool english, bool lastBattleVictory)
        {
            if (lastBattleVictory)
            {
                var nextStage = Mathf.Max(1, GetCurrentStage() + 1);
                return string.Format(LocalizationManager.GetText("progress.next_step_victory"), nextStage);
            }

            var retryStage = Mathf.Max(1, GetCurrentStage());
            return string.Format(LocalizationManager.GetText("progress.next_step_defeat"), retryStage);
        }

        public static string BuildProgressSummary(bool english)
        {
            EnsureInstance();
            var currentStage = GetCurrentStage();
            var highestCleared = GetHighestClearedStage();

            var builder = new StringBuilder();
            builder.Append(string.Format(LocalizationManager.GetText("progress.summary_line_stage"), currentStage, highestCleared))
                .Append("\n")
                .Append(LocalizationManager.GetText("progress.run_state"))
                .Append(LocalizationManager.GetText(HasActiveRun() ? "progress.run_state_active" : "progress.run_state_idle"))
                .Append("\n")
                .Append(LocalizationManager.GetText("progress.next_step_line"))
                .Append(HasActiveRun()
                    ? string.Format(LocalizationManager.GetText("progress.continue_stage"), currentStage)
                    : LocalizationManager.GetText("progress.start_from_stage_one"))
                .Append("\n")
                .Append(string.Format(LocalizationManager.GetText("progress.summary_line_cultivation"), GetCultivationLevel(), GetCultivationExp(), GetRequiredExpForNextLevel()))
                .Append("\n")
                .Append(BuildSpiritStoneSummary(english));

            if (Instance != null && Instance.HasLastBattle)
            {
                builder.Append("\n")
                    .Append(LocalizationManager.GetText("progress.summary_line_last_battle"))
                    .Append(BuildLastBattleSummary(english));
            }

            return builder.ToString();
        }

        public static string BuildLongevitySummary(bool english)
        {
            var remainingMonths = GetRemainingMonths();
            var years = remainingMonths / 12;
            var months = remainingMonths % 12;

            if (remainingMonths <= 0)
            {
                return LocalizationManager.GetText("progress.lifespan_exhausted");
            }

            return string.Format(LocalizationManager.GetText("progress.lifespan_remaining"), years, months);
        }

        public static string BuildMapRouteSummary(bool english)
        {
            var builder = new System.Text.StringBuilder();
            var currentStage = Mathf.Max(1, GetCurrentStage());
            var maxStage = GetMaxStage();
            var maxReachable = GetMaxReachableStage();

            for (var stage = 1; stage <= maxStage; stage++)
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                string stateKey;
                if (stage < currentStage)
                {
                    stateKey = "progress.route_state_done";
                }
                else if (stage == currentStage)
                {
                    stateKey = "progress.route_state_now";
                }
                else if (stage <= maxReachable)
                {
                    stateKey = "progress.route_state_open";
                }
                else
                {
                    stateKey = "progress.route_state_locked";
                }

                builder.Append(LocalizationManager.GetText(stateKey))
                    .Append(' ')
                    .Append(stage)
                    .Append(". ")
                    .Append(GetNodeTypeLabel(english, stage));
            }

            return builder.ToString();
        }

        public static string GetNodeTypeLabel(bool english, int stage)
        {
            if (stage == 1)
            {
                return LocalizationManager.GetText("map.node_type_village");
            }

            switch (GetNodeType(stage))
            {
                case MapNodeType.Elite:
                    return LocalizationManager.GetText("map.node_type_elite");
                case MapNodeType.Rest:
                    return LocalizationManager.GetText("map.node_type_rest");
                case MapNodeType.Boss:
                    return LocalizationManager.GetText("map.node_type_boss");
                case MapNodeType.Battle:
                default:
                    return LocalizationManager.GetText("map.node_type_battle");
            }
        }

        private void LoadProgress()
        {
            CurrentStage = Mathf.Max(0, PlayerPrefs.GetInt(CurrentStagePrefKey, 0));
            HighestClearedStage = Mathf.Max(0, PlayerPrefs.GetInt(HighestClearedStagePrefKey, 0));
            HasLastBattle = PlayerPrefs.GetInt(HasLastBattlePrefKey, 0) == 1;
            LastBattleStage = Mathf.Max(0, PlayerPrefs.GetInt(LastBattleStagePrefKey, 0));
            LastBattleRounds = Mathf.Max(0, PlayerPrefs.GetInt(LastBattleRoundsPrefKey, 0));
            LastBattleVictory = PlayerPrefs.GetInt(LastBattleVictoryPrefKey, 0) == 1;
            ElapsedMonths = Mathf.Max(0, PlayerPrefs.GetInt(ElapsedMonthsPrefKey, 0));
            CultivationLevel = Mathf.Max(1, PlayerPrefs.GetInt(CultivationLevelPrefKey, 1));
            CultivationExp = Mathf.Max(0, PlayerPrefs.GetInt(CultivationExpPrefKey, 0));
            SpiritStones = Mathf.Max(0, PlayerPrefs.GetInt(SpiritStonesPrefKey, 0));
            MetalSpiritStones = Mathf.Max(0, PlayerPrefs.GetInt(MetalSpiritStonesPrefKey, 0));
            WoodSpiritStones = Mathf.Max(0, PlayerPrefs.GetInt(WoodSpiritStonesPrefKey, 0));
            WaterSpiritStones = Mathf.Max(0, PlayerPrefs.GetInt(WaterSpiritStonesPrefKey, 0));
            FireSpiritStones = Mathf.Max(0, PlayerPrefs.GetInt(FireSpiritStonesPrefKey, 0));
            EarthSpiritStones = Mathf.Max(0, PlayerPrefs.GetInt(EarthSpiritStonesPrefKey, 0));
            equipmentInstanceCounter = Mathf.Max(0, PlayerPrefs.GetInt(EquipmentInstanceCounterPrefKey, 0));
            if (MetalSpiritStones + WoodSpiritStones + WaterSpiritStones + FireSpiritStones + EarthSpiritStones <= 0
                && SpiritStones > 0)
            {
                MetalSpiritStones = SpiritStones;
            }
            SyncSpiritStoneTotal();
            LoadOwnedEquipment();
            LoadLearnedSkills();
            LoadPendingSkillRewards();
            LoadCompletedFixedEventStages();
            LoadRandomStageEventStates();
            LoadObjectiveCounters();
            LoadActiveEffects();
        }

        private void SaveProgress()
        {
            PlayerPrefs.SetInt(CurrentStagePrefKey, CurrentStage);
            PlayerPrefs.SetInt(HighestClearedStagePrefKey, HighestClearedStage);
            PlayerPrefs.SetInt(HasLastBattlePrefKey, HasLastBattle ? 1 : 0);
            PlayerPrefs.SetInt(LastBattleStagePrefKey, LastBattleStage);
            PlayerPrefs.SetInt(LastBattleRoundsPrefKey, LastBattleRounds);
            PlayerPrefs.SetInt(LastBattleVictoryPrefKey, LastBattleVictory ? 1 : 0);
            PlayerPrefs.SetInt(ElapsedMonthsPrefKey, ElapsedMonths);
            PlayerPrefs.SetInt(CultivationLevelPrefKey, CultivationLevel);
            PlayerPrefs.SetInt(CultivationExpPrefKey, CultivationExp);
            SyncSpiritStoneTotal();
            PlayerPrefs.SetInt(SpiritStonesPrefKey, SpiritStones);
            PlayerPrefs.SetInt(MetalSpiritStonesPrefKey, MetalSpiritStones);
            PlayerPrefs.SetInt(WoodSpiritStonesPrefKey, WoodSpiritStones);
            PlayerPrefs.SetInt(WaterSpiritStonesPrefKey, WaterSpiritStones);
            PlayerPrefs.SetInt(FireSpiritStonesPrefKey, FireSpiritStones);
            PlayerPrefs.SetInt(EarthSpiritStonesPrefKey, EarthSpiritStones);
            PlayerPrefs.SetInt(EquipmentInstanceCounterPrefKey, equipmentInstanceCounter);
            PlayerPrefs.SetString(OwnedEquipmentPrefKey, SerializeOwnedEquipment());
            PlayerPrefs.SetString(LearnedSkillsPrefKey, SerializeLearnedSkills());
            PlayerPrefs.SetString(PendingSkillRewardsPrefKey, SerializePendingSkillRewards());
            PlayerPrefs.SetString(FixedStageEventsPrefKey, SerializeCompletedFixedEventStages());
            PlayerPrefs.SetString(RandomStageEventStatesPrefKey, SerializeRandomStageEventStates());
            PlayerPrefs.SetString(ObjectiveCountersPrefKey, SerializeObjectiveCounters());
            PlayerPrefs.SetString(ActiveEffectsPrefKey, SerializeActiveEffects());
            PlayerPrefs.SetString(RunDataSnapshotPrefKey, JsonUtility.ToJson(BuildRunDataSnapshot()));
            PlayerPrefs.Save();
        }

        private int GetRequiredExpInternal()
        {
            return 60 + Mathf.Max(0, CultivationLevel - 1) * 25;
        }

        private EquipmentConfig RollEquipmentDrop(EquipmentDatabase equipmentDatabase, int stage)
        {
            if (equipmentDatabase == null || equipmentDatabase.Equipments == null)
            {
                return null;
            }

            var candidates = new List<EquipmentConfig>();
            var targetLevel = GetEquipmentDropLevelByStage(stage);
            var preferredQuality = GetPreferredEquipmentQuality(stage, GetNodeType(stage));
            for (var i = 0; i < equipmentDatabase.Equipments.Count; i++)
            {
                var equipment = equipmentDatabase.Equipments[i];
                if (equipment == null)
                {
                    continue;
                }

                 if (equipment.Level != targetLevel)
                {
                    continue;
                }

                if (!string.Equals(NormalizeEquipmentQuality(equipment.Quality), preferredQuality, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var score = equipment.HP + equipment.ATK * 2 + equipment.DEF * 2 + equipment.MP;
                if (score < GetStageDropScoreFloor(stage))
                {
                    continue;
                }

                candidates.Add(equipment);
            }

            if (candidates.Count == 0)
            {
                var fallbackLevel = targetLevel;
                while (fallbackLevel >= 1 && candidates.Count == 0)
                {
                    for (var i = 0; i < equipmentDatabase.Equipments.Count; i++)
                    {
                        var equipment = equipmentDatabase.Equipments[i];
                        if (equipment == null || equipment.Level != fallbackLevel)
                        {
                            continue;
                        }

                        candidates.Add(equipment);
                    }

                    fallbackLevel--;
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            var index = UnityEngine.Random.Range(0, candidates.Count);
            return candidates[index];
        }

        private static int GetStageDropScoreFloor(int stage)
        {
            if (stage <= 1)
            {
                return 20;
            }

            if (stage <= 3)
            {
                return 24;
            }

            if (stage <= 5)
            {
                return 28;
            }

            return 30;
        }

        private static int GetEquipmentDropLevelByStage(int stage)
        {
            var resolvedStage = Mathf.Max(1, stage);
            if (resolvedStage <= 2)
            {
                return 1;
            }

            if (resolvedStage <= 4)
            {
                return 2;
            }

            if (resolvedStage <= 6)
            {
                return 3;
            }

            if (resolvedStage <= 8)
            {
                return 4;
            }

            return 5;
        }

        private static string GetPreferredEquipmentQuality(int stage, MapNodeType nodeType)
        {
            var roll = UnityEngine.Random.value;
            switch (nodeType)
            {
                case MapNodeType.Boss:
                    if (roll < 0.08f) return "gold";
                    if (roll < 0.48f) return "purple";
                    return "blue";
                case MapNodeType.Elite:
                    if (roll < 0.28f) return "blue";
                    return "green";
                case MapNodeType.Rest:
                    return stage >= 5 ? "green" : "white";
                case MapNodeType.Battle:
                default:
                    if (roll < 0.22f) return "green";
                    return "white";
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

        private void ResetOwnedEquipmentToBase()
        {
            ownedEquipments.Clear();
            for (var i = 0; i < BaseOwnedEquipmentIds.Length; i++)
            {
                AddOwnedEquipmentInstance(BaseOwnedEquipmentIds[i]);
            }
        }

        private void LoadOwnedEquipment()
        {
            ownedEquipments.Clear();
            var raw = PlayerPrefs.GetString(OwnedEquipmentPrefKey, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                ResetOwnedEquipmentToBase();
                return;
            }

            if (raw.TrimStart().StartsWith("{", StringComparison.Ordinal))
            {
                var wrapper = JsonUtility.FromJson<EquipmentInstanceListWrapper>(raw);
                if (wrapper != null && wrapper.Entries != null && wrapper.Entries.Count > 0)
                {
                    for (var i = 0; i < wrapper.Entries.Count; i++)
                    {
                        var entry = wrapper.Entries[i];
                        if (entry == null || string.IsNullOrWhiteSpace(entry.EquipmentId))
                        {
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(entry.InstanceId))
                        {
                            entry.InstanceId = GenerateEquipmentInstanceId();
                        }

                        ownedEquipments.Add(entry);
                    }

                    SyncEquipmentInstanceCounterFromOwnedEquipments();
                    return;
                }
            }

            var parts = raw.Split('|');
            for (var i = 0; i < parts.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(parts[i]))
                {
                    AddOwnedEquipmentInstance(parts[i].Trim());
                }
            }
        }

        private string SerializeOwnedEquipment()
        {
            var ordered = new List<EquipmentInstanceData>();
            for (var i = 0; i < ownedEquipments.Count; i++)
            {
                if (ownedEquipments[i] != null && !string.IsNullOrWhiteSpace(ownedEquipments[i].EquipmentId))
                {
                    ordered.Add(CloneEquipmentInstance(ownedEquipments[i]));
                }
            }

            ordered.Sort(delegate(EquipmentInstanceData left, EquipmentInstanceData right)
            {
                var leftKey = (left != null ? left.EquipmentId : string.Empty) + "|" + (left != null ? left.InstanceId : string.Empty);
                var rightKey = (right != null ? right.EquipmentId : string.Empty) + "|" + (right != null ? right.InstanceId : string.Empty);
                return string.Compare(leftKey, rightKey, StringComparison.OrdinalIgnoreCase);
            });

            return JsonUtility.ToJson(new EquipmentInstanceListWrapper
            {
                Entries = ordered
            });
        }

        private EquipmentInstanceData AddOwnedEquipmentInstance(string equipmentId)
        {
            if (string.IsNullOrWhiteSpace(equipmentId))
            {
                return null;
            }

            var created = new EquipmentInstanceData
            {
                InstanceId = GenerateEquipmentInstanceId(),
                EquipmentId = equipmentId.Trim()
            };
            ownedEquipments.Add(created);
            return created;
        }

        private string GenerateEquipmentInstanceId()
        {
            equipmentInstanceCounter = Mathf.Max(0, equipmentInstanceCounter) + 1;
            return "EI" + equipmentInstanceCounter.ToString("D6");
        }

        private void SyncEquipmentInstanceCounterFromOwnedEquipments()
        {
            var maxCounter = equipmentInstanceCounter;
            for (var i = 0; i < ownedEquipments.Count; i++)
            {
                var entry = ownedEquipments[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.InstanceId) || entry.InstanceId.Length <= 2)
                {
                    continue;
                }

                if (!entry.InstanceId.StartsWith("EI", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int parsedValue;
                if (int.TryParse(entry.InstanceId.Substring(2), out parsedValue))
                {
                    maxCounter = Mathf.Max(maxCounter, parsedValue);
                }
            }

            equipmentInstanceCounter = maxCounter;
        }

        private static EquipmentInstanceData CloneEquipmentInstance(EquipmentInstanceData source)
        {
            if (source == null)
            {
                return null;
            }

            return new EquipmentInstanceData
            {
                InstanceId = source.InstanceId,
                EquipmentId = source.EquipmentId
            };
        }

        private void LoadLearnedSkills()
        {
            characterRunData.Clear();
            var raw = PlayerPrefs.GetString(LearnedSkillsPrefKey, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            var wrapper = JsonUtility.FromJson<CharacterRunDataListWrapper>(raw);
            if (wrapper == null || wrapper.Entries == null)
            {
                return;
            }

            for (var i = 0; i < wrapper.Entries.Count; i++)
            {
                var entry = wrapper.Entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.CharacterId))
                {
                    continue;
                }

                if (entry.LearnedSkillIds == null)
                {
                    entry.LearnedSkillIds = new List<string>();
                }

                if (entry.EquippedActiveSkillIds == null)
                {
                    entry.EquippedActiveSkillIds = new List<string>();
                }

                if (entry.SkillLevels == null)
                {
                    entry.SkillLevels = new List<SkillLevelData>();
                }

                EnsureEquippedActiveSkills(entry);
                characterRunData.Add(entry);
            }
        }

        private string SerializeLearnedSkills()
        {
            var wrapper = new CharacterRunDataListWrapper
            {
                Entries = characterRunData
            };
            return JsonUtility.ToJson(wrapper);
        }

        private void LoadPendingSkillRewards()
        {
            pendingSkillRewards.Clear();
            var raw = PlayerPrefs.GetString(PendingSkillRewardsPrefKey, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            var wrapper = JsonUtility.FromJson<SkillRewardOptionListWrapper>(raw);
            if (wrapper == null || wrapper.Entries == null)
            {
                return;
            }

            for (var i = 0; i < wrapper.Entries.Count; i++)
            {
                var entry = wrapper.Entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.SkillId))
                {
                    continue;
                }

                pendingSkillRewards.Add(entry);
            }
        }

        private string SerializePendingSkillRewards()
        {
            var wrapper = new SkillRewardOptionListWrapper
            {
                Entries = pendingSkillRewards
            };
            return JsonUtility.ToJson(wrapper);
        }

        private CharacterRunData GetOrCreateCharacterRunData(string characterId)
        {
            for (var i = 0; i < characterRunData.Count; i++)
            {
                if (string.Equals(characterRunData[i].CharacterId, characterId, StringComparison.OrdinalIgnoreCase))
                {
                    if (characterRunData[i].LearnedSkillIds == null)
                    {
                        characterRunData[i].LearnedSkillIds = new List<string>();
                    }

                    if (characterRunData[i].EquippedActiveSkillIds == null)
                    {
                        characterRunData[i].EquippedActiveSkillIds = new List<string>();
                    }

                    if (characterRunData[i].SkillLevels == null)
                    {
                        characterRunData[i].SkillLevels = new List<SkillLevelData>();
                    }

                    EnsureEquippedActiveSkills(characterRunData[i]);
                    return characterRunData[i];
                }
            }

            var created = new CharacterRunData
            {
                CharacterId = characterId,
                LearnedSkillIds = new List<string>(),
                EquippedActiveSkillIds = new List<string>(),
                SkillLevels = new List<SkillLevelData>()
            };
            EnsureEquippedActiveSkills(created);
            characterRunData.Add(created);
            return created;
        }

        private List<string> GetLearnedSkillIdsInternal(string characterId)
        {
            var entry = GetOrCreateCharacterRunData(characterId);
            return new List<string>(entry.LearnedSkillIds);
        }

        private List<string> GetEquippedActiveSkillIdsInternal(string characterId)
        {
            var entry = GetOrCreateCharacterRunData(characterId);
            return new List<string>(entry.EquippedActiveSkillIds);
        }

        private int GetSkillLevelInternal(string characterId, string skillId)
        {
            var characterDatabase = CharacterDatabaseLoader.Load();
            var character = characterDatabase != null ? characterDatabase.GetById(characterId) : null;
            return GetSkillLevelInternal(GetOrCreateCharacterRunData(characterId), character, skillId);
        }

        private int GetSkillLevelInternal(CharacterRunData entry, CharacterConfig character, string skillId)
        {
            if (entry != null && entry.SkillLevels != null)
            {
                for (var i = 0; i < entry.SkillLevels.Count; i++)
                {
                    var levelData = entry.SkillLevels[i];
                    if (levelData != null && string.Equals(levelData.SkillId, skillId, StringComparison.OrdinalIgnoreCase))
                    {
                        return Mathf.Max(1, levelData.Level);
                    }
                }
            }

            return CharacterHasInitialSkill(character, skillId) || (entry != null && entry.LearnedSkillIds.Exists(id =>
                string.Equals(id, skillId, StringComparison.OrdinalIgnoreCase)))
                ? 1
                : 0;
        }

        private void SetSkillLevelInternal(CharacterRunData entry, string skillId, int level)
        {
            if (entry == null || string.IsNullOrEmpty(skillId))
            {
                return;
            }

            if (entry.SkillLevels == null)
            {
                entry.SkillLevels = new List<SkillLevelData>();
            }

            for (var i = 0; i < entry.SkillLevels.Count; i++)
            {
                var levelData = entry.SkillLevels[i];
                if (levelData != null && string.Equals(levelData.SkillId, skillId, StringComparison.OrdinalIgnoreCase))
                {
                    levelData.Level = Mathf.Max(1, level);
                    return;
                }
            }

            entry.SkillLevels.Add(new SkillLevelData
            {
                SkillId = skillId,
                Level = Mathf.Max(1, level)
            });
        }

        private void EquipActiveSkillInternal(string characterId, int slotIndex, string skillId)
        {
            var entry = GetOrCreateCharacterRunData(characterId);
            EnsureEquippedActiveSkills(entry);

            if (slotIndex < 0 || slotIndex >= 3)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(skillId))
            {
                entry.EquippedActiveSkillIds[slotIndex] = string.Empty;
                return;
            }

            var normalizedSkillId = skillId.Trim();
            if (!CharacterCanEquipActiveSkill(characterId, normalizedSkillId))
            {
                return;
            }

            for (var i = 0; i < entry.EquippedActiveSkillIds.Count; i++)
            {
                if (i == slotIndex)
                {
                    continue;
                }

                if (string.Equals(entry.EquippedActiveSkillIds[i], normalizedSkillId, StringComparison.OrdinalIgnoreCase))
                {
                    entry.EquippedActiveSkillIds[i] = string.Empty;
                }
            }

            entry.EquippedActiveSkillIds[slotIndex] = normalizedSkillId;
            RegisterObjectiveEventInternal("ConfigureSkill", characterId, 1);
            RegisterObjectiveEventInternal("ConfigureSkill", string.Empty, 1);
        }

        private void UnequipActiveSkillInternal(string characterId, int slotIndex)
        {
            var entry = GetOrCreateCharacterRunData(characterId);
            EnsureEquippedActiveSkills(entry);
            if (slotIndex < 0 || slotIndex >= entry.EquippedActiveSkillIds.Count)
            {
                return;
            }

            entry.EquippedActiveSkillIds[slotIndex] = string.Empty;
            RegisterObjectiveEventInternal("ConfigureSkill", characterId, 1);
            RegisterObjectiveEventInternal("ConfigureSkill", string.Empty, 1);
        }

        private void EnsureEquippedActiveSkills(CharacterRunData entry)
        {
            if (entry == null)
            {
                return;
            }

            var wasUninitialized = entry.EquippedActiveSkillIds == null || entry.EquippedActiveSkillIds.Count == 0;
            if (entry.EquippedActiveSkillIds == null)
            {
                entry.EquippedActiveSkillIds = new List<string>();
            }

            while (entry.EquippedActiveSkillIds.Count < 3)
            {
                entry.EquippedActiveSkillIds.Add(string.Empty);
            }

            if (entry.EquippedActiveSkillIds.Count > 3)
            {
                entry.EquippedActiveSkillIds.RemoveRange(3, entry.EquippedActiveSkillIds.Count - 3);
            }

            var defaultEquipped = BuildDefaultEquippedActiveSkills(entry.CharacterId, entry);
            for (var i = 0; i < entry.EquippedActiveSkillIds.Count; i++)
            {
                var equippedSkillId = entry.EquippedActiveSkillIds[i];
                if (string.IsNullOrWhiteSpace(equippedSkillId))
                {
                    continue;
                }

                if (CharacterCanEquipActiveSkill(entry.CharacterId, equippedSkillId))
                {
                    continue;
                }

                entry.EquippedActiveSkillIds[i] = i < defaultEquipped.Count ? defaultEquipped[i] : string.Empty;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < entry.EquippedActiveSkillIds.Count; i++)
            {
                var equippedSkillId = entry.EquippedActiveSkillIds[i];
                if (string.IsNullOrWhiteSpace(equippedSkillId))
                {
                    continue;
                }

                if (!seen.Add(equippedSkillId))
                {
                    entry.EquippedActiveSkillIds[i] = string.Empty;
                }
            }

            if (!wasUninitialized)
            {
                return;
            }

            for (var i = 0; i < entry.EquippedActiveSkillIds.Count; i++)
            {
                if (!string.IsNullOrEmpty(entry.EquippedActiveSkillIds[i]))
                {
                    continue;
                }

                for (var j = 0; j < defaultEquipped.Count; j++)
                {
                    if (seen.Add(defaultEquipped[j]))
                    {
                        entry.EquippedActiveSkillIds[i] = defaultEquipped[j];
                        break;
                    }
                }
            }
        }

        private void TryAutoEquipActiveSkill(CharacterRunData entry, string skillId)
        {
            if (entry == null || !CharacterCanEquipActiveSkill(entry.CharacterId, skillId))
            {
                return;
            }

            EnsureEquippedActiveSkills(entry);
            for (var i = 0; i < entry.EquippedActiveSkillIds.Count; i++)
            {
                if (string.Equals(entry.EquippedActiveSkillIds[i], skillId, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            for (var i = 0; i < entry.EquippedActiveSkillIds.Count; i++)
            {
                if (string.IsNullOrEmpty(entry.EquippedActiveSkillIds[i]))
                {
                    entry.EquippedActiveSkillIds[i] = skillId;
                    return;
                }
            }
        }

        private List<string> BuildDefaultEquippedActiveSkills(string characterId, CharacterRunData entry)
        {
            var knownSkillIds = GetKnownSkillIds(characterId, entry);
            var skillDatabase = SkillDatabaseLoader.Load();
            var activeSkills = new List<SkillConfig>();
            for (var i = 0; i < knownSkillIds.Count; i++)
            {
                var skill = skillDatabase != null ? skillDatabase.GetById(knownSkillIds[i]) : null;
                if (skill == null || IsPassiveSkill(skill))
                {
                    continue;
                }

                activeSkills.Add(skill);
            }

            activeSkills.Sort(delegate(SkillConfig left, SkillConfig right)
            {
                var priorityCompare = right.Priority.CompareTo(left.Priority);
                if (priorityCompare != 0)
                {
                    return priorityCompare;
                }

                return string.CompareOrdinal(left.Id, right.Id);
            });

            var results = new List<string>();
            for (var i = 0; i < activeSkills.Count && results.Count < 3; i++)
            {
                results.Add(activeSkills[i].Id);
            }

            return results;
        }

        private List<string> GetKnownSkillIds(string characterId, CharacterRunData entry)
        {
            var knownSkillIds = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var characterDatabase = CharacterDatabaseLoader.Load();
            var character = characterDatabase != null ? characterDatabase.GetById(characterId) : null;
            if (character != null)
            {
                AddSkillIds(character.InitialSkills, knownSkillIds, seen);
            }

            if (entry != null && entry.LearnedSkillIds != null)
            {
                for (var i = 0; i < entry.LearnedSkillIds.Count; i++)
                {
                    var skillId = entry.LearnedSkillIds[i];
                    if (!string.IsNullOrWhiteSpace(skillId) && seen.Add(skillId.Trim()))
                    {
                        knownSkillIds.Add(skillId.Trim());
                    }
                }
            }

            return knownSkillIds;
        }

        private static void AddSkillIds(string rawSkills, List<string> sink, HashSet<string> seen)
        {
            if (string.IsNullOrWhiteSpace(rawSkills))
            {
                return;
            }

            var parts = rawSkills.Split(new[] { '|', ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                var skillId = parts[i].Trim();
                if (string.IsNullOrEmpty(skillId) || !seen.Add(skillId))
                {
                    continue;
                }

                sink.Add(skillId);
            }
        }

        private bool CharacterCanEquipActiveSkill(string characterId, string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                return false;
            }

            var skillDatabase = SkillDatabaseLoader.Load();
            var skill = skillDatabase != null ? skillDatabase.GetById(skillId) : null;
            if (skill == null || IsPassiveSkill(skill))
            {
                return false;
            }

            var characterDatabase = CharacterDatabaseLoader.Load();
            var character = characterDatabase != null ? characterDatabase.GetById(characterId) : null;
            CharacterRunData entry = null;
            for (var i = 0; i < characterRunData.Count; i++)
            {
                if (string.Equals(characterRunData[i].CharacterId, characterId, StringComparison.OrdinalIgnoreCase))
                {
                    entry = characterRunData[i];
                    break;
                }
            }

            return CharacterHasInitialSkill(character, skillId)
                || (entry != null && entry.LearnedSkillIds != null && entry.LearnedSkillIds.Exists(delegate(string learnedId)
                {
                    return string.Equals(learnedId, skillId, StringComparison.OrdinalIgnoreCase);
                }));
        }

        private bool CharacterHasSkillInternal(CharacterConfig character, string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            if (CharacterHasInitialSkill(character, skillId))
            {
                return true;
            }

            if (character == null)
            {
                return false;
            }

            var entry = GetOrCreateCharacterRunData(character.Id);
            return entry.LearnedSkillIds.Exists(id => string.Equals(id, skillId, StringComparison.OrdinalIgnoreCase));
        }

        private RunData BuildRunDataSnapshot()
        {
            SyncSpiritStoneTotal();
            var data = new RunData
            {
                CurrentStage = CurrentStage,
                HighestClearedStage = HighestClearedStage,
                ElapsedMonths = ElapsedMonths,
                RemainingMonths = Mathf.Max(0, LifetimeMonths - ElapsedMonths),
                CultivationLevel = CultivationLevel,
                CultivationExp = CultivationExp,
                SpiritStones = SpiritStones,
                MetalSpiritStones = MetalSpiritStones,
                WoodSpiritStones = WoodSpiritStones,
                WaterSpiritStones = WaterSpiritStones,
                FireSpiritStones = FireSpiritStones,
                EarthSpiritStones = EarthSpiritStones,
                HasLastBattle = HasLastBattle,
                LastBattleStage = LastBattleStage,
                LastBattleRounds = LastBattleRounds,
                LastBattleVictory = LastBattleVictory
            };

            for (var i = 0; i < ownedEquipments.Count; i++)
            {
                if (ownedEquipments[i] != null)
                {
                    data.OwnedEquipments.Add(CloneEquipmentInstance(ownedEquipments[i]));
                }
            }
            for (var i = 0; i < characterRunData.Count; i++)
            {
                var entry = characterRunData[i];
                if (entry == null)
                {
                    continue;
                }

                data.Characters.Add(new CharacterRunData
                {
                    CharacterId = entry.CharacterId,
                    LearnedSkillIds = new List<string>(entry.LearnedSkillIds),
                    EquippedActiveSkillIds = entry.EquippedActiveSkillIds != null ? new List<string>(entry.EquippedActiveSkillIds) : new List<string>(),
                    SkillLevels = entry.SkillLevels != null ? new List<SkillLevelData>(entry.SkillLevels) : new List<SkillLevelData>()
                });
            }

            for (var i = 0; i < pendingSkillRewards.Count; i++)
            {
                if (pendingSkillRewards[i] != null)
                {
                    data.PendingSkillRewards.Add(pendingSkillRewards[i]);
                }
            }

            for (var i = 0; i < activeEffects.Count; i++)
            {
                var effect = activeEffects[i];
                if (effect != null)
                {
                    data.ActiveEffects.Add(CloneRunEffect(effect));
                }
            }

            return data;
        }

        private static string ToChineseNumber(int value)
        {
            var digits = new[] { "\u96f6", "\u4e00", "\u4e8c", "\u4e09", "\u56db", "\u4e94", "\u516d", "\u4e03", "\u516b", "\u4e5d" };
            if (value < 10)
            {
                return digits[Mathf.Clamp(value, 0, 9)];
            }

            if (value < 20)
            {
                return value == 10 ? "\u5341" : "\u5341" + digits[value % 10];
            }

            if (value < 100)
            {
                var tens = value / 10;
                var units = value % 10;
                return units == 0
                    ? digits[tens] + "\u5341"
                    : digits[tens] + "\u5341" + digits[units];
            }

            return value.ToString();
        }

        private static bool IsPassiveSkillReward(SkillConfig skill)
        {
            if (skill == null)
            {
                return true;
            }

            if (string.Equals(skill.Category, "Passive", StringComparison.OrdinalIgnoreCase)
                || string.Equals(skill.Category, "\u88ab\u52a8", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (skill.Effects == null || skill.Effects.Count == 0)
            {
                return string.Equals(skill.EffectType, "DotBoost", StringComparison.OrdinalIgnoreCase);
            }

            for (var i = 0; i < skill.Effects.Count; i++)
            {
                var effect = skill.Effects[i];
                if (effect != null && string.Equals(effect.EffectType, "DotBoost", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPassiveSkill(SkillConfig skill)
        {
            return IsPassiveSkillReward(skill);
        }
        private static bool CharacterHasInitialSkill(CharacterConfig character, string skillId)
        {
            if (character == null || string.IsNullOrEmpty(character.InitialSkills) || string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            var parts = character.InitialSkills.Split('|');
            for (var i = 0; i < parts.Length; i++)
            {
                if (string.Equals(parts[i].Trim(), skillId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetSkillRewardWeight(SkillConfig skill, MapNodeType nodeType, bool isUpgrade)
        {
            var quality = NormalizeSkillQuality(skill != null ? skill.Quality : string.Empty);
            var weight = 1;

            switch (nodeType)
            {
                case MapNodeType.Boss:
                    weight = quality == "epic" ? 12 : quality == "rare" ? 7 : 2;
                    break;
                case MapNodeType.Elite:
                    weight = quality == "epic" ? 5 : quality == "rare" ? 10 : 4;
                    break;
                default:
                    weight = quality == "epic" ? 1 : quality == "rare" ? 5 : 12;
                    break;
            }

            if (isUpgrade)
            {
                weight += 3;
            }

            return Mathf.Max(1, weight);
        }

        private static string NormalizeSkillQuality(string quality)
        {
            if (string.IsNullOrEmpty(quality))
            {
                return "common";
            }

            switch (quality.Trim().ToLowerInvariant())
            {
                case "\u7edd\u54c1":
                case "epic":
                case "legendary":
                    return "epic";
                case "\u7a00\u6709":
                case "rare":
                    return "rare";
                case "\u666e\u901a":
                case "common":
                default:
                    return "common";
            }
        }

        private static void CollectKnownSkillIds(string rawSkills, HashSet<string> sink)
        {
            if (sink == null || string.IsNullOrEmpty(rawSkills))
            {
                return;
            }

            var parts = rawSkills.Split('|');
            for (var i = 0; i < parts.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(parts[i]))
                {
                    sink.Add(parts[i].Trim());
                }
            }
        }

        public static string BuildSkillRewardDetail(SkillRewardOption option, bool english)
        {
            if (option == null)
            {
                return english ? "No reward option." : "没有可选奖励。";
            }

            var skillDatabase = SkillDatabaseLoader.Load();
            var skill = skillDatabase != null ? skillDatabase.GetById(option.SkillId) : null;
            var effectSummary = skill != null ? BuildSkillEffectSummary(skill, option.ResultLevel, english) : option.SkillDescription;

            return english
                ? "Target: " + option.CharacterName + "\nSkill: " + WrapSkillNameWithQualityColor(option.SkillName, option.SkillQuality) + "\nResult: Lv." + option.ResultLevel + (option.IsUpgrade ? " Upgrade" : " Learn") + "\nElement: " + option.SkillElement + "\nEffect: " + effectSummary
                : "角色：" + option.CharacterName + "\n功法：" + WrapSkillNameWithQualityColor(option.SkillName, option.SkillQuality) + "\n结果：Lv." + option.ResultLevel + (option.IsUpgrade ? " 升级" : " 习得") + "\n五行：" + option.SkillElement + "\n效果：" + effectSummary;
        }

        public static string BuildSkillRewardChoiceText(SkillRewardOption option, bool english)
        {
            if (option == null)
            {
                return english ? "Empty" : "空白奖励";
            }

            return english
                ? WrapSkillNameWithQualityColor(option.SkillName, option.SkillQuality) + "\nLv." + option.ResultLevel + "\n" + option.CharacterName
                : WrapSkillNameWithQualityColor(option.SkillName, option.SkillQuality) + "\nLv." + option.ResultLevel + "\n" + option.CharacterName;
        }

        public static string BuildSkillDetail(string characterId, SkillConfig skill, bool english)
        {
            if (skill == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.Append(english ? "角色: " : "角色：")
                .Append(GetCharacterName(characterId))
                .Append('\n')
                .Append(english ? "类型: " : "类型：")
                .Append(IsPassiveSkill(skill) ? (english ? "被动" : "被动") : (english ? "主动" : "主动"))
                .Append('\n')
                .Append(english ? "五行: " : "五行：")
                .Append(skill.Element)
                .Append('\n')
                .Append(english ? "优先级: " : "优先级：")
                .Append(skill.Priority)
                .Append('\n')
                .Append(english ? "冷却: " : "冷却：")
                .Append(Mathf.Max(0, skill.Cooldown))
                .Append(english ? " 回合" : " 回合")
                .Append('\n')
                .Append(english ? "触发: " : "触发：")
                .Append(DescribeTrigger(skill, english))
                .Append('\n')
                .Append(english ? "选敌: " : "选敌：")
                .Append(DescribeTargetRule(skill, english))
                .Append('\n')
                .Append(english ? "效果: " : "效果：")
                .Append(BuildSkillEffectSummary(skill, GetSkillLevel(characterId, skill.Id), english));
            return builder.ToString();
        }

        private static void AppendSkillNames(string characterId, string rawSkills, List<string> targetNames, HashSet<string> knownSkillIds, SkillDatabase skillDatabase, bool english)
        {
            if (targetNames == null || string.IsNullOrEmpty(rawSkills))
            {
                return;
            }

            var parts = rawSkills.Split('|');
            for (var i = 0; i < parts.Length; i++)
            {
                var skillId = parts[i].Trim();
                if (string.IsNullOrEmpty(skillId))
                {
                    continue;
                }

                knownSkillIds?.Add(skillId);
                targetNames.Add(GetSkillLabel(skillDatabase, characterId, skillId, english));
            }
        }

        private static string GetSkillDisplayName(SkillDatabase skillDatabase, string skillId)
        {
            if (skillDatabase == null || string.IsNullOrEmpty(skillId))
            {
                return skillId ?? string.Empty;
            }

            var skill = skillDatabase.GetById(skillId);
            return skill != null && !string.IsNullOrEmpty(skill.Name) ? skill.Name : skillId;
        }

        private static string GetSkillLabel(SkillDatabase skillDatabase, string characterId, string skillId, bool english)
        {
            if (skillDatabase == null || string.IsNullOrEmpty(skillId))
            {
                return GetSkillDisplayName(skillDatabase, skillId);
            }

            var skill = skillDatabase.GetById(skillId);
            if (skill == null)
            {
                return GetSkillDisplayName(skillDatabase, skillId)
                    + " "
                    + LocalizationManager.GetText("common.level_prefix")
                    + GetSkillLevel(characterId, skillId);
            }

            var builder = new StringBuilder();
            builder.Append(WrapSkillNameWithQualityColor(skill.Name, skill.Quality))
                .Append("  ")
                .Append(LocalizationManager.GetText("common.level_prefix"))
                .Append(GetSkillLevel(characterId, skillId));

            if (!string.IsNullOrEmpty(skill.Element))
            {
                builder.Append('\n')
                    .Append(LocalizationManager.GetText("progress.skills_element"))
                    .Append(skill.Element);
            }

            var effectSummary = BuildSkillEffectSummary(skill, GetSkillLevel(characterId, skillId), english);
            if (!string.IsNullOrEmpty(effectSummary))
            {
                builder.Append('\n')
                    .Append(LocalizationManager.GetText("progress.skills_effect"))
                    .Append(effectSummary);
            }

            return builder.ToString();
        }

        private static string GetCharacterName(string characterId)
        {
            var characterDatabase = CharacterDatabaseLoader.Load();
            var character = characterDatabase != null ? characterDatabase.GetById(characterId) : null;
            return character != null ? character.Name : characterId;
        }

        private static string DescribeTrigger(SkillConfig skill, bool english)
        {
            if (skill == null || string.IsNullOrWhiteSpace(skill.TriggerType))
            {
                return LocalizationManager.GetText("skill.trigger.default");
            }

            var triggerType = skill.TriggerType.Trim();
            var triggerValue = skill.TriggerValue ?? string.Empty;
            var triggerKey = GetTriggerLocalizationKey(triggerType);
            if (string.IsNullOrEmpty(triggerKey))
            {
                return triggerType;
            }

            var template = LocalizationManager.GetText(triggerKey);
            return TriggerNeedsArgument(triggerType)
                ? string.Format(template, DescribeTriggerArgument(triggerType, triggerValue))
                : template;
        }

        private static string DescribeTargetRule(SkillConfig skill, bool english)
        {
            if (skill == null || string.IsNullOrWhiteSpace(skill.TargetRule))
            {
                return LocalizationManager.GetText("skill.target.default");
            }

            var targetRule = skill.TargetRule.Trim();
            var targetKey = GetTargetRuleLocalizationKey(targetRule);
            if (!string.IsNullOrEmpty(targetKey))
            {
                return LocalizationManager.GetText(targetKey);
            }

            return skill.TargetRule;
        }

        private static string GetTriggerLocalizationKey(string triggerType)
        {
            switch ((triggerType ?? string.Empty).Trim())
            {
                case "":
                case "Always":
                case "Passive":
                    return "skill.trigger.default";
                case "FirstRound":
                    return "skill.trigger.first_round";
                case "SelfHpBelowPct":
                    return "skill.trigger.self_hp_below_pct";
                case "AllyHpBelowPct":
                    return "skill.trigger.ally_hp_below_pct";
                case "EnemyCountAtLeast":
                    return "skill.trigger.enemy_count_at_least";
                case "TargetHpBelowPct":
                    return "skill.trigger.target_hp_below_pct";
                case "TargetHpAbovePct":
                    return "skill.trigger.target_hp_above_pct";
                case "SelfNoShield":
                    return "skill.trigger.self_no_shield";
                case "TargetHasShield":
                    return "skill.trigger.target_has_shield";
                case "SelfHasStatus":
                    return "skill.trigger.self_has_status";
                case "TargetHasDebuff":
                    return "skill.trigger.target_has_debuff";
                default:
                    return null;
            }
        }

        private static bool TriggerNeedsArgument(string triggerType)
        {
            switch ((triggerType ?? string.Empty).Trim())
            {
                case "SelfHpBelowPct":
                case "AllyHpBelowPct":
                case "EnemyCountAtLeast":
                case "TargetHpBelowPct":
                case "TargetHpAbovePct":
                case "SelfHasStatus":
                case "TargetHasDebuff":
                    return true;
                default:
                    return false;
            }
        }

        private static string DescribeTriggerArgument(string triggerType, string triggerValue)
        {
            switch ((triggerType ?? string.Empty).Trim())
            {
                case "SelfHasStatus":
                case "TargetHasDebuff":
                    return string.IsNullOrWhiteSpace(triggerValue) ? "-" : triggerValue.Trim();
                default:
                    return string.IsNullOrWhiteSpace(triggerValue) ? "0" : triggerValue.Trim();
            }
        }

        private static string GetTargetRuleLocalizationKey(string targetRule)
        {
            switch ((targetRule ?? string.Empty).Trim())
            {
                case "":
                    return "skill.target.default";
                case "LowestHPEnemy":
                    return "skill.target.lowest_hp_enemy";
                case "HighestHPEnemy":
                    return "skill.target.highest_hp_enemy";
                case "HighestATKEnemy":
                    return "skill.target.highest_atk_enemy";
                case "RandomEnemy":
                    return "skill.target.random_enemy";
                case "AllEnemies":
                    return "skill.target.all_enemies";
                case "Self":
                    return "skill.target.self";
                case "LowestHPAlly":
                    return "skill.target.lowest_hp_ally";
                case "AllAllies":
                    return "skill.target.all_allies";
                default:
                    return null;
            }
        }

        private static string GetEquipmentSlotLabel(string slot, bool english)
        {
            switch ((slot ?? string.Empty).Trim())
            {
                case "Weapon":
                    return english ? "Weapon" : "武器";
                case "Armor":
                    return english ? "Armor" : "护甲";
                case "Accessory":
                    return english ? "Accessory" : "饰品";
                default:
                    return string.IsNullOrEmpty(slot)
                        ? (english ? "Unknown" : "未知")
                        : slot;
            }
        }

        private static string BuildEquipmentCardSubtitle(EquipmentConfig equipment, bool english)
        {
            if (equipment == null)
            {
                return string.Empty;
            }

            return GetEquipmentQualityLabel(equipment.Quality, english) + " / " + GetEquipmentLevelLabel(equipment.Level, english);
        }

        private static string GetEquipmentQualityLabel(string quality, bool english)
        {
            switch (NormalizeEquipmentQuality(quality))
            {
                case "green":
                    return english ? "Green" : "绿";
                case "blue":
                    return english ? "Blue" : "蓝";
                case "purple":
                    return english ? "Purple" : "紫";
                case "gold":
                    return english ? "Gold" : "金";
                case "white":
                default:
                    return english ? "White" : "白";
            }
        }

        private static string GetEquipmentLevelLabel(int level, bool english)
        {
            var clamped = Mathf.Clamp(level, 1, 5);
            if (english)
            {
                return "Lv." + clamped;
            }

            switch (clamped)
            {
                case 1: return "一阶";
                case 2: return "二阶";
                case 3: return "三阶";
                case 4: return "四阶";
                case 5: return "五阶";
                default: return "一阶";
            }
        }

        private static string GetEquipmentSlotElement(string slot)
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

        private static string BuildEquipmentStatCardLines(EquipmentConfig equipment, bool english)
        {
            if (equipment == null)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            if (equipment.HP != 0)
            {
                parts.Add((english ? "生命 " : "生命 ") + FormatSignedValue(equipment.HP));
            }

            if (equipment.ATK != 0)
            {
                parts.Add((english ? "攻击 " : "攻击 ") + FormatSignedValue(equipment.ATK));
            }

            if (equipment.DEF != 0)
            {
                parts.Add((english ? "防御 " : "防御 ") + FormatSignedValue(equipment.DEF));
            }

            if (equipment.MP != 0)
            {
                parts.Add((english ? "法力 " : "法力 ") + FormatSignedValue(equipment.MP));
            }

            return parts.Count > 0 ? string.Join(english ? "\n" : "\n", parts.ToArray()) : (english ? "No stat bonus" : "暂无属性加成");
        }

        private static string FormatSignedValue(int value)
        {
            return value > 0 ? "+" + value : value.ToString();
        }

        private static string BuildSkillEffectSummary(SkillConfig skill, int level, bool english)
        {
            if (skill == null)
            {
                return string.Empty;
            }

            if (skill.Effects == null || skill.Effects.Count == 0)
            {
                return skill.Description ?? string.Empty;
            }

            var parts = new List<string>();
            for (var i = 0; i < skill.Effects.Count; i++)
            {
                var effect = skill.Effects[i];
                if (effect == null)
                {
                    continue;
                }

                var value = Mathf.Max(0, effect.Value + Mathf.Max(0, level - 1) * effect.ValuePerLevel);
                parts.Add(BuildSkillEffectLine(effect, value, english));
            }

            return parts.Count > 0 ? string.Join(english ? "; " : "；", parts.ToArray()) : (skill.Description ?? string.Empty);
        }

        private static string BuildSkillEffectLine(SkillEffectConfig effect, int value, bool english)
        {
            var rounds = Mathf.Max(0, effect.DurationRounds);
            var scope = GetEffectTargetLabel(effect.TargetScope, english);
            var type = effect.EffectType ?? string.Empty;

            if (string.Equals(type, "Damage", StringComparison.OrdinalIgnoreCase))
            {
                return english ? scope + "deal an extra " + value + " damage" : scope + "额外造成 " + value + " 点伤害";
            }

            if (string.Equals(type, "Heal", StringComparison.OrdinalIgnoreCase))
            {
                return english ? scope + "heal " + value + " HP" : scope + "恢复 " + value + " 点生命";
            }

            if (string.Equals(type, "Shield", StringComparison.OrdinalIgnoreCase))
            {
                return english ? scope + "gain an extra " + value + " shield" : scope + "额外获得 " + value + " 点护盾";
            }

            if (string.Equals(type, "Dot", StringComparison.OrdinalIgnoreCase))
            {
                return english ? scope + "deal an extra " + value + " damage each round for " + rounds + " rounds" : scope + "每回合额外造成 " + value + " 点伤害，持续 " + rounds + " 回合";
            }

            if (string.Equals(type, "Hot", StringComparison.OrdinalIgnoreCase))
            {
                return english ? scope + "recover an extra " + value + " HP each round for " + rounds + " rounds" : scope + "每回合额外恢复 " + value + " 点生命，持续 " + rounds + " 回合";
            }

            if (string.Equals(type, "Control", StringComparison.OrdinalIgnoreCase))
            {
                return english ? scope + "control for " + Mathf.Max(1, rounds) + " rounds" : scope + "控制 " + Mathf.Max(1, rounds) + " 回合";
            }

            if (string.Equals(type, "ArmorBreak", StringComparison.OrdinalIgnoreCase))
            {
                return english ? scope + "reduce DEF by " + value + " for " + rounds + " rounds" : scope + "降低对方 " + value + " 点防御，持续 " + rounds + " 回合";
            }

            if (string.Equals(type, "Vulnerable", StringComparison.OrdinalIgnoreCase))
            {
                return english ? scope + "increase damage taken by " + value + "% for " + rounds + " rounds" : scope + "使目标受到伤害提高 " + value + "%，持续 " + rounds + " 回合";
            }

            if (string.Equals(type, "ManaGain", StringComparison.OrdinalIgnoreCase))
            {
                return english ? scope + "gain " + value + " MP" : scope + "恢复 " + value + " 点法力";
            }

            if (string.Equals(type, "AttackUp", StringComparison.OrdinalIgnoreCase))
            {
                return english ? scope + "gain +" + value + " ATK for " + rounds + " rounds" : scope + "提高 " + value + " 点攻击，持续 " + rounds + " 回合";
            }

            if (string.Equals(type, "DefenseUp", StringComparison.OrdinalIgnoreCase))
            {
                return english ? scope + "gain +" + value + " DEF for " + rounds + " rounds" : scope + "提高 " + value + " 点防御，持续 " + rounds + " 回合";
            }

            if (string.Equals(type, "DotBoost", StringComparison.OrdinalIgnoreCase))
            {
                return english ? "damage over time +" + value + "%" : "持续伤害提高 " + value + "%";
            }

            return english ? type + " " + value : type + " " + value;
        }

        private static string GetEffectTargetLabel(string targetScope, bool english)
        {
            switch ((targetScope ?? string.Empty).Trim())
            {
                case "Self":
                    return english ? "Self " : "自身";
                case "SingleEnemy":
                    return english ? "Single enemy " : "对单体敌人";
                case "SingleAlly":
                    return english ? "Single ally " : "对单体友方";
                case "AllEnemies":
                    return english ? "All enemies " : "对全体敌人";
                case "AllAllies":
                    return english ? "All allies " : "对全体友方";
                default:
                    return english ? "Target " : "对目标";
            }
        }

        private static string WrapSkillNameWithQualityColor(string skillName, string quality)
        {
            if (string.IsNullOrEmpty(skillName))
            {
                return string.Empty;
            }

            return "<color=" + GetQualityColor(quality) + ">" + skillName + "</color>";
        }

        private static string GetQualityColor(string quality)
        {
            switch ((quality ?? string.Empty).Trim())
            {
                case "优秀":
                case "uncommon":
                    return "#63D66E";
                case "稀有":
                case "rare":
                    return "#5BA8FF";
                case "史诗":
                case "epic":
                    return "#B77CFF";
                case "绝品":
                case "legendary":
                    return "#F0D45C";
                case "普通":
                case "common":
                default:
                    return "#F2F2F2";
            }
        }

        private void RegisterObjectiveEventInternal(string eventType, string conditionParam, int amount)
        {
            var normalizedEventType = NormalizeObjectiveToken(eventType);
            if (string.IsNullOrEmpty(normalizedEventType) || amount <= 0)
            {
                return;
            }

            var normalizedParam = NormalizeObjectiveToken(conditionParam);
            for (var i = 0; i < objectiveCounters.Count; i++)
            {
                var counter = objectiveCounters[i];
                if (counter == null
                    || !string.Equals(counter.EventType, normalizedEventType, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(counter.ConditionParam, normalizedParam, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                counter.Value += amount;
                return;
            }

            objectiveCounters.Add(new ObjectiveCounterData
            {
                EventType = normalizedEventType,
                ConditionParam = normalizedParam,
                Value = amount
            });
        }

        private string BuildObjectiveSummaryInternal(bool english)
        {
            var database = ObjectiveDatabaseLoader.Load();
            if (database == null || database.Objectives == null || database.Objectives.Count == 0)
            {
                return string.Empty;
            }

            ObjectiveConfig mainObjective;
            ObjectiveConfig hintObjective;
            List<ObjectiveConfig> stageObjectives;
            GetActiveObjectives(database, out mainObjective, out stageObjectives, out hintObjective);

            var builder = new StringBuilder();
            builder.Append("<color=#E6D2A2><b>")
                .Append(LocalizationManager.GetText("objective.section_title"))
                .Append("</b></color>");

            if (mainObjective == null && stageObjectives.Count == 0 && hintObjective == null)
            {
                builder.Append('\n')
                    .Append("<color=#9E9788>")
                    .Append(LocalizationManager.GetText("objective.empty"))
                    .Append("</color>");
                return builder.ToString();
            }

            if (mainObjective != null)
            {
                builder.Append('\n')
                    .Append("<color=#CDAA6A>")
                    .Append(LocalizationManager.GetText("objective.main_label"))
                    .Append("</color>")
                    .Append("<color=#F4F0E5><b>")
                    .Append(GetObjectiveLocalizedText(mainObjective.TitleKey))
                    .Append("</b></color>")
                    .Append(BuildObjectiveProgressText(mainObjective))
                    .Append('\n')
                    .Append("<color=#A9A092>")
                    .Append(GetObjectiveLocalizedText(mainObjective.ContentKey))
                    .Append("</color>");
            }

            if (stageObjectives.Count > 0)
            {
                builder.Append('\n')
                    .Append('\n')
                    .Append("<color=#CDAA6A>")
                    .Append(LocalizationManager.GetText("objective.stage_label"))
                    .Append("</color>");
                for (var i = 0; i < stageObjectives.Count; i++)
                {
                    var objective = stageObjectives[i];
                    builder.Append('\n')
                        .Append("<color=#D8D2C6>• </color><color=#F0ECE1>")
                        .Append(GetObjectiveLocalizedText(objective.TitleKey))
                        .Append("</color>")
                        .Append(BuildObjectiveProgressText(objective));
                }
            }

            if (hintObjective != null)
            {
                builder.Append('\n')
                    .Append('\n')
                    .Append("<color=#CDAA6A>")
                    .Append(LocalizationManager.GetText("objective.hint_label"))
                    .Append("</color>")
                    .Append("<color=#BFE3A6>")
                    .Append(GetObjectiveLocalizedText(hintObjective.TitleKey))
                    .Append("</color>")
                    .Append(BuildObjectiveProgressText(hintObjective));

                var hintContent = GetObjectiveLocalizedText(hintObjective.ContentKey);
                if (!string.IsNullOrEmpty(hintContent))
                {
                    builder.Append('\n')
                        .Append("<color=#8FA882>")
                        .Append(hintContent)
                        .Append("</color>");
                }
            }

            return builder.ToString();
        }

        private void GetActiveObjectives(ObjectiveDatabase database, out ObjectiveConfig mainObjective, out List<ObjectiveConfig> stageObjectives, out ObjectiveConfig hintObjective)
        {
            mainObjective = null;
            hintObjective = null;
            stageObjectives = new List<ObjectiveConfig>();
            var predecessorMap = BuildObjectivePredecessorMap(database);

            for (var i = 0; i < database.Objectives.Count; i++)
            {
                var objective = database.Objectives[i];
                if (objective == null || !objective.Visible)
                {
                    continue;
                }

                if (!IsObjectiveUnlocked(objective, predecessorMap) || IsObjectiveCompleted(objective))
                {
                    continue;
                }

                switch (NormalizeObjectiveToken(objective.Type))
                {
                    case "main":
                        if (mainObjective == null)
                        {
                            mainObjective = objective;
                        }
                        break;
                    case "stage":
                        if (stageObjectives.Count < 3)
                        {
                            stageObjectives.Add(objective);
                        }
                        break;
                    case "hint":
                        if (hintObjective == null)
                        {
                            hintObjective = objective;
                        }
                        break;
                }
            }
        }

        private Dictionary<string, string> BuildObjectivePredecessorMap(ObjectiveDatabase database)
        {
            var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (database == null || database.Objectives == null)
            {
                return results;
            }

            for (var i = 0; i < database.Objectives.Count; i++)
            {
                var objective = database.Objectives[i];
                if (objective == null || string.IsNullOrEmpty(objective.Id) || string.IsNullOrEmpty(objective.NextId))
                {
                    continue;
                }

                results[objective.NextId.Trim()] = objective.Id.Trim();
            }

            return results;
        }

        private bool IsObjectiveUnlocked(ObjectiveConfig objective, Dictionary<string, string> predecessorMap)
        {
            if (objective == null || predecessorMap == null)
            {
                return false;
            }

            string predecessorId;
            if (!predecessorMap.TryGetValue(objective.Id, out predecessorId) || string.IsNullOrEmpty(predecessorId))
            {
                return true;
            }

            var database = ObjectiveDatabaseLoader.Load();
            var predecessor = database != null ? database.GetById(predecessorId) : null;
            return predecessor == null || IsObjectiveCompleted(predecessor);
        }

        private bool IsObjectiveCompleted(ObjectiveConfig objective)
        {
            if (objective == null)
            {
                return false;
            }

            return GetObjectiveCurrentValue(objective) >= Mathf.Max(1, objective.TargetValue);
        }

        private int GetObjectiveEventCount(string eventType, string conditionParam)
        {
            var normalizedEventType = NormalizeObjectiveToken(eventType);
            var normalizedParam = NormalizeObjectiveToken(conditionParam);
            for (var i = 0; i < objectiveCounters.Count; i++)
            {
                var counter = objectiveCounters[i];
                if (counter == null
                    || !string.Equals(counter.EventType, normalizedEventType, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(counter.ConditionParam, normalizedParam, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return Mathf.Max(0, counter.Value);
            }

            return 0;
        }

        private int GetObjectiveCurrentValue(ObjectiveConfig objective)
        {
            if (objective == null)
            {
                return 0;
            }

            var conditionType = NormalizeObjectiveToken(objective.ConditionType);
            var conditionParam = NormalizeObjectiveToken(objective.ConditionParam);
            switch (conditionType)
            {
                case "reachstage":
                    return Mathf.Max(CurrentStage, HighestClearedStage);
                case "clearstage":
                    return Mathf.Max(0, HighestClearedStage);
                case "enternodetype":
                    return GetObjectiveEventCount("EnterNodeType", conditionParam);
                case "winbattle":
                    return GetObjectiveEventCount("WinBattle", string.Empty);
                case "winnodetypebattle":
                    return GetObjectiveEventCount("WinNodeTypeBattle", conditionParam);
                case "obtainequipment":
                    return GetObjectiveEventCount("ObtainEquipment", conditionParam);
                case "gainskillreward":
                    return GetObjectiveEventCount("GainSkillReward", conditionParam);
                case "configureskill":
                    return GetObjectiveEventCount("ConfigureSkill", conditionParam);
                case "triggerstory":
                    return GetObjectiveEventCount("TriggerStory", conditionParam);
                case "equippedskillcountatleast":
                    return GetEquippedActiveSkillCount(GetPrimaryCharacterId());
                default:
                    return 0;
            }
        }

        private string BuildObjectiveProgressText(ObjectiveConfig objective)
        {
            if (objective == null)
            {
                return string.Empty;
            }

            var targetValue = Mathf.Max(1, objective.TargetValue);
            var currentValue = Mathf.Clamp(GetObjectiveCurrentValue(objective), 0, targetValue);
            return " <color=#7E776B>(" + currentValue + "/" + targetValue + ")</color>";
        }

        private static string NormalizeObjectiveToken(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }

        private static string GetObjectiveLocalizedText(string key)
        {
            return string.IsNullOrEmpty(key) ? string.Empty : LocalizationManager.GetText(key);
        }

        private int GetEquippedActiveSkillCount(string characterId)
        {
            var equippedSkillIds = GetEquippedActiveSkillIds(characterId);
            var count = 0;
            for (var i = 0; i < equippedSkillIds.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(equippedSkillIds[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private void ResetSpiritStones()
        {
            SpiritStones = 0;
            MetalSpiritStones = 0;
            WoodSpiritStones = 0;
            WaterSpiritStones = 0;
            FireSpiritStones = 0;
            EarthSpiritStones = 0;
        }

        private void AddSpiritStones(string element, int amount)
        {
            var gain = Mathf.Max(0, amount);
            switch (NormalizeSpiritStoneElement(element))
            {
                case "metal":
                    MetalSpiritStones += gain;
                    break;
                case "wood":
                    WoodSpiritStones += gain;
                    break;
                case "water":
                    WaterSpiritStones += gain;
                    break;
                case "fire":
                    FireSpiritStones += gain;
                    break;
                case "earth":
                default:
                    EarthSpiritStones += gain;
                    break;
            }

            SyncSpiritStoneTotal();
        }

        private int GetTotalSpiritStones()
        {
            SyncSpiritStoneTotal();
            return SpiritStones;
        }

        private int GetSpiritStoneCountInternal(string element)
        {
            switch (NormalizeSpiritStoneElement(element))
            {
                case "metal":
                    return MetalSpiritStones;
                case "wood":
                    return WoodSpiritStones;
                case "water":
                    return WaterSpiritStones;
                case "fire":
                    return FireSpiritStones;
                case "earth":
                default:
                    return EarthSpiritStones;
            }
        }

        private void SyncSpiritStoneTotal()
        {
            SpiritStones = MetalSpiritStones + WoodSpiritStones + WaterSpiritStones + FireSpiritStones + EarthSpiritStones;
        }

        private string BuildSpiritStoneEntry(string element, bool english, bool richText)
        {
            var content = GetSpiritStoneShortName(element, english) + ":" + GetSpiritStoneCountInternal(element);
            return richText ? WrapSpiritStoneColor(element, content) : content;
        }

        private static string GetSpiritStoneElementByStage(int stage)
        {
            var config = GetStageNodeConfig(stage);
            if (config != null && !string.IsNullOrEmpty(config.SpiritStoneElement))
            {
                return NormalizeSpiritStoneElement(config.SpiritStoneElement);
            }

            switch ((Mathf.Max(1, stage) - 1) % 5)
            {
                case 0:
                    return "Metal";
                case 1:
                    return "Wood";
                case 2:
                    return "Water";
                case 3:
                    return "Fire";
                default:
                    return "Earth";
            }
        }

        public static string GetSpiritStoneName(string element, bool english)
        {
            return GetSpiritStoneLocalizedText(element, false);
        }

        private static string GetSpiritStoneShortName(string element, bool english)
        {
            return GetSpiritStoneLocalizedText(element, true);
        }

        private static string WrapSpiritStoneColor(string element, string content)
        {
            return "<color=" + GetSpiritStoneColorHex(element) + ">" + content + "</color>";
        }

        private static string GetSpiritStoneColorHexInternal(string element)
        {
            var database = SpiritStoneDatabaseLoader.Load();
            if (database != null && database.SpiritStones != null)
            {
                var normalized = NormalizeSpiritStoneElement(element);
                for (var i = 0; i < database.SpiritStones.Count; i++)
                {
                    var config = database.SpiritStones[i];
                    if (config == null)
                    {
                        continue;
                    }

                    if (NormalizeSpiritStoneElement(config.Element) == normalized
                        && !string.IsNullOrEmpty(config.ColorHex))
                    {
                        return config.ColorHex;
                    }
                }
            }

            switch (NormalizeSpiritStoneElement(element))
            {
                case "metal":
                    return "#E5C36A";
                case "wood":
                    return "#5BC16A";
                case "water":
                    return "#59A7FF";
                case "fire":
                    return "#FF6B5D";
                case "earth":
                default:
                    return "#E6D25A";
            }
        }

        private static string GetSpiritStoneLocalizedText(string element, bool shortName)
        {
            var normalized = NormalizeSpiritStoneElement(element);
            var key = "spirit_stone." + normalized + (shortName ? ".short" : ".name");
            var localized = LocalizationManager.GetText(key);
            if (!string.IsNullOrEmpty(localized) && !string.Equals(localized, key, StringComparison.Ordinal))
            {
                return localized;
            }

            switch (normalized)
            {
                case "metal":
                    return shortName ? "\u91d1" : "\u91d1\u7075\u77f3";
                case "wood":
                    return shortName ? "\u6728" : "\u6728\u7075\u77f3";
                case "water":
                    return shortName ? "\u6c34" : "\u6c34\u7075\u77f3";
                case "fire":
                    return shortName ? "\u706b" : "\u706b\u7075\u77f3";
                case "earth":
                default:
                    return shortName ? "\u571f" : "\u571f\u7075\u77f3";
            }
        }

        private static string NormalizeSpiritStoneElement(string element)
        {
            if (string.IsNullOrEmpty(element))
            {
                return "earth";
            }

            switch (element.Trim().ToLowerInvariant())
            {
                case "\u91d1":
                case "metal":
                    return "metal";
                case "\u6728":
                case "wood":
                    return "wood";
                case "\u6c34":
                case "water":
                    return "water";
                case "\u706b":
                case "fire":
                    return "fire";
                case "\u571f":
                case "earth":
                default:
                    return "earth";
            }
        }

        private static void Shuffle<T>(List<T> list)
        {
            if (list == null)
            {
                return;
            }

            for (var i = list.Count - 1; i > 0; i--)
            {
                var swapIndex = UnityEngine.Random.Range(0, i + 1);
                var temp = list[i];
                list[i] = list[swapIndex];
                list[swapIndex] = temp;
            }
        }

        private static void EnsureInstance()
        {
            if (Instance != null)
            {
                return;
            }

            var existing = FindObjectOfType<GameProgressManager>();
            if (existing != null)
            {
                Instance = existing;
                return;
            }

            var progressObject = new GameObject("GameProgressManager");
            progressObject.AddComponent<GameProgressManager>();
        }

        private StageRandomEventState GetRandomStageEventState(int stage)
        {
            var resolvedStage = Mathf.Max(1, stage);
            for (var i = 0; i < randomStageEventStates.Count; i++)
            {
                var state = randomStageEventStates[i];
                if (state != null && state.Stage == resolvedStage)
                {
                    return state;
                }
            }

            return null;
        }

        private int ApplyExpGainModifiers(int baseValue)
        {
            return Mathf.Max(0, baseValue) + GetTimedEffectValue("CultivationGainFlat");
        }

        private int ApplySpiritStoneGainModifiers(int baseValue)
        {
            return Mathf.Max(0, baseValue) + GetTimedEffectValue("SpiritStoneGainFlat");
        }

        private int GetTimedEffectValue(string effectType)
        {
            var total = 0;
            for (var i = 0; i < activeEffects.Count; i++)
            {
                var effect = activeEffects[i];
                if (effect == null || effect.RemainingMonths <= 0)
                {
                    continue;
                }

                if (string.Equals(effect.EffectType, effectType, StringComparison.OrdinalIgnoreCase))
                {
                    total += Mathf.Max(0, effect.Value);
                }
            }

            return total;
        }

        private void TickTimedEffects(int elapsedMonths)
        {
            if (elapsedMonths <= 0 || activeEffects.Count == 0)
            {
                return;
            }

            for (var i = activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeEffects[i];
                if (effect == null)
                {
                    activeEffects.RemoveAt(i);
                    continue;
                }

                effect.RemainingMonths = Mathf.Max(0, effect.RemainingMonths - elapsedMonths);
                if (effect.RemainingMonths <= 0)
                {
                    activeEffects.RemoveAt(i);
                }
            }
        }

        private static RunEffectData CloneRunEffect(RunEffectData effect)
        {
            if (effect == null)
            {
                return null;
            }

            return new RunEffectData
            {
                EffectType = effect.EffectType,
                Value = effect.Value,
                RemainingMonths = effect.RemainingMonths,
                TitleKey = effect.TitleKey,
                DescriptionKey = effect.DescriptionKey
            };
        }

        private static string FormatEffectDescription(string descriptionKey, int remainingMonths, int value)
        {
            if (string.IsNullOrEmpty(descriptionKey))
            {
                return string.Empty;
            }

            return string.Format(LocalizationManager.GetText(descriptionKey), remainingMonths, value);
        }

        private void LoadActiveEffects()
        {
            activeEffects.Clear();
            var raw = PlayerPrefs.GetString(ActiveEffectsPrefKey, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            var wrapper = JsonUtility.FromJson<RunEffectDataListWrapper>(raw);
            if (wrapper == null || wrapper.Entries == null)
            {
                return;
            }

            for (var i = 0; i < wrapper.Entries.Count; i++)
            {
                var entry = wrapper.Entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.EffectType) || entry.Value <= 0 || entry.RemainingMonths <= 0)
                {
                    continue;
                }

                activeEffects.Add(CloneRunEffect(entry));
            }
        }

        private string SerializeActiveEffects()
        {
            var wrapper = new RunEffectDataListWrapper();
            for (var i = 0; i < activeEffects.Count; i++)
            {
                var effect = activeEffects[i];
                if (effect != null && effect.RemainingMonths > 0)
                {
                    wrapper.Entries.Add(CloneRunEffect(effect));
                }
            }

            return JsonUtility.ToJson(wrapper);
        }

        private void LoadObjectiveCounters()
        {
            objectiveCounters.Clear();
            var raw = PlayerPrefs.GetString(ObjectiveCountersPrefKey, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            var wrapper = JsonUtility.FromJson<ObjectiveCounterListWrapper>(raw);
            if (wrapper == null || wrapper.Entries == null)
            {
                return;
            }

            for (var i = 0; i < wrapper.Entries.Count; i++)
            {
                var entry = wrapper.Entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.EventType) || entry.Value <= 0)
                {
                    continue;
                }

                objectiveCounters.Add(new ObjectiveCounterData
                {
                    EventType = NormalizeObjectiveToken(entry.EventType),
                    ConditionParam = NormalizeObjectiveToken(entry.ConditionParam),
                    Value = Mathf.Max(0, entry.Value)
                });
            }
        }

        private string SerializeObjectiveCounters()
        {
            var wrapper = new ObjectiveCounterListWrapper();
            for (var i = 0; i < objectiveCounters.Count; i++)
            {
                var counter = objectiveCounters[i];
                if (counter == null || string.IsNullOrEmpty(counter.EventType) || counter.Value <= 0)
                {
                    continue;
                }

                wrapper.Entries.Add(new ObjectiveCounterData
                {
                    EventType = counter.EventType,
                    ConditionParam = counter.ConditionParam,
                    Value = counter.Value
                });
            }

            return JsonUtility.ToJson(wrapper);
        }

        private void LoadCompletedFixedEventStages()
        {
            completedFixedEventStages.Clear();
            var raw = PlayerPrefs.GetString(FixedStageEventsPrefKey, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            var wrapper = JsonUtility.FromJson<IntListWrapper>(raw);
            if (wrapper == null || wrapper.Values == null)
            {
                return;
            }

            for (var i = 0; i < wrapper.Values.Count; i++)
            {
                var stage = Mathf.Max(1, wrapper.Values[i]);
                if (!completedFixedEventStages.Contains(stage))
                {
                    completedFixedEventStages.Add(stage);
                }
            }
        }

        private string SerializeCompletedFixedEventStages()
        {
            var wrapper = new IntListWrapper();
            wrapper.Values.AddRange(completedFixedEventStages);
            return JsonUtility.ToJson(wrapper);
        }

        private void LoadRandomStageEventStates()
        {
            randomStageEventStates.Clear();
            var raw = PlayerPrefs.GetString(RandomStageEventStatesPrefKey, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            var wrapper = JsonUtility.FromJson<StageRandomEventStateListWrapper>(raw);
            if (wrapper == null || wrapper.Entries == null)
            {
                return;
            }

            for (var i = 0; i < wrapper.Entries.Count; i++)
            {
                var entry = wrapper.Entries[i];
                if (entry == null || entry.Stage <= 0)
                {
                    continue;
                }

                randomStageEventStates.Add(new StageRandomEventState
                {
                    Stage = Mathf.Max(1, entry.Stage),
                    LastTriggeredMonth = Mathf.Max(0, entry.LastTriggeredMonth)
                });
            }
        }

        private string SerializeRandomStageEventStates()
        {
            var wrapper = new StageRandomEventStateListWrapper();
            for (var i = 0; i < randomStageEventStates.Count; i++)
            {
                var entry = randomStageEventStates[i];
                if (entry != null)
                {
                    wrapper.Entries.Add(new StageRandomEventState
                    {
                        Stage = entry.Stage,
                        LastTriggeredMonth = entry.LastTriggeredMonth
                    });
                }
            }

            return JsonUtility.ToJson(wrapper);
        }

        [Serializable]
        private class CharacterRunDataListWrapper
        {
            public List<CharacterRunData> Entries = new List<CharacterRunData>();
        }

        [Serializable]
        private class RunEffectDataListWrapper
        {
            public List<RunEffectData> Entries = new List<RunEffectData>();
        }

        [Serializable]
        private class ObjectiveCounterData
        {
            public string EventType;
            public string ConditionParam;
            public int Value;
        }

        [Serializable]
        private class ObjectiveCounterListWrapper
        {
            public List<ObjectiveCounterData> Entries = new List<ObjectiveCounterData>();
        }

        [Serializable]
        private class EquipmentInstanceListWrapper
        {
            public List<EquipmentInstanceData> Entries = new List<EquipmentInstanceData>();
        }

[Serializable]
        private class IntListWrapper
        {
            public List<int> Values = new List<int>();
        }

        [Serializable]
        private class SkillRewardOptionListWrapper
        {
            public List<SkillRewardOption> Entries = new List<SkillRewardOption>();
        }

        [Serializable]
        private class StageRandomEventState
        {
            public int Stage;
            public int LastTriggeredMonth;
        }

        [Serializable]
        private class StageRandomEventStateListWrapper
        {
            public List<StageRandomEventState> Entries = new List<StageRandomEventState>();
        }
    }
}








