using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace fake;

internal class LocaleResourceSource
{
    Assembly? AssemblyValue;
    string[] ResourceNames = new string[0];
    string[] ResourceContents = new string[0];

    internal static LocaleResourceSource FromAssembly(Assembly assembly)
    {
        Guard.NotNull(assembly, "assembly");
        LocaleResourceSource source = new LocaleResourceSource();
        source.AssemblyValue = assembly;
        return source;
    }

    internal static LocaleResourceSource FromContent(string[] names, string[] contents)
    {
        Guard.NotNull(names, "names");
        Guard.NotNull(contents, "contents");
        if (names.Length != contents.Length)
        {
            throw new ArgumentException("names and contents must have the same length.", "contents");
        }
        LocaleResourceSource source = new LocaleResourceSource();
        source.ResourceNames = new string[names.Length];
        source.ResourceContents = new string[contents.Length];
        Array.Copy(names, source.ResourceNames, names.Length);
        Array.Copy(contents, source.ResourceContents, contents.Length);
        return source;
    }

    internal string[] Names()
    {
        if (AssemblyValue != null)
        {
            return AssemblyValue.GetManifestResourceNames();
        }
        string[] result = new string[ResourceNames.Length];
        Array.Copy(ResourceNames, result, ResourceNames.Length);
        return result;
    }

    internal Stream? Open(string name)
    {
        if (AssemblyValue != null)
        {
            return AssemblyValue.GetManifestResourceStream(name);
        }
        int index = 0;
        while (index < ResourceNames.Length)
        {
            if (ResourceNames[index] == name)
            {
                if (ResourceContents[index] == null)
                {
                    return null;
                }
                return new MemoryStream(Encoding.UTF8.GetBytes(ResourceContents[index]), false);
            }
            index = index + 1;
        }
        return null;
    }
}

internal class LocaleResourceStore
{
    static readonly LocaleResourceStore?[] ProductValue = new LocaleResourceStore?[1];
    static readonly object ProductSync = new object();

    LocaleResourceSource Source;
    int MaximumResourceSize;
    int MaximumResourceCount;
    object Sync;
    bool LoadedValue;
    int OpenCountValue;
    Dictionary<string, LocaleFieldValue[]> Versions;

    internal LocaleResourceStore(
        LocaleResourceSource source,
        int maximumResourceSize,
        int maximumResourceCount)
    {
        Guard.NotNull(source, "source");
        Guard.Positive(maximumResourceSize, "maximumResourceSize");
        Guard.Positive(maximumResourceCount, "maximumResourceCount");
        Source = source;
        MaximumResourceSize = maximumResourceSize;
        MaximumResourceCount = maximumResourceCount;
        Sync = new object();
        LoadedValue = false;
        OpenCountValue = 0;
        Versions = new Dictionary<string, LocaleFieldValue[]>(StringComparer.Ordinal);
    }

    internal static LocaleResourceStore Product()
    {
        lock (ProductSync)
        {
            LocaleResourceStore? current = ProductValue[0];
            if (current == null)
            {
                current = new LocaleResourceStore(
                    LocaleResourceSource.FromAssembly(Assembly.GetExecutingAssembly()),
                    FakeLimits.DefaultMaximumOutputSize,
                    FakeLimits.DefaultMaximumCount);
                ProductValue[0] = current;
            }
            return current;
        }
    }

    internal LocaleFieldValue[] Values(string dataVersion)
    {
        return ValuesFromDataVersionAndMissingAsEmpty(dataVersion, false);
    }

    internal LocaleFieldValue[] ValuesOrEmpty(string dataVersion)
    {
        return ValuesFromDataVersionAndMissingAsEmpty(dataVersion, true);
    }

    LocaleFieldValue[] ValuesFromDataVersionAndMissingAsEmpty(string dataVersion, bool missingAsEmpty)
    {
        Guard.NotNullOrWhiteSpace(dataVersion, "dataVersion");
        EnsureLoaded();
        lock (Sync)
        {
            if (!Versions.ContainsKey(dataVersion))
            {
                if (Versions.Count == 0 || missingAsEmpty)
                {
                    return new LocaleFieldValue[0];
                }
                string[] available = new string[Versions.Count];
                Versions.Keys.CopyTo(available, 0);
                Array.Sort(available, StringComparer.Ordinal);
                throw FakeException.Create($"Locale data version '{dataVersion}' is not embedded. Available versions: {string.Join(", ", available)}.");
            }
            return CopyValues(Versions[dataVersion]);
        }
    }

    internal bool Loaded()
    {
        lock (Sync)
        {
            return LoadedValue;
        }
    }

    internal int OpenCount()
    {
        lock (Sync)
        {
            return OpenCountValue;
        }
    }

    void EnsureLoaded()
    {
        lock (Sync)
        {
            if (LoadedValue)
            {
                return;
            }
            Dictionary<string, LocaleFieldValue[]> loaded = Load();
            Versions = loaded;
            LoadedValue = true;
        }
    }

    Dictionary<string, LocaleFieldValue[]> Load()
    {
        string[] resources = Discover();
        Dictionary<string, List<LocaleFieldValue>> values = new Dictionary<string, List<LocaleFieldValue>>(StringComparer.Ordinal);
        Dictionary<string, string> fields = new Dictionary<string, string>(StringComparer.Ordinal);
        Dictionary<string, string> metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        Dictionary<string, string> metadataResources = new Dictionary<string, string>(StringComparer.Ordinal);
        int resourceIndex = 0;
        while (resourceIndex < resources.Length)
        {
            string resourceName = resources[resourceIndex];
            LocalePack pack = Read(resourceName);
            string metadataKey = Key(pack.DataVersion, pack.Locale);
            string fingerprint = MetadataFingerprint(pack);
            if (metadata.ContainsKey(metadataKey) && metadata[metadataKey] != fingerprint)
            {
                throw FakeException.Create($"Embedded locale resources '{metadataResources[metadataKey]}' and '{resourceName}' declare inconsistent fallback or review metadata for locale '{pack.Locale}' in data version '{pack.DataVersion}'.");
            }
            metadata[metadataKey] = fingerprint;
            metadataResources[metadataKey] = resourceName;
            if (!values.ContainsKey(pack.DataVersion))
            {
                values[pack.DataVersion] = new List<LocaleFieldValue>();
            }
            int fieldIndex = 0;
            while (fieldIndex < pack.Fields.Length)
            {
                LocalePackField field = pack.Fields[fieldIndex];
                string fieldKey = Key(pack.DataVersion, pack.Locale, field.Name);
                if (fields.ContainsKey(fieldKey))
                {
                    throw FakeException.Create($"Embedded locale resources '{fields[fieldKey]}' and '{resourceName}' both define field '{field.Name}' for locale '{pack.Locale}' in data version '{pack.DataVersion}'.");
                }
                fields[fieldKey] = resourceName;
                LocalePackProvenance provenance = FindProvenance(pack, field.Provenance);
                LocaleFieldValue value = LocaleFieldValue.WithMetadata(
                    pack.Locale,
                    field.Name,
                    ParseValue(field.ValueJson),
                    pack.DataVersion,
                    Describe(provenance));
                values[pack.DataVersion].Add(value);
                fieldIndex = fieldIndex + 1;
            }
            resourceIndex = resourceIndex + 1;
        }
        Dictionary<string, LocaleFieldValue[]> result = new Dictionary<string, LocaleFieldValue[]>(StringComparer.Ordinal);
        foreach (string version in values.Keys)
        {
            result[version] = values[version].ToArray();
        }
        return result;
    }

    string[] Discover()
    {
        string[] names = Source.Names();
        Guard.Count(names.Length, MaximumResourceCount, "embeddedResourceNames");
        List<string> resources = new List<string>();
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < names.Length)
        {
            string name = names[index];
            Guard.NotNullOrWhiteSpace(name, "embeddedResourceNames");
            if (IsLocaleResource(name))
            {
                if (!seen.Add(name))
                {
                    throw FakeException.Create($"Embedded locale resource name '{name}' is duplicated.");
                }
                resources.Add(name);
            }
            index = index + 1;
        }
        string[] result = resources.ToArray();
        Array.Sort(result, StringComparer.Ordinal);
        return result;
    }

    LocalePack Read(string resourceName)
    {
        Stream? stream = Source.Open(resourceName);
        OpenCountValue = OpenCountValue + 1;
        if (stream == null)
        {
            throw FakeException.Create($"Embedded locale resource '{resourceName}' could not be opened.");
        }
        using (stream)
        {
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, 1024, false))
            {
                string json = reader.ReadToEnd();
                return LocalePack.Parse(
                    json,
                    MaximumResourceSize,
                    MaximumResourceCount,
                    resourceName);
            }
        }
    }

    static bool IsLocaleResource(string name)
    {
        bool locale = name.StartsWith("fake.Data.locales.", StringComparison.Ordinal);
        bool invariant = name.StartsWith("fake.Data.invariant.", StringComparison.Ordinal);
        return (locale || invariant) && name.EndsWith(".json", StringComparison.Ordinal);
    }

    static string MetadataFingerprint(LocalePack pack)
    {
        return pack.ReviewStatus.Length + ":" + pack.ReviewStatus + Joined(pack.Fallbacks);
    }

    static string Joined(string[] values)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < values.Length)
        {
            result.Append(values[index].Length);
            result.Append(':');
            result.Append(values[index]);
            index = index + 1;
        }
        return result.ToString();
    }

    static LocalePackProvenance FindProvenance(LocalePack pack, string id)
    {
        int index = 0;
        while (index < pack.Provenance.Length)
        {
            if (pack.Provenance[index].Id == id)
            {
                return pack.Provenance[index];
            }
            index = index + 1;
        }
        throw FakeException.Create($"Validated locale pack for '{pack.Locale}' lost provenance id '{id}'.");
    }

    internal static string Describe(LocalePackProvenance provenance)
    {
        return $"{provenance.Source}; source version {provenance.SourceVersion}; license {provenance.License}; reviewer {provenance.Reviewer}";
    }

    internal static object ParseValue(string json)
    {
        using (JsonDocument document = JsonDocument.Parse(json))
        {
            return document.RootElement.Clone();
        }
    }

    static LocaleFieldValue[] CopyValues(LocaleFieldValue[] values)
    {
        LocaleFieldValue[] result = new LocaleFieldValue[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            JsonElement element = (JsonElement)values[index].Value;
            result[index] = LocaleFieldValue.WithMetadata(
                values[index].Locale,
                values[index].Field,
                element.Clone(),
                values[index].DataVersion ?? throw FakeException.Create("A stored locale field has no data version."),
                values[index].Provenance);
            index = index + 1;
        }
        return result;
    }

    static string Key(string first, string second)
    {
        return first.Length + ":" + first + second.Length + ":" + second;
    }

    static string Key(string first, string second, string third)
    {
        return Key(first, second) + third.Length + ":" + third;
    }
}
