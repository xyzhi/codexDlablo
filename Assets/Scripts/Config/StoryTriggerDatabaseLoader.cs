using UnityEngine;

namespace Wuxing.Config
{
    public static class StoryTriggerDatabaseLoader
    {
        private const string ResourcePath = "Configs/StoryTriggerDatabase";
        private static StoryTriggerDatabase cachedDatabase;

        public static StoryTriggerDatabase Load()
        {
            if (cachedDatabase != null)
            {
                return cachedDatabase;
            }

            var textAsset = Resources.Load<TextAsset>(ResourcePath);
            if (textAsset == null)
            {
                Debug.LogError($"Story trigger database json not found at Resources/{ResourcePath}.json");
                return null;
            }

            cachedDatabase = JsonUtility.FromJson<StoryTriggerDatabase>(textAsset.text);
            return cachedDatabase;
        }

        public static void ClearCache()
        {
            cachedDatabase = null;
        }
    }
}
