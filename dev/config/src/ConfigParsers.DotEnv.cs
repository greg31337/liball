using System.Collections.Generic;
using System.IO;
using System.Text;

namespace config;

internal class ConfigDotEnvParser
{
    internal static ConfigValue ParseDefault(string text)
    {
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();
        return Parse(text, "dotenv", 0, ConfigDuplicateKeyMode.Error, diagnostics);
    }

    internal static ConfigValue Parse(string text, string sourceName, int precedenceRank, ConfigDuplicateKeyMode duplicateKeyMode, ConfigDiagnostics diagnostics)
    {
        ConfigMap map = new ConfigMap();
        string[] lines = ConfigIniParser.NormalizeNewLines(text).Split('\n');

        int index = 0;
        while (index < lines.Length)
        {
            string rawLine = lines[index];
            string content = StripComment(rawLine);
            string line = content.Trim();
            int lineNumber = index + 1;

            if (line.Length > 0)
            {
                if (!ConfigIniParser.HasBalancedQuotes(content))
                {
                    diagnostics.AddParseError(sourceName, "", "malformed dotenv line", lineNumber, ConfigIniParser.FirstContentColumn(rawLine));
                }
                else
                {
                    ParseLine(map, rawLine, line, sourceName, precedenceRank, duplicateKeyMode, diagnostics, lineNumber);
                }
            }

            index = index + 1;
        }

        return ConfigValue.Object(map).SetSource(sourceName, precedenceRank, "", 0, 0);
    }

    internal static ConfigValue ParseAndExpand(string text, string sourceName, int precedenceRank, ConfigDuplicateKeyMode duplicateKeyMode, ConfigDiagnostics diagnostics)
    {
        return ParseAndExpandWithMissingMode(text, sourceName, precedenceRank, duplicateKeyMode, ConfigDotEnvMissingValueMode.Empty, diagnostics);
    }

    internal static ConfigValue ParseAndExpandWithMissingMode(string text, string sourceName, int precedenceRank, ConfigDuplicateKeyMode duplicateKeyMode, ConfigDotEnvMissingValueMode missingValueMode, ConfigDiagnostics diagnostics)
    {
        ConfigValue root = Parse(text, sourceName, precedenceRank, duplicateKeyMode, diagnostics);
        if (root.ObjectValue != null)
        {
            ExpandWithMissingMode(root.ObjectValue, diagnostics, sourceName, missingValueMode);
        }

        return root;
    }

    internal static ConfigValue ParseFile(string path, string sourceName, int precedenceRank, ConfigDuplicateKeyMode duplicateKeyMode, ConfigDiagnostics diagnostics)
    {
        string text = File.ReadAllText(path);
        return Parse(text, sourceName, precedenceRank, duplicateKeyMode, diagnostics);
    }

    internal static ConfigValue ParseOptionalFile(string path, string sourceName, int precedenceRank, ConfigDuplicateKeyMode duplicateKeyMode, ConfigDiagnostics diagnostics)
    {
        if (!File.Exists(path))
        {
            ConfigMap map = new ConfigMap();
            return ConfigValue.Object(map).SetSource(sourceName, precedenceRank, "", 0, 0);
        }

        return ParseFile(path, sourceName, precedenceRank, duplicateKeyMode, diagnostics);
    }

    internal static void Expand(ConfigMap map, ConfigDiagnostics diagnostics, string sourceName)
    {
        ExpandWithMissingMode(map, diagnostics, sourceName, ConfigDotEnvMissingValueMode.Empty);
    }

    internal static void ExpandWithMissingMode(ConfigMap map, ConfigDiagnostics diagnostics, string sourceName, ConfigDotEnvMissingValueMode missingValueMode)
    {
        ExpandMap(map, map, "", diagnostics, sourceName, missingValueMode);
    }

    internal static void ExpandMap(ConfigMap root, ConfigMap current, string parentPath, ConfigDiagnostics diagnostics, string sourceName, ConfigDotEnvMissingValueMode missingValueMode)
    {
        string[] keys = current.Keys();

        int index = 0;
        while (index < keys.Length)
        {
            string path = ConfigJsonParser.ChildPath(parentPath, keys[index]);
            ConfigValue value = current.Get(keys[index]);

            if (value.Kind == ConfigValueKind.Object && value.ObjectValue != null)
            {
                ExpandMap(root, value.ObjectValue, path, diagnostics, sourceName, missingValueMode);
            }
            else if (value.Kind == ConfigValueKind.String)
            {
                List<string> stack = new List<string>();
                value.StringValue = ExpandPath(root, path, diagnostics, sourceName, stack, missingValueMode);
            }

            index = index + 1;
        }
    }

    internal static string ExpandPath(ConfigMap root, string path, ConfigDiagnostics diagnostics, string sourceName, List<string> stack, ConfigDotEnvMissingValueMode missingValueMode)
    {
        ConfigValue value = root.GetPathDefault(path);
        if (value.IsMissing())
        {
            return MissingReferenceText(path, true, diagnostics, sourceName, missingValueMode, 0, 0);
        }

        if (value.Kind != ConfigValueKind.String)
        {
            return value.ToDisplayString();
        }

        if (ContainsPath(stack, path))
        {
            diagnostics.AddParseError(sourceName, path, "cycle in dotenv variable expansion", 0, 0);
            if (value.StringValue == null)
            {
                return "";
            }

            return value.StringValue;
        }

        stack.Add(path);

        string text = "";
        if (value.StringValue != null)
        {
            text = value.StringValue;
        }

        string expanded = ExpandText(root, path, text, diagnostics, sourceName, stack, missingValueMode);
        value.StringValue = expanded;
        stack.RemoveAt(stack.Count - 1);
        return expanded;
    }

    internal static string ExpandText(ConfigMap root, string currentPath, string text, ConfigDiagnostics diagnostics, string sourceName, List<string> stack, ConfigDotEnvMissingValueMode missingValueMode)
    {
        StringBuilder builder = new StringBuilder();

        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '$')
            {
                if (index + 1 < text.Length && text[index + 1] == '{')
                {
                    int end = FindClosingBrace(text, index + 2);
                    if (end >= 0)
                    {
                        string reference = text.Substring(index + 2, end - index - 2);
                        builder.Append(ExpandReference(root, currentPath, reference, diagnostics, sourceName, stack, missingValueMode, true));
                        index = end + 1;
                        continue;
                    }

                    ConfigValue currentValue = root.GetPathDefault(currentPath);
                    diagnostics.AddParseError(sourceName, currentPath, "malformed dotenv expansion", currentValue.LineNumber, currentValue.ColumnNumber);
                }
                else if (index + 1 < text.Length && IsVariableCharacter(text[index + 1]))
                {
                    int end = index + 1;
                    while (end < text.Length && IsVariableCharacter(text[end]))
                    {
                        end = end + 1;
                    }

                    string reference = text.Substring(index + 1, end - index - 1);
                    builder.Append(ExpandReference(root, currentPath, reference, diagnostics, sourceName, stack, missingValueMode, false));
                    index = end;
                    continue;
                }
            }

            builder.Append(text[index]);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string ExpandReference(ConfigMap root, string currentPath, string reference, ConfigDiagnostics diagnostics, string sourceName, List<string> stack, ConfigDotEnvMissingValueMode missingValueMode, bool braced)
    {
        string path = ConfigPath.Normalize(reference, ".");
        if (path.Length == 0)
        {
            return "";
        }

        ConfigValue value = root.GetPathDefault(path);
        if (value.IsMissing())
        {
            ConfigValue currentValue = root.GetPathDefault(currentPath);
            return MissingReferenceText(reference, braced, diagnostics, sourceName, missingValueMode, currentValue.LineNumber, currentValue.ColumnNumber);
        }

        return ExpandPath(root, path, diagnostics, sourceName, stack, missingValueMode);
    }

    internal static string MissingReferenceText(string reference, bool braced, ConfigDiagnostics diagnostics, string sourceName, ConfigDotEnvMissingValueMode missingValueMode, int lineNumber, int columnNumber)
    {
        string path = ConfigPath.Normalize(reference, ".");
        if (missingValueMode == ConfigDotEnvMissingValueMode.Error)
        {
            diagnostics.AddParseError(sourceName, path, "missing dotenv variable expansion", lineNumber, columnNumber);
            return "";
        }

        diagnostics.AddWarning(sourceName, path, "missing dotenv variable expansion", lineNumber, columnNumber);
        if (missingValueMode == ConfigDotEnvMissingValueMode.Preserve)
        {
            if (braced)
            {
                return "${" + reference + "}";
            }

            return "$" + reference;
        }

        return "";
    }

    internal static int FindClosingBrace(string text, int start)
    {
        int index = start;
        while (index < text.Length)
        {
            if (text[index] == '}')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool IsVariableCharacter(char value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return true;
        }

        if (value >= 'a' && value <= 'z')
        {
            return true;
        }

        if (value >= '0' && value <= '9')
        {
            return true;
        }

        if (value == '_' || value == '.')
        {
            return true;
        }

        return false;
    }

    internal static bool ContainsPath(List<string> paths, string path)
    {
        int index = 0;
        while (index < paths.Count)
        {
            if (paths[index] == path)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void ParseLine(
        ConfigMap map,
        string rawLine,
        string line,
        string sourceName,
        int precedenceRank,
        ConfigDuplicateKeyMode duplicateKeyMode,
        ConfigDiagnostics diagnostics,
        int lineNumber)
    {
        string content = RemoveExportPrefix(line);
        int assignmentIndex = FindAssignmentIndex(content);
        if (assignmentIndex < 0)
        {
            diagnostics.AddParseError(sourceName, "", "malformed dotenv line", lineNumber, ConfigIniParser.FirstContentColumn(rawLine));
            return;
        }

        string key = content.Substring(0, assignmentIndex).Trim();
        if (key.Length == 0)
        {
            diagnostics.AddParseError(sourceName, "", "missing dotenv key", lineNumber, ConfigIniParser.FirstContentColumn(rawLine));
            return;
        }

        string valueText = content.Substring(assignmentIndex + 1).Trim();
        string path = ConfigPath.Normalize(key, ".");
        ConfigValue value = ConfigValue.String(Unquote(valueText)).SetSource(sourceName, precedenceRank, path, lineNumber, ConfigIniParser.FirstContentColumn(rawLine));
        SetParsedValue(map, path, value, duplicateKeyMode, diagnostics, sourceName, lineNumber);
    }

    internal static void SetParsedValue(
        ConfigMap map,
        string path,
        ConfigValue value,
        ConfigDuplicateKeyMode duplicateKeyMode,
        ConfigDiagnostics diagnostics,
        string sourceName,
        int lineNumber)
    {
        if (!map.HasPathDefault(path))
        {
            map.SetPathDefault(path, value);
            return;
        }

        if (duplicateKeyMode == ConfigDuplicateKeyMode.LastWins)
        {
            map.SetPathDefault(path, value);
            return;
        }

        diagnostics.AddParseError(sourceName, path, "duplicate dotenv key", lineNumber, 1);
    }

    internal static string RemoveExportPrefix(string line)
    {
        if (line.StartsWith("export "))
        {
            return line.Substring(7).Trim();
        }

        if (line.StartsWith("export\t"))
        {
            return line.Substring(7).Trim();
        }

        return line;
    }

    internal static int FindAssignmentIndex(string line)
    {
        int index = 0;
        while (index < line.Length)
        {
            if (line[index] == '=')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static string StripComment(string line)
    {
        bool quote = false;
        char quoteChar = '\0';
        StringBuilder builder = new StringBuilder();

        int index = 0;
        while (index < line.Length)
        {
            char current = line[index];
            if (current == '"' || current == '\'')
            {
                if (!quote)
                {
                    quote = true;
                    quoteChar = current;
                }
                else if (quoteChar == current)
                {
                    quote = false;
                }
            }

            if (!quote && current == '#')
            {
                return builder.ToString();
            }

            builder.Append(current);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string Unquote(string value)
    {
        string trimmed = value.Trim();
        if (trimmed.Length < 2)
        {
            return trimmed;
        }

        char first = trimmed[0];
        char last = trimmed[trimmed.Length - 1];
        if (!((first == '"' && last == '"') || (first == '\'' && last == '\'')))
        {
            return trimmed;
        }

        if (first == '"')
        {
            return UnescapeDoubleQuoted(trimmed.Substring(1, trimmed.Length - 2));
        }

        return UnescapeSingleQuoted(trimmed.Substring(1, trimmed.Length - 2));
    }

    internal static string UnescapeDoubleQuoted(string value)
    {
        StringBuilder builder = new StringBuilder();

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\\' && index + 1 < value.Length)
            {
                char next = value[index + 1];
                if (next == 'n')
                {
                    builder.Append('\n');
                }
                else if (next == 'r')
                {
                    builder.Append('\r');
                }
                else if (next == 't')
                {
                    builder.Append('\t');
                }
                else
                {
                    builder.Append(next);
                }

                index = index + 2;
                continue;
            }

            builder.Append(value[index]);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string UnescapeSingleQuoted(string value)
    {
        StringBuilder builder = new StringBuilder();

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\\' && index + 1 < value.Length)
            {
                char next = value[index + 1];
                if (next == '\'' || next == '\\')
                {
                    builder.Append(next);
                    index = index + 2;
                    continue;
                }
            }

            builder.Append(value[index]);
            index = index + 1;
        }

        return builder.ToString();
    }
}
