using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace fake;

internal class LanguageRegistryEntry
{
    internal string Type = string.Empty;
    internal string Value = string.Empty;
    internal string? PreferredValue;
    internal string[] Prefixes = new string[0];
    internal string? SuppressScript;
    internal string? Macrolanguage;
    internal string? Scope;
    internal string? Deprecated;
}

internal class LanguageRegistry
{
    internal string FileDate = string.Empty;
    Dictionary<string, LanguageRegistryEntry> Languages;
    Dictionary<string, LanguageRegistryEntry> Extlangs;
    Dictionary<string, LanguageRegistryEntry> Scripts;
    Dictionary<string, LanguageRegistryEntry> Regions;
    Dictionary<string, LanguageRegistryEntry> Variants;
    Dictionary<string, LanguageRegistryEntry> Tags;

    internal LanguageRegistry()
    {
        FileDate = string.Empty;
        Languages = NewEntries();
        Extlangs = NewEntries();
        Scripts = NewEntries();
        Regions = NewEntries();
        Variants = NewEntries();
        Tags = NewEntries();
    }

    internal LanguageRegistryEntry? Language(string value)
    {
        return Find(Languages, value);
    }

    internal LanguageRegistryEntry? Extlang(string value)
    {
        return Find(Extlangs, value);
    }

    internal LanguageRegistryEntry? Script(string value)
    {
        return Find(Scripts, value);
    }

    internal LanguageRegistryEntry? Region(string value)
    {
        return Find(Regions, value);
    }

    internal LanguageRegistryEntry? Variant(string value)
    {
        return Find(Variants, value);
    }

    internal LanguageRegistryEntry? Tag(string value)
    {
        return Find(Tags, value);
    }

    internal void Add(LanguageRegistryEntry entry)
    {
        Dictionary<string, LanguageRegistryEntry>? entries = null;
        if (entry.Type == "language")
        {
            entries = Languages;
        }
        else if (entry.Type == "extlang")
        {
            entries = Extlangs;
        }
        else if (entry.Type == "script")
        {
            entries = Scripts;
        }
        else if (entry.Type == "region")
        {
            entries = Regions;
        }
        else if (entry.Type == "variant")
        {
            entries = Variants;
        }
        else if (entry.Type == "grandfathered" || entry.Type == "redundant")
        {
            entries = Tags;
        }
        if (entries == null)
        {
            throw FakeException.Create($"Language registry contains unsupported record type '{entry.Type}'.");
        }
        string key = entry.Value.ToLowerInvariant();
        if (entries.ContainsKey(key))
        {
            throw FakeException.Create($"Language registry contains duplicate {entry.Type} value '{entry.Value}'.");
        }
        entries[key] = entry;
    }

    static Dictionary<string, LanguageRegistryEntry> NewEntries()
    {
        return new Dictionary<string, LanguageRegistryEntry>(StringComparer.Ordinal);
    }

    static LanguageRegistryEntry? Find(Dictionary<string, LanguageRegistryEntry> entries, string value)
    {
        string key = value.ToLowerInvariant();
        if (!entries.ContainsKey(key))
        {
            return null;
        }
        return entries[key];
    }
}

internal static class LanguageRegistryStore
{
    static readonly LanguageRegistry?[] Value = new LanguageRegistry?[1];
    static readonly object Sync = new object();

    internal static LanguageRegistry Current()
    {
        lock (Sync)
        {
            LanguageRegistry? current = Value[0];
            if (current == null)
            {
                current = Load();
                Value[0] = current;
            }
            return current;
        }
    }

    static LanguageRegistry Load()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream? stream = assembly.GetManifestResourceStream("fake.Data.standards.language-tags.json");
        if (stream == null)
        {
            throw FakeException.Create("Embedded IANA language-subtag registry resource is missing.");
        }
        using (stream)
        {
            using (JsonDocument document = JsonDocument.Parse(stream))
            {
                LanguageRegistry registry = new LanguageRegistry();
                JsonElement root = document.RootElement;
                registry.FileDate = JsonText.Required(root.GetProperty("fileDate"), "$.fileDate");
                foreach (JsonElement item in root.GetProperty("records").EnumerateArray())
                {
                    LanguageRegistryEntry entry = new LanguageRegistryEntry();
                    entry.Type = JsonText.Required(item.GetProperty("type"), "$.records[].type");
                    entry.Value = JsonText.Required(item.GetProperty("value"), "$.records[].value");
                    entry.PreferredValue = JsonText.Optional(item.GetProperty("preferredValue"), "$.records[].preferredValue");
                    entry.Prefixes = Strings(item.GetProperty("prefixes"));
                    entry.SuppressScript = JsonText.Optional(item.GetProperty("suppressScript"), "$.records[].suppressScript");
                    entry.Macrolanguage = JsonText.Optional(item.GetProperty("macrolanguage"), "$.records[].macrolanguage");
                    entry.Scope = JsonText.Optional(item.GetProperty("scope"), "$.records[].scope");
                    entry.Deprecated = JsonText.Optional(item.GetProperty("deprecated"), "$.records[].deprecated");
                    registry.Add(entry);
                }
                return registry;
            }
        }
    }

    static string[] Strings(JsonElement values)
    {
        List<string> result = new List<string>();
        foreach (JsonElement value in values.EnumerateArray())
        {
            result.Add(JsonText.Required(value, "$.records[].prefixes[]"));
        }
        return result.ToArray();
    }
}
