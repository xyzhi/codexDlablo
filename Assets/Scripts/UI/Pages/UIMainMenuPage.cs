using UnityEngine;
using UnityEngine.UI;
using Wuxing.Game;
using Wuxing.Localization;

namespace Wuxing.UI
{
    public class UIMainMenuPage : UIPage
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button popupButton;
        [SerializeField] private Button toastButton;
        [SerializeField] private Button languageButton;
        [SerializeField] private Text progressText;

        public override void OnOpen(object data)
        {
            RefreshProgress();
            RefreshButtons();
        }

        private void OnEnable()
        {
            GameProgressManager.ProgressChanged += RefreshProgress;
            GameProgressManager.ProgressChanged += RefreshButtons;
            LocalizationManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            GameProgressManager.ProgressChanged -= RefreshProgress;
            GameProgressManager.ProgressChanged -= RefreshButtons;
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
        }

        private void Awake()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnClickStart);
            }

            if (popupButton != null)
            {
                popupButton.onClick.AddListener(OnClickPopup);
            }

            if (toastButton != null)
            {
                toastButton.onClick.AddListener(OnClickToast);
            }

            if (languageButton != null)
            {
                languageButton.onClick.AddListener(OnClickLanguage);
            }
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnClickStart);
            }

            if (popupButton != null)
            {
                popupButton.onClick.RemoveListener(OnClickPopup);
            }

            if (toastButton != null)
            {
                toastButton.onClick.RemoveListener(OnClickToast);
            }

            if (languageButton != null)
            {
                languageButton.onClick.RemoveListener(OnClickLanguage);
            }
        }

        private void OnClickStart()
        {
            GameProgressManager.StartRun();
            UIManager.Instance.ShowPage("Map");
        }

        private void OnClickPopup()
        {
            var popup = UIManager.Instance.ShowPopup<UIConfirmPopup>("Confirm");
            if (popup == null)
            {
                return;
            }

            var isEnglish = IsEnglish();
            if (GameProgressManager.HasActiveRun())
            {
                popup.Setup(
                    isEnglish ? "Reset Current Run" : "重置本轮",
                    isEnglish
                        ? "Return to the main menu and set current stage back to 0?"
                        : "回到主菜单，并把当前关卡重置为 0 吗？",
                    false,
                    delegate
                    {
                        GameProgressManager.ResetRun();
                        RefreshProgress();
                        RefreshButtons();
                    },
                    delegate { });
                return;
            }

            var stageTheme = GameProgressManager.GetStageTheme(isEnglish, 1);
            popup.Setup(
                isEnglish ? "Run Flow" : "流程说明",
                isEnglish
                    ? "Main Menu -> Map -> Battle -> Result -> Back To Map.\nFirst destination: " + stageTheme + "."
                    : "主菜单 -> 地图 -> 战斗 -> 结算 -> 返回地图。\n当前起点区域：" + stageTheme + "。",
                false,
                delegate { },
                null,
                isEnglish ? "OK" : "知道了",
                null);
        }

        private void OnClickToast()
        {
            UIManager.Instance.ShowToast(GameProgressManager.BuildLastBattleSummary(IsEnglish()), 2.2f);
        }

        private void OnClickLanguage()
        {
            LocalizationManager.ToggleLanguage();
            RefreshProgress();
            RefreshButtons();
            UIManager.Instance.ShowToastByKey("toast.language_changed");
        }

        private void RefreshProgress()
        {
            if (progressText == null)
            {
                return;
            }

            var isEnglish = IsEnglish();
            var stage = Mathf.Max(1, GameProgressManager.GetCurrentStage());
            if (isEnglish)
            {
                progressText.text = GameProgressManager.BuildProgressSummary(true)
                    + "\nRegion: " + GameProgressManager.GetStageTheme(true, stage)
                    + "\nLifespan: " + GameProgressManager.BuildLongevitySummary(true)
                    + "\nObjective: " + GameProgressManager.BuildCurrentObjective(true);
                return;
            }

            progressText.text = GameProgressManager.BuildProgressSummary(false)
                + "\n区域：" + GameProgressManager.GetStageTheme(false, stage)
                + "\n" + GameProgressManager.BuildLongevitySummary(false)
                + "\n目标：" + GameProgressManager.BuildCurrentObjective(false);
        }

        private void RefreshButtons()
        {
            var currentStage = GameProgressManager.GetCurrentStage();
            var hasActiveRun = GameProgressManager.HasActiveRun();
            var isEnglish = IsEnglish();

            SetButtonText(startButton, hasActiveRun
                ? (isEnglish ? "Continue To Map" : "继续前往地图")
                : (isEnglish ? "Start Run" : "开始游戏"));

            SetButtonText(popupButton, hasActiveRun
                ? (isEnglish ? "Reset Run" : "重置本轮")
                : (isEnglish ? "Flow Guide" : "流程说明"));

            SetButtonText(toastButton, isEnglish ? "Last Battle" : "上一场战报");
        }

        private void OnLanguageChanged()
        {
            RefreshProgress();
            RefreshButtons();
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

