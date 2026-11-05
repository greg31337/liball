using System.Collections.Generic;
using System.Text;

namespace gltf;

internal class UriPercentCodec
{
    internal string DecodeText(string value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        List<byte> bytes = BuildDecodedBytes(value);
        ValidateUtf8Bytes(bytes);
        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    internal string EncodePathSegment(string value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character <= 0x7F)
            {
                if (IsUnreservedCharacter(character))
                {
                    result.Append(character);
                }
                else
                {
                    AppendEncodedByte(result, (byte)character);
                }

                index++;
            }
            else
            {
                int scalar = ReadUnicodeScalar(value, index);
                AppendEncodedScalar(result, scalar);
                if (scalar > 0xFFFF)
                {
                    index += 2;
                }
                else
                {
                    index++;
                }
            }
        }

        return result.ToString();
    }

    internal string NormalizeAuthority(string value)
    {
        return NormalizeComponent(value, 1);
    }

    internal string NormalizePath(string value)
    {
        return NormalizeComponent(value, 2);
    }

    internal string NormalizeQuery(string value)
    {
        return NormalizeComponent(value, 3);
    }

    internal string NormalizeFragment(string value)
    {
        return NormalizeComponent(value, 3);
    }

    internal void ValidateEscapes(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '%')
            {
                if (index + 2 >= value.Length
                    || GetHexValue(value[index + 1]) < 0
                    || GetHexValue(value[index + 2]) < 0)
                {
                    throw new GltfResourceException(
                        $"Malformed percent escape at character {index}.");
                }

                index += 3;
            }
            else
            {
                index++;
            }
        }
    }

    internal List<byte> BuildDecodedBytes(string value)
    {
        ValidateEscapes(value);
        List<byte> result = new List<byte>();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '%')
            {
                int high = GetHexValue(value[index + 1]);
                int low = GetHexValue(value[index + 2]);
                result.Add((byte)((high * 16) + low));
                index += 3;
            }
            else if (character <= 0x7F)
            {
                result.Add((byte)character);
                index++;
            }
            else
            {
                int scalar = ReadUnicodeScalar(value, index);
                AppendUtf8Bytes(result, scalar);
                if (scalar > 0xFFFF)
                {
                    index += 2;
                }
                else
                {
                    index++;
                }
            }
        }

        return result;
    }

    internal void ValidateUtf8Bytes(List<byte> bytes)
    {
        int index = 0;
        while (index < bytes.Count)
        {
            byte first = bytes[index];
            int sequenceLength = 0;
            if (first <= 0x7F)
            {
                sequenceLength = 1;
            }
            else if (first >= 0xC2 && first <= 0xDF)
            {
                sequenceLength = 2;
            }
            else if (first >= 0xE0 && first <= 0xEF)
            {
                sequenceLength = 3;
            }
            else if (first >= 0xF0 && first <= 0xF4)
            {
                sequenceLength = 4;
            }
            else
            {
                throw new GltfResourceException(
                    $"Percent-decoded URI text is not valid UTF-8 at byte {index}.");
            }

            if (index + sequenceLength > bytes.Count)
            {
                throw new GltfResourceException(
                    $"Percent-decoded URI text is not valid UTF-8 at byte {index}.");
            }

            int continuationIndex = 1;
            while (continuationIndex < sequenceLength)
            {
                if (!IsContinuationByte(bytes[index + continuationIndex]))
                {
                    throw new GltfResourceException(
                        $"Percent-decoded URI text is not valid UTF-8 at byte {index + continuationIndex}.");
                }

                continuationIndex++;
            }

            if (sequenceLength == 3
                && first == 0xE0
                && bytes[index + 1] < 0xA0)
            {
                throw new GltfResourceException(
                    $"Percent-decoded URI text is not valid UTF-8 at byte {index}.");
            }

            if (sequenceLength == 3
                && first == 0xED
                && bytes[index + 1] > 0x9F)
            {
                throw new GltfResourceException(
                    $"Percent-decoded URI text is not valid UTF-8 at byte {index}.");
            }

            if (sequenceLength == 4
                && first == 0xF0
                && bytes[index + 1] < 0x90)
            {
                throw new GltfResourceException(
                    $"Percent-decoded URI text is not valid UTF-8 at byte {index}.");
            }

            if (sequenceLength == 4
                && first == 0xF4
                && bytes[index + 1] > 0x8F)
            {
                throw new GltfResourceException(
                    $"Percent-decoded URI text is not valid UTF-8 at byte {index}.");
            }

            index += sequenceLength;
        }
    }

    internal bool IsContinuationByte(byte value)
    {
        return value >= 0x80 && value <= 0xBF;
    }

    internal string NormalizeComponent(string value, int componentKind)
    {
        List<byte> decodedBytes = BuildDecodedBytes(value);
        ValidateUtf8Bytes(decodedBytes);
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '%')
            {
                int byteValue = (GetHexValue(value[index + 1]) * 16)
                    + GetHexValue(value[index + 2]);
                char decodedCharacter = (char)byteValue;
                if (byteValue <= 0x7F
                    && decodedCharacter != '.'
                    && IsUnreservedCharacter(decodedCharacter))
                {
                    result.Append(decodedCharacter);
                }
                else
                {
                    AppendEncodedByte(result, (byte)byteValue);
                }

                index += 3;
            }
            else if (character <= 0x7F)
            {
                if (IsAllowedComponentCharacter(character, componentKind))
                {
                    result.Append(character);
                }
                else
                {
                    AppendEncodedByte(result, (byte)character);
                }

                index++;
            }
            else
            {
                int scalar = ReadUnicodeScalar(value, index);
                AppendEncodedScalar(result, scalar);
                if (scalar > 0xFFFF)
                {
                    index += 2;
                }
                else
                {
                    index++;
                }
            }
        }

        return result.ToString();
    }

    internal bool IsAllowedComponentCharacter(char character, int componentKind)
    {
        if (IsUnreservedCharacter(character) || IsSubDelimiter(character))
        {
            return true;
        }

        if (componentKind == 1)
        {
            return character == ':'
                || character == '@'
                || character == '['
                || character == ']';
        }

        if (componentKind == 2)
        {
            return character == ':' || character == '@' || character == '/';
        }

        return character == ':'
            || character == '@'
            || character == '/'
            || character == '?';
    }

    internal bool IsUnreservedCharacter(char character)
    {
        return (character >= 'A' && character <= 'Z')
            || (character >= 'a' && character <= 'z')
            || (character >= '0' && character <= '9')
            || character == '-'
            || character == '.'
            || character == '_'
            || character == '~';
    }

    internal bool IsSubDelimiter(char character)
    {
        return character == '!'
            || character == '$'
            || character == '&'
            || character == '\''
            || character == '('
            || character == ')'
            || character == '*'
            || character == '+'
            || character == ','
            || character == ';'
            || character == '=';
    }

    internal int GetHexValue(char character)
    {
        if (character >= '0' && character <= '9')
        {
            return character - '0';
        }

        if (character >= 'A' && character <= 'F')
        {
            return character - 'A' + 10;
        }

        if (character >= 'a' && character <= 'f')
        {
            return character - 'a' + 10;
        }

        return -1;
    }

    internal char GetUpperHexCharacter(int value)
    {
        if (value < 10)
        {
            return (char)('0' + value);
        }

        return (char)('A' + value - 10);
    }

    internal int ReadUnicodeScalar(string value, int index)
    {
        char first = value[index];
        if (first >= 0xD800 && first <= 0xDBFF)
        {
            if (index + 1 >= value.Length)
            {
                throw new GltfResourceException(
                    $"URI text contains an unpaired UTF-16 surrogate at character {index}.");
            }

            char second = value[index + 1];
            if (second < 0xDC00 || second > 0xDFFF)
            {
                throw new GltfResourceException(
                    $"URI text contains an unpaired UTF-16 surrogate at character {index}.");
            }

            return 0x10000 + ((first - 0xD800) * 0x400) + (second - 0xDC00);
        }

        if (first >= 0xDC00 && first <= 0xDFFF)
        {
            throw new GltfResourceException(
                $"URI text contains an unpaired UTF-16 surrogate at character {index}.");
        }

        return first;
    }

    internal void AppendUtf8Bytes(List<byte> bytes, int scalar)
    {
        if (scalar <= 0x7F)
        {
            bytes.Add((byte)scalar);
        }
        else if (scalar <= 0x7FF)
        {
            bytes.Add((byte)(0xC0 | (scalar >> 6)));
            bytes.Add((byte)(0x80 | (scalar & 0x3F)));
        }
        else if (scalar <= 0xFFFF)
        {
            bytes.Add((byte)(0xE0 | (scalar >> 12)));
            bytes.Add((byte)(0x80 | ((scalar >> 6) & 0x3F)));
            bytes.Add((byte)(0x80 | (scalar & 0x3F)));
        }
        else
        {
            bytes.Add((byte)(0xF0 | (scalar >> 18)));
            bytes.Add((byte)(0x80 | ((scalar >> 12) & 0x3F)));
            bytes.Add((byte)(0x80 | ((scalar >> 6) & 0x3F)));
            bytes.Add((byte)(0x80 | (scalar & 0x3F)));
        }
    }

    internal void AppendEncodedScalar(StringBuilder result, int scalar)
    {
        List<byte> bytes = new List<byte>();
        AppendUtf8Bytes(bytes, scalar);
        int index = 0;
        while (index < bytes.Count)
        {
            AppendEncodedByte(result, bytes[index]);
            index++;
        }
    }

    internal void AppendEncodedByte(StringBuilder result, byte value)
    {
        result.Append('%');
        result.Append(GetUpperHexCharacter(value >> 4));
        result.Append(GetUpperHexCharacter(value & 0x0F));
    }
}
