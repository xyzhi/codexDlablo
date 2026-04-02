using System;

namespace Wuxing.Config
{
    [Serializable]
    public class SkillEffectConfig
    {
        public int EffectIndex;
        public string EffectType;
        public string TargetScope;
        public int Value;
        public int ValuePerLevel;
        public int DurationRounds;
        public int MaxStacks;
        public string StackRule;
        public string TriggerTiming;
        public string Notes;
    }
}
