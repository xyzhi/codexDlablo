using System;
using System.Collections.Generic;

namespace Wuxing.Config
{
    [Serializable]
    public class EquipmentDatabase
    {
        public List<EquipmentConfig> equipments = new List<EquipmentConfig>();

        public IReadOnlyList<EquipmentConfig> Equipments
        {
            get { return equipments; }
        }

        public EquipmentConfig GetById(string id)
        {
            for (var i = 0; i < equipments.Count; i++)
            {
                if (equipments[i] != null && equipments[i].Id == id)
                {
                    return equipments[i];
                }
            }

            return null;
        }
    }
}
