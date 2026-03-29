using UnityEngine;
using UnityEngine.UI;
using Wuxing.Game;
using Wuxing.Localization;

namespace Wuxing.UI
{
    public class UIMapPage : UIPage
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text regionText;
        [SerializeField] private Text longevityText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text routeText;
        [SerializeField] private Button advanceButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button backButton;

        public override void OnOpen(object data)
        {
            GameProgressManager.StartRun();
            RefreshView();
        }

        private void OnEnable()
        {
            GameProgressManager.ProgressChanged += RefreshView;
            LocalizationManager.LanguageChanged += RefreshView;
        }

        private void OnDisable()
        {
            GameProgressManager.ProgressChanged -= RefreshView;
            LocalizationManager.LanguageChanged -= RefreshView;
        }

        private void Awake()
        {
            if (advanceButton != null)
            {
                advanceButton.onClick.AddListener(OnClickAdvance);
            }

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(OnClickReset);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnClickBack);
            }
        }

        private void OnDestroy()
        {
            if (advanceButton != null)
            {
                advanceButton.onClick.RemoveListener(OnClickAdvance);
            }

            if (resetButton != null)
            {
                resetButton.onClick.RemoveListener(OnClickReset);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnClickBack);
            }
        }

        private void OnClickAdvance()
        {
            var currentStage = Mathf.Max(1, GameProgressManager.GetCurrentStage());
            var nodeType = GameProgressManager.GetNodeType(currentStage);
            if (GameProgressManager.IsBattleNode(nodeType))
            {
                UIManager.Instance.ShowPage("Battle");
                return;
            }

            var result = GameProgressManager.AdvanceNonBattleNode(1);
            if (result == RunAdvanceResult.LifespanEnded)
            {
                UIManager.Instance.ShowToastByKey("map.toast_lifespan_ended", 2f);
                UIManager.Instance.ShowPage("MainMenu");
                return;
            }

            if (result == RunAdvanceResult.ChapterComplete)
            {
                UIManager.Instance.ShowToastByKey("map.toast_route_complete", 2f);
                UIManager.Instance.ShowPage("MainMenu");
                return;
            }

            UIManager.Instance.ShowToastByKey("map.toast_month_passed", 1.5f);
            RefreshView();
        }

        private void OnClickReset()
        {
            GameProgressManager.ResetRun();
            UIManager.Instance.ShowPage("MainMenu");
        }

        private void OnClickBack()
        {
            UIManager.Instance.ShowPage("MainMenu");
        }

        private void RefreshView()
        {
            var isEnglish = IsEnglish();
            var currentStage = Mathf.Max(1, GameProgressManager.GetCurrentStage());
            var nodeType = GameProgressManager.GetNodeType(currentStage);

            if (titleText != null)
            {
                titleText.text = LocalizationManager.GetText("map.title");
            }

            if (statusText != null)
            {
                statusText.text = LocalizationManager.GetText("map.status_prefix")
                    + GameProgressManager.GetNodeTypeLabel(isEnglish, currentStage);
            }

            if (regionText != null)
            {
                regionText.text = LocalizationManager.GetText("map.region_prefix")
                    + GameProgressManager.GetStageTheme(isEnglish, currentStage);
            }

            if (longevityText != null)
            {
                longevityText.text = GameProgressManager.BuildLongevitySummary(isEnglish);
            }

            if (objectiveText != null)
            {
                objectiveText.text = GameProgressManager.BuildCurrentObjective(isEnglish);
            }

            if (routeText != null)
            {
                routeText.text = GameProgressManager.BuildMapRouteSummary(isEnglish);
            }

            SetButtonText(
                advanceButton,
                GameProgressManager.IsBattleNode(nodeType)
                    ? LocalizationManager.GetText("map.button_enter")
                    : LocalizationManager.GetText("map.button_pass_month"));
            SetButtonText(resetButton, LocalizationManager.GetText("map.button_reset_run"));
            SetButtonText(backButton, LocalizationManager.GetText("map.button_back_menu"));
        }

        private static void SetButtonText(Button button, string text)
        {
            if (button == null)
            {
                return;
            }

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = text;
            }
        }

        private static bool IsEnglish()
        {
            return LocalizationManager.Instance != null
                && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
        }
    }
}



