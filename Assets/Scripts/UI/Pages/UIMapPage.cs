using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
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
        private const int AnimatedVisibleNodeCount = 6;
        private const int CenterNodeIndex = 2;
        private const float NodeWidth = 164f;
        private const float NodeHeight = 148f;
        private const float LineVerticalOffset = 40f;
        private static readonly Color MapLineTint = new Color(1f, 0.96f, 0.82f, 0.92f);
        private static readonly Color MapLineGlowInner = new Color(1f, 0.52f, 0.18f, 0.42f);
        private static readonly Color MapLineGlowOuter = new Color(0.78f, 0.08f, 0.04f, 0.28f);
        private static readonly Color MapLineLift = new Color(1f, 0.86f, 0.5f, 0.3f);
        private static readonly Color NodeLabelColor = new Color(0.98f, 0.97f, 0.95f, 1f);
        private static readonly Color NodeLabelCurrentColor = new Color(0.98f, 0.92f, 0.8f, 1f);
        private static readonly Color NodeLabelOutlineColor = new Color(0.04f, 0.04f, 0.04f, 0.58f);
        private static readonly Color NodeLabelCurrentOutlineColor = new Color(0.14f, 0.11f, 0.06f, 0.74f);
        private static readonly Color NodeIconShadowColor = new Color(0f, 0f, 0f, 0.24f);
        private static readonly Color NodeIconCurrentShadowColor = new Color(0.2f, 0.16f, 0.09f, 0.42f);
        private static readonly Color NodeGlowColor = new Color(1f, 0.9f, 0.72f, 0.4f);
        private static readonly Color NodeGlowCurrentColor = new Color(1f, 1f, 1f, 0.8f);
        private static readonly float[] NodeVerticalPattern = { -330f, -110f, 110f, 330f, 550f };
        private const float SnakeSideOffset = 96f;
        private const string NodeGlowShaderName = "UI/MapNodeGlow";

        [SerializeField] private Text titleText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text regionText;
        [SerializeField] private Text longevityText;
        [SerializeField] private Image backgroundImage;
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
        [SerializeField] private Sprite villageNodeSprite;
        [SerializeField] private Sprite restNodeSprite;
        [SerializeField] private Sprite battleNodeSprite;
        [SerializeField] private Sprite eliteNodeSprite;
        [SerializeField] private Sprite bossNodeSprite;
        [SerializeField] private Sprite eventNodeSprite;
        [SerializeField] private Sprite fallbackNodeSprite;
        [SerializeField] private Sprite chapterBackground1;
        [SerializeField] private Sprite chapterBackground2;
        [SerializeField] private Sprite chapterBackground3;
        [SerializeField] private Sprite chapterBackground4;
        [SerializeField] private Sprite chapterBackground5;
        [SerializeField] private Sprite chapterBackground6;

        private readonly List<Button> nodeButtons = new List<Button>();
        private readonly List<Text> nodeButtonLabels = new List<Text>();
        private readonly List<Image> nodeButtonIcons = new List<Image>();
        private readonly List<Image> nodeButtonGlows = new List<Image>();
        private readonly List<Image> nodeLines = new List<Image>();
        private readonly List<int> visibleStages = new List<int>();
        private readonly Dictionary<int, Vector2> stagePositions = new Dictionary<int, Vector2>();

        private Image travelerMarker;
        private Coroutine moveCoroutine;
        private Sequence moveSequence;
        private int selectedStage;
        private bool isMoving;
        private RectTransform activeIconRect;
        private RectTransform activeLabelRect;
        private RectTransform activeGlowRect;
        private Image activeGlowImage;
        private Shadow activeIconShadow;
        private Vector2 nodeGraphBaseAnchoredPosition;
        private bool nodeGraphBasePositionCached;
        private static Sprite fadingLineSprite;
        private static Material nodeGlowMaterial;

        public override void OnOpen(object data)
        {
            GameProgressManager.StartRun();
            selectedStage = Mathf.Max(1, GameProgressManager.GetCurrentStage());
            RefreshView();
            StoryManager.TryTrigger("StartGame", Mathf.Max(1, GameProgressManager.GetCurrentStage()));
        }

        private void Awake()
        {
            CacheNodeGraphBasePosition();
            ApplyTextChrome();
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
            CacheNodeGraphBasePosition();
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

        private void Update()
        {
            UpdateCurrentNodePulse();
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
            if (moveSequence != null && moveSequence.IsActive())
            {
                moveSequence.Kill();
                moveSequence = null;
            }
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
                    ResetAnimatedMapState();
                    isMoving = false;
                    moveCoroutine = null;
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
            CacheNodeGraphBasePosition();
            var direction = toStage >= fromStage ? 1 : -1;
            RenderNodeGraph(BuildVisibleStages(fromStage, direction, true), fromStage, direction);

            Vector2 fromPosition;
            Vector2 toPosition;
            if (!stagePositions.TryGetValue(fromStage, out fromPosition) || !stagePositions.TryGetValue(toStage, out toPosition))
            {
                yield return null;
                yield break;
            }

            var fixedMarkerY = fromPosition.y + 6f;
            var targetGraphYOffset = fromPosition.y - toPosition.y;
            var tweenState = new MapMoveTweenState
            {
                Progress = 0f,
                GraphYOffset = 0f,
                MarkerX = fromPosition.x
            };

            bool completed = false;
            moveSequence = DOTween.Sequence().SetUpdate(true);
            moveSequence.Join(DOTween.To(
                    () => tweenState.Progress,
                    value =>
                    {
                        tweenState.Progress = value;
                        UpdateAnimatedMapVisibility(value, direction);
                    },
                    1f,
                    MoveDurationPerStep)
                .SetEase(Ease.InOutSine));
            moveSequence.Join(DOTween.To(
                    () => tweenState.GraphYOffset,
                    value =>
                    {
                        tweenState.GraphYOffset = value;
                        SetNodeGraphAnimatedOffset(value);
                        UpdateTravelerMarkerPosition(new Vector2(tweenState.MarkerX, fixedMarkerY - value));
                    },
                    targetGraphYOffset,
                    MoveDurationPerStep)
                .SetEase(Ease.InOutSine));
            moveSequence.Join(DOTween.To(
                    () => tweenState.MarkerX,
                    value =>
                    {
                        tweenState.MarkerX = value;
                        UpdateTravelerMarkerPosition(new Vector2(value, fixedMarkerY - tweenState.GraphYOffset));
                    },
                    toPosition.x,
                    MoveDurationPerStep)
                .SetEase(Ease.InOutSine));
            moveSequence.OnComplete(() =>
            {
                UpdateAnimatedMapVisibility(1f, direction);
                SetNodeGraphAnimatedOffset(targetGraphYOffset);
                UpdateTravelerMarkerPosition(new Vector2(toPosition.x, fixedMarkerY - targetGraphYOffset));
                completed = true;
            });
            moveSequence.Play();

            yield return new WaitUntil(() => completed);
            moveSequence = null;
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
            RefreshBackground(currentStage);

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
                var sceneTheme = GameProgressManager.GetStageTheme(isEnglish, currentStage);
                regionText.text = isEnglish ? "Current Scene: " + sceneTheme : "\u5f53\u524d\u573a\u666f\uff1a" + sceneTheme;
            }

            if (longevityText != null)
            {
                longevityText.text = isEnglish
                    ? "Lifespan  " + GameProgressManager.BuildLongevitySummary(isEnglish)
                    : "\u5bff\u5143  " + GameProgressManager.BuildLongevitySummary(isEnglish);
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

        private void RefreshBackground(int currentStage)
        {
            if (backgroundImage == null)
            {
                return;
            }

            var sprite = GetChapterBackgroundSprite(currentStage);
            if (sprite != null)
            {
                backgroundImage.sprite = sprite;
                backgroundImage.color = Color.white;
            }
        }

        private void RefreshButtons(bool isEnglish, int currentStage)
        {
            var canMovePrevious = !isMoving && GameProgressManager.CanTravelToStage(currentStage - 1);
            var canMoveNext = !isMoving && GameProgressManager.CanTravelToStage(currentStage + 1);
            var canEnterSelected = !isMoving;

            SetButtonText(previousButton, isEnglish ? "Back" : "\u56de\u9000");
            SetButtonText(nextButton, isEnglish ? "Ahead" : "\u524d\u884c");
            SetButtonText(enterButton, isEnglish ? "Enter" : "\u8fdb\u5165\u6b64\u5883");
            SetButtonText(equipmentButton, isEnglish ? "Bag" : "\u884c\u56ca");
            SetButtonText(spiritConvertButton, isEnglish ? "Stone" : "\u7075\u77f3");
            SetButtonText(skillOverviewButton, isEnglish ? "Arts" : "\u5fc3\u6cd5");
            SetButtonText(resetButton, isEnglish ? "Reset" : "\u91cd\u7f6e");
            SetButtonText(backButton, isEnglish ? "Title" : "\u8fd4\u59cb");

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

            CacheNodeGraphBasePosition();
            nodeGraphRoot.anchoredPosition = nodeGraphBaseAnchoredPosition;
            Canvas.ForceUpdateCanvases();
            EnsureGraphPool();
            RenderNodeGraph(BuildVisibleStages(currentStage, 0, false), currentStage, 0);
        }

        private void RenderNodeGraph(List<int> stagesToRender, int currentStage, int animationDirection)
        {
            visibleStages.Clear();
            stagePositions.Clear();
            activeLabelRect = null;
            activeIconRect = null;
            activeGlowRect = null;
            activeGlowImage = null;
            activeIconShadow = null;
            visibleStages.AddRange(stagesToRender);

            var count = visibleStages.Count;

            for (var i = 0; i < nodeButtons.Count; i++)
            {
                var active = i < count && visibleStages[i] > 0;
                nodeButtons[i].gameObject.SetActive(active);
                if (i < nodeLines.Count) nodeLines[i].gameObject.SetActive(false);
                if (!active) continue;

                var stage = visibleStages[i];
                var position = GetLoopedNodePosition(stage, i, count, animationDirection);
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
                    label.color = stage == currentStage ? NodeLabelCurrentColor : NodeLabelColor;
                    label.rectTransform.localScale = Vector3.one;
                    var labelOutline = label.GetComponent<Outline>();
                    if (labelOutline != null)
                    {
                        labelOutline.effectColor = stage == currentStage ? NodeLabelCurrentOutlineColor : NodeLabelOutlineColor;
                    }
                }

                var icon = nodeButtonIcons[i];
                var glow = i < nodeButtonGlows.Count ? nodeButtonGlows[i] : null;
                if (icon != null)
                {
                    icon.sprite = GetNodeSprite(stage);
                    icon.color = Color.white;
                    icon.rectTransform.localScale = Vector3.one;
                    if (icon.sprite != null)
                    {
                        icon.SetNativeSize();
                    }
                    icon.gameObject.SetActive(icon.sprite != null);
                    var iconShadow = icon.GetComponent<Shadow>();
                    if (iconShadow != null)
                    {
                        iconShadow.effectDistance = stage == currentStage ? Vector2.zero : new Vector2(2f, -2f);
                        iconShadow.effectColor = stage == currentStage ? new Color(0f, 0f, 0f, 0f) : NodeIconShadowColor;
                    }
                }

                if (glow != null)
                {
                    glow.sprite = icon != null ? icon.sprite : null;
                    glow.color = stage == currentStage ? NodeGlowCurrentColor : NodeGlowColor;
                    glow.material = GetNodeGlowMaterial();
                    glow.rectTransform.localScale = stage == currentStage ? Vector3.one : Vector3.one * 1.04f;
                    if (glow.sprite != null)
                    {
                        glow.SetNativeSize();
                    }
                    glow.gameObject.SetActive(glow.sprite != null);
                }

                if (stage == currentStage)
                {
                    activeGlowRect = glow != null ? glow.rectTransform : null;
                    activeGlowImage = glow;
                    activeIconRect = icon != null ? icon.rectTransform : null;
                    activeLabelRect = label != null ? label.rectTransform : null;
                    activeIconShadow = icon != null ? icon.GetComponent<Shadow>() : null;
                }

                nodeButtons[i].onClick.RemoveAllListeners();
                var capturedStage = stage;
                nodeButtons[i].onClick.AddListener(delegate { OnClickNodeStage(capturedStage); });
                nodeButtons[i].transform.SetAsLastSibling();

                if (i > 0)
                {
                    var previousStage = visibleStages[i - 1];
                    if (previousStage <= 0 || !stagePositions.ContainsKey(previousStage))
                    {
                        continue;
                    }

                    var previousPosition = stagePositions[previousStage];
                    ConfigureLine(nodeLines[i - 1].rectTransform, previousPosition, position);
                    ConfigureNodeLineVisual(nodeLines[i - 1], MapLineTint.a);
                    nodeLines[i - 1].gameObject.SetActive(true);
                }
            }

            if (travelerMarker != null)
            {
                travelerMarker.gameObject.SetActive(false);
            }
        }

        private void EnsureGraphPool()
        {
            while (nodeButtons.Count < AnimatedVisibleNodeCount)
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

                var glowObject = new GameObject("Glow", typeof(RectTransform), typeof(Image));
                glowObject.transform.SetParent(buttonObject.transform, false);
                glowObject.transform.SetAsFirstSibling();
                var glowRect = glowObject.GetComponent<RectTransform>();
                glowRect.anchorMin = new Vector2(0.5f, 1f);
                glowRect.anchorMax = new Vector2(0.5f, 1f);
                glowRect.pivot = new Vector2(0.5f, 0.5f);
                glowRect.anchoredPosition = new Vector2(0f, -34f);
                var glowImage = glowObject.GetComponent<Image>();
                glowImage.raycastTarget = false;
                glowImage.color = NodeGlowCurrentColor;
                glowImage.material = GetNodeGlowMaterial();

                var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(buttonObject.transform, false);
                var iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.5f, 1f);
                iconRect.anchorMax = new Vector2(0.5f, 1f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = new Vector2(0f, -34f);
                var iconImage = iconObject.GetComponent<Image>();
                iconImage.raycastTarget = false;
                var iconShadow = iconObject.AddComponent<Shadow>();
                iconShadow.effectColor = NodeIconShadowColor;
                iconShadow.effectDistance = new Vector2(2f, -2f);
                iconShadow.useGraphicAlpha = true;

                var label = UIFactory.CreateText(buttonObject.transform, "Label", "Node", 28, TextAnchor.LowerCenter, NodeLabelColor);
                label.rectTransform.anchorMin = new Vector2(0f, 0f);
                label.rectTransform.anchorMax = new Vector2(1f, 0.68f);
                label.rectTransform.offsetMin = new Vector2(-24f, 0f);
                label.rectTransform.offsetMax = new Vector2(24f, 0f);
                label.supportRichText = false;
                var labelOutline = label.gameObject.AddComponent<Outline>();
                labelOutline.effectColor = NodeLabelOutlineColor;
                labelOutline.effectDistance = new Vector2(1.5f, -1.5f);
                labelOutline.useGraphicAlpha = true;

                nodeButtons.Add(button);
                nodeButtonLabels.Add(label);
                nodeButtonGlows.Add(glowImage);
                nodeButtonIcons.Add(iconImage);
            }

            while (nodeLines.Count < AnimatedVisibleNodeCount - 1)
            {
                var lineObject = new GameObject("NodeLine" + nodeLines.Count, typeof(RectTransform), typeof(Image));
                lineObject.transform.SetParent(nodeGraphRoot, false);
                var rect = lineObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                var image = lineObject.GetComponent<Image>();
                image.raycastTarget = false;
                image.sprite = GetFadingLineSprite();
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = MapLineGlowOuter;
                EnsureLineLayer(lineObject.transform, "InnerGlow", 0.58f);
                EnsureLineLayer(lineObject.transform, "Core", 0.18f);
                var shadow = lineObject.AddComponent<Shadow>();
                shadow.effectColor = MapLineLift;
                shadow.effectDistance = new Vector2(2f, -2f);
                shadow.useGraphicAlpha = true;
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
                markerRect.sizeDelta = new Vector2(18f, 18f);
                travelerMarker = markerObject.GetComponent<Image>();
                travelerMarker.color = new Color(0.98f, 0.9f, 0.72f, 0.95f);
                travelerMarker.raycastTarget = false;
            }
        }

        private void ConfigureLine(RectTransform rect, Vector2 from, Vector2 to)
        {
            var start = from + new Vector2(0f, LineVerticalOffset);
            var end = to + new Vector2(0f, LineVerticalOffset);
            var direction = end - start;
            var length = direction.magnitude;

            rect.anchoredPosition = start + direction * 0.5f;
            rect.sizeDelta = new Vector2(length, 20f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        private void ConfigureNodeLineVisual(Image line, float alpha)
        {
            if (line == null)
            {
                return;
            }

            line.sprite = GetFadingLineSprite();
            line.type = Image.Type.Simple;
            line.preserveAspect = false;
            line.color = WithAlpha(MapLineGlowOuter, alpha * MapLineGlowOuter.a / MapLineTint.a);

            var innerGlow = line.transform.Find("InnerGlow")?.GetComponent<Image>();
            if (innerGlow != null)
            {
                innerGlow.sprite = GetFadingLineSprite();
                innerGlow.type = Image.Type.Simple;
                innerGlow.preserveAspect = false;
                innerGlow.color = WithAlpha(MapLineGlowInner, alpha * MapLineGlowInner.a / MapLineTint.a);
            }

            var core = line.transform.Find("Core")?.GetComponent<Image>();
            if (core != null)
            {
                core.sprite = GetFadingLineSprite();
                core.type = Image.Type.Simple;
                core.preserveAspect = false;
                core.color = WithAlpha(MapLineTint, alpha);
            }
        }

        private static Image EnsureLineLayer(Transform parent, string name, float scaleY)
        {
            var existing = parent.Find(name)?.GetComponent<Image>();
            if (existing != null)
            {
                ConfigureLineLayerRect(existing.rectTransform, scaleY);
                existing.raycastTarget = false;
                return existing;
            }

            var layerObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            layerObject.transform.SetParent(parent, false);
            var image = layerObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.sprite = GetFadingLineSprite();
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            ConfigureLineLayerRect(image.rectTransform, scaleY);
            return image;
        }

        private static void ConfigureLineLayerRect(RectTransform rect, float scaleY)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = new Vector3(1f, scaleY, 1f);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private static Sprite GetFadingLineSprite()
        {
            if (fadingLineSprite != null)
            {
                return fadingLineSprite;
            }

            const int width = 96;
            var texture = new Texture2D(width, 1, TextureFormat.RGBA32, false)
            {
                name = "GeneratedMapFadingLine",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (var x = 0; x < width; x++)
            {
                var t = x / (width - 1f);
                var edge = Mathf.Min(t, 1f - t);
                var alpha = Mathf.SmoothStep(0.18f, 1f, Mathf.Clamp01(edge / 0.22f));
                texture.SetPixel(x, 0, new Color(1f, 1f, 1f, alpha));
            }

            texture.Apply(false, true);
            fadingLineSprite = Sprite.Create(texture, new Rect(0f, 0f, width, 1f), new Vector2(0.5f, 0.5f), 100f);
            fadingLineSprite.name = "GeneratedMapFadingLineSprite";
            return fadingLineSprite;
        }

        private void UpdateTravelerMarkerPosition(Vector2 graphPosition)
        {
            if (travelerMarker == null) return;
            travelerMarker.rectTransform.anchoredPosition = graphPosition + new Vector2(44f, 6f);
            travelerMarker.gameObject.SetActive(true);
            travelerMarker.transform.SetAsLastSibling();
        }

        private void CacheNodeGraphBasePosition()
        {
            if (nodeGraphRoot == null || nodeGraphBasePositionCached)
            {
                return;
            }

            nodeGraphBaseAnchoredPosition = nodeGraphRoot.anchoredPosition;
            nodeGraphBasePositionCached = true;
        }

        private void SetNodeGraphAnimatedOffset(float yOffset)
        {
            if (nodeGraphRoot == null)
            {
                return;
            }

            nodeGraphRoot.anchoredPosition = nodeGraphBaseAnchoredPosition + new Vector2(0f, yOffset);
        }

        private void UpdateAnimatedMapVisibility(float progress, int direction)
        {
            var count = visibleStages.Count;
            if (count <= 0)
            {
                return;
            }

            var frontSlot = direction >= 0 ? 0 : count - 1;
            var backSlot = direction >= 0 ? count - 1 : 0;
            var fadeOutAlpha = Mathf.Lerp(1f, 0f, progress);
            var fadeInAlpha = Mathf.Lerp(0f, 1f, progress);

            for (var i = 0; i < nodeButtons.Count; i++)
            {
                if (i >= visibleStages.Count || visibleStages[i] <= 0 || !nodeButtons[i].gameObject.activeSelf)
                {
                    continue;
                }

                var alpha = 1f;
                if (i == frontSlot)
                {
                    alpha = fadeOutAlpha;
                }
                else if (i == backSlot)
                {
                    alpha = fadeInAlpha;
                }

                var icon = nodeButtonIcons[i];
                if (icon != null)
                {
                    var iconColor = icon.color;
                    iconColor.a = alpha;
                    icon.color = iconColor;
                }

                var label = nodeButtonLabels[i];
                if (label != null)
                {
                    var labelColor = label.color;
                    labelColor.a = alpha;
                    label.color = labelColor;
                }
            }

            for (var i = 0; i < nodeLines.Count; i++)
            {
                var line = nodeLines[i];
                if (line == null || !line.gameObject.activeSelf)
                {
                    continue;
                }

                var lineColor = MapLineTint;
                if ((direction >= 0 && i == 0) || (direction < 0 && i == nodeLines.Count - 1))
                {
                    lineColor.a = Mathf.Lerp(MapLineTint.a, 0f, progress);
                }
                else if ((direction >= 0 && i == nodeLines.Count - 1) || (direction < 0 && i == 0))
                {
                    lineColor.a = Mathf.Lerp(0f, MapLineTint.a, progress);
                }
                else
                {
                    lineColor.a = MapLineTint.a;
                }
                ConfigureNodeLineVisual(line, lineColor.a);
            }
        }

        private void ResetAnimatedMapState()
        {
            if (moveSequence != null && moveSequence.IsActive())
            {
                moveSequence.Kill();
                moveSequence = null;
            }

            if (nodeGraphRoot != null)
            {
                nodeGraphRoot.anchoredPosition = nodeGraphBaseAnchoredPosition;
            }

            if (travelerMarker != null)
            {
                travelerMarker.gameObject.SetActive(false);
            }
        }

        private void UpdateCurrentNodePulse()
        {
            if (activeIconRect == null)
            {
                return;
            }

            var pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 2.6f) * 0.5f;
            if (activeGlowRect != null)
            {
                activeGlowRect.localScale = Vector3.one * Mathf.Lerp(1.05f, 1.13f, pulse);
            }

            if (activeGlowImage != null)
            {
                var glowColor = activeGlowImage.color;
                glowColor.a = Mathf.Lerp(0.58f, 0.84f, pulse);
                activeGlowImage.color = glowColor;
            }

            activeIconRect.localScale = Vector3.one * Mathf.Lerp(1.02f, 1.1f, pulse);
            if (activeLabelRect != null)
            {
                activeLabelRect.localScale = Vector3.one * Mathf.Lerp(1f, 1.04f, pulse);
            }

        }

        private static Material GetNodeGlowMaterial()
        {
            if (nodeGlowMaterial != null)
            {
                return nodeGlowMaterial;
            }

            var shader = Shader.Find(NodeGlowShaderName);
            if (shader == null)
            {
                return null;
            }

            nodeGlowMaterial = new Material(shader)
            {
                name = "GeneratedMapNodeGlow",
                hideFlags = HideFlags.HideAndDontSave
            };
            return nodeGlowMaterial;
        }

        private void ApplyTextChrome()
        {
            ApplyTextShadow(titleText, new Color(0.96f, 0.92f, 0.84f, 0.18f), new Vector2(1f, -1f), null);
            ApplyTextShadow(statusText, new Color(0.96f, 0.92f, 0.84f, 0.2f), new Vector2(1f, -1f), null);
            ApplyTextShadow(regionText, new Color(0.12f, 0.08f, 0.03f, 0.24f), new Vector2(1f, -1f), null);
            ApplyTextShadow(longevityText, new Color(0f, 0f, 0f, 0.28f), new Vector2(1.5f, -1.5f), null);
            ApplyTextShadow(
                nodeDetailText,
                new Color(0f, 0f, 0f, 0.32f),
                new Vector2(1.5f, -1.5f),
                new Color(0.95f, 0.92f, 0.86f, 1f));
            ApplyTextShadow(
                routeText,
                new Color(0f, 0f, 0f, 0.32f),
                new Vector2(1.5f, -1.5f),
                new Color(0.95f, 0.92f, 0.86f, 1f));

            ApplyTextOutline(titleText, new Color(0f, 0f, 0f, 0.42f), new Vector2(1f, -1f));
            ApplyTextOutline(statusText, new Color(0.18f, 0.12f, 0.06f, 0.52f), new Vector2(1f, -1f));
            ApplyTextOutline(regionText, new Color(0.18f, 0.12f, 0.06f, 0.56f), new Vector2(1f, -1f));
            ApplyTextOutline(longevityText, new Color(0f, 0f, 0f, 0.62f), new Vector2(1.2f, -1.2f));
            ApplyTextOutline(nodeDetailText, new Color(0f, 0f, 0f, 0.68f), new Vector2(1.2f, -1.2f));
            ApplyTextOutline(routeText, new Color(0f, 0f, 0f, 0.68f), new Vector2(1.2f, -1.2f));
        }

        private static void ApplyTextShadow(Text text, Color shadowColor, Vector2 effectDistance, Color? overrideColor)
        {
            if (text == null)
            {
                return;
            }

            if (overrideColor.HasValue)
            {
                text.color = overrideColor.Value;
            }

            var shadow = text.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = text.gameObject.AddComponent<Shadow>();
            }

            shadow.effectColor = shadowColor;
            shadow.effectDistance = effectDistance;
            shadow.useGraphicAlpha = true;
        }

        private static void ApplyTextOutline(Text text, Color outlineColor, Vector2 effectDistance)
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

            outline.effectColor = outlineColor;
            outline.effectDistance = effectDistance;
            outline.useGraphicAlpha = true;
        }

        private string BuildSelectedNodeDetail(bool isEnglish, int currentStage)
        {
            var selectedNodeType = GameProgressManager.GetNodeType(selectedStage);
            var canReach = GameProgressManager.CanTravelToStage(selectedStage);
            var monthCost = Mathf.Max(1, Mathf.Abs(selectedStage - currentStage));
            var eventMode = GameProgressManager.GetStageEventMode(selectedStage);

            var builder = new StringBuilder();
            builder.Append(isEnglish ? "Stage " : "\u7b2c ")
                .Append(selectedStage)
                .Append(isEnglish ? "  " : " \u8282  ")
                .Append(GameProgressManager.GetNodeTypeLabel(isEnglish, selectedStage))
                .Append('\n')
                .Append(isEnglish ? "Travel: " : "\u8017\u65f6\uff1a")
                .Append(monthCost)
                .Append(isEnglish ? " mo" : "\u6708")
                .Append("  ")
                .Append(isEnglish ? "Reach: " : "\u62b5\u8fbe\uff1a")
                .Append(canReach ? (isEnglish ? "Yes" : "\u53ef\u8fbe") : (isEnglish ? "Locked" : "\u672a\u8fbe"));

            if (!GameProgressManager.IsBattleNode(selectedNodeType))
            {
                builder.Append('\n')
                    .Append(isEnglish ? "Mode: " : "\u4e8b\u4ef6\uff1a")
                    .Append(string.Equals(eventMode, "Random", StringComparison.OrdinalIgnoreCase)
                        ? (isEnglish ? "Random" : "\u968f\u673a")
                        : (isEnglish ? "Fixed" : "\u56fa\u5b9a"));
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
            builder.Append(isEnglish ? "Realm " : "\u5883\u754c ")
                .Append(GameProgressManager.GetCultivationLevel())
                .Append('\n')
                .Append(isEnglish ? "Exp " : "\u4fee\u4e3a ")
                .Append(GameProgressManager.GetCultivationExp())
                .Append('/')
                .Append(GameProgressManager.GetRequiredExpForNextLevel())
                .Append('\n')
                .Append(isEnglish ? "Gear " : "\u88c5\u5907 ")
                .Append(GameProgressManager.GetOwnedEquipmentIds().Count)
                .Append("  ")
                .Append(isEnglish ? "Node " : "\u8282\u70b9 ")
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
                ? "Visited Nodes: " + currentStage + "/" + maxStage
                : "\u5df2\u5386\u8282\u70b9\uff1a" + currentStage + "/" + maxStage;
        }

        private List<int> BuildVisibleStages(int currentStage, int direction, bool includeIncomingStage)
        {
            var stages = new List<int>(includeIncomingStage ? AnimatedVisibleNodeCount : VisibleNodeCount);
            var startOffset = includeIncomingStage
                ? (direction >= 0 ? -CenterNodeIndex : -CenterNodeIndex - 1)
                : -CenterNodeIndex;
            var slotCount = includeIncomingStage ? AnimatedVisibleNodeCount : VisibleNodeCount;

            for (var slot = 0; slot < slotCount; slot++)
            {
                var stage = currentStage + startOffset + slot;
                if (stage <= 0)
                {
                    stages.Add(-1);
                    continue;
                }

                if (!includeIncomingStage && currentStage <= 1 && stage > currentStage + 2)
                {
                    stages.Add(-1);
                    continue;
                }

                stages.Add(stage);
            }

            return stages;
        }

        private Vector2 GetLoopedNodePosition(int stage, int index, int visibleCount, int animationDirection)
        {
            if (visibleCount <= 0)
            {
                return Vector2.zero;
            }

            float y;
            if (visibleCount == AnimatedVisibleNodeCount)
            {
                if (animationDirection >= 0)
                {
                    if (index >= AnimatedVisibleNodeCount - 1)
                    {
                        var last = NodeVerticalPattern[NodeVerticalPattern.Length - 1];
                        var prev = NodeVerticalPattern[NodeVerticalPattern.Length - 2];
                        y = last + (last - prev);
                    }
                    else
                    {
                        y = NodeVerticalPattern[Mathf.Clamp(index, 0, NodeVerticalPattern.Length - 1)];
                    }
                }
                else
                {
                    if (index <= 0)
                    {
                        y = NodeVerticalPattern[0] - (NodeVerticalPattern[1] - NodeVerticalPattern[0]);
                    }
                    else
                    {
                        var fixedIndex = Mathf.Clamp(index - 1, 0, NodeVerticalPattern.Length - 1);
                        y = NodeVerticalPattern[fixedIndex];
                    }
                }
            }
            else
            {
                y = NodeVerticalPattern[Mathf.Clamp(index, 0, NodeVerticalPattern.Length - 1)];
            }

            var x = stage % 2 == 1 ? SnakeSideOffset : -SnakeSideOffset;
            return new Vector2(x, y);
        }

        private Sprite GetNodeSprite(int stage)
        {
            if (IsVillageStage(stage))
            {
                return villageNodeSprite != null ? villageNodeSprite : restNodeSprite;
            }

            switch (GameProgressManager.GetNodeType(stage))
            {
                case MapNodeType.Village:
                    return villageNodeSprite != null ? villageNodeSprite : restNodeSprite;
                case MapNodeType.Rest:
                case MapNodeType.Shop:
                    return restNodeSprite != null ? restNodeSprite : eventNodeSprite;
                case MapNodeType.Elite:
                    return eliteNodeSprite != null ? eliteNodeSprite : battleNodeSprite;
                case MapNodeType.Boss:
                    return bossNodeSprite != null ? bossNodeSprite : eliteNodeSprite;
                case MapNodeType.Event:
                    return eventNodeSprite != null ? eventNodeSprite : fallbackNodeSprite;
                case MapNodeType.Battle:
                    return battleNodeSprite != null ? battleNodeSprite : restNodeSprite;
                case MapNodeType.Other:
                default:
                    return fallbackNodeSprite != null ? fallbackNodeSprite : eventNodeSprite;
            }
        }

        private static bool IsVillageStage(int stage)
        {
            if (stage == 1)
            {
                return true;
            }

            var eventProfile = GameProgressManager.GetStageEventProfile(stage);
            return !string.IsNullOrEmpty(eventProfile)
                && eventProfile.IndexOf("Village", StringComparison.OrdinalIgnoreCase) >= 0;
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
                else if (!string.Equals(previousTheme, themeKey, StringComparison.Ordinal))
                {
                    chapterIndex += 1;
                    previousTheme = themeKey;
                }

                if (config.Stage >= targetStage)
                {
                    break;
                }
            }

            return Mathf.Clamp(chapterIndex, 1, 6);
        }

        private class MapMoveTweenState
        {
            public float Progress;
            public float GraphYOffset;
            public float MarkerX;
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
    }
}




