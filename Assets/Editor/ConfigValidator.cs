
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ConfigValidator
{
    private const string CharacterCsvPath = "Docs/Character.csv";
    private const string EnemyCsvPath = "Docs/Enemy.csv";
    private const string SkillCsvPath = "Docs/Skill.csv";
    private const string EquipmentCsvPath = "Docs/Equipment.csv";
    private const string SpiritStoneCsvPath = "Docs/SpiritStone.csv";

    private static readonly HashSet<string> ValidSkillQualities = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "普通",
        "稀有",
        "绝品",
        "common",
        "rare",
        "epic",
        "legendary"
    };

    private static readonly HashSet<string> ValidElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "金",
        "木",
        "水",
        "火",
        "土",
        "metal",
        "wood",
        "water",
        "fire",
        "earth"
    };

    private static readonly HashSet<string> ValidEquipmentSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Weapon",
        "Armor",
        "Accessory"
    };

    private static readonly Dictionary<string, string> SpiritStoneColorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "金", "#E5C36A" },
        { "木", "#5BC16A" },
        { "水", "#59A7FF" },
        { "火", "#FF6B5D" },
        { "土", "#E6D25A" },
        { "metal", "#E5C36A" },
        { "wood", "#5BC16A" },
        { "water", "#59A7FF" },
        { "fire", "#FF6B5D" },
        { "earth", "#E6D25A" }
    };

    [MenuItem("工具/配置/校验全部配置")]
    public static void ValidateAll()
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            var skillTable = LoadTable(SkillCsvPath, "技能表");
            var characterTable = LoadTable(CharacterCsvPath, "角色表");
            var enemyTable = LoadTable(EnemyCsvPath, "敌人表");
            var equipmentTable = LoadTable(EquipmentCsvPath, "装备表");
            var spiritStoneTable = LoadTable(SpiritStoneCsvPath, "灵石表");

            var skillIds = ValidateSkillTable(skillTable, errors, warnings);
            ValidateCharacterTable(characterTable, skillIds, errors, warnings);
            ValidateEnemyTable(enemyTable, skillIds, errors, warnings);
            ValidateEquipmentTable(equipmentTable, errors, warnings);
            ValidateSpiritStoneTable(spiritStoneTable, errors, warnings);

            if (warnings.Count > 0)
            {
                for (var i = 0; i < warnings.Count; i++)
                {
                    Debug.LogWarning(warnings[i]);
                }
            }

            if (errors.Count > 0)
            {
                for (var i = 0; i < errors.Count; i++)
                {
                    Debug.LogError(errors[i]);
                }

                Debug.LogError($"配置校验失败：共发现 {errors.Count} 个错误，{warnings.Count} 个警告。");
                return;
            }

            Debug.Log($"配置校验通过：未发现错误。警告 {warnings.Count} 个。");
        }
        catch (Exception exception)
        {
            Debug.LogError("配置校验执行失败：" + exception.Message);
        }
    }

    private static HashSet<string> ValidateSkillTable(CsvTable table, List<string> errors, List<string> warnings)
    {
        ValidateDuplicateIds(table, errors);

        var skillIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            RequireFields(row, errors, "ID", "Name", "Element", "Quality", "Category", "TargetType", "EffectType");
            ValidateIntegerField(row, errors, "MPCost");
            ValidateIntegerField(row, errors, "Power");
            ValidateIntegerField(row, errors, "Duration");

            var skillId = row.Get("ID");
            if (!string.IsNullOrWhiteSpace(skillId))
            {
                skillIds.Add(skillId.Trim());
            }

            var quality = row.Get("Quality");
            if (!string.IsNullOrWhiteSpace(quality) && !ValidSkillQualities.Contains(quality.Trim()))
            {
                errors.Add(BuildMessage(row, "Quality", $"技能品质不合法：'{quality}'。当前仅支持 普通/稀有/绝品（或 common/rare/epic/legendary）。"));
            }

            var element = row.Get("Element");
            if (!string.IsNullOrWhiteSpace(element) && !IsValidElementToken(element))
            {
                errors.Add(BuildMessage(row, "Element", $"技能五行不合法：'{element}'。"));
            }

            DetectMojibake(row, warnings, "Name");
            DetectMojibake(row, warnings, "Description");
            DetectMojibake(row, warnings, "Notes");
        }

        return skillIds;
    }

    private static void ValidateCharacterTable(CsvTable table, HashSet<string> skillIds, List<string> errors, List<string> warnings)
    {
        ValidateDuplicateIds(table, errors);

        for (var i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            RequireFields(row, errors, "ID", "Name", "ElementRoots", "ClassRole", "Position", "CombatStyle", "InitialSkills");
            ValidateIntegerField(row, errors, "HP");
            ValidateIntegerField(row, errors, "ATK");
            ValidateIntegerField(row, errors, "DEF");
            ValidateIntegerField(row, errors, "MP");

            ValidateElementList(row, errors, "ElementRoots");
            ValidateSkillReferenceList(row, "InitialSkills", skillIds, errors, warnings);
        }
    }

    private static void ValidateEnemyTable(CsvTable table, HashSet<string> skillIds, List<string> errors, List<string> warnings)
    {
        ValidateDuplicateIds(table, errors);

        for (var i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            RequireFields(row, errors, "ID", "Name", "Element", "Role", "Position", "CombatStyle", "Skills");
            ValidateIntegerField(row, errors, "HP");
            ValidateIntegerField(row, errors, "ATK");
            ValidateIntegerField(row, errors, "DEF");
            ValidateIntegerField(row, errors, "MP");

            ValidateElementList(row, errors, "Element");
            ValidateSkillReferenceList(row, "Skills", skillIds, errors, warnings);
        }
    }

    private static void ValidateEquipmentTable(CsvTable table, List<string> errors, List<string> warnings)
    {
        ValidateDuplicateIds(table, errors);

        for (var i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            RequireFields(row, errors, "ID", "Name", "Slot");
            ValidateIntegerField(row, errors, "HP");
            ValidateIntegerField(row, errors, "ATK");
            ValidateIntegerField(row, errors, "DEF");
            ValidateIntegerField(row, errors, "MP");

            var slot = row.Get("Slot");
            if (!string.IsNullOrWhiteSpace(slot) && !ValidEquipmentSlots.Contains(slot.Trim()))
            {
                errors.Add(BuildMessage(row, "Slot", $"装备槽位缺少运行时映射：'{slot}'。当前仅支持 Weapon/Armor/Accessory。"));
            }

            DetectMojibake(row, warnings, "Name");
            DetectMojibake(row, warnings, "Notes");
        }
    }
    private static void ValidateSpiritStoneTable(CsvTable table, List<string> errors, List<string> warnings)
    {
        ValidateDuplicateIds(table, errors);

        for (var i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            RequireFields(row, errors, "ID", "Name", "Element", "ColorHex");

            var element = row.Get("Element");
            if (!string.IsNullOrWhiteSpace(element) && !IsValidElementToken(element))
            {
                errors.Add(BuildMessage(row, "Element", $"灵石五行不合法：'{element}'。"));
            }

            var colorHex = row.Get("ColorHex");
            if (!string.IsNullOrWhiteSpace(colorHex))
            {
                var normalizedColor = colorHex.Trim().ToUpperInvariant();
                if (!IsHexColor(normalizedColor))
                {
                    errors.Add(BuildMessage(row, "ColorHex", $"灵石颜色格式不合法：'{colorHex}'。应为 #RRGGBB。"));
                }
                else if (!string.IsNullOrWhiteSpace(element))
                {
                    string expectedColor;
                    if (SpiritStoneColorMap.TryGetValue(element.Trim(), out expectedColor)
                        && !string.Equals(expectedColor, normalizedColor, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add(BuildMessage(row, "ColorHex", $"灵石颜色与当前运行时映射不一致：'{element}' 期望 {expectedColor}，实际为 {normalizedColor}。"));
                    }
                }
            }

            DetectMojibake(row, warnings, "Name");
            DetectMojibake(row, warnings, "Notes");
        }
    }

    private static void ValidateDuplicateIds(CsvTable table, List<string> errors)
    {
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            var id = row.Get("ID");
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            id = id.Trim();
            int firstLine;
            if (seen.TryGetValue(id, out firstLine))
            {
                errors.Add(BuildMessage(row, "ID", $"发现重复 ID：'{id}'，首次出现在第 {firstLine} 行。"));
            }
            else
            {
                seen[id] = row.LineNumber;
            }
        }
    }

    private static void ValidateSkillReferenceList(CsvRow row, string fieldName, HashSet<string> skillIds, List<string> errors, List<string> warnings)
    {
        var raw = row.Get(fieldName);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        var parts = raw.Split('|');
        for (var i = 0; i < parts.Length; i++)
        {
            var token = parts[i].Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            if (skillIds.Contains(token))
            {
                continue;
            }

            if (!LooksLikeSkillId(token))
            {
                warnings.Add(BuildMessage(row, fieldName, $"技能引用 '{token}' 不是技能 ID 格式。当前运行时按技能 ID 读取，建议改成 SKxxx。"));
            }

            errors.Add(BuildMessage(row, fieldName, $"技能引用无效：'{token}'，在 Skill.csv 中找不到对应 ID。"));
        }
    }

    private static void ValidateElementList(CsvRow row, List<string> errors, string fieldName)
    {
        var raw = row.Get(fieldName);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        var parts = raw.Split('|');
        for (var i = 0; i < parts.Length; i++)
        {
            var token = parts[i].Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            if (!IsValidElementToken(token))
            {
                errors.Add(BuildMessage(row, fieldName, $"五行字段不合法：'{token}'。"));
            }
        }
    }

    private static void RequireFields(CsvRow row, List<string> errors, params string[] fieldNames)
    {
        for (var i = 0; i < fieldNames.Length; i++)
        {
            var fieldName = fieldNames[i];
            if (string.IsNullOrWhiteSpace(row.Get(fieldName)))
            {
                errors.Add(BuildMessage(row, fieldName, "必填字段为空。"));
            }
        }
    }

    private static void ValidateIntegerField(CsvRow row, List<string> errors, string fieldName)
    {
        var value = row.Get(fieldName);
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(BuildMessage(row, fieldName, "数值字段为空。"));
            return;
        }

        int parsedValue;
        if (!int.TryParse(value.Trim(), out parsedValue))
        {
            errors.Add(BuildMessage(row, fieldName, $"数值字段无法解析为整数：'{value}'。"));
        }
    }
    private static void DetectMojibake(CsvRow row, List<string> warnings, string fieldName)
    {
        var value = row.Get(fieldName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (value.IndexOf('�') >= 0
            || value.IndexOf("鍙", StringComparison.Ordinal) >= 0
            || value.IndexOf("閲", StringComparison.Ordinal) >= 0
            || value.IndexOf("鐏", StringComparison.Ordinal) >= 0
            || value.IndexOf("姘", StringComparison.Ordinal) >= 0)
        {
            warnings.Add(BuildMessage(row, fieldName, $"疑似存在编码异常：'{value}'。"));
        }
    }

    private static bool LooksLikeSkillId(string token)
    {
        if (token.Length < 3)
        {
            return false;
        }

        return token.StartsWith("SK", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidElementToken(string token)
    {
        return ValidElements.Contains(token.Trim());
    }

    private static bool IsHexColor(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length != 7 || value[0] != '#')
        {
            return false;
        }

        for (var i = 1; i < value.Length; i++)
        {
            var ch = value[i];
            var isDigit = ch >= '0' && ch <= '9';
            var isUpperHex = ch >= 'A' && ch <= 'F';
            var isLowerHex = ch >= 'a' && ch <= 'f';
            if (!isDigit && !isUpperHex && !isLowerHex)
            {
                return false;
            }
        }

        return true;
    }

    private static string BuildMessage(CsvRow row, string fieldName, string message)
    {
        return $"[{row.Table.DisplayName}] 第 {row.LineNumber} 行 / {fieldName}：{message}";
    }

    private static CsvTable LoadTable(string relativePath, string displayName)
    {
        var absolutePath = GetProjectFilePath(relativePath);
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException(displayName + "不存在。", absolutePath);
        }

        var rows = ReadCsvRows(absolutePath);
        if (rows.Count <= 1)
        {
            throw new InvalidOperationException(displayName + "为空。");
        }

        var headers = rows[0];
        var table = new CsvTable(relativePath, displayName, headers);
        for (var i = 1; i < rows.Count; i++)
        {
            var columns = rows[i];
            if (IsRowEmpty(columns))
            {
                continue;
            }

            table.Rows.Add(new CsvRow(table, i + 1, headers, columns));
        }

        return table;
    }

    private static string GetProjectFilePath(string relativePath)
    {
        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.Combine(projectRoot, relativePath);
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
    private sealed class CsvTable
    {
        public CsvTable(string relativePath, string displayName, List<string> headers)
        {
            RelativePath = relativePath;
            DisplayName = displayName;
            Headers = headers;
            Rows = new List<CsvRow>();
        }

        public string RelativePath { get; }
        public string DisplayName { get; }
        public List<string> Headers { get; }
        public List<CsvRow> Rows { get; }
    }

    private sealed class CsvRow
    {
        private readonly Dictionary<string, string> values;

        public CsvRow(CsvTable table, int lineNumber, List<string> headers, List<string> columns)
        {
            Table = table;
            LineNumber = lineNumber;
            values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < headers.Count; i++)
            {
                var header = headers[i];
                var value = i < columns.Count ? columns[i] : string.Empty;
                if (!values.ContainsKey(header))
                {
                    values.Add(header, value);
                }
            }
        }

        public CsvTable Table { get; }
        public int LineNumber { get; }

        public string Get(string fieldName)
        {
            string value;
            return values.TryGetValue(fieldName, out value) ? value : string.Empty;
        }
    }
}
