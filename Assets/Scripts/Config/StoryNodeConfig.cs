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
        public string Notes;
    }
}
