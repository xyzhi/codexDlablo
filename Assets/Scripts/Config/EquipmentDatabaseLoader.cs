using UnityEngine;

namespace Wuxing.Config
{
    public static class EquipmentDatabaseLoader
    {
        private const string ResourcePath = "Configs/EquipmentDatabase";

        public static EquipmentDatabase Load()
        {
            var textAsset = Resources.Load<TextAsset>(ResourcePath);
            if (textAsset == null)
            {
                Debug.LogError("Equipment database json not found at Resources/Configs/EquipmentDatabase.json");
                return null;
            }

            return JsonUtility.FromJson<EquipmentDatabase>(textAsset.text);
        }
    }
}
