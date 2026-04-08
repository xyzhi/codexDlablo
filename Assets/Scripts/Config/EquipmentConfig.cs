using System;

namespace Wuxing.Config
{
    [Serializable]
    public class EquipmentConfig
    {
        public string Id;
        public string Name;
        public string Slot;
        public string Quality;
        public int Level;
        public int HP;
        public int ATK;
        public int DEF;
        public int MP;
        public string Notes;
    }
}

