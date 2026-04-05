using System;
using System.Collections.Generic;
using System.Linq;

namespace Wuxing.Config
{
    [Serializable]
    public class StoryNodeDatabase
    {
        public List<StoryNodeConfig> storyNodes = new List<StoryNodeConfig>();

        public IReadOnlyList<StoryNodeConfig> StoryNodes => storyNodes;

        public StoryNodeConfig GetById(string id)
        {
            return storyNodes.FirstOrDefault(config =>
                config != null
                && string.Equals(config.Id, id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
