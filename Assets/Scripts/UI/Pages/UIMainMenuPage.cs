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
        [SerializeField] private Text progressText;

        public override void OnOpen(object data)
        {
            RefreshLayout();
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
            UIManager.Instance.ShowPage("Start");
        }

        private void RefreshProgress()
        {
            if (progressText == null)
            {
                return;
            }

            progressText.alignment = TextAnchor.MiddleCenter;
            progressText.lineSpacing = 1.18f;

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

            SetButtonText(toastButton, LocalizationManager.GetText("menu.button_back_start"));
        }

        private void OnLanguageChanged()
        {
            RefreshLayout();
            RefreshProgress();
            RefreshButtons();
        }

        private void RefreshLayout()
        {
            SetRect("Header", 0.18f, 0.72f, 0.82f, 0.84f);
            SetRect("SubTitle", 0.22f, 0.64f, 0.78f, 0.68f);
            SetRect("ProgressPanel", 0.16f, 0.42f, 0.84f, 0.58f);
            SetRect("MenuPanel", 0.16f, 0.19f, 0.84f, 0.40f);
            SetRect("Footer", 0.16f, 0.11f, 0.84f, 0.16f);

            var menu = transform.Find("MenuPanel/Menu") as RectTransform;
            if (menu != null)
            {
                menu.anchorMin = new Vector2(0.08f, 0.12f);
                menu.anchorMax = new Vector2(0.92f, 0.88f);
                menu.offsetMin = Vector2.zero;
                menu.offsetMax = Vector2.zero;

                var layout = menu.GetComponent<VerticalLayoutGroup>();
                if (layout != null)
                {
                    layout.spacing = 16f;
                    layout.padding = new RectOffset(0, 0, 6, 6);
                }
            }

            ConfigureButtonHeight(startButton, 62f);
            ConfigureButtonHeight(popupButton, 62f);
            ConfigureButtonHeight(toastButton, 62f);
        }

        private void SetRect(string path, float minX, float minY, float maxX, float maxY)
        {
            var rect = transform.Find(path) as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ConfigureButtonHeight(Button button, float preferredHeight)
        {
            if (button == null)
            {
                return;
            }

            var layoutElement = button.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.preferredHeight = preferredHeight;
                layoutElement.minHeight = preferredHeight;
            }
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

