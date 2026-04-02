using UnityEngine;

namespace Wuxing.Config
{
    public static class EnemyEncounterDatabaseLoader
    {
        private const string ResourcePath = "Configs/EnemyEncounterDatabase";
        private static EnemyEncounterDatabase cached;

        public static EnemyEncounterDatabase Load()
        {
            if (cached != null)
            {
                return cached;
            }

            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null || string.IsNullOrEmpty(asset.text))
            {
                cached = new EnemyEncounterDatabase();
                return cached;
            }

            cached = JsonUtility.FromJson<EnemyEncounterDatabase>(asset.text);
            if (cached == null)
            {
                cached = new EnemyEncounterDatabase();
            }

            if (cached.Encounters == null)
            {
                cached.Encounters = new System.Collections.Generic.List<EnemyEncounterConfig>();
            }

            return cached;
        }

        public static void ClearCache()
        {
            cached = null;
        }
    }
}
