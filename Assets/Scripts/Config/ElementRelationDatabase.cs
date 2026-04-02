using System;
using System.Collections.Generic;
using System.Linq;

namespace Wuxing.Config
{
    [Serializable]
    public class ElementRelationDatabase
    {
        public List<ElementRelationConfig> Relations = new List<ElementRelationConfig>();

        public float GetMultiplier(string attackerElement, string defenderElement)
        {
            var relation = Relations.FirstOrDefault(item => item != null
                && string.Equals(item.AttackerElement, attackerElement, StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.DefenderElement, defenderElement, StringComparison.OrdinalIgnoreCase));
            return relation != null ? relation.Multiplier : 1f;
        }
    }
}
