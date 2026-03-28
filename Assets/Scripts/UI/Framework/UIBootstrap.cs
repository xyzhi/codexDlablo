using UnityEngine;
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

            UIManager.Instance.ShowPage("MainMenu");
        }
    }
}
