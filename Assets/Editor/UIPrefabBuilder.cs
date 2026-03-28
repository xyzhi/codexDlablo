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
        scaler.matchWidthOrHeight = 0.5f;

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
        var title = UIFactory.CreateText(header, "Title", "五行原型", 54, TextAnchor.MiddleCenter, Color.white);
        AddLocalizedText(title, "menu.title");

        var subTitle = UIFactory.CreateContainer(root.transform, "SubTitle", new Vector2(0.16f, 0.64f), new Vector2(0.84f, 0.72f), Vector2.zero, Vector2.zero);
        var subTitleText = UIFactory.CreateText(subTitle, "SubTitleText", "第一阶段 / 第一步 / 文字与线框 UGUI", 24, TextAnchor.MiddleCenter, new Color(0.82f, 0.84f, 0.9f, 1f));
        AddLocalizedText(subTitleText, "menu.subtitle");

        var menuPanel = UIFactory.CreatePanel(root.transform, "MenuPanel", new Color(0.13f, 0.14f, 0.18f, 0.92f));
        var menuRect = menuPanel.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.16f, 0.22f);
        menuRect.anchorMax = new Vector2(0.84f, 0.58f);
        menuRect.offsetMin = Vector2.zero;
        menuRect.offsetMax = Vector2.zero;
        UIFactory.AddOutlineBox(menuPanel.transform, "MenuOutline", new Color(0.82f, 0.84f, 0.9f, 0.75f), 1f);

        var menu = UIFactory.CreateContainer(menuPanel.transform, "Menu", new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);
        var layout = UIFactory.AddVerticalLayout(menu.gameObject, 18, TextAnchor.UpperCenter);
        layout.padding = new RectOffset(0, 0, 8, 8);

        var startButton = UIFactory.CreateButton(menu, "StartButton", "进入战斗页", delegate { });
        AddLocalizedText(startButton.GetComponentInChildren<Text>(), "menu.button_battle");
        UIFactory.AddLayoutElement(startButton.gameObject, 86f);

        var popupButton = UIFactory.CreateButton(menu, "PopupButton", "打开确认弹窗", delegate { });
        AddLocalizedText(popupButton.GetComponentInChildren<Text>(), "menu.button_popup");
        UIFactory.AddLayoutElement(popupButton.gameObject, 86f);

        var toastButton = UIFactory.CreateButton(menu, "ToastButton", "显示提示", delegate { });
        AddLocalizedText(toastButton.GetComponentInChildren<Text>(), "menu.button_toast");
        UIFactory.AddLayoutElement(toastButton.gameObject, 86f);

        var languageButton = UIFactory.CreateButton(menu, "LanguageButton", "切换语言", delegate { });
        AddLocalizedText(languageButton.GetComponentInChildren<Text>(), "menu.button_language");
        UIFactory.AddLayoutElement(languageButton.gameObject, 86f);

        var footer = UIFactory.CreateContainer(root.transform, "Footer", new Vector2(0.12f, 0.1f), new Vector2(0.88f, 0.18f), Vector2.zero, Vector2.zero);
        var footerText = UIFactory.CreateText(footer, "FooterText", "这一版先只用文字、线条和矩形框，方便后续替换美术。", 22, TextAnchor.MiddleCenter, new Color(0.72f, 0.76f, 0.82f, 1f));
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

        var title = UIFactory.CreateContainer(root.transform, "Title", new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);
        var titleText = UIFactory.CreateText(title, "Label", "战斗页占位", 46, TextAnchor.MiddleCenter, Color.white);
        AddLocalizedText(titleText, "battle.title");

        var status = UIFactory.CreateContainer(root.transform, "Status", new Vector2(0.2f, 0.78f), new Vector2(0.8f, 0.84f), Vector2.zero, Vector2.zero);
        var statusText = UIFactory.CreateText(status, "StatusText", "等待开始战斗", 24, TextAnchor.MiddleCenter, new Color(0.95f, 0.9f, 0.75f, 1f));
        AddLocalizedText(statusText, "battle.status_idle");

        var playerBodyText = CreateInfoBox(root.transform, "PlayerTeam", new Vector2(0.08f, 0.44f), new Vector2(0.44f, 0.76f), "我方队伍", "角色甲  HP 120/120\n角色乙  HP 150/150", "battle.player_team", "battle.player_content");
        var enemyBodyText = CreateInfoBox(root.transform, "EnemyTeam", new Vector2(0.56f, 0.44f), new Vector2(0.92f, 0.76f), "敌方队伍", "敌人甲  HP 100/100\n敌人乙  HP 90/90", "battle.enemy_team", "battle.enemy_content");
        var logBodyText = CreateInfoBox(root.transform, "BattleLog", new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.38f), "战斗日志", "下一步这里会接真实战斗日志。", "battle.log_title", "battle.log_content");

        var footer = UIFactory.CreateContainer(root.transform, "Footer", new Vector2(0.14f, 0.06f), new Vector2(0.86f, 0.16f), Vector2.zero, Vector2.zero);
        var layout = UIFactory.AddVerticalLayout(footer.gameObject, 12, TextAnchor.MiddleCenter);
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = true;

        var startButton = UIFactory.CreateButton(footer, "StartButton", "开始战斗", delegate { });
        AddLocalizedText(startButton.GetComponentInChildren<Text>(), "battle.button_start");
        UIFactory.AddLayoutElement(startButton.gameObject, 72f);

        var backButton = UIFactory.CreateButton(footer, "BackButton", "返回主菜单", delegate { });
        AddLocalizedText(backButton.GetComponentInChildren<Text>(), "battle.button_back");
        UIFactory.AddLayoutElement(backButton.gameObject, 72f);

        var tipButton = UIFactory.CreateButton(footer, "TipButton", "显示提示", delegate { });
        AddLocalizedText(tipButton.GetComponentInChildren<Text>(), "battle.button_tip");
        UIFactory.AddLayoutElement(tipButton.gameObject, 72f);

        var page = root.AddComponent<UIBattlePage>();
        BindSerializedProperty(page, "backButton", backButton);
        BindSerializedProperty(page, "tipButton", tipButton);
        BindSerializedProperty(page, "startBattleButton", startButton);
        BindSerializedProperty(page, "statusText", statusText);
        BindSerializedProperty(page, "playerTeamText", playerBodyText);
        BindSerializedProperty(page, "enemyTeamText", enemyBodyText);
        BindSerializedProperty(page, "battleLogText", logBodyText);

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

        var title = UIFactory.CreateText(panel.transform, "Title", "确认", 40, TextAnchor.UpperCenter, Color.white);
        title.rectTransform.anchorMin = new Vector2(0.08f, 0.72f);
        title.rectTransform.anchorMax = new Vector2(0.92f, 0.92f);
        title.rectTransform.offsetMin = Vector2.zero;
        title.rectTransform.offsetMax = Vector2.zero;

        var message = UIFactory.CreateText(panel.transform, "Message", "消息", 28, TextAnchor.MiddleCenter, new Color(0.93f, 0.93f, 0.93f, 1f));
        message.rectTransform.anchorMin = new Vector2(0.08f, 0.32f);
        message.rectTransform.anchorMax = new Vector2(0.92f, 0.68f);
        message.rectTransform.offsetMin = Vector2.zero;
        message.rectTransform.offsetMax = Vector2.zero;

        var confirmButton = UIFactory.CreateButton(panel.transform, "ConfirmButton", "确认", delegate { });
        AddLocalizedText(confirmButton.GetComponentInChildren<Text>(), "popup.confirm_button");
        var confirmRect = confirmButton.GetComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.1f, 0.08f);
        confirmRect.anchorMax = new Vector2(0.44f, 0.24f);
        confirmRect.offsetMin = Vector2.zero;
        confirmRect.offsetMax = Vector2.zero;

        var cancelButton = UIFactory.CreateButton(panel.transform, "CancelButton", "取消", delegate { });
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
        var message = UIFactory.CreateText(background.transform, "Message", "提示", 26, TextAnchor.MiddleCenter, Color.white);

        var toast = root.GetComponent<UIToastPopup>();
        BindSerializedProperty(toast, "messageText", message);

        SavePrefab(root, PopupsFolder + "/ToastPopup.prefab");
    }

    private static Text CreateInfoBox(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, string title, string content, string titleKey, string contentKey)
    {
        var panel = UIFactory.CreatePanel(parent, objectName, new Color(0.16f, 0.11f, 0.11f, 0.92f));
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = anchorMin;
        panelRect.anchorMax = anchorMax;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        UIFactory.AddOutlineBox(panel.transform, objectName + "Outline", new Color(0.86f, 0.78f, 0.72f, 0.7f), 1f);

        var titleRect = UIFactory.CreateContainer(panel.transform, "Title", new Vector2(0.04f, 0.74f), new Vector2(0.96f, 0.94f), Vector2.zero, Vector2.zero);
        var titleText = UIFactory.CreateText(titleRect, "TitleText", title, 28, TextAnchor.MiddleLeft, Color.white);
        AddLocalizedText(titleText, titleKey);

        var bodyRect = UIFactory.CreateContainer(panel.transform, "Body", new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.72f), Vector2.zero, Vector2.zero);
        var bodyText = UIFactory.CreateText(bodyRect, "BodyText", content, 24, TextAnchor.UpperLeft, new Color(0.92f, 0.92f, 0.92f, 1f));
        AddLocalizedText(bodyText, contentKey);
        return bodyText;
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
