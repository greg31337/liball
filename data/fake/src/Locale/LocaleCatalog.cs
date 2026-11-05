using System;
using System.Collections.Generic;

namespace fake;

internal class LocaleCatalogEntry
{
    internal string Locale = string.Empty;
    internal string Field = string.Empty;
    internal object Value = new object();
    internal string DataVersion = string.Empty;
    internal string Provenance = string.Empty;
}

internal class LocaleProviderCoverageBuilder
{
    internal string Provider = string.Empty;
    internal List<string> NativeFields;
    internal List<string> FallbackFields;
    internal List<string> FallbackSources;

    internal LocaleProviderCoverageBuilder(string provider)
    {
        Provider = provider;
        NativeFields = new List<string>();
        FallbackFields = new List<string>();
        FallbackSources = new List<string>();
    }
}

internal class LocaleCatalog
{
    GenerationContext Context;
    Dictionary<string, LocaleCatalogEntry> Entries;
    Dictionary<string, List<LocaleCatalogEntry>> Locales;

    internal LocaleCatalog(GenerationContext context, LocaleFieldValue[] values)
    {
        Guard.NotNull(values, "values");
        Guard.Count(values.Length, context.CapturedOptions.MaximumCount, "values");
        Context = context;
        Entries = new Dictionary<string, LocaleCatalogEntry>(StringComparer.Ordinal);
        Locales = new Dictionary<string, List<LocaleCatalogEntry>>(StringComparer.Ordinal);
        int index = 0;
        while (index < values.Length)
        {
            Add(context, values[index], index);
            index = index + 1;
        }
    }

    internal LocaleCatalogEntry? Find(string locale, string field)
    {
        string key = Key(locale, field);
        if (!Entries.ContainsKey(key))
        {
            return null;
        }
        return Entries[key];
    }

    internal bool ContainsLocale(string locale)
    {
        return Locales.ContainsKey(locale);
    }

    internal bool IsSupported(string locale)
    {
        LanguageTag tag = LanguageTagParser.Parse(locale, "locale");
        return ContainsLocale(tag.Normalized);
    }

    internal string[] Supported()
    {
        List<string> supported = new List<string>();
        foreach (string locale in Locales.Keys)
        {
            if (locale != LocaleFieldValue.InvariantLocale)
            {
                supported.Add(locale);
            }
        }
        string[] result = supported.ToArray();
        Array.Sort(result, StringComparer.Ordinal);
        return result;
    }

    internal LocaleCatalogEntry[] ForLocale(string locale)
    {
        if (!Locales.ContainsKey(locale))
        {
            return new LocaleCatalogEntry[0];
        }
        return Locales[locale].ToArray();
    }

    internal LocaleInfo Metadata(string locale)
    {
        LanguageTag tag = LanguageTagParser.Parse(locale, "locale");
        LocaleCatalogEntry[] entries = ForLocale(tag.Normalized);
        if (entries.Length == 0)
        {
            throw FakeException.Create($"Locale '{tag.Normalized}' has no native fields in the locale catalog.");
        }
        LocaleInfo info = new LocaleInfo();
        info.Tag = tag.Normalized;
        info.Language = tag.Language;
        info.Script = tag.Script;
        info.Territory = tag.Region;
        info.Fields = Fields(new string[] { tag.Normalized });
        info.DataVersion = entries[0].DataVersion;
        info.Provenance = Provenance(entries);
        LocaleMetadata.Populate(Context, this, tag, info);
        return info;
    }

    internal LocaleNameRecord[] Scripts()
    {
        return LocaleMetadata.Scripts(Context, this);
    }

    internal LocaleNameRecord[] Territories()
    {
        return LocaleMetadata.Territories(Context, this);
    }

    internal LocaleCoverage Coverage(string locale)
    {
        LanguageTag tag = LanguageTagParser.Parse(locale, "locale");
        string[] candidates = LocaleResolver.CandidateLocales(Context, tag);
        string[] fields = Fields(candidates);
        List<string> nativeFields = new List<string>();
        List<string> fallbackFields = new List<string>();
        List<string> fallbackSources = new List<string>();
        List<LocaleCatalogEntry> contributing = new List<LocaleCatalogEntry>();
        List<LocaleFieldCoverage> fieldCoverage = new List<LocaleFieldCoverage>();
        int fieldIndex = 0;
        while (fieldIndex < fields.Length)
        {
            LocaleCatalogEntry entry = FindSource(candidates, fields[fieldIndex]);
            bool native = entry.Locale == tag.Normalized;
            if (native)
            {
                nativeFields.Add(fields[fieldIndex]);
            }
            else
            {
                fallbackFields.Add(fields[fieldIndex]);
                fallbackSources.Add(entry.Locale);
            }
            contributing.Add(entry);
            LocaleFieldCoverage field = new LocaleFieldCoverage();
            field.Provider = ProviderName(entry.Field);
            field.Field = entry.Field;
            field.SourceLocale = entry.Locale;
            field.Native = native;
            field.DataVersion = entry.DataVersion;
            field.Provenance = entry.Provenance;
            fieldCoverage.Add(field);
            fieldIndex = fieldIndex + 1;
        }
        LocaleCoverage coverage = new LocaleCoverage();
        coverage.Locale = tag.Normalized;
        coverage.NativeFields = nativeFields.ToArray();
        coverage.FallbackFields = fallbackFields.ToArray();
        coverage.FallbackSources = fallbackSources.ToArray();
        coverage.NativeFieldCount = coverage.NativeFields.Length;
        coverage.FallbackFieldCount = coverage.FallbackFields.Length;
        coverage.Fields = fieldCoverage.ToArray();
        coverage.Providers = ProviderCoverage(coverage.Fields);
        List<string> nativeProviders = new List<string>();
        List<string> fallbackOnlyProviders = new List<string>();
        int providerIndex = 0;
        while (providerIndex < coverage.Providers.Length)
        {
            LocaleProviderCoverage provider = coverage.Providers[providerIndex];
            if (provider.HasNativeCoverage)
            {
                nativeProviders.Add(provider.Provider);
            }
            else if (provider.HasFallbackCoverage)
            {
                fallbackOnlyProviders.Add(provider.Provider);
            }
            providerIndex = providerIndex + 1;
        }
        coverage.NativeProviders = nativeProviders.ToArray();
        coverage.FallbackOnlyProviders = fallbackOnlyProviders.ToArray();
        coverage.ProviderCount = coverage.Providers.Length;
        coverage.NativeProviderCount = coverage.NativeProviders.Length;
        coverage.FallbackOnlyProviderCount = coverage.FallbackOnlyProviders.Length;
        LocaleCatalogEntry[] entries = contributing.ToArray();
        coverage.DataVersions = DataVersions(entries);
        coverage.Provenance = Provenance(entries);
        return coverage;
    }

    static LocaleProviderCoverage[] ProviderCoverage(LocaleFieldCoverage[] fields)
    {
        Dictionary<string, LocaleProviderCoverageBuilder> builders = new Dictionary<string, LocaleProviderCoverageBuilder>(StringComparer.Ordinal);
        int fieldIndex = 0;
        while (fieldIndex < fields.Length)
        {
            LocaleFieldCoverage field = fields[fieldIndex];
            if (!builders.ContainsKey(field.Provider))
            {
                builders[field.Provider] = new LocaleProviderCoverageBuilder(field.Provider);
            }
            LocaleProviderCoverageBuilder builder = builders[field.Provider];
            if (field.Native)
            {
                builder.NativeFields.Add(field.Field);
            }
            else
            {
                builder.FallbackFields.Add(field.Field);
                builder.FallbackSources.Add(field.SourceLocale);
            }
            fieldIndex = fieldIndex + 1;
        }
        string[] names = new string[builders.Count];
        builders.Keys.CopyTo(names, 0);
        Array.Sort(names, StringComparer.Ordinal);
        LocaleProviderCoverage[] result = new LocaleProviderCoverage[names.Length];
        int providerIndex = 0;
        while (providerIndex < names.Length)
        {
            LocaleProviderCoverageBuilder builder = builders[names[providerIndex]];
            LocaleProviderCoverage provider = new LocaleProviderCoverage();
            provider.Provider = builder.Provider;
            provider.NativeFields = builder.NativeFields.ToArray();
            provider.FallbackFields = builder.FallbackFields.ToArray();
            provider.FallbackSources = builder.FallbackSources.ToArray();
            provider.NativeFieldCount = provider.NativeFields.Length;
            provider.FallbackFieldCount = provider.FallbackFields.Length;
            provider.HasNativeCoverage = provider.NativeFieldCount != 0;
            provider.HasFallbackCoverage = provider.FallbackFieldCount != 0;
            result[providerIndex] = provider;
            providerIndex = providerIndex + 1;
        }
        return result;
    }

    static string ProviderName(string field)
    {
        int separator = field.IndexOf('.');
        if (separator <= 0)
        {
            return field;
        }
        return field.Substring(0, separator);
    }

    internal string[] Fields(string[] locales)
    {
        HashSet<string> fields = new HashSet<string>(StringComparer.Ordinal);
        int localeIndex = 0;
        while (localeIndex < locales.Length)
        {
            LocaleCatalogEntry[] entries = ForLocale(locales[localeIndex]);
            int entryIndex = 0;
            while (entryIndex < entries.Length)
            {
                fields.Add(entries[entryIndex].Field);
                entryIndex = entryIndex + 1;
            }
            localeIndex = localeIndex + 1;
        }
        string[] result = new string[fields.Count];
        fields.CopyTo(result);
        Array.Sort(result, StringComparer.Ordinal);
        return result;
    }

    LocaleCatalogEntry FindSource(string[] locales, string field)
    {
        int index = 0;
        while (index < locales.Length)
        {
            LocaleCatalogEntry? entry = Find(locales[index], field);
            if (entry != null)
            {
                return entry;
            }
            index = index + 1;
        }
        throw FakeException.Create($"Locale catalog field '{field}' has no source in its reported coverage chain.");
    }

    static string[] DataVersions(LocaleCatalogEntry[] entries)
    {
        HashSet<string> values = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < entries.Length)
        {
            values.Add(entries[index].DataVersion);
            index = index + 1;
        }
        return Sorted(values);
    }

    static string[] Provenance(LocaleCatalogEntry[] entries)
    {
        HashSet<string> values = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < entries.Length)
        {
            values.Add(entries[index].Provenance);
            index = index + 1;
        }
        return Sorted(values);
    }

    static string[] Sorted(HashSet<string> values)
    {
        string[] result = new string[values.Count];
        values.CopyTo(result);
        Array.Sort(result, StringComparer.Ordinal);
        return result;
    }

    static string Key(string locale, string field)
    {
        return locale.Length + ":" + locale + field.Length + ":" + field;
    }

    void Add(GenerationContext context, LocaleFieldValue value, int index)
    {
        if (value == null)
        {
            throw new ArgumentException($"values[{index}] cannot be null.", "values");
        }
        Guard.NotNullOrWhiteSpace(value.Locale, "values");
        Guard.NotNullOrWhiteSpace(value.Field, "values");
        Guard.NotNull(value.Value, "values");
        Guard.NotNullOrWhiteSpace(value.Provenance, "values");
        if (value.DataVersion != null)
        {
            Guard.NotNullOrWhiteSpace(value.DataVersion, "values");
        }
        string locale = NormalizeSource(value.Locale);
        string key = Key(locale, value.Field);
        if (Entries.ContainsKey(key))
        {
            throw new ArgumentException($"values contains more than one '{value.Field}' field for normalized locale '{locale}'.", "values");
        }
        LocaleCatalogEntry entry = new LocaleCatalogEntry();
        entry.Locale = locale;
        entry.Field = value.Field;
        entry.Value = value.Value;
        entry.DataVersion = value.DataVersion == null
            ? context.CapturedOptions.DataVersion
            : value.DataVersion;
        entry.Provenance = value.Provenance;
        if (!Locales.ContainsKey(locale))
        {
            Locales[locale] = new List<LocaleCatalogEntry>();
        }
        else if (Locales[locale][0].DataVersion != entry.DataVersion)
        {
            throw new ArgumentException($"values for normalized locale '{locale}' must use one data version.", "values");
        }
        Entries[key] = entry;
        Locales[locale].Add(entry);
    }

    static string NormalizeSource(string locale)
    {
        if (string.Equals(locale, LocaleFieldValue.InvariantLocale, StringComparison.OrdinalIgnoreCase))
        {
            return LocaleFieldValue.InvariantLocale;
        }
        return LanguageTagParser.Parse(locale, "values").Normalized;
    }
}
