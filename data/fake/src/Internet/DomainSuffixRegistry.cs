using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace fake;

internal class DomainSuffixRegistry
{
    internal string SourceVersion = string.Empty;
    string[] Values;
    HashSet<string> Lookup;

    internal DomainSuffixRegistry(string sourceVersion, string[] values)
    {
        Guard.NotNullOrWhiteSpace(sourceVersion, "sourceVersion");
        Guard.NotNull(values, "values");
        if (values.Length == 0)
        {
            throw FakeException.Create("Bundled IANA TLD metadata contains no records.");
        }
        SourceVersion = sourceVersion;
        Values = new string[values.Length];
        Array.Copy(values, Values, values.Length);
        Lookup = new HashSet<string>(StringComparer.Ordinal);
        string? previous = null;
        int index = 0;
        while (index < Values.Length)
        {
            string value = Values[index];
            DomainNameGenerator.ValidateAsciiLabel(value, "tld");
            if (value != value.ToLowerInvariant())
            {
                throw FakeException.Create($"Bundled IANA TLD metadata contains non-lowercase record '{value}'.");
            }
            if (previous != null && string.CompareOrdinal(previous, value) >= 0)
            {
                throw FakeException.Create($"Bundled IANA TLD metadata is not in unique ordinal order at '{value}'.");
            }
            Lookup.Add(value);
            previous = value;
            index = index + 1;
        }
    }

    internal string[] Copy()
    {
        string[] result = new string[Values.Length];
        Array.Copy(Values, result, Values.Length);
        return result;
    }

    internal string Choose(FakeRandom random)
    {
        return Values[random.IntFromMaximumExclusive(Values.Length)];
    }

    internal bool Contains(string value)
    {
        Guard.NotNullOrWhiteSpace(value, "value");
        string normalized = value.ToLowerInvariant();
        return Lookup.Contains(normalized);
    }
}

internal static class DomainSuffixRegistryStore
{
    static readonly DomainSuffixRegistry?[] Value = new DomainSuffixRegistry?[1];
    static readonly object Sync = new object();

    internal static DomainSuffixRegistry Current()
    {
        lock (Sync)
        {
            DomainSuffixRegistry? current = Value[0];
            if (current == null)
            {
                current = Load();
                Value[0] = current;
            }
            return current;
        }
    }

    static DomainSuffixRegistry Load()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream? stream = assembly.GetManifestResourceStream("fake.Data.standards.tlds.json");
        if (stream == null)
        {
            throw FakeException.Create("Embedded IANA TLD metadata resource is missing.");
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
                foreach (JsonElement item in root.GetProperty("records").EnumerateArray())
                {
                    values.Add(JsonText.Required(item, "$.records[]"));
                }
                return new DomainSuffixRegistry(sourceVersion, values.ToArray());
            }
        }
    }
}
