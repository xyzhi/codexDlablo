using System;
using System.Collections.Generic;
using System.Linq;

namespace Wuxing.Config
{
    [Serializable]
    public class StageBalanceDatabase
    {
        public List<StageBalanceConfig> stageBalances = new List<StageBalanceConfig>();

        public IReadOnlyList<StageBalanceConfig> StageBalances => stageBalances;

        public StageBalanceConfig GetByStage(int stage)
        {
            return stageBalances.FirstOrDefault(config => config != null && config.Stage == stage);
        }

        public int GetMaxStage()
        {
            return stageBalances == null || stageBalances.Count == 0
                ? 0
                : stageBalances.Where(config => config != null).Select(config => config.Stage).DefaultIfEmpty(0).Max();
        }
    }
}
