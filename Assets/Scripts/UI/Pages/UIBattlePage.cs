using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
        private const float TeamCardPortraitMaskInset = 13f;
        private const float AutoStartDelaySeconds = UIMapPage.MapToastDuration;
        private const string RoundDivider = "============";
        private const string EndDivider = "##########";
        private const string ActionPrefix = "  > ";
        private const float TeamCardHorizontalSpacing = 12f;
        private const float TeamCardVerticalSpacing = 16f;
        private const int TeamCardColumns = 3;
        private const int TeamCardRows = 2;
        private const float CardEntryDuration = 0.24f;
        private const float CardPulseDuration = 0.3f;
        private const float CardHitDuration = 0.22f;
        private const float CardDeathDuration = 0.42f;
        private const float FloatTextDuration = 0.7f;
        private const float ProjectileDuration = 0.18f;
        private static readonly Color ActionHighlightColor = new Color(0.98f, 0.9f, 0.56f, 1f);
        private static readonly Color HitFlashColor = new Color(1f, 0.55f, 0.55f, 1f);
        private static readonly Color HealFlashColor = new Color(0.58f, 0.92f, 0.68f, 1f);
        private static readonly Color DamageFloatColor = new Color(1f, 0.46f, 0.46f, 1f);
        private static readonly Color HealFloatColor = new Color(0.56f, 0.94f, 0.66f, 1f);
        private static readonly Color ShieldFloatColor = new Color(0.52f, 0.84f, 1f, 1f);
        private static readonly Color ProjectileColor = new Color(1f, 0.91f, 0.64f, 0.95f);
        private static readonly Color DeathFlashColor = new Color(1f, 0.28f, 0.28f, 1f);

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
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite chapterBackground1;
        [SerializeField] private Sprite chapterBackground2;
        [SerializeField] private Sprite chapterBackground3;
        [SerializeField] private Sprite chapterBackground4;
        [SerializeField] private Sprite chapterBackground5;
        [SerializeField] private Sprite chapterBackground6;
        [SerializeField] private Sprite previewCharacterSprite;
        [SerializeField] private Sprite enemyCharacterSprite;

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
        private BattlePlaybackResult activeBattlePlayback;
        private int nextBattleEventIndex;
        private bool canReopenBattleResultPopup;
        private int displayedBattleRoundCount;
        private int displayedBattleActionCount;
        private readonly Dictionary<string, Button> playerCardLookup = new Dictionary<string, Button>(System.StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Button> enemyCardLookup = new Dictionary<string, Button>(System.StringComparer.OrdinalIgnoreCase);
        private readonly List<BattleUnitSnapshot> currentPlayerSnapshots = new List<BattleUnitSnapshot>();
        private readonly List<BattleUnitSnapshot> currentEnemySnapshots = new List<BattleUnitSnapshot>();
        private readonly List<Graphic> transientBattleEffects = new List<Graphic>();
        private readonly HashSet<string> defeatedPlayerUnits = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> defeatedEnemyUnits = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        public override void OnOpen(object data)
        {
            GameProgressManager.StartRun();
            openedForEquipment = data as string == "equipment";
            ResetCurrentBattleState();
            RefreshBackground(GameProgressManager.GetCurrentStage());

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
            EnsureBackgroundImageReference();

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

            var wasBattlePlaying = battlePlaybackCoroutine != null;
            if (wasBattlePlaying)
            {
                StopCoroutine(battlePlaybackCoroutine);
                battlePlaybackCoroutine = null;
            }

            var isEnglish = LocalizationManager.Instance != null
                && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
            var popup = UIManager.Instance.ShowPopup<UIConfirmPopup>("Confirm");
            if (popup == null)
            {
                if (wasBattlePlaying)
                {
                    ResumeBattlePlayback();
                }

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
                delegate
                {
                    if (!wasBattlePlaying)
                    {
                        return;
                    }

                    ResumeBattlePlayback();
                },
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
            StartNewBattlePlayback();
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
                isEnglish ? "Return to the title screen and reset current progress?" : "\u8fd4\u56de\u6807\u9898\uff0c\u5e76\u5c06\u5f53\u524d\u8fdb\u5ea6\u91cd\u7f6e\u4e3a 0 \u5417\uff1f",
                false,
                delegate
                {
                    ClearStoredBattleResult();
                    GameProgressManager.ResetRun();
                    UIManager.Instance.ShowPage("Start");
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

        private void StartNewBattlePlayback()
        {
            activeBattlePlayback = BattleManager.RunSampleBattlePlayback();
            nextBattleEventIndex = 0;
            battlePlaybackCoroutine = StartCoroutine(PlayBattle(false));
        }

        private void ResumeBattlePlayback()
        {
            if (battlePlaybackCoroutine != null)
            {
                StopCoroutine(battlePlaybackCoroutine);
                battlePlaybackCoroutine = null;
            }

            if (activeBattlePlayback == null)
            {
                StartNewBattlePlayback();
                return;
            }

            battlePlaybackCoroutine = StartCoroutine(PlayBattle(true));
        }

        private IEnumerator PlayBattle(bool resumeFromCurrentState)
        {
            SetButtonsInteractable(false);
            if (!resumeFromCurrentState)
            {
                battleLogBuilder.Length = 0;
                displayedBattleRoundCount = 0;
                displayedBattleActionCount = 0;
                ResetLogFollow();
                ClearTransientBattleEffects();
                ApplyStatus(LocalizationManager.GetText("battle.status_idle"));
                ApplyBattleLog(string.Empty);
                PlayInitialTeamEntryAnimation();
                yield return new WaitForSeconds(CardEntryDuration * 0.85f);
            }

            var playback = activeBattlePlayback ?? BattleManager.RunSampleBattlePlayback();
            activeBattlePlayback = playback;
            for (var i = nextBattleEventIndex; i < playback.Events.Count; i++)
            {
                ApplyBattleEvent(playback.Events[i]);
                nextBattleEventIndex = i + 1;
                yield return new WaitForSeconds(GetEventDelay(playback.Events[i]));
            }

            ApplyStatus(playback.IsVictory
                ? LocalizationManager.GetText("battle.status_victory")
                : LocalizationManager.GetText("battle.status_defeat"));

            SetButtonsInteractable(true);
            ShowBattleResultPopup(playback);
            activeBattlePlayback = null;
            nextBattleEventIndex = 0;
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
            activeBattlePlayback = null;
            nextBattleEventIndex = 0;
            displayedBattleRoundCount = 0;
            displayedBattleActionCount = 0;
            ClearTransientBattleEffects();
            currentPlayerSnapshots.Clear();
            currentEnemySnapshots.Clear();
            defeatedPlayerUnits.Clear();
            defeatedEnemyUnits.Clear();
            RefreshPreview();
        }

        private IEnumerator AutoStartBattleNextFrame()
        {
            yield return new WaitForSeconds(AutoStartDelaySeconds);
            if (!openedForEquipment)
            {
                RunBeforeBattleTrigger(OnClickStartBattle);
            }
        }

        private void ApplyBattleEvent(BattleEvent battleEvent)
        {
            var previousPlayerSnapshots = CloneSnapshots(currentPlayerSnapshots);
            var previousEnemySnapshots = CloneSnapshots(currentEnemySnapshots);
            var nextPlayerSnapshots = ParseBattleSnapshots(battleEvent != null ? battleEvent.PlayerTeamSummary : null, true);
            var nextEnemySnapshots = ParseBattleSnapshots(battleEvent != null ? battleEvent.EnemyTeamSummary : null, false);

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
            UpdateSnapshots(currentPlayerSnapshots, nextPlayerSnapshots);
            UpdateSnapshots(currentEnemySnapshots, nextEnemySnapshots);

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

            PlayBattleEventPresentation(battleEvent, previousPlayerSnapshots, previousEnemySnapshots, nextPlayerSnapshots, nextEnemySnapshots);
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
            UpdateSnapshots(currentPlayerSnapshots, ParseBattleSnapshots(playerSummary, true));
            UpdateSnapshots(currentEnemySnapshots, ParseBattleSnapshots(enemySummary, false));
            if (!openedForEquipment && battlePlaybackCoroutine == null)
            {
                HideCardsBeforeBattleStart(playerTeamCardButtons);
                HideCardsBeforeBattleStart(enemyTeamCardButtons);
            }

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

        private void RefreshBackground(int currentStage)
        {
            EnsureBackgroundImageReference();
            if (backgroundImage == null)
            {
                return;
            }

            var sprite = GetChapterBackgroundSprite(currentStage);
            if (sprite != null)
            {
                backgroundImage.sprite = sprite;
                backgroundImage.color = Color.white;
                backgroundImage.preserveAspect = true;
            }
        }

        private void EnsureBackgroundImageReference()
        {
            if (backgroundImage != null)
            {
                return;
            }

            var images = GetComponentsInChildren<Image>(true);
            Image bestCandidate = null;
            var bestScore = float.MinValue;
            for (var i = 0; i < images.Length; i++)
            {
                var image = images[i];
                if (image == null || image.transform == transform)
                {
                    continue;
                }

                var rect = image.rectTransform;
                if (rect == null)
                {
                    continue;
                }

                var score = 0f;
                if (rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one)
                {
                    score += 10000f;
                }

                score += rect.rect.width * rect.rect.height;
                score -= image.transform.GetSiblingIndex() * 10f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCandidate = image;
                }
            }

            backgroundImage = bestCandidate;
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
            RunAfterBattleTrigger(OpenStoredBattleResultPopup);
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
                    isEnglish ? "Back To Title" : "\u8fd4\u56de\u6807\u9898",
                    LocalizationManager.GetText("battle.button_review")
                },
                new List<System.Action>
                {
                    delegate
                    {
                        ClearStoredBattleResult();
                        GameProgressManager.ResetRun();
                        UIManager.Instance.ShowPage("Start");
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

            var popup = UIManager.Instance.ShowPopup<UIRewardChoicePopup>("RewardChoice");
            if (popup == null)
            {
                ClearStoredBattleResult();
                HandlePostVictoryNavigation(isEnglish);
                return;
            }

            var actions = new List<System.Action>();
            for (var i = 0; i < options.Count && i < 3; i++)
            {
                var capturedIndex = i;
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

            popup.SetupSkillRewards(
                BuildVictoryChoiceTitle(isEnglish),
                BuildVictoryChoiceMessage(playback, reward, isEnglish),
                options,
                actions,
                LocalizationManager.GetText("battle.button_review"),
                delegate
                {
                    MarkBattleResultAsReviewable();
                },
                isEnglish);
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
                    UIManager.Instance.ShowPage("Start");
                    return;
                }

                popup.Setup(
                    isEnglish ? "Route Complete" : "\u8def\u7ebf\u5b8c\u6210",
                    isEnglish
                        ? "You have cleared the final boss of this route. The current run will be closed and return to the title screen."
                        : "\u4f60\u5df2\u51fb\u8d25\u5f53\u524d\u8def\u7ebf\u7684\u6700\u7ec8\u9996\u9886\u3002\u672c\u8f6e\u5c06\u5b8c\u6210\u7ed3\u7b97\uff0c\u5e76\u8fd4\u56de\u6807\u9898\u3002",
                    false,
                    delegate
                    {
                        GameProgressManager.ResetRun();
                        UIManager.Instance.ShowPage("Start");
                    },
                    null,
                    isEnglish ? "Finish Run" : "\u7ed3\u675f\u672c\u8f6e",
                    null);
                return;
            }

            UIManager.Instance.ShowPage("Map");
        }

        private void RunBeforeBattleTrigger(System.Action onComplete)
        {
            var stage = Mathf.Max(1, GameProgressManager.GetCurrentStage());
            if (!StoryManager.TryTrigger("BeforeBattle", stage, onComplete))
            {
                onComplete?.Invoke();
            }
        }

        private void RunAfterBattleTrigger(System.Action onComplete)
        {
            var stage = Mathf.Max(1, GameProgressManager.GetCurrentStage());
            if (!StoryManager.TryTrigger("AfterBattle", stage, onComplete))
            {
                onComplete?.Invoke();
            }
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

            var lookup = playerSide ? playerCardLookup : enemyCardLookup;
            lookup.Clear();
            var defeatedUnits = playerSide ? defeatedPlayerUnits : defeatedEnemyUnits;

            for (var i = 0; i < buttons.Count; i++)
            {
                if (buttons[i] != null)
                {
                    buttons[i].gameObject.SetActive(false);
                }
            }

            var cards = BuildUnitCardsFromSummary(teamSummary, equipmentSummary, playerSide);
            var cardSize = GetTeamCardSize(template);
            for (var i = 0; i < cards.Count; i++)
            {
                var button = GetOrCreateTeamCardButton(root, template, buttons, i);
                BindTeamCard(button, cards[i], playerSide);
                ApplyDefeatedCardState(button, defeatedUnits.Contains(cards[i].Title));
                RegisterTeamCardLookup(lookup, cards[i], button);
                var rect = button.GetComponent<RectTransform>();
                if (rect != null)
                {
                    var slotIndex = GetTeamSlotIndex(i, playerSide);
                    var row = slotIndex / TeamCardColumns;
                    var column = slotIndex % TeamCardColumns;
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition = new Vector2(column * (cardSize.x + TeamCardHorizontalSpacing), -row * (cardSize.y + TeamCardVerticalSpacing));
                }
            }
        }

        private void ConfigureBattleSummaryLayout()
        {
            ConfigureBattleOverlayLayout();
            ConfigureTeamPanelLayout(playerCardRoot, playerEquipmentText);
            ConfigureTeamPanelLayout(enemyCardRoot, enemyEquipmentText);
        }

        private void ConfigureBattleOverlayLayout()
        {
            var statusRect = statusText != null ? statusText.transform.parent as RectTransform : null;
            if (statusRect != null)
            {
                statusRect.gameObject.SetActive(false);
            }

            var stageRect = stageInfoText != null ? stageInfoText.transform.parent as RectTransform : null;
            if (stageRect != null)
            {
                stageRect.gameObject.SetActive(true);
            }

        }

        private static void ConfigureTeamPanelLayout(RectTransform cardRoot, Text equipmentText)
        {
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

        private static int GetTeamSlotIndex(int placementIndex, bool playerSide)
        {
            var row = placementIndex % TeamCardRows;
            var priorityColumn = placementIndex / TeamCardRows;
            if (priorityColumn >= TeamCardColumns)
            {
                priorityColumn = TeamCardColumns - 1;
            }

            var visualColumn = playerSide
                ? TeamCardColumns - 1 - priorityColumn
                : priorityColumn;

            return row * TeamCardColumns + visualColumn;
        }

        private static Button GetOrCreateTeamCardButton(RectTransform root, Button template, List<Button> buttons, int index)
        {
            while (buttons.Count <= index)
            {
                var clone = Instantiate(template.gameObject, root, false);
                clone.name = template.name + "_" + buttons.Count;
                clone.SetActive(false);
                var cloneButton = clone.GetComponent<Button>();
                buttons.Add(cloneButton);
            }

            return buttons[index];
        }

        private static Vector2 GetTeamCardSize(Button template)
        {
            var rect = template != null ? template.GetComponent<RectTransform>() : null;
            if (rect != null && rect.rect.width > 0f && rect.rect.height > 0f)
            {
                return rect.rect.size;
            }

            return new Vector2(UICardChromeUtility.StandardCardWidth, UICardChromeUtility.StandardCardHeight);
        }

        private void BindTeamCard(Button button, UICardData card, bool playerSide)
        {
            if (button == null || card == null)
            {
                return;
            }

            button.gameObject.SetActive(true);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate { OpenCardDetail(card); });
            ResetTeamCardVisualState(button);

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

            ApplyPreviewPortrait(button, playerSide);

            var progressRoot = button.transform.Find("ProgressRoot");
            if (progressRoot != null)
            {
                progressRoot.gameObject.SetActive(card.ShowProgress);
            }

            var progressFill = button.transform.Find("ProgressRoot/Fill")?.GetComponent<Image>();
            if (progressFill != null)
            {
                var ratio = Mathf.Clamp01(card.ProgressCurrent / (float)Mathf.Max(1, card.ProgressMax));
                var fillRect = progressFill.rectTransform;
                fillRect.anchorMin = new Vector2(0f, 0f);
                fillRect.anchorMax = new Vector2(ratio, 1f);
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
                progressFill.color = new Color(0.92f, 0.23f, 0.24f, 1f);
            }

            var progressLabel = button.transform.Find("ProgressRoot/ProgressLabel")?.GetComponent<Text>();
            if (progressLabel != null)
            {
                progressLabel.text = card.ProgressLabel ?? string.Empty;
                progressLabel.gameObject.SetActive(card.ShowProgress);
            }
        }

        private void RegisterTeamCardLookup(Dictionary<string, Button> lookup, UICardData card, Button button)
        {
            if (lookup == null || card == null || button == null || string.IsNullOrEmpty(card.Title))
            {
                return;
            }

            lookup[card.Title.Trim()] = button;
        }

        private static void ResetTeamCardVisualState(Button button)
        {
            if (button == null)
            {
                return;
            }

            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
            }

            var graphic = button.targetGraphic;
            if (graphic != null)
            {
                graphic.color = Color.white;
            }

            var canvasGroup = button.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        private static void ApplyDefeatedCardState(Button button, bool defeated)
        {
            if (button == null)
            {
                return;
            }

            var graphic = button.targetGraphic;
            if (graphic != null)
            {
                graphic.color = defeated
                    ? new Color(0.38f, 0.38f, 0.4f, 0.92f)
                    : Color.white;
            }

            var title = button.transform.Find("TitleText")?.GetComponent<Text>();
            var subtitle = button.transform.Find("SubtitleText")?.GetComponent<Text>();
            var progressLabel = button.transform.Find("ProgressRoot/ProgressLabel")?.GetComponent<Text>();
            ApplyTextDefeatedTint(title, defeated);
            ApplyTextDefeatedTint(subtitle, defeated);
            ApplyTextDefeatedTint(progressLabel, defeated);

            var progressFill = button.transform.Find("ProgressRoot/Fill")?.GetComponent<Image>();
            if (progressFill != null)
            {
                progressFill.color = defeated
                    ? new Color(0.35f, 0.35f, 0.37f, 0.95f)
                    : new Color(0.92f, 0.23f, 0.24f, 1f);
            }

            var canvasGroup = EnsureCanvasGroup(button.gameObject);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = defeated ? 0.72f : 1f;
            }
        }

        private static void ApplyTextDefeatedTint(Text text, bool defeated)
        {
            if (text == null)
            {
                return;
            }

            text.color = defeated
                ? new Color(0.72f, 0.72f, 0.74f, 0.95f)
                : Color.white;
        }

        private void PlayInitialTeamEntryAnimation()
        {
            PrepareCardsForEntry(playerTeamCardButtons);
            PrepareCardsForEntry(enemyTeamCardButtons);
            PlayEntryAnimationForButtons(playerTeamCardButtons, true);
            PlayEntryAnimationForButtons(enemyTeamCardButtons, false);
        }

        private static void HideCardsBeforeBattleStart(List<Button> buttons)
        {
            if (buttons == null)
            {
                return;
            }

            for (var i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i];
                if (button == null || !button.gameObject.activeSelf)
                {
                    continue;
                }

                ResetTeamCardVisualState(button);
                var canvasGroup = EnsureCanvasGroup(button.gameObject);
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }
            }
        }

        private static void PrepareCardsForEntry(List<Button> buttons)
        {
            if (buttons == null)
            {
                return;
            }

            for (var i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i];
                if (button == null || !button.gameObject.activeSelf)
                {
                    continue;
                }

                var rect = button.GetComponent<RectTransform>();
                var canvasGroup = EnsureCanvasGroup(button.gameObject);
                if (rect != null)
                {
                    rect.localScale = new Vector3(0.92f, 0.92f, 1f);
                }

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }
            }
        }

        private void PlayEntryAnimationForButtons(List<Button> buttons, bool playerSide)
        {
            if (buttons == null)
            {
                return;
            }

            var order = 0;
            for (var i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i];
                if (button == null || !button.gameObject.activeSelf)
                {
                    continue;
                }

                StartCoroutine(PlayCardEntryAnimation(button, playerSide, order));
                order++;
            }
        }

        private IEnumerator PlayCardEntryAnimation(Button button, bool playerSide, int order)
        {
            if (button == null)
            {
                yield break;
            }

            var rect = button.GetComponent<RectTransform>();
            if (rect == null)
            {
                yield break;
            }

            var canvasGroup = EnsureCanvasGroup(button.gameObject);
            var basePosition = rect.anchoredPosition;
            var startPosition = basePosition + new Vector2(playerSide ? -32f : 32f, 16f);
            var startScale = new Vector3(0.92f, 0.92f, 1f);
            var delay = order * 0.035f;
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            canvasGroup.alpha = 0f;
            rect.anchoredPosition = startPosition;
            rect.localScale = startScale;

            var elapsed = 0f;
            while (elapsed < CardEntryDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / CardEntryDuration);
                var eased = EaseOutCubic(t);
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, eased);
                rect.anchoredPosition = Vector2.LerpUnclamped(startPosition, basePosition, eased);
                rect.localScale = Vector3.LerpUnclamped(startScale, Vector3.one, eased);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            rect.anchoredPosition = basePosition;
            rect.localScale = Vector3.one;
        }

        private void PlayBattleEventPresentation(
            BattleEvent battleEvent,
            List<BattleUnitSnapshot> previousPlayerSnapshots,
            List<BattleUnitSnapshot> previousEnemySnapshots,
            List<BattleUnitSnapshot> nextPlayerSnapshots,
            List<BattleUnitSnapshot> nextEnemySnapshots)
        {
            if (battleEvent == null)
            {
                return;
            }

            var actor = FindActorSnapshot(battleEvent, nextPlayerSnapshots, nextEnemySnapshots);
            var impacts = BuildImpactList(previousPlayerSnapshots, previousEnemySnapshots, nextPlayerSnapshots, nextEnemySnapshots);

            if (actor != null && TryGetDisplayedCard(actor.Name, actor.PlayerSide, out var actorButton))
            {
                StartCoroutine(PlayCardPulseAnimation(actorButton, ActionHighlightColor, 1.08f, CardPulseDuration));
            }

            for (var i = 0; i < impacts.Count; i++)
            {
                var impact = impacts[i];
                if (!TryGetDisplayedCard(impact.Name, impact.PlayerSide, out var targetButton))
                {
                    continue;
                }

                if (impact.HpDelta < 0)
                {
                    var targetSnapshot = FindSnapshotByName(nextPlayerSnapshots, impact.Name) ?? FindSnapshotByName(nextEnemySnapshots, impact.Name);
                    var isDead = targetSnapshot != null && targetSnapshot.CurrentHp <= 0;
                    if (isDead)
                    {
                        TrackDefeatedUnit(impact.Name, impact.PlayerSide, true);
                        StartCoroutine(PlayCardDeathAnimation(targetButton));
                        CreateFloatingValue(targetButton.transform as RectTransform, impact.HpDelta.ToString(CultureInfo.InvariantCulture), DeathFlashColor);
                    }
                    else
                    {
                        TrackDefeatedUnit(impact.Name, impact.PlayerSide, false);
                        StartCoroutine(PlayCardHitAnimation(targetButton, HitFlashColor));
                        CreateFloatingValue(targetButton.transform as RectTransform, impact.HpDelta.ToString(CultureInfo.InvariantCulture), DamageFloatColor);
                    }
                }
                else if (impact.HpDelta > 0)
                {
                    TrackDefeatedUnit(impact.Name, impact.PlayerSide, false);
                    StartCoroutine(PlayCardPulseAnimation(targetButton, HealFlashColor, 1.04f, CardPulseDuration * 0.9f));
                    CreateFloatingValue(targetButton.transform as RectTransform, "+" + impact.HpDelta.ToString(CultureInfo.InvariantCulture), HealFloatColor);
                }
            }

            if (actor != null && impacts.Count > 0 && TryGetDisplayedCard(actor.Name, actor.PlayerSide, out var sourceButton))
            {
                for (var i = 0; i < impacts.Count; i++)
                {
                    if (!TryGetDisplayedCard(impacts[i].Name, impacts[i].PlayerSide, out var targetButton))
                    {
                        continue;
                    }

                    if (impacts[i].HpDelta < 0)
                    {
                        CreateProjectileEffect(sourceButton.transform as RectTransform, targetButton.transform as RectTransform);
                    }
                }
            }

            if (impacts.Count == 0 && actor != null && TryGetDisplayedCard(actor.Name, actor.PlayerSide, out var fallbackButton))
            {
                var shieldToken = LocalizationManager.GetText("battle.log_steadies");
                var healsToken = LocalizationManager.GetText("battle.log_heals");
                var log = battleEvent.Log ?? string.Empty;
                if (!string.IsNullOrEmpty(shieldToken) && log.Contains(shieldToken))
                {
                    CreateFloatingValue(fallbackButton.transform as RectTransform, LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English ? "Shield" : "护盾", ShieldFloatColor);
                }
                else if (!string.IsNullOrEmpty(healsToken) && log.Contains(healsToken))
                {
                    CreateFloatingValue(fallbackButton.transform as RectTransform, "+", HealFloatColor);
                }
            }
        }

        private static void UpdateSnapshots(List<BattleUnitSnapshot> target, List<BattleUnitSnapshot> source)
        {
            if (target == null)
            {
                return;
            }

            target.Clear();
            if (source == null)
            {
                return;
            }

            for (var i = 0; i < source.Count; i++)
            {
                target.Add(source[i].Clone());
            }
        }

        private static List<BattleUnitSnapshot> CloneSnapshots(List<BattleUnitSnapshot> snapshots)
        {
            var results = new List<BattleUnitSnapshot>();
            if (snapshots == null)
            {
                return results;
            }

            for (var i = 0; i < snapshots.Count; i++)
            {
                results.Add(snapshots[i].Clone());
            }

            return results;
        }

        private static List<BattleUnitSnapshot> ParseBattleSnapshots(string teamSummary, bool playerSide)
        {
            var results = new List<BattleUnitSnapshot>();
            if (string.IsNullOrWhiteSpace(teamSummary))
            {
                return results;
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

                results.Add(new BattleUnitSnapshot
                {
                    Name = match.Groups["name"].Value.Trim(),
                    PlayerSide = playerSide,
                    CurrentHp = int.Parse(match.Groups["hp"].Value),
                    MaxHp = int.Parse(match.Groups["max"].Value),
                    CurrentMp = int.Parse(match.Groups["mp"].Value),
                    MaxMp = int.Parse(match.Groups["mpmax"].Value)
                });
            }

            return results;
        }

        private static List<BattleImpactInfo> BuildImpactList(
            List<BattleUnitSnapshot> previousPlayerSnapshots,
            List<BattleUnitSnapshot> previousEnemySnapshots,
            List<BattleUnitSnapshot> nextPlayerSnapshots,
            List<BattleUnitSnapshot> nextEnemySnapshots)
        {
            var results = new List<BattleImpactInfo>();
            CollectImpacts(results, previousPlayerSnapshots, nextPlayerSnapshots);
            CollectImpacts(results, previousEnemySnapshots, nextEnemySnapshots);
            return results;
        }

        private static void CollectImpacts(List<BattleImpactInfo> results, List<BattleUnitSnapshot> previousSnapshots, List<BattleUnitSnapshot> nextSnapshots)
        {
            if (results == null || nextSnapshots == null)
            {
                return;
            }

            for (var i = 0; i < nextSnapshots.Count; i++)
            {
                var next = nextSnapshots[i];
                var previous = FindSnapshotByName(previousSnapshots, next.Name);
                if (previous == null)
                {
                    continue;
                }

                var hpDelta = next.CurrentHp - previous.CurrentHp;
                if (hpDelta == 0)
                {
                    continue;
                }

                results.Add(new BattleImpactInfo
                {
                    Name = next.Name,
                    PlayerSide = next.PlayerSide,
                    HpDelta = hpDelta
                });
            }
        }

        private static BattleUnitSnapshot FindSnapshotByName(List<BattleUnitSnapshot> snapshots, string name)
        {
            if (snapshots == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            for (var i = 0; i < snapshots.Count; i++)
            {
                if (string.Equals(snapshots[i].Name, name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return snapshots[i];
                }
            }

            return null;
        }

        private static BattleUnitSnapshot FindActorSnapshot(BattleEvent battleEvent, List<BattleUnitSnapshot> playerSnapshots, List<BattleUnitSnapshot> enemySnapshots)
        {
            if (battleEvent == null || string.IsNullOrEmpty(battleEvent.Log))
            {
                return null;
            }

            var snapshots = new List<BattleUnitSnapshot>();
            if (playerSnapshots != null)
            {
                snapshots.AddRange(playerSnapshots);
            }

            if (enemySnapshots != null)
            {
                snapshots.AddRange(enemySnapshots);
            }

            BattleUnitSnapshot bestMatch = null;
            var bestIndex = int.MaxValue;
            for (var i = 0; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                if (snapshot == null || string.IsNullOrEmpty(snapshot.Name))
                {
                    continue;
                }

                var index = battleEvent.Log.IndexOf(snapshot.Name, System.StringComparison.OrdinalIgnoreCase);
                if (index < 0 || index >= bestIndex)
                {
                    continue;
                }

                bestIndex = index;
                bestMatch = snapshot;
            }

            return bestMatch;
        }

        private void TrackDefeatedUnit(string name, bool playerSide, bool defeated)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            var targetSet = playerSide ? defeatedPlayerUnits : defeatedEnemyUnits;
            if (defeated)
            {
                targetSet.Add(name);
            }
            else
            {
                targetSet.Remove(name);
            }
        }

        private bool TryGetDisplayedCard(string name, bool playerSide, out Button button)
        {
            button = null;
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            var lookup = playerSide ? playerCardLookup : enemyCardLookup;
            return lookup.TryGetValue(name.Trim(), out button) && button != null;
        }

        private IEnumerator PlayCardPulseAnimation(Button button, Color flashColor, float targetScale, float duration)
        {
            if (button == null)
            {
                yield break;
            }

            var rect = button.GetComponent<RectTransform>();
            var graphic = button.targetGraphic;
            if (rect == null || graphic == null)
            {
                yield break;
            }

            var baseScale = Vector3.one;
            var baseColor = Color.white;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var wave = Mathf.Sin(t * Mathf.PI);
                rect.localScale = Vector3.LerpUnclamped(baseScale, new Vector3(targetScale, targetScale, 1f), wave);
                graphic.color = Color.Lerp(baseColor, flashColor, wave * 0.8f);
                yield return null;
            }

            rect.localScale = baseScale;
            graphic.color = baseColor;
        }

        private IEnumerator PlayCardHitAnimation(Button button, Color flashColor)
        {
            if (button == null)
            {
                yield break;
            }

            var rect = button.GetComponent<RectTransform>();
            var graphic = button.targetGraphic;
            if (rect == null || graphic == null)
            {
                yield break;
            }

            var basePosition = rect.anchoredPosition;
            var baseColor = Color.white;
            var elapsed = 0f;
            while (elapsed < CardHitDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / CardHitDuration);
                var shake = Mathf.Sin(t * Mathf.PI * 5f) * (1f - t) * 14f;
                rect.anchoredPosition = basePosition + new Vector2(shake, 0f);
                graphic.color = Color.Lerp(baseColor, flashColor, 1f - t * 0.75f);
                yield return null;
            }

            rect.anchoredPosition = basePosition;
            graphic.color = baseColor;
        }

        private IEnumerator PlayCardDeathAnimation(Button button)
        {
            if (button == null)
            {
                yield break;
            }

            var rect = button.GetComponent<RectTransform>();
            var graphic = button.targetGraphic;
            var canvasGroup = EnsureCanvasGroup(button.gameObject);
            if (rect == null || graphic == null || canvasGroup == null)
            {
                yield break;
            }

            var basePosition = rect.anchoredPosition;
            var baseColor = Color.white;
            var baseScale = Vector3.one;
            var elapsed = 0f;
            while (elapsed < CardDeathDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / CardDeathDuration);
                var impact = Mathf.Sin(Mathf.Min(1f, t * 1.2f) * Mathf.PI);
                var shake = Mathf.Sin(t * Mathf.PI * 9f) * (1f - t) * 18f;
                rect.anchoredPosition = basePosition + new Vector2(shake, -32f * t);
                rect.localScale = Vector3.LerpUnclamped(baseScale, new Vector3(0.78f, 0.78f, 1f), t);
                graphic.color = Color.Lerp(baseColor, DeathFlashColor, impact * 0.9f);
                canvasGroup.alpha = 1f - t * 0.78f;
                yield return null;
            }

            rect.anchoredPosition = basePosition;
            rect.localScale = baseScale;
            graphic.color = baseColor;
            canvasGroup.alpha = 1f;
            ApplyDefeatedCardState(button, true);
        }

        private void CreateFloatingValue(RectTransform target, string text, Color color)
        {
            if (target == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            var root = GetComponent<RectTransform>();
            if (root == null)
            {
                return;
            }

            var effect = new GameObject("BattleFloatText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            effect.transform.SetParent(root, false);
            var effectRect = effect.GetComponent<RectTransform>();
            effectRect.anchorMin = new Vector2(0.5f, 0.5f);
            effectRect.anchorMax = new Vector2(0.5f, 0.5f);
            effectRect.pivot = new Vector2(0.5f, 0.5f);
            effectRect.sizeDelta = new Vector2(120f, 36f);

            if (!TryGetLocalPointFromWorld(root, target.TransformPoint(target.rect.center), out var anchoredPosition))
            {
                Destroy(effect);
                return;
            }

            effectRect.anchoredPosition = anchoredPosition + new Vector2(0f, 28f);
            var label = effect.GetComponent<Text>();
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 24;
            label.color = color;
            label.raycastTarget = false;
            label.supportRichText = true;
            label.font = ResolveBattleEffectFont();
            EnsureReadableText(label);
            transientBattleEffects.Add(label);
            StartCoroutine(PlayFloatingEffect(label, effectRect));
        }

        private void CreateProjectileEffect(RectTransform source, RectTransform target)
        {
            if (source == null || target == null)
            {
                return;
            }

            var root = GetComponent<RectTransform>();
            if (root == null)
            {
                return;
            }

            if (!TryGetLocalPointFromWorld(root, source.TransformPoint(source.rect.center), out var startPosition)
                || !TryGetLocalPointFromWorld(root, target.TransformPoint(target.rect.center), out var endPosition))
            {
                return;
            }

            var effect = new GameObject("BattleProjectile", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            effect.transform.SetParent(root, false);
            var rect = effect.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(18f, 18f);
            rect.anchoredPosition = startPosition;

            var image = effect.GetComponent<Image>();
            image.color = ProjectileColor;
            image.raycastTarget = false;
            transientBattleEffects.Add(image);
            StartCoroutine(PlayProjectileEffect(image, rect, startPosition, endPosition));
        }

        private IEnumerator PlayFloatingEffect(Graphic graphic, RectTransform rect)
        {
            if (graphic == null || rect == null)
            {
                yield break;
            }

            var startPosition = rect.anchoredPosition;
            var endPosition = startPosition + new Vector2(0f, 42f);
            var startScale = new Vector3(0.92f, 0.92f, 1f);
            var endScale = new Vector3(1.08f, 1.08f, 1f);
            var color = graphic.color;
            var elapsed = 0f;
            while (elapsed < FloatTextDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / FloatTextDuration);
                var eased = EaseOutCubic(t);
                rect.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, eased);
                rect.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
                graphic.color = new Color(color.r, color.g, color.b, 1f - t);
                yield return null;
            }

            if (graphic != null)
            {
                transientBattleEffects.Remove(graphic);
                Destroy(graphic.gameObject);
            }
        }

        private IEnumerator PlayProjectileEffect(Graphic graphic, RectTransform rect, Vector2 startPosition, Vector2 endPosition)
        {
            if (graphic == null || rect == null)
            {
                yield break;
            }

            var color = graphic.color;
            var elapsed = 0f;
            while (elapsed < ProjectileDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / ProjectileDuration);
                var eased = EaseOutCubic(t);
                rect.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, eased);
                rect.localScale = Vector3.one * Mathf.Lerp(0.65f, 1.1f, 1f - Mathf.Abs(0.5f - t) * 2f);
                graphic.color = new Color(color.r, color.g, color.b, 1f - t * 0.35f);
                yield return null;
            }

            if (graphic != null)
            {
                transientBattleEffects.Remove(graphic);
                Destroy(graphic.gameObject);
            }
        }

        private void ClearTransientBattleEffects()
        {
            for (var i = transientBattleEffects.Count - 1; i >= 0; i--)
            {
                var effect = transientBattleEffects[i];
                if (effect != null)
                {
                    Destroy(effect.gameObject);
                }
            }

            transientBattleEffects.Clear();
            ResetCardListVisuals(playerTeamCardButtons);
            ResetCardListVisuals(enemyTeamCardButtons);
        }

        private static void ResetCardListVisuals(List<Button> buttons)
        {
            if (buttons == null)
            {
                return;
            }

            for (var i = 0; i < buttons.Count; i++)
            {
                ResetTeamCardVisualState(buttons[i]);
            }
        }

        private static CanvasGroup EnsureCanvasGroup(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            var group = target.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = target.AddComponent<CanvasGroup>();
            }

            return group;
        }

        private Font ResolveBattleEffectFont()
        {
            if (battleLogText != null && battleLogText.font != null)
            {
                return battleLogText.font;
            }

            if (statusText != null && statusText.font != null)
            {
                return statusText.font;
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static bool TryGetLocalPointFromWorld(RectTransform root, Vector3 worldPoint, out Vector2 localPoint)
        {
            localPoint = Vector2.zero;
            if (root == null)
            {
                return false;
            }

            var screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldPoint);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPoint, null, out localPoint);
        }

        private static float EaseOutCubic(float value)
        {
            var inverse = 1f - Mathf.Clamp01(value);
            return 1f - inverse * inverse * inverse;
        }

        private static void EnsureReadableText(Text text)
        {
            if (text == null)
            {
                return;
            }

            var outline = text.GetComponent<Outline>();
            if (outline == null)
            {
                outline = text.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
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
            RefreshBackground(GameProgressManager.GetCurrentStage());
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

        private void ApplyPreviewPortrait(Button button, bool playerSide)
        {
            if (button == null)
            {
                return;
            }

            var portrait = EnsurePortraitImage(button);
            if (portrait == null)
            {
                return;
            }

            var portraitSprite = playerSide || enemyCharacterSprite == null ? previewCharacterSprite : enemyCharacterSprite;
            portrait.sprite = portraitSprite;
            portrait.enabled = portraitSprite != null;
            portrait.color = new Color(1f, 1f, 1f, 0.9f);
            var portraitRect = portrait.rectTransform;
            ApplyPortraitCover(portraitRect, portraitSprite);
            portraitRect.localScale = new Vector3(playerSide ? 1f : -1f, 1f, 1f);
        }

        private static Image EnsurePortraitImage(Button button)
        {
            var maskRoot = EnsurePortraitMaskRoot(button);
            if (maskRoot == null)
            {
                return null;
            }

            var existing = maskRoot.Find("PortraitImage")?.GetComponent<Image>();
            if (existing != null)
            {
                var existingRect = existing.rectTransform;
                existingRect.anchorMin = new Vector2(0.5f, 0.5f);
                existingRect.anchorMax = new Vector2(0.5f, 0.5f);
                existingRect.pivot = new Vector2(0.5f, 0.5f);
                existingRect.anchoredPosition = Vector2.zero;
                existing.preserveAspect = false;
                return existing;
            }

            var portraitObject = new GameObject("PortraitImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            portraitObject.transform.SetParent(maskRoot, false);
            portraitObject.transform.SetSiblingIndex(0);

            var rect = portraitObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            var image = portraitObject.GetComponent<Image>();
            image.preserveAspect = false;
            image.raycastTarget = false;
            return image;
        }

        private static void ApplyPortraitCover(RectTransform portraitRect, Sprite sprite)
        {
            if (portraitRect == null || sprite == null)
            {
                return;
            }

            var maskRect = portraitRect.parent as RectTransform;
            var targetSize = maskRect != null ? maskRect.rect.size : Vector2.zero;
            if (targetSize.x <= 0f || targetSize.y <= 0f)
            {
                var cardRect = portraitRect.GetComponentInParent<Button>()?.transform as RectTransform;
                if (cardRect != null)
                {
                    targetSize = cardRect.rect.size - Vector2.one * (TeamCardPortraitMaskInset * 2f);
                }
            }

            if (targetSize.x <= 0f || targetSize.y <= 0f || sprite.rect.width <= 0f || sprite.rect.height <= 0f)
            {
                portraitRect.sizeDelta = Vector2.zero;
                return;
            }

            var spriteAspect = sprite.rect.width / sprite.rect.height;
            var targetAspect = targetSize.x / targetSize.y;
            var coverSize = spriteAspect > targetAspect
                ? new Vector2(targetSize.y * spriteAspect, targetSize.y)
                : new Vector2(targetSize.x, targetSize.x / spriteAspect);

            portraitRect.sizeDelta = coverSize;
        }

        private static Transform EnsurePortraitMaskRoot(Button button)
        {
            if (button == null)
            {
                return null;
            }

            var existing = button.transform.Find("PortraitMask");
            if (existing != null)
            {
                var existingRect = existing as RectTransform;
                if (existingRect != null)
                {
                    existingRect.anchorMin = Vector2.zero;
                    existingRect.anchorMax = Vector2.one;
                    existingRect.offsetMin = new Vector2(TeamCardPortraitMaskInset, TeamCardPortraitMaskInset);
                    existingRect.offsetMax = new Vector2(-TeamCardPortraitMaskInset, -TeamCardPortraitMaskInset);
                }

                var existingImage = existing.GetComponent<Image>();
                if (existingImage != null)
                {
                    existingImage.color = new Color(1f, 1f, 1f, 0.01f);
                    existingImage.raycastTarget = false;
                }

                if (existing.GetComponent<RectMask2D>() == null)
                {
                    existing.gameObject.AddComponent<RectMask2D>();
                }

                return existing;
            }

            var maskObject = new GameObject("PortraitMask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            maskObject.transform.SetParent(button.transform, false);
            maskObject.transform.SetSiblingIndex(0);

            var maskRect = maskObject.GetComponent<RectTransform>();
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.offsetMin = new Vector2(TeamCardPortraitMaskInset, TeamCardPortraitMaskInset);
            maskRect.offsetMax = new Vector2(-TeamCardPortraitMaskInset, -TeamCardPortraitMaskInset);

            var maskImage = maskObject.GetComponent<Image>();
            maskImage.color = new Color(1f, 1f, 1f, 0.01f);
            maskImage.raycastTarget = false;
            return maskObject.transform;
        }

        private Sprite GetChapterBackgroundSprite(int stage)
        {
            switch (GetChapterIndexForStage(stage))
            {
                case 1:
                    return chapterBackground1;
                case 2:
                    return chapterBackground2 != null ? chapterBackground2 : chapterBackground1;
                case 3:
                    return chapterBackground3 != null ? chapterBackground3 : chapterBackground2;
                case 4:
                    return chapterBackground4 != null ? chapterBackground4 : chapterBackground3;
                case 5:
                    return chapterBackground5 != null ? chapterBackground5 : chapterBackground4;
                case 6:
                default:
                    return chapterBackground6 != null ? chapterBackground6 : chapterBackground5;
            }
        }

        private int GetChapterIndexForStage(int stage)
        {
            var database = StageNodeDatabaseLoader.Load();
            var stageNodes = database != null ? database.StageNodes : null;
            if (stageNodes == null || stageNodes.Count == 0)
            {
                return Mathf.Clamp(((Mathf.Max(1, stage) - 1) / 20) + 1, 1, 6);
            }

            var targetStage = Mathf.Max(1, stage);
            var chapterIndex = 1;
            string previousTheme = null;

            for (var i = 0; i < stageNodes.Count; i++)
            {
                var config = stageNodes[i];
                if (config == null)
                {
                    continue;
                }

                var themeKey = string.IsNullOrEmpty(config.ThemeZh) ? config.ThemeEn : config.ThemeZh;
                if (previousTheme == null)
                {
                    previousTheme = themeKey;
                }
                else if (!string.Equals(previousTheme, themeKey, System.StringComparison.Ordinal))
                {
                    chapterIndex = Mathf.Min(6, chapterIndex + 1);
                    previousTheme = themeKey;
                }

                if (config.Stage >= targetStage)
                {
                    return chapterIndex;
                }
            }

            return Mathf.Clamp(chapterIndex, 1, 6);
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

            var equipment = BattleManager.GetOwnedEquipmentConfigByInstance(preferredSelectionEquipmentId)
                ?? equipmentDatabase.GetById(preferredSelectionEquipmentId);
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

        private void SortEquipmentsForDisplay(List<EquipmentInstanceData> equipments)
        {
            if (equipments == null || equipments.Count <= 1)
            {
                return;
            }

            var equipmentDatabase = EquipmentDatabaseLoader.Load();
            equipments.Sort(delegate(EquipmentInstanceData left, EquipmentInstanceData right)
            {
                var leftIsPreferred = left != null && left.InstanceId == preferredSelectionEquipmentId;
                var rightIsPreferred = right != null && right.InstanceId == preferredSelectionEquipmentId;
                if (leftIsPreferred != rightIsPreferred)
                {
                    return leftIsPreferred ? -1 : 1;
                }

                var leftConfig = left != null && equipmentDatabase != null ? equipmentDatabase.GetById(left.EquipmentId) : null;
                var rightConfig = right != null && equipmentDatabase != null ? equipmentDatabase.GetById(right.EquipmentId) : null;
                var leftScore = GetEquipmentDisplayScore(leftConfig);
                var rightScore = GetEquipmentDisplayScore(rightConfig);
                if (leftScore != rightScore)
                {
                    return rightScore.CompareTo(leftScore);
                }

                var leftName = leftConfig != null ? leftConfig.Name : string.Empty;
                var rightName = rightConfig != null ? rightConfig.Name : string.Empty;
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

        private static Color GetEquipmentAccentColor(EquipmentConfig equipment)
        {
            return equipment != null ? UIElementPalette.GetQualityColor(equipment.Quality) : Color.white;
        }

        private string BuildEquipmentMetaLabel(EquipmentConfig equipment)
        {
            if (equipment == null)
            {
                return string.Empty;
            }

            var isEnglish = LocalizationManager.Instance != null
                && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
            return BuildEquipmentQualityLabel(equipment.Quality, isEnglish)
                + " / "
                + BuildEquipmentLevelLabel(equipment.Level, isEnglish);
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

            var equipments = BattleManager.GetOwnedEquipmentInstancesForSlot(slot);
            var equipmentDatabase = EquipmentDatabaseLoader.Load();
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
                var equippedInstanceId = BattleManager.GetEquippedPlayerEquipmentInstanceId(selectedEquipmentUnitIndex, slotButtonData.Slot);
                var equippedConfig = BattleManager.GetOwnedEquipmentConfigByInstance(equippedInstanceId);
                SetSelectionCardTexts(slotButton, GetSlotDisplayName(slotButtonData.Slot), BattleManager.GetPlayerEquipmentName(selectedEquipmentUnitIndex, slotButtonData.Slot));

                ApplyEquipmentCardButton(slotButton, GetEquipmentAccentColor(equippedConfig), string.Equals(slotButtonData.Slot, slot, System.StringComparison.OrdinalIgnoreCase));
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
                var equipmentInstance = equipments[i];
                var equipment = equipmentInstance != null && equipmentDatabase != null ? equipmentDatabase.GetById(equipmentInstance.EquipmentId) : null;
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

                var capturedEquipmentId = equipmentInstance.InstanceId;
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
                SetSelectionCardTexts(button, BuildEquipmentOptionLabel(equipment), BuildEquipmentMetaLabel(equipment));
                ApplyEquipmentCardButton(button, GetEquipmentAccentColor(equipment), false);

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
            var equippedInstanceId = BattleManager.GetEquippedPlayerEquipmentInstanceId(selectedEquipmentUnitIndex, slot);
            var equipment = BattleManager.GetOwnedEquipmentConfigByInstance(equippedInstanceId);

            if (equipment == null)
            {
                return GetSlotDisplayName(slot) + "\n\n" + BuildCurrentEquippedLabel(slot);
            }

            var builder = new StringBuilder();
            builder.Append(equipment.Name)
                .Append("\n\n")
                .Append("\u54c1\u7ea7\uff1a").Append(BuildEquipmentQualityLabel(equipment.Quality, false))
                .Append("\n\u7b49\u7ea7\uff1a").Append(BuildEquipmentLevelLabel(equipment.Level, false))
                .Append('\n')
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

        private static string BuildEquipmentQualityLabel(string quality, bool english)
        {
            switch ((quality ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "绿":
                case "green":
                    return english ? "Green" : "绿";
                case "蓝":
                case "blue":
                    return english ? "Blue" : "蓝";
                case "紫":
                case "purple":
                    return english ? "Purple" : "紫";
                case "金":
                case "gold":
                    return english ? "Gold" : "金";
                case "白":
                case "white":
                default:
                    return english ? "White" : "白";
            }
        }

        private static string BuildEquipmentLevelLabel(int level, bool english)
        {
            var clamped = Mathf.Clamp(level, 1, 5);
            if (english)
            {
                return "Lv." + clamped;
            }

            switch (clamped)
            {
                case 1: return "一阶";
                case 2: return "二阶";
                case 3: return "三阶";
                case 4: return "四阶";
                case 5: return "五阶";
                default: return "一阶";
            }
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
            ApplyEquipmentCardButton(cycleWeaponButton, GetEquipmentAccentColor(BattleManager.GetOwnedEquipmentConfigByInstance(BattleManager.GetEquippedPlayerEquipmentInstanceId(selectedEquipmentUnitIndex, "Weapon"))), false);
            ApplyEquipmentCardButton(cycleArmorButton, GetEquipmentAccentColor(BattleManager.GetOwnedEquipmentConfigByInstance(BattleManager.GetEquippedPlayerEquipmentInstanceId(selectedEquipmentUnitIndex, "Armor"))), false);
            ApplyEquipmentCardButton(cycleAccessoryButton, GetEquipmentAccentColor(BattleManager.GetOwnedEquipmentConfigByInstance(BattleManager.GetEquippedPlayerEquipmentInstanceId(selectedEquipmentUnitIndex, "Accessory"))), false);
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

        private class BattleUnitSnapshot
        {
            public string Name;
            public bool PlayerSide;
            public int CurrentHp;
            public int MaxHp;
            public int CurrentMp;
            public int MaxMp;

            public BattleUnitSnapshot Clone()
            {
                return new BattleUnitSnapshot
                {
                    Name = Name,
                    PlayerSide = PlayerSide,
                    CurrentHp = CurrentHp,
                    MaxHp = MaxHp,
                    CurrentMp = CurrentMp,
                    MaxMp = MaxMp
                };
            }
        }

        private class BattleImpactInfo
        {
            public string Name;
            public bool PlayerSide;
            public int HpDelta;
        }
    }
}
















