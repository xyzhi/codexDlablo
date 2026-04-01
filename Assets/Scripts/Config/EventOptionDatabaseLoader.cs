using UnityEngine;
namespace Wuxing.Config
{
    public static class EventOptionDatabaseLoader
    {
        private const string ResourcePath = "Configs/EventOptionDatabase";
        private static EventOptionDatabase cachedDatabase;
        public static EventOptionDatabase Load()
        {
            if (cachedDatabase != null)
            {
                return cachedDatabase;
            }
            var textAsset = Resources.Load<TextAsset>(ResourcePath);
            if (textAsset == null)
            {
                Debug.LogError($"Event option database json not found at Resources/{ResourcePath}.json");
                return null;
            }
            cachedDatabase = JsonUtility.FromJson<EventOptionDatabase>(textAsset.text);
            return cachedDatabase;
        }
    }
}