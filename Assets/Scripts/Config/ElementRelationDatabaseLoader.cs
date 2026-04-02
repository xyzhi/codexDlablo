using UnityEngine;

namespace Wuxing.Config
{
    public static class ElementRelationDatabaseLoader
    {
        private const string ResourcePath = "Configs/ElementRelationDatabase";

        public static ElementRelationDatabase Load()
        {
            var textAsset = Resources.Load<TextAsset>(ResourcePath);
            if (textAsset == null)
            {
                Debug.LogError("Element relation database json not found at Resources/Configs/ElementRelationDatabase.json");
                return null;
            }

            return JsonUtility.FromJson<ElementRelationDatabase>(textAsset.text);
        }
    }
}
