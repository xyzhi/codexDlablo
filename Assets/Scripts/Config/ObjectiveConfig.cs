using System;

namespace Wuxing.Config
{
    [Serializable]
    public class ObjectiveConfig
    {
        public string Id;
        public string Group;
        public string Type;
        public string TitleKey;
        public string ContentKey;
        public string ConditionType;
        public string ConditionParam;
        public int TargetValue;
        public string NextId;
        public bool AutoTrack;
        public bool Visible;
        public string Notes;
    }
}
