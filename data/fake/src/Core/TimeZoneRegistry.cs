using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace fake;

internal class TimeZoneRegistryEntry
{
    internal string[] CountryCodes = new string[0];
    internal string Coordinates = string.Empty;
    internal string IanaId = string.Empty;
    internal string? Comment;
}

internal class TimeZoneRegistry
{
    internal string SourceVersion = string.Empty;
    TimeZoneRegistryEntry[] Entries;
    Dictionary<string, TimeZoneRegistryEntry> ById;

    internal TimeZoneRegistry(
        string sourceVersion,
        TimeZoneRegistryEntry[] entries)
    {
        SourceVersion = sourceVersion;
        Entries = entries;
        ById = new Dictionary<string, TimeZoneRegistryEntry>(StringComparer.Ordinal);
        int index = 0;
        while (index < entries.Length)
        {
            if (ById.ContainsKey(entries[index].IanaId))
            {
                throw FakeException.Create($"Bundled time-zone metadata contains duplicate IANA identifier '{entries[index].IanaId}'.");
            }
            ById[entries[index].IanaId] = entries[index];
            index++;
        }
    }

    internal TimeZoneRegistryEntry Find(string ianaId)
    {
        Guard.NotNullOrWhiteSpace(ianaId, "ianaId");
        if (!ById.ContainsKey(ianaId))
        {
            throw new ArgumentException(
                $"ianaId '{ianaId}' is not present in the bundled zone1970.tab metadata.",
                "ianaId");
        }
        return ById[ianaId];
    }

    internal TimeZoneRegistryEntry[] EntriesCopy()
    {
        return Copy(Entries);
    }

    internal TimeZoneRegistryEntry[] Country(string countryCode)
    {
        string normalized = NormalizeCountryCode(countryCode);
        List<TimeZoneRegistryEntry> result = new List<TimeZoneRegistryEntry>();
        int entryIndex = 0;
        while (entryIndex < Entries.Length)
        {
            int countryIndex = 0;
            while (countryIndex < Entries[entryIndex].CountryCodes.Length)
            {
                if (Entries[entryIndex].CountryCodes[countryIndex] == normalized)
                {
                    result.Add(Entries[entryIndex]);
                    break;
                }
                countryIndex++;
            }
            entryIndex++;
        }
        if (result.Count == 0)
        {
            throw new ArgumentException(
                $"countryCode '{normalized}' has no zone in the bundled zone1970.tab metadata.",
                "countryCode");
        }
        return result.ToArray();
    }

    static string NormalizeCountryCode(string countryCode)
    {
        Guard.NotNullOrWhiteSpace(countryCode, "countryCode");
        if (countryCode.Length != 2 ||
            !AsciiLetter(countryCode[0]) ||
            !AsciiLetter(countryCode[1]))
        {
            throw new ArgumentException(
                "countryCode must contain exactly two ASCII letters.",
                "countryCode");
        }
        return countryCode.ToUpperInvariant();
    }

    static bool AsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    static TimeZoneRegistryEntry[] Copy(TimeZoneRegistryEntry[] values)
    {
        TimeZoneRegistryEntry[] result = new TimeZoneRegistryEntry[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }
}

internal static class TimeZoneRegistryStore
{
    static readonly TimeZoneRegistry?[] Value = new TimeZoneRegistry?[1];
    static readonly object Sync = new object();

    internal static TimeZoneRegistry Current()
    {
        lock (Sync)
        {
            TimeZoneRegistry? current = Value[0];
            if (current == null)
            {
                current = Load();
                Value[0] = current;
            }
            return current;
        }
    }

    static TimeZoneRegistry Load()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream? stream = assembly.GetManifestResourceStream(
            "fake.Data.standards.time-zones.json");
        if (stream == null)
        {
            throw FakeException.Create("Embedded IANA time-zone metadata resource is missing.");
        }
        using (stream)
        {
            using (JsonDocument document = JsonDocument.Parse(stream))
            {
                JsonElement root = document.RootElement;
                string sourceVersion = JsonText.Required(
                    root.GetProperty("source").GetProperty("version"),
                    "$.source.version");
                List<TimeZoneRegistryEntry> entries = new List<TimeZoneRegistryEntry>();
                foreach (JsonElement item in root.GetProperty("records").EnumerateArray())
                {
                    TimeZoneRegistryEntry entry = new TimeZoneRegistryEntry();
                    entry.CountryCodes = Strings(item.GetProperty("countryCodes"));
                    entry.Coordinates = JsonText.Required(item.GetProperty("coordinates"), "$.records[].coordinates");
                    entry.IanaId = JsonText.Required(item.GetProperty("zone"), "$.records[].zone");
                    entry.Comment = Comment(item);
                    Validate(entry);
                    entries.Add(entry);
                }
                if (entries.Count == 0)
                {
                    throw FakeException.Create("Bundled IANA time-zone metadata contains no zone records.");
                }
                return new TimeZoneRegistry(sourceVersion, entries.ToArray());
            }
        }
    }

    static string[] Strings(JsonElement values)
    {
        List<string> result = new List<string>();
        foreach (JsonElement value in values.EnumerateArray())
        {
            result.Add(JsonText.Required(value, "$.records[].countryCodes[]"));
        }
        return result.ToArray();
    }

    static string? Comment(JsonElement item)
    {
        foreach (JsonProperty property in item.EnumerateObject())
        {
            if (property.Name == "comment")
            {
                return JsonText.Required(property.Value, "$.records[].comment");
            }
        }
        return null;
    }

    static void Validate(TimeZoneRegistryEntry entry)
    {
        if (entry.CountryCodes.Length == 0)
        {
            throw FakeException.Create($"Bundled time-zone record '{entry.IanaId}' has no country code.");
        }
        Guard.NotNullOrWhiteSpace(entry.Coordinates, "coordinates");
        Guard.NotNullOrWhiteSpace(entry.IanaId, "ianaId");
        int index = 0;
        while (index < entry.CountryCodes.Length)
        {
            string countryCode = entry.CountryCodes[index];
            if (countryCode == null || countryCode.Length != 2)
            {
                throw FakeException.Create($"Bundled time-zone record '{entry.IanaId}' has an invalid country code.");
            }
            index++;
        }
    }
}

internal static class RuntimeTimeZoneResolver
{
    static readonly Dictionary<string, TimeZoneInfo> Cache = new Dictionary<string, TimeZoneInfo>(StringComparer.Ordinal);
    static readonly object Sync = new object();

    internal static TimeZoneInfo Resolve(string ianaId)
    {
        lock (Sync)
        {
            if (Cache.ContainsKey(ianaId))
            {
                return Cache[ianaId];
            }
            MethodInfo? method = TimeZoneInfo.Utc.GetType().GetMethod(
                "TryFindSystemTimeZoneById");
            if (method == null)
            {
                throw FakeException.Create("The runtime does not expose time-zone identifier mapping.");
            }
            object?[] arguments = new object?[] { ianaId, null };
            object? invocationResult = method.Invoke(null, arguments);
            bool found = invocationResult != null && (bool)invocationResult;
            if (!found || arguments[1] == null)
            {
                throw FakeException.Create($"Runtime time-zone data cannot resolve bundled IANA identifier '{ianaId}'.");
            }
            object resolved = arguments[1] ?? throw FakeException.Create(
                $"Runtime time-zone data cannot resolve bundled IANA identifier '{ianaId}'.");
            TimeZoneInfo value = (TimeZoneInfo)resolved;
            Cache[ianaId] = value;
            return value;
        }
    }
}
