using System;

namespace Wuxing.Game
{
    [Serializable]
    public class BattleRewardResult
    {
        public int ExpGained;
        public int SpiritStonesGained;
        public string SpiritStoneElement;
        public string SpiritStoneName;
        public int LevelsGained;
        public string DroppedEquipmentId;
        public string DroppedEquipmentInstanceId;
        public string DroppedEquipmentName;
    }
}

