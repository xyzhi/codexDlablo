using UnityEditor;

public static class UIEditorShortcuts
{
    [MenuItem("工具/重建UI预设", priority = 2000)]
    public static void BuildUiPrefabsShortcut()
    {
        UIPrefabBuilder.BuildPrefabs();
    }

    [MenuItem("工具/强制重建UI预设", priority = 2001)]
    public static void ForceBuildUiPrefabsShortcut()
    {
        UIPrefabBuilder.BuildPrefabs(true);
    }
}

