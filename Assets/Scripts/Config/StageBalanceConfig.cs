using System;

namespace Wuxing.Config
{
    [Serializable]
    public class StageBalanceConfig
    {
        public int Stage;
        public int EnemyHPBonus;
        public int EnemyATKBonus;
        public int EnemyDEFBonus;
        public int EnemyMPBonus;
        public int EnemyEquipmentCount;
        public int PlayerHPBonus;
        public int PlayerATKBonus;
        public int PlayerDEFBonus;
        public int PlayerMPBonus;
        public string Notes;
    }
}
