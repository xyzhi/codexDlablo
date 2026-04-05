using UnityEngine;
using UnityEngine.UI;
using Wuxing.Game;
using Wuxing.Localization;

namespace Wuxing.UI
{
    public class UIStartPage : UIPage
    {
        [SerializeField] private Button enterButton;
        [SerializeField] private Button languageButton;
        [SerializeField] private Text languageStateText;
        [SerializeField] private RectTransform titleBlock;
        [SerializeField] private RectTransform actionPanel;
        [SerializeField] private RectTransform enterButtonRect;
        [SerializeField] private RectTransform orbitGlowRect;
        [SerializeField] private Graphic subtitleGraphic;
        [SerializeField] private Graphic topLineGraphic;
        [SerializeField] private CanvasGroup sigilCanvasGroup;

        private Vector2 _titleBasePosition;
        private Vector2 _actionBasePosition;
        private Vector2 _orbitGlowBasePosition;
        private Vector3 _buttonBaseScale = Vector3.one;
        private bool _layoutCached;
        private Text _languageButtonText;

        public override void OnOpen(object data)
        {
            CacheLayoutState();
            RefreshLanguageState();
            UpdateIdleAnimation(true);
        }

        private void Awake()
        {
            if (enterButton != null)
            {
                enterButton.onClick.AddListener(OnClickEnter);
            }

            if (languageButton != null)
            {
                languageButton.onClick.AddListener(OnClickLanguage);
                _languageButtonText = languageButton.GetComponentInChildren<Text>(true);
            }
        }

        private void OnEnable()
        {
            LocalizationManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
        }

        private void OnDestroy()
        {
            if (enterButton != null)
            {
                enterButton.onClick.RemoveListener(OnClickEnter);
            }

            if (languageButton != null)
            {
                languageButton.onClick.RemoveListener(OnClickLanguage);
            }
        }

        private void Update()
        {
            UpdateIdleAnimation(false);
        }

        private void OnClickEnter()
        {
            UIManager.Instance.ShowPage("Map");
        }

        private void OnClickLanguage()
        {
            LocalizationManager.ToggleLanguage();
            RefreshLanguageState();
            UIManager.Instance.ShowToastByKey("toast.language_changed");
        }

        private void OnLanguageChanged()
        {
            RefreshLanguageState();
        }

        private void CacheLayoutState()
        {
            if (_layoutCached)
            {
                return;
            }

            if (titleBlock != null)
            {
                _titleBasePosition = titleBlock.anchoredPosition;
            }

            if (actionPanel != null)
            {
                _actionBasePosition = actionPanel.anchoredPosition;
            }

            if (enterButtonRect != null)
            {
                _buttonBaseScale = enterButtonRect.localScale;
            }

            if (orbitGlowRect != null)
            {
                _orbitGlowBasePosition = orbitGlowRect.anchoredPosition;
            }

            _layoutCached = true;
        }

        private void RefreshLanguageState()
        {
            var isEnglish = LocalizationManager.Instance != null
                && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;

            if (languageStateText != null)
            {
                languageStateText.text = isEnglish ? "Current Language: English" : "\u5f53\u524d\u8bed\u8a00\uff1a\u7b80\u4f53\u4e2d\u6587";
            }

            if (_languageButtonText != null)
            {
                var targetKey = isEnglish ? "menu.button_language_to_zh" : "menu.button_language_to_en";
                _languageButtonText.text = LocalizationManager.GetText(targetKey);
            }
        }

        private void UpdateIdleAnimation(bool instant)
        {
            CacheLayoutState();
            var time = instant ? 0f : Time.unscaledTime;

            if (titleBlock != null)
            {
                titleBlock.anchoredPosition = _titleBasePosition + new Vector2(0f, Mathf.Sin(time * 0.7f) * 5f);
            }

            if (actionPanel != null)
            {
                actionPanel.anchoredPosition = _actionBasePosition + new Vector2(0f, Mathf.Sin(time * 0.9f + 0.6f) * 2f);
            }

            if (enterButtonRect != null)
            {
                var pulse = 1f + Mathf.Sin(time * 1.4f) * 0.014f;
                enterButtonRect.localScale = _buttonBaseScale * pulse;
            }

            if (orbitGlowRect != null)
            {
                var radiusX = 138f;
                var radiusY = 120f;
                var angle = time * 0.4f - Mathf.PI * 0.5f;
                orbitGlowRect.anchoredPosition = _orbitGlowBasePosition + new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
            }

            SetGraphicAlpha(subtitleGraphic, Mathf.Lerp(0.42f, 0.72f, Ping(time, 0.92f, -0.3f)));
            SetGraphicAlpha(topLineGraphic, Mathf.Lerp(0.14f, 0.34f, Ping(time, 0.58f, 0f)));

            if (sigilCanvasGroup != null)
            {
                sigilCanvasGroup.alpha = Mathf.Lerp(0.12f, 0.26f, Ping(time, 0.74f, 0.25f));
            }
        }

        private static float Ping(float time, float speed, float offset)
        {
            return (Mathf.Sin(time * speed + offset) + 1f) * 0.5f;
        }

        private static void SetGraphicAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null)
            {
                return;
            }

            var color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
