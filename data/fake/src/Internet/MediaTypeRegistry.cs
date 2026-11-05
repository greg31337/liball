using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace fake;

internal class MediaTypeRegistry
{
    internal string SourceVersion = string.Empty;
    string[] Values;
    string[] TopLevelTypes;
    HashSet<string> Lookup;

    internal MediaTypeRegistry(
        string sourceVersion,
        string[] values,
        string[] topLevelTypes)
    {
        Guard.NotNullOrWhiteSpace(sourceVersion, "sourceVersion");
        Guard.NotNull(values, "values");
        Guard.NotNull(topLevelTypes, "topLevelTypes");
        if (values.Length == 0 || values.Length != topLevelTypes.Length)
        {
            throw FakeException.Create("Bundled IANA media-type metadata contains no valid records.");
        }
        SourceVersion = sourceVersion;
        Values = new string[values.Length];
        TopLevelTypes = new string[topLevelTypes.Length];
        Array.Copy(values, Values, values.Length);
        Array.Copy(topLevelTypes, TopLevelTypes, topLevelTypes.Length);
        Lookup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int index = 0;
        while (index < Values.Length)
        {
            Lookup.Add(Values[index]);
            index = index + 1;
        }
    }

    internal string Choose(
        FakeRandom random,
        string? topLevelType,
        int maximumOutputSize)
    {
        string? normalized = NormalizeTopLevelType(topLevelType);
        int count = 0;
        int index = 0;
        while (index < Values.Length)
        {
            if (normalized == null || TopLevelTypes[index] == normalized)
            {
                count = count + 1;
            }
            index = index + 1;
        }
        if (count == 0)
        {
            throw new ArgumentException(
                $"topLevelType '{topLevelType}' has no registered media types.",
                "topLevelType");
        }
        int selected = random.IntFromMaximumExclusive(count);
        index = 0;
        while (index < Values.Length)
        {
            if (normalized == null || TopLevelTypes[index] == normalized)
            {
                if (selected == 0)
                {
                    string result = Values[index];
                    Guard.OutputSize(result.Length, maximumOutputSize, "mimeType");
                    return result;
                }
                selected = selected - 1;
            }
            index = index + 1;
        }
        throw FakeException.Create("An IANA media type could not be selected.");
    }

    internal bool Contains(string value)
    {
        Guard.NotNullOrWhiteSpace(value, "value");
        return Lookup.Contains(value);
    }

    internal string[] Copy(int maximumCount, int maximumOutputSize)
    {
        Guard.Count(Values.Length, maximumCount, "mimeTypes");
        string[] result = new string[Values.Length];
        Array.Copy(Values, result, Values.Length);
        int total = 0;
        int index = 0;
        while (index < result.Length)
        {
            total = total + result[index].Length;
            index = index + 1;
        }
        Guard.OutputSize(total, maximumOutputSize, "mimeTypes");
        return result;
    }

    static string? NormalizeTopLevelType(string? value)
    {
        if (value == null)
        {
            return null;
        }
        Guard.NotNullOrWhiteSpace(value, "topLevelType");
        string normalized = value.ToLowerInvariant();
        if (!Token(normalized))
        {
            throw new ArgumentException(
                "topLevelType must be an HTTP token without a slash.",
                "topLevelType");
        }
        return normalized;
    }

    internal static bool Token(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool valid = character >= 'a' && character <= 'z' ||
                character >= 'A' && character <= 'Z' ||
                character >= '0' && character <= '9' ||
                character == '!' || character == '#' ||
                character == '$' || character == '%' ||
                character == '&' || character == '\'' ||
                character == '*' || character == '+' ||
                character == '-' || character == '.' ||
                character == '^' || character == '_' ||
                character == '`' || character == '|' ||
                character == '~';
            if (!valid)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }
}

internal static class MediaTypeRegistryStore
{
    static readonly MediaTypeRegistry?[] Value = new MediaTypeRegistry?[1];
    static readonly object Sync = new object();

    internal static MediaTypeRegistry Current()
    {
        lock (Sync)
        {
            MediaTypeRegistry? current = Value[0];
            if (current == null)
            {
                current = Load();
                Value[0] = current;
            }
            return current;
        }
    }

    static MediaTypeRegistry Load()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream? stream = assembly.GetManifestResourceStream(
            "fake.Data.standards.media-types.json");
        if (stream == null)
        {
            throw FakeException.Create("Embedded IANA media-type metadata resource is missing.");
        }
        using (stream)
        {
            using (JsonDocument document = JsonDocument.Parse(stream))
            {
                JsonElement root = document.RootElement;
                string sourceVersion = JsonText.Required(
                    root.GetProperty("source").GetProperty("version"),
                    "$.source.version");
                List<string> values = new List<string>();
                List<string> topLevelTypes = new List<string>();
                HashSet<string> added = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);
                foreach (JsonElement item in root.GetProperty("records").EnumerateArray())
                {
                    string type = JsonText.Required(item.GetProperty("type"), "$.records[].type");
                    string subtype = JsonText.Required(item.GetProperty("subtype"), "$.records[].subtype");
                    string mediaType = JsonText.Required(item.GetProperty("mediaType"), "$.records[].mediaType");
                    if (TokenPair(type, subtype, mediaType) && added.Add(mediaType))
                    {
                        values.Add(mediaType);
                        topLevelTypes.Add(type.ToLowerInvariant());
                    }
                }
                return new MediaTypeRegistry(
                    sourceVersion,
                    values.ToArray(),
                    topLevelTypes.ToArray());
            }
        }
    }

    static bool TokenPair(string type, string subtype, string mediaType)
    {
        return MediaTypeRegistry.Token(type) &&
            MediaTypeRegistry.Token(subtype) &&
            mediaType == type + "/" + subtype;
    }
}
