using System;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace fake;

internal static class DomainNameGenerator
{
    internal const int MaximumLabelLength = 63;
    internal const int MaximumDomainLength = 253;
    const string Letters = "abcdefghijklmnopqrstuvwxyz";
    const string InteriorCharacters = "abcdefghijklmnopqrstuvwxyz0123456789-";
    const string FinalCharacters = "abcdefghijklmnopqrstuvwxyz0123456789";
    static readonly string[] IdnLabelPrefixes = new string[]
    {
        "idné", "δοκιμή", "пример", "例え"
    };

    internal static string Label(
        FakeRandom random,
        int length,
        int maximumOutputSize)
    {
        if (length < 1 || length > MaximumLabelLength)
        {
            throw new ArgumentOutOfRangeException(
                "length",
                length,
                $"length must be between 1 and {MaximumLabelLength}, inclusive.");
        }
        Guard.OutputSize(length, maximumOutputSize, "length");
        char[] result = new char[length];
        result[0] = Letters[random.IntFromMaximumExclusive(Letters.Length)];
        int index = 1;
        while (index < length - 1)
        {
            result[index] = InteriorCharacters[random.IntFromMaximumExclusive(InteriorCharacters.Length)];
            index = index + 1;
        }
        if (length > 1)
        {
            result[length - 1] = FinalCharacters[random.IntFromMaximumExclusive(FinalCharacters.Length)];
        }
        return new string(result);
    }

    internal static string DomainWord(FakeRandom random, int maximumOutputSize)
    {
        int length = random.IntFromMinimumAndMaximum(5, 13);
        return Label(random, length, maximumOutputSize);
    }

    internal static string ExampleDomain(
        GenerationContext context,
        FakeRandom random)
    {
        string[] values = DocumentationDomains(context);
        return values[random.IntFromMaximumExclusive(values.Length)];
    }

    internal static string DomainName(
        GenerationContext context,
        FakeRandom random)
    {
        string suffix;
        if (context.SafetyModeValue == SafetyMode.Reserved)
        {
            suffix = ExampleDomain(context, random);
        }
        else
        {
            suffix = DomainSuffixRegistryStore.Current().Choose(random);
        }
        string value = DomainWord(random, MaximumLabelLength) + "." + suffix;
        ValidateAsciiDomain(value, "domainName");
        Guard.OutputSize(
            value.Length,
            context.CapturedOptions.MaximumOutputSize,
            "domainName");
        return value;
    }

    internal static string HostName(
        GenerationContext context,
        FakeRandom random)
    {
        int subdomainCount = random.IntFromMinimumAndMaximum(1, 4);
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < subdomainCount)
        {
            if (builder.Length != 0)
            {
                builder.Append('.');
            }
            builder.Append(DomainWord(random, MaximumLabelLength));
            index = index + 1;
        }
        builder.Append('.');
        builder.Append(DomainName(context, random));
        string value = builder.ToString();
        ValidateAsciiDomain(value, "hostName");
        Guard.OutputSize(
            value.Length,
            context.CapturedOptions.MaximumOutputSize,
            "hostName");
        return value;
    }

    internal static string IdnDomainName(
        GenerationContext context,
        FakeRandom random)
    {
        string unicodeLabel =
            IdnLabelPrefixes[random.IntFromMaximumExclusive(IdnLabelPrefixes.Length)] +
            random.IntFromMaximumExclusive(1000).ToString("D3", CultureInfo.InvariantCulture);
        string suffix;
        if (context.SafetyModeValue == SafetyMode.Reserved)
        {
            suffix = ExampleDomain(context, random);
        }
        else
        {
            suffix = DomainSuffixRegistryStore.Current().Choose(random);
        }
        string value = unicodeLabel + "." + suffix;
        string ascii = ToAscii(value, context.CapturedOptions.MaximumOutputSize);
        return ToUnicode(ascii, context.CapturedOptions.MaximumOutputSize);
    }

    internal static string ToAscii(string domainName, int maximumOutputSize)
    {
        Guard.NotNullOrWhiteSpace(domainName, "domainName");
        string normalized = NormalizeSeparators(domainName);
        bool absolute = normalized.EndsWith(".", StringComparison.Ordinal);
        string body = absolute
            ? normalized.Substring(0, normalized.Length - 1)
            : normalized;
        if (body.Length == 0)
        {
            throw new ArgumentException("domainName must contain at least one label.", "domainName");
        }
        string[] labels = body.Split(new char[] { '.' });
        IdnMapping mapping = Mapping();
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < labels.Length)
        {
            if (labels[index].Length == 0)
            {
                throw new ArgumentException("domainName cannot contain an empty label.", "domainName");
            }
            string ascii = mapping.GetAscii(labels[index]).ToLowerInvariant();
            ValidateAsciiLabel(ascii, "domainName");
            string unicode = mapping.GetUnicode(ascii)
                .Normalize(NormalizationForm.FormC);
            ValidateUnicodeSecurityLabel(unicode);
            string roundTrip = mapping.GetAscii(unicode).ToLowerInvariant();
            if (roundTrip != ascii)
            {
                throw new ArgumentException(
                    "domainName contains a noncanonical Punycode label.",
                    "domainName");
            }
            if (builder.Length != 0)
            {
                builder.Append('.');
            }
            builder.Append(ascii);
            index = index + 1;
        }
        if (builder.Length > MaximumDomainLength)
        {
            throw new ArgumentOutOfRangeException(
                "domainName",
                domainName,
                $"domainName cannot exceed {MaximumDomainLength} ASCII characters excluding a final root dot.");
        }
        if (absolute)
        {
            builder.Append('.');
        }
        Guard.OutputSize(builder.Length, maximumOutputSize, "domainName");
        return builder.ToString();
    }

    internal static string ToUnicode(string domainName, int maximumOutputSize)
    {
        string canonicalAscii = ToAscii(
            domainName,
            Math.Max(maximumOutputSize, MaximumDomainLength + 1));
        bool absolute = canonicalAscii.EndsWith(".", StringComparison.Ordinal);
        string body = absolute
            ? canonicalAscii.Substring(0, canonicalAscii.Length - 1)
            : canonicalAscii;
        string[] labels = body.Split(new char[] { '.' });
        IdnMapping mapping = Mapping();
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < labels.Length)
        {
            if (builder.Length != 0)
            {
                builder.Append('.');
            }
            builder.Append(mapping.GetUnicode(labels[index]).Normalize(NormalizationForm.FormC));
            index = index + 1;
        }
        if (absolute)
        {
            builder.Append('.');
        }
        string result = builder.ToString();
        string roundTrip = ToAscii(result, Math.Max(maximumOutputSize, MaximumDomainLength + 1));
        if (roundTrip != canonicalAscii)
        {
            throw new ArgumentException("domainName is not a canonical round-trip IDN.", "domainName");
        }
        Guard.OutputSize(result.Length, maximumOutputSize, "domainName");
        return result;
    }

    internal static void ValidateAsciiDomain(string value, string parameterName)
    {
        Guard.NotNullOrWhiteSpace(value, parameterName);
        if (value.Length > MaximumDomainLength)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"{parameterName} cannot exceed {MaximumDomainLength} ASCII characters.");
        }
        string[] labels = value.Split(new char[] { '.' });
        int index = 0;
        while (index < labels.Length)
        {
            ValidateAsciiLabel(labels[index], parameterName);
            index = index + 1;
        }
    }

    internal static void ValidateAsciiLabel(string value, string parameterName)
    {
        Guard.NotNullOrEmpty(value, parameterName);
        if (value.Length > MaximumLabelLength)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"{parameterName} labels cannot exceed {MaximumLabelLength} ASCII characters.");
        }
        if (!AsciiLetterOrDigit(value[0]) ||
            !AsciiLetterOrDigit(value[value.Length - 1]))
        {
            throw new ArgumentException(
                $"{parameterName} labels must start and end with an ASCII letter or digit.",
                parameterName);
        }
        int index = 1;
        while (index < value.Length - 1)
        {
            char character = value[index];
            if (!AsciiLetterOrDigit(character) && character != '-')
            {
                throw new ArgumentException(
                    $"{parameterName} labels may contain only ASCII letters, digits, and hyphens.",
                    parameterName);
            }
            index = index + 1;
        }
    }

    static string[] DocumentationDomains(GenerationContext context)
    {
        object source = LocaleResolver.Resolve(
            context,
            context.CapturedOptions.Locale,
            "internet.domain.documentation",
            context.LocaleValues());
        string json = JsonSerializer.Serialize(source);
        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            {
                throw FakeException.Create("Resolved documentation-domain data must be a nonempty array.");
            }
            Guard.Count(
                root.GetArrayLength(),
                context.CapturedOptions.MaximumCount,
                "documentationDomains");
            string[] result = new string[root.GetArrayLength()];
            int index = 0;
            while (index < result.Length)
            {
                if (root[index].ValueKind != JsonValueKind.String)
                {
                    throw FakeException.Create("Resolved documentation-domain data must contain only strings.");
                }
                result[index] = JsonText.Required(
                    root[index],
                    "documentationDomain").ToLowerInvariant();
                ValidateAsciiDomain(result[index], "documentationDomain");
                index = index + 1;
            }
            return result;
        }
    }

    static IdnMapping Mapping()
    {
        IdnMapping result = new IdnMapping();
        result.AllowUnassigned = false;
        result.UseStd3AsciiRules = true;
        return result;
    }

    static string NormalizeSeparators(string value)
    {
        return value
            .Replace('\u3002', '.')
            .Replace('\uff0e', '.')
            .Replace('\uff61', '.');
    }

    static bool AsciiLetterOrDigit(char value)
    {
        return value >= 'a' && value <= 'z' ||
            value >= 'A' && value <= 'Z' ||
            value >= '0' && value <= '9';
    }

    static void ValidateUnicodeSecurityLabel(string value)
    {
        int script = 0;
        int index = 0;
        while (index < value.Length)
        {
            int codePoint = char.ConvertToUtf32(value, index);
            int current = SecurityScript(codePoint);
            if (current != 0)
            {
                if (script != 0 && script != current)
                {
                    throw new ArgumentException(
                        "domainName labels may not mix Latin, Greek, and Cyrillic scripts.",
                        "domainName");
                }
                script = current;
            }
            if (codePoint > 0xffff)
            {
                index = index + 1;
            }
            index = index + 1;
        }
    }

    static int SecurityScript(int codePoint)
    {
        if (codePoint >= 0x0041 && codePoint <= 0x005a ||
            codePoint >= 0x0061 && codePoint <= 0x007a ||
            codePoint >= 0x00c0 && codePoint <= 0x024f ||
            codePoint >= 0x1e00 && codePoint <= 0x1eff)
        {
            return 1;
        }
        if (codePoint >= 0x0370 && codePoint <= 0x03ff ||
            codePoint >= 0x1f00 && codePoint <= 0x1fff)
        {
            return 2;
        }
        if (codePoint >= 0x0400 && codePoint <= 0x052f ||
            codePoint >= 0x2de0 && codePoint <= 0x2dff ||
            codePoint >= 0xa640 && codePoint <= 0xa69f)
        {
            return 3;
        }
        return 0;
    }
}
