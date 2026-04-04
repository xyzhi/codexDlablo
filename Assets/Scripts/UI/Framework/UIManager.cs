using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Wuxing.Localization;

namespace Wuxing.UI
{
    public class UIManager : MonoBehaviour
    {
        private readonly Dictionary<string, string> _pagePrefabPaths = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _popupPrefabPaths = new Dictionary<string, string>();
        private readonly Stack<UIPopup> _popupStack = new Stack<UIPopup>();

        private UICanvasRoot _canvasRoot;
        private UIPage _currentPage;

        public static UIManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateCanvasRoot();
            RegisterDefaults();
        }

        public void ShowPage(string pageId, object data = null)
        {
            if (!_pagePrefabPaths.TryGetValue(pageId, out var prefabPath))
            {
                Debug.LogError($"Page not registered: {pageId}");
                return;
            }

            if (_currentPage != null)
            {
                _currentPage.OnClose();
                Destroy(_currentPage.gameObject);
            }

            _currentPage = InstantiatePage(prefabPath, _canvasRoot.PageLayer);
            if (_currentPage == null)
            {
                return;
            }

            _currentPage.OnOpen(data);
        }

        public T ShowPopup<T>(string popupId, object data = null) where T : UIPopup
        {
            if (!_popupPrefabPaths.TryGetValue(popupId, out var prefabPath))
            {
                Debug.LogError($"Popup not registered: {popupId}");
                return null;
            }

            var popup = InstantiatePopup(prefabPath, _canvasRoot.PopupLayer);
            if (popup == null)
            {
                return null;
            }

            popup.OnOpen(data);
            _popupStack.Push(popup);
            return popup as T;
        }

        public void CloseTopPopup()
        {
            if (_popupStack.Count == 0)
            {
                return;
            }

            var popup = _popupStack.Pop();
            popup.OnClose();
            Destroy(popup.gameObject);
        }

        public void ShowToast(string message, float duration = 1.5f)
        {
            var toastPrefab = Resources.Load<GameObject>("Prefabs/UI/Popups/ToastPopup");
            if (toastPrefab == null)
            {
                Debug.LogError("Toast prefab not found at Resources/Prefabs/UI/Popups/ToastPopup");
                return;
            }

            var popupObject = Instantiate(toastPrefab, _canvasRoot.ToastLayer, false);
            var popup = popupObject.GetComponent<UIToastPopup>();
            if (popup == null)
            {
                Debug.LogError("ToastPopup prefab is missing UIToastPopup component.");
                return;
            }

            popup.Show(message, duration);
        }

        public void ShowToastByKey(string key, float duration = 1.5f)
        {
            ShowToast(LocalizationManager.GetText(key), duration);
        }

        private void CreateCanvasRoot()
        {
            var canvasPrefab = Resources.Load<GameObject>("Prefabs/UI/CanvasRoot");
            if (canvasPrefab == null)
            {
                Debug.LogError("CanvasRoot prefab not found at Resources/Prefabs/UI/CanvasRoot. Run 工具/重建UI预设.");
                return;
            }

            var canvasObject = Instantiate(canvasPrefab, transform, false);
            canvasObject.name = "CanvasRoot";
            _canvasRoot = canvasObject.GetComponent<UICanvasRoot>();
            if (_canvasRoot == null)
            {
                Debug.LogError("CanvasRoot prefab is missing UICanvasRoot component.");
                return;
            }

            if (FindObjectOfType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            var cheatOverlay = canvasObject.GetComponent<UICheatRuntimeOverlay>();
            if (cheatOverlay == null)
            {
                cheatOverlay = canvasObject.AddComponent<UICheatRuntimeOverlay>();
            }

            cheatOverlay.Initialize(_canvasRoot.PopupLayer);
        }

        private void RegisterDefaults()
        {
            _pagePrefabPaths["Start"] = "Prefabs/UI/Pages/StartPage";
            _pagePrefabPaths["MainMenu"] = "Prefabs/UI/Pages/MainMenuPage";
            _pagePrefabPaths["Map"] = "Prefabs/UI/Pages/MapPage";
            _pagePrefabPaths["Battle"] = "Prefabs/UI/Pages/BattlePage";
            _popupPrefabPaths["Confirm"] = "Prefabs/UI/Popups/ConfirmPopup";
            _popupPrefabPaths["CardBrowser"] = "Prefabs/UI/Popups/CardBrowserPopup";
            _popupPrefabPaths["Equipment"] = "Prefabs/UI/Popups/EquipmentPopup";
            _popupPrefabPaths["SpiritConvert"] = "Prefabs/UI/Popups/SpiritStoneConvertPopup";
        }

        private static UIPage InstantiatePage(string prefabPath, Transform parent)
        {
            var prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"UI page prefab not found: {prefabPath}");
                return null;
            }

            var instance = Instantiate(prefab, parent, false);
            var page = instance.GetComponent<UIPage>();
            if (page == null)
            {
                Debug.LogError($"UI page prefab is missing UIPage component: {prefabPath}");
            }

            return page;
        }

        private static UIPopup InstantiatePopup(string prefabPath, Transform parent)
        {
            var prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"UI popup prefab not found: {prefabPath}");
                return null;
            }

            var instance = Instantiate(prefab, parent, false);
            var popup = instance.GetComponent<UIPopup>();
            if (popup == null)
            {
                Debug.LogError($"UI popup prefab is missing UIPopup component: {prefabPath}");
            }

            return popup;
        }
    }
}






