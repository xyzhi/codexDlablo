using System;

namespace Wuxing.Config
{
    [Serializable]
    public class EnemyConfig
    {
        public string Id;
        public string Name;
        public string Element;
        public string Role;
        public string Position;
        public int HP;
        public int ATK;
        public int DEF;
        public int MP;
        public string CombatStyle;
        public string Skills;
        public string Notes;
    }
}
