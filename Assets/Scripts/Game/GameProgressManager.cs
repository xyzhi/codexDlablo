using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Wuxing.Config;

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
        private const string RunDataSnapshotPrefKey = "game.progress.run_data_snapshot";
        private const int MaxStage = 8;
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
            reward.ExpGained = 18 + resolvedStage * 8;
            reward.SpiritStonesGained = 12 + resolvedStage * 5;
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
            switch (nodeType)
            {
                case MapNodeType.Rest:
                    reward.ExpGained = resolvedStage == 1 ? 12 : 10 + resolvedStage * 4;
                    reward.SpiritStonesGained = resolvedStage == 1 ? 10 : 6 + resolvedStage * 3;
                    break;
                default:
                    reward.ExpGained = 6 + resolvedStage * 2;
                    reward.SpiritStonesGained = 4 + resolvedStage * 2;
                    break;
            }

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

        public static string BuildSpiritStoneSummary(bool english, bool richText = false)
        {
            EnsureInstance();
            if (Instance == null)
            {
                return english ? "Spirit Stones: 0" : "灵石：0";
            }

            var parts = new List<string>
            {
                Instance.BuildSpiritStoneEntry("Metal", english, richText),
                Instance.BuildSpiritStoneEntry("Wood", english, richText),
                Instance.BuildSpiritStoneEntry("Water", english, richText),
                Instance.BuildSpiritStoneEntry("Fire", english, richText),
                Instance.BuildSpiritStoneEntry("Earth", english, richText)
            };

            return (english ? "Spirit Stones: " : "灵石：") + string.Join("  ", parts.ToArray());
        }

        public static string BuildSpiritStoneGainText(BattleRewardResult reward, bool english, bool richText = false)
        {
            if (reward == null || reward.SpiritStonesGained <= 0)
            {
                return english ? "Spirit Stones +0" : "灵石 +0";
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
                return english ? "Learned skills data is unavailable." : "当前无法读取已学功法数据。";
            }

            var builder = new StringBuilder();
            builder.Append(english ? "Current Run Skills" : "当前局功法总览");

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
                    .Append(english ? "Character: " : "角色：")
                    .Append(character.Name)
                    .Append('\n')
                    .Append(english ? "Current Skills: " : "当前可用：")
                    .Append(currentSkillNames.Count > 0
                        ? string.Join(english ? ", " : "、", currentSkillNames.ToArray())
                        : (english ? "None" : "无"))
                    .Append('\n')
                    .Append(english ? "New This Run: " : "本局新增：")
                    .Append(learnedSkillNames.Count > 0
                        ? string.Join(english ? ", " : "、", learnedSkillNames.ToArray())
                        : (english ? "None" : "暂无"));
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

        private static MapNodeType ParseNodeType(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue))
            {
                return MapNodeType.Battle;
            }

            switch (rawValue.Trim().ToLowerInvariant())
            {
                case "elite":
                case "精英":
                    return MapNodeType.Elite;
                case "rest":
                case "休整":
                    return MapNodeType.Rest;
                case "boss":
                case "首领":
                    return MapNodeType.Boss;
                case "battle":
                case "战斗":
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

            return english ? "Outer Hills" : "山门外围";
        }

        public static string BuildCurrentObjective(bool english)
        {
            var currentStage = GetCurrentStage();
            if (currentStage <= 0)
            {
                return english
                    ? "Start a new run from Stage 1."
                    : "从第 1 关开始新的一轮。";
            }

            return english
                ? "Clear or farm Stage " + currentStage + " before moving on."
                : "可反复挑战第 " + currentStage + " 关，刷成长后再推进。";
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

            var nodeType = GetNodeType(resolvedStage);
            if (english)
            {
                switch (nodeType)
                {
                    case MapNodeType.Elite:
                        return "Elite battle with higher growth and better loot.";
                    case MapNodeType.Rest:
                        return "Rest stop for stable progress and time management.";
                    case MapNodeType.Boss:
                        return "Boss battle. Winning can finish the current route.";
                    case MapNodeType.Battle:
                    default:
                        return "Standard battle for growth, spirit stones, and equipment drops.";
                }
            }

            switch (nodeType)
            {
                case MapNodeType.Elite:
                    return "精英战斗，成长更多，掉落也更好。";
                case MapNodeType.Rest:
                    return "休整节点，适合稳定推进，缓和本轮节奏。";
                case MapNodeType.Boss:
                    return "首领战斗，若能取胜，本轮路线直接完成。";
                case MapNodeType.Battle:
                default:
                    return "普通战斗，可获得修为、灵石与装备掉落。";
            }
        }

        public static string BuildLastBattleSummary(bool english)
        {
            if (GetLastBattleStage() <= 0)
            {
                return english ? "No battle record yet." : "还没有战斗记录。";
            }

            if (english)
            {
                return "Last battle: Stage " + GetLastBattleStage()
                    + ", " + (GetLastBattleVictory() ? "Victory" : "Defeat")
                    + ", " + GetLastBattleRounds() + " rounds.";
            }

            return "上一场：第 " + GetLastBattleStage()
                + " 关，" + (GetLastBattleVictory() ? "胜利" : "失败")
                + "，" + GetLastBattleRounds() + " 回合。";
        }

        public static string BuildPostBattleNextStep(bool english, bool lastBattleVictory)
        {
            if (lastBattleVictory)
            {
                var nextStage = Mathf.Max(1, GetCurrentStage() + 1);
                return english
                    ? "Next: return to the map, repeat this stage, or move on toward Stage " + nextStage + "."
                    : "下一步：返回地图后，可继续挑战本关，或前往第 " + nextStage + " 关。";
            }

            var retryStage = Mathf.Max(1, GetCurrentStage());
            return english
                ? "Next: adjust equipment and retry Stage " + retryStage + "."
                : "下一步：调整装备后重试第 " + retryStage + " 关。";
        }

        public static string BuildProgressSummary(bool english)
        {
            EnsureInstance();
            var currentStage = GetCurrentStage();
            var highestCleared = GetHighestClearedStage();

            if (english)
            {
                var builder =
                    "Current Stage " + currentStage + " / Highest Cleared " + highestCleared +
                    "\nRun State: " + (HasActiveRun() ? "In Progress" : "Idle") +
                    "\nNext Step: " + (HasActiveRun() ? ("Continue Stage " + currentStage) : "Start From Stage 1") +
                    "\nCultivation Lv." + GetCultivationLevel() +
                    " / Exp " + GetCultivationExp() + "/" + GetRequiredExpForNextLevel() +
                    "\n" + BuildSpiritStoneSummary(true);

                if (Instance != null && Instance.HasLastBattle)
                {
                    builder += "\nLast Battle: Stage " + Instance.LastBattleStage
                        + " / " + (Instance.LastBattleVictory ? "Victory" : "Defeat")
                        + " / Rounds " + Instance.LastBattleRounds;
                }

                return builder;
            }

            var summary =
                "当前关卡 " + currentStage + " / 最高通关 " + highestCleared +
                "\n本轮状态：" + (HasActiveRun() ? "进行中" : "待开始") +
                "\n下一步：" + (HasActiveRun() ? ("继续第 " + currentStage + " 关") : "从第 1 关开始") +
                "\n修为等级 " + GetCultivationLevel() +
                " / 经验 " + GetCultivationExp() + "/" + GetRequiredExpForNextLevel() +
                "\n" + BuildSpiritStoneSummary(false);

            if (Instance != null && Instance.HasLastBattle)
            {
                summary += "\n上一场：第 " + Instance.LastBattleStage
                    + " 关 / " + (Instance.LastBattleVictory ? "胜利" : "失败")
                    + " / " + Instance.LastBattleRounds + " 回合";
            }

            return summary;
        }

        public static string BuildLongevitySummary(bool english)
        {
            var remainingMonths = GetRemainingMonths();
            var years = remainingMonths / 12;
            var months = remainingMonths % 12;

            if (english)
            {
                return "Lifespan: " + years + "y " + months + "m remaining";
            }

            if (remainingMonths <= 0)
            {
                return "阳寿：已尽";
            }

            if (months == 0)
            {
                return "阳寿：余" + ToChineseNumber(years) + "载整";
            }

            return "阳寿：余" + ToChineseNumber(years) + "载" + ToChineseNumber(months) + "月";
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

                string state;
                if (stage < currentStage)
                {
                    state = english ? "[Done]" : "[已过]";
                }
                else if (stage == currentStage)
                {
                    state = english ? "[Now]" : "[当前]";
                }
                else if (stage <= maxReachable)
                {
                    state = english ? "[Open]" : "[可达]";
                }
                else
                {
                    state = english ? "[Locked]" : "[未开]";
                }

                builder.Append(state)
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
                return english ? "Village" : "新手村";
            }

            switch (GetNodeType(stage))
            {
                case MapNodeType.Elite:
                    return english ? "Elite" : "精英";
                case MapNodeType.Rest:
                    return english ? "Rest" : "休整";
                case MapNodeType.Boss:
                    return english ? "Boss" : "首领";
                case MapNodeType.Battle:
                default:
                    return english ? "Battle" : "战斗";
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
            var digits = new[] { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
            if (value < 10)
            {
                return digits[Mathf.Clamp(value, 0, 9)];
            }

            if (value < 20)
            {
                return value == 10 ? "十" : "十" + digits[value % 10];
            }

            if (value < 100)
            {
                var tens = value / 10;
                var units = value % 10;
                return units == 0
                    ? digits[tens] + "十"
                    : digits[tens] + "十" + digits[units];
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
                || string.Equals(skill.Category, "被动", StringComparison.OrdinalIgnoreCase)
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
                case "绝品":
                case "epic":
                case "legendary":
                    return "epic";
                case "稀有":
                case "rare":
                    return "rare";
                case "普通":
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
            return GetSkillDisplayName(skillDatabase, skillId) + " Lv." + GetSkillLevel(characterId, skillId);
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
            switch (NormalizeSpiritStoneElement(element))
            {
                case "metal":
                    return english ? "Metal Spirit Stone" : "金灵石";
                case "wood":
                    return english ? "Wood Spirit Stone" : "木灵石";
                case "water":
                    return english ? "Water Spirit Stone" : "水灵石";
                case "fire":
                    return english ? "Fire Spirit Stone" : "火灵石";
                case "earth":
                default:
                    return english ? "Earth Spirit Stone" : "土灵石";
            }
        }

        private static string GetSpiritStoneShortName(string element, bool english)
        {
            switch (NormalizeSpiritStoneElement(element))
            {
                case "metal":
                    return english ? "Metal" : "金";
                case "wood":
                    return english ? "Wood" : "木";
                case "water":
                    return english ? "Water" : "水";
                case "fire":
                    return english ? "Fire" : "火";
                case "earth":
                default:
                    return english ? "Earth" : "土";
            }
        }

        private static string WrapSpiritStoneColor(string element, string content)
        {
            return "<color=" + GetSpiritStoneColorHex(element) + ">" + content + "</color>";
        }

        private static string GetSpiritStoneColorHex(string element)
        {
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

        private static string NormalizeSpiritStoneElement(string element)
        {
            if (string.IsNullOrEmpty(element))
            {
                return "earth";
            }

            switch (element.Trim().ToLowerInvariant())
            {
                case "金":
                case "metal":
                    return "metal";
                case "木":
                case "wood":
                    return "wood";
                case "水":
                case "water":
                    return "water";
                case "火":
                case "fire":
                    return "fire";
                case "土":
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

        [Serializable]
        private class CharacterRunDataListWrapper
        {
            public List<CharacterRunData> Entries = new List<CharacterRunData>();
        }

        [Serializable]
        private class SkillRewardOptionListWrapper
        {
            public List<SkillRewardOption> Entries = new List<SkillRewardOption>();
        }
    }
}

