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

        public override void OnOpen(object data)
        {
            GameProgressManager.StartRun();
            ResetCurrentBattleState();
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

            if (battleLogScrollRect != null && battleLogScrollRect.viewport != null)
            {
                battleLogFollowController = battleLogScrollRect.viewport.GetComponent<UIScrollFollowController>();
                battleLogScrollRect.onValueChanged.AddListener(OnBattleLogScrollChanged);
            }

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
            UIManager.Instance.ShowPage("Map");
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
            GameProgressManager.ResetRun();
            UIManager.Instance.ShowPage("MainMenu");
        }

        private void OnClickEquipment()
        {
            EnsureSelectionFocusFromPreferredEquipment();
            RefreshEquipmentPanel();
            SetEquipmentPanelVisible(true);
        }

        private void OnClickCloseEquipment()
        {
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
            ResetLogFollow();
            ApplyStatus(LocalizationManager.GetText("battle.status_idle"));
            ApplyBattleLog(string.Empty);

            var playback = BattleManager.RunSampleBattlePlayback();
            for (var i = 0; i < playback.Events.Count; i++)
            {
                ApplyBattleEvent(playback.Events[i]);
                yield return new WaitForSeconds(GetEventDelay(playback.Events[i].Type));
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

            RefreshPreview();
        }

        private void ApplyBattleEvent(BattleEvent battleEvent)
        {
            if (playerTeamText != null)
            {
                playerTeamText.text = battleEvent.PlayerTeamSummary;
            }

            if (enemyTeamText != null)
            {
                enemyTeamText.text = battleEvent.EnemyTeamSummary;
            }

            if (playerEquipmentText != null)
            {
                playerEquipmentText.text = battleEvent.PlayerEquipmentSummary;
            }

            if (enemyEquipmentText != null)
            {
                enemyEquipmentText.text = battleEvent.EnemyEquipmentSummary;
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
            ApplyStatus(BattleManager.BuildBattlePreparationStatus());

            var previewPlayback = BattleManager.RunSampleBattlePlayback();
            var previewEvent = previewPlayback.Events.Count > 0 ? previewPlayback.Events[0] : null;

            if (playerTeamText != null)
            {
                playerTeamText.text = previewEvent != null && !string.IsNullOrEmpty(previewEvent.PlayerTeamSummary)
                    ? previewEvent.PlayerTeamSummary
                    : LocalizationManager.GetText("battle.player_content");
            }

            if (enemyTeamText != null)
            {
                enemyTeamText.text = previewEvent != null && !string.IsNullOrEmpty(previewEvent.EnemyTeamSummary)
                    ? previewEvent.EnemyTeamSummary
                    : LocalizationManager.GetText("battle.enemy_content");
            }

            if (playerEquipmentText != null)
            {
                playerEquipmentText.text = previewEvent != null
                    ? previewEvent.PlayerEquipmentSummary
                    : LocalizationManager.GetText("battle.equipment_none");
            }

            if (enemyEquipmentText != null)
            {
                enemyEquipmentText.text = previewEvent != null
                    ? previewEvent.EnemyEquipmentSummary
                    : LocalizationManager.GetText("battle.equipment_none");
            }

            ApplyBattleLog(BattleManager.BuildBattlePreparationSummary());
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
                : "第 " + stage + " 关 / " + region;
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
            return "<color=" + color + ">" + formattedLog + "</color>";
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

            var popup = UIManager.Instance.ShowPopup<UIConfirmPopup>("Confirm");
            if (popup == null)
            {
                return;
            }

            var title = playback.IsVictory
                ? LocalizationManager.GetText("battle.status_victory")
                : LocalizationManager.GetText("battle.status_defeat");

            var isEnglish = LocalizationManager.Instance != null
                && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
            BattleRewardResult reward = null;
            if (playback.IsVictory)
            {
                reward = GameProgressManager.GrantBattleRewards(GameProgressManager.GetCurrentStage());
                if (!string.IsNullOrEmpty(reward.DroppedEquipmentId))
                {
                    preferredSelectionEquipmentId = reward.DroppedEquipmentId;
                    EnsureSelectionFocusFromPreferredEquipment();
                }
            }

            var stageText = isEnglish
                ? "Stage " + GameProgressManager.GetCurrentStage()
                : "第 " + GameProgressManager.GetCurrentStage() + " 关";
            var message =
                stageText + "\n" +
                LocalizationManager.GetText("battle.result_rounds") + ": " + playback.TotalRounds + "\n\n" +
                BuildRewardSummary(reward, isEnglish) + "\n\n" +
                GameProgressManager.BuildPostBattleNextStep(isEnglish, playback.IsVictory) + "\n\n" +
                LocalizationManager.GetText("battle.player_team") + "\n" + playback.FinalPlayerTeamSummary + "\n\n" +
                LocalizationManager.GetText("battle.enemy_team") + "\n" + playback.FinalEnemyTeamSummary;
            var confirmLabel = isEnglish ? (playback.IsVictory ? "Next Stage" : "Retry") : (playback.IsVictory ? "下一关" : "重试");
            var cancelLabel = isEnglish ? "Close" : "关闭";
            GameProgressManager.RecordBattleResult(playback.IsVictory, GameProgressManager.GetCurrentStage(), playback.TotalRounds);

            popup.Setup(
                title,
                message,
                false,
                delegate
                {
                    if (playback.IsVictory)
                    {
                        var advanceResult = GameProgressManager.AdvanceAfterVictory();
                        if (advanceResult == RunAdvanceResult.LifespanEnded)
                        {
                            UIManager.Instance.ShowToast(isEnglish
                                ? "Lifespan exhausted. The run has ended."
                                : "阳寿已尽，本轮结束。", 2f);
                            UIManager.Instance.ShowPage("MainMenu");
                            return;
                        }

                        if (advanceResult == RunAdvanceResult.ChapterComplete)
                        {
                            UIManager.Instance.ShowToast(isEnglish
                                ? "This route is complete."
                                : "本轮路线已完成。", 2f);
                            UIManager.Instance.ShowPage("MainMenu");
                            return;
                        }

                        UIManager.Instance.ShowPage("Map");
                    }
                    else
                    {
                        GameProgressManager.RetryCurrentStage();
                        UIManager.Instance.ShowPage("Battle");
                    }
                },
                delegate { },
                confirmLabel,
                cancelLabel);
        }

        private static string BuildRewardSummary(BattleRewardResult reward, bool isEnglish)
        {
            if (reward == null)
            {
                return isEnglish
                    ? "Rewards: none."
                    : "战利品：无。";
            }

            var builder = new StringBuilder();
            if (isEnglish)
            {
                builder.Append("Rewards\n")
                    .Append("Exp +").Append(reward.ExpGained)
                    .Append(" / Spirit Stones +").Append(reward.SpiritStonesGained);

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

            builder.Append("战利品\n")
                .Append("经验 +").Append(reward.ExpGained)
                .Append(" / 灵石 +").Append(reward.SpiritStonesGained);

            if (reward.LevelsGained > 0)
            {
                builder.Append("\n修为提升 +").Append(reward.LevelsGained);
            }

            if (!string.IsNullOrEmpty(reward.DroppedEquipmentName))
            {
                builder.Append("\n装备掉落：").Append(reward.DroppedEquipmentName);
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

            equipmentDetailText.text = BattleManager.BuildPlayerEquipmentEditorText(selectedEquipmentUnitIndex);
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
            if (startBattleButton != null)
            {
                startBattleButton.interactable = interactable;
            }

            if (restartButton != null)
            {
                restartButton.interactable = interactable;
            }
        }

        private void OnLanguageChanged()
        {
            RefreshPreview();

            if (equipmentPanel != null && equipmentPanel.activeSelf)
            {
                RefreshEquipmentPanel();
                SetEquipmentPanelVisible(true);
            }
        }

        private static float GetEventDelay(BattleEventType eventType)
        {
            switch (eventType)
            {
                case BattleEventType.RoundStart:
                    return 0.16f;
                case BattleEventType.BattleEnd:
                    return 0.22f;
                case BattleEventType.Action:
                default:
                    return 0.08f;
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
            var suffix = isEnglish ? "Backpack" : "背包";
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

            var builder = new StringBuilder();
            if (equipment.Id == preferredSelectionEquipmentId)
            {
                builder.Append(LocalizationManager.Instance != null
                        && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English
                    ? "[New] "
                    : "[新] ");
            }

            builder.Append(equipment.Name);
            var suffix = BuildEquipmentOptionSuffix(equipment);
            if (!string.IsNullOrEmpty(suffix))
            {
                builder.Append("  ").Append(suffix);
            }

            return builder.ToString();
        }

        private static int GetEquipmentDisplayScore(EquipmentConfig equipment)
        {
            if (equipment == null)
            {
                return int.MinValue;
            }

            return equipment.HP + equipment.MP + equipment.ATK * 2 + equipment.DEF * 2;
        }

        private void RefreshEquipmentSelectionList()
        {
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
            RefreshEquipmentSelectionViewportLayout(equipments.Count + 1);

            var equippedButton = GetOrCreateEquipmentSelectionButton(0);
            equippedButton.gameObject.SetActive(true);
            ConfigureEquipmentSelectionButtonRect(equippedButton.GetComponent<RectTransform>(), 0f);
            equippedButton.onClick.RemoveAllListeners();
            equippedButton.interactable = false;
            var equippedLabel = equippedButton.GetComponentInChildren<Text>();
            if (equippedLabel != null)
            {
                equippedLabel.fontSize = 20;
                equippedLabel.alignment = TextAnchor.MiddleLeft;
                equippedLabel.text = BuildCurrentEquippedLabel(slot);
            }

            if (equipments.Count == 0)
            {
                var emptyButton = GetOrCreateEquipmentSelectionButton(1);
                emptyButton.gameObject.SetActive(true);
                emptyButton.interactable = false;
                ConfigureEquipmentSelectionButtonRect(emptyButton.GetComponent<RectTransform>(), 64f);
                emptyButton.onClick.RemoveAllListeners();
                var emptyLabel = emptyButton.GetComponentInChildren<Text>();
                if (emptyLabel != null)
                {
                    emptyLabel.fontSize = 20;
                    emptyLabel.alignment = TextAnchor.MiddleLeft;
                    emptyLabel.text = BuildEmptyBackpackLabel();
                }

                equipmentSelectionContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 192f);
                return;
            }

            var currentY = 64f;
            for (var i = 0; i < equipments.Count; i++)
            {
                var equipment = equipments[i];
                if (equipment == null)
                {
                    continue;
                }

                var button = GetOrCreateEquipmentSelectionButton(i + 1);
                button.gameObject.SetActive(true);
                button.interactable = true;
                var rect = button.GetComponent<RectTransform>();
                ConfigureEquipmentSelectionButtonRect(rect, currentY);
                currentY += 64f;

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
                    label.text = BuildEquipmentOptionLabel(equipment);
                }

            }

            equipmentSelectionContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(160f, currentY));
        }

        private string BuildCurrentEquippedLabel(string slot)
        {
            var isEnglish = LocalizationManager.Instance != null
                && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
            var prefix = isEnglish ? "Equipped: " : "当前穿戴：";
            var equipmentName = BattleManager.GetPlayerEquipmentName(selectedEquipmentUnitIndex, slot);
            return prefix + equipmentName;
        }

        private string BuildEmptyBackpackLabel()
        {
            var isEnglish = LocalizationManager.Instance != null
                && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
            return isEnglish ? "No other equipment in backpack." : "背包里暂时没有其他可选装备。";
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

            var visibleCount = Mathf.Clamp(equipmentCount, 2, 7);
            var targetSelectionHeight = Mathf.Clamp(visibleCount * 64f + 12f, 140f, sharedSelectionDetailHeight - 120f);
            var targetDetailHeight = Mathf.Max(120f, sharedSelectionDetailHeight - targetSelectionHeight);

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

        private static void ConfigureEquipmentSelectionButtonRect(RectTransform rect, float topOffset)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 56f);
            rect.anchoredPosition = new Vector2(0f, -topOffset);
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
        }

        private string BuildSelectedUnitButtonText()
        {
            var unitName = BattleManager.GetPlayerEquipmentUnitName(selectedEquipmentUnitIndex);
            return LocalizationManager.GetText("battle.editor_unit_prefix") + unitName;
        }

        private string BuildSlotButtonText(string slot)
        {
            var slotName = GetSlotDisplayName(slot);
            var equipmentName = BattleManager.GetPlayerEquipmentName(selectedEquipmentUnitIndex, slot);
            return slotName + ": " + equipmentName;
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





