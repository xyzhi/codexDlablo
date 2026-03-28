using UnityEngine;

namespace Wuxing.Config
{
    public static class SkillDatabaseLoader
    {
        private const string ResourcePath = "Configs/SkillDatabase";

        public static SkillDatabase Load()
        {
            var textAsset = Resources.Load<TextAsset>(ResourcePath);
            if (textAsset == null)
            {
                Debug.LogError("Skill database json not found at Resources/Configs/SkillDatabase.json");
                return null;
            }

            return JsonUtility.FromJson<SkillDatabase>(textAsset.text);
        }
    }
}
