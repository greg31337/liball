using System.Text;

namespace config;

internal class ConfigIniParser
{
    internal static ConfigValue ParseDefault(string text)
    {
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();
        return Parse(text, "ini", 0, ConfigDuplicateKeyMode.Error, diagnostics);
    }

    internal static ConfigValue Parse(string text, string sourceName, int precedenceRank, ConfigDuplicateKeyMode duplicateKeyMode, ConfigDiagnostics diagnostics)
    {
        ConfigMap map = new ConfigMap();
        string section = "";
        string[] lines = NormalizeNewLines(text).Split('\n');

        int index = 0;
        while (index < lines.Length)
        {
            string rawLine = lines[index];
            string content = StripComment(rawLine);
            string line = content.Trim();
            int lineNumber = index + 1;

            if (line.Length > 0)
            {
                if (!HasBalancedQuotes(content))
                {
                    diagnostics.AddParseError(sourceName, "", "malformed INI line", lineNumber, FirstContentColumn(rawLine));
                }
                else if (IsSectionLine(line))
                {
                    section = ConfigPath.Normalize(line.Substring(1, line.Length - 2).Trim(), ".");
                }
                else
                {
                    ParseAssignmentLine(map, section, rawLine, line, sourceName, precedenceRank, duplicateKeyMode, diagnostics, lineNumber);
                }
            }

            index = index + 1;
        }

        return ConfigValue.Object(map).SetSource(sourceName, precedenceRank, "", 0, 0);
    }

    internal static void ParseAssignmentLine(
        ConfigMap map,
        string section,
        string rawLine,
        string line,
        string sourceName,
        int precedenceRank,
        ConfigDuplicateKeyMode duplicateKeyMode,
        ConfigDiagnostics diagnostics,
        int lineNumber)
    {
        int assignmentIndex = FindAssignmentIndex(line);
        if (assignmentIndex < 0)
        {
            diagnostics.AddParseError(sourceName, "", "malformed INI line", lineNumber, FirstContentColumn(rawLine));
            return;
        }

        string key = line.Substring(0, assignmentIndex).Trim();
        string valueText = line.Substring(assignmentIndex + 1).Trim();
        if (key.Length == 0)
        {
            diagnostics.AddParseError(sourceName, "", "missing INI key", lineNumber, FirstContentColumn(rawLine));
            return;
        }

        string fullPath = FullPath(section, key);
        ConfigValue value = ConfigValue.String(Unquote(valueText)).SetSource(sourceName, precedenceRank, fullPath, lineNumber, FirstContentColumn(rawLine));
        SetParsedValue(map, fullPath, value, duplicateKeyMode, diagnostics, sourceName, lineNumber);
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

        diagnostics.AddParseError(sourceName, path, "duplicate INI key", lineNumber, 1);
    }

    internal static string FullPath(string section, string key)
    {
        string normalizedKey = ConfigPath.Normalize(key, ".");
        if (section.Length == 0)
        {
            return normalizedKey;
        }

        return section + "." + normalizedKey;
    }

    internal static bool IsSectionLine(string line)
    {
        if (!line.StartsWith("["))
        {
            return false;
        }

        return line.EndsWith("]");
    }

    internal static int FindAssignmentIndex(string line)
    {
        bool quote = false;
        char quoteChar = '\0';

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

            if (!quote && (current == '=' || current == ':'))
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

            if (!quote && (current == '#' || current == ';'))
            {
                return builder.ToString();
            }

            builder.Append(current);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static bool HasBalancedQuotes(string line)
    {
        bool quote = false;
        char quoteChar = '\0';
        int slashCount = 0;

        int index = 0;
        while (index < line.Length)
        {
            char current = line[index];
            if ((current == '"' || current == '\'') && slashCount % 2 == 0)
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

            if (current == '\\')
            {
                slashCount = slashCount + 1;
            }
            else
            {
                slashCount = 0;
            }

            index = index + 1;
        }

        return !quote;
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

        return Unescape(trimmed.Substring(1, trimmed.Length - 2));
    }

    internal static string Unescape(string value)
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

    internal static int FirstContentColumn(string line)
    {
        int index = 0;
        while (index < line.Length)
        {
            if (line[index] != ' ' && line[index] != '\t')
            {
                return index + 1;
            }

            index = index + 1;
        }

        return 1;
    }

    internal static string NormalizeNewLines(string text)
    {
        return text.Replace("\r\n", "\n").Replace("\r", "\n");
    }
}
