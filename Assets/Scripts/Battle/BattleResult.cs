using System.Collections.Generic;

namespace Wuxing.Battle
{
    public class BattleResult
    {
        public bool IsVictory;
        public string WinnerSide;
        public int TotalRounds;
        public string PlayerTeamSummary;
        public string EnemyTeamSummary;
        public List<string> Logs = new List<string>();
    }
}
