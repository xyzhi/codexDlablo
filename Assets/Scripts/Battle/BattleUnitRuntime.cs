using System;
using System.Collections.Generic;
using Wuxing.Config;

namespace Wuxing.Battle
{
    public class BattleUnitRuntime
    {
        public string Id;
        public string Name;
        public string Position;
        public string BattleElement;
        public int MaxHP;
        public int CurrentHP;
        public int ATK;
        public int DEF;
        public int MaxMP;
        public int CurrentMP;
        public int Shield;
        public List<string> SkillIds = new List<string>();
        public Dictionary<string, int> SkillLevels = new Dictionary<string, int>();
        public List<string> EquippedItemIds = new List<string>();
        public List<BattleStatusEffectRuntime> StatusEffects = new List<BattleStatusEffectRuntime>();

        public bool IsDead
        {
            get { return CurrentHP <= 0; }
        }

        public static BattleUnitRuntime FromCharacter(CharacterConfig config)
        {
            return new BattleUnitRuntime
            {
                Id = config.Id,
                Name = config.Name,
                Position = config.Position,
                BattleElement = ResolveBattleElement(config.ElementRoots),
                MaxHP = config.HP,
                CurrentHP = config.HP,
                ATK = config.ATK,
                DEF = config.DEF,
                MaxMP = config.MP,
                CurrentMP = config.MP,
                SkillIds = SplitSkills(config.InitialSkills),
                SkillLevels = CreateSkillLevelMap(config.InitialSkills)
            };
        }

        public static BattleUnitRuntime FromEnemy(EnemyConfig config)
        {
            return new BattleUnitRuntime
            {
                Id = config.Id,
                Name = config.Name,
                Position = config.Position,
                BattleElement = ResolveBattleElement(config.Element),
                MaxHP = config.HP,
                CurrentHP = config.HP,
                ATK = config.ATK,
                DEF = config.DEF,
                MaxMP = config.MP,
                CurrentMP = config.MP,
                SkillIds = SplitSkills(config.Skills),
                SkillLevels = CreateSkillLevelMap(config.Skills)
            };
        }

        public void ApplyEquipment(EquipmentConfig config)
        {
            if (config == null)
            {
                return;
            }

            MaxHP += config.HP;
            CurrentHP += config.HP;
            ATK += config.ATK;
            DEF += config.DEF;
            MaxMP += config.MP;
            CurrentMP += config.MP;
            EquippedItemIds.Add(config.Id);
        }

        public int GetSkillLevel(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return 1;
            }

            int level;
            return SkillLevels != null && SkillLevels.TryGetValue(skillId, out level) ? level : 1;
        }

        public void SetSkillLevel(string skillId, int level)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return;
            }

            if (SkillLevels == null)
            {
                SkillLevels = new Dictionary<string, int>();
            }

            SkillLevels[skillId] = level < 1 ? 1 : level;
            if (!SkillIds.Contains(skillId))
            {
                SkillIds.Add(skillId);
            }
        }

        public void ApplyShield(int amount)
        {
            Shield = Math.Max(0, Shield + Math.Max(0, amount));
        }

        public int ConsumeShield(int incomingDamage)
        {
            var damage = Math.Max(0, incomingDamage);
            if (Shield <= 0 || damage <= 0)
            {
                return damage;
            }

            var absorbed = Math.Min(Shield, damage);
            Shield -= absorbed;
            return damage - absorbed;
        }

        public int GetStatusTotalValue(string effectType)
        {
            if (StatusEffects == null || string.IsNullOrEmpty(effectType))
            {
                return 0;
            }

            var total = 0;
            for (var i = 0; i < StatusEffects.Count; i++)
            {
                var effect = StatusEffects[i];
                if (effect == null || effect.RemainingRounds <= 0)
                {
                    continue;
                }

                if (string.Equals(effect.EffectType, effectType, StringComparison.OrdinalIgnoreCase))
                {
                    total += effect.GetTotalValue();
                }
            }

            return total;
        }

        public bool ConsumeControlTurn()
        {
            if (StatusEffects == null || StatusEffects.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < StatusEffects.Count; i++)
            {
                var effect = StatusEffects[i];
                if (effect == null || effect.RemainingRounds <= 0)
                {
                    continue;
                }

                if (!string.Equals(effect.EffectType, "Control", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                effect.RemainingRounds = Math.Max(0, effect.RemainingRounds - 1);
                CleanupExpiredStatusEffects();
                return true;
            }

            return false;
        }

        public void TickPeriodicStatusEffects(string triggerTiming)
        {
            if (StatusEffects == null || StatusEffects.Count == 0)
            {
                return;
            }

            for (var i = StatusEffects.Count - 1; i >= 0; i--)
            {
                var effect = StatusEffects[i];
                if (effect == null)
                {
                    StatusEffects.RemoveAt(i);
                    continue;
                }

                if (!string.Equals(effect.TriggerTiming, triggerTiming, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                effect.RemainingRounds = Math.Max(0, effect.RemainingRounds - 1);
                if (effect.RemainingRounds <= 0)
                {
                    StatusEffects.RemoveAt(i);
                }
            }
        }

        public void TickDurationStatusEffects()
        {
            if (StatusEffects == null || StatusEffects.Count == 0)
            {
                return;
            }

            for (var i = StatusEffects.Count - 1; i >= 0; i--)
            {
                var effect = StatusEffects[i];
                if (effect == null)
                {
                    StatusEffects.RemoveAt(i);
                    continue;
                }

                if (string.Equals(effect.EffectType, "Control", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(effect.TriggerTiming, "TurnStart", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(effect.TriggerTiming, "TurnEnd", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(effect.TriggerTiming, "Passive", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                effect.RemainingRounds = Math.Max(0, effect.RemainingRounds - 1);
                if (effect.RemainingRounds <= 0)
                {
                    StatusEffects.RemoveAt(i);
                }
            }
        }

        public void AddStatusEffect(BattleStatusEffectRuntime effect)
        {
            if (effect == null)
            {
                return;
            }

            if (StatusEffects == null)
            {
                StatusEffects = new List<BattleStatusEffectRuntime>();
            }

            var stackRule = string.IsNullOrEmpty(effect.StackRule) ? "Refresh" : effect.StackRule;
            var maxStacks = Math.Max(1, effect.MaxStacks);

            for (var i = 0; i < StatusEffects.Count; i++)
            {
                var existing = StatusEffects[i];
                if (existing == null)
                {
                    continue;
                }

                if (!string.Equals(existing.EffectType, effect.EffectType, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(existing.TriggerTiming, effect.TriggerTiming, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(stackRule, "Stack", StringComparison.OrdinalIgnoreCase))
                {
                    existing.StackCount = Math.Min(maxStacks, Math.Max(1, existing.StackCount) + 1);
                    existing.Value = Math.Max(existing.Value, effect.Value);
                    existing.RemainingRounds = Math.Max(existing.RemainingRounds, effect.RemainingRounds);
                    existing.MaxStacks = maxStacks;
                    return;
                }

                existing.Value = Math.Max(existing.Value, effect.Value);
                existing.RemainingRounds = Math.Max(existing.RemainingRounds, effect.RemainingRounds);
                existing.StackCount = 1;
                existing.MaxStacks = maxStacks;
                existing.StackRule = stackRule;
                existing.SourceSkillId = effect.SourceSkillId;
                return;
            }

            effect.StackRule = stackRule;
            effect.MaxStacks = maxStacks;
            effect.StackCount = Math.Max(1, effect.StackCount);
            StatusEffects.Add(effect);
        }

        public void CleanupExpiredStatusEffects()
        {
            if (StatusEffects == null || StatusEffects.Count == 0)
            {
                return;
            }

            for (var i = StatusEffects.Count - 1; i >= 0; i--)
            {
                var effect = StatusEffects[i];
                if (effect == null || effect.RemainingRounds <= 0)
                {
                    StatusEffects.RemoveAt(i);
                }
            }
        }

        private static string ResolveBattleElement(string rawElement)
        {
            if (string.IsNullOrWhiteSpace(rawElement))
            {
                return "Earth";
            }

            var primary = rawElement.Split('|')[0].Trim();
            if (string.Equals(primary, "圣", StringComparison.OrdinalIgnoreCase) || string.Equals(primary, "holy", StringComparison.OrdinalIgnoreCase))
            {
                return "Holy";
            }
            if (string.Equals(primary, "金", StringComparison.OrdinalIgnoreCase) || string.Equals(primary, "metal", StringComparison.OrdinalIgnoreCase))
            {
                return "Metal";
            }
            if (string.Equals(primary, "木", StringComparison.OrdinalIgnoreCase) || string.Equals(primary, "wood", StringComparison.OrdinalIgnoreCase))
            {
                return "Wood";
            }
            if (string.Equals(primary, "水", StringComparison.OrdinalIgnoreCase) || string.Equals(primary, "water", StringComparison.OrdinalIgnoreCase))
            {
                return "Water";
            }
            if (string.Equals(primary, "火", StringComparison.OrdinalIgnoreCase) || string.Equals(primary, "fire", StringComparison.OrdinalIgnoreCase))
            {
                return "Fire";
            }
            return "Earth";
        }

        private static List<string> SplitSkills(string rawSkills)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(rawSkills))
            {
                return result;
            }

            var parts = rawSkills.Split('|');
            for (var i = 0; i < parts.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(parts[i]))
                {
                    result.Add(parts[i].Trim());
                }
            }

            return result;
        }

        private static Dictionary<string, int> CreateSkillLevelMap(string rawSkills)
        {
            var result = new Dictionary<string, int>();
            var skillIds = SplitSkills(rawSkills);
            for (var i = 0; i < skillIds.Count; i++)
            {
                result[skillIds[i]] = 1;
            }

            return result;
        }
    }
}
