using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace config;

internal class ConfigJsonParser
{
    internal static ConfigValue ParseDefault(string text)
    {
        return Parse(text, "json", 0);
    }

    internal static ConfigValue Parse(string text, string sourceName, int precedenceRank)
    {
        return ParseWithCaseMode(text, sourceName, precedenceRank, ConfigCaseMode.Sensitive);
    }

    internal static ConfigValue ParseWithCaseMode(string text, string sourceName, int precedenceRank, ConfigCaseMode caseMode)
    {
        JsonDocument document = JsonDocument.Parse(text);
        ConfigValue value = FromElement(document.RootElement, sourceName, precedenceRank, "", caseMode);
        document.Dispose();
        return value;
    }

    internal static ConfigValue ParseWithDiagnostics(string text, string sourceName, int precedenceRank, ConfigDuplicateKeyMode duplicateKeyMode, ConfigDiagnostics diagnostics)
    {
        Task<JsonDocument> parseTask = Task.Run(() => JsonDocument.Parse(text));
        SpinWait wait = new SpinWait();
        while (!parseTask.IsCompleted)
        {
            wait.SpinOnce();
        }

        if (parseTask.IsFaulted)
        {
            int lineNumber = 0;
            int columnNumber = 0;
            AggregateException? aggregate = parseTask.Exception;
            if (aggregate != null)
            {
                Exception failure = aggregate.GetBaseException();
                JsonException? jsonFailure = failure as JsonException;
                if (jsonFailure != null)
                {
                    if (jsonFailure.LineNumber.HasValue)
                    {
                        lineNumber = (int)jsonFailure.LineNumber.GetValueOrDefault() + 1;
                    }

                    if (jsonFailure.BytePositionInLine.HasValue)
                    {
                        columnNumber = (int)jsonFailure.BytePositionInLine.GetValueOrDefault() + 1;
                    }
                }
            }

            diagnostics.AddParseError(sourceName, "", "malformed JSON input", lineNumber, columnNumber);
            return ConfigValue.Object(new ConfigMap()).SetSource(sourceName, precedenceRank, "", 0, 0);
        }

        JsonDocument document = parseTask.Result;
        ConfigValue value = FromElementWithPolicy(document.RootElement, sourceName, precedenceRank, "", ConfigCaseMode.Sensitive, duplicateKeyMode, diagnostics);
        document.Dispose();
        return value;
    }

    internal static ConfigValue FromElementWithPolicy(JsonElement element, string sourceName, int precedenceRank, string path, ConfigCaseMode caseMode, ConfigDuplicateKeyMode duplicateKeyMode, ConfigDiagnostics diagnostics)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            return FromObjectWithPolicy(element, sourceName, precedenceRank, path, caseMode, duplicateKeyMode, diagnostics);
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            return FromArrayWithPolicy(element, sourceName, precedenceRank, path, caseMode, duplicateKeyMode, diagnostics);
        }

        return FromElement(element, sourceName, precedenceRank, path, caseMode);
    }

    internal static ConfigValue FromObjectWithPolicy(JsonElement element, string sourceName, int precedenceRank, string path, ConfigCaseMode caseMode, ConfigDuplicateKeyMode duplicateKeyMode, ConfigDiagnostics diagnostics)
    {
        ConfigMap map = ConfigMap.Create(caseMode);

        foreach (JsonProperty property in element.EnumerateObject())
        {
            string childPath = ChildPath(path, property.Name);
            ConfigValue value = FromElementWithPolicy(property.Value, sourceName, precedenceRank, childPath, caseMode, duplicateKeyMode, diagnostics);
            ConfigValue existing = map.Get(property.Name);
            if (existing.IsMissing())
            {
                map.Set(property.Name, value);
            }
            else if (duplicateKeyMode == ConfigDuplicateKeyMode.LastWins)
            {
                map.Set(property.Name, value);
            }
            else if (duplicateKeyMode == ConfigDuplicateKeyMode.MergeObjects && existing.Kind == ConfigValueKind.Object && value.Kind == ConfigValueKind.Object)
            {
                MergeObjectValues(existing, value, childPath, sourceName, duplicateKeyMode, diagnostics);
            }
            else
            {
                diagnostics.AddParseError(sourceName, childPath, "duplicate JSON key", 0, 0);
            }
        }

        return ConfigValue.Object(map).SetSource(sourceName, precedenceRank, path, 0, 0);
    }

    internal static ConfigValue FromArrayWithPolicy(JsonElement element, string sourceName, int precedenceRank, string path, ConfigCaseMode caseMode, ConfigDuplicateKeyMode duplicateKeyMode, ConfigDiagnostics diagnostics)
    {
        ConfigArray array = new ConfigArray();
        int index = 0;

        foreach (JsonElement child in element.EnumerateArray())
        {
            string childPath = ChildPath(path, index.ToString());
            array.Add(FromElementWithPolicy(child, sourceName, precedenceRank, childPath, caseMode, duplicateKeyMode, diagnostics));
            index = index + 1;
        }

        return ConfigValue.Array(array).SetSource(sourceName, precedenceRank, path, 0, 0);
    }

    internal static void MergeObjectValues(ConfigValue existing, ConfigValue later, string path, string sourceName, ConfigDuplicateKeyMode duplicateKeyMode, ConfigDiagnostics diagnostics)
    {
        if (existing.ObjectValue == null || later.ObjectValue == null)
        {
            diagnostics.AddParseError(sourceName, path, "duplicate JSON key", 0, 0);
            return;
        }

        string[] keys = later.ObjectValue.Keys();
        int index = 0;
        while (index < keys.Length)
        {
            string key = keys[index];
            string childPath = ChildPath(path, key);
            ConfigValue existingChild = existing.ObjectValue.Get(key);
            ConfigValue laterChild = later.ObjectValue.Get(key);
            if (existingChild.IsMissing())
            {
                existing.ObjectValue.Set(key, laterChild);
            }
            else if (existingChild.Kind == ConfigValueKind.Object && laterChild.Kind == ConfigValueKind.Object)
            {
                MergeObjectValues(existingChild, laterChild, childPath, sourceName, duplicateKeyMode, diagnostics);
            }
            else
            {
                diagnostics.AddParseError(sourceName, childPath, "duplicate JSON key", 0, 0);
            }

            index = index + 1;
        }
    }

    internal static ConfigValue FromElement(JsonElement element, string sourceName, int precedenceRank, string path, ConfigCaseMode caseMode)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            return FromObject(element, sourceName, precedenceRank, path, caseMode);
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            return FromArray(element, sourceName, precedenceRank, path, caseMode);
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            string? text = element.GetString();
            if (text == null)
            {
                text = "";
            }

            return ConfigValue.String(text).SetSource(sourceName, precedenceRank, path, 0, 0);
        }

        if (element.ValueKind == JsonValueKind.Number)
        {
            return ConfigValue.Number(element.GetDecimal()).SetSource(sourceName, precedenceRank, path, 0, 0);
        }

        if (element.ValueKind == JsonValueKind.True)
        {
            return ConfigValue.Boolean(true).SetSource(sourceName, precedenceRank, path, 0, 0);
        }

        if (element.ValueKind == JsonValueKind.False)
        {
            return ConfigValue.Boolean(false).SetSource(sourceName, precedenceRank, path, 0, 0);
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            return ConfigValue.Null().SetSource(sourceName, precedenceRank, path, 0, 0);
        }

        return ConfigValue.Missing().SetSource(sourceName, precedenceRank, path, 0, 0);
    }

    internal static ConfigValue FromObject(JsonElement element, string sourceName, int precedenceRank, string path, ConfigCaseMode caseMode)
    {
        ConfigMap map = ConfigMap.Create(caseMode);

        foreach (JsonProperty property in element.EnumerateObject())
        {
            string childPath = ChildPath(path, property.Name);
            map.Set(property.Name, FromElement(property.Value, sourceName, precedenceRank, childPath, caseMode));
        }

        return ConfigValue.Object(map).SetSource(sourceName, precedenceRank, path, 0, 0);
    }

    internal static ConfigValue FromArray(JsonElement element, string sourceName, int precedenceRank, string path, ConfigCaseMode caseMode)
    {
        ConfigArray array = new ConfigArray();
        int index = 0;

        foreach (JsonElement child in element.EnumerateArray())
        {
            string childPath = ChildPath(path, index.ToString());
            array.Add(FromElement(child, sourceName, precedenceRank, childPath, caseMode));
            index = index + 1;
        }

        return ConfigValue.Array(array).SetSource(sourceName, precedenceRank, path, 0, 0);
    }

    internal static string ChildPath(string parentPath, string childSegment)
    {
        string escaped = ConfigPath.EscapeSegment(childSegment, ".");
        if (parentPath.Length == 0)
        {
            return escaped;
        }

        return parentPath + "." + escaped;
    }
}
