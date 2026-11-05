using System;
using System.Text;

namespace yaml;

internal class YamlByteDecodingResult
{
    internal string Text;
    internal YamlDiagnostic? Diagnostic;

    internal YamlByteDecodingResult()
    {
        Text = "";
        Diagnostic = null;
    }
}

internal class YamlByteValidationResult
{
    internal bool Valid;
    internal int CharacterCount;
    internal int NormalizedCharacterCount;
    internal int InvalidByteIndex;
    internal int InvalidCharacterIndex;
    internal int InvalidLine;
    internal int InvalidColumn;

    internal YamlByteValidationResult()
    {
        Valid = true;
        CharacterCount = 0;
        NormalizedCharacterCount = 0;
        InvalidByteIndex = 0;
        InvalidCharacterIndex = 0;
        InvalidLine = 1;
        InvalidColumn = 1;
    }
}

internal static class YamlByteDecoder
{
    internal static YamlByteDecodingResult Decode(ReadOnlySpan<byte> bytes, YamlSecurityOptions? options)
    {
        YamlByteDecodingResult result = new YamlByteDecodingResult();
        if (HasUtf32ByteOrderMark(bytes))
        {
            result.Diagnostic = EncodingError(0, 1, 1, "UTF-32 input is not supported; use UTF-8 or BOM-marked UTF-16.");
            return result;
        }

        int offset = 0;
        int encoding = 8;
        if (HasUtf8ByteOrderMark(bytes))
        {
            offset = 3;
        }
        else if (HasUtf16LittleEndianByteOrderMark(bytes))
        {
            offset = 2;
            encoding = 16;
        }
        else if (HasUtf16BigEndianByteOrderMark(bytes))
        {
            offset = 2;
            encoding = -16;
        }

        ReadOnlySpan<byte> content = bytes.Slice(offset);
        YamlByteValidationResult validation;
        if (encoding == 8)
        {
            validation = ValidateUtf8(content, offset);
        }
        else
        {
            validation = ValidateUtf16(content, offset, encoding == -16);
        }

        if (!validation.Valid)
        {
            result.Diagnostic = EncodingError(
                validation.InvalidCharacterIndex,
                validation.InvalidLine,
                validation.InvalidColumn,
                "Input contains an invalid " + EncodingName(encoding) + " byte sequence at byte offset " + validation.InvalidByteIndex.ToString() + ".");
            return result;
        }

        if (options != null && options.EnforceLimits && !options.AllowsInputLength(validation.CharacterCount))
        {
            result.Diagnostic = YamlDiagnostic.Error(
                "YAMLS001",
                "YAML input length exceeds the configured security limit.",
                YamlSourceSpan.FromBounds(0, 1, 1, validation.CharacterCount, 1, EndColumn(validation.CharacterCount)));
            return result;
        }

        if (encoding == 16)
        {
            result.Text = Encoding.Unicode.GetString(content);
        }
        else if (encoding == -16)
        {
            result.Text = Encoding.BigEndianUnicode.GetString(content);
        }
        else
        {
            result.Text = Encoding.UTF8.GetString(content);
        }

        return result;
    }

    internal static bool HasUtf8ByteOrderMark(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191;
    }

    internal static bool HasUtf16LittleEndianByteOrderMark(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 2 && bytes[0] == 255 && bytes[1] == 254;
    }

    internal static bool HasUtf16BigEndianByteOrderMark(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 2 && bytes[0] == 254 && bytes[1] == 255;
    }

    internal static bool HasUtf32ByteOrderMark(ReadOnlySpan<byte> bytes)
    {
        bool littleEndian = bytes.Length >= 4
            && bytes[0] == 255
            && bytes[1] == 254
            && bytes[2] == 0
            && bytes[3] == 0;
        bool bigEndian = bytes.Length >= 4
            && bytes[0] == 0
            && bytes[1] == 0
            && bytes[2] == 254
            && bytes[3] == 255;
        return littleEndian || bigEndian;
    }

    internal static YamlByteValidationResult ValidateUtf8(ReadOnlySpan<byte> bytes, int sourceOffset)
    {
        YamlByteValidationResult result = new YamlByteValidationResult();
        int index = 0;
        int line = 1;
        int column = 1;
        bool previousWasCarriageReturn = false;
        while (index < bytes.Length)
        {
            int width = Utf8SequenceWidth(bytes, index);
            if (width == 0)
            {
                SetInvalid(result, sourceOffset + index, result.NormalizedCharacterCount, line, column);
                return result;
            }

            int codePoint = Utf8CodePoint(bytes, index, width);
            int characterWidth = width == 4 ? 2 : 1;
            result.CharacterCount = result.CharacterCount + characterWidth;
            if (codePoint == 13)
            {
                result.NormalizedCharacterCount = result.NormalizedCharacterCount + 1;
                line = line + 1;
                column = 1;
                previousWasCarriageReturn = true;
            }
            else if (codePoint == 10)
            {
                if (!previousWasCarriageReturn)
                {
                    result.NormalizedCharacterCount = result.NormalizedCharacterCount + 1;
                    line = line + 1;
                    column = 1;
                }

                previousWasCarriageReturn = false;
            }
            else
            {
                result.NormalizedCharacterCount = result.NormalizedCharacterCount + characterWidth;
                column = column + characterWidth;
                previousWasCarriageReturn = false;
            }

            index = index + width;
        }

        return result;
    }

    internal static int Utf8SequenceWidth(ReadOnlySpan<byte> bytes, int index)
    {
        byte first = bytes[index];
        if (first <= 127)
        {
            return 1;
        }

        if (first >= 194 && first <= 223)
        {
            return HasContinuationBytes(bytes, index, 2) ? 2 : 0;
        }

        if (first >= 224 && first <= 239)
        {
            if (!HasContinuationBytes(bytes, index, 3))
            {
                return 0;
            }

            byte second = bytes[index + 1];
            if (first == 224 && second < 160)
            {
                return 0;
            }

            if (first == 237 && second >= 160)
            {
                return 0;
            }

            return 3;
        }

        if (first >= 240 && first <= 244)
        {
            if (!HasContinuationBytes(bytes, index, 4))
            {
                return 0;
            }

            byte second = bytes[index + 1];
            if (first == 240 && second < 144)
            {
                return 0;
            }

            if (first == 244 && second >= 144)
            {
                return 0;
            }

            return 4;
        }

        return 0;
    }

    internal static bool HasContinuationBytes(ReadOnlySpan<byte> bytes, int index, int width)
    {
        if (index > bytes.Length - width)
        {
            return false;
        }

        int current = 1;
        while (current < width)
        {
            byte value = bytes[index + current];
            if (value < 128 || value > 191)
            {
                return false;
            }

            current = current + 1;
        }

        return true;
    }

    internal static int Utf8CodePoint(ReadOnlySpan<byte> bytes, int index, int width)
    {
        if (width == 1)
        {
            return bytes[index];
        }

        if (width == 2)
        {
            return ((bytes[index] & 31) << 6) | (bytes[index + 1] & 63);
        }

        if (width == 3)
        {
            return ((bytes[index] & 15) << 12)
                | ((bytes[index + 1] & 63) << 6)
                | (bytes[index + 2] & 63);
        }

        return ((bytes[index] & 7) << 18)
            | ((bytes[index + 1] & 63) << 12)
            | ((bytes[index + 2] & 63) << 6)
            | (bytes[index + 3] & 63);
    }

    internal static YamlByteValidationResult ValidateUtf16(ReadOnlySpan<byte> bytes, int sourceOffset, bool bigEndian)
    {
        YamlByteValidationResult result = new YamlByteValidationResult();
        int index = 0;
        int line = 1;
        int column = 1;
        bool previousWasCarriageReturn = false;
        while (index + 1 < bytes.Length)
        {
            int value = Utf16CodeUnit(bytes, index, bigEndian);
            if (value >= 55296 && value <= 56319)
            {
                if (index + 3 >= bytes.Length)
                {
                    SetInvalid(result, sourceOffset + index, result.NormalizedCharacterCount, line, column);
                    return result;
                }

                int low = Utf16CodeUnit(bytes, index + 2, bigEndian);
                if (low < 56320 || low > 57343)
                {
                    SetInvalid(result, sourceOffset + index, result.NormalizedCharacterCount, line, column);
                    return result;
                }

                result.CharacterCount = result.CharacterCount + 2;
                result.NormalizedCharacterCount = result.NormalizedCharacterCount + 2;
                column = column + 2;
                previousWasCarriageReturn = false;
                index = index + 4;
                continue;
            }

            if (value >= 56320 && value <= 57343)
            {
                SetInvalid(result, sourceOffset + index, result.NormalizedCharacterCount, line, column);
                return result;
            }

            result.CharacterCount = result.CharacterCount + 1;
            if (value == 13)
            {
                result.NormalizedCharacterCount = result.NormalizedCharacterCount + 1;
                line = line + 1;
                column = 1;
                previousWasCarriageReturn = true;
            }
            else if (value == 10)
            {
                if (!previousWasCarriageReturn)
                {
                    result.NormalizedCharacterCount = result.NormalizedCharacterCount + 1;
                    line = line + 1;
                    column = 1;
                }

                previousWasCarriageReturn = false;
            }
            else
            {
                result.NormalizedCharacterCount = result.NormalizedCharacterCount + 1;
                column = column + 1;
                previousWasCarriageReturn = false;
            }

            index = index + 2;
        }

        if (index < bytes.Length)
        {
            SetInvalid(result, sourceOffset + index, result.NormalizedCharacterCount, line, column);
        }

        return result;
    }

    internal static int Utf16CodeUnit(ReadOnlySpan<byte> bytes, int index, bool bigEndian)
    {
        if (bigEndian)
        {
            return (bytes[index] << 8) | bytes[index + 1];
        }

        return bytes[index] | (bytes[index + 1] << 8);
    }

    internal static void SetInvalid(YamlByteValidationResult result, int byteIndex, int characterIndex, int line, int column)
    {
        result.Valid = false;
        result.InvalidByteIndex = byteIndex;
        result.InvalidCharacterIndex = characterIndex;
        result.InvalidLine = line;
        result.InvalidColumn = column;
    }

    internal static YamlDiagnostic EncodingError(int characterIndex, int line, int column, string message)
    {
        return YamlDiagnostic.Error(
            "YAMLE001",
            message,
            YamlSourceSpan.FromBounds(characterIndex, line, column, characterIndex, line, column));
    }

    internal static string EncodingName(int encoding)
    {
        if (encoding == 16)
        {
            return "UTF-16 little-endian";
        }

        if (encoding == -16)
        {
            return "UTF-16 big-endian";
        }

        return "UTF-8";
    }

    internal static int EndColumn(int characterCount)
    {
        return characterCount == int.MaxValue ? int.MaxValue : characterCount + 1;
    }
}
