using System;
using System.Collections.Generic;
using System.Linq;

namespace Wuxing.Config
{
    [Serializable]
    public class StoryNodeDatabase
    {
        public List<StoryNodeConfig> storyNodes = new List<StoryNodeConfig>();
        public List<StoryChoiceConfig> storyChoices = new List<StoryChoiceConfig>();

        public IReadOnlyList<StoryNodeConfig> StoryNodes => storyNodes;
        public IReadOnlyList<StoryChoiceConfig> StoryChoices => storyChoices;

        public StoryNodeConfig GetById(string id)
        {
            return storyNodes.FirstOrDefault(config =>
                config != null
                && string.Equals(config.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public List<StoryChoiceConfig> GetChoicesByNodeId(string nodeId)
        {
            return storyChoices
                .Where(config => config != null && string.Equals(config.NodeId, nodeId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(config => config.Order)
                .ToList();
        }
    }
}
