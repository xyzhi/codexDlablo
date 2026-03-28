using System;
using System.Collections.Generic;
using System.Linq;

namespace Wuxing.Config
{
    [Serializable]
    public class EnemyDatabase
    {
        public List<EnemyConfig> enemies = new List<EnemyConfig>();

        public IReadOnlyList<EnemyConfig> Enemies
        {
            get { return enemies; }
        }

        public EnemyConfig GetById(string id)
        {
            return enemies.FirstOrDefault(enemy => enemy.Id == id);
        }
    }
}
