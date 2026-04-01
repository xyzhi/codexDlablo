using System;
using System.Collections.Generic;
using System.Linq;

namespace Wuxing.Config
{
    [Serializable]
    public class StageNodeDatabase
    {
        public List<StageNodeConfig> stageNodes = new List<StageNodeConfig>();

        public IReadOnlyList<StageNodeConfig> StageNodes => stageNodes;

        public StageNodeConfig GetByStage(int stage)
        {
            return stageNodes.FirstOrDefault(config => config != null && config.Stage == stage);
        }
    }
}