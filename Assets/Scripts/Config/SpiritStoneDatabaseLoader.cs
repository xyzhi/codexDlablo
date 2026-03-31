using UnityEngine;

namespace Wuxing.Config
{
    public static class SpiritStoneDatabaseLoader
    {
        private const string ResourcePath = "Configs/SpiritStoneDatabase";

        public static SpiritStoneDatabase Load()
        {
            var textAsset = Resources.Load<TextAsset>(ResourcePath);
            if (textAsset == null)
            {
                Debug.LogError($"Spirit stone database json not found at Resources/{ResourcePath}.json");
                return null;
            }

            return JsonUtility.FromJson<SpiritStoneDatabase>(textAsset.text);
        }
    }
}
