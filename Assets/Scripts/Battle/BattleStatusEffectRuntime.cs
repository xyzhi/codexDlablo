using System;

namespace Wuxing.Battle
{
    [Serializable]
    public class BattleStatusEffectRuntime
    {
        public string EffectType;
        public int Value;
        public int RemainingRounds;
        public int MaxStacks;
        public int StackCount;
        public string StackRule;
        public string TriggerTiming;
        public string SourceSkillId;

        public int GetTotalValue()
        {
            return Value * Math.Max(1, StackCount);
        }
    }
}
