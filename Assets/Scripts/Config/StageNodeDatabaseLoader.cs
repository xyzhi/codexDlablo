using UnityEngine;

namespace Wuxing.Config
{
    public static class StageNodeDatabaseLoader
    {
        private const string ResourcePath = "Configs/StageNodeDatabase";
        private static StageNodeDatabase cachedDatabase;

        public static StageNodeDatabase Load()
        {
            if (cachedDatabase != null)
            {
                return cachedDatabase;
            }

            var textAsset = Resources.Load<TextAsset>(ResourcePath);
            if (textAsset == null)
            {
                Debug.LogError($"Stage node database json not found at Resources/{ResourcePath}.json");
                return null;
            }

            cachedDatabase = JsonUtility.FromJson<StageNodeDatabase>(textAsset.text);
            return cachedDatabase;
        }
    }
}