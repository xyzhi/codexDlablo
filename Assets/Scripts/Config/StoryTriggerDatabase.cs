using System;
using System.Collections.Generic;
using System.Linq;

namespace Wuxing.Config
{
    [Serializable]
    public class StoryTriggerDatabase
    {
        public List<StoryTriggerConfig> storyTriggers = new List<StoryTriggerConfig>();

        public IReadOnlyList<StoryTriggerConfig> StoryTriggers => storyTriggers;

        public StoryTriggerConfig FindBestMatch(string triggerKey, int stage)
        {
            var exact = storyTriggers.FirstOrDefault(config =>
                IsMatching(config, triggerKey, stage, true));
            if (exact != null)
            {
                return exact;
            }

            return storyTriggers.FirstOrDefault(config =>
                IsMatching(config, triggerKey, stage, false));
        }

        private static bool IsMatching(StoryTriggerConfig config, string triggerKey, int stage, bool exactStage)
        {
            if (config == null || !config.Enabled)
            {
                return false;
            }

            if (!string.Equals(config.TriggerKey, triggerKey, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (exactStage)
            {
                return config.Stage > 0 && config.Stage == stage;
            }

            return config.Stage <= 0;
        }
    }
}
