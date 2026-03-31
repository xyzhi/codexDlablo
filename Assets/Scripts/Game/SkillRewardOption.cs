using System;

namespace Wuxing.Game
{
    [Serializable]
    public class SkillRewardOption
    {
        public string CharacterId;
        public string CharacterName;
        public string SkillId;
        public string SkillName;
        public string SkillElement;
        public string SkillQuality;
        public string SkillDescription;
        public int CurrentLevel;
        public int ResultLevel;
        public bool IsUpgrade;
    }
}
