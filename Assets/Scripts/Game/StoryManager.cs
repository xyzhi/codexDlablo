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
        private static readonly Dictionary<string, Action<string>> CallbackHandlers = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase);
        private static StoryManager instance;

        private readonly Queue<PendingStoryRequest> pendingRequests = new Queue<PendingStoryRequest>();
        private readonly HashSet<string> consumedTriggerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
            PlayerPrefs.DeleteKey(TriggerStatePrefKey);
            PlayerPrefs.Save();
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
            currentNode = node;
            if (node == null)
            {
                CompleteSequence();
                return;
            }

            var popupId = IsDialogNode(node) ? "Dialog" : "Story";
            var popup = UIManager.Instance.ShowPopup<UIPopup>(popupId, node);
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
