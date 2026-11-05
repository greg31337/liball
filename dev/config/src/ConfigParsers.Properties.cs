using System.Collections.Generic;
using System.Text;

namespace config;

internal class ConfigPropertiesLine
{
    internal string Text;
    internal int LineNumber;

    internal ConfigPropertiesLine(string text, int lineNumber)
    {
        Text = text;
        LineNumber = lineNumber;
    }
}

internal class ConfigPropertiesParser
{
    internal static ConfigValue ParseDefault(string text)
    {
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();
        return Parse(text, "properties", 0, ConfigDuplicateKeyMode.Error, diagnostics);
    }

    internal static ConfigValue Parse(string text, string sourceName, int precedenceRank, ConfigDuplicateKeyMode duplicateKeyMode, ConfigDiagnostics diagnostics)
    {
        ConfigMap map = new ConfigMap();
        List<ConfigPropertiesLine> lines = LogicalLines(text);

        int index = 0;
        while (index < lines.Count)
        {
            ConfigPropertiesLine logicalLine = lines[index];
            string line = logicalLine.Text.Trim();

            if (line.Length > 0 && !IsComment(line))
            {
                if (HasMalformedUnicodeEscape(logicalLine.Text))
                {
                    diagnostics.AddParseError(sourceName, "", "malformed properties escape", logicalLine.LineNumber, FirstContentColumn(logicalLine.Text));
                }
                else
                {
                    ParseLine(map, logicalLine.Text, sourceName, precedenceRank, duplicateKeyMode, diagnostics, logicalLine.LineNumber);
                }
            }

            index = index + 1;
        }

        return ConfigValue.Object(map).SetSource(sourceName, precedenceRank, "", 0, 0);
    }

    internal static void ParseLine(
        ConfigMap map,
        string rawLine,
        string sourceName,
        int precedenceRank,
        ConfigDuplicateKeyMode duplicateKeyMode,
        ConfigDiagnostics diagnostics,
        int lineNumber)
    {
        string content = TrimLeadingWhitespace(rawLine);
        int separatorIndex = FindSeparatorIndex(content);
        string keyText = "";
        string valueText = "";

        if (separatorIndex < 0)
        {
            keyText = content.Trim();
        }
        else
        {
            keyText = content.Substring(0, separatorIndex).Trim();
            valueText = content.Substring(FindValueStartIndex(content, separatorIndex)).Trim();
        }

        string key = UnescapeKeyPath(keyText);
        if (key.Length == 0)
        {
            diagnostics.AddParseError(sourceName, "", "missing properties key", lineNumber, FirstContentColumn(rawLine));
            return;
        }

        string path = ConfigPath.Normalize(key, ".");
        ConfigValue value = ConfigValue.String(Unescape(valueText)).SetSource(sourceName, precedenceRank, path, lineNumber, FirstContentColumn(rawLine));
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

        diagnostics.AddParseError(sourceName, path, "duplicate properties key", lineNumber, 1);
    }

    internal static List<ConfigPropertiesLine> LogicalLines(string text)
    {
        string[] physicalLines = ConfigIniParser.NormalizeNewLines(text).Split('\n');
        List<ConfigPropertiesLine> logicalLines = new List<ConfigPropertiesLine>();
        StringBuilder builder = new StringBuilder();
        int startLineNumber = 1;
        bool continuation = false;

        int index = 0;
        while (index < physicalLines.Length)
        {
            string line = physicalLines[index];
            if (!continuation)
            {
                builder.Clear();
                startLineNumber = index + 1;
            }
            else
            {
                line = TrimLeadingWhitespace(line);
            }

            if (EndsWithContinuation(line))
            {
                builder.Append(line.Substring(0, line.Length - 1));
                continuation = true;
            }
            else
            {
                builder.Append(line);
                logicalLines.Add(new ConfigPropertiesLine(builder.ToString(), startLineNumber));
                continuation = false;
            }

            index = index + 1;
        }

        if (continuation)
        {
            logicalLines.Add(new ConfigPropertiesLine(builder.ToString(), startLineNumber));
        }

        return logicalLines;
    }

    internal static bool EndsWithContinuation(string line)
    {
        int slashCount = 0;
        int index = line.Length - 1;
        while (index >= 0 && line[index] == '\\')
        {
            slashCount = slashCount + 1;
            index = index - 1;
        }

        return slashCount % 2 == 1;
    }

    internal static bool HasMalformedUnicodeEscape(string line)
    {
        int slashCount = 0;
        int index = 0;
        while (index < line.Length)
        {
            char current = line[index];
            if (current == '\\')
            {
                slashCount = slashCount + 1;
                index = index + 1;
                continue;
            }

            if (current == 'u' && slashCount % 2 == 1 && !CanReadUnicode(line, index + 1))
            {
                return true;
            }

            slashCount = 0;
            index = index + 1;
        }

        return false;
    }

    internal static int FindSeparatorIndex(string line)
    {
        int index = 0;
        while (index < line.Length)
        {
            if (line[index] == '\\')
            {
                index = index + 2;
                continue;
            }

            if (line[index] == '=' || line[index] == ':')
            {
                return index;
            }

            if (line[index] == ' ' || line[index] == '\t' || line[index] == '\f')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int FindValueStartIndex(string line, int separatorIndex)
    {
        int index = separatorIndex;

        if (index < line.Length && (line[index] == '=' || line[index] == ':'))
        {
            index = index + 1;
        }

        while (index < line.Length && (line[index] == ' ' || line[index] == '\t' || line[index] == '\f'))
        {
            index = index + 1;
        }

        if (index < line.Length && (line[index] == '=' || line[index] == ':'))
        {
            index = index + 1;
        }

        while (index < line.Length && (line[index] == ' ' || line[index] == '\t' || line[index] == '\f'))
        {
            index = index + 1;
        }

        return index;
    }

    internal static bool IsComment(string trimmedLine)
    {
        if (trimmedLine.StartsWith("#"))
        {
            return true;
        }

        return trimmedLine.StartsWith("!");
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
                if (next == 't')
                {
                    builder.Append('\t');
                    index = index + 2;
                    continue;
                }

                if (next == 'n')
                {
                    builder.Append('\n');
                    index = index + 2;
                    continue;
                }

                if (next == 'r')
                {
                    builder.Append('\r');
                    index = index + 2;
                    continue;
                }

                if (next == 'f')
                {
                    builder.Append('\f');
                    index = index + 2;
                    continue;
                }

                if (next == 'u' && CanReadUnicode(value, index + 2))
                {
                    builder.Append((char)ReadUnicode(value, index + 2));
                    index = index + 6;
                    continue;
                }

                builder.Append(next);
                index = index + 2;
                continue;
            }

            builder.Append(value[index]);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string UnescapeKeyPath(string value)
    {
        StringBuilder builder = new StringBuilder();

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\\' && index + 1 < value.Length)
            {
                char next = value[index + 1];
                if (next == '.')
                {
                    builder.Append("\\.");
                    index = index + 2;
                    continue;
                }

                if (next == 't')
                {
                    builder.Append('\t');
                    index = index + 2;
                    continue;
                }

                if (next == 'n')
                {
                    builder.Append('\n');
                    index = index + 2;
                    continue;
                }

                if (next == 'r')
                {
                    builder.Append('\r');
                    index = index + 2;
                    continue;
                }

                if (next == 'f')
                {
                    builder.Append('\f');
                    index = index + 2;
                    continue;
                }

                if (next == 'u' && CanReadUnicode(value, index + 2))
                {
                    builder.Append((char)ReadUnicode(value, index + 2));
                    index = index + 6;
                    continue;
                }

                builder.Append(next);
                index = index + 2;
                continue;
            }

            builder.Append(value[index]);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static bool CanReadUnicode(string value, int index)
    {
        if (index < 0 || index > value.Length)
        {
            return false;
        }

        if (4 > value.Length - index)
        {
            return false;
        }

        int offset = 0;
        while (offset < 4)
        {
            if (HexValue(value[index + offset]) < 0)
            {
                return false;
            }

            offset = offset + 1;
        }

        return true;
    }

    internal static int ReadUnicode(string value, int index)
    {
        int result = 0;
        int offset = 0;
        while (offset < 4)
        {
            result = result * 16 + HexValue(value[index + offset]);
            offset = offset + 1;
        }

        return result;
    }

    internal static int HexValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }

        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        return -1;
    }

    internal static string TrimLeadingWhitespace(string value)
    {
        int index = 0;
        while (index < value.Length && (value[index] == ' ' || value[index] == '\t' || value[index] == '\f'))
        {
            index = index + 1;
        }

        return value.Substring(index);
    }

    internal static int FirstContentColumn(string line)
    {
        return ConfigIniParser.FirstContentColumn(line);
    }
}
