using System;

namespace Wuxing.Config
{
    [Serializable]
    public class StoryTriggerConfig
    {
        public string Id;
        public string TriggerKey;
        public int Stage;
        public string NodeId;
        public bool OncePerRun;
        public bool Enabled;
        public string Notes;
    }
}
