using System.Collections.Generic;

namespace Wuxing.Battle
{
    public class BattlePlaybackResult
    {
        public bool IsVictory;
        public string WinnerSide;
        public int TotalRounds;
        public string FinalPlayerTeamSummary;
        public string FinalEnemyTeamSummary;
        public List<BattleEvent> Events = new List<BattleEvent>();
    }
}

