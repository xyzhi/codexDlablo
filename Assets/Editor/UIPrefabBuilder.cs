using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Wuxing.Localization;
using Wuxing.UI;

public static class UIPrefabBuilder
{
    private const string RootFolder = "Assets/Resources/Prefabs/UI";
    private const string PagesFolder = RootFolder + "/Pages";
    private const string PopupsFolder = RootFolder + "/Popups";
    private const string BuildStateFolder = "Library/Codex";
    private const string BuildStatePath = BuildStateFolder + "/ui_prefab_build_hash.txt";
    private const string TempPrefabFolder = "Assets/__UIPrefabBuilderTemp";

    public static void BuildPrefabs(bool forceRebuild = false)
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Prefabs");
        EnsureFolder(RootFolder);
        EnsureFolder(PagesFolder);
        EnsureFolder(PopupsFolder);

        var inputHash = ComputeBuildInputHash();
        if (!forceRebuild && AreAllGeneratedPrefabsPresent() && string.Equals(ReadLastBuildHash(), inputHash, System.StringComparison.Ordinal))
        {
            Debug.Log("UI prefabs are up to date. Skip rebuild.");
            return;
        }

        BuildCanvasRootPrefab();
        BuildStartPagePrefab();
        BuildMainMenuPrefab();
        BuildMapPagePrefab();
        BuildConfirmPopupPrefab();
        BuildCardBrowserPopupPrefab();
        BuildEquipmentPopupPrefab();
        BuildSkillPopupPrefab();
        BuildSpiritStoneConvertPopupPrefab();
        BuildToastPopupPrefab();

        WriteLastBuildHash(inputHash);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("UI prefabs rebuilt at Assets/Resources/Prefabs/UI");
    }

    private static void BuildCanvasRootPrefab()
    {
        var root = new GameObject("CanvasRoot", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(UICanvasRoot));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0f;

        var pageLayer = UIFactory.CreateContainer(root.transform, "PageLayer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var popupLayer = UIFactory.CreateContainer(root.transform, "PopupLayer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var toastLayer = UIFactory.CreateContainer(root.transform, "ToastLayer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        root.GetComponent<UICanvasRoot>().Bind(pageLayer, popupLayer, toastLayer);
        SavePrefab(root, RootFolder + "/CanvasRoot.prefab");
    }

    private static void BuildStartPagePrefab()
    {
        var root = UIFactory.CreatePanel(null, "StartPage", new Color(0.05f, 0.07f, 0.11f, 1f));
        UIFactory.AddOutlineBox(root.transform, "StartFrame", new Color(0.92f, 0.84f, 0.62f, 0.34f), 2f);

        var backdropTop = UIFactory.CreatePanel(root.transform, "BackdropTop", new Color(0.09f, 0.15f, 0.24f, 0.48f));
        var backdropTopRect = backdropTop.GetComponent<RectTransform>();
        backdropTopRect.anchorMin = new Vector2(0f, 0.5f);
        backdropTopRect.anchorMax = new Vector2(1f, 1f);
        backdropTopRect.offsetMin = Vector2.zero;
        backdropTopRect.offsetMax = Vector2.zero;

        var backdropBottom = UIFactory.CreatePanel(root.transform, "BackdropBottom", new Color(0.14f, 0.08f, 0.08f, 0.22f));
        var backdropBottomRect = backdropBottom.GetComponent<RectTransform>();
        backdropBottomRect.anchorMin = new Vector2(0f, 0f);
        backdropBottomRect.anchorMax = new Vector2(1f, 0.2f);
        backdropBottomRect.offsetMin = Vector2.zero;
        backdropBottomRect.offsetMax = Vector2.zero;

        var topLine = UIFactory.CreateLine(root.transform, "TopLine", new Color(1f, 0.86f, 0.58f, 0.24f), new Vector2(0.2f, 0.89f), new Vector2(0.8f, 0.89f), 3f);
        UIFactory.CreateLine(root.transform, "BottomLine", new Color(0.78f, 0.84f, 0.92f, 0.1f), new Vector2(0.24f, 0.11f), new Vector2(0.76f, 0.11f), 2f);

        var sigilRoot = new GameObject("SigilRoot", typeof(RectTransform), typeof(CanvasGroup));
        sigilRoot.transform.SetParent(root.transform, false);
        var sigilRect = sigilRoot.GetComponent<RectTransform>();
        sigilRect.anchorMin = new Vector2(0.5f, 0.6f);
        sigilRect.anchorMax = new Vector2(0.5f, 0.6f);
        sigilRect.pivot = new Vector2(0.5f, 0.5f);
        sigilRect.anchoredPosition = Vector2.zero;
        sigilRect.sizeDelta = new Vector2(500f, 500f);
        var sigilCanvasGroup = sigilRoot.GetComponent<CanvasGroup>();
        sigilCanvasGroup.alpha = 0.18f;

        var pentagonColor = new Color(0.95f, 0.86f, 0.68f, 0.26f);
        var starColor = new Color(0.76f, 0.85f, 0.94f, 0.22f);

        var p1 = new Vector2(0f, 182f);
        var p2 = new Vector2(172f, 56f);
        var p3 = new Vector2(106f, -152f);
        var p4 = new Vector2(-106f, -152f);
        var p5 = new Vector2(-172f, 56f);

        CreateSegment(sigilRoot.transform, "PentagonA", p1, p2, pentagonColor, 2f);
        CreateSegment(sigilRoot.transform, "PentagonB", p2, p3, pentagonColor, 2f);
        CreateSegment(sigilRoot.transform, "PentagonC", p3, p4, pentagonColor, 2f);
        CreateSegment(sigilRoot.transform, "PentagonD", p4, p5, pentagonColor, 2f);
        CreateSegment(sigilRoot.transform, "PentagonE", p5, p1, pentagonColor, 2f);

        CreateSegment(sigilRoot.transform, "StarA", p1, p3, starColor, 2f);
        CreateSegment(sigilRoot.transform, "StarB", p3, p5, starColor, 2f);
        CreateSegment(sigilRoot.transform, "StarC", p5, p2, starColor, 2f);
        CreateSegment(sigilRoot.transform, "StarD", p2, p4, starColor, 2f);
        CreateSegment(sigilRoot.transform, "StarE", p4, p1, starColor, 2f);

        var orbitGlow = UIFactory.CreatePanel(sigilRoot.transform, "OrbitGlow", new Color(0.92f, 0.86f, 0.68f, 0.22f));
        var orbitGlowRect = orbitGlow.GetComponent<RectTransform>();
        orbitGlowRect.anchorMin = new Vector2(0.5f, 0.5f);
        orbitGlowRect.anchorMax = new Vector2(0.5f, 0.5f);
        orbitGlowRect.pivot = new Vector2(0.5f, 0.5f);
        orbitGlowRect.anchoredPosition = new Vector2(0f, -120f);
        orbitGlowRect.sizeDelta = new Vector2(20f, 20f);

        var titleBlock = UIFactory.CreateContainer(root.transform, "TitleBlock", new Vector2(0.14f, 0.5f), new Vector2(0.86f, 0.71f), Vector2.zero, Vector2.zero);
        var titleText = UIFactory.CreateText(titleBlock, "Title", "\u4e94\u884c\u884c\u8005", 90, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.84f, 1f));
        titleText.rectTransform.anchorMin = new Vector2(0f, 0.34f);
        titleText.rectTransform.anchorMax = new Vector2(1f, 0.8f);
        titleText.rectTransform.offsetMin = Vector2.zero;
        titleText.rectTransform.offsetMax = Vector2.zero;
        AddLocalizedText(titleText, "landing.title");

        var subtitleText = UIFactory.CreateText(titleBlock, "Subtitle", "\u5355\u673a\u8089\u9e3d\u4fee\u4ed9\u4e4b\u65c5", 20, TextAnchor.LowerCenter, new Color(0.92f, 0.93f, 0.95f, 0.58f));
        subtitleText.rectTransform.anchorMin = new Vector2(0.22f, 0.04f);
        subtitleText.rectTransform.anchorMax = new Vector2(0.78f, 0.2f);
        subtitleText.rectTransform.offsetMin = Vector2.zero;
        subtitleText.rectTransform.offsetMax = Vector2.zero;
        AddLocalizedText(subtitleText, "landing.subtitle");

        var actionPanel = UIFactory.CreatePanel(root.transform, "ActionPanel", new Color(0.08f, 0.09f, 0.12f, 0.54f));
        var actionPanelRect = actionPanel.GetComponent<RectTransform>();
        actionPanelRect.anchorMin = new Vector2(0.25f, 0.19f);
        actionPanelRect.anchorMax = new Vector2(0.75f, 0.32f);
        actionPanelRect.offsetMin = Vector2.zero;
        actionPanelRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(actionPanel.transform, "ActionOutline", new Color(1f, 0.86f, 0.62f, 0.08f), 1f);

        var enterButton = UIFactory.CreateButton(actionPanel.transform, "EnterButton", "\u8fdb\u5165\u6e38\u620f", delegate { });
        var enterButtonRect = enterButton.GetComponent<RectTransform>();
        enterButtonRect.anchorMin = new Vector2(0.08f, 0.42f);
        enterButtonRect.anchorMax = new Vector2(0.92f, 0.8f);
        enterButtonRect.offsetMin = Vector2.zero;
        enterButtonRect.offsetMax = Vector2.zero;
        enterButton.GetComponent<Image>().color = new Color(0.46f, 0.2f, 0.12f, 0.9f);
        AddLocalizedText(enterButton.GetComponentInChildren<Text>(), "landing.button_enter");

        var languageButton = UIFactory.CreateButton(actionPanel.transform, "LanguageButton", "\u8bed\u8a00\u5207\u6362", delegate { });
        var languageButtonRect = languageButton.GetComponent<RectTransform>();
        languageButtonRect.anchorMin = new Vector2(0.08f, 0.1f);
        languageButtonRect.anchorMax = new Vector2(0.34f, 0.24f);
        languageButtonRect.offsetMin = Vector2.zero;
        languageButtonRect.offsetMax = Vector2.zero;
        languageButton.GetComponent<Image>().color = new Color(0.14f, 0.2f, 0.26f, 0.72f);
        AddLocalizedText(languageButton.GetComponentInChildren<Text>(), "menu.button_language");

        var languageStateText = UIFactory.CreateText(actionPanel.transform, "LanguageStateText", "\u5f53\u524d\u8bed\u8a00", 17, TextAnchor.MiddleRight, new Color(0.82f, 0.89f, 0.95f, 0.68f));
        languageStateText.rectTransform.anchorMin = new Vector2(0.42f, 0.08f);
        languageStateText.rectTransform.anchorMax = new Vector2(0.92f, 0.26f);
        languageStateText.rectTransform.offsetMin = Vector2.zero;
        languageStateText.rectTransform.offsetMax = Vector2.zero;

        var page = root.AddComponent<UIStartPage>();
        BindSerializedProperty(page, "enterButton", enterButton);
        BindSerializedProperty(page, "languageButton", languageButton);
        BindSerializedProperty(page, "languageStateText", languageStateText);
        BindSerializedProperty(page, "titleBlock", titleBlock);
        BindSerializedProperty(page, "actionPanel", actionPanelRect);
        BindSerializedProperty(page, "enterButtonRect", enterButtonRect);
        BindSerializedProperty(page, "orbitGlowRect", orbitGlowRect);
        BindSerializedProperty(page, "subtitleGraphic", subtitleText);
        BindSerializedProperty(page, "topLineGraphic", topLine);
        BindSerializedProperty(page, "sigilCanvasGroup", sigilCanvasGroup);
        SavePrefab(root, PagesFolder + "/StartPage.prefab");
    }

    private static void BuildMainMenuPrefab()
    {
        var root = UIFactory.CreatePanel(null, "MainMenuPage", new Color(0.09f, 0.1f, 0.14f, 1f));
        UIFactory.AddOutlineBox(root.transform, "ScreenFrame", new Color(0.8f, 0.82f, 0.86f, 0.85f), 2f);
        UIFactory.CreateLine(root.transform, "TopLine", new Color(0.7f, 0.74f, 0.8f, 1f), new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.88f), 2f);
        UIFactory.CreateLine(root.transform, "BottomLine", new Color(0.7f, 0.74f, 0.8f, 1f), new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.1f), 2f);

        var header = UIFactory.CreateContainer(root.transform, "Header", new Vector2(0.18f, 0.72f), new Vector2(0.82f, 0.84f), Vector2.zero, Vector2.zero);
        var title = UIFactory.CreateText(header, "Title", "Wuxing Prototype", 54, TextAnchor.MiddleCenter, Color.white);
        AddLocalizedText(title, "menu.title");

        var subtitle = UIFactory.CreateContainer(root.transform, "SubTitle", new Vector2(0.22f, 0.64f), new Vector2(0.78f, 0.68f), Vector2.zero, Vector2.zero);
        var subtitleText = UIFactory.CreateText(subtitle, "SubTitleText", "Phase 1 / Step 1 / Text and line UGUI", 24, TextAnchor.MiddleCenter, new Color(0.82f, 0.84f, 0.9f, 1f));
        AddLocalizedText(subtitleText, "menu.subtitle");

        var progressPanel = UIFactory.CreatePanel(root.transform, "ProgressPanel", new Color(0.13f, 0.14f, 0.18f, 0.92f));
        var progressPanelRect = progressPanel.GetComponent<RectTransform>();
        progressPanelRect.anchorMin = new Vector2(0.16f, 0.42f);
        progressPanelRect.anchorMax = new Vector2(0.84f, 0.58f);
        progressPanelRect.offsetMin = Vector2.zero;
        progressPanelRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(progressPanel.transform, "ProgressOutline", new Color(0.82f, 0.84f, 0.9f, 0.55f), 1f);
        var progressText = UIFactory.CreateText(progressPanel.transform, "ProgressText", "Current Stage 1 / Highest Cleared 0", 22, TextAnchor.MiddleCenter, new Color(0.95f, 0.86f, 0.72f, 1f));
        progressText.rectTransform.offsetMin = new Vector2(20f, 14f);
        progressText.rectTransform.offsetMax = new Vector2(-20f, -14f);
        progressText.lineSpacing = 1.18f;

        var menuPanel = UIFactory.CreatePanel(root.transform, "MenuPanel", new Color(0.13f, 0.14f, 0.18f, 0.92f));
        var menuRect = menuPanel.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.16f, 0.19f);
        menuRect.anchorMax = new Vector2(0.84f, 0.40f);
        menuRect.offsetMin = Vector2.zero;
        menuRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(menuPanel.transform, "MenuOutline", new Color(0.82f, 0.84f, 0.9f, 0.75f), 1f);

        var menu = UIFactory.CreateContainer(menuPanel.transform, "Menu", new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);
        var layout = UIFactory.AddVerticalLayout(menu.gameObject, 16, TextAnchor.UpperCenter);
        layout.padding = new RectOffset(0, 0, 6, 6);

        var startButton = UIFactory.CreateButton(menu, "StartButton", "Open Battle", delegate { });
        AddLocalizedText(startButton.GetComponentInChildren<Text>(), "menu.button_battle");
        UIFactory.AddLayoutElement(startButton.gameObject, 62f);

        var popupButton = UIFactory.CreateButton(menu, "PopupButton", "Open Popup", delegate { });
        AddLocalizedText(popupButton.GetComponentInChildren<Text>(), "menu.button_popup");
        UIFactory.AddLayoutElement(popupButton.gameObject, 62f);

        var toastButton = UIFactory.CreateButton(menu, "ToastButton", "Show Toast", delegate { });
        AddLocalizedText(toastButton.GetComponentInChildren<Text>(), "menu.button_toast");
        UIFactory.AddLayoutElement(toastButton.gameObject, 62f);

        var footer = UIFactory.CreateContainer(root.transform, "Footer", new Vector2(0.16f, 0.11f), new Vector2(0.84f, 0.16f), Vector2.zero, Vector2.zero);
        var footerText = UIFactory.CreateText(footer, "FooterText", "This version uses text, lines, and rectangles first so art can be replaced later.", 22, TextAnchor.MiddleCenter, new Color(0.72f, 0.76f, 0.82f, 1f));
        AddLocalizedText(footerText, "menu.footer");

        var page = root.AddComponent<UIMainMenuPage>();
        BindSerializedProperty(page, "startButton", startButton);
        BindSerializedProperty(page, "popupButton", popupButton);
        BindSerializedProperty(page, "toastButton", toastButton);
        BindSerializedProperty(page, "progressText", progressText);
        SavePrefab(root, PagesFolder + "/MainMenuPage.prefab");
    }

    private static void BuildMapPagePrefab()
    {
        var root = UIFactory.CreatePanel(null, "MapPage", new Color(0.08f, 0.09f, 0.11f, 1f));
        UIFactory.AddOutlineBox(root.transform, "MapFrame", new Color(0.84f, 0.8f, 0.68f, 0.8f), 2f);

        var title = UIFactory.CreateContainer(root.transform, "Title", new Vector2(0.08f, 0.91f), new Vector2(0.92f, 0.97f), Vector2.zero, Vector2.zero);
        var titleText = UIFactory.CreateText(title, "TitleText", "Map Route", 42, TextAnchor.MiddleCenter, Color.white);

        var status = UIFactory.CreateContainer(root.transform, "Status", new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.91f), Vector2.zero, Vector2.zero);
        var statusText = UIFactory.CreateText(status, "StatusText", "Current Node", 24, TextAnchor.MiddleCenter, new Color(0.95f, 0.88f, 0.74f, 1f));

        var region = UIFactory.CreateContainer(root.transform, "Region", new Vector2(0.08f, 0.81f), new Vector2(0.92f, 0.85f), Vector2.zero, Vector2.zero);
        var regionText = UIFactory.CreateText(region, "RegionText", "Region", 24, TextAnchor.MiddleCenter, new Color(0.8f, 0.9f, 0.95f, 1f));

        var longevityPanel = UIFactory.CreatePanel(root.transform, "LongevityPanel", new Color(0.14f, 0.11f, 0.11f, 0.92f));
        var longevityRect = longevityPanel.GetComponent<RectTransform>();
        longevityRect.anchorMin = new Vector2(0.08f, 0.72f);
        longevityRect.anchorMax = new Vector2(0.92f, 0.79f);
        longevityRect.offsetMin = Vector2.zero;
        longevityRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(longevityPanel.transform, "LongevityOutline", new Color(0.88f, 0.8f, 0.72f, 0.55f), 1f);
        var longevityText = UIFactory.CreateText(longevityPanel.transform, "LongevityText", "Lifespan", 28, TextAnchor.MiddleCenter, new Color(0.95f, 0.92f, 0.8f, 1f));
        longevityText.rectTransform.offsetMin = new Vector2(18f, 6f);
        longevityText.rectTransform.offsetMax = new Vector2(-18f, -6f);

        var graphPanel = UIFactory.CreatePanel(root.transform, "GraphPanel", new Color(0.12f, 0.1f, 0.1f, 0.95f));
        var graphRect = graphPanel.GetComponent<RectTransform>();
        graphRect.anchorMin = new Vector2(0.08f, 0.53f);
        graphRect.anchorMax = new Vector2(0.92f, 0.71f);
        graphRect.offsetMin = Vector2.zero;
        graphRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(graphPanel.transform, "GraphOutline", new Color(0.88f, 0.8f, 0.72f, 0.55f), 1f);

        var graphRoot = UIFactory.CreateContainer(graphPanel.transform, "NodeGraphRoot", new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero);

        var detailPanel = UIFactory.CreatePanel(root.transform, "DetailPanel", new Color(0.12f, 0.1f, 0.1f, 0.95f));
        var detailRect = detailPanel.GetComponent<RectTransform>();
        detailRect.anchorMin = new Vector2(0.08f, 0.145f);
        detailRect.anchorMax = new Vector2(0.48f, 0.49f);
        detailRect.offsetMin = Vector2.zero;
        detailRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(detailPanel.transform, "DetailOutline", new Color(0.88f, 0.8f, 0.72f, 0.55f), 1f);
        var detailText = UIFactory.CreateText(detailPanel.transform, "DetailText", "Node Detail", 20, TextAnchor.UpperLeft, new Color(0.94f, 0.92f, 0.88f, 1f));
        detailText.rectTransform.offsetMin = new Vector2(18f, 14f);
        detailText.rectTransform.offsetMax = new Vector2(-18f, -14f);
        detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailText.verticalOverflow = VerticalWrapMode.Overflow;

        var routePanel = UIFactory.CreatePanel(root.transform, "RoutePanel", new Color(0.11f, 0.09f, 0.09f, 0.96f));
        var routeRect = routePanel.GetComponent<RectTransform>();
        routeRect.anchorMin = new Vector2(0.52f, 0.145f);
        routeRect.anchorMax = new Vector2(0.92f, 0.49f);
        routeRect.offsetMin = Vector2.zero;
        routeRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(routePanel.transform, "RouteOutline", new Color(0.88f, 0.8f, 0.72f, 0.55f), 1f);
        var routeText = UIFactory.CreateText(routePanel.transform, "RouteText", "Profile", 23, TextAnchor.UpperLeft, new Color(0.94f, 0.94f, 0.94f, 1f));
        routeText.rectTransform.offsetMin = new Vector2(18f, 14f);
        routeText.rectTransform.offsetMax = new Vector2(-18f, -14f);

        var footer = UIFactory.CreateContainer(root.transform, "Footer", new Vector2(0.08f, 0.035f), new Vector2(0.92f, 0.125f), Vector2.zero, Vector2.zero);

        var navRow = UIFactory.CreateContainer(footer, "NavRow", new Vector2(0f, 0.56f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        var previousButton = UIFactory.CreateButton(navRow, "PreviousButton", "Previous", delegate { });
        var previousRect = previousButton.GetComponent<RectTransform>();
        previousRect.anchorMin = new Vector2(0f, 0f);
        previousRect.anchorMax = new Vector2(0.28f, 1f);
        previousRect.offsetMin = Vector2.zero;
        previousRect.offsetMax = new Vector2(-8f, 0f);

        var enterButton = UIFactory.CreateButton(navRow, "EnterButton", "Enter Node", delegate { });
        var enterRect = enterButton.GetComponent<RectTransform>();
        enterRect.anchorMin = new Vector2(0.3f, 0f);
        enterRect.anchorMax = new Vector2(0.7f, 1f);
        enterRect.offsetMin = Vector2.zero;
        enterRect.offsetMax = Vector2.zero;

        var nextButton = UIFactory.CreateButton(navRow, "NextButton", "Next", delegate { });
        var nextRect = nextButton.GetComponent<RectTransform>();
        nextRect.anchorMin = new Vector2(0.72f, 0f);
        nextRect.anchorMax = new Vector2(1f, 1f);
        nextRect.offsetMin = new Vector2(8f, 0f);
        nextRect.offsetMax = Vector2.zero;

        var actionRow = UIFactory.CreateContainer(footer, "ActionRow", new Vector2(0f, 0f), new Vector2(1f, 0.38f), Vector2.zero, Vector2.zero);
        var equipmentButton = UIFactory.CreateButton(actionRow, "EquipmentButton", "Equipment", delegate { });
        var equipmentRect = equipmentButton.GetComponent<RectTransform>();
        equipmentRect.anchorMin = new Vector2(0f, 0f);
        equipmentRect.anchorMax = new Vector2(0.18f, 1f);
        equipmentRect.offsetMin = Vector2.zero;
        equipmentRect.offsetMax = new Vector2(-6f, 0f);

        var spiritConvertButton = UIFactory.CreateButton(actionRow, "SpiritConvertButton", "\u7075\u77f3\u8f6c\u6362", delegate { });
        var spiritConvertRect = spiritConvertButton.GetComponent<RectTransform>();
        spiritConvertRect.anchorMin = new Vector2(0.2f, 0f);
        spiritConvertRect.anchorMax = new Vector2(0.38f, 1f);
        spiritConvertRect.offsetMin = Vector2.zero;
        spiritConvertRect.offsetMax = new Vector2(-4f, 0f);

        var skillOverviewButton = UIFactory.CreateButton(actionRow, "SkillOverviewButton", "\u5df2\u5b66\u529f\u6cd5", delegate { });
        var skillOverviewRect = skillOverviewButton.GetComponent<RectTransform>();
        skillOverviewRect.anchorMin = new Vector2(0.4f, 0f);
        skillOverviewRect.anchorMax = new Vector2(0.58f, 1f);
        skillOverviewRect.offsetMin = Vector2.zero;
        skillOverviewRect.offsetMax = new Vector2(-2f, 0f);

        var resetButton = UIFactory.CreateButton(actionRow, "ResetButton", "Reset Run", delegate { });
        var resetRect = resetButton.GetComponent<RectTransform>();
        resetRect.anchorMin = new Vector2(0.6f, 0f);
        resetRect.anchorMax = new Vector2(0.78f, 1f);
        resetRect.offsetMin = Vector2.zero;
        resetRect.offsetMax = new Vector2(-2f, 0f);

        var backButton = UIFactory.CreateButton(actionRow, "BackButton", "Back To Menu", delegate { });
        var backRect = backButton.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.8f, 0f);
        backRect.anchorMax = new Vector2(1f, 1f);
        backRect.offsetMin = new Vector2(4f, 0f);
        backRect.offsetMax = Vector2.zero;

        var page = root.AddComponent<UIMapPage>();
        BindSerializedProperty(page, "titleText", titleText);
        BindSerializedProperty(page, "statusText", statusText);
        BindSerializedProperty(page, "regionText", regionText);
        BindSerializedProperty(page, "longevityText", longevityText);
        BindSerializedProperty(page, "nodeGraphRoot", graphRoot);
        BindSerializedProperty(page, "nodeDetailText", detailText);
        BindSerializedProperty(page, "routeText", routeText);
        BindSerializedProperty(page, "previousButton", previousButton);
        BindSerializedProperty(page, "enterButton", enterButton);
        BindSerializedProperty(page, "nextButton", nextButton);
        BindSerializedProperty(page, "equipmentButton", equipmentButton);
        BindSerializedProperty(page, "spiritConvertButton", spiritConvertButton);
        BindSerializedProperty(page, "skillOverviewButton", skillOverviewButton);
        BindSerializedProperty(page, "resetButton", resetButton);
        BindSerializedProperty(page, "backButton", backButton);
        SavePrefab(root, PagesFolder + "/MapPage.prefab");
    }

    private static void BuildBattlePagePrefab()
    {
        var root = UIFactory.CreatePanel(null, "BattlePage", new Color(0.12f, 0.08f, 0.08f, 1f));
        UIFactory.AddOutlineBox(root.transform, "BattleFrame", new Color(0.86f, 0.78f, 0.72f, 0.8f), 2f);

        var title = UIFactory.CreateContainer(root.transform, "Title", new Vector2(0.06f, 0.92f), new Vector2(0.94f, 0.98f), Vector2.zero, Vector2.zero);
        var titleText = UIFactory.CreateText(title, "Label", "Battle", 42, TextAnchor.MiddleCenter, Color.white);
        AddLocalizedText(titleText, "battle.title");

        var status = UIFactory.CreateContainer(root.transform, "Status", new Vector2(0.12f, 0.87f), new Vector2(0.88f, 0.915f), Vector2.zero, Vector2.zero);
        var statusText = UIFactory.CreateText(status, "StatusText", "Waiting", 24, TextAnchor.MiddleCenter, new Color(0.95f, 0.9f, 0.75f, 1f));
        AddLocalizedText(statusText, "battle.status_idle");

        var stageInfo = UIFactory.CreateContainer(root.transform, "StageInfo", new Vector2(0.18f, 0.885f), new Vector2(0.82f, 0.925f), Vector2.zero, Vector2.zero);
        var stageInfoText = UIFactory.CreateText(stageInfo, "StageInfoText", "Stage 1", 24, TextAnchor.MiddleCenter, new Color(0.82f, 0.88f, 0.96f, 1f));

        var overlay = UIFactory.CreatePanel(root.transform, "CombatOverlay", new Color(0.18f, 0.12f, 0.12f, 0.94f));
        var overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = new Vector2(0.03f, 0.03f);
        overlayRect.anchorMax = new Vector2(0.97f, 0.88f);
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(overlay.transform, "OverlayOutline", new Color(0.9f, 0.82f, 0.74f, 0.7f), 1f);

        var summaryBar = UIFactory.CreateContainer(overlay.transform, "SummaryBar", new Vector2(0.02f, 0.57f), new Vector2(0.97f, 0.96f), Vector2.zero, Vector2.zero);
        var playerPanel = CreateTeamInfoPanel(summaryBar, "PlayerTeam", new Vector2(0f, 0f), new Vector2(0.48f, 1f), "Player", "Unit A  HP 120/120\nUnit B  HP 150/150", "battle.player_team", "battle.player_content");
        var enemyPanel = CreateTeamInfoPanel(summaryBar, "EnemyTeam", new Vector2(0.52f, 0f), new Vector2(1f, 1f), "Enemy", "Enemy A  HP 100/100\nEnemy B  HP 90/90", "battle.enemy_team", "battle.enemy_content");

        var logPanel = UIFactory.CreatePanel(overlay.transform, "LogPanel", new Color(0.13f, 0.09f, 0.09f, 0.96f));
        var logPanelRect = logPanel.GetComponent<RectTransform>();
        logPanelRect.anchorMin = new Vector2(0.03f, 0.12f);
        logPanelRect.anchorMax = new Vector2(0.97f, 0.54f);
        logPanelRect.offsetMin = Vector2.zero;
        logPanelRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(logPanel.transform, "LogOutline", new Color(0.88f, 0.8f, 0.72f, 0.6f), 1f);

        var logTitle = UIFactory.CreateContainer(logPanel.transform, "LogTitle", new Vector2(0.02f, 0.91f), new Vector2(0.98f, 0.985f), Vector2.zero, Vector2.zero);
        var logTitleText = UIFactory.CreateText(logTitle, "LogTitleText", "Battle Log", 30, TextAnchor.MiddleLeft, Color.white);
        AddLocalizedText(logTitleText, "battle.log_title");

        var scrollRoot = UIFactory.CreateContainer(logPanel.transform, "LogScrollRoot", new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.89f), Vector2.zero, Vector2.zero);
        var logScrollRect = UIFactory.CreateScrollRect(scrollRoot, "BattleLogScroll", new Color(0f, 0f, 0f, 0.12f));
        UIFactory.Stretch(logScrollRect.GetComponent<RectTransform>());

        var logBodyText = UIFactory.CreateText(logScrollRect.content, "BodyText", "Battle log preview.", 28, TextAnchor.UpperLeft, new Color(0.94f, 0.94f, 0.94f, 1f));
        var logBodyRect = logBodyText.rectTransform;
        logBodyRect.anchorMin = new Vector2(0f, 1f);
        logBodyRect.anchorMax = new Vector2(1f, 1f);
        logBodyRect.pivot = new Vector2(0.5f, 1f);
        logBodyRect.offsetMin = new Vector2(18f, 0f);
        logBodyRect.offsetMax = new Vector2(-18f, 0f);
        logBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        logBodyText.verticalOverflow = VerticalWrapMode.Overflow;
        var fitter = logBodyText.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        AddLocalizedText(logBodyText, "battle.log_content");

        var footer = UIFactory.CreateContainer(overlay.transform, "Footer", new Vector2(0.24f, 0.03f), new Vector2(0.76f, 0.11f), Vector2.zero, Vector2.zero);
        var backButton = UIFactory.CreateButton(footer, "BackButton", "Flee", delegate { });
        UIFactory.Stretch(backButton.GetComponent<RectTransform>());

        var equipmentPanel = UIFactory.CreatePanel(overlay.transform, "EquipmentPanel", new Color(0f, 0f, 0f, 0.55f));
        var equipmentPanelRect = equipmentPanel.GetComponent<RectTransform>();
        equipmentPanelRect.anchorMin = Vector2.zero;
        equipmentPanelRect.anchorMax = Vector2.one;
        equipmentPanelRect.offsetMin = Vector2.zero;
        equipmentPanelRect.offsetMax = Vector2.zero;
        
        var equipmentContent = UIFactory.CreatePanel(equipmentPanel.transform, "EquipmentContent", new Color(0.1f, 0.07f, 0.07f, 0.98f));
        var equipmentContentRect = equipmentContent.GetComponent<RectTransform>();
        equipmentContentRect.anchorMin = new Vector2(0.05f, 0.1f);
        equipmentContentRect.anchorMax = new Vector2(0.95f, 0.9f);
        equipmentContentRect.offsetMin = Vector2.zero;
        equipmentContentRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(equipmentContent.transform, "EquipmentOutline", new Color(0.9f, 0.82f, 0.74f, 0.75f), 1f);

        var equipmentTitle = UIFactory.CreateContainer(equipmentContent.transform, "EquipmentTitle", new Vector2(0.05f, 0.88f), new Vector2(0.75f, 0.97f), Vector2.zero, Vector2.zero);
        var equipmentTitleText = UIFactory.CreateText(equipmentTitle, "EquipmentTitleText", "Equipment Detail", 30, TextAnchor.MiddleLeft, Color.white);
        AddLocalizedText(equipmentTitleText, "battle.equipment_detail_title");

        var controlsRoot = UIFactory.CreateContainer(equipmentContent.transform, "ControlsRoot", new Vector2(0.05f, 0.44f), new Vector2(0.95f, 0.86f), Vector2.zero, Vector2.zero);

        var cycleEquipmentButton = UIFactory.CreateButton(controlsRoot, "CycleEquipmentButton", "Switch Preset", delegate { });
        AddLocalizedText(cycleEquipmentButton.GetComponentInChildren<Text>(), "battle.button_cycle_equipment");
        var cycleEquipmentRect = cycleEquipmentButton.GetComponent<RectTransform>();
        cycleEquipmentRect.anchorMin = new Vector2(0f, 0.76f);
        cycleEquipmentRect.anchorMax = new Vector2(0.43f, 0.94f);
        cycleEquipmentRect.offsetMin = Vector2.zero;
        cycleEquipmentRect.offsetMax = Vector2.zero;

        var cycleEquipmentUnitButton = UIFactory.CreateButton(controlsRoot, "CycleEquipmentUnitButton", "Unit", delegate { });
        var cycleEquipmentUnitRect = cycleEquipmentUnitButton.GetComponent<RectTransform>();
        cycleEquipmentUnitRect.anchorMin = new Vector2(0f, 0.72f);
        cycleEquipmentUnitRect.anchorMax = new Vector2(0.48f, 0.98f);
        cycleEquipmentUnitRect.offsetMin = Vector2.zero;
        cycleEquipmentUnitRect.offsetMax = Vector2.zero;

        var cycleWeaponButton = UIFactory.CreateButton(controlsRoot, "CycleWeaponButton", "Weapon", delegate { });
        var cycleWeaponRect = cycleWeaponButton.GetComponent<RectTransform>();
        cycleWeaponRect.anchorMin = new Vector2(0f, 0.38f);
        cycleWeaponRect.anchorMax = new Vector2(0.31f, 0.66f);
        cycleWeaponRect.offsetMin = Vector2.zero;
        cycleWeaponRect.offsetMax = Vector2.zero;

        var cycleArmorButton = UIFactory.CreateButton(controlsRoot, "CycleArmorButton", "Armor", delegate { });
        var cycleArmorRect = cycleArmorButton.GetComponent<RectTransform>();
        cycleArmorRect.anchorMin = new Vector2(0.345f, 0.38f);
        cycleArmorRect.anchorMax = new Vector2(0.655f, 0.66f);
        cycleArmorRect.offsetMin = Vector2.zero;
        cycleArmorRect.offsetMax = Vector2.zero;

        var cycleAccessoryButton = UIFactory.CreateButton(controlsRoot, "CycleAccessoryButton", "Accessory", delegate { });
        var cycleAccessoryRect = cycleAccessoryButton.GetComponent<RectTransform>();
        cycleAccessoryRect.anchorMin = new Vector2(0.69f, 0.38f);
        cycleAccessoryRect.anchorMax = new Vector2(1f, 0.66f);
        cycleAccessoryRect.offsetMin = Vector2.zero;
        cycleAccessoryRect.offsetMax = Vector2.zero;

        var autoOffenseButton = UIFactory.CreateButton(controlsRoot, "AutoOffenseButton", "Auto Offense", delegate { });
        var autoOffenseRect = autoOffenseButton.GetComponent<RectTransform>();
        autoOffenseRect.anchorMin = new Vector2(0f, 0.02f);
        autoOffenseRect.anchorMax = new Vector2(0.48f, 0.26f);
        autoOffenseRect.offsetMin = Vector2.zero;
        autoOffenseRect.offsetMax = Vector2.zero;

        var autoDefenseButton = UIFactory.CreateButton(controlsRoot, "AutoDefenseButton", "Auto Defense", delegate { });
        var autoDefenseRect = autoDefenseButton.GetComponent<RectTransform>();
        autoDefenseRect.anchorMin = new Vector2(0.52f, 0.02f);
        autoDefenseRect.anchorMax = new Vector2(1f, 0.26f);
        autoDefenseRect.offsetMin = Vector2.zero;
        autoDefenseRect.offsetMax = Vector2.zero;

        var resetEquipmentButton = UIFactory.CreateButton(controlsRoot, "ResetEquipmentButton", "Reset", delegate { });
        var resetEquipmentRect = resetEquipmentButton.GetComponent<RectTransform>();
        resetEquipmentRect.anchorMin = new Vector2(0.52f, 0.72f);
        resetEquipmentRect.anchorMax = new Vector2(1f, 0.98f);
        resetEquipmentRect.offsetMin = Vector2.zero;
        resetEquipmentRect.offsetMax = Vector2.zero;

        var closeEquipmentButton = UIFactory.CreateButton(equipmentContent.transform, "CloseEquipmentButton", "Close", delegate { });
        AddLocalizedText(closeEquipmentButton.GetComponentInChildren<Text>(), "battle.button_close_equipment");
        var closeEquipmentRect = closeEquipmentButton.GetComponent<RectTransform>();
        closeEquipmentRect.anchorMin = new Vector2(0.72f, 0.88f);
        closeEquipmentRect.anchorMax = new Vector2(0.95f, 0.97f);
        closeEquipmentRect.offsetMin = Vector2.zero;
        closeEquipmentRect.offsetMax = Vector2.zero;

        var selectionTitle = UIFactory.CreateContainer(equipmentContent.transform, "SelectionTitle", new Vector2(0.05f, 0.48f), new Vector2(0.95f, 0.54f), Vector2.zero, Vector2.zero);
        var selectionTitleText = UIFactory.CreateText(selectionTitle, "SelectionTitleText", "\u88c5\u5907\u5217\u8868", 22, TextAnchor.MiddleLeft, new Color(0.95f, 0.86f, 0.74f, 1f));

        var selectionScrollRoot = UIFactory.CreateContainer(equipmentContent.transform, "SelectionScrollRoot", new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.41f), Vector2.zero, Vector2.zero);
        var selectionScrollRect = UIFactory.CreateScrollRect(selectionScrollRoot, "SelectionScroll", new Color(0f, 0f, 0f, 0.18f));
        UIFactory.Stretch(selectionScrollRect.GetComponent<RectTransform>());

        var equipmentScrollRoot = UIFactory.CreateContainer(equipmentContent.transform, "EquipmentScrollRoot", new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.17f), Vector2.zero, Vector2.zero);
        var equipmentScrollRect = UIFactory.CreateScrollRect(equipmentScrollRoot, "EquipmentDetailScroll", new Color(0f, 0f, 0f, 0.18f));
        UIFactory.Stretch(equipmentScrollRect.GetComponent<RectTransform>());
        var equipmentDetailText = UIFactory.CreateText(equipmentScrollRect.content, "EquipmentDetailText", "Equipment detail", 22, TextAnchor.UpperLeft, new Color(0.92f, 0.92f, 0.92f, 1f));
        var equipmentDetailTextRect = equipmentDetailText.rectTransform;
        equipmentDetailTextRect.anchorMin = new Vector2(0f, 1f);
        equipmentDetailTextRect.anchorMax = new Vector2(1f, 1f);
        equipmentDetailTextRect.pivot = new Vector2(0.5f, 1f);
        equipmentDetailTextRect.offsetMin = new Vector2(18f, 0f);
        equipmentDetailTextRect.offsetMax = new Vector2(-18f, 0f);
        equipmentDetailText.horizontalOverflow = HorizontalWrapMode.Wrap;
        equipmentDetailText.verticalOverflow = VerticalWrapMode.Overflow;
        var equipmentDetailFitter = equipmentDetailText.gameObject.AddComponent<ContentSizeFitter>();
        equipmentDetailFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        equipmentDetailFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        equipmentPanel.SetActive(false);

        var page = root.AddComponent<UIBattlePage>();
        BindSerializedProperty(page, "battleLogOverlay", overlay);
        BindSerializedProperty(page, "backButton", backButton);
        BindSerializedProperty(page, "closeEquipmentButton", closeEquipmentButton);
        BindSerializedProperty(page, "cycleEquipmentPresetButton", cycleEquipmentButton);
        BindSerializedProperty(page, "cycleEquipmentUnitButton", cycleEquipmentUnitButton);
        BindSerializedProperty(page, "cycleWeaponButton", cycleWeaponButton);
        BindSerializedProperty(page, "cycleArmorButton", cycleArmorButton);
        BindSerializedProperty(page, "cycleAccessoryButton", cycleAccessoryButton);
        BindSerializedProperty(page, "autoOffenseButton", autoOffenseButton);
        BindSerializedProperty(page, "autoDefenseButton", autoDefenseButton);
        BindSerializedProperty(page, "resetEquipmentButton", resetEquipmentButton);
        BindSerializedProperty(page, "equipmentPanel", equipmentPanel);
        BindSerializedProperty(page, "equipmentSelectionTitleText", selectionTitleText);
        BindSerializedProperty(page, "equipmentSelectionContent", selectionScrollRect.content);
        BindSerializedProperty(page, "equipmentDetailText", equipmentDetailText);
        BindSerializedProperty(page, "stageInfoText", stageInfoText);
        BindSerializedProperty(page, "statusText", statusText);
        BindSerializedProperty(page, "playerTeamText", playerPanel.BodyText);
        BindSerializedProperty(page, "enemyTeamText", enemyPanel.BodyText);
        BindSerializedProperty(page, "playerCardRoot", playerPanel.CardRoot);
        BindSerializedProperty(page, "playerCardTemplate", playerPanel.CardTemplate);
        BindSerializedProperty(page, "enemyCardRoot", enemyPanel.CardRoot);
        BindSerializedProperty(page, "enemyCardTemplate", enemyPanel.CardTemplate);
        BindSerializedProperty(page, "playerEquipmentText", playerPanel.EquipmentText);
        BindSerializedProperty(page, "enemyEquipmentText", enemyPanel.EquipmentText);
        BindSerializedProperty(page, "battleLogText", logBodyText);
        BindSerializedProperty(page, "battleLogScrollRect", logScrollRect);

        SavePrefab(root, PagesFolder + "/BattlePage.prefab");
    }

    private static void BuildConfirmPopupPrefab()
    {
        var root = UIFactory.CreatePanel(null, "ConfirmPopup", new Color(0f, 0f, 0f, 0.6f));
        var panel = UIFactory.CreatePanel(root.transform, "Panel", new Color(0.15f, 0.16f, 0.2f, 1f));
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.08f, 0.24f);
        panelRect.anchorMax = new Vector2(0.92f, 0.76f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(panel.transform, "Outline", new Color(0.82f, 0.84f, 0.9f, 0.75f), 1f);

        var title = UIFactory.CreateText(panel.transform, "Title", "Confirm", 40, TextAnchor.UpperCenter, Color.white);
        title.rectTransform.anchorMin = new Vector2(0.08f, 0.72f);
        title.rectTransform.anchorMax = new Vector2(0.92f, 0.92f);
        title.rectTransform.offsetMin = Vector2.zero;
        title.rectTransform.offsetMax = Vector2.zero;

        var message = UIFactory.CreateText(panel.transform, "Message", "Message", 28, TextAnchor.MiddleCenter, new Color(0.93f, 0.93f, 0.93f, 1f));
        message.rectTransform.anchorMin = new Vector2(0.08f, 0.34f);
        message.rectTransform.anchorMax = new Vector2(0.92f, 0.7f);
        message.rectTransform.offsetMin = Vector2.zero;
        message.rectTransform.offsetMax = Vector2.zero;

        var confirmButton = UIFactory.CreateButton(panel.transform, "ConfirmButton", "Confirm", delegate { });
        AddLocalizedText(confirmButton.GetComponentInChildren<Text>(), "popup.confirm_button");
        var confirmRect = confirmButton.GetComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.12f, 0.1f);
        confirmRect.anchorMax = new Vector2(0.44f, 0.2f);
        confirmRect.offsetMin = Vector2.zero;
        confirmRect.offsetMax = Vector2.zero;

        var cancelButton = UIFactory.CreateButton(panel.transform, "CancelButton", "Cancel", delegate { });
        AddLocalizedText(cancelButton.GetComponentInChildren<Text>(), "popup.cancel_button");
        var cancelRect = cancelButton.GetComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0.56f, 0.1f);
        cancelRect.anchorMax = new Vector2(0.88f, 0.2f);
        cancelRect.offsetMin = Vector2.zero;
        cancelRect.offsetMax = Vector2.zero;

        var popup = root.AddComponent<UIConfirmPopup>();
        BindSerializedProperty(popup, "titleText", title);
        BindSerializedProperty(popup, "messageText", message);
        BindSerializedProperty(popup, "confirmButton", confirmButton);
        BindSerializedProperty(popup, "cancelButton", cancelButton);
        BindSerializedProperty(popup, "titleLocalizedText", AddLocalizedText(title, "popup.confirm.title"));
        BindSerializedProperty(popup, "messageLocalizedText", AddLocalizedText(message, "popup.confirm.message"));

        SavePrefab(root, PopupsFolder + "/ConfirmPopup.prefab");
    }

    private static void BuildCardBrowserPopupPrefab()
    {
        var root = UIFactory.CreatePanel(null, "CardBrowserPopup", new Color(0f, 0f, 0f, 0.72f));
        var panel = UIFactory.CreatePanel(root.transform, "Panel", new Color(0.06f, 0.06f, 0.07f, 0.985f));
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.05f, 0.08f);
        panelRect.anchorMax = new Vector2(0.95f, 0.9f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(panel.transform, "Outline", new Color(0.76f, 0.46f, 0.18f, 0.95f), 2f);

        var title = UIFactory.CreateText(panel.transform, "Title", "\u5361\u724c\u603b\u89c8", 36, TextAnchor.MiddleLeft, Color.white);
        title.rectTransform.anchorMin = new Vector2(0.04f, 0.91f);
        title.rectTransform.anchorMax = new Vector2(0.7f, 0.98f);
        title.rectTransform.offsetMin = Vector2.zero;
        title.rectTransform.offsetMax = Vector2.zero;

        var subtitle = UIFactory.CreateText(panel.transform, "Subtitle", string.Empty, 18, TextAnchor.MiddleLeft, new Color(0.82f, 0.88f, 0.95f, 0f));
        subtitle.rectTransform.anchorMin = new Vector2(0.04f, 0.89f);
        subtitle.rectTransform.anchorMax = new Vector2(0.7f, 0.9f);
        subtitle.rectTransform.offsetMin = Vector2.zero;
        subtitle.rectTransform.offsetMax = Vector2.zero;

        var closeButton = UIFactory.CreateButton(panel.transform, "CloseButton", "\u5173\u95ed", delegate { });
        var closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.86f, 0.915f);
        closeRect.anchorMax = new Vector2(0.96f, 0.972f);
        closeRect.offsetMin = Vector2.zero;
        closeRect.offsetMax = Vector2.zero;
        var closeLabel = closeButton.GetComponentInChildren<Text>();
        if (closeLabel != null)
        {
            closeLabel.fontSize = 18;
        }

        var leftScrollRoot = UIFactory.CreateContainer(panel.transform, "CardScrollRoot", new Vector2(0.04f, 0.36f), new Vector2(0.96f, 0.84f), Vector2.zero, Vector2.zero);
        var cardScrollRect = UIFactory.CreateScrollRect(leftScrollRoot, "CardScroll", new Color(0f, 0f, 0f, 0.18f));
        UIFactory.Stretch(cardScrollRect.GetComponent<RectTransform>());

        var detailPanel = UIFactory.CreatePanel(panel.transform, "DetailPanel", new Color(0.05f, 0.05f, 0.06f, 0.985f));
        var detailRect = detailPanel.GetComponent<RectTransform>();
        detailRect.anchorMin = new Vector2(0.04f, 0.08f);
        detailRect.anchorMax = new Vector2(0.96f, 0.29f);
        detailRect.offsetMin = Vector2.zero;
        detailRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(detailPanel.transform, "DetailOutline", new Color(0.76f, 0.46f, 0.18f, 0.8f), 2f);

        var detailTitle = UIFactory.CreateText(detailPanel.transform, "DetailTitle", "\u8be6\u60c5", 28, TextAnchor.UpperLeft, Color.white);
        detailTitle.rectTransform.anchorMin = new Vector2(0.04f, 0.66f);
        detailTitle.rectTransform.anchorMax = new Vector2(0.96f, 0.92f);
        detailTitle.rectTransform.offsetMin = Vector2.zero;
        detailTitle.rectTransform.offsetMax = Vector2.zero;

        var detailBody = UIFactory.CreateText(detailPanel.transform, "DetailBody", string.Empty, 18, TextAnchor.UpperLeft, new Color(0.94f, 0.94f, 0.94f, 1f));
        detailBody.rectTransform.anchorMin = new Vector2(0.04f, 0.08f);
        detailBody.rectTransform.anchorMax = new Vector2(0.96f, 0.6f);
        detailBody.rectTransform.offsetMin = Vector2.zero;
        detailBody.rectTransform.offsetMax = Vector2.zero;
        detailBody.supportRichText = true;

        var template = UIFactory.CreateButton(cardScrollRect.content, "CardTemplate", "\u5361\u724c", delegate { });
        var templateRect = template.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 1f);
        templateRect.anchorMax = new Vector2(0f, 1f);
        templateRect.pivot = new Vector2(0f, 1f);
        templateRect.sizeDelta = new Vector2(0f, 118f);
        templateRect.anchoredPosition = Vector2.zero;
        templateRect.sizeDelta = new Vector2(180f, 260f);
        var templateImage = template.GetComponent<Image>();
        if (templateImage != null)
        {
            templateImage.color = new Color(0.03f, 0.03f, 0.035f, 0.99f);
        }
        UIFactory.AddOutlineBox(template.transform, "OuterFrame", new Color(0.78f, 0.48f, 0.2f, 0.98f), 3f);
        var innerFrame = UIFactory.AddOutlineBox(template.transform, "InnerFrame", new Color(0.78f, 0.48f, 0.2f, 0.45f), 1f);
        if (innerFrame != null)
        {
            var innerRect = innerFrame.GetComponent<RectTransform>();
            innerRect.offsetMin = new Vector2(6f, 6f);
            innerRect.offsetMax = new Vector2(-6f, -6f);
        }
        var selectedFrame = UIFactory.AddOutlineBox(template.transform, "SelectedFrame", new Color(1f, 0.95f, 0.78f, 0.95f), 2f);
        if (selectedFrame != null)
        {
            var selectedRect = selectedFrame.GetComponent<RectTransform>();
            selectedRect.offsetMin = new Vector2(2f, 2f);
            selectedRect.offsetMax = new Vector2(-2f, -2f);
            selectedFrame.gameObject.SetActive(false);
        }
        var templateText = template.GetComponentInChildren<Text>();
        if (templateText != null)
        {
            templateText.text = string.Empty;
            templateText.gameObject.SetActive(false);
        }
        var titleText = UIFactory.CreateText(template.transform, "TitleText", "\u6807\u9898", 22, TextAnchor.UpperLeft, new Color(0.96f, 0.92f, 0.84f, 1f));
        titleText.rectTransform.anchorMin = new Vector2(0.08f, 0.72f);
        titleText.rectTransform.anchorMax = new Vector2(0.92f, 0.9f);
        titleText.rectTransform.offsetMin = Vector2.zero;
        titleText.rectTransform.offsetMax = Vector2.zero;
        titleText.supportRichText = true;
        var subText = UIFactory.CreateText(template.transform, "SubtitleText", "\u526f\u6807\u9898", 18, TextAnchor.UpperLeft, new Color(0.72f, 0.78f, 0.84f, 0.92f));
        subText.rectTransform.anchorMin = new Vector2(0.08f, 0.52f);
        subText.rectTransform.anchorMax = new Vector2(0.92f, 0.68f);
        subText.rectTransform.offsetMin = Vector2.zero;
        subText.rectTransform.offsetMax = Vector2.zero;
        subText.supportRichText = true;
        var progressRoot = UIFactory.CreatePanel(template.transform, "ProgressRoot", new Color(1f, 1f, 1f, 0.03f));
        var progressRootRect = progressRoot.GetComponent<RectTransform>();
        progressRootRect.anchorMin = new Vector2(0.08f, 0.1f);
        progressRootRect.anchorMax = new Vector2(0.92f, 0.18f);
        progressRootRect.offsetMin = Vector2.zero;
        progressRootRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(progressRoot.transform, "ProgressOutline", new Color(0.76f, 0.46f, 0.18f, 0.55f), 1f);
        var fill = UIFactory.CreatePanel(progressRoot.transform, "Fill", new Color(0.9f, 0.9f, 0.9f, 0.9f));
        fill.GetComponent<Image>().type = Image.Type.Filled;
        fill.GetComponent<Image>().fillMethod = Image.FillMethod.Horizontal;
        fill.GetComponent<Image>().fillOrigin = 0;
        fill.GetComponent<Image>().fillAmount = 0.5f;
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var progressLabel = UIFactory.CreateText(progressRoot.transform, "ProgressLabel", "HP 0/0", 16, TextAnchor.MiddleCenter, new Color(0.96f, 0.96f, 0.96f, 1f));
        progressLabel.supportRichText = false;
        template.gameObject.SetActive(false);

        var popup = root.AddComponent<UICardBrowserPopup>();
        BindSerializedProperty(popup, "titleText", title);
        BindSerializedProperty(popup, "subtitleText", subtitle);
        BindSerializedProperty(popup, "cardContentRoot", cardScrollRect.content);
        BindSerializedProperty(popup, "cardTemplateButton", template);
        BindSerializedProperty(popup, "detailTitleText", detailTitle);
        BindSerializedProperty(popup, "detailBodyText", detailBody);
        BindSerializedProperty(popup, "closeButton", closeButton);

        SavePrefab(root, PopupsFolder + "/CardBrowserPopup.prefab");
    }

    private static void BuildEquipmentPopupPrefab()
    {
        var root = UIFactory.CreatePanel(null, "EquipmentPopup", new Color(0f, 0f, 0f, 0.72f));
        var panel = UIFactory.CreatePanel(root.transform, "Panel", new Color(0.06f, 0.06f, 0.07f, 0.985f));
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.05f, 0.08f);
        panelRect.anchorMax = new Vector2(0.95f, 0.9f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(panel.transform, "Outline", new Color(0.76f, 0.46f, 0.18f, 0.95f), 2f);

        var title = UIFactory.CreateText(panel.transform, "Title", "\u88c5\u5907", 36, TextAnchor.UpperLeft, Color.white);
        title.rectTransform.anchorMin = new Vector2(0.04f, 0.91f);
        title.rectTransform.anchorMax = new Vector2(0.6f, 0.98f);
        title.rectTransform.offsetMin = Vector2.zero;
        title.rectTransform.offsetMax = Vector2.zero;

        var closeButton = UIFactory.CreateButton(panel.transform, "CloseButton", "\u5173\u95ed", delegate { });
        var closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.86f, 0.915f);
        closeRect.anchorMax = new Vector2(0.96f, 0.972f);
        closeRect.offsetMin = Vector2.zero;
        closeRect.offsetMax = Vector2.zero;
        var closeLabel = closeButton.GetComponentInChildren<Text>();
        if (closeLabel != null)
        {
            closeLabel.fontSize = 18;
        }

        var slotScrollRoot = UIFactory.CreateContainer(panel.transform, "SlotScrollRoot", new Vector2(0.04f, 0.64f), new Vector2(0.96f, 0.86f), Vector2.zero, Vector2.zero);
        var slotScrollRect = UIFactory.CreateScrollRect(slotScrollRoot, "SlotScroll", new Color(0f, 0f, 0f, 0.18f));
        UIFactory.Stretch(slotScrollRect.GetComponent<RectTransform>());

        var inventoryScrollRoot = UIFactory.CreateContainer(panel.transform, "InventoryScrollRoot", new Vector2(0.04f, 0.28f), new Vector2(0.96f, 0.6f), Vector2.zero, Vector2.zero);
        var inventoryScrollRect = UIFactory.CreateScrollRect(inventoryScrollRoot, "InventoryScroll", new Color(0f, 0f, 0f, 0.18f));
        UIFactory.Stretch(inventoryScrollRect.GetComponent<RectTransform>());

        var detailPanel = UIFactory.CreatePanel(panel.transform, "DetailPanel", new Color(0.05f, 0.05f, 0.06f, 0.985f));
        var detailRect = detailPanel.GetComponent<RectTransform>();
        detailRect.anchorMin = new Vector2(0.04f, 0.08f);
        detailRect.anchorMax = new Vector2(0.96f, 0.23f);
        detailRect.offsetMin = Vector2.zero;
        detailRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(detailPanel.transform, "DetailOutline", new Color(0.76f, 0.46f, 0.18f, 0.8f), 2f);

        var detailTitle = UIFactory.CreateText(detailPanel.transform, "DetailTitle", "\u88c5\u5907\u8be6\u60c5", 24, TextAnchor.UpperLeft, Color.white);
        detailTitle.rectTransform.anchorMin = new Vector2(0.04f, 0.66f);
        detailTitle.rectTransform.anchorMax = new Vector2(0.96f, 0.92f);
        detailTitle.rectTransform.offsetMin = Vector2.zero;
        detailTitle.rectTransform.offsetMax = Vector2.zero;

        var detailBody = UIFactory.CreateText(detailPanel.transform, "DetailBody", string.Empty, 18, TextAnchor.UpperLeft, new Color(0.94f, 0.94f, 0.94f, 1f));
        detailBody.rectTransform.anchorMin = new Vector2(0.04f, 0.08f);
        detailBody.rectTransform.anchorMax = new Vector2(0.96f, 0.6f);
        detailBody.rectTransform.offsetMin = Vector2.zero;
        detailBody.rectTransform.offsetMax = Vector2.zero;

        var slotTemplate = UIFactory.CreateButton(slotScrollRect.content, "SlotTemplate", string.Empty, delegate { });
        var slotTemplateRect = slotTemplate.GetComponent<RectTransform>();
        slotTemplateRect.anchorMin = new Vector2(0f, 1f);
        slotTemplateRect.anchorMax = new Vector2(0f, 1f);
        slotTemplateRect.pivot = new Vector2(0f, 1f);
        slotTemplateRect.anchoredPosition = Vector2.zero;
        slotTemplateRect.sizeDelta = new Vector2(UICardChromeUtility.StandardCardWidth, UICardChromeUtility.StandardCardHeight);
        var slotLabel = slotTemplate.GetComponentInChildren<Text>();
        if (slotLabel != null)
        {
            slotLabel.text = string.Empty;
            slotLabel.gameObject.SetActive(false);
        }
        var slotTitle = UIFactory.CreateText(slotTemplate.transform, "TitleText", "\u6b66\u5668", 22, TextAnchor.UpperLeft, new Color(0.96f, 0.92f, 0.84f, 1f));
        slotTitle.rectTransform.anchorMin = new Vector2(0.08f, 0.72f);
        slotTitle.rectTransform.anchorMax = new Vector2(0.92f, 0.9f);
        slotTitle.rectTransform.offsetMin = Vector2.zero;
        slotTitle.rectTransform.offsetMax = Vector2.zero;
        var slotSubtitle = UIFactory.CreateText(slotTemplate.transform, "SubtitleText", "\u65e0\u88c5\u5907", 18, TextAnchor.UpperLeft, new Color(0.72f, 0.78f, 0.84f, 0.92f));
        slotSubtitle.rectTransform.anchorMin = new Vector2(0.08f, 0.52f);
        slotSubtitle.rectTransform.anchorMax = new Vector2(0.92f, 0.68f);
        slotSubtitle.rectTransform.offsetMin = Vector2.zero;
        slotSubtitle.rectTransform.offsetMax = Vector2.zero;
        var slotProgressRoot = UIFactory.CreatePanel(slotTemplate.transform, "ProgressRoot", new Color(1f, 1f, 1f, 0.03f));
        slotProgressRoot.GetComponent<RectTransform>().anchorMin = new Vector2(0.08f, 0.1f);
        slotProgressRoot.GetComponent<RectTransform>().anchorMax = new Vector2(0.92f, 0.18f);
        slotProgressRoot.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        slotProgressRoot.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        slotProgressRoot.SetActive(false);
        slotTemplate.gameObject.SetActive(false);

        var inventoryTemplate = Object.Instantiate(slotTemplate.gameObject, inventoryScrollRect.content, false);
        inventoryTemplate.name = "InventoryTemplate";
        inventoryTemplate.SetActive(false);

        var popup = root.AddComponent<UIEquipmentPopup>();
        BindSerializedProperty(popup, "titleText", title);
        BindSerializedProperty(popup, "closeButton", closeButton);
        BindSerializedProperty(popup, "slotContentRoot", slotScrollRect.content);
        BindSerializedProperty(popup, "slotTemplateButton", slotTemplate);
        BindSerializedProperty(popup, "inventoryContentRoot", inventoryScrollRect.content);
        BindSerializedProperty(popup, "inventoryTemplateButton", inventoryTemplate.GetComponent<Button>());
        BindSerializedProperty(popup, "detailTitleText", detailTitle);
        BindSerializedProperty(popup, "detailBodyText", detailBody);

        SavePrefab(root, PopupsFolder + "/EquipmentPopup.prefab");
    }

    private static void BuildSkillPopupPrefab()
    {
        var root = UIFactory.CreatePanel(null, "SkillPopup", new Color(0f, 0f, 0f, 0.72f));
        var panel = UIFactory.CreatePanel(root.transform, "Panel", new Color(0.06f, 0.06f, 0.07f, 0.985f));
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.05f, 0.08f);
        panelRect.anchorMax = new Vector2(0.95f, 0.9f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(panel.transform, "Outline", new Color(0.76f, 0.46f, 0.18f, 0.95f), 2f);

        var title = UIFactory.CreateText(panel.transform, "Title", "已学功法", 36, TextAnchor.UpperLeft, Color.white);
        title.rectTransform.anchorMin = new Vector2(0.04f, 0.91f);
        title.rectTransform.anchorMax = new Vector2(0.6f, 0.98f);
        title.rectTransform.offsetMin = Vector2.zero;
        title.rectTransform.offsetMax = Vector2.zero;

        var closeButton = UIFactory.CreateButton(panel.transform, "CloseButton", "关闭", delegate { });
        var closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.86f, 0.915f);
        closeRect.anchorMax = new Vector2(0.96f, 0.972f);
        closeRect.offsetMin = Vector2.zero;
        closeRect.offsetMax = Vector2.zero;
        var closeLabel = closeButton.GetComponentInChildren<Text>();
        if (closeLabel != null)
        {
            closeLabel.fontSize = 18;
        }

        var slotScrollRoot = UIFactory.CreateContainer(panel.transform, "SlotScrollRoot", new Vector2(0.04f, 0.64f), new Vector2(0.96f, 0.86f), Vector2.zero, Vector2.zero);
        var slotScrollRect = UIFactory.CreateScrollRect(slotScrollRoot, "SlotScroll", new Color(0f, 0f, 0f, 0.18f));
        UIFactory.Stretch(slotScrollRect.GetComponent<RectTransform>());

        var libraryScrollRoot = UIFactory.CreateContainer(panel.transform, "LibraryScrollRoot", new Vector2(0.04f, 0.28f), new Vector2(0.96f, 0.6f), Vector2.zero, Vector2.zero);
        var libraryScrollRect = UIFactory.CreateScrollRect(libraryScrollRoot, "LibraryScroll", new Color(0f, 0f, 0f, 0.18f));
        UIFactory.Stretch(libraryScrollRect.GetComponent<RectTransform>());

        var detailPanel = UIFactory.CreatePanel(panel.transform, "DetailPanel", new Color(0.05f, 0.05f, 0.06f, 0.985f));
        var detailRect = detailPanel.GetComponent<RectTransform>();
        detailRect.anchorMin = new Vector2(0.04f, 0.08f);
        detailRect.anchorMax = new Vector2(0.96f, 0.23f);
        detailRect.offsetMin = Vector2.zero;
        detailRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(detailPanel.transform, "DetailOutline", new Color(0.76f, 0.46f, 0.18f, 0.8f), 2f);

        var detailTitle = UIFactory.CreateText(detailPanel.transform, "DetailTitle", "功法详情", 24, TextAnchor.UpperLeft, Color.white);
        detailTitle.rectTransform.anchorMin = new Vector2(0.04f, 0.66f);
        detailTitle.rectTransform.anchorMax = new Vector2(0.96f, 0.92f);
        detailTitle.rectTransform.offsetMin = Vector2.zero;
        detailTitle.rectTransform.offsetMax = Vector2.zero;

        var detailBody = UIFactory.CreateText(detailPanel.transform, "DetailBody", string.Empty, 18, TextAnchor.UpperLeft, new Color(0.94f, 0.94f, 0.94f, 1f));
        detailBody.rectTransform.anchorMin = new Vector2(0.04f, 0.08f);
        detailBody.rectTransform.anchorMax = new Vector2(0.96f, 0.6f);
        detailBody.rectTransform.offsetMin = Vector2.zero;
        detailBody.rectTransform.offsetMax = Vector2.zero;

        var slotTemplate = UIFactory.CreateButton(slotScrollRect.content, "SlotTemplate", string.Empty, delegate { });
        var slotTemplateRect = slotTemplate.GetComponent<RectTransform>();
        slotTemplateRect.anchorMin = new Vector2(0f, 1f);
        slotTemplateRect.anchorMax = new Vector2(0f, 1f);
        slotTemplateRect.pivot = new Vector2(0f, 1f);
        slotTemplateRect.anchoredPosition = Vector2.zero;
        slotTemplateRect.sizeDelta = new Vector2(UICardChromeUtility.StandardCardWidth, UICardChromeUtility.StandardCardHeight);
        var slotLabel = slotTemplate.GetComponentInChildren<Text>();
        if (slotLabel != null)
        {
            slotLabel.text = string.Empty;
            slotLabel.gameObject.SetActive(false);
        }
        var slotTitle = UIFactory.CreateText(slotTemplate.transform, "TitleText", "技能栏1", 22, TextAnchor.UpperLeft, new Color(0.96f, 0.92f, 0.84f, 1f));
        slotTitle.rectTransform.anchorMin = new Vector2(0.08f, 0.72f);
        slotTitle.rectTransform.anchorMax = new Vector2(0.92f, 0.9f);
        slotTitle.rectTransform.offsetMin = Vector2.zero;
        slotTitle.rectTransform.offsetMax = Vector2.zero;
        var slotSubtitle = UIFactory.CreateText(slotTemplate.transform, "SubtitleText", "空技能栏", 18, TextAnchor.UpperLeft, new Color(0.72f, 0.78f, 0.84f, 0.92f));
        slotSubtitle.rectTransform.anchorMin = new Vector2(0.08f, 0.52f);
        slotSubtitle.rectTransform.anchorMax = new Vector2(0.92f, 0.68f);
        slotSubtitle.rectTransform.offsetMin = Vector2.zero;
        slotSubtitle.rectTransform.offsetMax = Vector2.zero;
        var slotProgressRoot = UIFactory.CreatePanel(slotTemplate.transform, "ProgressRoot", new Color(1f, 1f, 1f, 0.03f));
        slotProgressRoot.GetComponent<RectTransform>().anchorMin = new Vector2(0.08f, 0.1f);
        slotProgressRoot.GetComponent<RectTransform>().anchorMax = new Vector2(0.92f, 0.18f);
        slotProgressRoot.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        slotProgressRoot.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        slotProgressRoot.SetActive(false);
        slotTemplate.gameObject.SetActive(false);

        var libraryTemplate = Object.Instantiate(slotTemplate.gameObject, libraryScrollRect.content, false);
        libraryTemplate.name = "LibraryTemplate";
        libraryTemplate.SetActive(false);

        var popup = root.AddComponent<UISkillPopup>();
        BindSerializedProperty(popup, "titleText", title);
        BindSerializedProperty(popup, "closeButton", closeButton);
        BindSerializedProperty(popup, "slotContentRoot", slotScrollRect.content);
        BindSerializedProperty(popup, "slotTemplateButton", slotTemplate);
        BindSerializedProperty(popup, "libraryContentRoot", libraryScrollRect.content);
        BindSerializedProperty(popup, "libraryTemplateButton", libraryTemplate.GetComponent<Button>());
        BindSerializedProperty(popup, "detailTitleText", detailTitle);
        BindSerializedProperty(popup, "detailBodyText", detailBody);

        SavePrefab(root, PopupsFolder + "/SkillPopup.prefab");
    }
    private static void BuildSpiritStoneConvertPopupPrefab()
    {
        var root = UIFactory.CreatePanel(null, "SpiritStoneConvertPopup", new Color(0f, 0f, 0f, 0.74f));
        var panel = UIFactory.CreatePanel(root.transform, "Panel", new Color(0.08f, 0.09f, 0.12f, 0.97f));
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.08f, 0.18f);
        panelRect.anchorMax = new Vector2(0.92f, 0.82f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(panel.transform, "Outline", new Color(0.94f, 0.84f, 0.6f, 0.66f), 1f);

        var titleText = UIFactory.CreateText(panel.transform, "TitleText", "\u7075\u77f3\u8f6c\u6362", 40, TextAnchor.MiddleCenter, new Color(1f, 0.96f, 0.86f, 1f));
        titleText.rectTransform.anchorMin = new Vector2(0.08f, 0.9f);
        titleText.rectTransform.anchorMax = new Vector2(0.92f, 0.97f);
        titleText.rectTransform.offsetMin = Vector2.zero;
        titleText.rectTransform.offsetMax = Vector2.zero;

        var introText = UIFactory.CreateText(panel.transform, "IntroText", "\u6309\u4e94\u884c\u76f8\u751f\uff0c\u5c06\u4e00\u79cd\u7075\u77f3\u70bc\u5316\u4e3a\u4e0b\u4e00\u76f8\u7075\u77f3\u3002", 20, TextAnchor.MiddleCenter, new Color(0.84f, 0.9f, 0.96f, 0.82f));
        introText.rectTransform.anchorMin = new Vector2(0.08f, 0.82f);
        introText.rectTransform.anchorMax = new Vector2(0.92f, 0.89f);
        introText.rectTransform.offsetMin = Vector2.zero;
        introText.rectTransform.offsetMax = Vector2.zero;

        var sigilArea = UIFactory.CreateContainer(panel.transform, "SigilArea", new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.78f), Vector2.zero, Vector2.zero);
        var centerGlow = UIFactory.CreatePanel(sigilArea, "CenterGlow", new Color(0.2f, 0.24f, 0.32f, 0.16f));
        var centerGlowRect = centerGlow.GetComponent<RectTransform>();
        centerGlowRect.anchorMin = new Vector2(0.5f, 0.5f);
        centerGlowRect.anchorMax = new Vector2(0.5f, 0.5f);
        centerGlowRect.pivot = new Vector2(0.5f, 0.5f);
        centerGlowRect.anchoredPosition = Vector2.zero;
        centerGlowRect.sizeDelta = new Vector2(320f, 320f);

        var flowBeam = UIFactory.CreatePanel(sigilArea, "FlowBeam", new Color(1f, 0.92f, 0.76f, 0f));
        var flowBeamRect = flowBeam.GetComponent<RectTransform>();
        flowBeamRect.anchorMin = new Vector2(0.5f, 0.5f);
        flowBeamRect.anchorMax = new Vector2(0.5f, 0.5f);
        flowBeamRect.pivot = new Vector2(0f, 0.5f);
        flowBeamRect.anchoredPosition = Vector2.zero;
        flowBeamRect.sizeDelta = new Vector2(0f, 14f);

        var p1 = new Vector2(0f, 182f);
        var p2 = new Vector2(172f, 56f);
        var p3 = new Vector2(108f, -150f);
        var p4 = new Vector2(-108f, -150f);
        var p5 = new Vector2(-172f, 56f);
        var edgeColor = new Color(0.94f, 0.84f, 0.6f, 0.24f);
        var innerColor = new Color(0.75f, 0.86f, 0.98f, 0.13f);

        CreateSegment(sigilArea, "PentagonA", p1, p2, edgeColor, 2f);
        CreateSegment(sigilArea, "PentagonB", p2, p3, edgeColor, 2f);
        CreateSegment(sigilArea, "PentagonC", p3, p4, edgeColor, 2f);
        CreateSegment(sigilArea, "PentagonD", p4, p5, edgeColor, 2f);
        CreateSegment(sigilArea, "PentagonE", p5, p1, edgeColor, 2f);
        CreateSegment(sigilArea, "InnerA", p1, p3, innerColor, 1.4f);
        CreateSegment(sigilArea, "InnerB", p3, p5, innerColor, 1.4f);
        CreateSegment(sigilArea, "InnerC", p5, p2, innerColor, 1.4f);
        CreateSegment(sigilArea, "InnerD", p2, p4, innerColor, 1.4f);
        CreateSegment(sigilArea, "InnerE", p4, p1, innerColor, 1.4f);

        CreateSpiritStoneNode(sigilArea, "Metal", p1);
        CreateSpiritStoneNode(sigilArea, "Water", p2);
        CreateSpiritStoneNode(sigilArea, "Wood", p3);
        CreateSpiritStoneNode(sigilArea, "Fire", p4);
        CreateSpiritStoneNode(sigilArea, "Earth", p5);

        var summaryText = UIFactory.CreateText(panel.transform, "SummaryText", "2 \u91d1\u7075\u77f3 -> 1 \u6c34\u7075\u77f3", 22, TextAnchor.MiddleCenter, new Color(0.96f, 0.94f, 0.88f, 1f));
        summaryText.rectTransform.anchorMin = new Vector2(0.1f, 0.24f);
        summaryText.rectTransform.anchorMax = new Vector2(0.9f, 0.33f);
        summaryText.rectTransform.offsetMin = Vector2.zero;
        summaryText.rectTransform.offsetMax = Vector2.zero;

        var quantityLabelText = UIFactory.CreateText(panel.transform, "QuantityLabelText", "\u8f6c\u6362\u6b21\u6570", 20, TextAnchor.MiddleLeft, new Color(0.84f, 0.9f, 0.96f, 0.82f));
        quantityLabelText.rectTransform.anchorMin = new Vector2(0.1f, 0.18f);
        quantityLabelText.rectTransform.anchorMax = new Vector2(0.32f, 0.22f);
        quantityLabelText.rectTransform.offsetMin = Vector2.zero;
        quantityLabelText.rectTransform.offsetMax = Vector2.zero;

        var quantityRow = UIFactory.CreateContainer(panel.transform, "QuantityRow", new Vector2(0.1f, 0.11f), new Vector2(0.9f, 0.17f), Vector2.zero, Vector2.zero);
        var minusButton = UIFactory.CreateButton(quantityRow, "MinusButton", "-", delegate { });
        var minusRect = minusButton.GetComponent<RectTransform>();
        minusRect.anchorMin = new Vector2(0f, 0f);
        minusRect.anchorMax = new Vector2(0.18f, 1f);
        minusRect.offsetMin = Vector2.zero;
        minusRect.offsetMax = new Vector2(-4f, 0f);

        var quantityValueText = UIFactory.CreateText(quantityRow, "QuantityValueText", "1", 26, TextAnchor.MiddleCenter, new Color(1f, 0.96f, 0.86f, 1f));
        quantityValueText.rectTransform.anchorMin = new Vector2(0.22f, 0f);
        quantityValueText.rectTransform.anchorMax = new Vector2(0.48f, 1f);
        quantityValueText.rectTransform.offsetMin = Vector2.zero;
        quantityValueText.rectTransform.offsetMax = Vector2.zero;

        var plusButton = UIFactory.CreateButton(quantityRow, "PlusButton", "+", delegate { });
        var plusRect = plusButton.GetComponent<RectTransform>();
        plusRect.anchorMin = new Vector2(0.52f, 0f);
        plusRect.anchorMax = new Vector2(0.7f, 1f);
        plusRect.offsetMin = Vector2.zero;
        plusRect.offsetMax = new Vector2(-4f, 0f);

        var maxButton = UIFactory.CreateButton(quantityRow, "MaxButton", "MAX", delegate { });
        var maxRect = maxButton.GetComponent<RectTransform>();
        maxRect.anchorMin = new Vector2(0.74f, 0f);
        maxRect.anchorMax = new Vector2(1f, 1f);
        maxRect.offsetMin = Vector2.zero;
        maxRect.offsetMax = Vector2.zero;

        var actionRow = UIFactory.CreateContainer(panel.transform, "ActionRow", new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.07f), Vector2.zero, Vector2.zero);
        var convertButton = UIFactory.CreateButton(actionRow, "ConvertButton", "\u5f00\u59cb\u8f6c\u6362", delegate { });
        var convertRect = convertButton.GetComponent<RectTransform>();
        convertRect.anchorMin = new Vector2(0f, 0f);
        convertRect.anchorMax = new Vector2(0.62f, 1f);
        convertRect.offsetMin = Vector2.zero;
        convertRect.offsetMax = new Vector2(-6f, 0f);
        convertButton.GetComponent<Image>().color = new Color(0.42f, 0.22f, 0.12f, 0.94f);

        var closeButton = UIFactory.CreateButton(actionRow, "CloseButton", "\u5173\u95ed", delegate { });
        var closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.66f, 0f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.offsetMin = Vector2.zero;
        closeRect.offsetMax = Vector2.zero;

        var resultText = UIFactory.CreateText(panel.transform, "ResultText", "\u70b9\u51fb\u4efb\u610f\u4e00\u7cfb\u7075\u77f3\uff0c\u6309\u76f8\u751f\u89c4\u5219\u8fdb\u884c\u8f6c\u6362\u3002", 18, TextAnchor.MiddleCenter, new Color(0.92f, 0.84f, 0.72f, 0.9f));
        resultText.rectTransform.anchorMin = new Vector2(0.08f, 0.07f);
        resultText.rectTransform.anchorMax = new Vector2(0.92f, 0.12f);
        resultText.rectTransform.offsetMin = Vector2.zero;
        resultText.rectTransform.offsetMax = Vector2.zero;

        root.AddComponent<UISpiritStoneConvertPopup>();
        SavePrefab(root, PopupsFolder + "/SpiritStoneConvertPopup.prefab");
    }
    private static void BuildToastPopupPrefab()
    {
        var root = new GameObject("ToastPopup", typeof(RectTransform), typeof(CanvasGroup), typeof(UIToastPopup));
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.18f, 0.42f);
        rect.anchorMax = new Vector2(0.82f, 0.58f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var background = UIFactory.CreatePanel(root.transform, "Background", new Color(0.22f, 0.11f, 0.05f, 0.92f));
        UIFactory.AddOutlineBox(background.transform, "Outline", new Color(1f, 0.88f, 0.52f, 0.85f), 2f);
        var message = UIFactory.CreateText(background.transform, "Message", "Toast", 34, TextAnchor.MiddleCenter, new Color(1f, 0.96f, 0.85f, 1f));

        var toast = root.GetComponent<UIToastPopup>();
        BindSerializedProperty(toast, "messageText", message);

        SavePrefab(root, PopupsFolder + "/ToastPopup.prefab");
    }

    private static TeamInfoPanelRefs CreateTeamInfoPanel(
        Transform parent,
        string objectName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        string title,
        string content,
        string titleKey,
        string contentKey)
    {
        var panel = UIFactory.CreatePanel(parent, objectName, new Color(0.16f, 0.11f, 0.11f, 0.92f));
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = anchorMin;
        panelRect.anchorMax = anchorMax;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        UIFactory.AddOutlineBox(panel.transform, objectName + "Outline", new Color(0.86f, 0.78f, 0.72f, 0.7f), 1f);

        var titleRect = UIFactory.CreateContainer(panel.transform, "Title", new Vector2(0.04f, 0.89f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);
        var titleText = UIFactory.CreateText(titleRect, "TitleText", title, 26, TextAnchor.MiddleLeft, Color.white);
        AddLocalizedText(titleText, titleKey);

        var bodyRect = UIFactory.CreateContainer(panel.transform, "Body", new Vector2(0.02f, 0.14f), new Vector2(0.98f, 0.82f), Vector2.zero, Vector2.zero);
        var cardRoot = UIFactory.CreateContainer(bodyRect, "CardRoot", new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        var bodyText = UIFactory.CreateText(bodyRect, "BodyText", string.Empty, 20, TextAnchor.UpperLeft, new Color(0.92f, 0.92f, 0.92f, 0f));
        bodyText.raycastTarget = false;

        var cardTemplate = UIFactory.CreateButton(cardRoot, "CardTemplate", string.Empty, delegate { });
        var cardTemplateRect = cardTemplate.GetComponent<RectTransform>();
        cardTemplateRect.anchorMin = new Vector2(0f, 1f);
        cardTemplateRect.anchorMax = new Vector2(0f, 1f);
        cardTemplateRect.pivot = new Vector2(0f, 1f);
        cardTemplateRect.anchoredPosition = Vector2.zero;
        cardTemplateRect.sizeDelta = new Vector2(UICardChromeUtility.StandardCardWidth, UICardChromeUtility.StandardCardHeight);
        var cardTemplateLabel = cardTemplate.GetComponentInChildren<Text>();
        if (cardTemplateLabel != null)
        {
            cardTemplateLabel.text = string.Empty;
            cardTemplateLabel.gameObject.SetActive(false);
        }
        var cardTitleText = UIFactory.CreateText(cardTemplate.transform, "TitleText", "\u89d2\u8272", 20, TextAnchor.UpperLeft, new Color(0.96f, 0.92f, 0.84f, 1f));
        cardTitleText.rectTransform.anchorMin = new Vector2(0.08f, 0.72f);
        cardTitleText.rectTransform.anchorMax = new Vector2(0.92f, 0.9f);
        cardTitleText.rectTransform.offsetMin = Vector2.zero;
        cardTitleText.rectTransform.offsetMax = Vector2.zero;
        cardTitleText.supportRichText = true;
        var cardSubtitleText = UIFactory.CreateText(cardTemplate.transform, "SubtitleText", "Lv.1", 18, TextAnchor.UpperLeft, new Color(0.72f, 0.78f, 0.84f, 0.92f));
        cardSubtitleText.rectTransform.anchorMin = new Vector2(0.08f, 0.52f);
        cardSubtitleText.rectTransform.anchorMax = new Vector2(0.92f, 0.7f);
        cardSubtitleText.rectTransform.offsetMin = Vector2.zero;
        cardSubtitleText.rectTransform.offsetMax = Vector2.zero;
        var cardProgressRoot = UIFactory.CreatePanel(cardTemplate.transform, "ProgressRoot", new Color(1f, 1f, 1f, 0.03f));
        var cardProgressRect = cardProgressRoot.GetComponent<RectTransform>();
        cardProgressRect.anchorMin = new Vector2(0.08f, 0.12f);
        cardProgressRect.anchorMax = new Vector2(0.92f, 0.2f);
        cardProgressRect.offsetMin = Vector2.zero;
        cardProgressRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(cardProgressRoot.transform, "ProgressOutline", new Color(0.76f, 0.46f, 0.18f, 0.55f), 1f);
        var cardFill = UIFactory.CreatePanel(cardProgressRoot.transform, "Fill", new Color(0.9f, 0.9f, 0.9f, 0.9f));
        cardFill.GetComponent<Image>().type = Image.Type.Filled;
        cardFill.GetComponent<Image>().fillMethod = Image.FillMethod.Horizontal;
        cardFill.GetComponent<Image>().fillOrigin = 0;
        cardFill.GetComponent<Image>().fillAmount = 0.5f;
        var cardProgressLabel = UIFactory.CreateText(cardProgressRoot.transform, "ProgressLabel", "HP 0/0", 15, TextAnchor.MiddleCenter, new Color(0.96f, 0.96f, 0.96f, 1f));

        var equipmentTitleRect = UIFactory.CreateContainer(panel.transform, "EquipmentTitle", new Vector2(0.04f, 0.16f), new Vector2(0.96f, 0.22f), Vector2.zero, Vector2.zero);
        var equipmentTitleText = UIFactory.CreateText(equipmentTitleRect, "EquipmentTitleText", "Equipment", 20, TextAnchor.MiddleLeft, new Color(0.95f, 0.86f, 0.74f, 1f));
        AddLocalizedText(equipmentTitleText, "battle.equipment_title");
        equipmentTitleRect.gameObject.SetActive(false);

        var equipmentBodyRect = UIFactory.CreateContainer(panel.transform, "EquipmentBody", new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.16f), Vector2.zero, Vector2.zero);
        var equipmentBodyText = UIFactory.CreateText(equipmentBodyRect, "EquipmentBodyText", "No equipment", 16, TextAnchor.UpperLeft, new Color(0.82f, 0.84f, 0.86f, 1f));
        AddLocalizedText(equipmentBodyText, "battle.equipment_none");
        equipmentBodyRect.gameObject.SetActive(false);

        return new TeamInfoPanelRefs
        {
            BodyText = bodyText,
            CardRoot = cardRoot,
            CardTemplate = cardTemplate,
            EquipmentText = equipmentBodyText
        };
    }



    private static Button CreateSpiritStoneNode(Transform parent, string element, Vector2 position)
    {
        var buttonObject = new GameObject("Node" + element, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(164f, 172f);

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.01f);

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        var ring = UIFactory.CreatePanel(buttonObject.transform, "Ring", new Color(1f, 1f, 1f, 0.08f));
        var ringRect = ring.GetComponent<RectTransform>();
        ringRect.anchorMin = new Vector2(0.5f, 0.5f);
        ringRect.anchorMax = new Vector2(0.5f, 0.5f);
        ringRect.pivot = new Vector2(0.5f, 0.5f);
        ringRect.anchoredPosition = new Vector2(0f, 6f);
        ringRect.sizeDelta = new Vector2(108f, 108f);
        ringRect.localEulerAngles = new Vector3(0f, 0f, 45f);
        UIFactory.AddOutlineBox(ring.transform, "Outline", new Color(1f, 0.94f, 0.76f, 0.55f), 1f);

        var glow = UIFactory.CreatePanel(buttonObject.transform, "Glow", new Color(1f, 1f, 1f, 0.1f));
        var glowRect = glow.GetComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.anchoredPosition = new Vector2(0f, 6f);
        glowRect.sizeDelta = new Vector2(126f, 126f);
        glowRect.localEulerAngles = new Vector3(0f, 0f, 45f);

        var core = UIFactory.CreatePanel(buttonObject.transform, "Core", new Color(1f, 1f, 1f, 0.68f));
        var coreRect = core.GetComponent<RectTransform>();
        coreRect.anchorMin = new Vector2(0.5f, 0.5f);
        coreRect.anchorMax = new Vector2(0.5f, 0.5f);
        coreRect.pivot = new Vector2(0.5f, 0.5f);
        coreRect.anchoredPosition = new Vector2(0f, 6f);
        coreRect.sizeDelta = new Vector2(78f, 78f);
        coreRect.localEulerAngles = new Vector3(0f, 0f, 45f);
        UIFactory.AddOutlineBox(core.transform, "Outline", new Color(1f, 0.95f, 0.82f, 0.65f), 1f);

        var namePlate = UIFactory.CreatePanel(buttonObject.transform, "NamePlate", new Color(0.08f, 0.1f, 0.14f, 0.42f));
        var namePlateRect = namePlate.GetComponent<RectTransform>();
        namePlateRect.anchorMin = new Vector2(0.12f, 0.66f);
        namePlateRect.anchorMax = new Vector2(0.88f, 0.84f);
        namePlateRect.offsetMin = Vector2.zero;
        namePlateRect.offsetMax = Vector2.zero;

        var nameText = UIFactory.CreateText(buttonObject.transform, "NameText", element, 18, TextAnchor.MiddleCenter, new Color(0.95f, 0.96f, 0.98f, 0.96f));
        nameText.rectTransform.anchorMin = new Vector2(0.08f, 0.67f);
        nameText.rectTransform.anchorMax = new Vector2(0.92f, 0.84f);
        nameText.rectTransform.offsetMin = Vector2.zero;
        nameText.rectTransform.offsetMax = Vector2.zero;
        var nameOutline = nameText.gameObject.AddComponent<Outline>();
        nameOutline.effectColor = new Color(0f, 0f, 0f, 0.78f);
        nameOutline.effectDistance = new Vector2(1f, -1f);

        var countPlate = UIFactory.CreatePanel(buttonObject.transform, "CountPlate", new Color(0.12f, 0.09f, 0.08f, 0.4f));
        var countPlateRect = countPlate.GetComponent<RectTransform>();
        countPlateRect.anchorMin = new Vector2(0.18f, 0.06f);
        countPlateRect.anchorMax = new Vector2(0.82f, 0.24f);
        countPlateRect.offsetMin = Vector2.zero;
        countPlateRect.offsetMax = Vector2.zero;

        var countText = UIFactory.CreateText(buttonObject.transform, "CountText", "x0", 24, TextAnchor.MiddleCenter, new Color(1f, 0.96f, 0.86f, 1f));
        countText.rectTransform.anchorMin = new Vector2(0.16f, 0.06f);
        countText.rectTransform.anchorMax = new Vector2(0.84f, 0.24f);
        countText.rectTransform.offsetMin = Vector2.zero;
        countText.rectTransform.offsetMax = Vector2.zero;
        var countOutline = countText.gameObject.AddComponent<Outline>();
        countOutline.effectColor = new Color(0f, 0f, 0f, 0.82f);
        countOutline.effectDistance = new Vector2(1f, -1f);

        return button;
    }
    private static Image CreateSegment(Transform parent, string name, Vector2 start, Vector2 end, Color color, float thickness)
    {
        var lineObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        lineObject.transform.SetParent(parent, false);

        var rect = lineObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        var delta = end - start;
        rect.sizeDelta = new Vector2(delta.magnitude, thickness);
        rect.anchoredPosition = (start + end) * 0.5f;
        rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        var image = lineObject.GetComponent<Image>();
        image.color = color;
        return image;
    }
    private static void SetNodeRect(RectTransform rect, Vector2 anchor, float size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(size, size);
    }
    private sealed class TeamInfoPanelRefs
    {
        public Text BodyText;
        public RectTransform CardRoot;
        public Button CardTemplate;
        public Text EquipmentText;
    }

    private static LocalizedText AddLocalizedText(Text target, string key)
    {
        var localizedText = target.gameObject.AddComponent<LocalizedText>();
        localizedText.SetKey(key);
        return localizedText;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            EnsureFolder(directory.Replace("\\", "/"));
        }

        EnsureFolder(TempPrefabFolder);
        var tempPath = TempPrefabFolder + "/" + Path.GetFileName(path);
        var absoluteTargetPath = GetProjectAbsolutePath(path);
        var absoluteTempPath = GetProjectAbsolutePath(tempPath);

        if (AssetDatabase.LoadAssetAtPath<Object>(tempPath) != null)
        {
            AssetDatabase.DeleteAsset(tempPath);
        }

        PrefabUtility.SaveAsPrefabAsset(root, tempPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();

        var shouldReplace = !File.Exists(absoluteTargetPath) || !AreTextFilesEquivalent(absoluteTargetPath, absoluteTempPath);
        if (shouldReplace)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.MoveAsset(tempPath, path);
        }
        else
        {
            AssetDatabase.DeleteAsset(tempPath);
        }
    }

    private static bool AreTextFilesEquivalent(string leftPath, string rightPath)
    {
        if (!File.Exists(leftPath) || !File.Exists(rightPath))
        {
            return false;
        }

        var left = NormalizeText(File.ReadAllText(leftPath));
        var right = NormalizeText(File.ReadAllText(rightPath));
        return string.Equals(left, right, System.StringComparison.Ordinal);
    }

    private static string NormalizeText(string value)
    {
        return (value ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n");
    }

    private static bool AreAllGeneratedPrefabsPresent()
    {
        return File.Exists(GetProjectAbsolutePath(RootFolder + "/CanvasRoot.prefab"))
            && File.Exists(GetProjectAbsolutePath(PagesFolder + "/StartPage.prefab"))
            && File.Exists(GetProjectAbsolutePath(PagesFolder + "/MainMenuPage.prefab"))
            && File.Exists(GetProjectAbsolutePath(PagesFolder + "/MapPage.prefab"))
            && File.Exists(GetProjectAbsolutePath(PopupsFolder + "/ConfirmPopup.prefab"))
            && File.Exists(GetProjectAbsolutePath(PopupsFolder + "/CardBrowserPopup.prefab"))
            && File.Exists(GetProjectAbsolutePath(PopupsFolder + "/EquipmentPopup.prefab"))
            && File.Exists(GetProjectAbsolutePath(PopupsFolder + "/SkillPopup.prefab"))
            && File.Exists(GetProjectAbsolutePath(PopupsFolder + "/SpiritStoneConvertPopup.prefab"))
            && File.Exists(GetProjectAbsolutePath(PopupsFolder + "/ToastPopup.prefab"));
    }

    private static string ComputeBuildInputHash()
    {
        var builder = new StringBuilder();
        foreach (var path in GetTrackedInputFiles())
        {
            builder.Append(path).Append('\n');
            builder.Append(File.ReadAllText(GetProjectAbsolutePath(path))).Append('\n');
        }

        using (var sha = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(builder.ToString());
            var hash = sha.ComputeHash(bytes);
            return System.BitConverter.ToString(hash).Replace("-", string.Empty);
        }
    }

    private static IEnumerable<string> GetTrackedInputFiles()
    {
        var files = new List<string>();
        AddTrackedCsFiles(files, "Assets/Scripts/UI");
        AddTrackedCsFiles(files, "Assets/Scripts/Localization");
        files.Add("Assets/Editor/UIPrefabBuilder.cs");
        files.Add("Assets/Editor/UIEditorShortcuts.cs");
        files.Sort(System.StringComparer.OrdinalIgnoreCase);
        return files.Distinct(System.StringComparer.OrdinalIgnoreCase);
    }

    private static void AddTrackedCsFiles(List<string> files, string folder)
    {
        var absoluteFolder = GetProjectAbsolutePath(folder);
        if (!Directory.Exists(absoluteFolder))
        {
            return;
        }

        var folderFiles = Directory.GetFiles(absoluteFolder, "*.cs", SearchOption.AllDirectories);
        for (var i = 0; i < folderFiles.Length; i++)
        {
            files.Add(ToProjectRelativePath(folderFiles[i]));
        }
    }

    private static string ReadLastBuildHash()
    {
        var path = GetProjectAbsolutePath(BuildStatePath);
        return File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;
    }

    private static void WriteLastBuildHash(string hash)
    {
        var folder = GetProjectAbsolutePath(BuildStateFolder);
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        File.WriteAllText(GetProjectAbsolutePath(BuildStatePath), hash ?? string.Empty);
    }

    private static string GetProjectAbsolutePath(string relativePath)
    {
        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var normalized = relativePath.Replace("\\", "/");
        return Path.GetFullPath(Path.Combine(projectRoot, normalized));
    }

    private static string ToProjectRelativePath(string absolutePath)
    {
        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace("\\", "/");
        var normalized = Path.GetFullPath(absolutePath).Replace("\\", "/");
        if (normalized.StartsWith(projectRoot + "/", System.StringComparison.OrdinalIgnoreCase))
        {
            return normalized.Substring(projectRoot.Length + 1);
        }

        return normalized;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var parent = Path.GetDirectoryName(path).Replace("\\", "/");
        var name = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, name);
    }

    private static void BindSerializedProperty(Object target, string fieldName, Object reference)
    {
        var serializedObject = new SerializedObject(target);
        var property = serializedObject.FindProperty(fieldName);
        property.objectReferenceValue = reference;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}


























