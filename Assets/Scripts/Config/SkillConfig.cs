using System;

namespace Wuxing.Config
{
    [Serializable]
    public class SkillConfig
    {
        public string Id;
        public string Name;
        public string Element;
        public string Quality;
        public string Category;
        public string TargetType;
        public int MPCost;
        public int Power;
        public int Duration;
        public string EffectType;
        public string Description;
        public string Notes;
    }
}

