namespace ttf;

internal class SfntDecodedName
{
    string value;
    bool encodingSupported;
    int replacementCount;

    internal SfntDecodedName(
        string value,
        bool encodingSupported,
        int replacementCount)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        if (replacementCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("replacementCount");
        }

        this.value = value;
        this.encodingSupported = encodingSupported;
        this.replacementCount = replacementCount;
    }

    internal string GetValue()
    {
        return value;
    }

    internal bool IsEncodingSupported()
    {
        return encodingSupported;
    }

    internal bool HasReplacementCharacters()
    {
        return replacementCount != 0;
    }

    internal int GetReplacementCount()
    {
        return replacementCount;
    }

    internal bool IsValid()
    {
        return encodingSupported && replacementCount == 0 && value.Length != 0;
    }

    internal int GetDiagnosticCount()
    {
        if (replacementCount != 0)
        {
            return 2;
        }

        return IsValid() ? 0 : 1;
    }

    internal FontDiagnosticCode GetDiagnosticCode(int index)
    {
        if (index < 0 || index >= GetDiagnosticCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        if (index == 0)
        {
            return FontDiagnosticCode.InvalidNameEncoding;
        }

        return FontDiagnosticCode.StringDecodingReplacement;
    }
}

internal static class SfntNameDecoder
{
    const char ReplacementCharacter = '\uFFFD';

    internal static SfntDecodedName Decode(SfntNameRecord record)
    {
        if (record == null)
        {
            throw new System.ArgumentNullException("record");
        }

        SfntNameEncodingKind kind = record.GetEncodingKind();

        if (kind == SfntNameEncodingKind.Utf16BigEndian)
        {
            return DecodeUtf16BigEndian(record.GetStringData());
        }

        if (kind == SfntNameEncodingKind.MacintoshRoman)
        {
            int codePage = GetMacintoshRomanOverrideCodePage(
                record.GetLanguageId());

            if (codePage == 0)
            {
                codePage = 10000;
            }

            return DecodeCodePage(record.GetStringData(), codePage);
        }

        if (kind == SfntNameEncodingKind.MacintoshLegacy)
        {
            return DecodeCodePage(
                record.GetStringData(),
                GetMacintoshCodePage(record.GetEncodingId()));
        }

        if (kind == SfntNameEncodingKind.WindowsCodePage936)
        {
            return DecodeCodePage(record.GetStringData(), 936);
        }

        if (kind == SfntNameEncodingKind.WindowsCodePage932)
        {
            return DecodeCodePage(record.GetStringData(), 932);
        }

        if (kind == SfntNameEncodingKind.WindowsCodePage950)
        {
            return DecodeCodePage(record.GetStringData(), 950);
        }

        if (kind == SfntNameEncodingKind.WindowsCodePage949)
        {
            return DecodeCodePage(record.GetStringData(), 949);
        }

        return new SfntDecodedName(string.Empty, false, 0);
    }

    internal static bool IsEncodingSupported(SfntNameRecord record)
    {
        if (record == null)
        {
            throw new System.ArgumentNullException("record");
        }

        SfntNameEncodingKind kind = record.GetEncodingKind();

        if (kind == SfntNameEncodingKind.Utf16BigEndian)
        {
            return true;
        }

        if (kind == SfntNameEncodingKind.MacintoshRoman)
        {
            int codePage = GetMacintoshRomanOverrideCodePage(
                record.GetLanguageId());
            return IsCodePageAvailable(codePage == 0 ? 10000 : codePage);
        }

        if (kind == SfntNameEncodingKind.MacintoshLegacy)
        {
            return IsCodePageAvailable(
                GetMacintoshCodePage(record.GetEncodingId()));
        }

        if (kind == SfntNameEncodingKind.WindowsCodePage936)
        {
            return IsCodePageAvailable(936);
        }

        if (kind == SfntNameEncodingKind.WindowsCodePage932)
        {
            return IsCodePageAvailable(932);
        }

        if (kind == SfntNameEncodingKind.WindowsCodePage950)
        {
            return IsCodePageAvailable(950);
        }

        if (kind == SfntNameEncodingKind.WindowsCodePage949)
        {
            return IsCodePageAvailable(949);
        }

        return false;
    }

    static SfntDecodedName DecodeUtf16BigEndian(FontDataSlice stringData)
    {
        ReadOnlySpan<byte> bytes = stringData.GetMemory().Span;
        char[] characters = new char[(bytes.Length + 1) / 2];
        int byteIndex = 0;
        int characterIndex = 0;
        int replacementCount = 0;

        while (byteIndex < bytes.Length)
        {
            if (byteIndex + 1 >= bytes.Length)
            {
                characters[characterIndex] = ReplacementCharacter;
                characterIndex += 1;
                replacementCount += 1;
                break;
            }

            int codeUnit = bytes[byteIndex] << 8 | bytes[byteIndex + 1];

            if (codeUnit >= 0xD800 && codeUnit <= 0xDBFF)
            {
                if (byteIndex + 3 < bytes.Length)
                {
                    int lowSurrogate = bytes[byteIndex + 2] << 8 |
                        bytes[byteIndex + 3];

                    if (lowSurrogate >= 0xDC00 && lowSurrogate <= 0xDFFF)
                    {
                        characters[characterIndex] = (char)codeUnit;
                        characters[characterIndex + 1] = (char)lowSurrogate;
                        characterIndex += 2;
                        byteIndex += 4;
                        continue;
                    }
                }

                characters[characterIndex] = ReplacementCharacter;
                replacementCount += 1;
            }
            else if (codeUnit >= 0xDC00 && codeUnit <= 0xDFFF)
            {
                characters[characterIndex] = ReplacementCharacter;
                replacementCount += 1;
            }
            else
            {
                characters[characterIndex] = (char)codeUnit;
            }

            characterIndex += 1;
            byteIndex += 2;
        }

        return new SfntDecodedName(
            new string(characters, 0, characterIndex),
            true,
            replacementCount);
    }

    static SfntDecodedName DecodeCodePage(
        FontDataSlice stringData,
        int codePage)
    {
        if (codePage == 0)
        {
            return new SfntDecodedName(string.Empty, false, 0);
        }

        LegacyDecodedText? decoded = LegacyEncodingData.Decode(
            stringData,
            codePage);

        if (decoded == null)
        {
            return new SfntDecodedName(string.Empty, false, 0);
        }

        return new SfntDecodedName(
            decoded.GetValue(),
            true,
            decoded.GetReplacementCount());
    }

    static bool IsCodePageAvailable(int codePage)
    {
        return LegacyEncodingData.IsCodePageSupported(codePage);
    }

    static int GetMacintoshRomanOverrideCodePage(int languageId)
    {
        if (languageId == 15)
        {
            return 10079;
        }

        if (languageId == 17)
        {
            return 10081;
        }

        if (languageId == 18)
        {
            return 10082;
        }

        if (languageId == 24 || languageId == 25 || languageId == 26 ||
            languageId == 27 || languageId == 28 || languageId == 36 ||
            languageId == 38 || languageId == 39 || languageId == 40)
        {
            return 10029;
        }

        if (languageId == 37)
        {
            return 10010;
        }

        return 0;
    }

    static int GetMacintoshCodePage(int encodingId)
    {
        if (encodingId >= 1 && encodingId <= 7)
        {
            return 10000 + encodingId;
        }

        if (encodingId == 21)
        {
            return 10021;
        }

        if (encodingId == 25)
        {
            return 10008;
        }

        if (encodingId == 29)
        {
            return 10029;
        }

        if (encodingId == 35)
        {
            return 10081;
        }

        if (encodingId == 37)
        {
            return 10079;
        }

        return 0;
    }
}
