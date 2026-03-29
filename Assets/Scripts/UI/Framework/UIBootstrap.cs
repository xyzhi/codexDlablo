using UnityEngine;
using Wuxing.Game;
using Wuxing.Localization;

namespace Wuxing.UI
{
    public class UIBootstrap : MonoBehaviour
    {
        private void Start()
        {
            if (LocalizationManager.Instance == null)
            {
                var localizationObject = new GameObject("LocalizationManager");
                localizationObject.AddComponent<LocalizationManager>();
            }

            if (UIManager.Instance == null)
            {
                var managerObject = new GameObject("UIManager");
                managerObject.AddComponent<UIManager>();
            }

            if (GameProgressManager.Instance == null)
            {
                var progressObject = new GameObject("GameProgressManager");
                progressObject.AddComponent<GameProgressManager>();
            }

            UIManager.Instance.ShowPage("MainMenu");
        }
    }
}

