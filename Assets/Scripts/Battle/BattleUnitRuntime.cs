using System.Collections.Generic;
using Wuxing.Config;

namespace Wuxing.Battle
{
    public class BattleUnitRuntime
    {
        public string Id;
        public string Name;
        public string Position;
        public int MaxHP;
        public int CurrentHP;
        public int ATK;
        public int DEF;
        public int MaxMP;
        public int CurrentMP;
        public List<string> SkillIds = new List<string>();
        public List<string> EquippedItemIds = new List<string>();

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
                MaxHP = config.HP,
                CurrentHP = config.HP,
                ATK = config.ATK,
                DEF = config.DEF,
                MaxMP = config.MP,
                CurrentMP = config.MP,
                SkillIds = SplitSkills(config.InitialSkills)
            };
        }

        public static BattleUnitRuntime FromEnemy(EnemyConfig config)
        {
            return new BattleUnitRuntime
            {
                Id = config.Id,
                Name = config.Name,
                Position = config.Position,
                MaxHP = config.HP,
                CurrentHP = config.HP,
                ATK = config.ATK,
                DEF = config.DEF,
                MaxMP = config.MP,
                CurrentMP = config.MP,
                SkillIds = SplitSkills(config.Skills)
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
    }
}
