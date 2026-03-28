using System;
using System.Collections.Generic;
using System.Linq;

namespace Wuxing.Config
{
    [Serializable]
    public class SkillDatabase
    {
        public List<SkillConfig> skills = new List<SkillConfig>();

        public IReadOnlyList<SkillConfig> Skills
        {
            get { return skills; }
        }

        public SkillConfig GetById(string id)
        {
            return skills.FirstOrDefault(skill => skill.Id == id);
        }
    }
}
