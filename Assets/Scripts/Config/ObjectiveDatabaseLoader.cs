using UnityEngine;

namespace Wuxing.Config
{
    public static class ObjectiveDatabaseLoader
    {
        private const string ResourcePath = "Configs/ObjectiveDatabase";
        private static ObjectiveDatabase cachedDatabase;

        public static ObjectiveDatabase Load()
        {
            if (cachedDatabase != null)
            {
                return cachedDatabase;
            }

            var textAsset = Resources.Load<TextAsset>(ResourcePath);
            if (textAsset == null)
            {
                Debug.LogError("Objective database json not found at Resources/" + ResourcePath + ".json");
                return null;
            }

            cachedDatabase = JsonUtility.FromJson<ObjectiveDatabase>(textAsset.text);
            return cachedDatabase;
        }
    }
}
