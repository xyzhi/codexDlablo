using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wuxing.Config;
using Wuxing.Game;
using Wuxing.Localization;

namespace Wuxing.UI
{
    public class UISpiritStoneConvertPopup : UIPopup
    {
        private sealed class StoneNodeRefs
        {
            public string Element;
            public Button Button;
            public RectTransform Rect;
            public Image Core;
            public Image Glow;
            public Image Ring;
            public Image NamePlate;
            public Image CountPlate;
            public Text NameText;
            public Text CountText;
        }

        private Text titleText;
        private Text introText;
        private Text summaryText;
        private Text quantityLabelText;
        private Text quantityValueText;
        private Text resultText;
        private Button closeButton;
        private Button convertButton;
        private Button decreaseButton;
        private Button increaseButton;
        private Button maxButton;
        private Text closeButtonText;
        private Text convertButtonText;
        private Text maxButtonText;
        private RectTransform sigilAreaRect;
        private Image centerGlow;
        private Image flowBeam;

        private readonly Dictionary<string, StoneNodeRefs> nodeRefs = new Dictionary<string, StoneNodeRefs>(StringComparer.OrdinalIgnoreCase);
        private SpiritStoneConversionDatabase conversionDatabase;
        private SpiritStoneConversionConfig selectedConversion;
        private int convertCount = 1;
        private bool isAnimating;

        private void Awake()
        {
            titleText = FindText("Panel/TitleText");
            introText = FindText("Panel/IntroText");
            summaryText = FindText("Panel/SummaryText");
            quantityLabelText = FindText("Panel/QuantityLabelText");
            quantityValueText = FindText("Panel/QuantityRow/QuantityValueText");
            resultText = FindText("Panel/ResultText");
            closeButton = FindButton("Panel/ActionRow/CloseButton");
            convertButton = FindButton("Panel/ActionRow/ConvertButton");
            decreaseButton = FindButton("Panel/QuantityRow/MinusButton");
            increaseButton = FindButton("Panel/QuantityRow/PlusButton");
            maxButton = FindButton("Panel/QuantityRow/MaxButton");
            closeButtonText = FindText("Panel/ActionRow/CloseButton/Label");
            convertButtonText = FindText("Panel/ActionRow/ConvertButton/Label");
            maxButtonText = FindText("Panel/QuantityRow/MaxButton/Label");
            sigilAreaRect = FindRect("Panel/SigilArea");
            centerGlow = FindImage("Panel/SigilArea/CenterGlow");
            flowBeam = FindImage("Panel/SigilArea/FlowBeam");

            RegisterNode("Metal");
            RegisterNode("Water");
            RegisterNode("Wood");
            RegisterNode("Fire");
            RegisterNode("Earth");

            if (closeButton != null) closeButton.onClick.AddListener(CloseSelf);
            if (convertButton != null) convertButton.onClick.AddListener(OnClickConvert);
            if (decreaseButton != null) decreaseButton.onClick.AddListener(OnClickDecrease);
            if (increaseButton != null) increaseButton.onClick.AddListener(OnClickIncrease);
            if (maxButton != null) maxButton.onClick.AddListener(OnClickMax);
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

        public override void OnOpen(object data)
        {
            conversionDatabase = SpiritStoneConversionDatabaseLoader.Load();
            if ((selectedConversion == null || !HasConversion(selectedConversion.SourceElement)) && conversionDatabase != null && conversionDatabase.Conversions.Count > 0)
            {
                selectedConversion = conversionDatabase.Conversions[0];
            }

            convertCount = Mathf.Max(1, convertCount);
            SetResultText(LocalizationManager.GetText("convert.status_select"));
            RefreshView();
        }

        private void OnProgressChanged()
        {
            if (gameObject.activeInHierarchy)
            {
                RefreshView();
            }
        }

        private void OnLanguageChanged()
        {
            if (gameObject.activeInHierarchy)
            {
                RefreshView();
            }
        }

        private void RegisterNode(string element)
        {
            var basePath = "Panel/SigilArea/Node" + element;
            var button = FindButton(basePath);
            if (button == null)
            {
                return;
            }

            var refs = new StoneNodeRefs
            {
                Element = element,
                Button = button,
                Rect = button.GetComponent<RectTransform>(),
                Core = FindImage(basePath + "/Core"),
                Glow = FindImage(basePath + "/Glow"),
                Ring = FindImage(basePath + "/Ring"),
                NamePlate = FindImage(basePath + "/NamePlate"),
                CountPlate = FindImage(basePath + "/CountPlate"),
                NameText = FindText(basePath + "/NameText"),
                CountText = FindText(basePath + "/CountText")
            };

            button.onClick.AddListener(delegate { SelectConversion(element); });
            nodeRefs[element] = refs;
        }

        private void SelectConversion(string sourceElement)
        {
            if (conversionDatabase == null)
            {
                return;
            }

            var next = conversionDatabase.GetBySourceElement(sourceElement);
            if (next == null)
            {
                return;
            }

            selectedConversion = next;
            convertCount = 1;
            SetResultText(LocalizationManager.GetText("convert.status_select"));
            RefreshView();
        }

        private void OnClickDecrease()
        {
            if (isAnimating)
            {
                return;
            }

            convertCount = Mathf.Max(1, convertCount - 1);
            RefreshView();
        }

        private void OnClickIncrease()
        {
            if (isAnimating)
            {
                return;
            }

            var maxCount = GetMaxConversionCount();
            if (maxCount <= 0)
            {
                return;
            }

            convertCount = Mathf.Clamp(convertCount + 1, 1, maxCount);
            RefreshView();
        }

        private void OnClickMax()
        {
            if (isAnimating)
            {
                return;
            }

            var maxCount = GetMaxConversionCount();
            if (maxCount > 0)
            {
                convertCount = maxCount;
                RefreshView();
            }
        }

        private void OnClickConvert()
        {
            if (isAnimating || selectedConversion == null)
            {
                return;
            }

            var maxCount = GetMaxConversionCount();
            if (maxCount <= 0)
            {
                SetResultText(LocalizationManager.GetText("convert.status_not_enough"));
                RefreshView();
                return;
            }

            var actualCount = Mathf.Clamp(convertCount, 1, maxCount);
            if (!GameProgressManager.TryConvertSpiritStones(
                    selectedConversion.SourceElement,
                    selectedConversion.TargetElement,
                    selectedConversion.CostAmount,
                    selectedConversion.GainAmount,
                    actualCount))
            {
                SetResultText(LocalizationManager.GetText("convert.status_not_enough"));
                RefreshView();
                return;
            }

            var totalCost = selectedConversion.CostAmount * actualCount;
            var totalGain = selectedConversion.GainAmount * actualCount;
            SetResultText(string.Format(
                LocalizationManager.GetText("convert.status_done"),
                totalCost,
                GameProgressManager.GetSpiritStoneName(selectedConversion.SourceElement, IsEnglish()),
                totalGain,
                GameProgressManager.GetSpiritStoneName(selectedConversion.TargetElement, IsEnglish())));

            RefreshView();
            StartCoroutine(PlayConvertEffect(selectedConversion.SourceElement, selectedConversion.TargetElement));
        }

        private IEnumerator PlayConvertEffect(string sourceElement, string targetElement)
        {
            StoneNodeRefs sourceRefs;
            StoneNodeRefs targetRefs;
            if (!nodeRefs.TryGetValue(sourceElement, out sourceRefs) || !nodeRefs.TryGetValue(targetElement, out targetRefs))
            {
                yield break;
            }

            isAnimating = true;
            RefreshView();

            var sourceScale = sourceRefs.Rect != null ? sourceRefs.Rect.localScale : Vector3.one;
            var targetScale = targetRefs.Rect != null ? targetRefs.Rect.localScale : Vector3.one;
            var sourceGlowColor = sourceRefs.Glow != null ? sourceRefs.Glow.color : Color.clear;
            var targetGlowColor = targetRefs.Glow != null ? targetRefs.Glow.color : Color.clear;
            var centerGlowColor = centerGlow != null ? centerGlow.color : Color.clear;
            const float duration = 0.52f;
            var elapsed = 0f;

            SetupFlowBeam(sourceRefs, targetRefs);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var easeOut = 1f - Mathf.Pow(1f - t, 2f);
                var pulse = Mathf.Sin(t * Mathf.PI);
                var sourcePulse = 1f - 0.22f * easeOut;
                var targetPulse = 1f + 0.2f * pulse;

                if (sourceRefs.Rect != null)
                {
                    sourceRefs.Rect.localScale = sourceScale * sourcePulse;
                }

                if (targetRefs.Rect != null)
                {
                    targetRefs.Rect.localScale = targetScale * targetPulse;
                }

                if (sourceRefs.Glow != null)
                {
                    var color = sourceGlowColor;
                    color.a = Mathf.Lerp(sourceGlowColor.a, 0.03f, easeOut);
                    sourceRefs.Glow.color = color;
                }

                if (targetRefs.Glow != null)
                {
                    var color = targetGlowColor;
                    color.a = Mathf.Lerp(targetGlowColor.a, 0.72f, pulse);
                    targetRefs.Glow.color = color;
                }

                if (centerGlow != null)
                {
                    var color = centerGlowColor;
                    color.a = Mathf.Lerp(centerGlowColor.a, 0.34f, pulse);
                    centerGlow.color = color;
                }

                UpdateFlowBeam(sourceRefs, targetRefs, t, pulse);
                yield return null;
            }

            if (sourceRefs.Rect != null) sourceRefs.Rect.localScale = sourceScale;
            if (targetRefs.Rect != null) targetRefs.Rect.localScale = targetScale;
            if (centerGlow != null) centerGlow.color = centerGlowColor;
            if (flowBeam != null) flowBeam.color = Color.clear;
            isAnimating = false;
            RefreshView();
        }

        private void RefreshView()
        {
            var isEnglish = IsEnglish();
            if (titleText != null) titleText.text = LocalizationManager.GetText("convert.title");
            if (introText != null) introText.text = LocalizationManager.GetText("convert.intro");
            if (quantityLabelText != null) quantityLabelText.text = LocalizationManager.GetText("convert.quantity");
            if (closeButtonText != null) closeButtonText.text = LocalizationManager.GetText("common.button_close");
            if (convertButtonText != null) convertButtonText.text = LocalizationManager.GetText("convert.button_convert");
            if (maxButtonText != null) maxButtonText.text = LocalizationManager.GetText("convert.button_max");

            foreach (var pair in nodeRefs)
            {
                RefreshNode(pair.Value, isEnglish);
            }

            if (selectedConversion == null && conversionDatabase != null && conversionDatabase.Conversions.Count > 0)
            {
                selectedConversion = conversionDatabase.Conversions[0];
            }

            var maxCount = GetMaxConversionCount();
            if (maxCount <= 0)
            {
                convertCount = 0;
            }
            else if (convertCount <= 0)
            {
                convertCount = 1;
            }
            else
            {
                convertCount = Mathf.Clamp(convertCount, 1, maxCount);
            }

            if (quantityValueText != null)
            {
                quantityValueText.text = maxCount <= 0 ? "0" : convertCount.ToString();
            }

            if (summaryText != null)
            {
                summaryText.text = BuildSummaryText(isEnglish, maxCount);
            }

            if (convertButton != null)
            {
                convertButton.interactable = !isAnimating && selectedConversion != null && maxCount > 0;
            }

            if (decreaseButton != null) decreaseButton.interactable = !isAnimating && convertCount > 1;
            if (increaseButton != null) increaseButton.interactable = !isAnimating && maxCount > 0 && convertCount < maxCount;
            if (maxButton != null) maxButton.interactable = !isAnimating && maxCount > 0 && convertCount < maxCount;
        }

        private void RefreshNode(StoneNodeRefs refs, bool isEnglish)
        {
            if (refs == null)
            {
                return;
            }

            var baseColor = ParseColor(GameProgressManager.GetSpiritStoneColorHex(refs.Element), new Color(0.8f, 0.8f, 0.8f, 1f));
            var isSource = selectedConversion != null && string.Equals(selectedConversion.SourceElement, refs.Element, StringComparison.OrdinalIgnoreCase);
            var isTarget = selectedConversion != null && string.Equals(selectedConversion.TargetElement, refs.Element, StringComparison.OrdinalIgnoreCase);

            if (refs.NameText != null)
            {
                refs.NameText.text = GameProgressManager.GetSpiritStoneName(refs.Element, isEnglish);
                refs.NameText.color = isSource || isTarget
                    ? new Color(1f, 0.98f, 0.9f, 1f)
                    : new Color(0.9f, 0.93f, 0.98f, 0.92f);
            }

            if (refs.CountText != null)
            {
                refs.CountText.text = "x" + GameProgressManager.GetSpiritStoneCount(refs.Element);
                refs.CountText.color = isSource || isTarget
                    ? new Color(1f, 0.96f, 0.84f, 1f)
                    : new Color(0.96f, 0.94f, 0.88f, 0.95f);
            }

            if (refs.Core != null)
            {
                var coreColor = baseColor;
                coreColor.a = isSource ? 1f : (isTarget ? 0.9f : 0.62f);
                refs.Core.color = coreColor;
            }

            if (refs.Glow != null)
            {
                var glowColor = baseColor;
                glowColor.a = isSource ? 0.52f : (isTarget ? 0.38f : 0.1f);
                refs.Glow.color = glowColor;
            }

            if (refs.Ring != null)
            {
                refs.Ring.color = isSource
                    ? new Color(1f, 0.94f, 0.72f, 0.92f)
                    : (isTarget ? new Color(0.82f, 0.94f, 1f, 0.74f) : new Color(1f, 1f, 1f, 0.08f));
            }

            if (refs.NamePlate != null)
            {
                refs.NamePlate.color = isSource || isTarget
                    ? new Color(0.09f, 0.1f, 0.14f, 0.72f)
                    : new Color(0.09f, 0.1f, 0.14f, 0.4f);
            }

            if (refs.CountPlate != null)
            {
                refs.CountPlate.color = isSource || isTarget
                    ? new Color(0.12f, 0.09f, 0.08f, 0.72f)
                    : new Color(0.12f, 0.09f, 0.08f, 0.38f);
            }

            if (refs.Button != null)
            {
                refs.Button.interactable = !isAnimating && HasConversion(refs.Element);
            }

            if (refs.Rect != null && !isAnimating)
            {
                refs.Rect.localScale = isSource ? Vector3.one * 1.08f : (isTarget ? Vector3.one * 1.04f : Vector3.one);
            }
        }

        private string BuildSummaryText(bool isEnglish, int maxCount)
        {
            if (selectedConversion == null)
            {
                return LocalizationManager.GetText("convert.status_select");
            }

            var sourceName = GameProgressManager.GetSpiritStoneName(selectedConversion.SourceElement, isEnglish);
            var targetName = GameProgressManager.GetSpiritStoneName(selectedConversion.TargetElement, isEnglish);
            var ratio = string.Format(
                LocalizationManager.GetText("convert.summary_ratio"),
                selectedConversion.CostAmount,
                sourceName,
                selectedConversion.GainAmount,
                targetName);

            if (maxCount <= 0 || convertCount <= 0)
            {
                return ratio + "\n" + LocalizationManager.GetText("convert.status_not_enough");
            }

            return ratio + "\n" + string.Format(
                LocalizationManager.GetText("convert.summary_preview"),
                selectedConversion.CostAmount * convertCount,
                sourceName,
                selectedConversion.GainAmount * convertCount,
                targetName);
        }

        private int GetMaxConversionCount()
        {
            if (selectedConversion == null)
            {
                return 0;
            }

            var cost = Mathf.Max(1, selectedConversion.CostAmount);
            var owned = GameProgressManager.GetSpiritStoneCount(selectedConversion.SourceElement);
            return Mathf.Max(0, owned / cost);
        }

        private bool HasConversion(string sourceElement)
        {
            return conversionDatabase != null && conversionDatabase.GetBySourceElement(sourceElement) != null;
        }

        private void SetResultText(string value)
        {
            if (resultText != null)
            {
                resultText.text = value;
            }
        }

        private void CloseSelf()
        {
            UIManager.Instance.CloseTopPopup();
        }

        private bool IsEnglish()
        {
            return LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
        }

        private Text FindText(string path)
        {
            var target = transform.Find(path);
            return target != null ? target.GetComponent<Text>() : null;
        }

        private Button FindButton(string path)
        {
            var target = transform.Find(path);
            return target != null ? target.GetComponent<Button>() : null;
        }

        private RectTransform FindRect(string path)
        {
            var target = transform.Find(path);
            return target != null ? target.GetComponent<RectTransform>() : null;
        }

        private Image FindImage(string path)
        {
            var target = transform.Find(path);
            return target != null ? target.GetComponent<Image>() : null;
        }

        private void SetupFlowBeam(StoneNodeRefs sourceRefs, StoneNodeRefs targetRefs)
        {
            if (flowBeam == null || sourceRefs == null || targetRefs == null || sourceRefs.Rect == null || targetRefs.Rect == null)
            {
                return;
            }

            var from = sourceRefs.Rect.anchoredPosition;
            var to = targetRefs.Rect.anchoredPosition;
            var delta = to - from;
            var rect = flowBeam.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(delta.magnitude * 0.12f, 14f);
            rect.anchoredPosition = from;
            rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            flowBeam.color = new Color(1f, 0.92f, 0.76f, 0f);
        }

        private void UpdateFlowBeam(StoneNodeRefs sourceRefs, StoneNodeRefs targetRefs, float normalized, float pulse)
        {
            if (flowBeam == null || sourceRefs == null || targetRefs == null || sourceRefs.Rect == null || targetRefs.Rect == null)
            {
                return;
            }

            var from = sourceRefs.Rect.anchoredPosition;
            var to = targetRefs.Rect.anchoredPosition;
            var delta = to - from;
            var beamRect = flowBeam.rectTransform;
            beamRect.sizeDelta = new Vector2(delta.magnitude * Mathf.Lerp(0.12f, 1f, normalized), Mathf.Lerp(10f, 18f, pulse));
            beamRect.anchoredPosition = Vector2.Lerp(from, to, normalized * 0.18f);

            var color = flowBeam.color;
            color.a = Mathf.Lerp(0f, 0.84f, pulse);
            flowBeam.color = color;
        }

        private static Color ParseColor(string hex, Color fallback)
        {
            Color color;
            return ColorUtility.TryParseHtmlString(hex, out color) ? color : fallback;
        }
    }
}