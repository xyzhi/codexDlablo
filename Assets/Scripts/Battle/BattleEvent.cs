namespace Wuxing.Battle
{
    public class BattleEvent
    {
        public BattleEventType Type;
        public string Log;
        public string PlayerTeamSummary;
        public string EnemyTeamSummary;
        public string PlayerEquipmentSummary;
        public string EnemyEquipmentSummary;
        public bool IsBattleFinished;
        public bool? IsVictory;
    }
}
