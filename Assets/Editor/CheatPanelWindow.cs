using UnityEditor;
using UnityEngine;
using Wuxing.Config;
using Wuxing.Game;

public class CheatPanelWindow : EditorWindow
{
    private readonly string[] spiritStoneElements = { "金", "木", "水", "火", "土" };
    private readonly string[] spiritStoneElementIds = { "Metal", "Wood", "Water", "Fire", "Earth" };

    private int spiritStoneElementIndex;
    private int spiritStoneAmount = 100;
    private int cultivationLevel = 10;
    private int targetStage = 3;
    private int selectedEquipmentIndex;
    private int selectedCharacterIndex;
    private int selectedSkillIndex;
    private int skillLevel = 1;
    private Vector2 scrollPosition;

    [MenuItem("工具/调试/作弊面板", priority = 2100)]
    private static void Open()
    {
        var window = GetWindow<CheatPanelWindow>("作弊面板");
        window.minSize = new Vector2(420f, 620f);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.LabelField("运行时作弊面板", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("进入 Play 模式后可直接改当前局数据。建议只在本地调试使用。", MessageType.Info);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("当前不在 Play 模式。请先运行游戏，再使用下面的功能。", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        DrawRunSummary();
        EditorGUILayout.Space(8f);
        DrawRunActions();
        EditorGUILayout.Space(8f);
        DrawSpiritStoneTools();
        EditorGUILayout.Space(8f);
        DrawCultivationTools();
        EditorGUILayout.Space(8f);
        DrawEquipmentTools();
        EditorGUILayout.Space(8f);
        DrawSkillTools();
        EditorGUILayout.EndScrollView();
    }

    private static void DrawRunSummary()
    {
        var runData = GameProgressManager.GetRunDataSnapshot();
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("当前局概览", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("当前关卡", runData.CurrentStage.ToString());
        EditorGUILayout.LabelField("最高已通关", runData.HighestClearedStage.ToString());
        EditorGUILayout.LabelField("修为等级", runData.CultivationLevel.ToString());
        EditorGUILayout.LabelField("修为经验", runData.CultivationExp + " / " + GameProgressManager.GetRequiredExpForNextLevel());
        EditorGUILayout.LabelField("灵石总量", runData.SpiritStones.ToString());
        EditorGUILayout.LabelField("已拥有装备", runData.OwnedEquipmentIds.Count.ToString());
        EditorGUILayout.EndVertical();
    }

    private static void DrawRunActions()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("局内控制", EditorStyles.boldLabel);

        if (!GameProgressManager.HasActiveRun())
        {
            if (GUILayout.Button("开始新的一局"))
            {
                GameProgressManager.StartRun();
            }
        }
        else if (GUILayout.Button("重置当前局"))
        {
            GameProgressManager.ResetRun();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSpiritStoneTools()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("灵石", EditorStyles.boldLabel);
        spiritStoneElementIndex = EditorGUILayout.Popup("五行类型", spiritStoneElementIndex, spiritStoneElements);
        spiritStoneAmount = EditorGUILayout.IntField("增加数量", spiritStoneAmount);

        if (GUILayout.Button("直接获得灵石"))
        {
            GameProgressManager.DebugGrantSpiritStones(
                spiritStoneElementIds[Mathf.Clamp(spiritStoneElementIndex, 0, spiritStoneElementIds.Length - 1)],
                spiritStoneAmount);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("五系各+500"))
        {
            GrantAllSpiritStoneElements(500);
        }
        if (GUILayout.Button("五系各+5000"))
        {
            GrantAllSpiritStoneElements(5000);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawCultivationTools()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("等级与关卡", EditorStyles.boldLabel);
        cultivationLevel = EditorGUILayout.IntField("目标等级", cultivationLevel);
        if (GUILayout.Button("直接设置等级"))
        {
            GameProgressManager.DebugSetCultivationLevel(cultivationLevel);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("等级 10"))
        {
            GameProgressManager.DebugSetCultivationLevel(10);
        }
        if (GUILayout.Button("等级 30"))
        {
            GameProgressManager.DebugSetCultivationLevel(30);
        }
        if (GUILayout.Button("等级 99"))
        {
            GameProgressManager.DebugSetCultivationLevel(99);
        }
        EditorGUILayout.EndHorizontal();

        targetStage = EditorGUILayout.IntField("目标关卡", targetStage);
        if (GUILayout.Button("跳到该关卡"))
        {
            GameProgressManager.DebugJumpToStage(targetStage);
        }

        if (GUILayout.Button("跳到最后一关"))
        {
            GameProgressManager.DebugJumpToStage(GameProgressManager.GetMaxStage());
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawEquipmentTools()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("装备", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("这里是把装备直接发到当前局背包里，不会自动穿上。拿到后去战斗页或装备页切换即可。", MessageType.None);

        var equipmentDatabase = EquipmentDatabaseLoader.Load();
        var equipmentNames = BuildEquipmentOptions(equipmentDatabase);
        selectedEquipmentIndex = Mathf.Clamp(selectedEquipmentIndex, 0, Mathf.Max(0, equipmentNames.Length - 1));
        selectedEquipmentIndex = EditorGUILayout.Popup("选择装备", selectedEquipmentIndex, equipmentNames);

        using (new EditorGUI.DisabledScope(equipmentDatabase == null || equipmentDatabase.Equipments.Count == 0))
        {
            if (GUILayout.Button("直接获得这件装备"))
            {
                var equipment = equipmentDatabase.Equipments[selectedEquipmentIndex];
                if (equipment != null)
                {
                    GameProgressManager.DebugGrantEquipment(equipment.Id);
                }
            }

            if (GUILayout.Button("直接获得全部装备"))
            {
                for (var i = 0; i < equipmentDatabase.Equipments.Count; i++)
                {
                    var equipment = equipmentDatabase.Equipments[i];
                    if (equipment != null)
                    {
                        GameProgressManager.DebugGrantEquipment(equipment.Id);
                    }
                }
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSkillTools()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("功法", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("功法是直接写进当前局角色数据里。给完后重新打开战斗页或相关面板，就能看到等级变化。", MessageType.None);

        var characterDatabase = CharacterDatabaseLoader.Load();
        var skillDatabase = SkillDatabaseLoader.Load();
        var characterOptions = BuildCharacterOptions(characterDatabase);
        var skillOptions = BuildSkillOptions(skillDatabase);

        selectedCharacterIndex = Mathf.Clamp(selectedCharacterIndex, 0, Mathf.Max(0, characterOptions.Length - 1));
        selectedSkillIndex = Mathf.Clamp(selectedSkillIndex, 0, Mathf.Max(0, skillOptions.Length - 1));
        selectedCharacterIndex = EditorGUILayout.Popup("角色", selectedCharacterIndex, characterOptions);
        selectedSkillIndex = EditorGUILayout.Popup("功法", selectedSkillIndex, skillOptions);
        skillLevel = EditorGUILayout.IntField("功法等级", skillLevel);

        using (new EditorGUI.DisabledScope(characterDatabase == null || skillDatabase == null || characterDatabase.Characters.Count == 0 || skillDatabase.Skills.Count == 0))
        {
            if (GUILayout.Button("直接获得这门功法"))
            {
                var character = characterDatabase.Characters[selectedCharacterIndex];
                var skill = skillDatabase.Skills[selectedSkillIndex];
                if (character != null && skill != null)
                {
                    GameProgressManager.DebugGrantSkill(character.Id, skill.Id, skillLevel);
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全功法 Lv1"))
            {
                GrantAllSkills(characterDatabase, skillDatabase, 1);
            }
            if (GUILayout.Button("全功法 Lv5"))
            {
                GrantAllSkills(characterDatabase, skillDatabase, 5);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void GrantAllSpiritStoneElements(int amount)
    {
        for (var i = 0; i < spiritStoneElementIds.Length; i++)
        {
            GameProgressManager.DebugGrantSpiritStones(spiritStoneElementIds[i], amount);
        }
    }

    private static void GrantAllSkills(CharacterDatabase characterDatabase, SkillDatabase skillDatabase, int level)
    {
        if (characterDatabase == null || skillDatabase == null)
        {
            return;
        }

        for (var i = 0; i < characterDatabase.Characters.Count; i++)
        {
            var character = characterDatabase.Characters[i];
            if (character == null)
            {
                continue;
            }

            for (var j = 0; j < skillDatabase.Skills.Count; j++)
            {
                var skill = skillDatabase.Skills[j];
                if (skill != null)
                {
                    GameProgressManager.DebugGrantSkill(character.Id, skill.Id, level);
                }
            }
        }
    }

    private static string[] BuildEquipmentOptions(EquipmentDatabase equipmentDatabase)
    {
        if (equipmentDatabase == null || equipmentDatabase.Equipments == null || equipmentDatabase.Equipments.Count == 0)
        {
            return new[] { "暂无装备数据" };
        }

        var results = new string[equipmentDatabase.Equipments.Count];
        for (var i = 0; i < equipmentDatabase.Equipments.Count; i++)
        {
            var equipment = equipmentDatabase.Equipments[i];
            results[i] = equipment == null ? "(空)" : equipment.Id + " / " + equipment.Name;
        }

        return results;
    }

    private static string[] BuildCharacterOptions(CharacterDatabase characterDatabase)
    {
        if (characterDatabase == null || characterDatabase.Characters == null || characterDatabase.Characters.Count == 0)
        {
            return new[] { "暂无角色数据" };
        }

        var results = new string[characterDatabase.Characters.Count];
        for (var i = 0; i < characterDatabase.Characters.Count; i++)
        {
            var character = characterDatabase.Characters[i];
            results[i] = character == null ? "(空)" : character.Id + " / " + character.Name;
        }

        return results;
    }

    private static string[] BuildSkillOptions(SkillDatabase skillDatabase)
    {
        if (skillDatabase == null || skillDatabase.Skills == null || skillDatabase.Skills.Count == 0)
        {
            return new[] { "暂无功法数据" };
        }

        var results = new string[skillDatabase.Skills.Count];
        for (var i = 0; i < skillDatabase.Skills.Count; i++)
        {
            var skill = skillDatabase.Skills[i];
            results[i] = skill == null ? "(空)" : skill.Id + " / " + skill.Name;
        }

        return results;
    }
}
