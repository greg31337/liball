using System;

namespace xml;

internal class UnicodeScalar
{
    internal int Value;
    internal int Utf16Offset;
    internal int Utf16CodeUnitCount;
    internal bool EndOfInput;

    internal UnicodeScalar(int value, int utf16Offset, int utf16CodeUnitCount, bool endOfInput)
    {
        Value = value;
        Utf16Offset = utf16Offset;
        Utf16CodeUnitCount = utf16CodeUnitCount;
        EndOfInput = endOfInput;
    }
}

internal class UnicodeScalarScanner
{
    internal ReadOnlyMemory<char> Characters;
    internal int Offset;

    internal UnicodeScalarScanner(ReadOnlyMemory<char> characters)
    {
        Characters = characters;
        Offset = 0;
    }

    internal UnicodeScalar ReadUnicodeScalar()
    {
        ReadOnlySpan<char> span = Characters.Span;
        int start = Offset;
        if (start >= span.Length)
        {
            return new UnicodeScalar(0, start, 0, true);
        }

        int first = span[start];
        if (first >= 0xD800 && first <= 0xDBFF)
        {
            if (start + 1 >= span.Length)
            {
                throw CreateSurrogateError("Unpaired high surrogate", start);
            }

            int second = span[start + 1];
            if (second < 0xDC00 || second > 0xDFFF)
            {
                throw CreateSurrogateError("High surrogate is not followed by a low surrogate", start);
            }

            int scalar = 0x10000 + (first - 0xD800) * 0x400 + second - 0xDC00;
            Offset = start + 2;
            return new UnicodeScalar(scalar, start, 2, false);
        }

        if (first >= 0xDC00 && first <= 0xDFFF)
        {
            throw CreateSurrogateError("Unpaired low surrogate", start);
        }

        Offset = start + 1;
        return new UnicodeScalar(first, start, 1, false);
    }

    internal int CurrentUtf16Offset()
    {
        return Offset;
    }

    internal bool IsUnicodeScalarEnd()
    {
        return Offset >= Characters.Length;
    }

    internal void ResetUnicodeScalarScanner()
    {
        Offset = 0;
    }

    internal FormatException CreateSurrogateError(string message, int offset)
    {
        return new FormatException(message + " at UTF-16 offset " + offset + ".");
    }
}

internal static class Xml10CharacterRules
{
    internal static bool IsLegalXml10Character(int scalar)
    {
        if (scalar == 0x9 || scalar == 0xA || scalar == 0xD)
        {
            return true;
        }

        if (scalar >= 0x20 && scalar <= 0xD7FF)
        {
            return true;
        }

        if (scalar >= 0xE000 && scalar <= 0xFFFD)
        {
            return true;
        }

        return scalar >= 0x10000 && scalar <= 0x10FFFF;
    }

    internal static bool IsXml10NameStartCharacter(int scalar)
    {
        if (scalar == ':' || scalar == '_' || scalar >= 'A' && scalar <= 'Z' || scalar >= 'a' && scalar <= 'z')
        {
            return true;
        }

        if (scalar >= 0xC0 && scalar <= 0xD6 || scalar >= 0xD8 && scalar <= 0xF6 || scalar >= 0xF8 && scalar <= 0x2FF)
        {
            return true;
        }

        if (scalar >= 0x370 && scalar <= 0x37D || scalar >= 0x37F && scalar <= 0x1FFF)
        {
            return true;
        }

        if (scalar >= 0x200C && scalar <= 0x200D || scalar >= 0x2070 && scalar <= 0x218F)
        {
            return true;
        }

        if (scalar >= 0x2C00 && scalar <= 0x2FEF || scalar >= 0x3001 && scalar <= 0xD7FF)
        {
            return true;
        }

        if (scalar >= 0xF900 && scalar <= 0xFDCF || scalar >= 0xFDF0 && scalar <= 0xFFFD)
        {
            return true;
        }

        return scalar >= 0x10000 && scalar <= 0xEFFFF;
    }

    internal static bool IsXml10NameCharacter(int scalar)
    {
        if (IsXml10NameStartCharacter(scalar))
        {
            return true;
        }

        if (scalar == '-' || scalar == '.' || scalar >= '0' && scalar <= '9' || scalar == 0xB7)
        {
            return true;
        }

        return scalar >= 0x300 && scalar <= 0x36F || scalar >= 0x203F && scalar <= 0x2040;
    }

    internal static void ValidateXml10Characters(ReadOnlyMemory<char> characters)
    {
        UnicodeScalarScanner scanner = new UnicodeScalarScanner(characters);
        while (!scanner.IsUnicodeScalarEnd())
        {
            UnicodeScalar scalar = scanner.ReadUnicodeScalar();
            if (!IsLegalXml10Character(scalar.Value))
            {
                throw new FormatException("Illegal XML 1.0 character U+" + scalar.Value.ToString("X") + " at UTF-16 offset " + scalar.Utf16Offset + ".");
            }
        }
    }
}

internal class XmlCharacterReference
{
    internal int ScalarValue;
    internal int SourceCharacterCount;
    internal string ReplacementText;
    internal bool RestrictedCharacter;

    internal XmlCharacterReference(int scalarValue, int sourceCharacterCount, string replacementText, bool restrictedCharacter)
    {
        ScalarValue = scalarValue;
        SourceCharacterCount = sourceCharacterCount;
        ReplacementText = replacementText;
        RestrictedCharacter = restrictedCharacter;
    }
}

internal static class Xml10CharacterReferenceParser
{
    internal static XmlCharacterReference ParseXml10CharacterReference(ReadOnlySpan<char> characters, int startOffset)
    {
        Guard.NonNegativeInt(startOffset, "startOffset");
        if (startOffset >= characters.Length || characters[startOffset] != '&' || startOffset + 1 >= characters.Length || characters[startOffset + 1] != '#')
        {
            throw CreateCharacterReferenceError("A character reference must begin with '&#'", startOffset);
        }

        int position = startOffset + 2;
        int numberBase = 10;
        if (position < characters.Length && characters[position] == 'x')
        {
            numberBase = 16;
            position = position + 1;
        }

        int digitCount = 0;
        int scalar = 0;
        while (position < characters.Length && characters[position] != ';')
        {
            int digit = CharacterReferenceDigit(characters[position], numberBase);
            if (digit < 0)
            {
                throw CreateCharacterReferenceError("A character reference contains an invalid digit", position);
            }

            if (scalar > (0x10FFFF - digit) / numberBase)
            {
                throw CreateCharacterReferenceError("A character reference exceeds the Unicode scalar range", position);
            }

            scalar = scalar * numberBase + digit;
            digitCount = digitCount + 1;
            position = position + 1;
        }

        if (digitCount == 0)
        {
            throw CreateCharacterReferenceError("A character reference requires at least one digit", position);
        }

        if (position >= characters.Length || characters[position] != ';')
        {
            throw CreateCharacterReferenceError("A character reference must end with ';'", position);
        }

        if (!Xml10CharacterRules.IsLegalXml10Character(scalar))
        {
            throw CreateCharacterReferenceError("A character reference does not identify a legal XML 1.0 character", startOffset);
        }

        string replacement = char.ConvertFromUtf32(scalar);
        return new XmlCharacterReference(scalar, position - startOffset + 1, replacement, false);
    }

    internal static int CharacterReferenceDigit(char value, int numberBase)
    {
        if (value >= '0' && value <= '9')
        {
            int digit = value - '0';
            return digit < numberBase ? digit : -1;
        }

        if (numberBase == 16 && value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        if (numberBase == 16 && value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }

        return -1;
    }

    internal static FormatException CreateCharacterReferenceError(string message, int offset)
    {
        return new FormatException(message + " at UTF-16 offset " + offset + ".");
    }
}
