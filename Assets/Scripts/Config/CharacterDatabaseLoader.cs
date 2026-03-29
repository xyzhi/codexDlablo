using UnityEngine;

namespace Wuxing.Config
{
    public static class CharacterDatabaseLoader
    {
        private const string ResourcePath = "Configs/CharacterDatabase";

        public static CharacterDatabase Load()
        {
            var textAsset = Resources.Load<TextAsset>(ResourcePath);
            if (textAsset == null)
            {
                Debug.LogError($"Character database json not found at Resources/{ResourcePath}.json");
                return null;
            }

            return JsonUtility.FromJson<CharacterDatabase>(textAsset.text);
        }
    }
}

