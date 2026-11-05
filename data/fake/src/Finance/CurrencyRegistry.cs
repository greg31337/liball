using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace fake;

internal class CurrencyRegistry
{
    internal string Source = string.Empty;
    internal string SourceUrl = string.Empty;
    internal string SourceVersion = string.Empty;
    internal string PublishedOn = string.Empty;
    internal string RetrievedOn = string.Empty;
    CurrencyRecord[] Values;
    Dictionary<string, CurrencyRecord> ByCode;
    Dictionary<string, CurrencyRecord> ByNumericCode;

    internal CurrencyRegistry(
        string source,
        string sourceUrl,
        string sourceVersion,
        string publishedOn,
        string retrievedOn,
        CurrencyRecord[] values)
    {
        Guard.NotNullOrWhiteSpace(source, "source");
        Guard.NotNullOrWhiteSpace(sourceUrl, "sourceUrl");
        Guard.NotNullOrWhiteSpace(sourceVersion, "sourceVersion");
        Guard.NotNullOrWhiteSpace(publishedOn, "publishedOn");
        Guard.NotNullOrWhiteSpace(retrievedOn, "retrievedOn");
        Guard.NotNull(values, "values");
        if (values.Length == 0)
        {
            throw FakeException.Create("Bundled ISO 4217 metadata contains no coded records.");
        }
        Source = source;
        SourceUrl = sourceUrl;
        SourceVersion = sourceVersion;
        PublishedOn = publishedOn;
        RetrievedOn = retrievedOn;
        Values = new CurrencyRecord[values.Length];
        Array.Copy(values, Values, values.Length);
        ByCode = new Dictionary<string, CurrencyRecord>(StringComparer.Ordinal);
        ByNumericCode = new Dictionary<string, CurrencyRecord>(StringComparer.Ordinal);
        string? previous = null;
        int index = 0;
        while (index < Values.Length)
        {
            CurrencyRecord value = Values[index];
            Validate(value);
            if (previous != null && string.CompareOrdinal(previous, value.Code) >= 0)
            {
                throw FakeException.Create($"Bundled ISO 4217 metadata is not in unique alphabetic-code order at '{value.Code}'.");
            }
            if (ByNumericCode.ContainsKey(value.NumericCode))
            {
                throw FakeException.Create($"Bundled ISO 4217 metadata contains duplicate numeric code '{value.NumericCode}'.");
            }
            ByCode[value.Code] = value;
            ByNumericCode[value.NumericCode] = value;
            previous = value.Code;
            index = index + 1;
        }
    }

    internal CurrencyRecord Choose(FakeRandom random, int maximumOutputSize)
    {
        return Copy(Values[random.IntFromMaximumExclusive(Values.Length)], maximumOutputSize);
    }

    internal string ChooseCode(FakeRandom random, int maximumOutputSize)
    {
        string result = Values[random.IntFromMaximumExclusive(Values.Length)].Code;
        Guard.OutputSize(result.Length, maximumOutputSize, "currencyCode");
        return result;
    }

    internal string ChooseName(FakeRandom random, int maximumOutputSize)
    {
        string result = Values[random.IntFromMaximumExclusive(Values.Length)].Name;
        Guard.OutputSize(result.Length, maximumOutputSize, "currencyName");
        return result;
    }

    internal CurrencyRecord Find(string code, int maximumOutputSize)
    {
        string normalized = NormalizeCode(code);
        if (!ByCode.ContainsKey(normalized))
        {
            throw new ArgumentException(
                $"code '{code}' is not present in the bundled current ISO 4217 snapshot.",
                "code");
        }
        return Copy(ByCode[normalized], maximumOutputSize);
    }

    internal CurrencyRecord FindNumeric(
        string numericCode,
        int maximumOutputSize)
    {
        ValidateNumericCode(numericCode, "numericCode");
        if (!ByNumericCode.ContainsKey(numericCode))
        {
            throw new ArgumentException(
                $"numericCode '{numericCode}' is not present in the bundled current ISO 4217 snapshot.",
                "numericCode");
        }
        return Copy(ByNumericCode[numericCode], maximumOutputSize);
    }

    internal CurrencyRecord[] CopyAll(
        int maximumCount,
        int maximumOutputSize)
    {
        Guard.Count(Values.Length, maximumCount, "currencies");
        CurrencyRecord[] result = new CurrencyRecord[Values.Length];
        int total = 0;
        int index = 0;
        while (index < Values.Length)
        {
            result[index] = Copy(Values[index], FakeLimits.DefaultMaximumOutputSize);
            total = total + Size(result[index]);
            index = index + 1;
        }
        Guard.OutputSize(total, maximumOutputSize, "currencies");
        return result;
    }

    internal CurrencyCatalogMetadata Metadata(int maximumOutputSize)
    {
        CurrencyCatalogMetadata result = new CurrencyCatalogMetadata();
        result.Source = Source;
        result.SourceUrl = SourceUrl;
        result.SourceVersion = SourceVersion;
        result.PublishedOn = PublishedOn;
        result.RetrievedOn = RetrievedOn;
        result.RecordCount = Values.Length;
        int size = Source.Length + SourceUrl.Length + SourceVersion.Length +
            PublishedOn.Length + RetrievedOn.Length;
        Guard.OutputSize(size, maximumOutputSize, "currencyMetadata");
        return result;
    }

    static CurrencyRecord Copy(CurrencyRecord value, int maximumOutputSize)
    {
        Guard.OutputSize(Size(value), maximumOutputSize, "currency");
        CurrencyRecord result = new CurrencyRecord();
        result.Code = value.Code;
        result.NumericCode = value.NumericCode;
        result.MinorUnits = value.MinorUnits;
        result.HasMinorUnits = value.HasMinorUnits;
        result.Name = value.Name;
        result.Entities = new string[value.Entities.Length];
        Array.Copy(value.Entities, result.Entities, value.Entities.Length);
        result.RegistryVersion = value.RegistryVersion;
        return result;
    }

    static int Size(CurrencyRecord value)
    {
        int size = value.Code.Length + value.NumericCode.Length +
            value.Name.Length + value.RegistryVersion.Length;
        int index = 0;
        while (index < value.Entities.Length)
        {
            size = size + value.Entities[index].Length;
            index = index + 1;
        }
        return size;
    }

    static string NormalizeCode(string code)
    {
        Guard.NotNullOrWhiteSpace(code, "code");
        if (code.Length != 3 ||
            !AsciiLetter(code[0]) ||
            !AsciiLetter(code[1]) ||
            !AsciiLetter(code[2]))
        {
            throw new ArgumentException(
                "code must contain exactly three ASCII letters.",
                "code");
        }
        return code.ToUpperInvariant();
    }

    static void ValidateNumericCode(string numericCode, string parameterName)
    {
        Guard.NotNullOrWhiteSpace(numericCode, parameterName);
        if (numericCode.Length != 3 ||
            !AsciiDigit(numericCode[0]) ||
            !AsciiDigit(numericCode[1]) ||
            !AsciiDigit(numericCode[2]))
        {
            throw new ArgumentException(
                $"{parameterName} must contain exactly three ASCII digits.",
                parameterName);
        }
    }

    static bool AsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    static bool AsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    static void Validate(CurrencyRecord value)
    {
        if (value.Code == null || value.Code != NormalizeCode(value.Code))
        {
            throw FakeException.Create("Bundled ISO 4217 metadata contains an invalid alphabetic code.");
        }
        ValidateNumericCode(value.NumericCode, "numericCode");
        Guard.NotNullOrWhiteSpace(value.Name, "name");
        Guard.NotNullOrWhiteSpace(value.RegistryVersion, "registryVersion");
        Guard.NotNull(value.Entities, "entities");
        if (value.Entities.Length == 0)
        {
            throw FakeException.Create($"Bundled ISO 4217 record '{value.Code}' contains no entity mappings.");
        }
        if (value.HasMinorUnits && (value.MinorUnits < 0 || value.MinorUnits > 9))
        {
            throw FakeException.Create($"Bundled ISO 4217 record '{value.Code}' contains invalid minor units.");
        }
        int index = 0;
        while (index < value.Entities.Length)
        {
            Guard.NotNullOrWhiteSpace(value.Entities[index], "entity");
            index = index + 1;
        }
    }
}

internal static class CurrencyRegistryStore
{
    static readonly CurrencyRegistry?[] Value = new CurrencyRegistry?[1];
    static readonly object Sync = new object();

    internal static CurrencyRegistry Current()
    {
        lock (Sync)
        {
            CurrencyRegistry? current = Value[0];
            if (current == null)
            {
                current = Load();
                Value[0] = current;
            }
            return current;
        }
    }

    static CurrencyRegistry Load()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream? stream = assembly.GetManifestResourceStream(
            "fake.Data.standards.currencies.json");
        if (stream == null)
        {
            throw FakeException.Create("Embedded ISO 4217 currency metadata resource is missing.");
        }
        using (stream)
        {
            using (JsonDocument document = JsonDocument.Parse(stream))
            {
                JsonElement root = document.RootElement;
                JsonElement source = root.GetProperty("source");
                string sourceName = JsonText.Required(source.GetProperty("name"), "$.source.name");
                string sourceUrl = JsonText.Required(source.GetProperty("url"), "$.source.url");
                string sourceVersion = JsonText.Required(source.GetProperty("version"), "$.source.version");
                string publishedOn = PublicationDate(sourceVersion);
                string retrievedOn = JsonText.Required(source.GetProperty("retrievedOn"), "$.source.retrievedOn");
                List<CurrencyRecord> values = new List<CurrencyRecord>();
                foreach (JsonElement item in root.GetProperty("records").EnumerateArray())
                {
                    CurrencyRecord value = new CurrencyRecord();
                    value.Code = JsonText.Required(item.GetProperty("code"), "$.records[].code");
                    value.NumericCode = JsonText.Required(item.GetProperty("numericCode"), "$.records[].numericCode");
                    JsonElement minorUnits = item.GetProperty("minorUnits");
                    value.HasMinorUnits = minorUnits.ValueKind != JsonValueKind.Null;
                    value.MinorUnits = value.HasMinorUnits ? minorUnits.GetInt32() : 0;
                    value.Name = JsonText.Required(item.GetProperty("name"), "$.records[].name");
                    value.Entities = Strings(item.GetProperty("entities"));
                    value.RegistryVersion = sourceVersion;
                    values.Add(value);
                }
                return new CurrencyRegistry(
                    sourceName,
                    sourceUrl,
                    sourceVersion,
                    publishedOn,
                    retrievedOn,
                    values.ToArray());
            }
        }
    }

    static string PublicationDate(string sourceVersion)
    {
        const string Prefix = "published ";
        Guard.NotNullOrWhiteSpace(sourceVersion, "sourceVersion");
        if (!sourceVersion.StartsWith(Prefix, StringComparison.Ordinal) ||
            sourceVersion.Length != Prefix.Length + 10)
        {
            throw FakeException.Create("Bundled ISO 4217 metadata contains an invalid publication version.");
        }
        return sourceVersion.Substring(Prefix.Length);
    }

    static string[] Strings(JsonElement values)
    {
        List<string> result = new List<string>();
        foreach (JsonElement value in values.EnumerateArray())
        {
            result.Add(JsonText.Required(value, "$.records[].entities[]"));
        }
        return result.ToArray();
    }
}
