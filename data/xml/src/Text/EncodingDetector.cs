using System;
using System.Text;

namespace xml;

internal enum ByteOrderMarkKind
{
    None = 0,
    Utf8 = 1,
    Utf16BigEndian = 2,
    Utf16LittleEndian = 3,
    Utf32BigEndian = 4,
    Utf32LittleEndian = 5,
    Utf32Unusual2143 = 6,
    Utf32Unusual3412 = 7
}

internal enum InitialEncodingPatternKind
{
    Unknown = 0,
    AsciiCompatible = 1,
    Utf16BigEndian = 2,
    Utf16LittleEndian = 3,
    Utf32BigEndian = 4,
    Utf32LittleEndian = 5,
    Utf32Unusual2143 = 6,
    Utf32Unusual3412 = 7,
    Ebcdic = 8
}

internal enum EncodingPolicyKind
{
    UnicodeOnly = 0,
    BuiltInText = 1
}

internal static class EncodingDetector
{
    internal static ByteOrderMarkKind DetectByteOrderMark(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
        {
            return ByteOrderMarkKind.Utf32BigEndian;
        }

        if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00)
        {
            return ByteOrderMarkKind.Utf32LittleEndian;
        }

        if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFF && bytes[3] == 0xFE)
        {
            return ByteOrderMarkKind.Utf32Unusual2143;
        }

        if (bytes.Length >= 4 && bytes[0] == 0xFE && bytes[1] == 0xFF && bytes[2] == 0x00 && bytes[3] == 0x00)
        {
            return ByteOrderMarkKind.Utf32Unusual3412;
        }

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return ByteOrderMarkKind.Utf8;
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return ByteOrderMarkKind.Utf16BigEndian;
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return ByteOrderMarkKind.Utf16LittleEndian;
        }

        return ByteOrderMarkKind.None;
    }

    internal static int GetByteOrderMarkLength(ByteOrderMarkKind kind)
    {
        if (kind == ByteOrderMarkKind.Utf8)
        {
            return 3;
        }

        if (kind == ByteOrderMarkKind.Utf16BigEndian || kind == ByteOrderMarkKind.Utf16LittleEndian)
        {
            return 2;
        }

        if (kind == ByteOrderMarkKind.Utf32BigEndian || kind == ByteOrderMarkKind.Utf32LittleEndian || kind == ByteOrderMarkKind.Utf32Unusual2143 || kind == ByteOrderMarkKind.Utf32Unusual3412)
        {
            return 4;
        }

        return 0;
    }

    internal static InitialEncodingPatternKind DetectInitialEncodingPattern(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4)
        {
            return InitialEncodingPatternKind.Unknown;
        }

        if (bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0x00 && bytes[3] == 0x3C)
        {
            return InitialEncodingPatternKind.Utf32BigEndian;
        }

        if (bytes[0] == 0x3C && bytes[1] == 0x00 && bytes[2] == 0x00 && bytes[3] == 0x00)
        {
            return InitialEncodingPatternKind.Utf32LittleEndian;
        }

        if (bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0x3C && bytes[3] == 0x00)
        {
            return InitialEncodingPatternKind.Utf32Unusual2143;
        }

        if (bytes[0] == 0x00 && bytes[1] == 0x3C && bytes[2] == 0x00 && bytes[3] == 0x00)
        {
            return InitialEncodingPatternKind.Utf32Unusual3412;
        }

        if (bytes[0] == 0x00 && bytes[1] == 0x3C && bytes[2] == 0x00 && bytes[3] == 0x3F)
        {
            return InitialEncodingPatternKind.Utf16BigEndian;
        }

        if (bytes[0] == 0x3C && bytes[1] == 0x00 && bytes[2] == 0x3F && bytes[3] == 0x00)
        {
            return InitialEncodingPatternKind.Utf16LittleEndian;
        }

        if (bytes[0] == 0x3C && bytes[1] == 0x3F && bytes[2] == 0x78 && bytes[3] == 0x6D)
        {
            return InitialEncodingPatternKind.AsciiCompatible;
        }

        if (bytes[0] == 0x4C && bytes[1] == 0x6F && bytes[2] == 0xA7 && bytes[3] == 0x94)
        {
            return InitialEncodingPatternKind.Ebcdic;
        }

        return InitialEncodingPatternKind.Unknown;
    }

    internal static Encoding ResolveXmlEncodingName(string encodingName, EncodingPolicyKind policy)
    {
        Guard.NotNullOrEmpty(encodingName, "encodingName");
        ValidateEncodingPolicy(policy);
        if (string.Equals(encodingName, "utf-8", StringComparison.OrdinalIgnoreCase) || string.Equals(encodingName, "utf8", StringComparison.OrdinalIgnoreCase))
        {
            return new UTF8Encoding(false, true);
        }

        if (string.Equals(encodingName, "utf-16", StringComparison.OrdinalIgnoreCase) || string.Equals(encodingName, "utf16", StringComparison.OrdinalIgnoreCase) || string.Equals(encodingName, "unicode", StringComparison.OrdinalIgnoreCase) || string.Equals(encodingName, "utf-16le", StringComparison.OrdinalIgnoreCase) || string.Equals(encodingName, "utf16le", StringComparison.OrdinalIgnoreCase))
        {
            return new UnicodeEncoding(false, false, true);
        }

        if (string.Equals(encodingName, "utf-16be", StringComparison.OrdinalIgnoreCase) || string.Equals(encodingName, "utf16be", StringComparison.OrdinalIgnoreCase) || string.Equals(encodingName, "unicodefffe", StringComparison.OrdinalIgnoreCase))
        {
            return new UnicodeEncoding(true, false, true);
        }

        if (string.Equals(encodingName, "utf-32", StringComparison.OrdinalIgnoreCase) || string.Equals(encodingName, "utf32", StringComparison.OrdinalIgnoreCase) || string.Equals(encodingName, "utf-32le", StringComparison.OrdinalIgnoreCase) || string.Equals(encodingName, "utf32le", StringComparison.OrdinalIgnoreCase))
        {
            return new UTF32Encoding(false, false, true);
        }

        if (string.Equals(encodingName, "utf-32be", StringComparison.OrdinalIgnoreCase) || string.Equals(encodingName, "utf32be", StringComparison.OrdinalIgnoreCase))
        {
            return new UTF32Encoding(true, false, true);
        }

        if (string.Equals(encodingName, "us-ascii", StringComparison.OrdinalIgnoreCase) || string.Equals(encodingName, "ascii", StringComparison.OrdinalIgnoreCase))
        {
            EnsureLegacyEncodingPermitted(policy, encodingName);
            return CreateStrictAsciiEncoding();
        }

        if (string.Equals(encodingName, "iso-8859-1", StringComparison.OrdinalIgnoreCase) || string.Equals(encodingName, "iso8859-1", StringComparison.OrdinalIgnoreCase) || string.Equals(encodingName, "latin1", StringComparison.OrdinalIgnoreCase) || string.Equals(encodingName, "latin-1", StringComparison.OrdinalIgnoreCase))
        {
            EnsureLegacyEncodingPermitted(policy, encodingName);
            return CreateStrictLatin1Encoding();
        }

        throw new NotSupportedException("The encoding is not a supported built-in XML encoding: " + encodingName + ".");
    }

    internal static Encoding ResolveCallerSelectedEncoding(XmlInput input, EncodingPolicyKind policy)
    {
        Guard.NotNull(input, "input");
        ValidateEncodingPolicy(policy);
        Encoding? selected = input.Encoding;
        if (selected != null)
        {
            return CreateStrictBuiltInEncoding(selected, policy);
        }

        string? name = input.EncodingName;
        if (name == null || name.Length == 0)
        {
            throw new ArgumentException("The XML input does not select an encoding.", "input");
        }

        return ResolveXmlEncodingName(name, policy);
    }

    internal static Encoding CreateStrictBuiltInEncoding(Encoding encoding, EncodingPolicyKind policy)
    {
        Guard.NotNull(encoding, "encoding");
        ValidateEncodingPolicy(policy);
        int codePage = encoding.CodePage;
        if (codePage == 65001)
        {
            return new UTF8Encoding(false, true);
        }

        if (codePage == 1200)
        {
            return new UnicodeEncoding(false, false, true);
        }

        if (codePage == 1201)
        {
            return new UnicodeEncoding(true, false, true);
        }

        if (codePage == 12000)
        {
            return new UTF32Encoding(false, false, true);
        }

        if (codePage == 12001)
        {
            return new UTF32Encoding(true, false, true);
        }

        if (codePage == 20127)
        {
            EnsureLegacyEncodingPermitted(policy, encoding.WebName);
            return CreateStrictAsciiEncoding();
        }

        if (codePage == 28591)
        {
            EnsureLegacyEncodingPermitted(policy, encoding.WebName);
            return CreateStrictLatin1Encoding();
        }

        throw new NotSupportedException("The encoding code page is not in the built-in XML encoding policy: " + codePage + ".");
    }

    internal static Encoding CreateStrictAsciiEncoding()
    {
        Encoding result = (Encoding)Encoding.ASCII.Clone();
        result.DecoderFallback = DecoderFallback.ExceptionFallback;
        result.EncoderFallback = EncoderFallback.ExceptionFallback;
        return result;
    }

    internal static Encoding CreateStrictLatin1Encoding()
    {
        Encoding result = (Encoding)Encoding.Latin1.Clone();
        result.DecoderFallback = DecoderFallback.ExceptionFallback;
        result.EncoderFallback = EncoderFallback.ExceptionFallback;
        return result;
    }

    internal static void EnsureLegacyEncodingPermitted(EncodingPolicyKind policy, string encodingName)
    {
        if (policy != EncodingPolicyKind.BuiltInText)
        {
            throw new NotSupportedException("The encoding is rejected by the active encoding policy: " + encodingName + ".");
        }
    }

    internal static void ValidateEncodingPolicy(EncodingPolicyKind policy)
    {
        if (policy != EncodingPolicyKind.UnicodeOnly && policy != EncodingPolicyKind.BuiltInText)
        {
            throw new ArgumentOutOfRangeException("policy");
        }
    }
}
