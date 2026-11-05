using System.IO;
using System.Text;

namespace config;

internal class ConfigFlatParser
{
    internal static ConfigValue ParseDefault(string text)
    {
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();
        return Parse(text, "flat", 0, ConfigDuplicateKeyMode.Error, diagnostics);
    }

    internal static ConfigValue ParseStream(Stream stream, string sourceName, int precedenceRank, ConfigDuplicateKeyMode duplicateKeyMode, ConfigDiagnostics diagnostics)
    {
        StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true);
        string text = reader.ReadToEnd();
        reader.Dispose();
        return Parse(text, sourceName, precedenceRank, duplicateKeyMode, diagnostics);
    }

    internal static ConfigValue Parse(string text, string sourceName, int precedenceRank, ConfigDuplicateKeyMode duplicateKeyMode, ConfigDiagnostics diagnostics)
    {
        ConfigMap map = new ConfigMap();
        string[] lines = ConfigIniParser.NormalizeNewLines(text).Split('\n');

        int index = 0;
        while (index < lines.Length)
        {
            string rawLine = lines[index];
            string content = ConfigIniParser.StripComment(rawLine);
            string line = content.Trim();
            int lineNumber = index + 1;

            if (line.Length > 0)
            {
                if (!ConfigIniParser.HasBalancedQuotes(content))
                {
                    diagnostics.AddParseError(sourceName, "", "malformed flat key-value line", lineNumber, ConfigIniParser.FirstContentColumn(rawLine));
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
        int assignmentIndex = FindAssignmentIndex(line);
        if (assignmentIndex < 0)
        {
            diagnostics.AddParseError(sourceName, "", "malformed flat key-value line", lineNumber, ConfigIniParser.FirstContentColumn(rawLine));
            return;
        }

        string key = line.Substring(0, assignmentIndex).Trim();
        if (key.Length == 0)
        {
            diagnostics.AddParseError(sourceName, "", "missing flat key", lineNumber, ConfigIniParser.FirstContentColumn(rawLine));
            return;
        }

        string valueText = line.Substring(assignmentIndex + 1).Trim();
        string path = ConfigPath.Normalize(key, ".");
        ConfigValue value = ConfigValue.String(ConfigIniParser.Unquote(valueText)).SetSource(sourceName, precedenceRank, path, lineNumber, ConfigIniParser.FirstContentColumn(rawLine));
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

        diagnostics.AddParseError(sourceName, path, "duplicate flat key", lineNumber, 1);
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

            if (!quote && current == '=')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }
}
