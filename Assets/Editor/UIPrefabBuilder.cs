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

    [MenuItem("工具/UI/生成UI预设")]
    public static void BuildPrefabs()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Prefabs");
        EnsureFolder(RootFolder);
        EnsureFolder(PagesFolder);
        EnsureFolder(PopupsFolder);

        BuildCanvasRootPrefab();
        BuildMainMenuPrefab();
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

        var menuPanel = UIFactory.CreatePanel(root.transform, "MenuPanel", new Color(0.13f, 0.14f, 0.18f, 0.92f));
        var menuRect = menuPanel.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.16f, 0.18f);
        menuRect.anchorMax = new Vector2(0.84f, 0.62f);
        menuRect.offsetMin = Vector2.zero;
        menuRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(menuPanel.transform, "MenuOutline", new Color(0.82f, 0.84f, 0.9f, 0.75f), 1f);

        var menu = UIFactory.CreateContainer(menuPanel.transform, "Menu", new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);
        var layout = UIFactory.AddVerticalLayout(menu.gameObject, 18, TextAnchor.UpperCenter);
        layout.padding = new RectOffset(0, 0, 8, 8);

        var startButton = UIFactory.CreateButton(menu, "StartButton", "Open Battle", delegate { });
        AddLocalizedText(startButton.GetComponentInChildren<Text>(), "menu.button_battle");
        UIFactory.AddLayoutElement(startButton.gameObject, 86f);

        var popupButton = UIFactory.CreateButton(menu, "PopupButton", "Open Popup", delegate { });
        AddLocalizedText(popupButton.GetComponentInChildren<Text>(), "menu.button_popup");
        UIFactory.AddLayoutElement(popupButton.gameObject, 86f);

        var toastButton = UIFactory.CreateButton(menu, "ToastButton", "Show Toast", delegate { });
        AddLocalizedText(toastButton.GetComponentInChildren<Text>(), "menu.button_toast");
        UIFactory.AddLayoutElement(toastButton.gameObject, 86f);

        var languageButton = UIFactory.CreateButton(menu, "LanguageButton", "Language", delegate { });
        AddLocalizedText(languageButton.GetComponentInChildren<Text>(), "menu.button_language");
        UIFactory.AddLayoutElement(languageButton.gameObject, 86f);

        var footer = UIFactory.CreateContainer(root.transform, "Footer", new Vector2(0.12f, 0.1f), new Vector2(0.88f, 0.18f), Vector2.zero, Vector2.zero);
        var footerText = UIFactory.CreateText(footer, "FooterText", "This version uses text, lines, and rectangles first so art can be replaced later.", 22, TextAnchor.MiddleCenter, new Color(0.72f, 0.76f, 0.82f, 1f));
        AddLocalizedText(footerText, "menu.footer");

        var page = root.AddComponent<UIMainMenuPage>();
        BindSerializedProperty(page, "startButton", startButton);
        BindSerializedProperty(page, "popupButton", popupButton);
        BindSerializedProperty(page, "toastButton", toastButton);
        BindSerializedProperty(page, "languageButton", languageButton);
        SavePrefab(root, PagesFolder + "/MainMenuPage.prefab");
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

        var footer = UIFactory.CreateContainer(overlay.transform, "Footer", new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.18f), Vector2.zero, Vector2.zero);
        var footerLayout = UIFactory.AddVerticalLayout(footer.gameObject, 6, TextAnchor.MiddleCenter);
        footerLayout.childForceExpandHeight = true;
        footerLayout.childForceExpandWidth = true;

        var startButton = UIFactory.CreateButton(footer, "StartButton", "Start", delegate { });
        AddLocalizedText(startButton.GetComponentInChildren<Text>(), "battle.button_start");
        UIFactory.AddLayoutElement(startButton.gameObject, 38f);

        var backButton = UIFactory.CreateButton(footer, "BackButton", "Back", delegate { });
        AddLocalizedText(backButton.GetComponentInChildren<Text>(), "battle.button_back");
        UIFactory.AddLayoutElement(backButton.gameObject, 38f);

        var tipButton = UIFactory.CreateButton(footer, "TipButton", "Tip", delegate { });
        AddLocalizedText(tipButton.GetComponentInChildren<Text>(), "battle.button_tip");
        UIFactory.AddLayoutElement(tipButton.gameObject, 38f);

        var closeLogButton = UIFactory.CreateButton(footer, "CloseLogButton", "Close Log", delegate { });
        AddLocalizedText(closeLogButton.GetComponentInChildren<Text>(), "battle.button_close_log");
        UIFactory.AddLayoutElement(closeLogButton.gameObject, 38f);

        var equipmentButton = UIFactory.CreateButton(footer, "EquipmentButton", "Equipment", delegate { });
        AddLocalizedText(equipmentButton.GetComponentInChildren<Text>(), "battle.button_equipment");
        UIFactory.AddLayoutElement(equipmentButton.gameObject, 38f);

        var equipmentPanel = UIFactory.CreatePanel(overlay.transform, "EquipmentPanel", new Color(0.1f, 0.07f, 0.07f, 0.98f));
        var equipmentPanelRect = equipmentPanel.GetComponent<RectTransform>();
        equipmentPanelRect.anchorMin = new Vector2(0.06f, 0.16f);
        equipmentPanelRect.anchorMax = new Vector2(0.94f, 0.84f);
        equipmentPanelRect.offsetMin = Vector2.zero;
        equipmentPanelRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(equipmentPanel.transform, "EquipmentOutline", new Color(0.9f, 0.82f, 0.74f, 0.75f), 1f);

        var equipmentTitle = UIFactory.CreateContainer(equipmentPanel.transform, "EquipmentTitle", new Vector2(0.05f, 0.88f), new Vector2(0.75f, 0.97f), Vector2.zero, Vector2.zero);
        var equipmentTitleText = UIFactory.CreateText(equipmentTitle, "EquipmentTitleText", "Equipment Detail", 30, TextAnchor.MiddleLeft, Color.white);
        AddLocalizedText(equipmentTitleText, "battle.equipment_detail_title");

        var cycleEquipmentButton = UIFactory.CreateButton(equipmentPanel.transform, "CycleEquipmentButton", "Switch Preset", delegate { });
        AddLocalizedText(cycleEquipmentButton.GetComponentInChildren<Text>(), "battle.button_cycle_equipment");
        var cycleEquipmentRect = cycleEquipmentButton.GetComponent<RectTransform>();
        cycleEquipmentRect.anchorMin = new Vector2(0.05f, 0.78f);
        cycleEquipmentRect.anchorMax = new Vector2(0.45f, 0.87f);
        cycleEquipmentRect.offsetMin = Vector2.zero;
        cycleEquipmentRect.offsetMax = Vector2.zero;

        var closeEquipmentButton = UIFactory.CreateButton(equipmentPanel.transform, "CloseEquipmentButton", "Close", delegate { });
        AddLocalizedText(closeEquipmentButton.GetComponentInChildren<Text>(), "battle.button_close_equipment");
        var closeEquipmentRect = closeEquipmentButton.GetComponent<RectTransform>();
        closeEquipmentRect.anchorMin = new Vector2(0.72f, 0.88f);
        closeEquipmentRect.anchorMax = new Vector2(0.95f, 0.97f);
        closeEquipmentRect.offsetMin = Vector2.zero;
        closeEquipmentRect.offsetMax = Vector2.zero;

        var equipmentDetailRect = UIFactory.CreateContainer(equipmentPanel.transform, "EquipmentDetail", new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.76f), Vector2.zero, Vector2.zero);
        var equipmentDetailText = UIFactory.CreateText(equipmentDetailRect, "EquipmentDetailText", "Equipment detail", 22, TextAnchor.UpperLeft, new Color(0.92f, 0.92f, 0.92f, 1f));
        equipmentPanel.SetActive(false);

        var page = root.AddComponent<UIBattlePage>();
        BindSerializedProperty(page, "battleLogOverlay", overlay);
        BindSerializedProperty(page, "backButton", backButton);
        BindSerializedProperty(page, "tipButton", tipButton);
        BindSerializedProperty(page, "startBattleButton", startButton);
        BindSerializedProperty(page, "closeLogButton", closeLogButton);
        BindSerializedProperty(page, "equipmentButton", equipmentButton);
        BindSerializedProperty(page, "closeEquipmentButton", closeEquipmentButton);
        BindSerializedProperty(page, "cycleEquipmentPresetButton", cycleEquipmentButton);
        BindSerializedProperty(page, "equipmentPanel", equipmentPanel);
        BindSerializedProperty(page, "equipmentDetailText", equipmentDetailText);
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
        rect.anchorMin = new Vector2(0.2f, 0.08f);
        rect.anchorMax = new Vector2(0.8f, 0.16f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var background = UIFactory.CreatePanel(root.transform, "Background", new Color(0f, 0f, 0f, 0.75f));
        UIFactory.AddOutlineBox(background.transform, "Outline", new Color(0.9f, 0.9f, 0.9f, 0.6f), 1f);
        var message = UIFactory.CreateText(background.transform, "Message", "Toast", 26, TextAnchor.MiddleCenter, Color.white);

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
