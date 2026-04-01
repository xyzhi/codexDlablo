using System;

namespace Wuxing.Config
{
    [Serializable]
    public class StageNodeConfig
    {
        public int Stage;
        public string NodeType;
        public string EventProfile;
        public string SpiritStoneElement;
        public string ThemeZh;
        public string ThemeEn;
        public string DetailZh;
        public string DetailEn;
        public string EventMode;
        public int CooldownMonths;
        public int BattleExpReward;
        public int BattleSpiritStoneReward;
        public int NonBattleExpReward;
        public int NonBattleSpiritStoneReward;
        public string Notes;
    }
}
