using UnityEngine;

namespace Wuxing.Config
{
    public static class BattleFormulaDatabaseLoader
    {
        private const string ResourcePath = "Configs/BattleFormulaDatabase";

        public static BattleFormulaDatabase Load()
        {
            var textAsset = Resources.Load<TextAsset>(ResourcePath);
            if (textAsset == null)
            {
                Debug.LogError("Battle formula database json not found at Resources/Configs/BattleFormulaDatabase.json");
                return null;
            }

            return JsonUtility.FromJson<BattleFormulaDatabase>(textAsset.text);
        }
    }
}
