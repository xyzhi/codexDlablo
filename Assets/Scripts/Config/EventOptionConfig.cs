using System;

namespace Wuxing.Config
{
    [Serializable]
    public class EventOptionConfig
    {
        public string Profile;
        public int OptionIndex;
        public string TitleKey;
        public string RewardMode;
        public string UtilityAction;
        public string SpiritStoneElement;
        public int ExpBase;
        public int ExpPerStage;
        public int SpiritStoneBase;
        public int SpiritStonePerStage;
        public int SpiritStoneCostBase;
        public int SpiritStoneCostPerStage;
        public string SkillRewardNodeType;
        public string SelectionTitleKey;
        public string SelectionMessageKey;
        public string ResultTitleKey;
        public string ResultIntroKey;
        public string EmptyResultKey;
        public string BuffType;
        public int BuffValueBase;
        public int BuffValuePerStage;
        public int BuffDurationMonths;
        public string BuffTitleKey;
        public string BuffDescriptionKey;
        public string Notes;
    }
}
