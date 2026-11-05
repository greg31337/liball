using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace fake;

internal class IbanRegistry
{
    internal string Source = string.Empty;
    internal string SourceUrl = string.Empty;
    internal string SourceVersion = string.Empty;
    internal string Upstream = string.Empty;
    internal string RetrievedOn = string.Empty;
    IbanFormat[] Values;
    Dictionary<string, IbanFormat> ByCountry;
    Dictionary<string, IbanFormat> ByPrefix;

    internal IbanRegistry(
        string source,
        string sourceUrl,
        string sourceVersion,
        string upstream,
        string retrievedOn,
        IbanFormat[] values)
    {
        Guard.NotNullOrWhiteSpace(source, "source");
        Guard.NotNullOrWhiteSpace(sourceUrl, "sourceUrl");
        Guard.NotNullOrWhiteSpace(sourceVersion, "sourceVersion");
        Guard.NotNullOrWhiteSpace(upstream, "upstream");
        Guard.NotNullOrWhiteSpace(retrievedOn, "retrievedOn");
        Guard.NotNull(values, "values");
        if (values.Length == 0)
        {
            throw FakeException.Create("Bundled IBAN format metadata contains no records.");
        }
        Source = source;
        SourceUrl = sourceUrl;
        SourceVersion = sourceVersion;
        Upstream = upstream;
        RetrievedOn = retrievedOn;
        Values = new IbanFormat[values.Length];
        Array.Copy(values, Values, values.Length);
        ByCountry = new Dictionary<string, IbanFormat>(StringComparer.Ordinal);
        ByPrefix = new Dictionary<string, IbanFormat>(StringComparer.Ordinal);
        string? previous = null;
        int index = 0;
        while (index < Values.Length)
        {
            IbanFormat value = Values[index];
            Validate(value);
            if (previous != null && string.CompareOrdinal(previous, value.CountryCode) >= 0)
            {
                throw FakeException.Create($"Bundled IBAN metadata is not in unique country-code order at '{value.CountryCode}'.");
            }
            ByCountry[value.CountryCode] = value;
            if (!ByPrefix.ContainsKey(value.IbanPrefix) || value.CountryCode == value.IbanPrefix)
            {
                ByPrefix[value.IbanPrefix] = value;
            }
            previous = value.CountryCode;
            index = index + 1;
        }
    }

    internal IbanFormat Choose(FakeRandom random, int maximumOutputSize)
    {
        return Copy(Values[random.IntFromMaximumExclusive(Values.Length)], maximumOutputSize);
    }

    internal IbanFormat Find(string countryCode, int maximumOutputSize)
    {
        return Copy(FindInternal(countryCode), maximumOutputSize);
    }

    internal IbanFormat FindInternal(string countryCode)
    {
        string normalized = NormalizeCountryCode(countryCode);
        if (!ByCountry.ContainsKey(normalized))
        {
            throw new ArgumentException(
                $"countryCode '{countryCode}' is not present in the bundled IBAN registry transcription.",
                "countryCode");
        }
        return ByCountry[normalized];
    }

    internal IbanFormat? FindPrefix(string prefix)
    {
        if (!ByPrefix.ContainsKey(prefix))
        {
            return null;
        }
        return ByPrefix[prefix];
    }

    internal IbanFormat? FindCountryOrNull(string countryCode)
    {
        if (!ByCountry.ContainsKey(countryCode))
        {
            return null;
        }
        return ByCountry[countryCode];
    }

    internal IbanFormat[] CopyAll(int maximumCount, int maximumOutputSize)
    {
        Guard.Count(Values.Length, maximumCount, "ibanFormats");
        IbanFormat[] result = new IbanFormat[Values.Length];
        int total = 0;
        int index = 0;
        while (index < Values.Length)
        {
            result[index] = Copy(Values[index], FakeLimits.DefaultMaximumOutputSize);
            total = total + Size(result[index]);
            index = index + 1;
        }
        Guard.OutputSize(total, maximumOutputSize, "ibanFormats");
        return result;
    }

    internal IbanCatalogMetadata Metadata(int maximumOutputSize)
    {
        IbanCatalogMetadata result = new IbanCatalogMetadata();
        result.Source = Source;
        result.SourceUrl = SourceUrl;
        result.SourceVersion = SourceVersion;
        result.Upstream = Upstream;
        result.RetrievedOn = RetrievedOn;
        result.RecordCount = Values.Length;
        int size = Source.Length + SourceUrl.Length + SourceVersion.Length +
            Upstream.Length + RetrievedOn.Length;
        Guard.OutputSize(size, maximumOutputSize, "ibanMetadata");
        return result;
    }

    static IbanFormat Copy(IbanFormat value, int maximumOutputSize)
    {
        Guard.OutputSize(Size(value), maximumOutputSize, "ibanFormat");
        IbanFormat result = new IbanFormat();
        result.CountryCode = value.CountryCode;
        result.IbanPrefix = value.IbanPrefix;
        result.IbanLength = value.IbanLength;
        result.BbanPattern = value.BbanPattern;
        result.BbanLength = value.BbanLength;
        result.IsSepa = value.IsSepa;
        result.BankCodeStart = value.BankCodeStart;
        result.BankCodeLength = value.BankCodeLength;
        result.BranchCodeStart = value.BranchCodeStart;
        result.BranchCodeLength = value.BranchCodeLength;
        result.AccountCodeStart = value.AccountCodeStart;
        result.AccountCodeLength = value.AccountCodeLength;
        result.RegistryVersion = value.RegistryVersion;
        return result;
    }

    static int Size(IbanFormat value)
    {
        return value.CountryCode.Length + value.IbanPrefix.Length +
            value.BbanPattern.Length + value.RegistryVersion.Length;
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

    static bool UpperAsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z';
    }

    static void Validate(IbanFormat value)
    {
        if (value.CountryCode == null || value.CountryCode.Length != 2 ||
            !UpperAsciiLetter(value.CountryCode[0]) ||
            !UpperAsciiLetter(value.CountryCode[1]))
        {
            throw FakeException.Create("Bundled IBAN metadata contains an invalid country code.");
        }
        if (value.IbanPrefix == null || value.IbanPrefix.Length != 2 ||
            !UpperAsciiLetter(value.IbanPrefix[0]) ||
            !UpperAsciiLetter(value.IbanPrefix[1]))
        {
            throw FakeException.Create($"Bundled IBAN record '{value.CountryCode}' contains an invalid IBAN prefix.");
        }
        Guard.NotNullOrWhiteSpace(value.BbanPattern, "bbanPattern");
        Guard.NotNullOrWhiteSpace(value.RegistryVersion, "registryVersion");
        if (value.BbanLength <= 0 || value.IbanLength != value.BbanLength + 4 ||
            value.IbanLength > 34 || PatternLength(value.BbanPattern) != value.BbanLength)
        {
            throw FakeException.Create($"Bundled IBAN record '{value.CountryCode}' contains inconsistent lengths.");
        }
        ValidateRange(value.BankCodeStart, value.BankCodeLength, value.BbanLength, value.CountryCode);
        ValidateRange(value.BranchCodeStart, value.BranchCodeLength, value.BbanLength, value.CountryCode);
        ValidateRange(value.AccountCodeStart, value.AccountCodeLength, value.BbanLength, value.CountryCode);
    }

    static int PatternLength(string pattern)
    {
        int total = 0;
        int index = 0;
        while (index < pattern.Length)
        {
            int length = 0;
            while (index < pattern.Length && pattern[index] >= '0' && pattern[index] <= '9')
            {
                length = length * 10 + pattern[index] - '0';
                index = index + 1;
            }
            if (length <= 0 || index + 1 >= pattern.Length || pattern[index] != '!' ||
                pattern[index + 1] != 'n' && pattern[index + 1] != 'a' && pattern[index + 1] != 'c')
            {
                throw FakeException.Create($"Bundled IBAN metadata contains invalid BBAN pattern '{pattern}'.");
            }
            total = total + length;
            index = index + 2;
        }
        return total;
    }

    static void ValidateRange(int start, int length, int maximum, string countryCode)
    {
        if (start < 0 || length < 0 || start > maximum || length > maximum - start)
        {
            throw FakeException.Create($"Bundled IBAN record '{countryCode}' contains an invalid component range.");
        }
    }
}

internal static class IbanRegistryStore
{
    static readonly IbanRegistry?[] Value = new IbanRegistry?[1];
    static readonly object Sync = new object();

    internal static IbanRegistry Current()
    {
        lock (Sync)
        {
            IbanRegistry? current = Value[0];
            if (current == null)
            {
                current = Load();
                Value[0] = current;
            }
            return current;
        }
    }

    static IbanRegistry Load()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream? stream = assembly.GetManifestResourceStream(
            "fake.Data.standards.iban-formats.json");
        if (stream == null)
        {
            throw FakeException.Create("Embedded IBAN format metadata resource is missing.");
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
                string upstream = JsonText.Required(source.GetProperty("upstream"), "$.source.upstream");
                string retrievedOn = JsonText.Required(source.GetProperty("retrievedOn"), "$.source.retrievedOn");
                List<IbanFormat> values = new List<IbanFormat>();
                foreach (JsonElement item in root.GetProperty("records").EnumerateArray())
                {
                    IbanFormat value = new IbanFormat();
                    value.CountryCode = JsonText.Required(item.GetProperty("countryCode"), "$.records[].countryCode");
                    value.IbanPrefix = JsonText.Required(item.GetProperty("ibanPrefix"), "$.records[].ibanPrefix");
                    value.IbanLength = item.GetProperty("ibanLength").GetInt32();
                    value.BbanPattern = JsonText.Required(item.GetProperty("bbanPattern"), "$.records[].bbanPattern");
                    value.BbanLength = item.GetProperty("bbanLength").GetInt32();
                    value.IsSepa = item.GetProperty("sepa").GetBoolean();
                    value.BankCodeStart = item.GetProperty("bankCodeStart").GetInt32();
                    value.BankCodeLength = item.GetProperty("bankCodeLength").GetInt32();
                    value.BranchCodeStart = item.GetProperty("branchCodeStart").GetInt32();
                    value.BranchCodeLength = item.GetProperty("branchCodeLength").GetInt32();
                    value.AccountCodeStart = item.GetProperty("accountCodeStart").GetInt32();
                    value.AccountCodeLength = item.GetProperty("accountCodeLength").GetInt32();
                    value.RegistryVersion = sourceVersion;
                    values.Add(value);
                }
                return new IbanRegistry(
                    sourceName,
                    sourceUrl,
                    sourceVersion,
                    upstream,
                    retrievedOn,
                    values.ToArray());
            }
        }
    }
}
