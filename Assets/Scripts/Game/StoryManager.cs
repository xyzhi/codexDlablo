using System;
using System.Collections.Generic;
using UnityEngine;
using Wuxing.Config;
using Wuxing.UI;

namespace Wuxing.Game
{
    public class StoryManager : MonoBehaviour
    {
        private const string TriggerStatePrefKey = "game.story.trigger_states";
        private const string StoryFlagPrefKey = "game.story.flags";
        private const string StoryValuePrefKey = "game.story.values";
        private static readonly Dictionary<string, Action<string>> CallbackHandlers = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase);
        private static StoryManager instance;

        private readonly Queue<PendingStoryRequest> pendingRequests = new Queue<PendingStoryRequest>();
        private readonly HashSet<string> consumedTriggerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> storyFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> storyValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private StoryNodeConfig currentNode;
        private Action currentCompleteCallback;
        private int pauseDepth;
        private float cachedTimeScale = 1f;

        public static event Action<string, string> CallbackInvoked;

        public static StoryManager Instance
        {
            get
            {
                EnsureInstance();
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadConsumedTriggerStates();
            LoadStoryStates();
        }

        public static bool TryTrigger(string triggerKey, int stage, Action onComplete = null)
        {
            return Instance.TryTriggerInternal(triggerKey, stage, onComplete);
        }

        public static void AdvanceCurrentNode()
        {
            if (Instance == null)
            {
                return;
            }

            Instance.AdvanceCurrentNodeInternal();
        }

        public static void RegisterCallback(string callbackKey, Action<string> handler)
        {
            if (string.IsNullOrEmpty(callbackKey) || handler == null)
            {
                return;
            }

            CallbackHandlers[callbackKey] = handler;
        }

        public static void UnregisterCallback(string callbackKey, Action<string> handler)
        {
            if (string.IsNullOrEmpty(callbackKey) || handler == null)
            {
                return;
            }

            Action<string> existing;
            if (!CallbackHandlers.TryGetValue(callbackKey, out existing))
            {
                return;
            }

            if (existing == handler)
            {
                CallbackHandlers.Remove(callbackKey);
            }
        }

        public static void ClearRunTriggerStates()
        {
            if (Instance == null)
            {
                return;
            }

            Instance.consumedTriggerIds.Clear();
            Instance.storyFlags.Clear();
            Instance.storyValues.Clear();
            PlayerPrefs.DeleteKey(TriggerStatePrefKey);
            PlayerPrefs.DeleteKey(StoryFlagPrefKey);
            PlayerPrefs.DeleteKey(StoryValuePrefKey);
            PlayerPrefs.Save();
        }

        public static IReadOnlyList<StoryChoiceConfig> GetCurrentChoices()
        {
            if (Instance == null || Instance.currentNode == null)
            {
                return Array.Empty<StoryChoiceConfig>();
            }

            return Instance.GetChoicesForNode(Instance.currentNode.Id);
        }

        public static bool SelectChoice(string choiceId)
        {
            return Instance != null && Instance.SelectChoiceInternal(choiceId);
        }

        private static void EnsureInstance()
        {
            if (instance != null)
            {
                return;
            }

            var existing = FindObjectOfType<StoryManager>();
            if (existing != null)
            {
                instance = existing;
                return;
            }

            var go = new GameObject("StoryManager");
            instance = go.AddComponent<StoryManager>();
        }

        private bool TryTriggerInternal(string triggerKey, int stage, Action onComplete)
        {
            if (string.IsNullOrEmpty(triggerKey))
            {
                return false;
            }

            var triggerDatabase = StoryTriggerDatabaseLoader.Load();
            var nodeDatabase = StoryNodeDatabaseLoader.Load();
            if (triggerDatabase == null || nodeDatabase == null)
            {
                return false;
            }

            var trigger = triggerDatabase.FindBestMatch(triggerKey, Mathf.Max(0, stage));
            if (trigger == null || string.IsNullOrEmpty(trigger.NodeId))
            {
                return false;
            }

            if (trigger.OncePerRun && consumedTriggerIds.Contains(trigger.Id))
            {
                return false;
            }

            var node = nodeDatabase.GetById(trigger.NodeId);
            if (node == null)
            {
                Debug.LogWarning("Story trigger references missing node: " + trigger.NodeId);
                return false;
            }

            if (currentNode != null)
            {
                pendingRequests.Enqueue(new PendingStoryRequest
                {
                    TriggerId = trigger.Id,
                    TriggerKey = triggerKey,
                    OncePerRun = trigger.OncePerRun,
                    FirstNodeId = trigger.NodeId,
                    OnComplete = onComplete
                });
                return true;
            }

            GameProgressManager.RegisterObjectiveEvent("TriggerStory", triggerKey, 1);
            GameProgressManager.RegisterObjectiveEvent("TriggerStory", string.Empty, 1);
            StartSequence(trigger.Id, trigger.OncePerRun, node, onComplete);
            return true;
        }

        private void StartSequence(string triggerId, bool oncePerRun, StoryNodeConfig firstNode, Action onComplete)
        {
            if (oncePerRun && !string.IsNullOrEmpty(triggerId))
            {
                consumedTriggerIds.Add(triggerId);
                SaveConsumedTriggerStates();
            }

            currentCompleteCallback = onComplete;
            PauseStoryFlow();
            OpenNode(firstNode);
        }

        private void OpenNode(StoryNodeConfig node)
        {
            var safety = 0;
            var activeNode = node;
            while (activeNode != null
                && string.Equals(activeNode.Type, "Condition", StringComparison.OrdinalIgnoreCase)
                && safety < 32)
            {
                activeNode = ResolveConditionalNextNode(activeNode);
                safety++;
            }

            currentNode = activeNode;
            if (activeNode == null)
            {
                CompleteSequence();
                return;
            }

            var popupId = IsDialogNode(activeNode) ? "Dialog" : "Story";
            var popup = UIManager.Instance.ShowPopup<UIPopup>(popupId, activeNode);
            if (popup == null)
            {
                CompleteSequence();
            }
        }

        private void AdvanceCurrentNodeInternal()
        {
            if (currentNode == null)
            {
                CompleteSequence();
                return;
            }

            InvokeNodeCallback(currentNode);
            UIManager.Instance.CloseTopPopup();

            var nextNodeId = currentNode.NextNodeId;
            currentNode = null;

            if (!string.IsNullOrEmpty(nextNodeId))
            {
                var nodeDatabase = StoryNodeDatabaseLoader.Load();
                var nextNode = nodeDatabase != null ? nodeDatabase.GetById(nextNodeId) : null;
                OpenNode(nextNode);
                return;
            }

            CompleteSequence();
        }

        private IReadOnlyList<StoryChoiceConfig> GetChoicesForNode(string nodeId)
        {
            var nodeDatabase = StoryNodeDatabaseLoader.Load();
            if (nodeDatabase == null || string.IsNullOrEmpty(nodeId))
            {
                return Array.Empty<StoryChoiceConfig>();
            }

            return nodeDatabase.GetChoicesByNodeId(nodeId);
        }

        private bool SelectChoiceInternal(string choiceId)
        {
            if (currentNode == null || string.IsNullOrEmpty(choiceId))
            {
                return false;
            }

            var choices = GetChoicesForNode(currentNode.Id);
            StoryChoiceConfig selectedChoice = null;
            for (var i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                if (choice != null && string.Equals(choice.Id, choiceId, StringComparison.OrdinalIgnoreCase))
                {
                    selectedChoice = choice;
                    break;
                }
            }

            if (selectedChoice == null)
            {
                return false;
            }

            InvokeNodeCallback(currentNode);
            ApplyChoiceEffects(selectedChoice);
            UIManager.Instance.CloseTopPopup();

            var nextNodeId = selectedChoice.NextNodeId;
            currentNode = null;
            if (!string.IsNullOrEmpty(nextNodeId))
            {
                var nodeDatabase = StoryNodeDatabaseLoader.Load();
                var nextNode = nodeDatabase != null ? nodeDatabase.GetById(nextNodeId) : null;
                OpenNode(nextNode);
                return true;
            }

            CompleteSequence();
            return true;
        }

        private void CompleteSequence()
        {
            currentNode = null;
            ResumeStoryFlow();

            var callback = currentCompleteCallback;
            currentCompleteCallback = null;
            callback?.Invoke();

            if (pendingRequests.Count <= 0)
            {
                return;
            }

            var pending = pendingRequests.Dequeue();
            var nodeDatabase = StoryNodeDatabaseLoader.Load();
            var nextNode = nodeDatabase != null ? nodeDatabase.GetById(pending.FirstNodeId) : null;
            GameProgressManager.RegisterObjectiveEvent("TriggerStory", pending.TriggerKey, 1);
            GameProgressManager.RegisterObjectiveEvent("TriggerStory", string.Empty, 1);
            StartSequence(pending.TriggerId, pending.OncePerRun, nextNode, pending.OnComplete);
        }

        private void ApplyChoiceEffects(StoryChoiceConfig choice)
        {
            if (choice == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(choice.SetFlag))
            {
                storyFlags.Add(choice.SetFlag.Trim());
            }

            ApplyValueDelta(choice.AddValue);
            SaveStoryStates();
        }

        private void ApplyValueDelta(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return;
            }

            var parts = rawValue.Split(':');
            int delta;
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || !int.TryParse(parts[1], out delta))
            {
                return;
            }

            var key = parts[0].Trim();
            int currentValue;
            storyValues.TryGetValue(key, out currentValue);
            storyValues[key] = currentValue + delta;
        }

        private StoryNodeConfig ResolveConditionalNextNode(StoryNodeConfig node)
        {
            var nodeDatabase = StoryNodeDatabaseLoader.Load();
            if (nodeDatabase == null || node == null)
            {
                return null;
            }

            var passed = EvaluateCondition(node);
            var nextNodeId = passed ? node.NextNodeId : node.FalseNextNodeId;
            return string.IsNullOrEmpty(nextNodeId) ? null : nodeDatabase.GetById(nextNodeId);
        }

        private bool EvaluateCondition(StoryNodeConfig node)
        {
            if (node == null)
            {
                return false;
            }

            switch ((node.ConditionType ?? string.Empty).Trim())
            {
                case "HasFlag":
                    return HasStoryFlag(node.ConditionParam);
                case "CompareValue":
                    return CompareStoryValue(node.ConditionParam, node.ConditionOperator, node.ConditionValue);
                default:
                    return !string.IsNullOrEmpty(node.NextNodeId);
            }
        }

        private bool HasStoryFlag(string flagKey)
        {
            return !string.IsNullOrWhiteSpace(flagKey) && storyFlags.Contains(flagKey.Trim());
        }

        private bool CompareStoryValue(string valueKey, string op, int targetValue)
        {
            if (string.IsNullOrWhiteSpace(valueKey))
            {
                return false;
            }

            var currentValue = GetStoryValue(valueKey);
            switch ((op ?? string.Empty).Trim())
            {
                case ">":
                    return currentValue > targetValue;
                case ">=":
                    return currentValue >= targetValue;
                case "<":
                    return currentValue < targetValue;
                case "<=":
                    return currentValue <= targetValue;
                case "!=":
                    return currentValue != targetValue;
                case "==":
                default:
                    return currentValue == targetValue;
            }
        }

        private int GetStoryValue(string valueKey)
        {
            if (string.IsNullOrWhiteSpace(valueKey))
            {
                return 0;
            }

            int value;
            return storyValues.TryGetValue(valueKey.Trim(), out value) ? value : 0;
        }

        private static bool IsDialogNode(StoryNodeConfig node)
        {
            return node != null
                && string.Equals(node.Type, "Dialog", StringComparison.OrdinalIgnoreCase);
        }

        private static void InvokeNodeCallback(StoryNodeConfig node)
        {
            if (node == null || string.IsNullOrEmpty(node.CallbackKey))
            {
                return;
            }

            CallbackInvoked?.Invoke(node.CallbackKey, node.CallbackParam);

            Action<string> handler;
            if (CallbackHandlers.TryGetValue(node.CallbackKey, out handler))
            {
                handler.Invoke(node.CallbackParam);
            }
        }

        private void PauseStoryFlow()
        {
            if (pauseDepth == 0)
            {
                cachedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            pauseDepth++;
        }

        private void ResumeStoryFlow()
        {
            pauseDepth = Mathf.Max(0, pauseDepth - 1);
            if (pauseDepth == 0)
            {
                Time.timeScale = cachedTimeScale <= 0f ? 1f : cachedTimeScale;
            }
        }

        private void LoadConsumedTriggerStates()
        {
            consumedTriggerIds.Clear();
            var raw = PlayerPrefs.GetString(TriggerStatePrefKey, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            var parts = raw.Split('|');
            for (var i = 0; i < parts.Length; i++)
            {
                var value = parts[i].Trim();
                if (!string.IsNullOrEmpty(value))
                {
                    consumedTriggerIds.Add(value);
                }
            }
        }

        private void SaveConsumedTriggerStates()
        {
            PlayerPrefs.SetString(TriggerStatePrefKey, string.Join("|", new List<string>(consumedTriggerIds).ToArray()));
            PlayerPrefs.Save();
        }

        private void LoadStoryStates()
        {
            storyFlags.Clear();
            storyValues.Clear();

            var rawFlags = PlayerPrefs.GetString(StoryFlagPrefKey, string.Empty);
            if (!string.IsNullOrEmpty(rawFlags))
            {
                var parts = rawFlags.Split('|');
                for (var i = 0; i < parts.Length; i++)
                {
                    var value = parts[i].Trim();
                    if (!string.IsNullOrEmpty(value))
                    {
                        storyFlags.Add(value);
                    }
                }
            }

            var rawValues = PlayerPrefs.GetString(StoryValuePrefKey, string.Empty);
            if (string.IsNullOrEmpty(rawValues))
            {
                return;
            }

            var entries = rawValues.Split('|');
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (string.IsNullOrWhiteSpace(entry))
                {
                    continue;
                }

                var parts = entry.Split(':');
                int parsedValue;
                if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || !int.TryParse(parts[1], out parsedValue))
                {
                    continue;
                }

                storyValues[parts[0].Trim()] = parsedValue;
            }
        }

        private void SaveStoryStates()
        {
            PlayerPrefs.SetString(StoryFlagPrefKey, string.Join("|", new List<string>(storyFlags).ToArray()));

            var valueEntries = new List<string>();
            foreach (var pair in storyValues)
            {
                valueEntries.Add(pair.Key + ":" + pair.Value);
            }

            PlayerPrefs.SetString(StoryValuePrefKey, string.Join("|", valueEntries.ToArray()));
            PlayerPrefs.Save();
        }

        private sealed class PendingStoryRequest
        {
            public string TriggerId;
            public string TriggerKey;
            public bool OncePerRun;
            public string FirstNodeId;
            public Action OnComplete;
        }
    }
}
