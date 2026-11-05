using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CountryCodes.DataGenerator.Parsers;

internal static class DebianIsoJsonParser
{
    private static readonly HashSet<string> CountryProperties = new HashSet<string>(
        new[] { "alpha_2", "alpha_3", "common_name", "flag", "name", "numeric", "official_name" },
        StringComparer.Ordinal);

    private static readonly HashSet<string> SubdivisionProperties = new HashSet<string>(
        new[] { "code", "name", "parent", "type" },
        StringComparer.Ordinal);

    private static readonly HashSet<string> HistoricalProperties = new HashSet<string>(
        new[] { "alpha_2", "alpha_3", "alpha_4", "comment", "name", "numeric", "withdrawal_date" },
        StringComparer.Ordinal);

    internal static IReadOnlyList<DebianCountryRecord> ParseCountries(
        Stream input,
        string sourceName)
    {
        using JsonDocument document = ParseDocument(input, sourceName);
        JsonElement rows = ReadSingleArray(document.RootElement, "3166-1", sourceName);
        List<DebianCountryRecord> records = new List<DebianCountryRecord>();
        int index = 0;
        foreach (JsonElement row in rows.EnumerateArray())
        {
            string context = $"3166-1[{index}]";
            EnsureObject(row, sourceName, context);
            EnsureKnownProperties(row, CountryProperties, sourceName, context);
            records.Add(new DebianCountryRecord(
                ReadRequiredString(row, "alpha_2", sourceName, context),
                ReadRequiredString(row, "alpha_3", sourceName, context),
                ReadRequiredString(row, "numeric", sourceName, context),
                ReadRequiredString(row, "name", sourceName, context),
                ReadOptionalString(row, "official_name", sourceName, context),
                ReadOptionalString(row, "common_name", sourceName, context)));
            index += 1;
        }

        EnsureNotEmpty(records.Count, sourceName, "3166-1");
        return records;
    }

    internal static IReadOnlyList<DebianSubdivisionRecord> ParseSubdivisions(
        Stream input,
        string sourceName)
    {
        using JsonDocument document = ParseDocument(input, sourceName);
        JsonElement rows = ReadSingleArray(document.RootElement, "3166-2", sourceName);
        List<DebianSubdivisionRecord> records = new List<DebianSubdivisionRecord>();
        int index = 0;
        foreach (JsonElement row in rows.EnumerateArray())
        {
            string context = $"3166-2[{index}]";
            EnsureObject(row, sourceName, context);
            EnsureKnownProperties(row, SubdivisionProperties, sourceName, context);
            records.Add(new DebianSubdivisionRecord(
                ReadRequiredString(row, "code", sourceName, context),
                ReadRequiredString(row, "name", sourceName, context),
                ReadRequiredString(row, "type", sourceName, context),
                ReadOptionalString(row, "parent", sourceName, context)));
            index += 1;
        }

        EnsureNotEmpty(records.Count, sourceName, "3166-2");
        return records;
    }

    internal static IReadOnlyList<DebianHistoricalRecord> ParseHistoricalCountries(
        Stream input,
        string sourceName)
    {
        using JsonDocument document = ParseDocument(input, sourceName);
        JsonElement rows = ReadSingleArray(document.RootElement, "3166-3", sourceName);
        List<DebianHistoricalRecord> records = new List<DebianHistoricalRecord>();
        int index = 0;
        foreach (JsonElement row in rows.EnumerateArray())
        {
            string context = $"3166-3[{index}]";
            EnsureObject(row, sourceName, context);
            EnsureKnownProperties(row, HistoricalProperties, sourceName, context);
            records.Add(new DebianHistoricalRecord(
                ReadRequiredString(row, "alpha_2", sourceName, context),
                ReadRequiredString(row, "alpha_3", sourceName, context),
                ReadRequiredString(row, "alpha_4", sourceName, context),
                ReadRequiredString(row, "name", sourceName, context),
                ReadOptionalString(row, "numeric", sourceName, context),
                ReadOptionalString(row, "withdrawal_date", sourceName, context),
                ReadOptionalString(row, "comment", sourceName, context)));
            index += 1;
        }

        EnsureNotEmpty(records.Count, sourceName, "3166-3");
        return records;
    }

    private static JsonDocument ParseDocument(Stream input, string sourceName)
    {
        try
        {
            return JsonDocument.Parse(input, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 32
            });
        }
        catch (JsonException exception)
        {
            throw new SourceParseException(sourceName, "invalid JSON", exception);
        }
    }

    private static JsonElement ReadSingleArray(
        JsonElement root,
        string expectedProperty,
        string sourceName)
    {
        EnsureObject(root, sourceName, "root");
        int propertyCount = 0;
        JsonElement result = default;
        foreach (JsonProperty property in root.EnumerateObject())
        {
            propertyCount += 1;
            if (!string.Equals(property.Name, expectedProperty, StringComparison.Ordinal))
            {
                throw new SourceParseException(
                    sourceName,
                    $"unexpected root property '{property.Name}'; expected '{expectedProperty}'");
            }

            result = property.Value;
        }

        if (propertyCount != 1 || result.ValueKind != JsonValueKind.Array)
        {
            throw new SourceParseException(
                sourceName,
                $"root must contain exactly one array property named '{expectedProperty}'");
        }

        return result;
    }

    private static void EnsureObject(JsonElement value, string sourceName, string context)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new SourceParseException(sourceName, $"{context} must be an object");
        }
    }

    private static void EnsureKnownProperties(
        JsonElement row,
        HashSet<string> allowedProperties,
        string sourceName,
        string context)
    {
        foreach (JsonProperty property in row.EnumerateObject())
        {
            if (!allowedProperties.Contains(property.Name))
            {
                throw new SourceParseException(
                    sourceName,
                    $"{context} contains unexpected property '{property.Name}'");
            }
        }
    }

    private static string ReadRequiredString(
        JsonElement row,
        string propertyName,
        string sourceName,
        string context)
    {
        if (!row.TryGetProperty(propertyName, out JsonElement value) ||
            value.ValueKind != JsonValueKind.String)
        {
            throw new SourceParseException(
                sourceName,
                $"{context}.{propertyName} must be a string");
        }

        string? text = value.GetString();
        if (string.IsNullOrEmpty(text))
        {
            throw new SourceParseException(
                sourceName,
                $"{context}.{propertyName} must not be empty");
        }

        return text;
    }

    private static string? ReadOptionalString(
        JsonElement row,
        string propertyName,
        string sourceName,
        string context)
    {
        if (!row.TryGetProperty(propertyName, out JsonElement value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw new SourceParseException(
                sourceName,
                $"{context}.{propertyName} must be a string when present");
        }

        string? text = value.GetString();
        if (string.IsNullOrEmpty(text))
        {
            throw new SourceParseException(
                sourceName,
                $"{context}.{propertyName} must not be empty when present");
        }

        return text;
    }

    private static void EnsureNotEmpty(int count, string sourceName, string arrayName)
    {
        if (count == 0)
        {
            throw new SourceParseException(sourceName, $"{arrayName} must contain at least one row");
        }
    }
}
