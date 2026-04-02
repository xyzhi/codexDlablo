using System;

namespace Wuxing.Config
{
    [Serializable]
    public class BattleFormulaConfig
    {
        public float DamageMultiplier;
        public int FlatDamageBonus;
        public float VulnerablePerPoint;
        public float DefenseMitigationFactor;
        public float MinDamage;
        public string Notes;
    }
}
