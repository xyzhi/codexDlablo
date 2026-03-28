using UnityEditor;

public static class UIEditorShortcuts
{
    [MenuItem("工具/重建UI预设", priority = 2000)]
    public static void BuildUiPrefabsShortcut()
    {
        UIPrefabBuilder.BuildPrefabs();
    }
}
