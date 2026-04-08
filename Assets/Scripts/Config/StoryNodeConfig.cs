using System;

namespace Wuxing.Config
{
    [Serializable]
    public class StoryNodeConfig
    {
        public string Id;
        public string Type;
        public string SpeakerKey;
        public string LeftSpeakerKey;
        public string RightSpeakerKey;
        public string ActiveSpeakerSide;
        public string TitleKey;
        public string ContentKey;
        public int TypingCharsPerSecond;
        public float SkipHintDelay;
        public bool Skippable;
        public string NextNodeId;
        public string CallbackKey;
        public string CallbackParam;
        public string ConditionType;
        public string ConditionParam;
        public string ConditionOperator;
        public int ConditionValue;
        public string FalseNextNodeId;
        public string Notes;
    }

    [Serializable]
    public class StoryChoiceConfig
    {
        public string Id;
        public string NodeId;
        public int Order;
        public string TitleKey;
        public string NextNodeId;
        public string SetFlag;
        public string AddValue;
        public string Notes;
    }
}
