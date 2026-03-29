using System.Collections.Generic;
using System.Linq;

namespace Wuxing.Battle
{
    public class BattleTeamRuntime
    {
        public List<BattleUnitRuntime> Units = new List<BattleUnitRuntime>();

        public bool IsAllDead
        {
            get { return Units.Count == 0 || Units.All(unit => unit.IsDead); }
        }

        public BattleUnitRuntime GetFirstAlive()
        {
            return Units.FirstOrDefault(unit => !unit.IsDead);
        }
    }
}

