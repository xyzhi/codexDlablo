using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Wuxing.Config;
using Wuxing.Localization;

public static class CharacterCsvImporter
{
    private const string CharacterCsvPath = "Docs/Character.csv";
    private const string EnemyCsvPath = "Docs/Enemy.csv";
    private const string SkillCsvPath = "Docs/Skill.csv";
    private const string LocalizationCsvPath = "Docs/Localization.csv";

    private const string ConfigOutputFolder = "Assets/Resources/Configs";
    private const string LocalizationOutputFolder = "Assets/Resources/Localization";

    private const string CharacterJsonPath = ConfigOutputFolder + "/CharacterDatabase.json";
    private const string EnemyJsonPath = ConfigOutputFolder + "/EnemyDatabase.json";
    private const string SkillJsonPath = ConfigOutputFolder + "/SkillDatabase.json";
    private const string LocalizationJsonPath = LocalizationOutputFolder + "/GameText.json";

    [MenuItem("工具/配置/导入全部CSV")]
    public static void ImportAll()
    {
        try
        {
            ImportCharacters();
            ImportEnemies();
            ImportSkills();
            ImportLocalization();
            AssetDatabase.Refresh();
            Debug.Log("角色、敌人、技能和语言表已完成导入。");
        }
        catch (Exception exception)
        {
            Debug.LogError(exception.Message);
        }
    }

    [MenuItem("工具/配置/仅导入角色表")]
    public static void ImportCharacters()
    {
        var rows = ReadRequiredRows(CharacterCsvPath, "Character CSV");
        var configs = new List<CharacterConfig>();

        for (var i = 1; i < rows.Count; i++)
        {
            var columns = rows[i];
            if (IsRowEmpty(columns))
            {
                continue;
            }

            if (columns.Count < 12)
            {
                throw new InvalidOperationException($"Invalid character row at line {i + 1}.");
            }

            configs.Add(new CharacterConfig
            {
                Id = columns[0],
                Name = columns[1],
                ElementRoots = columns[2],
                ClassRole = columns[3],
                Position = columns[4],
                HP = ParseInt(columns[5], nameof(CharacterConfig.HP), i + 1),
                ATK = ParseInt(columns[6], nameof(CharacterConfig.ATK), i + 1),
                DEF = ParseInt(columns[7], nameof(CharacterConfig.DEF), i + 1),
                MP = ParseInt(columns[8], nameof(CharacterConfig.MP), i + 1),
                CombatStyle = columns[9],
                InitialSkills = columns[10],
                GrowthNotes = columns[11]
            });
        }

        EnsureFolderExists(ConfigOutputFolder);
        var database = new CharacterDatabase();
        database.characters = configs;
        WriteJson(CharacterJsonPath, JsonUtility.ToJson(database, true));
        Debug.Log($"Imported {configs.Count} characters to {CharacterJsonPath}");
    }

    [MenuItem("工具/配置/仅导入敌人表")]
    public static void ImportEnemies()
    {
        var rows = ReadRequiredRows(EnemyCsvPath, "Enemy CSV");
        var configs = new List<EnemyConfig>();

        for (var i = 1; i < rows.Count; i++)
        {
            var columns = rows[i];
            if (IsRowEmpty(columns))
            {
                continue;
            }

            if (columns.Count < 12)
            {
                throw new InvalidOperationException($"Invalid enemy row at line {i + 1}.");
            }

            configs.Add(new EnemyConfig
            {
                Id = columns[0],
                Name = columns[1],
                Element = columns[2],
                Role = columns[3],
                Position = columns[4],
                HP = ParseInt(columns[5], nameof(EnemyConfig.HP), i + 1),
                ATK = ParseInt(columns[6], nameof(EnemyConfig.ATK), i + 1),
                DEF = ParseInt(columns[7], nameof(EnemyConfig.DEF), i + 1),
                MP = ParseInt(columns[8], nameof(EnemyConfig.MP), i + 1),
                CombatStyle = columns[9],
                Skills = columns[10],
                Notes = columns[11]
            });
        }

        EnsureFolderExists(ConfigOutputFolder);
        var database = new EnemyDatabase();
        database.enemies = configs;
        WriteJson(EnemyJsonPath, JsonUtility.ToJson(database, true));
        Debug.Log($"Imported {configs.Count} enemies to {EnemyJsonPath}");
    }

    [MenuItem("工具/配置/仅导入技能表")]
    public static void ImportSkills()
    {
        var rows = ReadRequiredRows(SkillCsvPath, "Skill CSV");
        var configs = new List<SkillConfig>();

        for (var i = 1; i < rows.Count; i++)
        {
            var columns = rows[i];
            if (IsRowEmpty(columns))
            {
                continue;
            }

            if (columns.Count < 11)
            {
                throw new InvalidOperationException($"Invalid skill row at line {i + 1}.");
            }

            configs.Add(new SkillConfig
            {
                Id = columns[0],
                Name = columns[1],
                Element = columns[2],
                Category = columns[3],
                TargetType = columns[4],
                MPCost = ParseInt(columns[5], nameof(SkillConfig.MPCost), i + 1),
                Power = ParseInt(columns[6], nameof(SkillConfig.Power), i + 1),
                Duration = ParseInt(columns[7], nameof(SkillConfig.Duration), i + 1),
                EffectType = columns[8],
                Description = columns[9],
                Notes = columns[10]
            });
        }

        EnsureFolderExists(ConfigOutputFolder);
        var database = new SkillDatabase();
        database.skills = configs;
        WriteJson(SkillJsonPath, JsonUtility.ToJson(database, true));
        Debug.Log($"Imported {configs.Count} skills to {SkillJsonPath}");
    }

    [MenuItem("工具/配置/仅导入语言表")]
    public static void ImportLocalization()
    {
        var rows = ReadRequiredRows(LocalizationCsvPath, "Localization CSV");
        var entries = new List<LocalizationEntry>();

        for (var i = 1; i < rows.Count; i++)
        {
            var columns = rows[i];
            if (IsRowEmpty(columns))
            {
                continue;
            }

            if (columns.Count < 3)
            {
                throw new InvalidOperationException($"Invalid localization row at line {i + 1}.");
            }

            entries.Add(new LocalizationEntry
            {
                key = columns[0],
                zhHans = columns[1],
                en = columns[2]
            });
        }

        EnsureFolderExists(LocalizationOutputFolder);
        var table = new LocalizationTable();
        table.entries = entries.ToArray();
        WriteJson(LocalizationJsonPath, JsonUtility.ToJson(table, true));
        Debug.Log($"Imported {entries.Count} localization rows to {LocalizationJsonPath}");
    }

    private static List<List<string>> ReadRequiredRows(string relativePath, string displayName)
    {
        var csvPath = GetProjectFilePath(relativePath);
        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException(displayName + " not found.", csvPath);
        }

        var rows = ReadCsvRows(csvPath);
        if (rows.Count <= 1)
        {
            throw new InvalidOperationException(displayName + " is empty.");
        }

        return rows;
    }

    private static string GetProjectFilePath(string relativePath)
    {
        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.Combine(projectRoot, relativePath);
    }

    private static void WriteJson(string assetRelativePath, string json)
    {
        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var absolutePath = Path.Combine(projectRoot, assetRelativePath);
        File.WriteAllText(absolutePath, json, new UTF8Encoding(false));
    }

    private static int ParseInt(string value, string fieldName, int lineNumber)
    {
        int result;
        if (int.TryParse(value, out result))
        {
            return result;
        }

        throw new FormatException($"Failed to parse {fieldName} at line {lineNumber}: {value}");
    }

    private static bool IsRowEmpty(List<string> columns)
    {
        for (var i = 0; i < columns.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(columns[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static List<List<string>> ReadCsvRows(string path)
    {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(stream, Encoding.UTF8, true))
        {
            return ParseCsv(reader.ReadToEnd());
        }
    }

    private static List<List<string>> ParseCsv(string content)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var value = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < content.Length; i++)
        {
            var ch = content[i];

            if (ch == '"')
            {
                if (inQuotes && i + 1 < content.Length && content[i + 1] == '"')
                {
                    value.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                row.Add(value.ToString());
                value.Clear();
                continue;
            }

            if ((ch == '\n' || ch == '\r') && !inQuotes)
            {
                if (ch == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                {
                    i++;
                }

                row.Add(value.ToString());
                value.Clear();
                rows.Add(row);
                row = new List<string>();
                continue;
            }

            value.Append(ch);
        }

        if (value.Length > 0 || row.Count > 0)
        {
            row.Add(value.ToString());
            rows.Add(row);
        }

        return rows;
    }

    private static void EnsureFolderExists(string assetFolderPath)
    {
        var parts = assetFolderPath.Split('/');
        var current = parts[0];

        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
