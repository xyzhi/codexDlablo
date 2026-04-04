using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Wuxing.Battle;
using Wuxing.Config;
using Wuxing.Game;
using Wuxing.Localization;

namespace Wuxing.UI
{
    public class UIBattlePage : UIPage
    {
        private const float AutoStartDelaySeconds = UIMapPage.MapToastDuration;
        private const string RoundDivider = "============";
        private const string EndDivider = "##########";
        private const string ActionPrefix = "  > ";

        [SerializeField] private GameObject battleLogOverlay;
        [SerializeField] private Button backButton;
        [SerializeField] private Button startBattleButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button equipmentButton;
        [SerializeField] private Button closeEquipmentButton;
        [SerializeField] private Button cycleEquipmentPresetButton;
        [SerializeField] private Button cycleEquipmentUnitButton;
        [SerializeField] private Button cycleWeaponButton;
        [SerializeField] private Button cycleArmorButton;
        [SerializeField] private Button cycleAccessoryButton;
        [SerializeField] private Button autoOffenseButton;
        [SerializeField] private Button autoDefenseButton;
        [SerializeField] private Button resetEquipmentButton;
        [SerializeField] private GameObject equipmentPanel;
        [SerializeField] private Text equipmentSelectionTitleText;
        [SerializeField] private RectTransform equipmentSelectionContent;
        [SerializeField] private Text equipmentDetailText;
        [SerializeField] private Text stageInfoText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text playerTeamText;
        [SerializeField] private Text enemyTeamText;
        [SerializeField] private RectTransform playerCardRoot;
        [SerializeField] private Button playerCardTemplate;
        [SerializeField] private RectTransform enemyCardRoot;
        [SerializeField] private Button enemyCardTemplate;
        [SerializeField] private Text playerEquipmentText;
        [SerializeField] private Text enemyEquipmentText;
        [SerializeField] private Text battleLogText;
        [SerializeField] private ScrollRect battleLogScrollRect;

        private Coroutine battlePlaybackCoroutine;
        private readonly StringBuilder battleLogBuilder = new StringBuilder();
        private UIScrollFollowController battleLogFollowController;
        private bool isApplyingAutoScroll;
        private List<string> knownSkillNames;
        private int selectedEquipmentUnitIndex;
        private string selectingSlot;
        private string preferredSelectionEquipmentId;
        private bool equipmentLayoutCached;
        private float selectionTopInset;
        private float detailBottomInset;
        private float sharedSelectionDetailHeight;
        private readonly List<Button> equipmentSelectionButtons = new List<Button>();
        private readonly List<Button> playerTeamCardButtons = new List<Button>();
        private readonly List<Button> enemyTeamCardButtons = new List<Button>();
        private bool openedForEquipment;
        private BattlePlaybackResult storedBattleResultPlayback;
        private BattleRewardResult storedBattleResultReward;
        private bool canReopenBattleResultPopup;
        private int displayedBattleRoundCount;
        private int displayedBattleActionCount;

        public override void OnOpen(object data)
        {
            GameProgressManager.StartRun();
            openedForEquipment = data as string == "equipment";
            ResetCurrentBattleState();

            if (openedForEquipment)
            {
                EnsureSelectionFocusFromPreferredEquipment();
                RefreshEquipmentPanel();
                SetEquipmentPanelVisible(true);
                ApplyStatus(LocalizationManager.Instance != null
                    && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English
                    ? "Adjust Equipment"
                    : "\u8c03\u6574\u88c5\u5907");
                SetBackButtonLabel(LocalizationManager.Instance != null
                    && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English
                    ? "Back To Map"
                    : "\u8fd4\u56de\u5730\u56fe");
                return;
            }

            UpdateBackButtonStateLabel();
            StartCoroutine(AutoStartBattleNextFrame());
        }

        private void OnEnable()
        {
            LocalizationManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
        }

        private void Awake()
        {
            if (battleLogText != null)
            {
                battleLogText.supportRichText = true;
            }

            ConfigureBattleSummaryLayout();

            if (battleLogScrollRect != null && battleLogScrollRect.viewport != null)
            {
                battleLogFollowController = battleLogScrollRect.viewport.GetComponent<UIScrollFollowController>();
                battleLogScrollRect.onValueChanged.AddListener(OnBattleLogScrollChanged);
            }

            InitializeTeamCardTemplate(playerCardTemplate, playerTeamCardButtons);
            InitializeTeamCardTemplate(enemyCardTemplate, enemyTeamCardButtons);

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnClickBack);
            }

            if (startBattleButton != null)
            {
                startBattleButton.onClick.AddListener(OnClickStartBattle);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnClickRestart);
            }

            if (equipmentButton != null)
            {
                equipmentButton.onClick.AddListener(OnClickEquipment);
            }

            if (closeEquipmentButton != null)
            {
                closeEquipmentButton.onClick.AddListener(OnClickCloseEquipment);
            }

            if (cycleEquipmentPresetButton != null)
            {
                cycleEquipmentPresetButton.onClick.AddListener(OnClickCycleEquipmentPreset);
            }

            if (cycleEquipmentUnitButton != null)
            {
                cycleEquipmentUnitButton.onClick.AddListener(OnClickCycleEquipmentUnit);
            }

            if (cycleWeaponButton != null)
            {
                cycleWeaponButton.onClick.AddListener(OnClickCycleWeapon);
            }

            if (cycleArmorButton != null)
            {
                cycleArmorButton.onClick.AddListener(OnClickCycleArmor);
            }

            if (cycleAccessoryButton != null)
            {
                cycleAccessoryButton.onClick.AddListener(OnClickCycleAccessory);
            }

            if (autoOffenseButton != null)
            {
                autoOffenseButton.onClick.AddListener(OnClickAutoOffense);
            }

            if (autoDefenseButton != null)
            {
                autoDefenseButton.onClick.AddListener(OnClickAutoDefense);
            }

            if (resetEquipmentButton != null)
            {
                resetEquipmentButton.onClick.AddListener(OnClickResetEquipment);
            }
        }

        private void OnDestroy()
        {
            if (battlePlaybackCoroutine != null)
            {
                StopCoroutine(battlePlaybackCoroutine);
            }

            if (battleLogScrollRect != null)
            {
                battleLogScrollRect.onValueChanged.RemoveListener(OnBattleLogScrollChanged);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnClickBack);
            }

            if (startBattleButton != null)
            {
                startBattleButton.onClick.RemoveListener(OnClickStartBattle);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnClickRestart);
            }

            if (equipmentButton != null)
            {
                equipmentButton.onClick.RemoveListener(OnClickEquipment);
            }

            if (closeEquipmentButton != null)
            {
                closeEquipmentButton.onClick.RemoveListener(OnClickCloseEquipment);
            }

            if (cycleEquipmentPresetButton != null)
            {
                cycleEquipmentPresetButton.onClick.RemoveListener(OnClickCycleEquipmentPreset);
            }

            if (cycleEquipmentUnitButton != null)
            {
                cycleEquipmentUnitButton.onClick.RemoveListener(OnClickCycleEquipmentUnit);
            }

            if (cycleWeaponButton != null)
            {
                cycleWeaponButton.onClick.RemoveListener(OnClickCycleWeapon);
            }

            if (cycleArmorButton != null)
            {
                cycleArmorButton.onClick.RemoveListener(OnClickCycleArmor);
            }

            if (cycleAccessoryButton != null)
            {
                cycleAccessoryButton.onClick.RemoveListener(OnClickCycleAccessory);
            }

            if (autoOffenseButton != null)
            {
                autoOffenseButton.onClick.RemoveListener(OnClickAutoOffense);
            }

            if (autoDefenseButton != null)
            {
                autoDefenseButton.onClick.RemoveListener(OnClickAutoDefense);
            }

            if (resetEquipmentButton != null)
            {
                resetEquipmentButton.onClick.RemoveListener(OnClickResetEquipment);
            }
        }

        private void OnClickBack()
        {
            if (openedForEquipment)
            {
                UIManager.Instance.ShowPage("Map");
                return;
            }

            if (CanReopenBattleResultPopup())
            {
                OpenStoredBattleResultPopup();
                return;
            }

            if (battlePlaybackCoroutine != null)
            {
                StopCoroutine(battlePlaybackCoroutine);
                battlePlaybackCoroutine = null;
            }

            var isEnglish = LocalizationManager.Instance != null
                && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
            var popup = UIManager.Instance.ShowPopup<UIConfirmPopup>("Confirm");
            if (popup == null)
            {
                return;
            }

            popup.Setup(
                isEnglish ? "Retreat" : "\u64a4\u9000\u7ed3\u7b97",
                BuildRetreatMessage(isEnglish),
                false,
                delegate
                {
                    GameProgressManager.RetreatToPreviousStage();
                    UIManager.Instance.ShowToast(isEnglish
                        ? "You fled the battle and fell back one stage."
                        : "\u4f60\u5df2\u9003\u79bb\u6218\u6597\uff0c\u9000\u56de\u4e0a\u4e00\u5173\u3002", 2f);
                    UIManager.Instance.ShowPage("Map");
                },
                delegate { },
                isEnglish ? "Confirm Retreat" : "\u786e\u8ba4\u64a4\u9000",
                isEnglish ? "Keep Fighting" : "\u7ee7\u7eed\u6218\u6597");
        }
        private void OnClickStartBattle()
        {
            if (battlePlaybackCoroutine != null)
            {
                StopCoroutine(battlePlaybackCoroutine);
            }

            SetLogOverlayVisible(true);
            battlePlaybackCoroutine = StartCoroutine(PlayBattle());
        }

        private void OnClickRestart()
        {
            var isEnglish = LocalizationManager.Instance != null
                && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
            var popup = UIManager.Instance.ShowPopup<UIConfirmPopup>("Confirm");
            if (popup == null)
            {
                return;
            }

            popup.Setup(
                isEnglish ? "Reset Run" : "\u91cd\u7f6e\u672c\u8f6e",
                isEnglish ? "Return to the main menu and reset current progress?" : "\u56de\u5230\u4e3b\u83dc\u5355\uff0c\u5e76\u5c06\u5f53\u524d\u8fdb\u5ea6\u91cd\u7f6e\u4e3a 0 \u5417\uff1f",
                false,
                delegate
                {
                    ClearStoredBattleResult();
                    GameProgressManager.ResetRun();
                    UIManager.Instance.ShowPage("MainMenu");
                },
                delegate { },
                isEnglish ? "Confirm" : "\u786e\u8ba4",
                isEnglish ? "Cancel" : "\u53d6\u6d88");
        }

        private void OnClickEquipment()
        {
            EnsureSelectionFocusFromPreferredEquipment();
            RefreshEquipmentPanel();
            SetEquipmentPanelVisible(true);
        }

        private void OnClickCloseEquipment()
        {
            if (openedForEquipment)
            {
                UIManager.Instance.ShowPage("Map");
                return;
            }

            SetEquipmentPanelVisible(false);
        }

        private void OnClickCycleEquipmentPreset()
        {
            BattleManager.CyclePlayerEquipmentPreset();
            RefreshPreview();
            RefreshEquipmentPanel();
            SetEquipmentPanelVisible(true);
        }

        private void OnClickCycleEquipmentUnit()
        {
            var unitCount = BattleManager.GetPlayerEquipmentUnitCount();
            if (unitCount <= 0)
            {
                return;
            }

            selectedEquipmentUnitIndex = (selectedEquipmentUnitIndex + 1) % unitCount;
            RefreshEquipmentPanel();
            SetEquipmentPanelVisible(true);
        }

        private void OnClickCycleWeapon()
        {
            preferredSelectionEquipmentId = null;
            OpenEquipmentSelection("Weapon");
        }

        private void OnClickCycleArmor()
        {
            preferredSelectionEquipmentId = null;
            OpenEquipmentSelection("Armor");
        }

        private void OnClickCycleAccessory()
        {
            preferredSelectionEquipmentId = null;
            OpenEquipmentSelection("Accessory");
        }

        private void OnClickAutoOffense()
        {
            BattleManager.AutoEquipPlayerUnitOffense(selectedEquipmentUnitIndex);
            RefreshPreview();
            RefreshEquipmentPanel();
            SetEquipmentPanelVisible(true);
        }

        private void OnClickAutoDefense()
        {
            BattleManager.AutoEquipPlayerUnitDefense(selectedEquipmentUnitIndex);
            RefreshPreview();
            RefreshEquipmentPanel();
            SetEquipmentPanelVisible(true);
        }

        private void OnClickResetEquipment()
        {
            BattleManager.ResetPlayerEquipmentOverrides();
            RefreshPreview();
            RefreshEquipmentPanel();
            SetEquipmentPanelVisible(true);
        }

        private IEnumerator PlayBattle()
        {
            SetButtonsInteractable(false);
            battleLogBuilder.Length = 0;
            displayedBattleRoundCount = 0;
            displayedBattleActionCount = 0;
            ResetLogFollow();
            ApplyStatus(LocalizationManager.GetText("battle.status_idle"));
            ApplyBattleLog(string.Empty);

            var playback = BattleManager.RunSampleBattlePlayback();
            for (var i = 0; i < playback.Events.Count; i++)
            {
                ApplyBattleEvent(playback.Events[i]);
                yield return new WaitForSeconds(GetEventDelay(playback.Events[i]));
            }

            ApplyStatus(playback.IsVictory
                ? LocalizationManager.GetText("battle.status_victory")
                : LocalizationManager.GetText("battle.status_defeat"));

            SetButtonsInteractable(true);
            ShowBattleResultPopup(playback);
            battlePlaybackCoroutine = null;
        }

        private void ResetCurrentBattleState()
        {
            if (battlePlaybackCoroutine != null)
            {
                StopCoroutine(battlePlaybackCoroutine);
                battlePlaybackCoroutine = null;
            }

            ClearStoredBattleResult();
            displayedBattleRoundCount = 0;
            displayedBattleActionCount = 0;
            RefreshPreview();
        }

        private IEnumerator AutoStartBattleNextFrame()
        {
            yield return new WaitForSeconds(AutoStartDelaySeconds);
            if (!openedForEquipment)
            {
                OnClickStartBattle();
            }
        }

        private void ApplyBattleEvent(BattleEvent battleEvent)
        {
            if (playerTeamText != null)
            {
                playerTeamText.text = string.Empty;
            }

            if (enemyTeamText != null)
            {
                enemyTeamText.text = string.Empty;
            }

            RefreshTeamCardsFromSummary(battleEvent.PlayerTeamSummary, battleEvent.PlayerEquipmentSummary, true, playerCardRoot, playerCardTemplate, playerTeamCardButtons);
            RefreshTeamCardsFromSummary(battleEvent.EnemyTeamSummary, battleEvent.EnemyEquipmentSummary, false, enemyCardRoot, enemyCardTemplate, enemyTeamCardButtons);

            if (playerEquipmentText != null)
            {
                playerEquipmentText.text = string.Empty;
            }

            if (enemyEquipmentText != null)
            {
                enemyEquipmentText.text = string.Empty;
            }

            if (!string.IsNullOrEmpty(battleEvent.Log))
            {
                if (battleLogBuilder.Length > 0)
                {
                    battleLogBuilder.Append('\n');
                }

                battleLogBuilder.Append(FormatBattleLogLine(battleEvent));
                ApplyBattleLog(battleLogBuilder.ToString());
            }

            if (battleEvent.IsBattleFinished && battleEvent.IsVictory.HasValue)
            {
                ApplyStatus(battleEvent.IsVictory.Value
                    ? LocalizationManager.GetText("battle.status_victory")
                    : LocalizationManager.GetText("battle.status_defeat"));
            }
        }

        private void RefreshPreview()
        {
            ApplyStageInfo();
            ApplyStatus(openedForEquipment
                ? (LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English ? "Adjust Equipment" : "\u8c03\u6574\u88c5\u5907")
                : (LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English ? "Battle In Progress" : "\u6218\u6597\u8fdb\u884c\u4e2d"));

            var previewPlayback = BattleManager.RunSampleBattlePlayback();
            var previewEvent = previewPlayback.Events.Count > 0 ? previewPlayback.Events[0] : null;

            if (playerTeamText != null)
            {
                playerTeamText.text = string.Empty;
            }

            if (enemyTeamText != null)
            {
                enemyTeamText.text = string.Empty;
            }

            var playerSummary = previewEvent != null && !string.IsNullOrEmpty(previewEvent.PlayerTeamSummary)
                ? previewEvent.PlayerTeamSummary
                : LocalizationManager.GetText("battle.player_content");
            var enemySummary = previewEvent != null && !string.IsNullOrEmpty(previewEvent.EnemyTeamSummary)
                ? previewEvent.EnemyTeamSummary
                : LocalizationManager.GetText("battle.enemy_content");
            var playerEquipmentSummary = previewEvent != null
                ? previewEvent.PlayerEquipmentSummary
                : LocalizationManager.GetText("battle.equipment_none");
            var enemyEquipmentSummary = previewEvent != null
                ? previewEvent.EnemyEquipmentSummary
                : LocalizationManager.GetText("battle.equipment_none");

            RefreshTeamCardsFromSummary(playerSummary, playerEquipmentSummary, true, playerCardRoot, playerCardTemplate, playerTeamCardButtons);
            RefreshTeamCardsFromSummary(enemySummary, enemyEquipmentSummary, false, enemyCardRoot, enemyCardTemplate, enemyTeamCardButtons);

            if (playerEquipmentText != null)
            {
                playerEquipmentText.text = string.Empty;
            }

            if (enemyEquipmentText != null)
            {
                enemyEquipmentText.text = string.Empty;
            }

            ApplyBattleLog(string.Empty);
            ResetLogFollow();
            RefreshEquipmentPanel();
            SetEquipmentPanelVisible(false);
            SetLogOverlayVisible(true);
            SetButtonsInteractable(true);
        }

        private void ApplyStatus(string status)
        {
            if (statusText != null)
            {
                statusText.text = status;
            }
        }

        private void ApplyStageInfo()
        {
            if (stageInfoText == null)
            {
                return;
            }

            var isEnglish = LocalizationManager.Instance != null
                && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
            var stage = GameProgressManager.GetCurrentStage();
            var region = GameProgressManager.GetStageTheme(isEnglish, stage);
            stageInfoText.text = isEnglish
                ? "Stage " + stage + " / " + region
                : "\u7b2c" + stage + "\u5173 / " + region;
        }

        private void ApplyBattleLog(string content)
        {
            if (battleLogText == null || battleLogScrollRect == null || battleLogScrollRect.content == null)
            {
                return;
            }

            battleLogText.text = content;
            Canvas.ForceUpdateCanvases();
            RefreshBattleLogContentHeight();

            if (ShouldAutoScrollLog())
            {
                ScrollLogToBottom();
            }
        }

        private void ScrollLogToBottom()
        {
            if (battleLogScrollRect == null)
            {
                return;
            }

            isApplyingAutoScroll = true;
            battleLogScrollRect.verticalNormalizedPosition = 0f;
            isApplyingAutoScroll = false;
        }

        private bool ShouldAutoScrollLog()
        {
            if (battleLogFollowController == null)
            {
                return true;
            }

            return !battleLogFollowController.IsDragging && battleLogFollowController.AutoFollow;
        }

        private void ResetLogFollow()
        {
            if (battleLogFollowController != null)
            {
                battleLogFollowController.ResetToAutoFollow();
            }
        }

        private void RefreshBattleLogContentHeight()
        {
            if (battleLogText == null || battleLogScrollRect == null || battleLogScrollRect.content == null)
            {
                return;
            }

            var contentRect = battleLogScrollRect.content;
            var preferredHeight = battleLogText.preferredHeight;
            var viewportHeight = battleLogScrollRect.viewport != null
                ? battleLogScrollRect.viewport.rect.height
                : 0f;

            var targetHeight = Mathf.Max(preferredHeight + 12f, viewportHeight);
            contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);

            var scrollbar = battleLogScrollRect.verticalScrollbar;
            if (scrollbar != null && viewportHeight > 0f)
            {
                scrollbar.size = Mathf.Clamp01(viewportHeight / Mathf.Max(targetHeight, viewportHeight));
            }
        }

        private void OnBattleLogScrollChanged(Vector2 position)
        {
            if (isApplyingAutoScroll || battleLogFollowController == null)
            {
                return;
            }

            if (position.y > 0.001f)
            {
                battleLogFollowController.DisableAutoFollow();
            }
        }

        private string FormatBattleLogLine(BattleEvent battleEvent)
        {
            var color = GetBattleLogColor(battleEvent);
            var formattedLog = battleEvent != null ? battleEvent.Log : string.Empty;
            formattedLog = HighlightNumbers(formattedLog, battleEvent);
            formattedLog = HighlightKnownNames(formattedLog, battleEvent);
            formattedLog = HighlightSkillNames(formattedLog);
            formattedLog = HighlightBattleMoments(formattedLog, battleEvent);

            if (battleEvent == null)
            {
                return "<color=" + color + ">" + formattedLog + "</color>";
            }

            if (battleEvent.Type == BattleEventType.RoundStart)
            {
                displayedBattleRoundCount++;
                displayedBattleActionCount = 0;
                return "<color=" + color + ">" + RoundDivider + "</color>\n"
                    + "<b><color=" + color + ">" + formattedLog + "</color></b>\n"
                    + "<color=#8E8270>R" + displayedBattleRoundCount + "</color>";
            }

            if (battleEvent.Type == BattleEventType.BattleEnd)
            {
                return "\n<color=" + color + ">" + EndDivider + "</color>\n"
                    + "<b><color=" + color + ">" + formattedLog + "</color></b>\n"
                    + "<color=" + color + ">" + EndDivider + "</color>";
            }

            displayedBattleActionCount++;
            return "<color=#7B6B57>" + displayedBattleActionCount.ToString("00") + "</color> "
                + GetActionTag(battleEvent)
                + ActionPrefix
                + "<color=" + color + ">" + formattedLog + "</color>";
        }

        private static string GetBattleLogColor(BattleEvent battleEvent)
        {
            if (battleEvent == null)
            {
                return "#F0E6D8";
            }

            switch (battleEvent.Type)
            {
                case BattleEventType.RoundStart:
                    return "#F6D28A";
                case BattleEventType.BattleEnd:
                    return battleEvent.IsVictory == true ? "#7EE0A1" : "#FF8D8D";
                case BattleEventType.Action:
                default:
                    return "#F0E6D8";
            }
        }

        private static string GetActionTag(BattleEvent battleEvent)
        {
            if (battleEvent == null || battleEvent.Type != BattleEventType.Action)
            {
                return string.Empty;
            }

            var log = battleEvent.Log ?? string.Empty;
            var fallenToken = LocalizationManager.GetText("battle.log_fallen");
            var castsToken = LocalizationManager.GetText("battle.log_casts");
            var attacksToken = LocalizationManager.GetText("battle.log_attacks");
            var healsToken = LocalizationManager.GetText("battle.log_heals");
            var steadiesToken = LocalizationManager.GetText("battle.log_steadies");

            if (!string.IsNullOrEmpty(fallenToken) && log.Contains(fallenToken))
            {
                return "<color=#FF8D8D>[KO]</color>";
            }

            if (!string.IsNullOrEmpty(castsToken) && log.Contains(castsToken))
            {
                return "<color=#C9A7FF>[SKILL]</color>";
            }

            if (!string.IsNullOrEmpty(attacksToken) && log.Contains(attacksToken))
            {
                return "<color=#F6D28A>[ATK]</color>";
            }

            if (!string.IsNullOrEmpty(healsToken) && log.Contains(healsToken))
            {
                return "<color=#7EE0A1>[HEAL]</color>";
            }

            if (!string.IsNullOrEmpty(steadiesToken) && log.Contains(steadiesToken))
            {
                return "<color=#8FD3FF>[SHIELD]</color>";
            }

            if (log.Contains("MP +"))
            {
                return "<color=#7CB8FF>[MP]</color>";
            }

            return "<color=#AFA08D>[ACT]</color>";
        }

        private string HighlightKnownNames(string content, BattleEvent battleEvent)
        {
            if (string.IsNullOrEmpty(content) || battleEvent == null)
            {
                return content;
            }

            var names = GetKnownUnitNames(battleEvent);
            for (var i = 0; i < names.Count; i++)
            {
                var name = names[i];
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                content = content.Replace(name, "<color=#8FD3FF>" + name + "</color>");
            }

            return content;
        }

        private List<string> GetKnownUnitNames(BattleEvent battleEvent)
        {
            var results = new List<string>();
            CollectNamesFromSummary(results, battleEvent.PlayerTeamSummary);
            CollectNamesFromSummary(results, battleEvent.EnemyTeamSummary);
            results.Sort(delegate(string left, string right)
            {
                return right.Length.CompareTo(left.Length);
            });
            return results;
        }

        private static string HighlightBattleMoments(string content, BattleEvent battleEvent)
        {
            if (string.IsNullOrEmpty(content) || battleEvent == null)
            {
                return content;
            }

            var fallenToken = LocalizationManager.GetText("battle.log_fallen");
            var castsToken = LocalizationManager.GetText("battle.log_casts");
            var attacksToken = LocalizationManager.GetText("battle.log_attacks");
            var healsToken = LocalizationManager.GetText("battle.log_heals");
            var steadiesToken = LocalizationManager.GetText("battle.log_steadies");

            if (!string.IsNullOrEmpty(fallenToken))
            {
                content = content.Replace(fallenToken, "<b><color=#FF8D8D>" + fallenToken + "</color></b>");
            }

            if (!string.IsNullOrEmpty(castsToken))
            {
                content = content.Replace(castsToken, "<b><color=#C9A7FF>" + castsToken + "</color></b>");
            }

            if (!string.IsNullOrEmpty(attacksToken))
            {
                content = content.Replace(attacksToken, "<b><color=#F6D28A>" + attacksToken + "</color></b>");
            }

            if (!string.IsNullOrEmpty(healsToken))
            {
                content = content.Replace(healsToken, "<b><color=#7EE0A1>" + healsToken + "</color></b>");
            }

            if (!string.IsNullOrEmpty(steadiesToken))
            {
                content = content.Replace(steadiesToken, "<b><color=#8FD3FF>" + steadiesToken + "</color></b>");
            }

            content = content.Replace("MP +", "<b><color=#7CB8FF>MP +</color></b>");
            return content;
        }

        private static void CollectNamesFromSummary(List<string> results, string summary)
        {
            if (string.IsNullOrEmpty(summary))
            {
                return;
            }

            var lines = summary.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var hpIndex = line.IndexOf("  HP ");
                var candidate = hpIndex >= 0 ? line.Substring(0, hpIndex).Trim() : line.Trim();
                if (string.IsNullOrEmpty(candidate) || results.Contains(candidate))
                {
                    continue;
                }

                results.Add(candidate);
            }
        }

        private static string HighlightNumbers(string content, BattleEvent battleEvent)
        {
            if (string.IsNullOrEmpty(content))
            {
                return content;
            }

            var numberColor = GetNumberColor(battleEvent);
            return Regex.Replace(content, @"\d+", "<color=" + numberColor + ">$0</color>");
        }

        private static string GetNumberColor(BattleEvent battleEvent)
        {
            if (battleEvent == null)
            {
                return "#F6D28A";
            }

            if (battleEvent.Type == BattleEventType.RoundStart)
            {
                return "#F6D28A";
            }

            var log = battleEvent.Log ?? string.Empty;
            var healToken = LocalizationManager.GetText("battle.log_heals");
            var damageToken = LocalizationManager.GetText("battle.log_damage");

            if (!string.IsNullOrEmpty(healToken) && log.Contains(healToken))
            {
                return "#7EE0A1";
            }

            if (!string.IsNullOrEmpty(damageToken) && log.Contains(damageToken))
            {
                return "#FF7A7A";
            }

            return "#F6D28A";
        }

        private void ShowBattleResultPopup(BattlePlaybackResult playback)
        {
            if (playback == null || UIManager.Instance == null)
            {
                return;
            }

            BattleRewardResult reward = null;
            if (playback.IsVictory)
            {
                reward = GameProgressManager.GrantBattleRewards(GameProgressManager.GetCurrentStage());
                GameProgressManager.PrepareSkillRewardOptions();
                GameProgressManager.MarkCurrentStageCleared();
                if (reward != null && !string.IsNullOrEmpty(reward.DroppedEquipmentId))
                {
                    preferredSelectionEquipmentId = reward.DroppedEquipmentId;
                    EnsureSelectionFocusFromPreferredEquipment();
                }
            }

            GameProgressManager.RecordBattleResult(playback.IsVictory, GameProgressManager.GetCurrentStage(), playback.TotalRounds);
            StoreBattleResult(playback, reward);
            OpenStoredBattleResultPopup();
        }

        private void OpenStoredBattleResultPopup()
        {
            if (storedBattleResultPlayback == null || UIManager.Instance == null)
            {
                return;
            }

            var isEnglish = LocalizationManager.Instance != null
                && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
            canReopenBattleResultPopup = false;
            UpdateBackButtonStateLabel();

            if (storedBattleResultPlayback.IsVictory)
            {
                ShowVictoryRewardChoicePopup(storedBattleResultPlayback, storedBattleResultReward, isEnglish);
                return;
            }

            ShowDefeatResultPopup(storedBattleResultPlayback, isEnglish);
        }

        private void ShowDefeatResultPopup(BattlePlaybackResult playback, bool isEnglish)
        {
            var popup = UIManager.Instance.ShowPopup<UIConfirmPopup>("Confirm");
            if (popup == null)
            {
                return;
            }

            popup.SetupChoices(
                LocalizationManager.GetText("battle.status_defeat"),
                BuildDefeatMessage(playback, isEnglish),
                new List<string>
                {
                    isEnglish ? "Back To Main Menu" : "\u8fd4\u56de\u4e3b\u83dc\u5355",
                    LocalizationManager.GetText("battle.button_review")
                },
                new List<System.Action>
                {
                    delegate
                    {
                        ClearStoredBattleResult();
                        GameProgressManager.ResetRun();
                        UIManager.Instance.ShowPage("MainMenu");
                    },
                    delegate
                    {
                        MarkBattleResultAsReviewable();
                    }
                });
        }

        private void ShowVictoryRewardChoicePopup(BattlePlaybackResult playback, BattleRewardResult reward, bool isEnglish)
        {
            var options = GameProgressManager.GetPendingSkillRewardOptions();
            if (options.Count == 0)
            {
                ClearStoredBattleResult();
                HandlePostVictoryNavigation(isEnglish);
                return;
            }

            var popup = UIManager.Instance.ShowPopup<UIConfirmPopup>("Confirm");
            if (popup == null)
            {
                ClearStoredBattleResult();
                HandlePostVictoryNavigation(isEnglish);
                return;
            }

            var labels = new List<string>();
            var actions = new List<System.Action>();
            for (var i = 0; i < options.Count && i < 3; i++)
            {
                var capturedIndex = i;
                var option = options[i];
                labels.Add(BuildSkillRewardChoiceLabel(option, isEnglish));
                actions.Add(delegate
                {
                    var appliedOption = GameProgressManager.ApplyPendingSkillReward(capturedIndex);
                    if (appliedOption != null)
                    {
                        UIManager.Instance.ShowToast(BuildSkillRewardToast(appliedOption, isEnglish), 1.8f);
                    }

                    ClearStoredBattleResult();
                    HandlePostVictoryNavigation(isEnglish);
                });
            }

            labels.Add(LocalizationManager.GetText("battle.button_review"));
            actions.Add(delegate
            {
                MarkBattleResultAsReviewable();
            });

            var message = new StringBuilder();
            message.Append(BuildVictoryChoiceMessage(playback, reward, isEnglish));
            for (var i = 0; i < options.Count && i < 3; i++)
            {
                message.Append("\n\n");
                message.Append(isEnglish ? "Option " : "选项").Append(i + 1).Append(isEnglish ? "\n" : "\n");
                message.Append(BuildSkillRewardOptionText(options[i], isEnglish));
            }

            popup.SetupChoices(
                BuildVictoryChoiceTitle(isEnglish),
                BuildVictoryChoiceMessage(playback, reward, isEnglish),
                labels,
                actions);
        }

        private void StoreBattleResult(BattlePlaybackResult playback, BattleRewardResult reward)
        {
            storedBattleResultPlayback = playback;
            storedBattleResultReward = reward;
            canReopenBattleResultPopup = false;
            UpdateBackButtonStateLabel();
        }

        private void MarkBattleResultAsReviewable()
        {
            canReopenBattleResultPopup = storedBattleResultPlayback != null;
            UpdateBackButtonStateLabel();
        }

        private bool CanReopenBattleResultPopup()
        {
            return !openedForEquipment && canReopenBattleResultPopup && storedBattleResultPlayback != null;
        }

        private void ClearStoredBattleResult()
        {
            storedBattleResultPlayback = null;
            storedBattleResultReward = null;
            canReopenBattleResultPopup = false;
            UpdateBackButtonStateLabel();
        }

        private void HandlePostVictoryNavigation(bool isEnglish)
        {
            ClearStoredBattleResult();
            if (GameProgressManager.GetCurrentStage() >= GameProgressManager.GetMaxStage())
            {
                var popup = UIManager.Instance.ShowPopup<UIConfirmPopup>("Confirm");
                if (popup == null)
                {
                    GameProgressManager.ResetRun();
                    UIManager.Instance.ShowPage("MainMenu");
                    return;
                }

                popup.Setup(
                    isEnglish ? "Route Complete" : "\u8def\u7ebf\u5b8c\u6210",
                    isEnglish
                        ? "You have cleared the final boss of this route. The current run will be closed and return to the main menu."
                        : "\u4f60\u5df2\u51fb\u8d25\u5f53\u524d\u8def\u7ebf\u7684\u6700\u7ec8\u9996\u9886\u3002\u672c\u8f6e\u5c06\u5b8c\u6210\u7ed3\u7b97\uff0c\u5e76\u8fd4\u56de\u4e3b\u83dc\u5355\u3002",
                    false,
                    delegate
                    {
                        GameProgressManager.ResetRun();
                        UIManager.Instance.ShowPage("MainMenu");
                    },
                    null,
                    isEnglish ? "Finish Run" : "\u7ed3\u675f\u672c\u8f6e",
                    null);
                return;
            }

            UIManager.Instance.ShowPage("Map");
        }

        private static string BuildVictoryChoiceTitle(bool isEnglish)
        {
            return isEnglish ? "Battle Victory" : "\u6218\u6597\u80dc\u5229";
        }

        private static string BuildVictoryChoiceMessage(BattlePlaybackResult playback, BattleRewardResult reward, bool isEnglish)
        {
            var stageText = isEnglish
                ? "Stage " + GameProgressManager.GetCurrentStage()
                : "\u7b2c " + GameProgressManager.GetCurrentStage() + " \u5173";
            return stageText + "\n"
                + LocalizationManager.GetText("battle.result_rounds") + ": " + playback.TotalRounds + "\n\n"
                + BuildRewardSummary(reward, isEnglish) + "\n\n"
                + (isEnglish ? "Choose one reward. After choosing, return to the map." : "\u8bf7\u9009\u62e9\u4e00\u9879\u529f\u6cd5\u673a\u7f18\uff0c\u9009\u62e9\u540e\u5c06\u76f4\u63a5\u8fd4\u56de\u5730\u56fe\u3002");
        }
        private static string BuildDefeatMessage(BattlePlaybackResult playback, bool isEnglish)
        {
            if (isEnglish)
            {
                return "Run Failed\n\nStage: " + GameProgressManager.GetCurrentStage()
                    + "\nRounds: " + (playback != null ? playback.TotalRounds : 0)
                    + "\nResult: Defeat"
                    + "\n\nThe current run has ended and will return to the main menu.";
            }

            return "\u672c\u8f6e\u5931\u8d25\n\n\u5173\u5361\uff1a\u7b2c " + GameProgressManager.GetCurrentStage() + " \u5173"
                + "\n\u56de\u5408\u6570\uff1a" + (playback != null ? playback.TotalRounds : 0)
                + "\n\u7ed3\u679c\uff1a\u6218\u8d25"
                + "\n\n\u5f53\u524d\u672c\u8f6e\u5df2\u7ed3\u675f\uff0c\u5373\u5c06\u8fd4\u56de\u4e3b\u83dc\u5355\u3002";
        }
        private static string BuildRetreatMessage(bool isEnglish)
        {
            var currentStage = Mathf.Max(1, GameProgressManager.GetCurrentStage());
            var retreatStage = Mathf.Max(1, currentStage - 1);
            if (isEnglish)
            {
                return "Current Stage: " + currentStage
                    + "\nAfter retreat: Stage " + retreatStage
                    + "\nPenalty: lose current battle progress and return to the map."
                    + "\n\nConfirm retreat?";
            }

            return "\u5f53\u524d\u5173\u5361\uff1a\u7b2c " + currentStage + " \u5173"
                + "\n\u64a4\u9000\u540e\uff1a\u7b2c " + retreatStage + " \u5173"
                + "\n\u4ee3\u4ef7\uff1a\u653e\u5f03\u672c\u573a\u6218\u6597\u8fdb\u5ea6\uff0c\u5e76\u8fd4\u56de\u5730\u56fe\u3002"
                + "\n\n\u662f\u5426\u786e\u8ba4\u64a4\u9000\uff1f";
        }
        private static string BuildSkillRewardOptionText(SkillRewardOption option, bool isEnglish)
        {
            return GameProgressManager.BuildSkillRewardDetail(option, isEnglish);
        }

        private static string BuildSkillRewardChoiceLabel(SkillRewardOption option, bool isEnglish)
        {
            return GameProgressManager.BuildSkillRewardChoiceText(option, isEnglish);
        }

        private static string BuildSkillRewardToast(SkillRewardOption option, bool isEnglish)
        {
            if (option == null)
            {
                return string.Empty;
            }

            return isEnglish
                ? option.CharacterName + (option.IsUpgrade ? " upgraded " : " learned ") + option.SkillName + " to Lv." + option.ResultLevel + "."
                : option.CharacterName + (option.IsUpgrade ? " \u5c06 " : " \u5b66\u4f1a ") + option.SkillName + (option.IsUpgrade ? " \u63d0\u5347\u81f3 Lv." + option.ResultLevel + "\u3002" : "\u3002");
        }
        private static string BuildRewardSummary(BattleRewardResult reward, bool isEnglish)
        {
            if (reward == null)
            {
                return isEnglish
                    ? "Rewards: none."
                    : "\u5956\u52b1\uff1a\u65e0\u3002";
            }

            var builder = new StringBuilder();
            if (isEnglish)
            {
                builder.Append("Rewards\n")
                    .Append("Exp +").Append(reward.ExpGained)
                    .Append(" / ").Append(GameProgressManager.BuildSpiritStoneGainText(reward, true, false));

                if (reward.LevelsGained > 0)
                {
                    builder.Append("\nLevel Up +").Append(reward.LevelsGained);
                }

                if (!string.IsNullOrEmpty(reward.DroppedEquipmentName))
                {
                    builder.Append("\nEquipment Drop: ").Append(reward.DroppedEquipmentName);
                }

                return builder.ToString();
            }

            builder.Append("\u6218\u5229\u54c1\n")
                .Append("\u7ecf\u9a8c +").Append(reward.ExpGained)
                .Append(" / ").Append(GameProgressManager.BuildSpiritStoneGainText(reward, false, false));

            if (reward.LevelsGained > 0)
            {
                builder.Append("\n\u4fee\u4e3a\u63d0\u5347 +").Append(reward.LevelsGained);
            }

            if (!string.IsNullOrEmpty(reward.DroppedEquipmentName))
            {
                builder.Append("\n\u88c5\u5907\u6389\u843d\uff1a").Append(reward.DroppedEquipmentName);
            }

            return builder.ToString();
        }
        private void RefreshEquipmentPanel()
        {
            if (equipmentDetailText == null)
            {
                return;
            }

            EnsureSelectionFocusFromPreferredEquipment();

            var unitCount = BattleManager.GetPlayerEquipmentUnitCount();
            if (unitCount > 0)
            {
                selectedEquipmentUnitIndex = Mathf.Clamp(selectedEquipmentUnitIndex, 0, unitCount - 1);
            }
            else
            {
                selectedEquipmentUnitIndex = 0;
            }

            equipmentDetailText.text = BuildSelectedEquipmentDetailText();
            RefreshEquipmentEditorButtonTexts();
            RefreshEquipmentSelectionList();
        }

        private string HighlightSkillNames(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return content;
            }

            var skillNames = GetKnownSkillNames();
            for (var i = 0; i < skillNames.Count; i++)
            {
                var skillName = skillNames[i];
                if (string.IsNullOrEmpty(skillName))
                {
                    continue;
                }

                content = content.Replace(skillName, "<color=#C9A7FF>" + skillName + "</color>");
            }

            return content;
        }

        private List<string> GetKnownSkillNames()
        {
            if (knownSkillNames != null)
            {
                return knownSkillNames;
            }

            knownSkillNames = new List<string>();
            var database = SkillDatabaseLoader.Load();
            if (database == null || database.Skills == null)
            {
                return knownSkillNames;
            }

            for (var i = 0; i < database.Skills.Count; i++)
            {
                var skill = database.Skills[i];
                if (skill == null || string.IsNullOrEmpty(skill.Name) || knownSkillNames.Contains(skill.Name))
                {
                    continue;
                }

                knownSkillNames.Add(skill.Name);
            }

            knownSkillNames.Sort(delegate(string left, string right)
            {
                return right.Length.CompareTo(left.Length);
            });

            return knownSkillNames;
        }

        private void InitializeTeamCardTemplate(Button template, List<Button> targetButtons)
        {
            if (template == null || targetButtons == null)
            {
                return;
            }

            template.gameObject.SetActive(false);
            if (!targetButtons.Contains(template))
            {
                targetButtons.Add(template);
            }
        }

        private void RefreshTeamCardsFromSummary(string teamSummary, string equipmentSummary, bool playerSide, RectTransform root, Button template, List<Button> buttons)
        {
            if (root == null || template == null || buttons == null)
            {
                return;
            }

            for (var i = 0; i < buttons.Count; i++)
            {
                if (buttons[i] != null)
                {
                    buttons[i].gameObject.SetActive(false);
                }
            }

            var cards = BuildUnitCardsFromSummary(teamSummary, equipmentSummary, playerSide);
            for (var i = 0; i < cards.Count; i++)
            {
                var button = GetOrCreateTeamCardButton(root, template, buttons, i);
                BindTeamCard(button, cards[i]);
                var rect = button.GetComponent<RectTransform>();
                if (rect != null)
                {
                    var row = i / 2;
                    var column = i % 2;
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition = new Vector2(column * (UICardChromeUtility.StandardCardWidth + 16f), -row * (UICardChromeUtility.StandardCardHeight + 16f));
                    rect.sizeDelta = new Vector2(UICardChromeUtility.StandardCardWidth, UICardChromeUtility.StandardCardHeight);
                }
            }

            var rootRows = Mathf.CeilToInt(cards.Count / 2f);
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, UICardChromeUtility.StandardCardWidth * 2f + 16f);
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(UICardChromeUtility.StandardCardHeight, rootRows * UICardChromeUtility.StandardCardHeight + Mathf.Max(0, rootRows - 1) * 16f));
        }

        private void ConfigureBattleSummaryLayout()
        {
            ConfigureTeamPanelLayout(playerCardRoot, playerEquipmentText);
            ConfigureTeamPanelLayout(enemyCardRoot, enemyEquipmentText);
        }

        private static void ConfigureTeamPanelLayout(RectTransform cardRoot, Text equipmentText)
        {
            if (cardRoot != null)
            {
                var bodyRect = cardRoot.parent as RectTransform;
                if (bodyRect != null)
                {
                    bodyRect.anchorMin = new Vector2(0.04f, 0.06f);
                    bodyRect.anchorMax = new Vector2(0.96f, 0.78f);
                    bodyRect.offsetMin = Vector2.zero;
                    bodyRect.offsetMax = Vector2.zero;
                }
            }

            if (equipmentText != null)
            {
                var equipmentBody = equipmentText.transform.parent as RectTransform;
                if (equipmentBody != null)
                {
                    equipmentBody.gameObject.SetActive(false);
                    var panel = equipmentBody.parent as RectTransform;
                    if (panel != null)
                    {
                        var titleNode = panel.Find("EquipmentTitle");
                        if (titleNode != null)
                        {
                            titleNode.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        private static Button GetOrCreateTeamCardButton(RectTransform root, Button template, List<Button> buttons, int index)
        {
            while (buttons.Count <= index)
            {
                var clone = Instantiate(template.gameObject, root, false);
                clone.name = template.name + "_" + buttons.Count;
                clone.SetActive(false);
                buttons.Add(clone.GetComponent<Button>());
            }

            return buttons[index];
        }

        private void BindTeamCard(Button button, UICardData card)
        {
            if (button == null || card == null)
            {
                return;
            }

            button.gameObject.SetActive(true);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate { OpenCardDetail(card); });

            UICardChromeUtility.Apply(button, card.BorderColor, false);

            var title = button.transform.Find("TitleText")?.GetComponent<Text>();
            if (title != null)
            {
                title.text = card.Title;
                title.supportRichText = true;
            }

            var subtitle = button.transform.Find("SubtitleText")?.GetComponent<Text>();
            if (subtitle != null)
            {
                subtitle.text = card.Subtitle;
            }

            var progressRoot = button.transform.Find("ProgressRoot");
            if (progressRoot != null)
            {
                progressRoot.gameObject.SetActive(card.ShowProgress);
            }

            var progressFill = button.transform.Find("ProgressRoot/Fill")?.GetComponent<Image>();
            if (progressFill != null)
            {
                progressFill.fillAmount = Mathf.Clamp01(card.ProgressCurrent / (float)Mathf.Max(1, card.ProgressMax));
                progressFill.color = card.BorderColor;
            }

            var progressLabel = button.transform.Find("ProgressRoot/ProgressLabel")?.GetComponent<Text>();
            if (progressLabel != null)
            {
                progressLabel.text = card.ProgressLabel ?? string.Empty;
                progressLabel.gameObject.SetActive(card.ShowProgress);
            }
        }

        private List<UICardData> BuildUnitCardsFromSummary(string teamSummary, string equipmentSummary, bool playerSide)
        {
            var cards = new List<UICardData>();
            if (string.IsNullOrWhiteSpace(teamSummary))
            {
                return cards;
            }

            var lines = teamSummary.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i] != null ? lines[i].Trim() : string.Empty;
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                var match = Regex.Match(line, "^(?<name>.+?)\\s+HP\\s+(?<hp>\\d+)\\/(?<max>\\d+)\\s+MP\\s+(?<mp>\\d+)\\/(?<mpmax>\\d+)");
                if (!match.Success)
                {
                    continue;
                }

                var name = match.Groups["name"].Value.Trim();
                var currentHp = int.Parse(match.Groups["hp"].Value);
                var maxHp = int.Parse(match.Groups["max"].Value);
                var currentMp = int.Parse(match.Groups["mp"].Value);
                var maxMp = int.Parse(match.Groups["mpmax"].Value);
                var level = playerSide ? GameProgressManager.GetCultivationLevel() : Mathf.Max(1, GameProgressManager.GetCurrentStage());
                var element = ResolveUnitElementByName(name, playerSide);
                var detail = new StringBuilder();
                detail.Append("\u7b49\u7ea7\uff1aLv.").Append(level)
                    .Append('\n').Append("\u4e94\u884c\uff1a").Append(string.IsNullOrEmpty(element) ? "\u65e0" : element)
                    .Append('\n').Append("\u751f\u547d\uff1a").Append(currentHp).Append('/').Append(maxHp)
                    .Append('\n').Append("\u6cd5\u529b\uff1a").Append(currentMp).Append('/').Append(maxMp);

                var equipmentLine = FindEquipmentLine(name, equipmentSummary);
                if (!string.IsNullOrEmpty(equipmentLine))
                {
                    detail.Append('\n').Append("\u88c5\u5907\uff1a").Append(equipmentLine);
                }

                var skillLine = BuildUnitSkillSummary(name, playerSide);
                if (!string.IsNullOrEmpty(skillLine))
                {
                    detail.Append('\n').Append("\u529f\u6cd5\uff1a").Append(skillLine);
                }

                cards.Add(new UICardData
                {
                    Id = (playerSide ? "Player:" : "Enemy:") + name,
                    Title = name,
                    Subtitle = "Lv." + level,
                    DetailTitle = name,
                    DetailBody = detail.ToString(),
                    ShowProgress = true,
                    ProgressCurrent = currentHp,
                    ProgressMax = maxHp,
                    ProgressLabel = "HP " + currentHp + "/" + maxHp,
                    BorderColor = UIElementPalette.GetBorderColor(element)
                });
            }

            return cards;
        }

        private static string ResolveUnitElementByName(string unitName, bool playerSide)
        {
            if (playerSide)
            {
                var database = CharacterDatabaseLoader.Load();
                if (database != null && database.Characters != null)
                {
                    for (var i = 0; i < database.Characters.Count; i++)
                    {
                        var character = database.Characters[i];
                        if (character != null && string.Equals(character.Name, unitName, System.StringComparison.OrdinalIgnoreCase))
                        {
                            return character.ElementRoots;
                        }
                    }
                }
            }
            else
            {
                var database = EnemyDatabaseLoader.Load();
                if (database != null && database.Enemies != null)
                {
                    for (var i = 0; i < database.Enemies.Count; i++)
                    {
                        var enemy = database.Enemies[i];
                        if (enemy != null && string.Equals(enemy.Name, unitName, System.StringComparison.OrdinalIgnoreCase))
                        {
                            return enemy.Element;
                        }
                    }
                }
            }

            return "None";
        }

        private static string FindEquipmentLine(string unitName, string equipmentSummary)
        {
            if (string.IsNullOrWhiteSpace(equipmentSummary))
            {
                return string.Empty;
            }

            var lines = equipmentSummary.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i] != null ? lines[i].Trim() : string.Empty;
                if (line.StartsWith(unitName + ":", System.StringComparison.OrdinalIgnoreCase))
                {
                    return line.Substring(unitName.Length + 1).Trim();
                }
            }

            return string.Empty;
        }

        private static string BuildUnitSkillSummary(string unitName, bool playerSide)
        {
            var skillDatabase = SkillDatabaseLoader.Load();
            if (skillDatabase == null)
            {
                return string.Empty;
            }

            if (playerSide)
            {
                var characterDatabase = CharacterDatabaseLoader.Load();
                if (characterDatabase == null || characterDatabase.Characters == null)
                {
                    return string.Empty;
                }

                for (var i = 0; i < characterDatabase.Characters.Count; i++)
                {
                    var character = characterDatabase.Characters[i];
                    if (character == null || !string.Equals(character.Name, unitName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var skillIds = new List<string>();
                    if (!string.IsNullOrEmpty(character.InitialSkills))
                    {
                        var initial = character.InitialSkills.Split(',');
                        for (var j = 0; j < initial.Length; j++)
                        {
                            var trimmed = initial[j].Trim();
                            if (!string.IsNullOrEmpty(trimmed) && !skillIds.Contains(trimmed))
                            {
                                skillIds.Add(trimmed);
                            }
                        }
                    }

                    var learned = GameProgressManager.GetLearnedSkillIds(character.Id);
                    for (var j = 0; j < learned.Count; j++)
                    {
                        if (!string.IsNullOrEmpty(learned[j]) && !skillIds.Contains(learned[j]))
                        {
                            skillIds.Add(learned[j]);
                        }
                    }

                    return BuildSkillNameSummary(skillIds, skillDatabase);
                }
            }
            else
            {
                var enemyDatabase = EnemyDatabaseLoader.Load();
                if (enemyDatabase == null || enemyDatabase.Enemies == null)
                {
                    return string.Empty;
                }

                for (var i = 0; i < enemyDatabase.Enemies.Count; i++)
                {
                    var enemy = enemyDatabase.Enemies[i];
                    if (enemy == null || !string.Equals(enemy.Name, unitName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var skillIds = new List<string>();
                    if (!string.IsNullOrEmpty(enemy.Skills))
                    {
                        var split = enemy.Skills.Split(',');
                        for (var j = 0; j < split.Length; j++)
                        {
                            var trimmed = split[j].Trim();
                            if (!string.IsNullOrEmpty(trimmed) && !skillIds.Contains(trimmed))
                            {
                                skillIds.Add(trimmed);
                            }
                        }
                    }

                    return BuildSkillNameSummary(skillIds, skillDatabase);
                }
            }

            return string.Empty;
        }

        private static string BuildSkillNameSummary(List<string> skillIds, SkillDatabase skillDatabase)
        {
            var names = new List<string>();
            for (var i = 0; i < skillIds.Count; i++)
            {
                var skill = skillDatabase.GetById(skillIds[i]);
                if (skill != null && !string.IsNullOrEmpty(skill.Name))
                {
                    names.Add(skill.Name);
                }
            }

            return names.Count > 0 ? string.Join(" / ", names.ToArray()) : string.Empty;
        }

        private void OpenCardDetail(UICardData card)
        {
            var popup = UIManager.Instance.ShowPopup<UIConfirmPopup>("Confirm");
            if (popup == null)
            {
                return;
            }

            popup.Setup(
                card != null ? card.DetailTitle : string.Empty,
                card != null ? card.DetailBody : string.Empty,
                false,
                null,
                null,
                LocalizationManager.GetText("common.button_close"),
                null);
        }

        private void SetLogOverlayVisible(bool visible)
        {
            if (battleLogOverlay != null)
            {
                battleLogOverlay.SetActive(visible);
            }
        }

        private void SetEquipmentPanelVisible(bool visible)
        {
            if (equipmentPanel != null)
            {
                equipmentPanel.SetActive(visible);
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (backButton != null)
            {
                backButton.interactable = true;
            }
        }

        private void OnLanguageChanged()
        {
            RefreshPreview();
            UpdateBackButtonStateLabel();

            if (equipmentPanel != null && equipmentPanel.activeSelf)
            {
                RefreshEquipmentPanel();
                SetEquipmentPanelVisible(true);
            }
        }

        private void UpdateBackButtonStateLabel()
        {
            if (openedForEquipment)
            {
                SetBackButtonLabel(LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English
                    ? "Back To Map"
                    : "\u8fd4\u56de\u5730\u56fe");
                return;
            }

            if (CanReopenBattleResultPopup())
            {
                SetBackButtonLabel(LocalizationManager.GetText("battle.button_open_result"));
                return;
            }

            SetBackButtonLabel(LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English
                ? "Flee"
                : "\u9003\u8dd1");
        }

        private void SetBackButtonLabel(string text)
        {
            if (backButton == null)
            {
                return;
            }

            var label = backButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = text;
            }
        }

        private static float GetEventDelay(BattleEvent battleEvent)
        {
            if (battleEvent == null)
            {
                return 0.18f;
            }

            switch (battleEvent.Type)
            {
                case BattleEventType.RoundStart:
                    return 0.42f;
                case BattleEventType.BattleEnd:
                    return 0.72f;
                case BattleEventType.Action:
                default:
                    var log = battleEvent.Log ?? string.Empty;
                    var fallenToken = LocalizationManager.GetText("battle.log_fallen");
                    var castsToken = LocalizationManager.GetText("battle.log_casts");
                    var healsToken = LocalizationManager.GetText("battle.log_heals");
                    var steadiesToken = LocalizationManager.GetText("battle.log_steadies");

                    if (!string.IsNullOrEmpty(fallenToken) && log.Contains(fallenToken))
                    {
                        return 0.42f;
                    }

                    if (!string.IsNullOrEmpty(castsToken) && log.Contains(castsToken))
                    {
                        return 0.28f;
                    }

                    if (!string.IsNullOrEmpty(healsToken) && log.Contains(healsToken))
                    {
                        return 0.24f;
                    }

                    if (!string.IsNullOrEmpty(steadiesToken) && log.Contains(steadiesToken))
                    {
                        return 0.24f;
                    }

                    return 0.18f;
            }
        }

        private void CycleSelectedEquipmentSlot(string slot)
        {
            BattleManager.CyclePlayerEquipmentForUnitIndexSlot(selectedEquipmentUnitIndex, slot);
            RefreshPreview();
            RefreshEquipmentPanel();
            SetEquipmentPanelVisible(true);
        }

        private void OpenEquipmentSelection(string slot)
        {
            selectingSlot = slot;
            RefreshEquipmentSelectionList();
            SetEquipmentPanelVisible(true);
        }

        private void EnsureSelectionFocusFromPreferredEquipment()
        {
            if (string.IsNullOrEmpty(preferredSelectionEquipmentId))
            {
                if (string.IsNullOrEmpty(selectingSlot))
                {
                    selectingSlot = "Weapon";
                }

                return;
            }

            var equipmentDatabase = EquipmentDatabaseLoader.Load();
            if (equipmentDatabase == null)
            {
                return;
            }

            var equipment = equipmentDatabase.GetById(preferredSelectionEquipmentId);
            if (equipment != null && !string.IsNullOrEmpty(equipment.Slot))
            {
                selectingSlot = equipment.Slot;
            }
        }

        private string ResolveSelectionSlot()
        {
            EnsureSelectionFocusFromPreferredEquipment();
            return string.IsNullOrEmpty(selectingSlot) ? "Weapon" : selectingSlot;
        }

        private string BuildSelectionTitle(string slot, int count)
        {
            var isEnglish = LocalizationManager.Instance != null
                && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
            var suffix = isEnglish ? "Backpack" : "\u80cc\u5305";
            return GetSlotDisplayName(slot) + " " + suffix + " (" + count + ")";
        }

        private void SortEquipmentsForDisplay(List<EquipmentConfig> equipments)
        {
            if (equipments == null || equipments.Count <= 1)
            {
                return;
            }

            equipments.Sort(delegate(EquipmentConfig left, EquipmentConfig right)
            {
                var leftIsPreferred = left != null && left.Id == preferredSelectionEquipmentId;
                var rightIsPreferred = right != null && right.Id == preferredSelectionEquipmentId;
                if (leftIsPreferred != rightIsPreferred)
                {
                    return leftIsPreferred ? -1 : 1;
                }

                var leftScore = GetEquipmentDisplayScore(left);
                var rightScore = GetEquipmentDisplayScore(right);
                if (leftScore != rightScore)
                {
                    return rightScore.CompareTo(leftScore);
                }

                var leftName = left != null ? left.Name : string.Empty;
                var rightName = right != null ? right.Name : string.Empty;
                return string.Compare(leftName, rightName, System.StringComparison.OrdinalIgnoreCase);
            });
        }

        private string BuildEquipmentOptionLabel(EquipmentConfig equipment)
        {
            if (equipment == null)
            {
                return LocalizationManager.GetText("battle.equipment_none");
            }

            return equipment.Name;
        }

        private static int GetEquipmentDisplayScore(EquipmentConfig equipment)
        {
            if (equipment == null)
            {
                return int.MinValue;
            }

            return equipment.HP + equipment.MP + equipment.ATK * 2 + equipment.DEF * 2;
        }

        private struct SlotButtonData
        {
            public SlotButtonData(string slot, float leftOffset)
            {
                Slot = slot;
                LeftOffset = leftOffset;
            }

            public string Slot;
            public float LeftOffset;
        }

        private void RefreshEquipmentSelectionList()
        {
            const float cardSpacingX = 16f;
            const float cardSpacingY = 16f;
            const float cardStepY = UICardChromeUtility.StandardCardHeight + cardSpacingY;
            const float secondColumnX = UICardChromeUtility.StandardCardWidth + cardSpacingX;
            const float thirdColumnX = secondColumnX * 2f;
            const float fourthColumnX = secondColumnX * 3f;

            if (equipmentSelectionContent == null || equipmentSelectionTitleText == null)
            {
                return;
            }

            for (var i = 0; i < equipmentSelectionButtons.Count; i++)
            {
                if (equipmentSelectionButtons[i] != null)
                {
                    equipmentSelectionButtons[i].gameObject.SetActive(false);
                }
            }

            var slot = ResolveSelectionSlot();

            var equipments = BattleManager.GetOwnedEquipmentsForSlot(slot);
            SortEquipmentsForDisplay(equipments);
            equipmentSelectionTitleText.text = BuildSelectionTitle(slot, equipments.Count);
            RefreshEquipmentSelectionViewportLayout(equipments.Count + 7);

            var slotButtons = new[]
            {
                new SlotButtonData("Weapon", 0f),
                new SlotButtonData("Armor", secondColumnX),
                new SlotButtonData("Accessory", thirdColumnX)
            };

            for (var slotIndex = 0; slotIndex < slotButtons.Length; slotIndex++)
            {
                var slotButtonData = slotButtons[slotIndex];
                var slotButton = GetOrCreateEquipmentSelectionButton(slotIndex);
                slotButton.gameObject.SetActive(true);
                slotButton.interactable = true;
                ConfigureEquipmentSelectionButtonRect(slotButton.GetComponent<RectTransform>(), slotButtonData.LeftOffset, 0f);
                slotButton.onClick.RemoveAllListeners();
                var capturedSlot = slotButtonData.Slot;
                slotButton.onClick.AddListener(delegate
                {
                    selectingSlot = capturedSlot;
                    preferredSelectionEquipmentId = null;
                    RefreshEquipmentPanel();
                    SetEquipmentPanelVisible(true);
                });

                var slotLabel = slotButton.GetComponentInChildren<Text>();
                if (slotLabel != null)
                {
                    slotLabel.fontSize = 20;
                    slotLabel.alignment = TextAnchor.UpperLeft;
                }
                SetSelectionCardTexts(slotButton, GetSlotDisplayName(slotButtonData.Slot), BattleManager.GetPlayerEquipmentName(selectedEquipmentUnitIndex, slotButtonData.Slot));

                ApplyEquipmentCardButton(slotButton, Color.white, string.Equals(slotButtonData.Slot, slot, System.StringComparison.OrdinalIgnoreCase));
            }

            var unequipButton = GetOrCreateEquipmentSelectionButton(3);
            unequipButton.gameObject.SetActive(true);
            unequipButton.interactable = true;
            ConfigureEquipmentSelectionButtonRect(unequipButton.GetComponent<RectTransform>(), 0f, cardStepY);
            unequipButton.onClick.RemoveAllListeners();
            unequipButton.onClick.AddListener(delegate
            {
                BattleManager.UnequipPlayerEquipmentForUnitIndexSlot(selectedEquipmentUnitIndex, slot);
                preferredSelectionEquipmentId = null;
                RefreshPreview();
                RefreshEquipmentPanel();
                SetEquipmentPanelVisible(true);
            });
            var unequipLabel = unequipButton.GetComponentInChildren<Text>();
            if (unequipLabel != null)
            {
                unequipLabel.fontSize = 20;
                unequipLabel.alignment = TextAnchor.UpperLeft;
            }
            SetSelectionCardTexts(unequipButton, BuildUnequipLabel(), GetSlotDisplayName(slot));
            ApplyEquipmentCardButton(unequipButton, Color.white, false);

            if (equipments.Count == 0)
            {
                var emptyButton = GetOrCreateEquipmentSelectionButton(4);
                emptyButton.gameObject.SetActive(true);
                emptyButton.interactable = false;
                ConfigureEquipmentSelectionButtonRect(emptyButton.GetComponent<RectTransform>(), secondColumnX, cardStepY);
                emptyButton.onClick.RemoveAllListeners();
                var emptyLabel = emptyButton.GetComponentInChildren<Text>();
                if (emptyLabel != null)
                {
                    emptyLabel.fontSize = 20;
                    emptyLabel.alignment = TextAnchor.UpperLeft;
                }
                SetSelectionCardTexts(emptyButton, BuildEmptyBackpackLabel(), string.Empty);
                ApplyEquipmentCardButton(emptyButton, Color.white, false);

                equipmentSelectionContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fourthColumnX + UICardChromeUtility.StandardCardWidth);
                equipmentSelectionContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cardStepY + UICardChromeUtility.StandardCardHeight);
                return;
            }

            for (var i = 0; i < equipments.Count; i++)
            {
                var equipment = equipments[i];
                if (equipment == null)
                {
                    continue;
                }

                var button = GetOrCreateEquipmentSelectionButton(i + 4);
                button.gameObject.SetActive(true);
                button.interactable = true;
                var rect = button.GetComponent<RectTransform>();
                var row = i / 4 + 2;
                var column = i % 4;
                var leftOffset = column == 0
                    ? 0f
                    : column == 1
                        ? secondColumnX
                        : column == 2
                            ? thirdColumnX
                            : fourthColumnX;
                ConfigureEquipmentSelectionButtonRect(rect, leftOffset, row * cardStepY);

                var capturedEquipmentId = equipment.Id;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(delegate
                {
                    BattleManager.EquipOwnedItemForUnitIndex(selectedEquipmentUnitIndex, capturedEquipmentId);
                    preferredSelectionEquipmentId = capturedEquipmentId;
                    RefreshPreview();
                    RefreshEquipmentPanel();
                    SetEquipmentPanelVisible(true);
                });

                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.fontSize = 20;
                    label.alignment = TextAnchor.MiddleLeft;
                }
                SetSelectionCardTexts(button, BuildEquipmentOptionLabel(equipment), GetSlotDisplayName(equipment.Slot));
                ApplyEquipmentCardButton(button, Color.white, false);

            }

            var rows = Mathf.CeilToInt(equipments.Count / 4f) + 2;
            equipmentSelectionContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fourthColumnX + UICardChromeUtility.StandardCardWidth);
            equipmentSelectionContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(UICardChromeUtility.StandardCardHeight, rows * UICardChromeUtility.StandardCardHeight + Mathf.Max(0, rows - 1) * cardSpacingY));
        }

        private string BuildUnequipLabel()
        {
            var isEnglish = LocalizationManager.Instance != null
                && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
            return isEnglish ? "Unequip" : "\u5378\u4e0b\u88c5\u5907";
        }

        private string BuildEmptyBackpackLabel()
        {
            var isEnglish = LocalizationManager.Instance != null
                && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
            return isEnglish ? "No other equipment in backpack." : "\u80cc\u5305\u4e2d\u6ca1\u6709\u5176\u4ed6\u53ef\u66ff\u6362\u88c5\u5907\u3002";
        }

        private string BuildSelectedEquipmentDetailText()
        {
            var slot = ResolveSelectionSlot();
            var equipmentName = BattleManager.GetPlayerEquipmentName(selectedEquipmentUnitIndex, slot);
            var equipmentDatabase = EquipmentDatabaseLoader.Load();
            if (equipmentDatabase == null)
            {
                return equipmentName;
            }

            EquipmentConfig equipment = null;
            var owned = BattleManager.GetOwnedEquipmentsForSlot(slot);
            for (var i = 0; i < owned.Count; i++)
            {
                if (owned[i] != null && string.Equals(owned[i].Name, equipmentName, System.StringComparison.Ordinal))
                {
                    equipment = owned[i];
                    break;
                }
            }

            if (equipment == null)
            {
                return GetSlotDisplayName(slot) + "\n\n" + BuildCurrentEquippedLabel(slot);
            }

            var builder = new StringBuilder();
            builder.Append(equipment.Name)
                .Append("\n\n")
                .Append("\u90e8\u4f4d\uff1a").Append(GetSlotDisplayName(equipment.Slot));
            if (equipment.ATK != 0)
            {
                builder.Append("\n\u653b\u51fb ").Append(equipment.ATK >= 0 ? "+" : string.Empty).Append(equipment.ATK);
            }
            if (equipment.DEF != 0)
            {
                builder.Append("\n\u9632\u5fa1 ").Append(equipment.DEF >= 0 ? "+" : string.Empty).Append(equipment.DEF);
            }
            if (equipment.HP != 0)
            {
                builder.Append("\n\u751f\u547d ").Append(equipment.HP >= 0 ? "+" : string.Empty).Append(equipment.HP);
            }
            if (equipment.MP != 0)
            {
                builder.Append("\n\u6cd5\u529b ").Append(equipment.MP >= 0 ? "+" : string.Empty).Append(equipment.MP);
            }
            if (!string.IsNullOrEmpty(equipment.Notes))
            {
                builder.Append("\n\u8bf4\u660e\uff1a").Append(equipment.Notes);
            }

            return builder.ToString();
        }

        private string BuildCurrentEquippedLabel(string slot)
        {
            var isEnglish = LocalizationManager.Instance != null
                && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
            var prefix = isEnglish ? "Equipped: " : "\u5f53\u524d\u7a7f\u6234\uff1a";
            var equipmentName = BattleManager.GetPlayerEquipmentName(selectedEquipmentUnitIndex, slot);
            return prefix + equipmentName;
        }

        private static void SetSelectionCardTexts(Button button, string title, string subtitle)
        {
            if (button == null)
            {
                return;
            }

            var titleText = button.transform.Find("TitleText")?.GetComponent<Text>();
            if (titleText != null)
            {
                titleText.text = title;
            }

            var subtitleText = button.transform.Find("SubtitleText")?.GetComponent<Text>();
            if (subtitleText != null)
            {
                subtitleText.text = subtitle;
                subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
            }

            if (titleText == null && subtitleText == null)
            {
                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = string.IsNullOrEmpty(subtitle) ? title : title + "\n" + subtitle;
                }
            }
        }

        private void RefreshEquipmentSelectionViewportLayout(int equipmentCount)
        {
            var selectionRoot = GetSelectionScrollRoot();
            var detailRoot = GetEquipmentDetailScrollRoot();
            if (selectionRoot == null || detailRoot == null)
            {
                return;
            }

            CacheEquipmentLayoutMetrics(selectionRoot, detailRoot);
            if (!equipmentLayoutCached)
            {
                return;
            }

            var visibleRows = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(1, equipmentCount) / 2f), 1, 3);
            var targetSelectionHeight = Mathf.Clamp(visibleRows * UICardChromeUtility.StandardCardHeight + Mathf.Max(0, visibleRows - 1) * 16f + 24f, 240f, sharedSelectionDetailHeight - 220f);
            var targetDetailHeight = Mathf.Max(160f, sharedSelectionDetailHeight - targetSelectionHeight);

            selectionRoot.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, selectionTopInset, targetSelectionHeight);
            detailRoot.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, detailBottomInset, targetDetailHeight);
        }

        private void CacheEquipmentLayoutMetrics(RectTransform selectionRoot, RectTransform detailRoot)
        {
            if (equipmentLayoutCached || selectionRoot == null || detailRoot == null)
            {
                return;
            }

            var parent = selectionRoot.parent as RectTransform;
            if (parent == null || detailRoot.parent != parent)
            {
                return;
            }

            var parentBounds = parent.rect;
            var selectionBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(parent, selectionRoot);
            var detailBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(parent, detailRoot);

            selectionTopInset = parentBounds.yMax - selectionBounds.max.y;
            detailBottomInset = detailBounds.min.y - parentBounds.yMin;
            sharedSelectionDetailHeight = selectionBounds.size.y + detailBounds.size.y;
            equipmentLayoutCached = sharedSelectionDetailHeight > 0f;
        }

        private RectTransform GetSelectionScrollRoot()
        {
            if (equipmentSelectionContent == null
                || equipmentSelectionContent.parent == null
                || equipmentSelectionContent.parent.parent == null
                || equipmentSelectionContent.parent.parent.parent == null)
            {
                return null;
            }

            return equipmentSelectionContent.parent.parent.parent as RectTransform;
        }

        private RectTransform GetEquipmentDetailScrollRoot()
        {
            if (equipmentDetailText == null
                || equipmentDetailText.transform.parent == null
                || equipmentDetailText.transform.parent.parent == null
                || equipmentDetailText.transform.parent.parent.parent == null
                || equipmentDetailText.transform.parent.parent.parent.parent == null)
            {
                return null;
            }

            return equipmentDetailText.transform.parent.parent.parent.parent as RectTransform;
        }

        private Button GetOrCreateEquipmentSelectionButton(int index)
        {
            while (equipmentSelectionButtons.Count <= index)
            {
                var button = UIFactory.CreateListButton(equipmentSelectionContent, "EquipOption_" + equipmentSelectionButtons.Count, "Option", delegate { });
                equipmentSelectionButtons.Add(button);
            }

            return equipmentSelectionButtons[index];
        }

        private static void ConfigureEquipmentSelectionButtonRect(RectTransform rect, float leftOffset, float topOffset)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(UICardChromeUtility.StandardCardWidth, UICardChromeUtility.StandardCardHeight);
            rect.anchoredPosition = new Vector2(leftOffset, -topOffset);
        }

        private static string BuildEquipmentOptionSuffix(EquipmentConfig equipment)
        {
            if (equipment == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            AppendEquipmentStat(builder, "HP", equipment.HP);
            AppendEquipmentStat(builder, "ATK", equipment.ATK);
            AppendEquipmentStat(builder, "DEF", equipment.DEF);
            AppendEquipmentStat(builder, "MP", equipment.MP);
            return builder.ToString();
        }

        private static void AppendEquipmentStat(StringBuilder builder, string statName, int value)
        {
            if (value == 0)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append("  ");
            }

            builder.Append(statName).Append('+').Append(value);
        }

        private void RefreshEquipmentEditorButtonTexts()
        {
            UpdateButtonText(cycleEquipmentUnitButton, BuildSelectedUnitButtonText());
            UpdateButtonText(cycleWeaponButton, BuildSlotButtonText("Weapon"));
            UpdateButtonText(cycleArmorButton, BuildSlotButtonText("Armor"));
            UpdateButtonText(cycleAccessoryButton, BuildSlotButtonText("Accessory"));
            UpdateButtonText(autoOffenseButton, BuildAutoOffenseButtonText());
            UpdateButtonText(autoDefenseButton, BuildAutoDefenseButtonText());
            UpdateButtonText(resetEquipmentButton, BuildResetButtonText());
            RefreshEquipmentChrome();
        }

        private string BuildSelectedUnitButtonText()
        {
            var unitName = BattleManager.GetPlayerEquipmentUnitName(selectedEquipmentUnitIndex);
            return LocalizationManager.GetText("battle.editor_unit_prefix") + "\n" + unitName;
        }

        private string BuildSlotButtonText(string slot)
        {
            var slotName = GetSlotDisplayName(slot);
            var equipmentName = BattleManager.GetPlayerEquipmentName(selectedEquipmentUnitIndex, slot);
            return slotName + "\n" + equipmentName;
        }

        private static string GetSlotDisplayName(string slot)
        {
            switch (slot)
            {
                case "Weapon":
                    return LocalizationManager.GetText("battle.slot_weapon");
                case "Armor":
                    return LocalizationManager.GetText("battle.slot_armor");
                case "Accessory":
                    return LocalizationManager.GetText("battle.slot_accessory");
                default:
                    return slot;
            }
        }

        private string BuildResetButtonText()
        {
            return LocalizationManager.GetText("battle.button_reset_loadout");
        }

        private string BuildAutoOffenseButtonText()
        {
            return LocalizationManager.GetText("battle.button_auto_offense");
        }

        private string BuildAutoDefenseButtonText()
        {
            return LocalizationManager.GetText("battle.button_auto_defense");
        }

        private void RefreshEquipmentChrome()
        {
            ApplyEquipmentCardButton(cycleEquipmentUnitButton, Color.white, true);
            ApplyEquipmentCardButton(cycleWeaponButton, Color.white, false);
            ApplyEquipmentCardButton(cycleArmorButton, Color.white, false);
            ApplyEquipmentCardButton(cycleAccessoryButton, Color.white, false);
        }

        private static void ApplyEquipmentCardButton(Button button, Color borderColor, bool emphasize)
        {
            if (button == null)
            {
                return;
            }

            UICardChromeUtility.Apply(button, borderColor, emphasize);
            var rectTransform = button.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0f, rectTransform.anchorMin.y);
                rectTransform.anchorMax = new Vector2(0f, rectTransform.anchorMax.y);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, UICardChromeUtility.StandardCardWidth);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, UICardChromeUtility.StandardCardHeight);
            }

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.alignment = TextAnchor.UpperLeft;
                label.supportRichText = true;
                label.fontSize = 20;
                var rect = label.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(18f, 16f);
                rect.offsetMax = new Vector2(-18f, -16f);
            }
        }

        private static void UpdateButtonText(Button button, string text)
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
    }
}
















