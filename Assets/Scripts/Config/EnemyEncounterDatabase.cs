using System;
using System.Collections.Generic;

namespace Wuxing.Config
{
    [Serializable]
    public class EnemyEncounterDatabase
    {
        public List<EnemyEncounterConfig> Encounters = new List<EnemyEncounterConfig>();
    }
}
