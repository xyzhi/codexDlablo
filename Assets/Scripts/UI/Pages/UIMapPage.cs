using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Wuxing.Battle;
using Wuxing.Config;
using Wuxing.Game;
using Wuxing.Localization;

namespace Wuxing.UI
{
    public class UIMapPage : UIPage
    {
        public const float MapToastDuration = 1.25f;

        private const float MoveDurationPerStep = 0.5f;
        private const int VisibleNodeCount = 5;
        private const int CenterNodeIndex = 2;
        private const float NodeWidth = 164f;
        private const float NodeHeight = 148f;
        private const float LineVerticalOffset = 40f;
        private static readonly Vector2[] NodeLayoutPattern =
        {
            new Vector2(-340f, -280f),
            new Vector2(-90f, -80f),
            new Vector2(170f, 110f),
            new Vector2(-30f, 340f),
            new Vector2(230f, 560f)
        };

        [SerializeField] private Text titleText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text regionText;
        [SerializeField] private Text longevityText;
        [SerializeField] private RectTransform nodeGraphRoot;
        [SerializeField] private Text nodeDetailText;
        [SerializeField] private Text routeText;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button enterButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button equipmentButton;
        [SerializeField] private Button spiritConvertButton;
        [SerializeField] private Button skillOverviewButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Sprite bottomButtonSprite;
        [SerializeField] private Sprite mapLineSprite;
        [SerializeField] private Sprite restNodeSprite;
        [SerializeField] private Sprite battleNodeSprite;
        [SerializeField] private Sprite eliteNodeSprite;
        [SerializeField] private Sprite bossNodeSprite;
        [SerializeField] private Sprite eventNodeSprite;

        private readonly List<Button> nodeButtons = new List<Button>();
        private readonly List<Text> nodeButtonLabels = new List<Text>();
        private readonly List<Image> nodeButtonIcons = new List<Image>();
        private readonly List<Image> nodeLines = new List<Image>();
        private readonly List<int> visibleStages = new List<int>();
        private readonly Dictionary<int, Vector2> stagePositions = new Dictionary<int, Vector2>();

        private Image travelerMarker;
        private Coroutine moveCoroutine;
        private int selectedStage;
        private bool isMoving;

        public override void OnOpen(object data)
        {
            GameProgressManager.StartRun();
            selectedStage = Mathf.Max(1, GameProgressManager.GetCurrentStage());
            RefreshView();
            StoryManager.TryTrigger("StartGame", Mathf.Max(1, GameProgressManager.GetCurrentStage()));
        }

        private void Awake()
        {
            if (previousButton != null) previousButton.onClick.AddListener(OnClickPrevious);
            if (enterButton != null) enterButton.onClick.AddListener(OnClickEnter);
            if (nextButton != null) nextButton.onClick.AddListener(OnClickNext);
            if (equipmentButton != null) equipmentButton.onClick.AddListener(OnClickEquipment);
            if (spiritConvertButton != null) spiritConvertButton.onClick.AddListener(OnClickSpiritConvert);
            if (skillOverviewButton != null) skillOverviewButton.onClick.AddListener(OnClickSkillOverview);
            if (resetButton != null) resetButton.onClick.AddListener(OnClickReset);
            if (backButton != null) backButton.onClick.AddListener(OnClickBack);
        }

        private void OnEnable()
        {
            GameProgressManager.ProgressChanged += OnProgressChanged;
            LocalizationManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            GameProgressManager.ProgressChanged -= OnProgressChanged;
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
        }

        private void OnDestroy()
        {
            if (previousButton != null) previousButton.onClick.RemoveListener(OnClickPrevious);
            if (enterButton != null) enterButton.onClick.RemoveListener(OnClickEnter);
            if (nextButton != null) nextButton.onClick.RemoveListener(OnClickNext);
            if (equipmentButton != null) equipmentButton.onClick.RemoveListener(OnClickEquipment);
            if (spiritConvertButton != null) spiritConvertButton.onClick.RemoveListener(OnClickSpiritConvert);
            if (skillOverviewButton != null) skillOverviewButton.onClick.RemoveListener(OnClickSkillOverview);
            if (resetButton != null) resetButton.onClick.RemoveListener(OnClickReset);
            if (backButton != null) backButton.onClick.RemoveListener(OnClickBack);
        }

        private void OnProgressChanged()
        {
            if (!gameObject.activeInHierarchy) return;
            var currentStage = Mathf.Max(1, GameProgressManager.GetCurrentStage());
            selectedStage = Mathf.Clamp(selectedStage, 1, Mathf.Max(currentStage, GameProgressManager.GetMaxReachableStage()));
            RefreshView();
        }

        private void OnLanguageChanged()
        {
            if (gameObject.activeInHierarchy)
            {
                RefreshView();
            }
        }

        private void OnClickPrevious()
        {
            var targetStage = Mathf.Max(1, GameProgressManager.GetCurrentStage() - 1);
            if (!GameProgressManager.CanTravelToStage(targetStage)) return;
            StartMoveToStage(targetStage, true);
        }

        private void OnClickEnter()
        {
            if (isMoving) return;
            var currentStage = Mathf.Max(1, GameProgressManager.GetCurrentStage());
            ShowArrivalToast(currentStage);
            TriggerStageEvent(currentStage);
        }

        private void OnClickNext()
        {
            var targetStage = GameProgressManager.GetCurrentStage() + 1;
            if (!GameProgressManager.CanTravelToStage(targetStage)) return;
            StartMoveToStage(targetStage, true);
        }

        private void OnClickEquipment()
        {
            if (isMoving) return;

            UIManager.Instance.ShowPopup<UIEquipmentPopup>("Equipment");
        }

        private void OnClickSpiritConvert()
        {
            if (isMoving) return;
            UIManager.Instance.ShowPopup<UISpiritStoneConvertPopup>("SpiritConvert");
        }
        private void OnClickSkillOverview()
        {
            if (isMoving) return;
            UIManager.Instance.ShowPopup<UISkillPopup>("Skill", GameProgressManager.GetPrimaryCharacterId());
        }

        private void OnClickReset()
        {
            if (isMoving) return;
            var popup = UIManager.Instance.ShowPopup<UIConfirmPopup>("Confirm");
            if (popup == null)
            {
                GameProgressManager.ResetRun();
                UIManager.Instance.ShowPage("Start");
                return;
            }

            popup.Setup(
                IsEnglish() ? "Reset Run" : "\u91cd\u7f6e\u672c\u8f6e",
                IsEnglish() ? "Return to the title screen and reset current progress?" : "\u8fd4\u56de\u6807\u9898\uff0c\u5e76\u5c06\u5f53\u524d\u8fdb\u5ea6\u91cd\u7f6e\u4e3a 0 \u5417\uff1f",
                false,
                delegate
                {
                    GameProgressManager.ResetRun();
                    UIManager.Instance.ShowPage("Start");
                },
                delegate { },
                IsEnglish() ? "Confirm" : "\u786e\u8ba4",
                IsEnglish() ? "Cancel" : "\u53d6\u6d88");
        }

        private void OnClickBack()
        {
            if (isMoving) return;
            UIManager.Instance.ShowPage("Start");
        }

        private void OnClickNodeStage(int stage)
        {
            selectedStage = stage;
            RefreshView();
        }

        private void ShowLifespanEndedPopup()
        {
            isMoving = false;
            moveCoroutine = null;

            var popup = UIManager.Instance.ShowPopup<UIConfirmPopup>("Confirm");
            if (popup == null)
            {
                GameProgressManager.ResetRun();
                UIManager.Instance.ShowPage("Start");
                return;
            }

            popup.Setup(
                LocalizationManager.GetText("map.lifespan_title"),
                LocalizationManager.GetText("map.lifespan_message"),
                false,
                delegate
                {
                    GameProgressManager.ResetRun();
                    UIManager.Instance.ShowPage("Start");
                },
                null,
                LocalizationManager.GetText("map.button_back_main"),
                null);
        }

        private void StartMoveToStage(int targetStage, bool triggerEventAfterMove)
        {
            if (isMoving) return;
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(MoveToStageCoroutine(targetStage, triggerEventAfterMove));
        }

        private IEnumerator MoveToStageCoroutine(int targetStage, bool triggerEventAfterMove)
        {
            isMoving = true;
            RefreshView();

            var currentStage = Mathf.Max(1, GameProgressManager.GetCurrentStage());
            var direction = targetStage >= currentStage ? 1 : -1;
            var activeStage = currentStage;

            while (activeStage != targetStage)
            {
                var nextStage = activeStage + direction;
                yield return AnimateTravelStep(activeStage, nextStage);

                var advanceResult = GameProgressManager.TravelToStage(nextStage);
                if (advanceResult == RunAdvanceResult.LifespanEnded)
                {
                    ShowLifespanEndedPopup();
                    yield break;
                }

                activeStage = nextStage;
                selectedStage = activeStage;
                RefreshView();
                yield return null;
            }

            isMoving = false;
            moveCoroutine = null;
            RefreshView();

            if (triggerEventAfterMove)
            {
                var arrivedStage = Mathf.Max(1, GameProgressManager.GetCurrentStage());
                ShowArrivalToast(arrivedStage);
                TriggerStageEvent(arrivedStage);
            }
        }

        private IEnumerator AnimateTravelStep(int fromStage, int toStage)
        {
            Vector2 fromPosition;
            Vector2 toPosition;
            if (!stagePositions.TryGetValue(fromStage, out fromPosition) || !stagePositions.TryGetValue(toStage, out toPosition))
            {
                yield return null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < MoveDurationPerStep)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / MoveDurationPerStep);
                UpdateTravelerMarkerPosition(Vector2.Lerp(fromPosition, toPosition, progress));
                yield return null;
            }
        }

        private void ShowArrivalToast(int stage)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast(BuildArrivalToast(stage), MapToastDuration);
            }
        }

        private void TriggerStageEvent(int stage)
        {
            var eventProfile = GameProgressManager.GetStageEventProfile(stage);
            var nodeType = GameProgressManager.GetNodeType(stage);
            var eventMode = GameProgressManager.GetStageEventMode(stage);
            GameProgressManager.RegisterObjectiveEvent("EnterNodeType", nodeType.ToString(), 1);

            if (GameProgressManager.IsBattleNode(nodeType))
            {
                UIManager.Instance.ShowPage("Battle");
                return;
            }

            if (string.Equals(eventMode, "Random", StringComparison.OrdinalIgnoreCase))
            {
                TriggerRandomConfiguredEvent(stage, eventProfile);
                return;
            }

            if (!string.IsNullOrEmpty(eventProfile))
            {
                ShowConfiguredEventChoices(stage, eventProfile);
                return;
            }

            if (nodeType == MapNodeType.Rest)
            {
                ShowConfiguredEventChoices(stage, "Rest");
                return;
            }

            UIManager.Instance.ShowToast(LocalizationManager.GetText("map.toast_event_triggered"), MapToastDuration);
        }

        private void ShowNodeRewardPopup(string title, string message)
        {
            var popup = UIManager.Instance.ShowPopup<UIConfirmPopup>("Confirm");
            if (popup == null)
            {
                UIManager.Instance.ShowToast(message, MapToastDuration);
                return;
            }

            popup.Setup(
                title,
                message,
                false,
                null,
                null,
                LocalizationManager.GetText("map.button_continue"),
                null);
        }

        private void ShowRandomEventCooldownPopup(int stage)
        {
            var remainingMonths = GameProgressManager.GetRandomStageCooldownRemainingMonths(stage);
            ShowNodeRewardPopup(
                LocalizationManager.GetText("map.random_cooldown_title"),
                string.Format(LocalizationManager.GetText("map.random_cooldown_message"), Mathf.Max(0, remainingMonths)));
        }

        private void ShowConfiguredEventChoices(int stage, string profile)
        {
            var options = GetEventOptions(profile);
            if (options.Count == 0)
            {
                UIManager.Instance.ShowToast(LocalizationManager.GetText("map.toast_event_triggered"), MapToastDuration);
                return;
            }

            var profileConfig = GetEventProfile(profile);
            var titleKey = profileConfig != null && !string.IsNullOrEmpty(profileConfig.TitleKey)
                ? profileConfig.TitleKey
                : "map.title";
            var messageKey = profileConfig != null && !string.IsNullOrEmpty(profileConfig.MessageKey)
                ? profileConfig.MessageKey
                : "map.toast_event_triggered";

            var popup = UIManager.Instance.ShowPopup<UIConfirmPopup>("Confirm");
            if (popup == null)
            {
                ResolveConfiguredEventChoice(stage, profile, 0, false);
                return;
            }

            var isEnglish = IsEnglish();
            var choiceLabels = new List<string>();
            var choiceActions = new List<Action>();
            for (var i = 0; i < options.Count; i++)
            {
                var capturedIndex = i;
                var option = options[i];
                choiceLabels.Add(BuildConfiguredChoiceLabel(option, stage, isEnglish));
                choiceActions.Add(delegate { ResolveConfiguredEventChoice(stage, profile, capturedIndex, false); });
            }

            popup.SetupChoices(
                LocalizationManager.GetText(titleKey),
                LocalizationManager.GetText(messageKey),
                choiceLabels,
                choiceActions);
        }

        private void TriggerRandomConfiguredEvent(int stage, string profile)
        {
            if (GameProgressManager.GetRandomStageCooldownRemainingMonths(stage) > 0)
            {
                ShowRandomEventCooldownPopup(stage);
                return;
            }

            var options = GetEventOptions(profile);
            if (options.Count == 0)
            {
                UIManager.Instance.ShowToast(LocalizationManager.GetText("map.toast_event_triggered"), MapToastDuration);
                return;
            }

            var availableOptions = new List<EventOptionConfig>();
            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (option == null)
                {
                    continue;
                }

                var element = ResolveEventSpiritStoneElement(option, stage);
                var cost = ResolveScaledValue(option.SpiritStoneCostBase, option.SpiritStoneCostPerStage, stage);
                if (cost <= 0 || GameProgressManager.HasEnoughSpiritStones(element, cost))
                {
                    availableOptions.Add(option);
                }
            }

            if (availableOptions.Count == 0)
            {
                ShowNodeRewardPopup(
                    LocalizationManager.GetText("map.not_enough_stones_title"),
                    LocalizationManager.GetText("map.not_enough_stones_message"));
                return;
            }

            var selectedOption = availableOptions[UnityEngine.Random.Range(0, availableOptions.Count)];
            ExecuteConfiguredEventOption(stage, selectedOption, true);
        }

        private void ResolveConfiguredEventChoice(int stage, string profile, int optionIndex, bool isRandomEvent)
        {
            var options = GetEventOptions(profile);
            if (optionIndex < 0 || optionIndex >= options.Count)
            {
                return;
            }

            var option = options[optionIndex];
            ExecuteConfiguredEventOption(stage, option, isRandomEvent);
        }

        private void ExecuteConfiguredEventOption(int stage, EventOptionConfig option, bool isRandomEvent)
        {
            if (option == null)
            {
                return;
            }

            var element = ResolveEventSpiritStoneElement(option, stage);
            var cost = ResolveScaledValue(option.SpiritStoneCostBase, option.SpiritStoneCostPerStage, stage);
            if (cost > 0 && !GameProgressManager.TrySpendSpiritStones(element, cost))
            {
                ShowNodeRewardPopup(
                    LocalizationManager.GetText("map.not_enough_stones_title"),
                    LocalizationManager.GetText("map.not_enough_stones_message"));
                return;
            }

            var exp = ResolveScaledValue(option.ExpBase, option.ExpPerStage, stage);
            var spiritStoneCount = ResolveScaledValue(option.SpiritStoneBase, option.SpiritStonePerStage, stage);
            var rewardMode = ParseRewardMode(option.RewardMode);
            if (rewardMode == "Utility")
            {
                ExecuteUtilityAction(stage, option, isRandomEvent);
                return;
            }

            if (rewardMode == "Skill")
            {
                GameProgressManager.PrepareSkillRewardOptions(ParseRewardNodeType(option.SkillRewardNodeType));
                ShowConfiguredSkillRewardChoices(stage, option, isRandomEvent);
                return;
            }

            BattleRewardResult reward = rewardMode == "Equipment"
                ? GameProgressManager.GrantEquipmentReward(stage, exp, element, spiritStoneCount)
                : GameProgressManager.GrantProgressReward(exp, element, spiritStoneCount);
            var appliedEffect = ApplyConfiguredTimedEffect(option, stage);

            CompleteNodeRewardFlow(
                stage,
                LocalizationManager.GetText(option.ResultTitleKey),
                LocalizationManager.GetText(option.ResultIntroKey),
                reward,
                appliedEffect,
                isRandomEvent);
        }

        private void ExecuteUtilityAction(int stage, EventOptionConfig option, bool isRandomEvent)
        {
            FinalizeStageEventState(stage, isRandomEvent);
            RefreshView();

            var action = option.UtilityAction ?? string.Empty;
            if (string.Equals(action, "OpenEquipment", StringComparison.OrdinalIgnoreCase))
            {
                UIManager.Instance.ShowPopup<UIEquipmentPopup>("Equipment");
                return;
            }

            if (string.Equals(action, "OpenSkillOverview", StringComparison.OrdinalIgnoreCase))
            {
                UIManager.Instance.ShowPopup<UISkillPopup>("Skill", GameProgressManager.GetPrimaryCharacterId());
                return;
            }

            ShowNodeRewardPopup(
                LocalizationManager.GetText(option.ResultTitleKey),
                LocalizationManager.GetText(option.ResultIntroKey));
        }

        private void ShowConfiguredSkillRewardChoices(int stage, EventOptionConfig option, bool isRandomEvent)
        {
            var isEnglish = IsEnglish();
            var options = GameProgressManager.GetPendingSkillRewardOptions();
            if (options.Count == 0)
            {
                FinalizeConfiguredSkillReward(stage, option, null, isEnglish, isRandomEvent);
                return;
            }

            if (isRandomEvent)
            {
                var randomIndex = UnityEngine.Random.Range(0, options.Count);
                var autoApplied = GameProgressManager.ApplyPendingSkillReward(randomIndex);
                FinalizeConfiguredSkillReward(stage, option, autoApplied, isEnglish, true);
                return;
            }

            var popup = UIManager.Instance.ShowPopup<UIConfirmPopup>("Confirm");
            if (popup == null)
            {
                var applied = GameProgressManager.ApplyPendingSkillReward(0);
                FinalizeConfiguredSkillReward(stage, option, applied, isEnglish, false);
                return;
            }

            var choiceLabels = new List<string>();
            var choiceActions = new List<Action>();
            for (var i = 0; i < options.Count; i++)
            {
                var capturedIndex = i;
                choiceLabels.Add(BuildMapSkillRewardChoiceLabel(options[i], isEnglish));
                choiceActions.Add(delegate
                {
                    var applied = GameProgressManager.ApplyPendingSkillReward(capturedIndex);
                    FinalizeConfiguredSkillReward(stage, option, applied, isEnglish, false);
                });
            }

            popup.SetupChoices(
                LocalizationManager.GetText(option.SelectionTitleKey),
                LocalizationManager.GetText(option.SelectionMessageKey),
                choiceLabels,
                choiceActions);
        }

        private void FinalizeConfiguredSkillReward(int stage, EventOptionConfig option, SkillRewardOption appliedOption, bool isEnglish, bool isRandomEvent)
        {
            var appliedEffect = ApplyConfiguredTimedEffect(option, stage);
            FinalizeStageEventState(stage, isRandomEvent);
            RefreshView();
            var message = appliedOption == null
                ? LocalizationManager.GetText(option.EmptyResultKey)
                : LocalizationManager.GetText(option.ResultIntroKey) + "\n\n" + BuildMapSkillRewardToast(appliedOption, isEnglish);
            if (appliedEffect != null)
            {
                message += "\n\n" + BuildEffectResultText(appliedEffect);
            }
            ShowNodeRewardPopup(
                LocalizationManager.GetText(option.ResultTitleKey),
                message);
        }

        private static List<EventOptionConfig> GetEventOptions(string profile)
        {
            var database = EventOptionDatabaseLoader.Load();
            return database != null ? database.GetByProfile(profile) : new List<EventOptionConfig>();
        }

        private static EventProfileConfig GetEventProfile(string profile)
        {
            var database = EventProfileDatabaseLoader.Load();
            return database != null ? database.GetByProfile(profile) : null;
        }

        private string BuildConfiguredChoiceLabel(EventOptionConfig option, int stage, bool isEnglish)
        {
            if (option == null)
            {
                return string.Empty;
            }

            var title = LocalizationManager.GetText(option.TitleKey);
            var lines = new List<string>();
            var element = ResolveEventSpiritStoneElement(option, stage);
            var spiritStoneName = GameProgressManager.GetSpiritStoneName(element, isEnglish);
            var cost = ResolveScaledValue(option.SpiritStoneCostBase, option.SpiritStoneCostPerStage, stage);
            var exp = ResolveScaledValue(option.ExpBase, option.ExpPerStage, stage);
            var spiritStoneCount = ResolveScaledValue(option.SpiritStoneBase, option.SpiritStonePerStage, stage);
            var rewardMode = ParseRewardMode(option.RewardMode);

            if (cost > 0)
            {
                lines.Add(string.Format(LocalizationManager.GetText("map.choice_cost"), spiritStoneName, cost));
            }

            if (exp > 0)
            {
                lines.Add(string.Format(LocalizationManager.GetText("map.choice_gain_exp"), exp));
            }

            if (spiritStoneCount > 0)
            {
                lines.Add(string.Format(LocalizationManager.GetText("map.choice_gain_stones"), spiritStoneName, spiritStoneCount));
            }

            if (rewardMode == "Equipment")
            {
                lines.Add(LocalizationManager.GetText("map.choice_gain_equipment"));
            }
            else if (rewardMode == "Skill")
            {
                lines.Add(GetSkillRewardGainText(ParseRewardNodeType(option.SkillRewardNodeType)));
            }
            else if (rewardMode == "Utility")
            {
                lines.Add(GetUtilityActionText(option.UtilityAction));
            }

            var effectPreview = BuildEffectPreviewText(option, stage);
            if (!string.IsNullOrEmpty(effectPreview))
            {
                lines.Add(effectPreview);
            }

            return BuildChoiceLabel(title, lines.ToArray());
        }

        private static string ResolveEventSpiritStoneElement(EventOptionConfig option, int stage)
        {
            if (option == null || string.IsNullOrEmpty(option.SpiritStoneElement) || string.Equals(option.SpiritStoneElement, "Stage", StringComparison.OrdinalIgnoreCase))
            {
                return GameProgressManager.GetSpiritStoneElementForStage(stage);
            }

            return option.SpiritStoneElement;
        }

        private static int ResolveScaledValue(int baseValue, int perStage, int stage)
        {
            return Mathf.Max(0, baseValue + Mathf.Max(0, stage) * perStage);
        }

        private RunEffectData ApplyConfiguredTimedEffect(EventOptionConfig option, int stage)
        {
            if (option == null || string.IsNullOrEmpty(option.BuffType))
            {
                return null;
            }

            var value = ResolveScaledValue(option.BuffValueBase, option.BuffValuePerStage, stage);
            var duration = Mathf.Max(0, option.BuffDurationMonths);
            return GameProgressManager.AddTimedEffect(option.BuffType, value, duration, option.BuffTitleKey, option.BuffDescriptionKey);
        }

        private string BuildEffectPreviewText(EventOptionConfig option, int stage)
        {
            if (option == null || string.IsNullOrEmpty(option.BuffType) || string.IsNullOrEmpty(option.BuffDescriptionKey))
            {
                return string.Empty;
            }

            var value = ResolveScaledValue(option.BuffValueBase, option.BuffValuePerStage, stage);
            var duration = Mathf.Max(0, option.BuffDurationMonths);
            if (value <= 0 || duration <= 0)
            {
                return string.Empty;
            }

            return string.Format(LocalizationManager.GetText(option.BuffDescriptionKey), duration, value);
        }

        private string BuildEffectResultText(RunEffectData effect)
        {
            if (effect == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(effect.TitleKey))
            {
                return string.Format(LocalizationManager.GetText(effect.DescriptionKey), effect.RemainingMonths, effect.Value);
            }

            return LocalizationManager.GetText(effect.TitleKey) + "\n" + string.Format(LocalizationManager.GetText(effect.DescriptionKey), effect.RemainingMonths, effect.Value);
        }

        private static string ParseRewardMode(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue))
            {
                return "Progress";
            }

            if (string.Equals(rawValue, "Equipment", StringComparison.OrdinalIgnoreCase))
            {
                return "Equipment";
            }

            if (string.Equals(rawValue, "Skill", StringComparison.OrdinalIgnoreCase))
            {
                return "Skill";
            }

            if (string.Equals(rawValue, "Utility", StringComparison.OrdinalIgnoreCase))
            {
                return "Utility";
            }

            return "Progress";
        }

        private static MapNodeType ParseRewardNodeType(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue))
            {
                return MapNodeType.Battle;
            }

            if (string.Equals(rawValue, "Elite", StringComparison.OrdinalIgnoreCase))
            {
                return MapNodeType.Elite;
            }

            if (string.Equals(rawValue, "Boss", StringComparison.OrdinalIgnoreCase))
            {
                return MapNodeType.Boss;
            }

            return MapNodeType.Battle;
        }

        private string GetSkillRewardGainText(MapNodeType rewardNodeType)
        {
            switch (rewardNodeType)
            {
                case MapNodeType.Elite:
                    return LocalizationManager.GetText("map.choice_gain_skill_elite");
                case MapNodeType.Boss:
                    return LocalizationManager.GetText("map.choice_gain_skill_boss");
                default:
                    return LocalizationManager.GetText("map.choice_gain_skill");
            }
        }

        private void CompleteNodeRewardFlow(int stage, string title, string intro, BattleRewardResult reward, RunEffectData appliedEffect, bool isRandomEvent)
        {
            FinalizeStageEventState(stage, isRandomEvent);
            RefreshView();
            ShowNodeRewardPopup(title, BuildNodeRewardMessage(intro, reward, appliedEffect, IsEnglish()));
        }

        private void FinalizeStageEventState(int stage, bool isRandomEvent)
        {
            if (isRandomEvent)
            {
                GameProgressManager.MarkRandomStageEventTriggered(stage);
            }

            GameProgressManager.MarkCurrentStageCleared();
        }

        private string GetUtilityActionText(string utilityAction)
        {
            if (string.Equals(utilityAction, "OpenEquipment", StringComparison.OrdinalIgnoreCase))
            {
                return LocalizationManager.GetText("map.choice_open_equipment");
            }

            if (string.Equals(utilityAction, "OpenSkillOverview", StringComparison.OrdinalIgnoreCase))
            {
                return LocalizationManager.GetText("map.choice_open_skills");
            }

            return LocalizationManager.GetText("map.choice_open_route");
        }

        private static string BuildChoiceLabel(string title, params string[] lines)
        {
            var builder = new StringBuilder();
            builder.Append(title);
            for (var i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i]))
                {
                    continue;
                }

                builder.Append('\n').Append(lines[i]);
            }

            return builder.ToString();
        }

        private static string BuildMapSkillRewardChoiceLabel(SkillRewardOption option, bool isEnglish)
        {
            if (option == null)
            {
                return LocalizationManager.GetText("map.reward_choice_empty");
            }

            return GameProgressManager.BuildSkillRewardChoiceText(option, isEnglish);
        }

        private static string BuildMapSkillRewardToast(SkillRewardOption option, bool isEnglish)
        {
            if (option == null)
            {
                return string.Empty;
            }

            return string.Format(
                LocalizationManager.GetText(option.IsUpgrade ? "map.reward_toast_upgrade" : "map.reward_toast_learn"),
                option.CharacterName,
                option.SkillName,
                option.ResultLevel);
        }

        private string BuildNodeRewardMessage(string intro, BattleRewardResult reward, RunEffectData appliedEffect, bool isEnglish)
        {
            if (reward == null)
            {
                return intro;
            }

            var builder = new StringBuilder();
            builder.Append(intro)
                .Append('\n')
                .Append('\n')
                .Append(LocalizationManager.GetText("map.reward_title"))
                .Append('\n')
                .Append(LocalizationManager.GetText("map.reward_exp"))
                .Append(reward.ExpGained)
                .Append(" / ")
                .Append(GameProgressManager.BuildSpiritStoneGainText(reward, isEnglish, false));

            if (!string.IsNullOrEmpty(reward.DroppedEquipmentName))
            {
                builder.Append('\n')
                    .Append(LocalizationManager.GetText("map.reward_equipment"))
                    .Append(reward.DroppedEquipmentName);
            }

            if (reward.LevelsGained > 0)
            {
                builder.Append('\n')
                    .Append(LocalizationManager.GetText("map.reward_levelup"))
                    .Append(reward.LevelsGained);
            }

            if (appliedEffect != null)
            {
                builder.Append('\n')
                    .Append('\n')
                    .Append(BuildEffectResultText(appliedEffect));
            }

            return builder.ToString();
        }

        private void RefreshView()
        {
            var isEnglish = IsEnglish();
            var currentStage = Mathf.Max(1, GameProgressManager.GetCurrentStage());
            var maxReachableStage = GameProgressManager.GetMaxReachableStage();
            selectedStage = Mathf.Clamp(selectedStage <= 0 ? currentStage : selectedStage, 1, Mathf.Max(currentStage, maxReachableStage));

            if (titleText != null)
            {
                titleText.text = LocalizationManager.GetText("menu.title");
            }

            if (statusText != null)
            {
                statusText.text = BuildHeaderSummary(isEnglish, currentStage);
            }

            if (regionText != null)
            {
                regionText.text = isEnglish ? "Cultivation Route" : "修行灵图";
            }

            if (longevityText != null)
            {
                longevityText.text = isEnglish
                    ? "Lifespan  " + GameProgressManager.BuildLongevitySummary(isEnglish)
                    : "寿元  " + GameProgressManager.BuildLongevitySummary(isEnglish);
            }

            if (nodeDetailText != null)
            {
                nodeDetailText.text = BuildSelectedNodeDetail(isEnglish, currentStage);
            }

            if (routeText != null)
            {
                routeText.supportRichText = true;
                routeText.fontSize = 19;
                routeText.lineSpacing = 1.02f;
                routeText.text = BuildCondensedProfileText(isEnglish, currentStage);
            }

            RefreshButtons(isEnglish, currentStage);
            RefreshNodeGraph(currentStage);
        }

        private void RefreshButtons(bool isEnglish, int currentStage)
        {
            var canMovePrevious = !isMoving && GameProgressManager.CanTravelToStage(currentStage - 1);
            var canMoveNext = !isMoving && GameProgressManager.CanTravelToStage(currentStage + 1);
            var canEnterSelected = !isMoving;

            SetButtonText(previousButton, isEnglish ? "Back" : "回退");
            SetButtonText(nextButton, isEnglish ? "Forward" : "前行");
            SetButtonText(enterButton, isEnglish ? "Start" : "启程");
            SetButtonText(equipmentButton, isEnglish ? "Bag" : "行囊");
            SetButtonText(spiritConvertButton, isEnglish ? "Stones" : "灵石");
            SetButtonText(skillOverviewButton, isEnglish ? "Arts" : "心法");
            SetButtonText(resetButton, isEnglish ? "Reset" : "重置");
            SetButtonText(backButton, isEnglish ? "Title" : "返始");

            ApplyBottomButtonChrome(previousButton);
            ApplyBottomButtonChrome(nextButton);
            ApplyBottomButtonChrome(enterButton);
            ApplyBottomButtonChrome(equipmentButton);
            ApplyBottomButtonChrome(spiritConvertButton);
            ApplyBottomButtonChrome(skillOverviewButton);
            ApplyBottomButtonChrome(resetButton);
            ApplyBottomButtonChrome(backButton);

            if (previousButton != null) previousButton.interactable = canMovePrevious;
            if (nextButton != null) nextButton.interactable = canMoveNext;
            if (enterButton != null) enterButton.interactable = canEnterSelected;
            if (equipmentButton != null) equipmentButton.interactable = !isMoving;
            if (spiritConvertButton != null) spiritConvertButton.interactable = !isMoving;
            if (skillOverviewButton != null) skillOverviewButton.interactable = !isMoving;
            if (resetButton != null) resetButton.interactable = !isMoving;
            if (backButton != null) backButton.interactable = !isMoving;
        }

        private void RefreshNodeGraph(int currentStage)
        {
            if (nodeGraphRoot == null) return;

            Canvas.ForceUpdateCanvases();
            EnsureGraphPool();

            visibleStages.Clear();
            stagePositions.Clear();

            for (var slot = 0; slot < VisibleNodeCount; slot++)
            {
                var stage = currentStage + (slot - CenterNodeIndex);
                if (stage <= 0)
                {
                    visibleStages.Add(-1);
                    continue;
                }

                if (currentStage <= 1 && stage > currentStage + 2)
                {
                    visibleStages.Add(-1);
                    continue;
                }

                visibleStages.Add(stage);
            }

            var count = visibleStages.Count;

            for (var i = 0; i < nodeButtons.Count; i++)
            {
                var active = i < count && visibleStages[i] > 0;
                nodeButtons[i].gameObject.SetActive(active);
                if (i < nodeLines.Count) nodeLines[i].gameObject.SetActive(false);
                if (!active) continue;

                var stage = visibleStages[i];
                var position = GetLoopedNodePosition(i, count);
                stagePositions[stage] = position;

                var buttonRect = nodeButtons[i].GetComponent<RectTransform>();
                buttonRect.anchoredPosition = position;
                buttonRect.sizeDelta = new Vector2(NodeWidth, NodeHeight);

                var buttonImage = nodeButtons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = new Color(1f, 1f, 1f, 0f);
                }

                var label = nodeButtonLabels[i];
                if (label != null)
                {
                    label.text = BuildNodeLabel(stage);
                    label.alignment = TextAnchor.MiddleCenter;
                    label.fontSize = 28;
                    label.color = GetNodeTextColor(stage, currentStage);
                }

                var icon = nodeButtonIcons[i];
                if (icon != null)
                {
                    icon.sprite = GetNodeSprite(stage);
                    icon.color = GetNodeIconColor(stage, currentStage);
                    if (icon.sprite != null)
                    {
                        icon.SetNativeSize();
                    }
                    icon.gameObject.SetActive(icon.sprite != null);
                }

                nodeButtons[i].onClick.RemoveAllListeners();
                var capturedStage = stage;
                nodeButtons[i].onClick.AddListener(delegate { OnClickNodeStage(capturedStage); });

                if (i > 0)
                {
                    var previousStage = visibleStages[i - 1];
                    if (previousStage <= 0 || !stagePositions.ContainsKey(previousStage))
                    {
                        continue;
                    }

                    var previousPosition = stagePositions[previousStage];
                    ConfigureLine(nodeLines[i - 1].rectTransform, previousPosition, position);
                    nodeLines[i - 1].sprite = mapLineSprite;
                    nodeLines[i - 1].type = Image.Type.Simple;
                    nodeLines[i - 1].preserveAspect = true;
                    nodeLines[i - 1].color = new Color(0.94f, 0.82f, 0.52f, 0.82f);
                    nodeLines[i - 1].gameObject.SetActive(true);
                }
            }

            if (stagePositions.TryGetValue(currentStage, out var currentPosition))
            {
                UpdateTravelerMarkerPosition(currentPosition);
            }
        }

        private void EnsureGraphPool()
        {
            while (nodeButtons.Count < VisibleNodeCount)
            {
                var buttonObject = new GameObject("NodeButton" + nodeButtons.Count, typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(nodeGraphRoot, false);
                var button = buttonObject.GetComponent<Button>();
                var buttonImage = buttonObject.GetComponent<Image>();
                button.targetGraphic = buttonImage;
                buttonImage.color = new Color(1f, 1f, 1f, 0f);

                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(buttonObject.transform, false);
                var iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.5f, 1f);
                iconRect.anchorMax = new Vector2(0.5f, 1f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = new Vector2(0f, -34f);
                var iconImage = iconObject.GetComponent<Image>();
                iconImage.raycastTarget = false;

                var label = UIFactory.CreateText(buttonObject.transform, "Label", "Node", 28, TextAnchor.LowerCenter, new Color(0.1f, 0.08f, 0.05f, 1f));
                label.rectTransform.anchorMin = new Vector2(0f, 0f);
                label.rectTransform.anchorMax = new Vector2(1f, 0.68f);
                label.rectTransform.offsetMin = new Vector2(-24f, 0f);
                label.rectTransform.offsetMax = new Vector2(24f, 0f);
                label.supportRichText = false;

                nodeButtons.Add(button);
                nodeButtonLabels.Add(label);
                nodeButtonIcons.Add(iconImage);
            }

            while (nodeLines.Count < VisibleNodeCount - 1)
            {
                var lineObject = new GameObject("NodeLine" + nodeLines.Count, typeof(RectTransform), typeof(Image));
                lineObject.transform.SetParent(nodeGraphRoot, false);
                var rect = lineObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                var image = lineObject.GetComponent<Image>();
                image.raycastTarget = false;
                image.sprite = mapLineSprite;
                nodeLines.Add(image);
                lineObject.transform.SetAsFirstSibling();
            }

            if (travelerMarker == null)
            {
                var markerObject = new GameObject("TravelerMarker", typeof(RectTransform), typeof(Image));
                markerObject.transform.SetParent(nodeGraphRoot, false);
                var markerRect = markerObject.GetComponent<RectTransform>();
                markerRect.anchorMin = new Vector2(0.5f, 0.5f);
                markerRect.anchorMax = new Vector2(0.5f, 0.5f);
                markerRect.pivot = new Vector2(0.5f, 0.5f);
                markerRect.sizeDelta = new Vector2(22f, 22f);
                travelerMarker = markerObject.GetComponent<Image>();
                travelerMarker.color = new Color(0.98f, 0.82f, 0.46f, 1f);
                travelerMarker.raycastTarget = false;
            }
        }

        private void ConfigureLine(RectTransform rect, Vector2 from, Vector2 to)
        {
            var direction = to - from;
            rect.anchoredPosition = from + direction * 0.5f + new Vector2(0f, LineVerticalOffset);
            var lineImage = rect.GetComponent<Image>();
            if (lineImage != null && lineImage.sprite != null)
            {
                lineImage.SetNativeSize();
            }
            rect.localScale = new Vector3(direction.x < 0f ? -1f : 1f, 1f, 1f);
            rect.localRotation = Quaternion.identity;
        }

        private void UpdateTravelerMarkerPosition(Vector2 graphPosition)
        {
            if (travelerMarker == null) return;
            travelerMarker.rectTransform.anchoredPosition = graphPosition + new Vector2(44f, 6f);
            travelerMarker.gameObject.SetActive(true);
            travelerMarker.transform.SetAsLastSibling();
        }

        private string BuildSelectedNodeDetail(bool isEnglish, int currentStage)
        {
            var selectedNodeType = GameProgressManager.GetNodeType(selectedStage);
            var canReach = GameProgressManager.CanTravelToStage(selectedStage);
            var monthCost = Mathf.Max(1, Mathf.Abs(selectedStage - currentStage));
            var eventMode = GameProgressManager.GetStageEventMode(selectedStage);

            var builder = new StringBuilder();
            builder.Append(isEnglish ? "Stage " : "第 ")
                .Append(selectedStage)
                .Append(isEnglish ? "  " : " 节  ")
                .Append(GameProgressManager.GetNodeTypeLabel(isEnglish, selectedStage))
                .Append('\n')
                .Append(isEnglish ? "Travel: " : "耗时：")
                .Append(monthCost)
                .Append(isEnglish ? " mo" : "月")
                .Append("  ")
                .Append(isEnglish ? "Reach: " : "抵达：")
                .Append(canReach ? (isEnglish ? "Yes" : "可达") : (isEnglish ? "Locked" : "未达"));

            if (!GameProgressManager.IsBattleNode(selectedNodeType))
            {
                builder.Append('\n')
                    .Append(isEnglish ? "Mode: " : "事件：")
                    .Append(string.Equals(eventMode, "Random", StringComparison.OrdinalIgnoreCase)
                        ? (isEnglish ? "Random" : "随机")
                        : (isEnglish ? "Fixed" : "固定"));
            }

            var detail = GameProgressManager.BuildNodeDetail(isEnglish, selectedStage);
            if (!string.IsNullOrEmpty(detail))
            {
                builder.Append('\n').Append('\n').Append(detail);
            }

            return builder.ToString();
        }

        private string BuildCondensedProfileText(bool isEnglish, int currentStage)
        {
            var builder = new StringBuilder();
            var objectiveSummary = GameProgressManager.BuildObjectiveSummary(isEnglish);
            if (!string.IsNullOrEmpty(objectiveSummary))
            {
                builder.Append(objectiveSummary.Trim());
            }
            if (builder.Length > 0)
            {
                builder.Append('\n').Append('\n');
            }
            builder.Append(isEnglish ? "Realm " : "境界 ")
                .Append(GameProgressManager.GetCultivationLevel())
                .Append('\n')
                .Append(isEnglish ? "Exp " : "修为 ")
                .Append(GameProgressManager.GetCultivationExp())
                .Append('/')
                .Append(GameProgressManager.GetRequiredExpForNextLevel())
                .Append('\n')
                .Append(isEnglish ? "Gear " : "装备 ")
                .Append(GameProgressManager.GetOwnedEquipmentIds().Count)
                .Append("  ")
                .Append(isEnglish ? "Node " : "节点 ")
                .Append(currentStage);
            return builder.ToString();
        }

        private string BuildArrivalToast(int stage)
        {
            return string.Format(
                LocalizationManager.GetText("map.arrival_toast"),
                stage,
                GameProgressManager.GetNodeTypeLabel(IsEnglish(), stage));
        }

        private string BuildNodeLabel(int stage)
        {
            return GameProgressManager.GetNodeTypeLabel(IsEnglish(), stage);
        }

        private string BuildHeaderSummary(bool isEnglish, int currentStage)
        {
            var maxStage = Mathf.Max(currentStage, GameProgressManager.GetMaxReachableStage());
            return isEnglish
                ? "Realm: " + GameProgressManager.GetCultivationLevel() + "\nPath: " + currentStage + "/" + maxStage
                : "当前境界：" + GameProgressManager.GetCultivationLevel() + "\n已历节点：" + currentStage + "/" + maxStage;
        }

        private Vector2 GetLoopedNodePosition(int index, int visibleCount)
        {
            if (visibleCount <= 0)
            {
                return Vector2.zero;
            }

            var startOffset = Mathf.Max(0, (NodeLayoutPattern.Length - visibleCount) / 2);
            var patternIndex = Mathf.Clamp(startOffset + index, 0, NodeLayoutPattern.Length - 1);
            return NodeLayoutPattern[patternIndex];
        }

        private Sprite GetNodeSprite(int stage)
        {
            switch (GameProgressManager.GetNodeType(stage))
            {
                case MapNodeType.Rest:
                    return restNodeSprite != null ? restNodeSprite : eventNodeSprite;
                case MapNodeType.Elite:
                    return eliteNodeSprite != null ? eliteNodeSprite : battleNodeSprite;
                case MapNodeType.Boss:
                    return bossNodeSprite != null ? bossNodeSprite : eliteNodeSprite;
                case MapNodeType.Battle:
                    return battleNodeSprite != null ? battleNodeSprite : restNodeSprite;
                default:
                    return eventNodeSprite != null ? eventNodeSprite : restNodeSprite;
            }
        }

        private Color GetNodeTextColor(int stage, int currentStage)
        {
            if (stage == currentStage) return new Color(0.48f, 0.28f, 0.1f, 1f);
            if (stage == selectedStage) return new Color(0.12f, 0.12f, 0.12f, 1f);
            if (GameProgressManager.CanTravelToStage(stage)) return new Color(0.08f, 0.08f, 0.08f, 1f);
            return new Color(0.48f, 0.48f, 0.48f, 0.82f);
        }

        private Color GetNodeIconColor(int stage, int currentStage)
        {
            if (stage == currentStage) return new Color(1f, 0.9f, 0.55f, 1f);
            if (stage == selectedStage) return new Color(0.94f, 0.94f, 0.94f, 1f);
            if (GameProgressManager.CanTravelToStage(stage)) return new Color(0.9f, 0.9f, 0.9f, 1f);
            return new Color(0.65f, 0.65f, 0.65f, 0.72f);
        }

        private bool IsEnglish()
        {
            return LocalizationManager.Instance != null
                && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
        }

        private static void SetButtonText(Button button, string text)
        {
            if (button == null) return;
            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.text = text;
        }

        private void ApplyBottomButtonChrome(Button button)
        {
            if (button == null)
            {
                return;
            }

            var rootGraphic = button.GetComponent<Image>();
            if (rootGraphic != null)
            {
                rootGraphic.sprite = null;
                rootGraphic.color = new Color(1f, 1f, 1f, 0f);
                rootGraphic.type = Image.Type.Simple;
            }

            var images = button.GetComponentsInChildren<Image>(true);
            for (var i = 0; i < images.Length; i++)
            {
                if (!string.Equals(images[i].gameObject.name, "ButtonSurface", StringComparison.Ordinal))
                {
                    continue;
                }

                images[i].sprite = bottomButtonSprite;
                images[i].color = Color.white;
                images[i].type = Image.Type.Sliced;
                images[i].preserveAspect = false;
            }
        }
    }
}




