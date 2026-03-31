using System.IO;
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

    public static void BuildPrefabs()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Prefabs");
        EnsureFolder(RootFolder);
        EnsureFolder(PagesFolder);
        EnsureFolder(PopupsFolder);

        BuildCanvasRootPrefab();
        BuildMainMenuPrefab();
        BuildMapPagePrefab();
        BuildBattlePagePrefab();
        BuildConfirmPopupPrefab();
        BuildToastPopupPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("已生成 UI 预设到 Assets/Resources/Prefabs/UI");
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
        scaler.matchWidthOrHeight = 1f;

        var pageLayer = UIFactory.CreateContainer(root.transform, "PageLayer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var popupLayer = UIFactory.CreateContainer(root.transform, "PopupLayer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var toastLayer = UIFactory.CreateContainer(root.transform, "ToastLayer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        root.GetComponent<UICanvasRoot>().Bind(pageLayer, popupLayer, toastLayer);
        SavePrefab(root, RootFolder + "/CanvasRoot.prefab");
    }

    private static void BuildMainMenuPrefab()
    {
        var root = UIFactory.CreatePanel(null, "MainMenuPage", new Color(0.09f, 0.1f, 0.14f, 1f));
        UIFactory.AddOutlineBox(root.transform, "ScreenFrame", new Color(0.8f, 0.82f, 0.86f, 0.85f), 2f);
        UIFactory.CreateLine(root.transform, "TopLine", new Color(0.7f, 0.74f, 0.8f, 1f), new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.88f), 2f);
        UIFactory.CreateLine(root.transform, "BottomLine", new Color(0.7f, 0.74f, 0.8f, 1f), new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.1f), 2f);

        var header = UIFactory.CreateContainer(root.transform, "Header", new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.88f), Vector2.zero, Vector2.zero);
        var title = UIFactory.CreateText(header, "Title", "Wuxing Prototype", 54, TextAnchor.MiddleCenter, Color.white);
        AddLocalizedText(title, "menu.title");

        var subtitle = UIFactory.CreateContainer(root.transform, "SubTitle", new Vector2(0.16f, 0.64f), new Vector2(0.84f, 0.72f), Vector2.zero, Vector2.zero);
        var subtitleText = UIFactory.CreateText(subtitle, "SubTitleText", "Phase 1 / Step 1 / Text and line UGUI", 24, TextAnchor.MiddleCenter, new Color(0.82f, 0.84f, 0.9f, 1f));
        AddLocalizedText(subtitleText, "menu.subtitle");

        var progressPanel = UIFactory.CreatePanel(root.transform, "ProgressPanel", new Color(0.13f, 0.14f, 0.18f, 0.92f));
        var progressPanelRect = progressPanel.GetComponent<RectTransform>();
        progressPanelRect.anchorMin = new Vector2(0.16f, 0.47f);
        progressPanelRect.anchorMax = new Vector2(0.84f, 0.63f);
        progressPanelRect.offsetMin = Vector2.zero;
        progressPanelRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(progressPanel.transform, "ProgressOutline", new Color(0.82f, 0.84f, 0.9f, 0.55f), 1f);
        var progressText = UIFactory.CreateText(progressPanel.transform, "ProgressText", "Current Stage 1 / Highest Cleared 0", 22, TextAnchor.UpperCenter, new Color(0.95f, 0.86f, 0.72f, 1f));
        progressText.rectTransform.offsetMin = new Vector2(20f, 14f);
        progressText.rectTransform.offsetMax = new Vector2(-20f, -14f);

        var menuPanel = UIFactory.CreatePanel(root.transform, "MenuPanel", new Color(0.13f, 0.14f, 0.18f, 0.92f));
        var menuRect = menuPanel.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.16f, 0.19f);
        menuRect.anchorMax = new Vector2(0.84f, 0.46f);
        menuRect.offsetMin = Vector2.zero;
        menuRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(menuPanel.transform, "MenuOutline", new Color(0.82f, 0.84f, 0.9f, 0.75f), 1f);

        var menu = UIFactory.CreateContainer(menuPanel.transform, "Menu", new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);
        var layout = UIFactory.AddVerticalLayout(menu.gameObject, 14, TextAnchor.UpperCenter);
        layout.padding = new RectOffset(0, 0, 8, 8);

        var startButton = UIFactory.CreateButton(menu, "StartButton", "Open Battle", delegate { });
        AddLocalizedText(startButton.GetComponentInChildren<Text>(), "menu.button_battle");
        UIFactory.AddLayoutElement(startButton.gameObject, 70f);

        var popupButton = UIFactory.CreateButton(menu, "PopupButton", "Open Popup", delegate { });
        AddLocalizedText(popupButton.GetComponentInChildren<Text>(), "menu.button_popup");
        UIFactory.AddLayoutElement(popupButton.gameObject, 70f);

        var toastButton = UIFactory.CreateButton(menu, "ToastButton", "Show Toast", delegate { });
        AddLocalizedText(toastButton.GetComponentInChildren<Text>(), "menu.button_toast");
        UIFactory.AddLayoutElement(toastButton.gameObject, 70f);

        var languageButton = UIFactory.CreateButton(menu, "LanguageButton", "Language", delegate { });
        AddLocalizedText(languageButton.GetComponentInChildren<Text>(), "menu.button_language");
        UIFactory.AddLayoutElement(languageButton.gameObject, 70f);

        var footer = UIFactory.CreateContainer(root.transform, "Footer", new Vector2(0.12f, 0.1f), new Vector2(0.88f, 0.18f), Vector2.zero, Vector2.zero);
        var footerText = UIFactory.CreateText(footer, "FooterText", "This version uses text, lines, and rectangles first so art can be replaced later.", 22, TextAnchor.MiddleCenter, new Color(0.72f, 0.76f, 0.82f, 1f));
        AddLocalizedText(footerText, "menu.footer");

        var page = root.AddComponent<UIMainMenuPage>();
        BindSerializedProperty(page, "startButton", startButton);
        BindSerializedProperty(page, "popupButton", popupButton);
        BindSerializedProperty(page, "toastButton", toastButton);
        BindSerializedProperty(page, "languageButton", languageButton);
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
        detailRect.anchorMin = new Vector2(0.08f, 0.29f);
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
        routeRect.anchorMin = new Vector2(0.52f, 0.29f);
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
        equipmentRect.anchorMax = new Vector2(0.31f, 1f);
        equipmentRect.offsetMin = Vector2.zero;
        equipmentRect.offsetMax = new Vector2(-8f, 0f);

        var resetButton = UIFactory.CreateButton(actionRow, "ResetButton", "Reset Run", delegate { });
        var resetRect = resetButton.GetComponent<RectTransform>();
        resetRect.anchorMin = new Vector2(0.345f, 0f);
        resetRect.anchorMax = new Vector2(0.655f, 1f);
        resetRect.offsetMin = Vector2.zero;
        resetRect.offsetMax = Vector2.zero;

        var backButton = UIFactory.CreateButton(actionRow, "BackButton", "Back To Menu", delegate { });
        var backRect = backButton.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.69f, 0f);
        backRect.anchorMax = new Vector2(1f, 1f);
        backRect.offsetMin = new Vector2(8f, 0f);
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
        BindSerializedProperty(page, "resetButton", resetButton);
        BindSerializedProperty(page, "backButton", backButton);
        SavePrefab(root, PagesFolder + "/MapPage.prefab");
    }

    private static void BuildBattlePagePrefab()
    {
        var root = UIFactory.CreatePanel(null, "BattlePage", new Color(0.12f, 0.08f, 0.08f, 1f));
        UIFactory.AddOutlineBox(root.transform, "BattleFrame", new Color(0.86f, 0.78f, 0.72f, 0.8f), 2f);

        var title = UIFactory.CreateContainer(root.transform, "Title", new Vector2(0.06f, 0.9f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);
        var titleText = UIFactory.CreateText(title, "Label", "Battle", 42, TextAnchor.MiddleCenter, Color.white);
        AddLocalizedText(titleText, "battle.title");

        var status = UIFactory.CreateContainer(root.transform, "Status", new Vector2(0.12f, 0.85f), new Vector2(0.88f, 0.9f), Vector2.zero, Vector2.zero);
        var statusText = UIFactory.CreateText(status, "StatusText", "Waiting", 24, TextAnchor.MiddleCenter, new Color(0.95f, 0.9f, 0.75f, 1f));
        AddLocalizedText(statusText, "battle.status_idle");

        var stageInfo = UIFactory.CreateContainer(root.transform, "StageInfo", new Vector2(0.2f, 0.81f), new Vector2(0.8f, 0.85f), Vector2.zero, Vector2.zero);
        var stageInfoText = UIFactory.CreateText(stageInfo, "StageInfoText", "Stage 1", 24, TextAnchor.MiddleCenter, new Color(0.82f, 0.88f, 0.96f, 1f));

        var overlay = UIFactory.CreatePanel(root.transform, "CombatOverlay", new Color(0.18f, 0.12f, 0.12f, 0.94f));
        var overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = new Vector2(0.03f, 0.03f);
        overlayRect.anchorMax = new Vector2(0.97f, 0.83f);
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(overlay.transform, "OverlayOutline", new Color(0.9f, 0.82f, 0.74f, 0.7f), 1f);

        var summaryBar = UIFactory.CreateContainer(overlay.transform, "SummaryBar", new Vector2(0.03f, 0.74f), new Vector2(0.97f, 0.96f), Vector2.zero, Vector2.zero);
        var playerPanel = CreateTeamInfoPanel(summaryBar, "PlayerTeam", new Vector2(0f, 0f), new Vector2(0.48f, 1f), "Player", "Unit A  HP 120/120\nUnit B  HP 150/150", "battle.player_team", "battle.player_content");
        var enemyPanel = CreateTeamInfoPanel(summaryBar, "EnemyTeam", new Vector2(0.52f, 0f), new Vector2(1f, 1f), "Enemy", "Enemy A  HP 100/100\nEnemy B  HP 90/90", "battle.enemy_team", "battle.enemy_content");

        var logPanel = UIFactory.CreatePanel(overlay.transform, "LogPanel", new Color(0.13f, 0.09f, 0.09f, 0.96f));
        var logPanelRect = logPanel.GetComponent<RectTransform>();
        logPanelRect.anchorMin = new Vector2(0.03f, 0.2f);
        logPanelRect.anchorMax = new Vector2(0.97f, 0.71f);
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

        var footer = UIFactory.CreateContainer(overlay.transform, "Footer", new Vector2(0.24f, 0.03f), new Vector2(0.76f, 0.1f), Vector2.zero, Vector2.zero);
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

        var controlsRoot = UIFactory.CreateContainer(equipmentContent.transform, "ControlsRoot", new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.86f), Vector2.zero, Vector2.zero);

        var cycleEquipmentButton = UIFactory.CreateButton(controlsRoot, "CycleEquipmentButton", "Switch Preset", delegate { });
        AddLocalizedText(cycleEquipmentButton.GetComponentInChildren<Text>(), "battle.button_cycle_equipment");
        var cycleEquipmentRect = cycleEquipmentButton.GetComponent<RectTransform>();
        cycleEquipmentRect.anchorMin = new Vector2(0f, 0.76f);
        cycleEquipmentRect.anchorMax = new Vector2(0.43f, 0.94f);
        cycleEquipmentRect.offsetMin = Vector2.zero;
        cycleEquipmentRect.offsetMax = Vector2.zero;

        var cycleEquipmentUnitButton = UIFactory.CreateButton(controlsRoot, "CycleEquipmentUnitButton", "Unit", delegate { });
        var cycleEquipmentUnitRect = cycleEquipmentUnitButton.GetComponent<RectTransform>();
        cycleEquipmentUnitRect.anchorMin = new Vector2(0.47f, 0.76f);
        cycleEquipmentUnitRect.anchorMax = new Vector2(1f, 0.94f);
        cycleEquipmentUnitRect.offsetMin = Vector2.zero;
        cycleEquipmentUnitRect.offsetMax = Vector2.zero;

        var cycleWeaponButton = UIFactory.CreateButton(controlsRoot, "CycleWeaponButton", "Weapon", delegate { });
        var cycleWeaponRect = cycleWeaponButton.GetComponent<RectTransform>();
        cycleWeaponRect.anchorMin = new Vector2(0f, 0.5f);
        cycleWeaponRect.anchorMax = new Vector2(0.3f, 0.7f);
        cycleWeaponRect.offsetMin = Vector2.zero;
        cycleWeaponRect.offsetMax = Vector2.zero;

        var cycleArmorButton = UIFactory.CreateButton(controlsRoot, "CycleArmorButton", "Armor", delegate { });
        var cycleArmorRect = cycleArmorButton.GetComponent<RectTransform>();
        cycleArmorRect.anchorMin = new Vector2(0.35f, 0.5f);
        cycleArmorRect.anchorMax = new Vector2(0.65f, 0.7f);
        cycleArmorRect.offsetMin = Vector2.zero;
        cycleArmorRect.offsetMax = Vector2.zero;

        var cycleAccessoryButton = UIFactory.CreateButton(controlsRoot, "CycleAccessoryButton", "Accessory", delegate { });
        var cycleAccessoryRect = cycleAccessoryButton.GetComponent<RectTransform>();
        cycleAccessoryRect.anchorMin = new Vector2(0.7f, 0.5f);
        cycleAccessoryRect.anchorMax = new Vector2(1f, 0.7f);
        cycleAccessoryRect.offsetMin = Vector2.zero;
        cycleAccessoryRect.offsetMax = Vector2.zero;

        var autoOffenseButton = UIFactory.CreateButton(controlsRoot, "AutoOffenseButton", "Auto Offense", delegate { });
        var autoOffenseRect = autoOffenseButton.GetComponent<RectTransform>();
        autoOffenseRect.anchorMin = new Vector2(0f, 0.24f);
        autoOffenseRect.anchorMax = new Vector2(0.47f, 0.44f);
        autoOffenseRect.offsetMin = Vector2.zero;
        autoOffenseRect.offsetMax = Vector2.zero;

        var autoDefenseButton = UIFactory.CreateButton(controlsRoot, "AutoDefenseButton", "Auto Defense", delegate { });
        var autoDefenseRect = autoDefenseButton.GetComponent<RectTransform>();
        autoDefenseRect.anchorMin = new Vector2(0.53f, 0.24f);
        autoDefenseRect.anchorMax = new Vector2(1f, 0.44f);
        autoDefenseRect.offsetMin = Vector2.zero;
        autoDefenseRect.offsetMax = Vector2.zero;

        var resetEquipmentButton = UIFactory.CreateButton(controlsRoot, "ResetEquipmentButton", "Reset", delegate { });
        var resetEquipmentRect = resetEquipmentButton.GetComponent<RectTransform>();
        resetEquipmentRect.anchorMin = new Vector2(0f, 0f);
        resetEquipmentRect.anchorMax = new Vector2(1f, 0.18f);
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
        var selectionTitleText = UIFactory.CreateText(selectionTitle, "SelectionTitleText", "武器列表", 22, TextAnchor.MiddleLeft, new Color(0.95f, 0.86f, 0.74f, 1f));

        var selectionScrollRoot = UIFactory.CreateContainer(equipmentContent.transform, "SelectionScrollRoot", new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.47f), Vector2.zero, Vector2.zero);
        var selectionScrollRect = UIFactory.CreateScrollRect(selectionScrollRoot, "SelectionScroll", new Color(0f, 0f, 0f, 0.18f));
        UIFactory.Stretch(selectionScrollRect.GetComponent<RectTransform>());

        var equipmentScrollRoot = UIFactory.CreateContainer(equipmentContent.transform, "EquipmentScrollRoot", new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.25f), Vector2.zero, Vector2.zero);
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
        panelRect.anchorMin = new Vector2(0.12f, 0.32f);
        panelRect.anchorMax = new Vector2(0.88f, 0.68f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(panel.transform, "Outline", new Color(0.82f, 0.84f, 0.9f, 0.75f), 1f);

        var title = UIFactory.CreateText(panel.transform, "Title", "Confirm", 40, TextAnchor.UpperCenter, Color.white);
        title.rectTransform.anchorMin = new Vector2(0.08f, 0.72f);
        title.rectTransform.anchorMax = new Vector2(0.92f, 0.92f);
        title.rectTransform.offsetMin = Vector2.zero;
        title.rectTransform.offsetMax = Vector2.zero;

        var message = UIFactory.CreateText(panel.transform, "Message", "Message", 28, TextAnchor.MiddleCenter, new Color(0.93f, 0.93f, 0.93f, 1f));
        message.rectTransform.anchorMin = new Vector2(0.08f, 0.32f);
        message.rectTransform.anchorMax = new Vector2(0.92f, 0.68f);
        message.rectTransform.offsetMin = Vector2.zero;
        message.rectTransform.offsetMax = Vector2.zero;

        var confirmButton = UIFactory.CreateButton(panel.transform, "ConfirmButton", "Confirm", delegate { });
        AddLocalizedText(confirmButton.GetComponentInChildren<Text>(), "popup.confirm_button");
        var confirmRect = confirmButton.GetComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.1f, 0.08f);
        confirmRect.anchorMax = new Vector2(0.44f, 0.24f);
        confirmRect.offsetMin = Vector2.zero;
        confirmRect.offsetMax = Vector2.zero;

        var cancelButton = UIFactory.CreateButton(panel.transform, "CancelButton", "Cancel", delegate { });
        AddLocalizedText(cancelButton.GetComponentInChildren<Text>(), "popup.cancel_button");
        var cancelRect = cancelButton.GetComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0.56f, 0.08f);
        cancelRect.anchorMax = new Vector2(0.9f, 0.24f);
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

        var titleRect = UIFactory.CreateContainer(panel.transform, "Title", new Vector2(0.04f, 0.78f), new Vector2(0.96f, 0.94f), Vector2.zero, Vector2.zero);
        var titleText = UIFactory.CreateText(titleRect, "TitleText", title, 26, TextAnchor.MiddleLeft, Color.white);
        AddLocalizedText(titleText, titleKey);

        var bodyRect = UIFactory.CreateContainer(panel.transform, "Body", new Vector2(0.04f, 0.42f), new Vector2(0.96f, 0.76f), Vector2.zero, Vector2.zero);
        var bodyText = UIFactory.CreateText(bodyRect, "BodyText", content, 20, TextAnchor.UpperLeft, new Color(0.92f, 0.92f, 0.92f, 1f));
        AddLocalizedText(bodyText, contentKey);

        var equipmentTitleRect = UIFactory.CreateContainer(panel.transform, "EquipmentTitle", new Vector2(0.04f, 0.26f), new Vector2(0.96f, 0.4f), Vector2.zero, Vector2.zero);
        var equipmentTitleText = UIFactory.CreateText(equipmentTitleRect, "EquipmentTitleText", "Equipment", 20, TextAnchor.MiddleLeft, new Color(0.95f, 0.86f, 0.74f, 1f));
        AddLocalizedText(equipmentTitleText, "battle.equipment_title");

        var equipmentBodyRect = UIFactory.CreateContainer(panel.transform, "EquipmentBody", new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.26f), Vector2.zero, Vector2.zero);
        var equipmentBodyText = UIFactory.CreateText(equipmentBodyRect, "EquipmentBodyText", "No equipment", 16, TextAnchor.UpperLeft, new Color(0.82f, 0.84f, 0.86f, 1f));
        AddLocalizedText(equipmentBodyText, "battle.equipment_none");

        return new TeamInfoPanelRefs
        {
            BodyText = bodyText,
            EquipmentText = equipmentBodyText
        };
    }

    private sealed class TeamInfoPanelRefs
    {
        public Text BodyText;
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

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
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



