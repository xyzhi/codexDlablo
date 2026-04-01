using System;
using System.Collections.Generic;
using System.Linq;

namespace Wuxing.Config
{
    [Serializable]
    public class SpiritStoneConversionDatabase
    {
        public List<SpiritStoneConversionConfig> conversions = new List<SpiritStoneConversionConfig>();

        public IReadOnlyList<SpiritStoneConversionConfig> Conversions => conversions;

        public SpiritStoneConversionConfig GetBySourceElement(string sourceElement)
        {
            return conversions.FirstOrDefault(config =>
                config != null &&
                string.Equals(config.SourceElement, sourceElement, StringComparison.OrdinalIgnoreCase));
        }
    }
}
