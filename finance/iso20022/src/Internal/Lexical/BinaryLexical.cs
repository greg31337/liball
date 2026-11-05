using System;

namespace iso20022;

internal static class BinaryLexical
{
    internal static IsoBinaryValue? ParseBase64(ReadOnlySpan<char> text)
    {
        int nonWhitespaceCount = 0;
        int index = 0;
        while (index < text.Length)
        {
            if (!IsXmlWhitespace(text[index]))
            {
                nonWhitespaceCount++;
            }

            index++;
        }

        if (nonWhitespaceCount == 0)
        {
            IsoBinaryValue emptyResult = new IsoBinaryValue();
            emptyResult.Encoding = BinaryEncodingKind.Base64;
            emptyResult.LexicalValue = text.ToString();
            return emptyResult;
        }

        if (nonWhitespaceCount % 4 != 0)
        {
            return null;
        }

        int groupCount = nonWhitespaceCount / 4;
        int maximumLength = groupCount * 3;
        byte[] decoded = new byte[maximumLength];
        int[] quartet = new int[4];
        int quartetIndex = 0;
        int groupIndex = 0;
        int outputIndex = 0;
        index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (!IsXmlWhitespace(character))
            {
                quartet[quartetIndex] = DecodeBase64Character(character);
                quartetIndex++;
                if (quartetIndex == 4)
                {
                    bool finalGroup = groupIndex == groupCount - 1;
                    int first = quartet[0];
                    int second = quartet[1];
                    int third = quartet[2];
                    int fourth = quartet[3];
                    if (first < 0 || second < 0)
                    {
                        return null;
                    }

                    if (third == -2)
                    {
                        if (!finalGroup || fourth != -2 || (second & 15) != 0)
                        {
                            return null;
                        }

                        decoded[outputIndex] = (byte)((first << 2) | (second >> 4));
                        outputIndex++;
                    }
                    else if (fourth == -2)
                    {
                        if (!finalGroup || third < 0 || (third & 3) != 0)
                        {
                            return null;
                        }

                        decoded[outputIndex] = (byte)((first << 2) | (second >> 4));
                        decoded[outputIndex + 1] = (byte)(((second & 15) << 4) | (third >> 2));
                        outputIndex = outputIndex + 2;
                    }
                    else
                    {
                        if (third < 0 || fourth < 0)
                        {
                            return null;
                        }

                        decoded[outputIndex] = (byte)((first << 2) | (second >> 4));
                        decoded[outputIndex + 1] = (byte)(((second & 15) << 4) | (third >> 2));
                        decoded[outputIndex + 2] = (byte)(((third & 3) << 6) | fourth);
                        outputIndex = outputIndex + 3;
                    }

                    quartetIndex = 0;
                    groupIndex++;
                }
            }

            index++;
        }

        byte[] value = new byte[outputIndex];
        Array.Copy(decoded, value, outputIndex);
        IsoBinaryValue result = new IsoBinaryValue();
        result.Value = value;
        result.Encoding = BinaryEncodingKind.Base64;
        result.LexicalValue = text.ToString();
        return result;
    }

    internal static IsoBinaryValue? ParseHexadecimal(ReadOnlySpan<char> text)
    {
        if (text.Length % 2 != 0)
        {
            return null;
        }

        byte[] value = new byte[text.Length / 2];
        int inputIndex = 0;
        int outputIndex = 0;
        while (inputIndex < text.Length)
        {
            int high = DecodeHexadecimalCharacter(text[inputIndex]);
            int low = DecodeHexadecimalCharacter(text[inputIndex + 1]);
            if (high < 0 || low < 0)
            {
                return null;
            }

            value[outputIndex] = (byte)((high << 4) | low);
            inputIndex = inputIndex + 2;
            outputIndex++;
        }

        IsoBinaryValue result = new IsoBinaryValue();
        result.Value = value;
        result.Encoding = BinaryEncodingKind.Hexadecimal;
        result.LexicalValue = text.ToString();
        return result;
    }

    internal static string FormatBase64(ReadOnlySpan<byte> value)
    {
        return Convert.ToBase64String(value);
    }

    internal static string FormatHexadecimal(ReadOnlySpan<byte> value)
    {
        char[] characters = new char[value.Length * 2];
        int index = 0;
        while (index < value.Length)
        {
            int high = value[index] >> 4;
            int low = value[index] & 15;
            characters[index * 2] = EncodeHexadecimalDigit(high);
            characters[index * 2 + 1] = EncodeHexadecimalDigit(low);
            index++;
        }

        return new string(characters);
    }

    internal static bool IsXmlWhitespace(char character)
    {
        return character == ' ' || character == '\t' || character == '\r' || character == '\n';
    }

    internal static int DecodeBase64Character(char character)
    {
        if (character >= 'A' && character <= 'Z')
        {
            return character - 'A';
        }

        if (character >= 'a' && character <= 'z')
        {
            return character - 'a' + 26;
        }

        if (character >= '0' && character <= '9')
        {
            return character - '0' + 52;
        }

        if (character == '+')
        {
            return 62;
        }

        if (character == '/')
        {
            return 63;
        }

        if (character == '=')
        {
            return -2;
        }

        return -1;
    }

    internal static int DecodeHexadecimalCharacter(char character)
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

    internal static char EncodeHexadecimalDigit(int value)
    {
        if (value < 10)
        {
            return (char)('0' + value);
        }

        return (char)('A' + value - 10);
    }
}
