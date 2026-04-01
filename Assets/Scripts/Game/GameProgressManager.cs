using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Wuxing.Config;
using Wuxing.Localization;

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
        private const string RunDataSnapshotPrefKey = "game.progress.run_data_snapshot";
        private const int MaxStage = 100;
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

        private readonly List<string> ownedEquipmentIds = new List<string>();
        private readonly List<CharacterRunData> characterRunData = new List<CharacterRunData>();
        private readonly List<SkillRewardOption> pendingSkillRewards = new List<SkillRewardOption>();
        private readonly List<int> completedFixedEventStages = new List<int>();
        private readonly List<StageRandomEventState> randomStageEventStates = new List<StageRandomEventState>();

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
            reward.ExpGained = GetConfiguredBattleExpReward(resolvedStage);
            reward.SpiritStonesGained = GetConfiguredBattleSpiritStoneReward(resolvedStage);
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
                    Instance.ownedEquipmentIds.Add(drop.Id);
                    reward.DroppedEquipmentId = drop.Id;
                    reward.DroppedEquipmentName = drop.Name;
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
            reward.ExpGained = GetConfiguredNonBattleExpReward(resolvedStage, nodeType);
            reward.SpiritStonesGained = GetConfiguredNonBattleSpiritStoneReward(resolvedStage, nodeType);
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

            reward.ExpGained = Mathf.Max(0, expGained);
            reward.SpiritStonesGained = Mathf.Max(0, spiritStoneCount);
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

            if (!Instance.ownedEquipmentIds.Exists(id => string.Equals(id, equipmentId, StringComparison.OrdinalIgnoreCase)))
            {
                Instance.ownedEquipmentIds.Add(equipmentId);
                Instance.SaveProgress();
                ProgressChanged?.Invoke();
            }
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
            return Instance != null && Instance.ownedEquipmentIds.Exists(delegate(string id)
            {
                return string.Equals(id, equipmentId, StringComparison.OrdinalIgnoreCase);
            });
        }

        public static List<string> GetOwnedEquipmentIds()
        {
            EnsureInstance();
            if (Instance == null)
            {
                return new List<string>();
            }

            var results = new List<string>(Instance.ownedEquipmentIds);
            results.Sort(StringComparer.OrdinalIgnoreCase);
            return results;
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

                AppendSkillNames(character.Id, character.InitialSkills, currentSkillNames, knownSkillIds, skillDatabase);
                for (var j = 0; j < learnedSkillIds.Count; j++)
                {
                    learnedSkillNames.Add(GetSkillLabel(skillDatabase, character.Id, learnedSkillIds[j]));
                    if (knownSkillIds.Add(learnedSkillIds[j]))
                    {
                        currentSkillNames.Add(GetSkillLabel(skillDatabase, character.Id, learnedSkillIds[j]));
                    }
                }

                builder.Append("\n\n")
                    .Append(LocalizationManager.GetText("progress.skills_character"))
                    .Append(character.Name)
                    .Append('\n')
                    .Append(LocalizationManager.GetText("progress.skills_current"))
                    .Append(currentSkillNames.Count > 0
                        ? string.Join(LocalizationManager.GetText("common.separator_names"), currentSkillNames.ToArray())
                        : LocalizationManager.GetText("progress.skills_none"))
                    .Append('\n')
                    .Append(LocalizationManager.GetText("progress.skills_new"))
                    .Append(learnedSkillNames.Count > 0
                        ? string.Join(LocalizationManager.GetText("common.separator_names"), learnedSkillNames.ToArray())
                        : LocalizationManager.GetText("progress.skills_none"));
            }

            return builder.ToString();
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
            reward.ExpGained = Mathf.Max(0, expGained);
            reward.SpiritStoneElement = string.IsNullOrEmpty(spiritStoneElement)
                ? GetSpiritStoneElementByStage(resolvedStage)
                : spiritStoneElement;
            reward.SpiritStoneName = GetSpiritStoneName(reward.SpiritStoneElement, false);
            reward.SpiritStonesGained = Mathf.Max(0, spiritStoneCount);

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
                    Instance.ownedEquipmentIds.Add(drop.Id);
                    reward.DroppedEquipmentId = drop.Id;
                    reward.DroppedEquipmentName = drop.Name;
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
            option.CurrentLevel = previousLevel;
            option.ResultLevel = nextLevel;
            option.IsUpgrade = alreadyKnown;

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
            return database != null && database.StageNodes != null && database.StageNodes.Count > 0
                ? database.StageNodes.Count
                : MaxStage;
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

            Instance.ElapsedMonths += Mathf.Max(1, months);
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
            var currentStage = GetCurrentStage();
            if (currentStage <= 0)
            {
                return LocalizationManager.GetText("progress.current_objective_start");
            }

            return string.Format(LocalizationManager.GetText("progress.current_objective_stage"), currentStage);
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
            PlayerPrefs.SetString(OwnedEquipmentPrefKey, SerializeOwnedEquipment());
            PlayerPrefs.SetString(LearnedSkillsPrefKey, SerializeLearnedSkills());
            PlayerPrefs.SetString(PendingSkillRewardsPrefKey, SerializePendingSkillRewards());
            PlayerPrefs.SetString(FixedStageEventsPrefKey, SerializeCompletedFixedEventStages());
            PlayerPrefs.SetString(RandomStageEventStatesPrefKey, SerializeRandomStageEventStates());
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
            for (var i = 0; i < equipmentDatabase.Equipments.Count; i++)
            {
                var equipment = equipmentDatabase.Equipments[i];
                if (equipment == null)
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

        private void ResetOwnedEquipmentToBase()
        {
            ownedEquipmentIds.Clear();
            for (var i = 0; i < BaseOwnedEquipmentIds.Length; i++)
            {
                ownedEquipmentIds.Add(BaseOwnedEquipmentIds[i]);
            }
        }

        private void LoadOwnedEquipment()
        {
            ResetOwnedEquipmentToBase();
            var raw = PlayerPrefs.GetString(OwnedEquipmentPrefKey, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            var parts = raw.Split('|');
            for (var i = 0; i < parts.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(parts[i]))
                {
                    ownedEquipmentIds.Add(parts[i]);
                }
            }
        }

        private string SerializeOwnedEquipment()
        {
            var ordered = new List<string>(ownedEquipmentIds);
            ordered.Sort(StringComparer.OrdinalIgnoreCase);
            return string.Join("|", ordered.ToArray());
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

                if (entry.SkillLevels == null)
                {
                    entry.SkillLevels = new List<SkillLevelData>();
                }

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
                    if (characterRunData[i].SkillLevels == null)
                    {
                        characterRunData[i].SkillLevels = new List<SkillLevelData>();
                    }
                    return characterRunData[i];
                }
            }

            var created = new CharacterRunData
            {
                CharacterId = characterId,
                SkillLevels = new List<SkillLevelData>()
            };
            characterRunData.Add(created);
            return created;
        }

        private List<string> GetLearnedSkillIdsInternal(string characterId)
        {
            var entry = GetOrCreateCharacterRunData(characterId);
            return new List<string>(entry.LearnedSkillIds);
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

            data.OwnedEquipmentIds.AddRange(ownedEquipmentIds);
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

            return string.Equals(skill.Category, "Passive", StringComparison.OrdinalIgnoreCase)
                || string.Equals(skill.Category, "\u88ab\u52a8", StringComparison.OrdinalIgnoreCase)
                || string.Equals(skill.EffectType, "DotBoost", StringComparison.OrdinalIgnoreCase);
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

        private static void AppendSkillNames(string characterId, string rawSkills, List<string> targetNames, HashSet<string> knownSkillIds, SkillDatabase skillDatabase)
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
                targetNames.Add(GetSkillLabel(skillDatabase, characterId, skillId));
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

        private static string GetSkillLabel(SkillDatabase skillDatabase, string characterId, string skillId)
        {
            return GetSkillDisplayName(skillDatabase, skillId)
                + " "
                + LocalizationManager.GetText("common.level_prefix")
                + GetSkillLevel(characterId, skillId);
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



