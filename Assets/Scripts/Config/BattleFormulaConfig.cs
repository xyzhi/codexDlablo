using System;

namespace Wuxing.Config
{
    [Serializable]
    public class BattleFormulaConfig
    {
        public float DamageMultiplier;
        public int FlatDamageBonus;
        public float HealMultiplier;
        public float ShieldMultiplier;
        public float VulnerablePerPoint;
        public float DefenseMitigationFactor;
        public float CritMultiplier;
        public float MinDamage;
        public string Notes;
    }
}
