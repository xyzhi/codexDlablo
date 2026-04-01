using System;

namespace Wuxing.Config
{
    [Serializable]
    public class SpiritStoneConversionConfig
    {
        public string SourceElement;
        public string TargetElement;
        public int CostAmount;
        public int GainAmount;
        public string Notes;
    }
}
