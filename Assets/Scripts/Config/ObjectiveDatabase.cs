using System;
using System.Collections.Generic;

namespace Wuxing.Config
{
    [Serializable]
    public class ObjectiveDatabase
    {
        public List<ObjectiveConfig> objectives = new List<ObjectiveConfig>();

        public IReadOnlyList<ObjectiveConfig> Objectives
        {
            get { return objectives; }
        }

        public ObjectiveConfig GetById(string id)
        {
            for (var i = 0; i < objectives.Count; i++)
            {
                var objective = objectives[i];
                if (objective != null && string.Equals(objective.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return objective;
                }
            }

            return null;
        }
    }
}
