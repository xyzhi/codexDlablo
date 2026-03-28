using UnityEngine;

namespace Wuxing.Config
{
    public static class EnemyDatabaseLoader
    {
        private const string ResourcePath = "Configs/EnemyDatabase";

        public static EnemyDatabase Load()
        {
            var textAsset = Resources.Load<TextAsset>(ResourcePath);
            if (textAsset == null)
            {
                Debug.LogError("Enemy database json not found at Resources/Configs/EnemyDatabase.json");
                return null;
            }

            return JsonUtility.FromJson<EnemyDatabase>(textAsset.text);
        }
    }
}
