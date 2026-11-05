namespace barcode;

internal class Code39EncodedData
{
    internal string Text = "";
    internal string EncodedText = "";
    internal int[] Values = new int[0];
    internal int[] PatternBits = new int[0];
    internal Code39CharacterMode CharacterMode;
}

internal class Code39DecodedData
{
    internal string Text = "";
    internal string EncodedText = "";
    internal int[] Values = new int[0];
    internal int[] PatternBits = new int[0];
    internal Code39CharacterMode CharacterMode;
}

internal static class Code39Encoder
{
    internal static Code39EncodedData Encode(string text, Code39CharacterMode characterMode)
    {
        ValidateCharacterMode(characterMode);
        string encodedText;
        if (characterMode == Code39CharacterMode.Standard)
        {
            Code39Patterns.ValidateStandardText(text);
            encodedText = text;
        }
        else
        {
            encodedText = EncodeFullAscii(text);
        }

        int[] values = ConvertTextToValues(encodedText);
        int[] patterns = CreateFramedPatterns(values);
        Code39EncodedData encoded = new Code39EncodedData();
        encoded.Text = text;
        encoded.EncodedText = encodedText;
        encoded.Values = values;
        encoded.PatternBits = patterns;
        encoded.CharacterMode = characterMode;
        return encoded;
    }

    static string EncodeFullAscii(string text)
    {
        if (text == null)
        {
            throw new BarcodeEncodingException("Full ASCII Code 39 text must not be null.");
        }

        if (text.Length == 0)
        {
            throw new BarcodeEncodingException("Full ASCII Code 39 text must not be empty.");
        }

        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            int value = text[index];
            if (value > 127)
            {
                throw new BarcodeEncodingException("Full ASCII Code 39 supports only seven-bit ASCII characters.");
            }

            AppendFullAsciiCharacter(builder, value);
            index = index + 1;
        }

        return builder.ToString();
    }

    static void AppendFullAsciiCharacter(global::System.Text.StringBuilder builder, int value)
    {
        if (value == 32 || value == 45 || value == 46 ||
            (value >= 48 && value <= 57) ||
            (value >= 65 && value <= 90))
        {
            builder.Append((char)value);
            return;
        }

        if (value == 0)
        {
            builder.Append("%U");
        }
        else if (value >= 1 && value <= 26)
        {
            builder.Append('$');
            builder.Append((char)('A' + value - 1));
        }
        else if (value >= 27 && value <= 31)
        {
            builder.Append('%');
            builder.Append((char)('A' + value - 27));
        }
        else if (value >= 33 && value <= 47)
        {
            builder.Append('/');
            builder.Append((char)('A' + value - 33));
        }
        else if (value == 58)
        {
            builder.Append("/Z");
        }
        else if (value >= 59 && value <= 63)
        {
            builder.Append('%');
            builder.Append((char)('F' + value - 59));
        }
        else if (value == 64)
        {
            builder.Append("%V");
        }
        else if (value >= 91 && value <= 95)
        {
            builder.Append('%');
            builder.Append((char)('K' + value - 91));
        }
        else if (value == 96)
        {
            builder.Append("%W");
        }
        else if (value >= 97 && value <= 122)
        {
            builder.Append('+');
            builder.Append((char)('A' + value - 97));
        }
        else
        {
            builder.Append('%');
            builder.Append((char)('P' + value - 123));
        }
    }

    static int[] ConvertTextToValues(string encodedText)
    {
        int[] values = new int[encodedText.Length];
        int index = 0;
        while (index < encodedText.Length)
        {
            values[index] = Code39Patterns.GetValue(encodedText[index]);
            index = index + 1;
        }

        return values;
    }

    static int[] CreateFramedPatterns(int[] values)
    {
        int[] patterns = new int[values.Length + 2];
        patterns[0] = Code39Patterns.StartStopPatternBits;
        int index = 0;
        while (index < values.Length)
        {
            patterns[index + 1] = Code39Patterns.GetDataPatternBits(values[index]);
            index = index + 1;
        }

        patterns[patterns.Length - 1] = Code39Patterns.StartStopPatternBits;
        return patterns;
    }

    static void ValidateCharacterMode(Code39CharacterMode characterMode)
    {
        if (characterMode != Code39CharacterMode.Standard &&
            characterMode != Code39CharacterMode.FullAscii)
        {
            throw new BarcodeEncodingException("Code 39 character mode is invalid.");
        }
    }
}

internal static class Code39Decoder
{
    internal static Code39DecodedData DecodePatterns(
        int[] patternBits,
        Code39CharacterMode characterMode)
    {
        Code39DecodedData? decoded = TryDecodePatterns(patternBits, characterMode);
        if (decoded == null)
        {
            throw new BarcodeDecodingException("Code 39 patterns do not contain valid framed data.");
        }

        return decoded;
    }

    internal static Code39DecodedData? TryDecodePatterns(
        int[] patternBits,
        Code39CharacterMode characterMode)
    {
        if (patternBits == null)
        {
            throw new BarcodeValidationException("Code 39 patterns must not be null.");
        }

        ValidateCharacterMode(characterMode);
        if (patternBits.Length < 3 ||
            patternBits[0] != Code39Patterns.StartStopPatternBits ||
            patternBits[patternBits.Length - 1] != Code39Patterns.StartStopPatternBits)
        {
            return null;
        }

        int dataLength = patternBits.Length - 2;
        char[] characters = new char[dataLength];
        int index = 0;
        while (index < dataLength)
        {
            int value = FindDataValueForPattern(patternBits[index + 1]);
            if (value < 0)
            {
                return null;
            }

            characters[index] = Code39Patterns.GetCharacter(value);
            index = index + 1;
        }

        return TryCreateDecodedData(
            new string(characters),
            CopyPatterns(patternBits),
            characterMode);
    }

    internal static Code39DecodedData DecodeDataCharacters(
        string encodedText,
        Code39CharacterMode characterMode)
    {
        Code39DecodedData? decoded = TryDecodeDataCharacters(encodedText, characterMode);
        if (decoded == null)
        {
            throw new BarcodeDecodingException("Code 39 data characters are malformed.");
        }

        return decoded;
    }

    internal static Code39DecodedData? TryDecodeDataCharacters(
        string encodedText,
        Code39CharacterMode characterMode)
    {
        if (encodedText == null)
        {
            throw new BarcodeValidationException("Code 39 data characters must not be null.");
        }

        ValidateCharacterMode(characterMode);
        if (encodedText.Length == 0)
        {
            return null;
        }

        int[] patterns = new int[encodedText.Length + 2];
        patterns[0] = Code39Patterns.StartStopPatternBits;
        int index = 0;
        while (index < encodedText.Length)
        {
            if (!Code39Patterns.IsStandardDataCharacter(encodedText[index]))
            {
                return null;
            }

            patterns[index + 1] = Code39Patterns.GetCharacterPatternBits(encodedText[index]);
            index = index + 1;
        }

        patterns[patterns.Length - 1] = Code39Patterns.StartStopPatternBits;
        return TryCreateDecodedData(encodedText, patterns, characterMode);
    }

    static Code39DecodedData? TryCreateDecodedData(
        string encodedText,
        int[] patterns,
        Code39CharacterMode characterMode)
    {
        string? text;
        if (characterMode == Code39CharacterMode.Standard)
        {
            text = encodedText;
        }
        else
        {
            text = TryDecodeFullAscii(encodedText);
            if (text == null)
            {
                return null;
            }
        }

        int[] values = new int[encodedText.Length];
        int index = 0;
        while (index < encodedText.Length)
        {
            values[index] = Code39Patterns.GetValue(encodedText[index]);
            index = index + 1;
        }

        Code39DecodedData decoded = new Code39DecodedData();
        decoded.Text = text;
        decoded.EncodedText = encodedText;
        decoded.Values = values;
        decoded.PatternBits = patterns;
        decoded.CharacterMode = characterMode;
        return decoded;
    }

    static string? TryDecodeFullAscii(string encodedText)
    {
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        int index = 0;
        while (index < encodedText.Length)
        {
            char character = encodedText[index];
            if (character != '$' && character != '%' &&
                character != '/' && character != '+')
            {
                builder.Append(character);
                index = index + 1;
                continue;
            }

            if (index + 1 >= encodedText.Length)
            {
                return null;
            }

            int decodedValue = DecodeShiftPair(character, encodedText[index + 1]);
            if (decodedValue < 0)
            {
                return null;
            }

            builder.Append((char)decodedValue);
            index = index + 2;
        }

        return builder.ToString();
    }

    static int DecodeShiftPair(char shift, char following)
    {
        if (shift == '+')
        {
            return following >= 'A' && following <= 'Z'
                ? following - 'A' + 97
                : -1;
        }

        if (shift == '$')
        {
            return following >= 'A' && following <= 'Z'
                ? following - 'A' + 1
                : -1;
        }

        if (shift == '/')
        {
            if (following >= 'A' && following <= 'O')
            {
                return following - 'A' + 33;
            }

            return following == 'Z' ? 58 : -1;
        }

        if (following >= 'A' && following <= 'E')
        {
            return following - 'A' + 27;
        }

        if (following >= 'F' && following <= 'J')
        {
            return following - 'F' + 59;
        }

        if (following >= 'K' && following <= 'O')
        {
            return following - 'K' + 91;
        }

        if (following >= 'P' && following <= 'T')
        {
            return following - 'P' + 123;
        }

        if (following == 'U')
        {
            return 0;
        }

        if (following == 'V')
        {
            return 64;
        }

        return following == 'W' ? 96 : -1;
    }

    static int FindDataValueForPattern(int patternBits)
    {
        int value = 0;
        while (value < Code39Patterns.DataCharacterCount)
        {
            if (Code39Patterns.GetDataPatternBits(value) == patternBits)
            {
                return value;
            }

            value = value + 1;
        }

        return -1;
    }

    static int[] CopyPatterns(int[] patternBits)
    {
        int[] copy = new int[patternBits.Length];
        int index = 0;
        while (index < patternBits.Length)
        {
            copy[index] = patternBits[index];
            index = index + 1;
        }

        return copy;
    }

    static void ValidateCharacterMode(Code39CharacterMode characterMode)
    {
        if (characterMode != Code39CharacterMode.Standard &&
            characterMode != Code39CharacterMode.FullAscii)
        {
            throw new BarcodeValidationException("Code 39 character mode is invalid.");
        }
    }
}
