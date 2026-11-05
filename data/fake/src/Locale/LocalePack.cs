using System;
using System.Collections.Generic;
using System.Text.Json;

namespace fake;

internal class LocalePackProvenance
{
    internal string Id = string.Empty;
    internal string Source = string.Empty;
    internal string SourceVersion = string.Empty;
    internal string License = string.Empty;
    internal string Reviewer = string.Empty;
}

internal class LocalePackField
{
    internal string Name = string.Empty;
    internal string Provenance = string.Empty;
    internal string ValueJson = string.Empty;
}

internal class LocalePack
{
    internal const int CurrentSchemaVersion = 1;

    internal int SchemaVersion;
    internal string DataVersion = string.Empty;
    internal string Locale = string.Empty;
    internal string[] Fallbacks = new string[0];
    internal string ReviewStatus = string.Empty;
    internal LocalePackProvenance[] Provenance = new LocalePackProvenance[0];
    internal LocalePackField[] Fields = new LocalePackField[0];
    internal string? ResourceName;

    internal LocalePack()
    {
        SchemaVersion = CurrentSchemaVersion;
        DataVersion = string.Empty;
        Locale = string.Empty;
        Fallbacks = new string[0];
        ReviewStatus = string.Empty;
        Provenance = new LocalePackProvenance[0];
        Fields = new LocalePackField[0];
        ResourceName = null;
    }

    internal static LocalePack Parse(
        string json,
        int maximumOutputSize,
        int maximumCount)
    {
        return Parse(json, maximumOutputSize, maximumCount, null);
    }

    internal static LocalePack Parse(
        string json,
        int maximumOutputSize,
        int maximumCount,
        string? resourceName)
    {
        Guard.NotNullOrEmpty(json, "json");
        Guard.OutputSize(json.Length, maximumOutputSize, "json");
        Guard.Positive(maximumCount, "maximumCount");
        if (resourceName != null)
        {
            Guard.NotNullOrWhiteSpace(resourceName, "resourceName");
            LocaleJsonSyntax.Validate(json, resourceName);
        }
        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;
            RequireObject(root, "$", "locale pack");
            ValidateProperties(
                root,
                "$",
                new string[]
                {
                    "schemaVersion",
                    "dataVersion",
                    "locale",
                    "fallbacks",
                    "reviewStatus",
                    "provenance",
                    "fields"
                });
            LocalePack pack = new LocalePack();
            pack.ResourceName = resourceName;
            pack.SchemaVersion = ParseSchemaVersion(Required(root, "schemaVersion", "$"));
            pack.DataVersion = NonemptyString(Required(root, "dataVersion", "$"), "$.dataVersion");
            pack.Locale = CanonicalLocale(Required(root, "locale", "$"), "$.locale", true);
            pack.Fallbacks = ParseFallbacks(
                Required(root, "fallbacks", "$"),
                pack.Locale,
                maximumCount);
            pack.ReviewStatus = NonemptyString(Required(root, "reviewStatus", "$"), "$.reviewStatus");
            HashSet<string> provenanceIds = new HashSet<string>(StringComparer.Ordinal);
            pack.Provenance = ParseProvenance(
                Required(root, "provenance", "$"),
                provenanceIds,
                maximumCount);
            pack.Fields = ParseFields(
                Required(root, "fields", "$"),
                provenanceIds,
                maximumCount);
            return pack;
        }
    }

    static int ParseSchemaVersion(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number || value.GetRawText() != "1")
        {
            Violation("$.schemaVersion", $"must be the integer {CurrentSchemaVersion}");
        }
        return CurrentSchemaVersion;
    }

    static string[] ParseFallbacks(
        JsonElement value,
        string locale,
        int maximumCount)
    {
        RequireArray(value, "$.fallbacks", "fallbacks");
        ValidateCount(value.GetArrayLength(), maximumCount, "$.fallbacks");
        List<string> fallbacks = new List<string>();
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            string path = $"$.fallbacks[{index}]";
            string fallback = CanonicalLocale(item, path, true);
            if (fallback == locale)
            {
                Violation(path, "must not repeat the pack locale");
            }
            if (!seen.Add(fallback))
            {
                Violation(path, $"duplicates fallback '{fallback}'");
            }
            fallbacks.Add(fallback);
            index = index + 1;
        }
        if (locale == LocaleFieldValue.InvariantLocale && fallbacks.Count != 0)
        {
            Violation("$.fallbacks", "must be empty for the invariant pack");
        }
        return fallbacks.ToArray();
    }

    static LocalePackProvenance[] ParseProvenance(
        JsonElement value,
        HashSet<string> ids,
        int maximumCount)
    {
        RequireArray(value, "$.provenance", "provenance");
        ValidateCount(value.GetArrayLength(), maximumCount, "$.provenance");
        if (value.GetArrayLength() == 0)
        {
            Violation("$.provenance", "must contain at least one provenance record");
        }
        List<LocalePackProvenance> result = new List<LocalePackProvenance>();
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            string path = $"$.provenance[{index}]";
            RequireObject(item, path, "provenance record");
            ValidateProperties(
                item,
                path,
                new string[] { "id", "source", "sourceVersion", "license", "reviewer" });
            LocalePackProvenance record = new LocalePackProvenance();
            record.Id = NonemptyString(Required(item, "id", path), path + ".id");
            record.Source = NonemptyString(Required(item, "source", path), path + ".source");
            record.SourceVersion = NonemptyString(
                Required(item, "sourceVersion", path),
                path + ".sourceVersion");
            record.License = NonemptyString(Required(item, "license", path), path + ".license");
            record.Reviewer = NonemptyString(Required(item, "reviewer", path), path + ".reviewer");
            ValidateIdentifier(record.Id, path + ".id");
            if (!ids.Add(record.Id))
            {
                Violation(path + ".id", $"duplicates provenance id '{record.Id}'");
            }
            result.Add(record);
            index = index + 1;
        }
        return result.ToArray();
    }

    static LocalePackField[] ParseFields(
        JsonElement value,
        HashSet<string> provenanceIds,
        int maximumCount)
    {
        RequireArray(value, "$.fields", "fields");
        ValidateCount(value.GetArrayLength(), maximumCount, "$.fields");
        if (value.GetArrayLength() == 0)
        {
            Violation("$.fields", "must contain at least one field");
        }
        List<LocalePackField> result = new List<LocalePackField>();
        HashSet<string> fieldNames = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            string path = $"$.fields[{index}]";
            RequireObject(item, path, "field record");
            ValidateProperties(item, path, new string[] { "name", "provenance", "value" });
            LocalePackField field = new LocalePackField();
            field.Name = NonemptyString(Required(item, "name", path), path + ".name");
            field.Provenance = NonemptyString(
                Required(item, "provenance", path),
                path + ".provenance");
            ValidateFieldName(field.Name, path + ".name");
            if (!fieldNames.Add(field.Name))
            {
                Violation(path + ".name", $"duplicates field '{field.Name}'");
            }
            if (!provenanceIds.Contains(field.Provenance))
            {
                Violation(
                    path + ".provenance",
                    $"references unknown provenance id '{field.Provenance}'");
            }
            JsonElement fieldValue = Required(item, "value", path);
            ValidateValue(fieldValue, path + ".value", maximumCount);
            field.ValueJson = fieldValue.GetRawText();
            result.Add(field);
            index = index + 1;
        }
        return result.ToArray();
    }

    static void ValidateValue(JsonElement value, string path, int maximumCount)
    {
        if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
        {
            Violation(path, "must not be null");
        }
        if (value.ValueKind == JsonValueKind.Array)
        {
            ValidateCount(value.GetArrayLength(), maximumCount, path);
            int index = 0;
            foreach (JsonElement item in value.EnumerateArray())
            {
                ValidateValue(item, $"{path}[{index}]", maximumCount);
                index = index + 1;
            }
        }
        else if (value.ValueKind == JsonValueKind.Object)
        {
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            int count = 0;
            foreach (JsonProperty property in value.EnumerateObject())
            {
                count = count + 1;
                ValidateCount(count, maximumCount, path);
                if (!names.Add(property.Name))
                {
                    Violation(path + "." + property.Name, $"duplicates member '{property.Name}'");
                }
                ValidateValue(property.Value, path + "." + property.Name, maximumCount);
            }
        }
    }

    static string CanonicalLocale(JsonElement value, string path, bool allowInvariant)
    {
        string locale = NonemptyString(value, path);
        if (allowInvariant && locale == LocaleFieldValue.InvariantLocale)
        {
            return locale;
        }
        LanguageTag tag = LanguageTagParser.Parse(locale, "json");
        if (tag.Normalized != locale)
        {
            Violation(path, $"must use canonical tag '{tag.Normalized}'");
        }
        return locale;
    }

    static void ValidateIdentifier(string value, string path)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool valid = character >= 'a' && character <= 'z';
            valid = valid || character >= '0' && character <= '9';
            valid = valid || character == '-';
            if (!valid || index == 0 && character == '-' || index == value.Length - 1 && character == '-')
            {
                Violation(path, "must contain lowercase ASCII letters, digits, and internal hyphens only");
            }
            index = index + 1;
        }
    }

    static void ValidateFieldName(string value, string path)
    {
        int index = 0;
        bool segmentStart = true;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '.')
            {
                if (segmentStart || index == value.Length - 1)
                {
                    Violation(path, "must use nonempty dot-separated segments");
                }
                segmentStart = true;
            }
            else
            {
                bool letter = character >= 'a' && character <= 'z';
                bool digit = character >= '0' && character <= '9';
                bool hyphen = character == '-';
                if (!letter && !digit && !hyphen)
                {
                    Violation(path, "must contain lowercase ASCII letters, digits, dots, and hyphens only");
                }
                if (segmentStart && !letter)
                {
                    Violation(path, "segments must begin with a lowercase ASCII letter");
                }
                segmentStart = false;
            }
            index = index + 1;
        }
    }

    static void ValidateProperties(JsonElement value, string path, string[] allowed)
    {
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (!seen.Add(property.Name))
            {
                Violation(path + "." + property.Name, $"duplicates member '{property.Name}'");
            }
            if (!Allowed(property.Name, allowed))
            {
                Violation(path + "." + property.Name, $"is not allowed by locale-pack schema version {CurrentSchemaVersion}");
            }
        }
    }

    static bool Allowed(string name, string[] allowed)
    {
        int index = 0;
        while (index < allowed.Length)
        {
            if (name == allowed[index])
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static JsonElement Required(JsonElement value, string name, string path)
    {
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Name == name)
            {
                return property.Value;
            }
        }
        Violation(path, $"requires member '{name}'");
        return default(JsonElement);
    }

    static string NonemptyString(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            Violation(path, "must be a string");
        }
        string? result = value.GetString();
        if (string.IsNullOrWhiteSpace(result))
        {
            Violation(path, "must not be empty or whitespace");
        }
        return result ?? throw FakeException.Create(path + " must not be null");
    }

    static void RequireObject(JsonElement value, string path, string description)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            Violation(path, $"must be a JSON object containing the {description}");
        }
    }

    static void RequireArray(JsonElement value, string path, string description)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            Violation(path, $"must be a JSON array containing {description}");
        }
    }

    static void ValidateCount(int count, int maximumCount, string path)
    {
        if (count > maximumCount)
        {
            Violation(path, $"contains {count} items, exceeding the configured maximum of {maximumCount}");
        }
    }

    static void Violation(string path, string message)
    {
        throw FakeException.Create($"Locale pack schema violation at '{path}': {message}.");
    }
}
