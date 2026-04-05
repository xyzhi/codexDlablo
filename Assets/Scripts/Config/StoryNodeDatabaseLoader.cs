using UnityEngine;

namespace Wuxing.Config
{
    public static class StoryNodeDatabaseLoader
    {
        private const string ResourcePath = "Configs/StoryNodeDatabase";
        private static StoryNodeDatabase cachedDatabase;

        public static StoryNodeDatabase Load()
        {
            if (cachedDatabase != null)
            {
                return cachedDatabase;
            }

            var textAsset = Resources.Load<TextAsset>(ResourcePath);
            if (textAsset == null)
            {
                Debug.LogError($"Story node database json not found at Resources/{ResourcePath}.json");
                return null;
            }

            cachedDatabase = JsonUtility.FromJson<StoryNodeDatabase>(textAsset.text);
            return cachedDatabase;
        }

        public static void ClearCache()
        {
            cachedDatabase = null;
        }
    }
}
