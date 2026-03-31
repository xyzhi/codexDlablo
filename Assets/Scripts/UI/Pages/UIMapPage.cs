using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Wuxing.Battle;
using Wuxing.Game;
using Wuxing.Localization;

namespace Wuxing.UI
{
    public class UIMapPage : UIPage
    {
        public const float MapToastDuration = 1.6f;

        private const float MoveDurationPerStep = 0.5f;
        private const float NodeSpacing = 185f;
        private const float NodeWidth = 148f;
        private const float NodeHeight = 90f;

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
        [SerializeField] private Button resetButton;
        [SerializeField] private Button backButton;

        private readonly List<Button> nodeButtons = new List<Button>();
        private readonly List<Text> nodeButtonLabels = new List<Text>();
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
        }

        private void Awake()
        {
            if (previousButton != null) previousButton.onClick.AddListener(OnClickPrevious);
            if (enterButton != null) enterButton.onClick.AddListener(OnClickEnter);
            if (nextButton != null) nextButton.onClick.AddListener(OnClickNext);
            if (equipmentButton != null) equipmentButton.onClick.AddListener(OnClickEquipment);
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
            var currentStage = Mathf.Max(1, GameProgressManager.GetCurrentStage());
            if (selectedStage == currentStage)
            {
                TriggerStageEvent(currentStage);
                return;
            }

            if (!GameProgressManager.CanTravelToStage(selectedStage)) return;
            StartMoveToStage(selectedStage, true);
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
            UIManager.Instance.ShowPage("Battle", "equipment");
        }

        private void OnClickReset()
        {
            if (isMoving) return;
            GameProgressManager.ResetRun();
            UIManager.Instance.ShowPage("MainMenu");
        }

        private void OnClickBack()
        {
            if (isMoving) return;
            UIManager.Instance.ShowPage("MainMenu");
        }

        private void OnClickNodeStage(int stage)
        {
            selectedStage = stage;
            RefreshView();
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
                    UIManager.Instance.ShowToast(IsEnglish()
                        ? "Lifespan exhausted. The run has ended."
                        : "\u9633\u5bff\u5df2\u5c3d\uff0c\u672c\u8f6e\u7ed3\u675f\u3002", MapToastDuration);
                    UIManager.Instance.ShowPage("MainMenu");
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

            UIManager.Instance.ShowToast(BuildArrivalToast(Mathf.Max(1, GameProgressManager.GetCurrentStage())), MapToastDuration);

            if (triggerEventAfterMove)
            {
                TriggerStageEvent(Mathf.Max(1, GameProgressManager.GetCurrentStage()));
            }
        }

        private IEnumerator AnimateTravelStep(int fromStage, int toStage)
        {
            if (!stagePositions.TryGetValue(fromStage, out var fromPosition) || !stagePositions.TryGetValue(toStage, out var toPosition))
            {
                yield return null;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < MoveDurationPerStep)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / MoveDurationPerStep);
                UpdateTravelerMarkerPosition(Vector2.Lerp(fromPosition, toPosition, t));
                yield return null;
            }
        }

        private void TriggerStageEvent(int stage)
        {
            var nodeType = GameProgressManager.GetNodeType(stage);
            if (stage == 1)
            {
                GameProgressManager.MarkCurrentStageCleared();
                RefreshView();
                UIManager.Instance.ShowToast(IsEnglish()
                    ? "Novice Village is reserved for future features."
                    : "新手村当前作为占位区域，后续会加入更多功能。", MapToastDuration);
                return;
            }

            if (GameProgressManager.IsBattleNode(nodeType))
            {
                UIManager.Instance.ShowPage("Battle");
                return;
            }

            if (nodeType == MapNodeType.Rest)
            {
                GameProgressManager.MarkCurrentStageCleared();
                RefreshView();
                UIManager.Instance.ShowToast(IsEnglish()
                    ? "You stop to regulate your breath. One month passes quietly."
                    : "你在此调息静修，一月悄然而过。", MapToastDuration);
                return;
            }

            UIManager.Instance.ShowToast(IsEnglish()
                ? "The destination event has been triggered."
                : "目的地事件已触发。", MapToastDuration);
        }

        private void RefreshView()
        {
            var isEnglish = IsEnglish();
            var currentStage = Mathf.Max(1, GameProgressManager.GetCurrentStage());
            var maxReachableStage = GameProgressManager.GetMaxReachableStage();
            selectedStage = Mathf.Clamp(selectedStage <= 0 ? currentStage : selectedStage, 1, Mathf.Max(currentStage, maxReachableStage));

            if (titleText != null)
            {
                titleText.text = LocalizationManager.GetText("map.title");
            }

            if (statusText != null)
            {
                statusText.text = isEnglish
                    ? "Current Position: Stage " + currentStage + " 路 " + GameProgressManager.GetNodeTypeLabel(true, currentStage)
                    : "当前位置：第 " + currentStage + " 关 · " + GameProgressManager.GetNodeTypeLabel(false, currentStage);
            }

            if (regionText != null)
            {
                regionText.text = (isEnglish ? "Region: " : "区域：") + GameProgressManager.GetStageTheme(isEnglish, currentStage);
            }

            if (longevityText != null)
            {
                longevityText.text = GameProgressManager.BuildLongevitySummary(isEnglish);
            }

            if (nodeDetailText != null)
            {
                nodeDetailText.text = BuildSelectedNodeDetail(isEnglish, currentStage);
            }

            if (routeText != null)
            {
                routeText.text = BuildProfileText(isEnglish, currentStage, maxReachableStage);
            }

            RefreshButtons(isEnglish, currentStage);
            RefreshNodeGraph(currentStage);
        }

        private void RefreshButtons(bool isEnglish, int currentStage)
        {
            var canMovePrevious = !isMoving && GameProgressManager.CanTravelToStage(currentStage - 1);
            var canMoveNext = !isMoving && GameProgressManager.CanTravelToStage(currentStage + 1);
            var canEnterSelected = !isMoving && GameProgressManager.CanTravelToStage(selectedStage);

            SetButtonText(previousButton, isEnglish ? "Previous" : "后退");
            SetButtonText(nextButton, isEnglish ? "Next" : "前进");
            SetButtonText(enterButton, isEnglish ? "Enter Node" : "进入节点");
            SetButtonText(equipmentButton, isEnglish ? "Equipment" : "查看装备");
            SetButtonText(resetButton, isEnglish ? "Reset Run" : "重置本轮");
            SetButtonText(backButton, LocalizationManager.GetText("map.button_back_menu"));

            if (previousButton != null) previousButton.interactable = canMovePrevious;
            if (nextButton != null) nextButton.interactable = canMoveNext;
            if (enterButton != null) enterButton.interactable = canEnterSelected;
            if (equipmentButton != null) equipmentButton.interactable = !isMoving;
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

            var minStage = Mathf.Max(1, currentStage - 2);
            var maxStage = Mathf.Min(GameProgressManager.GetMaxStage(), currentStage + 2);
            for (var stage = minStage; stage <= maxStage; stage++)
            {
                visibleStages.Add(stage);
            }

            var count = visibleStages.Count;
            var startX = -((count - 1) * NodeSpacing) * 0.5f;

            for (var i = 0; i < nodeButtons.Count; i++)
            {
                var active = i < count;
                nodeButtons[i].gameObject.SetActive(active);
                if (i < nodeLines.Count) nodeLines[i].gameObject.SetActive(false);
                if (!active) continue;

                var stage = visibleStages[i];
                var position = new Vector2(startX + i * NodeSpacing, 0f);
                stagePositions[stage] = position;

                var buttonRect = nodeButtons[i].GetComponent<RectTransform>();
                buttonRect.anchoredPosition = position;
                buttonRect.sizeDelta = new Vector2(NodeWidth, NodeHeight);

                var buttonImage = nodeButtons[i].GetComponent<Image>();
                if (buttonImage != null) buttonImage.color = GetNodeColor(stage, currentStage);

                var label = nodeButtonLabels[i];
                if (label != null)
                {
                    label.text = BuildNodeLabel(stage);
                    label.alignment = TextAnchor.MiddleCenter;
                    label.fontSize = 22;
                }

                nodeButtons[i].onClick.RemoveAllListeners();
                var capturedStage = stage;
                nodeButtons[i].onClick.AddListener(delegate { OnClickNodeStage(capturedStage); });

                if (i > 0)
                {
                    var previousPosition = stagePositions[visibleStages[i - 1]];
                    ConfigureLine(nodeLines[i - 1].rectTransform, previousPosition, position);
                    nodeLines[i - 1].color = new Color(0.78f, 0.72f, 0.6f, 0.75f);
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
            while (nodeButtons.Count < 5)
            {
                var button = UIFactory.CreateButton(nodeGraphRoot, "NodeButton" + nodeButtons.Count, "Node", delegate { });
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                nodeButtons.Add(button);
                nodeButtonLabels.Add(button.GetComponentInChildren<Text>());
            }

            while (nodeLines.Count < 4)
            {
                var lineObject = new GameObject("NodeLine" + nodeLines.Count, typeof(RectTransform), typeof(Image));
                lineObject.transform.SetParent(nodeGraphRoot, false);
                var rect = lineObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                var image = lineObject.GetComponent<Image>();
                image.raycastTarget = false;
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
                travelerMarker.color = new Color(0.98f, 0.9f, 0.5f, 1f);
                travelerMarker.raycastTarget = false;
            }
        }

        private void ConfigureLine(RectTransform rect, Vector2 from, Vector2 to)
        {
            var direction = to - from;
            rect.anchoredPosition = from + direction * 0.5f;
            rect.sizeDelta = new Vector2(direction.magnitude, 5f);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        private void UpdateTravelerMarkerPosition(Vector2 graphPosition)
        {
            if (travelerMarker == null) return;
            travelerMarker.rectTransform.anchoredPosition = graphPosition + new Vector2(0f, 74f);
            travelerMarker.gameObject.SetActive(true);
            travelerMarker.transform.SetAsLastSibling();
        }

        private string BuildSelectedNodeDetail(bool isEnglish, int currentStage)
        {
            var selectedNodeType = GameProgressManager.GetNodeType(selectedStage);
            var canReach = GameProgressManager.CanTravelToStage(selectedStage);
            var monthCost = Mathf.Max(1, Mathf.Abs(selectedStage - currentStage));

            var builder = new StringBuilder();
            builder.Append(isEnglish ? "Selected Node" : "选中节点")
                .Append('\n')
                .Append(isEnglish ? "Stage " : "第 ")
                .Append(selectedStage)
                .Append(isEnglish ? " · " : " 关 · ")
                .Append(GameProgressManager.GetNodeTypeLabel(isEnglish, selectedStage))
                .Append('\n')
                .Append(isEnglish ? "Reachable: " : "是否可达：")
                .Append(canReach ? (isEnglish ? "Yes" : "可达") : (isEnglish ? "Locked" : "未解锁"))
                .Append('\n')
                .Append(isEnglish ? "Time Cost: " : "耗时：")
                .Append(monthCost)
                .Append(isEnglish ? " month(s)" : " 月")
                .Append('\n')
                .Append('\n')
                .Append(GameProgressManager.BuildNodeDetail(isEnglish, selectedStage));

            if (selectedStage == currentStage && GameProgressManager.IsBattleNode(selectedNodeType))
            {
                builder.Append('\n')
                    .Append('\n')
                    .Append(BattleManager.BuildBattlePreparationSummary());
            }

            builder.Append('\n')
                .Append('\n')
                .Append(isEnglish ? "Arrival Effect: " : "到达效果：")
                .Append(selectedNodeType == MapNodeType.Rest
                    ? (isEnglish ? "Trigger rest immediately and stay on the map." : "立刻触发休整事件，并停留在地图上。")
                    : (isEnglish ? "Start battle automatically after movement ends." : "移动结束后自动进入战斗。"));

            return builder.ToString();
        }

        private string BuildProfileText(bool isEnglish, int currentStage, int maxReachableStage)
        {
            var builder = new StringBuilder();
            builder.Append(isEnglish ? "Profile" : "个人信息")
                .Append('\n')
                .Append(isEnglish ? "Current Stage: " : "当前关卡：")
                .Append(currentStage)
                .Append('\n')
                .Append(isEnglish ? "Highest Cleared: " : "最高通关：")
                .Append(GameProgressManager.GetHighestClearedStage())
                .Append('\n')
                .Append(isEnglish ? "Reachable Frontier: " : "可达前沿：第 ")
                .Append(maxReachableStage)
                .Append(isEnglish ? string.Empty : " 关")
                .Append('\n')
                .Append(isEnglish ? "Cultivation Lv." : "修为等级 ")
                .Append(GameProgressManager.GetCultivationLevel())
                .Append('\n')
                .Append(isEnglish ? "Exp: " : "经验：")
                .Append(GameProgressManager.GetCultivationExp())
                .Append('/')
                .Append(GameProgressManager.GetRequiredExpForNextLevel())
                .Append('\n')
                .Append(GameProgressManager.BuildSpiritStoneSummary(isEnglish, true))
                .Append('\n')
                .Append(isEnglish ? "Owned Equipment: " : "已拥有装备：")
                .Append(GameProgressManager.GetOwnedEquipmentIds().Count)
                .Append('\n')
                .Append('\n')
                .Append(GameProgressManager.BuildLastBattleSummary(isEnglish));
            return builder.ToString();
        }

        private string BuildArrivalToast(int stage)
        {
            var isEnglish = IsEnglish();
            return isEnglish
                ? "Arrived at Stage " + stage + " 路 " + GameProgressManager.GetNodeTypeLabel(true, stage)
                : "已抵达第 " + stage + " 关 · " + GameProgressManager.GetNodeTypeLabel(false, stage);
        }

        private string BuildNodeLabel(int stage)
        {
            var isEnglish = IsEnglish();
            return (isEnglish ? "Stage " : "第 ") + stage + (isEnglish ? string.Empty : "关") + "\n"
                + GameProgressManager.GetNodeTypeLabel(isEnglish, stage);
        }

        private Color GetNodeColor(int stage, int currentStage)
        {
            if (stage == currentStage) return new Color(0.42f, 0.28f, 0.12f, 0.98f);
            if (stage == selectedStage) return new Color(0.18f, 0.28f, 0.4f, 0.98f);
            if (stage < currentStage) return new Color(0.18f, 0.18f, 0.2f, 0.95f);
            if (GameProgressManager.CanTravelToStage(stage)) return new Color(0.22f, 0.22f, 0.26f, 0.96f);
            return new Color(0.11f, 0.11f, 0.13f, 0.88f);
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
