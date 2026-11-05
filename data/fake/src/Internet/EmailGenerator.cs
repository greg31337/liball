using System;
using System.Globalization;
using System.Text;

namespace fake;

internal static class EmailGenerator
{
    internal const int MaximumLocalPartOctets = 64;
    internal const int MaximumAddressOctets = 254;
    const string AtomCharacters = "abcdefghijklmnopqrstuvwxyz0123456789";
    static readonly string[] InternationalCharacters = new string[] { "é", "λ", "ж", "名" };
    static readonly string[] DisplayPrefixes = new string[]
    {
        "Fixture User",
        "Sample Person",
        "Example Contact"
    };
    static readonly string[] InternationalDisplayPrefixes = new string[]
    {
        "Fixture é",
        "Sample λ",
        "Example 名"
    };

    internal static string DisplayName(
        FakeRandom random,
        int maximumOutputSize)
    {
        string value = DisplayPrefixes[random.IntFromMaximumExclusive(DisplayPrefixes.Length)] +
            " " +
            random.IntFromMaximumExclusive(1000).ToString("D3", CultureInfo.InvariantCulture);
        Guard.OutputSize(value.Length, maximumOutputSize, "displayName");
        return value;
    }

    internal static string InternationalDisplayName(
        FakeRandom random,
        int maximumOutputSize)
    {
        string value = InternationalDisplayPrefixes[random.IntFromMaximumExclusive(InternationalDisplayPrefixes.Length)] +
            random.IntFromMaximumExclusive(1000).ToString("D3", CultureInfo.InvariantCulture);
        Guard.OutputSize(value.Length, maximumOutputSize, "displayName");
        return value;
    }

    internal static string LocalPart(
        FakeRandom random,
        EmailLocalPartCase kind,
        int maximumOutputSize)
    {
        string value = string.Empty;
        switch (kind)
        {
            case EmailLocalPartCase.Typical:
                value = Atom(random, 4, 11) + "." + Atom(random, 4, 11);
                break;
            case EmailLocalPartCase.PlusTagged:
                value = Atom(random, 5, 13) + "+tag" + random.IntFromMaximumExclusive(100).ToString("D2", CultureInfo.InvariantCulture);
                break;
            case EmailLocalPartCase.Apostrophe:
                value = Atom(random, 3, 9) + "'" + Atom(random, 3, 9);
                break;
            case EmailLocalPartCase.QuotedSpace:
                value = "\"local part " + random.IntFromMaximumExclusive(100).ToString("D2", CultureInfo.InvariantCulture) + "\"";
                break;
            case EmailLocalPartCase.QuotedSpecial:
                value = "\"quoted\\\"value\\\\part" + random.IntFromMaximumExclusive(10).ToString(CultureInfo.InvariantCulture) + "\"";
                break;
            case EmailLocalPartCase.MaximumLength:
                value = new string('a', MaximumLocalPartOctets);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    "kind",
                    kind,
                    "kind is not a supported email local-part case.");
        }
        ValidateLocalPartLength(value, false);
        Guard.OutputSize(value.Length, maximumOutputSize, "localPart");
        return value;
    }

    internal static string InternationalLocalPart(
        FakeRandom random,
        int maximumOutputSize)
    {
        string value = "mail" +
            InternationalCharacters[random.IntFromMaximumExclusive(InternationalCharacters.Length)] +
            ".fixture" +
            random.IntFromMaximumExclusive(1000).ToString("D3", CultureInfo.InvariantCulture);
        ValidateLocalPartLength(value, true);
        Guard.OutputSize(value.Length, maximumOutputSize, "localPart");
        return value;
    }

    internal static string Address(
        GenerationContext context,
        FakeRandom random,
        EmailLocalPartCase kind,
        bool forceExample)
    {
        string localPart = LocalPart(
            random,
            kind,
            context.CapturedOptions.MaximumOutputSize);
        string domain;
        if (forceExample || context.SafetyModeValue == SafetyMode.Reserved)
        {
            domain = DomainNameGenerator.ExampleDomain(context, random);
        }
        else
        {
            domain = DomainNameGenerator.DomainName(context, random);
        }
        DomainNameGenerator.ValidateAsciiDomain(domain, "domain");
        string value = localPart + "@" + domain;
        ValidateAddressLength(value);
        Guard.OutputSize(
            value.Length,
            context.CapturedOptions.MaximumOutputSize,
            "email");
        return value;
    }

    internal static string InternationalAddress(
        GenerationContext context,
        FakeRandom random)
    {
        string localPart = InternationalLocalPart(
            random,
            context.CapturedOptions.MaximumOutputSize);
        string domain = DomainNameGenerator.IdnDomainName(context, random);
        DomainNameGenerator.ToAscii(
            domain,
            Math.Max(
                context.CapturedOptions.MaximumOutputSize,
                DomainNameGenerator.MaximumDomainLength + 1));
        string value = localPart + "@" + domain;
        ValidateAddressLength(value);
        Guard.OutputSize(
            value.Length,
            context.CapturedOptions.MaximumOutputSize,
            "email");
        return value;
    }

    internal static string Mailbox(
        GenerationContext context,
        FakeRandom random,
        string displayName,
        bool international)
    {
        string formattedName = QuotedDisplayName(
            displayName,
            international,
            context.CapturedOptions.MaximumOutputSize);
        string address = international
            ? InternationalAddress(context, random)
            : Address(context, random, EmailLocalPartCase.Typical, false);
        string value = formattedName + " <" + address + ">";
        Guard.OutputSize(
            value.Length,
            context.CapturedOptions.MaximumOutputSize,
            "mailbox");
        return value;
    }

    static string Atom(
        FakeRandom random,
        int minimumLength,
        int maximumLength)
    {
        int length = random.IntFromMinimumAndMaximum(minimumLength, maximumLength);
        char[] result = new char[length];
        result[0] = (char)('a' + random.IntFromMaximumExclusive(26));
        int index = 1;
        while (index < result.Length)
        {
            result[index] = AtomCharacters[random.IntFromMaximumExclusive(AtomCharacters.Length)];
            index = index + 1;
        }
        return new string(result);
    }

    static string QuotedDisplayName(
        string value,
        bool international,
        int maximumOutputSize)
    {
        Guard.NotNullOrWhiteSpace(value, "displayName");
        UnicodeText.Validate(value, "displayName");
        StringBuilder builder = new StringBuilder();
        builder.Append('"');
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < 32 || character == 127)
            {
                throw new ArgumentException(
                    "displayName cannot contain control characters or header folding.",
                    "displayName");
            }
            if (!international && character > 127)
            {
                throw new ArgumentException(
                    "displayName must contain only ASCII characters for an RFC 5322 mailbox.",
                    "displayName");
            }
            if (character == '"' || character == '\\')
            {
                builder.Append('\\');
            }
            builder.Append(character);
            Guard.OutputSize(builder.Length + 1, maximumOutputSize, "displayName");
            index = index + 1;
        }
        builder.Append('"');
        return builder.ToString();
    }

    static void ValidateLocalPartLength(string value, bool international)
    {
        int length = international
            ? Encoding.UTF8.GetByteCount(value)
            : value.Length;
        if (length > MaximumLocalPartOctets)
        {
            throw FakeException.Create(
                $"Generated email local part exceeds the {MaximumLocalPartOctets}-octet SMTP limit.");
        }
    }

    static void ValidateAddressLength(string value)
    {
        int length = Encoding.UTF8.GetByteCount(value);
        if (length > MaximumAddressOctets)
        {
            throw FakeException.Create(
                $"Generated email address exceeds the {MaximumAddressOctets}-octet SMTP limit.");
        }
    }
}
