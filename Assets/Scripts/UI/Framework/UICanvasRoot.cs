using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wuxing.Game;
using Wuxing.Localization;

namespace Wuxing.UI
{
    public class UICanvasRoot : MonoBehaviour
    {
        private struct TextStyleState
        {
            public Font Font;
            public FontStyle FontStyle;
        }

        [SerializeField] private Transform pageLayer;
        [SerializeField] private Transform popupLayer;
        [SerializeField] private Transform toastLayer;
        [SerializeField] private Font chineseFont;

        private readonly Dictionary<Text, TextStyleState> _textStates = new Dictionary<Text, TextStyleState>();
        private float _nextRefreshAt;

        public Transform PageLayer
        {
            get { return pageLayer; }
        }

        public Transform PopupLayer
        {
            get { return popupLayer; }
        }

        public Transform ToastLayer
        {
            get { return toastLayer; }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            LocalizationManager.LanguageChanged += RefreshFonts;
            RefreshFonts();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            LocalizationManager.LanguageChanged -= RefreshFonts;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || Time.unscaledTime < _nextRefreshAt)
            {
                return;
            }

            _nextRefreshAt = Time.unscaledTime + 0.5f;
            RefreshFonts();
        }

        public void Bind(Transform pageLayerTransform, Transform popupLayerTransform, Transform toastLayerTransform)
        {
            pageLayer = pageLayerTransform;
            popupLayer = popupLayerTransform;
            toastLayer = toastLayerTransform;
        }

        public void RefreshFonts()
        {
            RegisterTexts(transform);
            CleanupDeadTexts();

            foreach (var pair in _textStates)
            {
                ApplyFont(pair.Key, pair.Value);
            }
        }

        public void RefreshFonts(Transform scopeRoot)
        {
            RegisterTexts(scopeRoot);

            foreach (var pair in _textStates)
            {
                if (pair.Key != null && pair.Key.transform.IsChildOf(scopeRoot))
                {
                    ApplyFont(pair.Key, pair.Value);
                }
            }
        }

        private void RegisterTexts(Transform scopeRoot)
        {
            if (scopeRoot == null)
            {
                return;
            }

            var texts = scopeRoot.GetComponentsInChildren<Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                var text = texts[i];
                if (text == null || _textStates.ContainsKey(text))
                {
                    continue;
                }

                _textStates[text] = new TextStyleState
                {
                    Font = text.font,
                    FontStyle = text.fontStyle
                };
            }
        }

        private void CleanupDeadTexts()
        {
            var deadTexts = new List<Text>();
            foreach (var pair in _textStates)
            {
                if (pair.Key == null)
                {
                    deadTexts.Add(pair.Key);
                }
            }

            for (var i = 0; i < deadTexts.Count; i++)
            {
                _textStates.Remove(deadTexts[i]);
            }
        }

        private void ApplyFont(Text text, TextStyleState state)
        {
            if (text == null)
            {
                return;
            }

            var isEnglish = LocalizationManager.Instance != null
                && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;

            if (!isEnglish && chineseFont != null)
            {
                text.font = chineseFont;
                text.fontStyle = FontStyle.Bold;
                return;
            }

            if (state.Font != null)
            {
                text.font = state.Font;
            }

            text.fontStyle = state.FontStyle;
        }
    }
}
