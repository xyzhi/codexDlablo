using System;
using System.Collections.Generic;
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
            "C001", "C002"
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
                Instance.SpiritStones = 0;
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
            Instance.SpiritStones = 0;
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

            Instance.CultivationExp += reward.ExpGained;
            Instance.SpiritStones += reward.SpiritStonesGained;

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
            return Instance != null ? Instance.SpiritStones : 0;
        }

        public static int GetRequiredExpForNextLevel()
        {
            EnsureInstance();
            return Instance != null ? Instance.GetRequiredExpInternal() : 60;
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

        public static void PrepareSkillRewardOptions()
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

            var candidates = new List<SkillRewardOption>();
            for (var i = 0; i < BasePlayerCharacterIds.Length; i++)
            {
                var characterId = BasePlayerCharacterIds[i];
                var character = characterDatabase.GetById(characterId);
                if (character == null)
                {
                    continue;
                }

                var knownSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                CollectKnownSkillIds(character.InitialSkills, knownSkills);
                var learnedSkills = Instance.GetLearnedSkillIdsInternal(characterId);
                for (var j = 0; j < learnedSkills.Count; j++)
                {
                    knownSkills.Add(learnedSkills[j]);
                }

                for (var j = 0; j < skillDatabase.Skills.Count; j++)
                {
                    var skill = skillDatabase.Skills[j];
                    if (skill == null || string.IsNullOrEmpty(skill.Id) || knownSkills.Contains(skill.Id) || IsPassiveSkillReward(skill))
                    {
                        continue;
                    }

                    candidates.Add(new SkillRewardOption
                    {
                        CharacterId = character.Id,
                        CharacterName = character.Name,
                        SkillId = skill.Id,
                        SkillName = skill.Name,
                        SkillElement = skill.Element,
                        SkillDescription = skill.Description
                    });
                }
            }

            Shuffle(candidates);
            for (var i = 0; i < candidates.Count && Instance.pendingSkillRewards.Count < 3; i++)
            {
                Instance.pendingSkillRewards.Add(candidates[i]);
            }

            Instance.SaveProgress();
            ProgressChanged?.Invoke();
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
            if (!entry.LearnedSkillIds.Exists(delegate(string skillId)
            {
                return string.Equals(skillId, option.SkillId, StringComparison.OrdinalIgnoreCase);
            }))
            {
                entry.LearnedSkillIds.Add(option.SkillId);
            }

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
            return MaxStage;
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
            var frontierStage = Mathf.Clamp(Instance.HighestClearedStage + 1, 1, MaxStage);
            return Mathf.Clamp(Mathf.Max(currentStage, frontierStage), 1, MaxStage);
        }

        public static bool CanTravelToStage(int stage)
        {
            return stage >= 1 && stage <= GetMaxReachableStage();
        }

        public static int GetRemainingMonths()
        {
            return Mathf.Max(0, LifetimeMonths - GetElapsedMonths());
        }

        public static MapNodeType GetNodeType(int stage)
        {
            if (stage <= 0)
            {
                return MapNodeType.Battle;
            }

            if (stage == 1)
            {
                return MapNodeType.Rest;
            }

            switch (stage)
            {
                case 3:
                case 6:
                    return MapNodeType.Elite;
                case 4:
                case 7:
                    return MapNodeType.Rest;
                case 8:
                    return MapNodeType.Boss;
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

            if (Instance.CurrentStage >= MaxStage)
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

            var resolvedStage = Mathf.Clamp(stage, 0, MaxStage);
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
            var resolvedStage = Mathf.Max(1, stage);
            if (resolvedStage <= 2)
            {
                return english ? "Outer Hills" : "山门外围";
            }

            if (resolvedStage <= 4)
            {
                return english ? "Burning Trail" : "焚风古道";
            }

            if (resolvedStage <= 6)
            {
                return english ? "Deep Ruins" : "地宫遗迹";
            }

            return english ? "Ascension Path" : "登天阶";
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
            var nodeType = GetNodeType(resolvedStage);

            if (resolvedStage == 1)
            {
                return english
                    ? "Safe starting area reserved for future features."
                    : "当前作为安全起点，后续会加入更多功能。";
            }

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
                    "\nSpirit Stones: " + GetSpiritStones();

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
                "\n灵石：" + GetSpiritStones();

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
            PlayerPrefs.SetInt(SpiritStonesPrefKey, SpiritStones);
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
                    return characterRunData[i];
                }
            }

            var created = new CharacterRunData
            {
                CharacterId = characterId
            };
            characterRunData.Add(created);
            return created;
        }

        private List<string> GetLearnedSkillIdsInternal(string characterId)
        {
            var entry = GetOrCreateCharacterRunData(characterId);
            return new List<string>(entry.LearnedSkillIds);
        }

        private RunData BuildRunDataSnapshot()
        {
            var data = new RunData
            {
                CurrentStage = CurrentStage,
                HighestClearedStage = HighestClearedStage,
                ElapsedMonths = ElapsedMonths,
                RemainingMonths = Mathf.Max(0, LifetimeMonths - ElapsedMonths),
                CultivationLevel = CultivationLevel,
                CultivationExp = CultivationExp,
                SpiritStones = SpiritStones,
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
                    LearnedSkillIds = new List<string>(entry.LearnedSkillIds)
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

