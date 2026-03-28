using System;
using UnityEngine;

namespace Wuxing.Config
{
    [Serializable]
    public class CharacterConfig
    {
        public string Id;
        public string Name;
        public string ElementRoots;
        public string ClassRole;
        public string Position;
        public int HP;
        public int ATK;
        public int DEF;
        public int MP;
        public string CombatStyle;
        public string InitialSkills;
        public string GrowthNotes;
    }
}
