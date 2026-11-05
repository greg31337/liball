using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace fake;

internal class LocaleRegistrationStore
{
    string DataVersion;
    int MaximumOutputSize;
    int MaximumCount;
    object Sync;
    Dictionary<string, LocaleFieldValue> Registrations;

    internal LocaleRegistrationStore(
        string dataVersion,
        int maximumOutputSize,
        int maximumCount)
    {
        Guard.NotNullOrWhiteSpace(dataVersion, "dataVersion");
        Guard.Positive(maximumOutputSize, "maximumOutputSize");
        Guard.Positive(maximumCount, "maximumCount");
        DataVersion = dataVersion;
        MaximumOutputSize = maximumOutputSize;
        MaximumCount = maximumCount;
        Sync = new object();
        Registrations = new Dictionary<string, LocaleFieldValue>(StringComparer.Ordinal);
    }

    internal void Register(string resourceName, Stream stream)
    {
        Guard.NotNullOrWhiteSpace(resourceName, "resourceName");
        Guard.NotNull(stream, "stream");
        if (!stream.CanRead)
        {
            throw new ArgumentException("stream must be readable.", "stream");
        }
        LocalePack pack;
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
        {
            string json = reader.ReadToEnd();
            pack = LocalePack.Parse(
                json,
                MaximumOutputSize,
                MaximumCount,
                resourceName);
        }
        if (pack.DataVersion != DataVersion)
        {
            throw FakeException.Create($"Caller locale resource '{resourceName}' uses data version '{pack.DataVersion}', but this Fake instance selected '{DataVersion}'.");
        }
        LocaleFieldValue[] values = new LocaleFieldValue[pack.Fields.Length];
        int index = 0;
        while (index < pack.Fields.Length)
        {
            LocalePackField field = pack.Fields[index];
            LocalePackProvenance provenance = FindProvenance(pack, field.Provenance);
            values[index] = LocaleFieldValue.WithMetadata(
                pack.Locale,
                field.Name,
                LocaleResourceStore.ParseValue(field.ValueJson),
                pack.DataVersion,
                LocaleResourceStore.Describe(provenance));
            index = index + 1;
        }
        RegisterValues(values);
    }

    internal void RegisterFromLocaleAndDataVersionAndProvenanceAndFields(
        string locale,
        string dataVersion,
        string provenance,
        Dictionary<string, object> fields)
    {
        Guard.NotNullOrWhiteSpace(locale, "locale");
        Guard.NotNullOrWhiteSpace(dataVersion, "dataVersion");
        Guard.NotNullOrWhiteSpace(provenance, "provenance");
        Guard.NotNull(fields, "fields");
        Guard.Count(fields.Count, MaximumCount, "fields");
        if (fields.Count == 0)
        {
            throw new ArgumentException("fields must contain at least one field.", "fields");
        }
        if (dataVersion != DataVersion)
        {
            throw new ArgumentException($"dataVersion must equal the captured data version '{DataVersion}'.", "dataVersion");
        }
        string normalizedLocale = NormalizeLocale(locale);
        string[] names = new string[fields.Count];
        fields.Keys.CopyTo(names, 0);
        Array.Sort(names, StringComparer.Ordinal);
        LocaleFieldValue[] values = new LocaleFieldValue[names.Length];
        int index = 0;
        while (index < names.Length)
        {
            string name = names[index];
            Guard.NotNullOrWhiteSpace(name, "fields");
            object value = fields[name];
            Guard.NotNull(value, "fields");
            values[index] = LocaleFieldValue.WithMetadata(
                normalizedLocale,
                name,
                value,
                dataVersion,
                provenance);
            index = index + 1;
        }
        RegisterValues(values);
    }

    internal LocaleFieldValue[] Merge(LocaleFieldValue[] embedded)
    {
        Guard.NotNull(embedded, "embedded");
        lock (Sync)
        {
            Dictionary<string, LocaleFieldValue> merged = new Dictionary<string, LocaleFieldValue>(StringComparer.Ordinal);
            int embeddedIndex = 0;
            while (embeddedIndex < embedded.Length)
            {
                LocaleFieldValue value = embedded[embeddedIndex];
                merged[Key(value.Locale, value.Field)] = value;
                embeddedIndex = embeddedIndex + 1;
            }
            foreach (string key in Registrations.Keys)
            {
                merged[key] = Registrations[key];
            }
            Guard.Count(merged.Count, MaximumCount, "localeFields");
            string[] keys = new string[merged.Count];
            merged.Keys.CopyTo(keys, 0);
            Array.Sort(keys, StringComparer.Ordinal);
            LocaleFieldValue[] result = new LocaleFieldValue[keys.Length];
            int index = 0;
            while (index < keys.Length)
            {
                result[index] = Copy(merged[keys[index]]);
                index = index + 1;
            }
            return result;
        }
    }

    internal int RegistrationCount()
    {
        lock (Sync)
        {
            return Registrations.Count;
        }
    }

    internal LocaleRegistrationStore SnapshotStore()
    {
        LocaleRegistrationStore result = new LocaleRegistrationStore(
            DataVersion,
            MaximumOutputSize,
            MaximumCount);
        lock (Sync)
        {
            foreach (string key in Registrations.Keys)
            {
                result.Registrations[key] = Copy(Registrations[key]);
            }
        }
        return result;
    }

    void RegisterValues(LocaleFieldValue[] values)
    {
        lock (Sync)
        {
            Dictionary<string, LocaleFieldValue> next = new Dictionary<string, LocaleFieldValue>(Registrations, StringComparer.Ordinal);
            int index = 0;
            while (index < values.Length)
            {
                LocaleFieldValue value = values[index];
                next[Key(value.Locale, value.Field)] = Copy(value);
                index = index + 1;
            }
            Guard.Count(next.Count, MaximumCount, "registeredLocaleFields");
            Registrations = next;
        }
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
        throw FakeException.Create($"Validated caller locale pack for '{pack.Locale}' lost provenance id '{id}'.");
    }

    static string NormalizeLocale(string locale)
    {
        if (string.Equals(locale, LocaleFieldValue.InvariantLocale, StringComparison.OrdinalIgnoreCase))
        {
            return LocaleFieldValue.InvariantLocale;
        }
        return LanguageTagParser.Parse(locale, "locale").Normalized;
    }

    static LocaleFieldValue Copy(LocaleFieldValue value)
    {
        if (value.DataVersion == null)
            return new LocaleFieldValue(value.Locale, value.Field, value.Value);
        return LocaleFieldValue.WithMetadata(
            value.Locale,
            value.Field,
            value.Value,
            value.DataVersion,
            value.Provenance);
    }

    static string Key(string locale, string field)
    {
        return locale.Length + ":" + locale + field.Length + ":" + field;
    }
}
