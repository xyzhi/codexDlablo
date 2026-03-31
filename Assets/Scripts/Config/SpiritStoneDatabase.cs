using System;
using System.Collections.Generic;
using System.Linq;

namespace Wuxing.Config
{
    [Serializable]
    public class SpiritStoneDatabase
    {
        public List<SpiritStoneConfig> spiritStones = new List<SpiritStoneConfig>();

        public IReadOnlyList<SpiritStoneConfig> SpiritStones => spiritStones;

        public SpiritStoneConfig GetByElement(string element)
        {
            return spiritStones.FirstOrDefault(config => config != null && config.Element == element);
        }
    }
}
