using UnityEngine;

namespace Wuxing.Config
{
    public static class EventProfileDatabaseLoader
    {
        private const string ResourcePath = "Configs/EventProfileDatabase";
        private static EventProfileDatabase cachedDatabase;

        public static EventProfileDatabase Load()
        {
            if (cachedDatabase != null)
            {
                return cachedDatabase;
            }

            var textAsset = Resources.Load<TextAsset>(ResourcePath);
            if (textAsset == null)
            {
                Debug.LogError($"Event profile database json not found at Resources/{ResourcePath}.json");
                return null;
            }

            cachedDatabase = JsonUtility.FromJson<EventProfileDatabase>(textAsset.text);
            return cachedDatabase;
        }
    }
}