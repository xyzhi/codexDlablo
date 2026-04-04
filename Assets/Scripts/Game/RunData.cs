using System;
using System.Collections.Generic;

namespace Wuxing.Game
{
    [Serializable]
    public class RunData
    {
        public int CurrentStage;
        public int HighestClearedStage;
        public int ElapsedMonths;
        public int RemainingMonths;
        public int CultivationLevel;
        public int CultivationExp;
        public int SpiritStones;
        public int MetalSpiritStones;
        public int WoodSpiritStones;
        public int WaterSpiritStones;
        public int FireSpiritStones;
        public int EarthSpiritStones;
        public bool HasLastBattle;
        public int LastBattleStage;
        public int LastBattleRounds;
        public bool LastBattleVictory;
        public List<EquipmentInstanceData> OwnedEquipments = new List<EquipmentInstanceData>();
        public List<CharacterRunData> Characters = new List<CharacterRunData>();
        public List<SkillRewardOption> PendingSkillRewards = new List<SkillRewardOption>();
        public List<RunEffectData> ActiveEffects = new List<RunEffectData>();
    }

    [Serializable]
    public class CharacterRunData
    {
        public string CharacterId;
        public List<string> LearnedSkillIds = new List<string>();
        public List<SkillLevelData> SkillLevels = new List<SkillLevelData>();
    }

    [Serializable]
    public class SkillLevelData
    {
        public string SkillId;
        public int Level;
    }

    [Serializable]
    public class RunEffectData
    {
        public string EffectType;
        public int Value;
        public int RemainingMonths;
        public string TitleKey;
        public string DescriptionKey;
    }
}
