using System;
using System.IO;
using UnityEditor;

[InitializeOnLoad]
public static class ConfigAutoSync
{
    private static bool isSyncQueued;
    private static bool isImporting;

    static ConfigAutoSync()
    {
        EditorApplication.delayCall += TrySyncIfNeeded;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            TrySyncIfNeeded();
        }
    }

    public static void QueueSync()
    {
        if (isSyncQueued)
        {
            return;
        }

        isSyncQueued = true;
        EditorApplication.delayCall += TrySyncIfNeeded;
    }

    private static void TrySyncIfNeeded()
    {
        isSyncQueued = false;
        if (isImporting)
        {
            return;
        }

        if (AreAllOutputsUpToDate())
        {
            return;
        }

        try
        {
            isImporting = true;
            CharacterCsvImporter.ImportAll();
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogError("Auto config sync failed: " + exception.Message);
        }
        finally
        {
            isImporting = false;
        }
    }

    private static bool AreAllOutputsUpToDate()
    {
        return IsOutputUpToDate("Docs/Character.csv", "Assets/Resources/Configs/CharacterDatabase.json")
            && IsOutputUpToDate("Docs/Enemy.csv", "Assets/Resources/Configs/EnemyDatabase.json")
            && IsOutputUpToDate("Docs/Skill.csv", "Assets/Resources/Configs/SkillDatabase.json")
            && IsOutputUpToDate("Docs/SkillEffect.csv", "Assets/Resources/Configs/SkillDatabase.json")
            && IsOutputUpToDate("Docs/BattleFormula.csv", "Assets/Resources/Configs/BattleFormulaDatabase.json")
            && IsOutputUpToDate("Docs/ElementRelation.csv", "Assets/Resources/Configs/ElementRelationDatabase.json")
            && IsOutputUpToDate("Docs/EnemyEncounter.csv", "Assets/Resources/Configs/EnemyEncounterDatabase.json")
            && IsOutputUpToDate("Docs/Equipment.csv", "Assets/Resources/Configs/EquipmentDatabase.json")
            && IsOutputUpToDate("Docs/SpiritStone.csv", "Assets/Resources/Configs/SpiritStoneDatabase.json")
            && IsOutputUpToDate("Docs/StageBalance.csv", "Assets/Resources/Configs/StageBalanceDatabase.json")
            && IsOutputUpToDate("Docs/StageNode.csv", "Assets/Resources/Configs/StageNodeDatabase.json")
            && IsOutputUpToDate("Docs/EventOption.csv", "Assets/Resources/Configs/EventOptionDatabase.json")
            && IsOutputUpToDate("Docs/EventProfile.csv", "Assets/Resources/Configs/EventProfileDatabase.json")
            && IsOutputUpToDate("Docs/Localization.csv", "Assets/Resources/Localization/GameText.json");
    }

    private static bool IsOutputUpToDate(string inputRelativePath, string outputRelativePath)
    {
        var projectRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
        var inputPath = Path.Combine(projectRoot, inputRelativePath);
        var outputPath = Path.Combine(projectRoot, outputRelativePath);

        if (!File.Exists(inputPath) || !File.Exists(outputPath))
        {
            return false;
        }

        return File.GetLastWriteTimeUtc(outputPath) >= File.GetLastWriteTimeUtc(inputPath);
    }
}

public sealed class ConfigCsvAutoSyncPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (ContainsConfigCsv(importedAssets) || ContainsConfigCsv(movedAssets) || ContainsConfigCsv(movedFromAssetPaths))
        {
            ConfigAutoSync.QueueSync();
        }
    }

    private static bool ContainsConfigCsv(string[] assetPaths)
    {
        if (assetPaths == null)
        {
            return false;
        }

        for (var i = 0; i < assetPaths.Length; i++)
        {
            var path = assetPaths[i];
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            if (path.StartsWith("Docs/", StringComparison.OrdinalIgnoreCase)
                && path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}





