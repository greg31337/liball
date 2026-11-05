using System;
using System.Globalization;
using System.Text;

namespace fake;

internal static class UriGenerator
{
    const string Hex = "0123456789ABCDEF";
    static readonly string[] PathValues = new string[]
    {
        "alpha",
        "sample file",
        "draft copy",
        "report+draft",
        "a/b",
        "100%"
    };
    static readonly string[] QueryNames = new string[]
    {
        "page",
        "search term",
        "category",
        "filter value",
        "next/url"
    };
    static readonly string[] QueryValues = new string[]
    {
        "alpha",
        "sample value",
        "review copy",
        "a&b",
        "one=two",
        "100%"
    };
    static readonly string[] FragmentValues = new string[]
    {
        "section one",
        "overview",
        "part/one",
        "question?two",
        "100%"
    };
    static readonly string[] UserInfoValues = new string[]
    {
        "fixture user",
        "sample",
        "reader name",
        "reader+one"
    };

    internal static string Encode(
        string value,
        int maximumOutputSize)
    {
        Guard.NotNull(value, "value");
        UnicodeText.Validate(value, "value");
        Guard.OutputSize(value.Length, maximumOutputSize, "value");
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        int length = 0;
        int byteIndex = 0;
        while (byteIndex < bytes.Length)
        {
            length = length + (Unreserved(bytes[byteIndex]) ? 1 : 3);
            byteIndex = byteIndex + 1;
        }
        Guard.OutputSize(length, maximumOutputSize, "value");
        char[] result = new char[length];
        int resultIndex = 0;
        byteIndex = 0;
        while (byteIndex < bytes.Length)
        {
            byte valueByte = bytes[byteIndex];
            if (Unreserved(valueByte))
            {
                result[resultIndex] = (char)valueByte;
                resultIndex = resultIndex + 1;
            }
            else
            {
                result[resultIndex] = '%';
                result[resultIndex + 1] = Hex[valueByte >> 4];
                result[resultIndex + 2] = Hex[valueByte & 15];
                resultIndex = resultIndex + 3;
            }
            byteIndex = byteIndex + 1;
        }
        return new string(result);
    }

    internal static string UserInfo(
        FakeRandom random,
        int maximumOutputSize)
    {
        string raw = UserInfoValues[random.IntFromMaximumExclusive(UserInfoValues.Length)] +
            "-" +
            random.IntFromMaximumExclusive(1000).ToString("D3", CultureInfo.InvariantCulture);
        return Encode(raw, maximumOutputSize);
    }

    internal static string PathSegment(
        FakeRandom random,
        int maximumOutputSize)
    {
        string raw = PathValues[random.IntFromMaximumExclusive(PathValues.Length)] +
            "-" +
            random.IntFromMaximumExclusive(1000).ToString("D3", CultureInfo.InvariantCulture);
        return Encode(raw, maximumOutputSize);
    }

    internal static string Path(
        FakeRandom random,
        int segmentCount,
        int maximumCount,
        int maximumOutputSize)
    {
        Guard.Count(segmentCount, maximumCount, "segmentCount");
        if (segmentCount == 0)
        {
            return string.Empty;
        }
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < segmentCount)
        {
            builder.Append('/');
            builder.Append(PathSegment(random, maximumOutputSize));
            Guard.OutputSize(builder.Length, maximumOutputSize, "segmentCount");
            index = index + 1;
        }
        return builder.ToString();
    }

    internal static string Query(
        FakeRandom random,
        int parameterCount,
        int maximumCount,
        int maximumOutputSize)
    {
        Guard.Count(parameterCount, maximumCount, "parameterCount");
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < parameterCount)
        {
            if (builder.Length != 0)
            {
                builder.Append('&');
            }
            string name = QueryNames[random.IntFromMaximumExclusive(QueryNames.Length)] +
                "-" +
                index.ToString(CultureInfo.InvariantCulture);
            string value = QueryValues[random.IntFromMaximumExclusive(QueryValues.Length)] +
                "-" +
                random.IntFromMaximumExclusive(1000).ToString("D3", CultureInfo.InvariantCulture);
            builder.Append(Encode(name, maximumOutputSize));
            builder.Append('=');
            builder.Append(Encode(value, maximumOutputSize));
            Guard.OutputSize(builder.Length, maximumOutputSize, "parameterCount");
            index = index + 1;
        }
        return builder.ToString();
    }

    internal static string Fragment(
        FakeRandom random,
        int maximumOutputSize)
    {
        string raw = FragmentValues[random.IntFromMaximumExclusive(FragmentValues.Length)] +
            "-" +
            random.IntFromMaximumExclusive(1000).ToString("D3", CultureInfo.InvariantCulture);
        return Encode(raw, maximumOutputSize);
    }

    internal static string Url(
        GenerationContext context,
        FakeRandom random,
        string scheme,
        bool includeUserInfo,
        bool includePath,
        bool includeQuery,
        bool includeFragment)
    {
        string normalizedScheme = Scheme(scheme);
        StringBuilder builder = new StringBuilder();
        builder.Append(normalizedScheme);
        builder.Append("://");
        if (includeUserInfo)
        {
            builder.Append(UserInfo(random, context.CapturedOptions.MaximumOutputSize));
            builder.Append('@');
        }
        builder.Append(DomainNameGenerator.HostName(context, random));
        if (includePath)
        {
            builder.Append(Path(
                random,
                random.IntFromMinimumAndMaximum(1, 5),
                context.CapturedOptions.MaximumCount,
                context.CapturedOptions.MaximumOutputSize));
        }
        if (includeQuery)
        {
            builder.Append('?');
            builder.Append(Query(
                random,
                random.IntFromMinimumAndMaximum(1, 5),
                context.CapturedOptions.MaximumCount,
                context.CapturedOptions.MaximumOutputSize));
        }
        if (includeFragment)
        {
            builder.Append('#');
            builder.Append(Fragment(random, context.CapturedOptions.MaximumOutputSize));
        }
        Guard.OutputSize(
            builder.Length,
            context.CapturedOptions.MaximumOutputSize,
            "url");
        return builder.ToString();
    }

    internal static string Invalid(
        UriInvalidKind kind,
        int maximumOutputSize)
    {
        string value = string.Empty;
        switch (kind)
        {
            case UriInvalidKind.Scheme:
                value = "1https://example.com/path";
                break;
            case UriInvalidKind.Host:
                value = "https://-bad.example.com/path";
                break;
            case UriInvalidKind.PercentEncoding:
                value = "https://example.com/path%";
                break;
            case UriInvalidKind.Whitespace:
                value = "https://example.com/a path";
                break;
            case UriInvalidKind.Authority:
                value = "https//example.com/path";
                break;
            case UriInvalidKind.UserInfo:
                value = "https://user@@example.com/path";
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    "kind",
                    kind,
                    "kind is not a supported URI invalidity category.");
        }
        Guard.OutputSize(value.Length, maximumOutputSize, "url");
        return value;
    }

    static string Scheme(string value)
    {
        Guard.NotNullOrWhiteSpace(value, "scheme");
        if (!AsciiLetter(value[0]))
        {
            throw new ArgumentException("scheme must begin with an ASCII letter.", "scheme");
        }
        int index = 1;
        while (index < value.Length)
        {
            char character = value[index];
            if (!AsciiLetter(character) &&
                !AsciiDigit(character) &&
                character != '+' &&
                character != '-' &&
                character != '.')
            {
                throw new ArgumentException(
                    "scheme may contain only ASCII letters, digits, plus, hyphen, and period after its first letter.",
                    "scheme");
            }
            index = index + 1;
        }
        return value.ToLowerInvariant();
    }

    static bool Unreserved(byte value)
    {
        return value >= (byte)'a' && value <= (byte)'z' ||
            value >= (byte)'A' && value <= (byte)'Z' ||
            value >= (byte)'0' && value <= (byte)'9' ||
            value == (byte)'-' ||
            value == (byte)'.' ||
            value == (byte)'_' ||
            value == (byte)'~';
    }

    static bool AsciiLetter(char value)
    {
        return value >= 'a' && value <= 'z' ||
            value >= 'A' && value <= 'Z';
    }

    static bool AsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }
}
