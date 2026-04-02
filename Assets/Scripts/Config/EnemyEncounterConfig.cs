using System;

namespace Wuxing.Config
{
    [Serializable]
    public class EnemyEncounterConfig
    {
        public string Id;
        public int StageFrom;
        public int StageTo;
        public string NodeType;
        public string EnemyIds;
        public string OverrideElement;
        public int Weight;
        public string Notes;
    }
}
