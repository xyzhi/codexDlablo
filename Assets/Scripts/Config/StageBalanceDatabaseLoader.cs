using UnityEngine;

namespace Wuxing.Config
{
    public static class StageBalanceDatabaseLoader
    {
        private const string ResourcePath = "Configs/StageBalanceDatabase";
        private static StageBalanceDatabase cachedDatabase;

        public static StageBalanceDatabase Load()
        {
            if (cachedDatabase != null)
            {
                return cachedDatabase;
            }

            var textAsset = Resources.Load<TextAsset>(ResourcePath);
            if (textAsset == null)
            {
                Debug.LogError($"Stage balance database json not found at Resources/{ResourcePath}.json");
                return null;
            }

            cachedDatabase = JsonUtility.FromJson<StageBalanceDatabase>(textAsset.text);
            return cachedDatabase;
        }
    }
}
