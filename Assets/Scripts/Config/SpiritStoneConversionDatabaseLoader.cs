using UnityEngine;

namespace Wuxing.Config
{
    public static class SpiritStoneConversionDatabaseLoader
    {
        private const string ResourcePath = "Configs/SpiritStoneConversionDatabase";
        private static SpiritStoneConversionDatabase cachedDatabase;

        public static SpiritStoneConversionDatabase Load()
        {
            if (cachedDatabase != null)
            {
                return cachedDatabase;
            }

            var textAsset = Resources.Load<TextAsset>(ResourcePath);
            if (textAsset == null)
            {
                Debug.LogError($"Spirit stone conversion database json not found at Resources/{ResourcePath}.json");
                return null;
            }

            cachedDatabase = JsonUtility.FromJson<SpiritStoneConversionDatabase>(textAsset.text);
            return cachedDatabase;
        }
    }
}
