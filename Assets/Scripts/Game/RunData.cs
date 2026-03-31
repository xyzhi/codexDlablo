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
        public bool HasLastBattle;
        public int LastBattleStage;
        public int LastBattleRounds;
        public bool LastBattleVictory;
        public List<string> OwnedEquipmentIds = new List<string>();
        public List<CharacterRunData> Characters = new List<CharacterRunData>();
        public List<SkillRewardOption> PendingSkillRewards = new List<SkillRewardOption>();
    }

    [Serializable]
    public class CharacterRunData
    {
        public string CharacterId;
        public List<string> LearnedSkillIds = new List<string>();
    }
}
