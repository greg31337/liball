namespace barcode;

internal enum Pdf417TextSubmode
{
    Alpha = 0,
    Lower = 1,
    Mixed = 2,
    Punctuation = 3
}

internal class Pdf417TextEncodingResult
{
    internal int[] Values = new int[0];
    internal int[] Codewords = new int[0];
    internal Pdf417TextSubmode InitialSubmode;
    internal Pdf417TextSubmode FinalSubmode;
}

internal class Pdf417TextDecodingResult
{
    internal string Text = "";
    internal int[] Values = new int[0];
    internal Pdf417TextSubmode InitialSubmode;
    internal Pdf417TextSubmode FinalSubmode;
}

internal static class Pdf417TextCompaction
{
    internal const int TextLatchCodeword = 900;
    internal const int ValueBase = 30;
    internal const int PaddingValue = 29;
    internal const string MixedCharacters = "0123456789&\r\t,:#-.$/+%*=^";
    internal const string PunctuationCharacters = ";<>@[\\]_`~!\r\t,:\n-.$/\"|*()?{}'";

    internal static Pdf417TextEncodingResult EncodeText(
        string text,
        Pdf417TextSubmode initialSubmode)
    {
        if (text == null)
        {
            throw new BarcodeEncodingException("PDF417 text compaction input must not be null.");
        }

        ValidateSubmode(initialSubmode);
        ValidateText(text);
        int maximumValueCount = CheckedMath.Add(
            CheckedMath.Multiply(text.Length, 2),
            1);
        int[] buffer = new int[maximumValueCount];
        int valueCount = 0;
        int index = 0;
        Pdf417TextSubmode submode = initialSubmode;
        while (index < text.Length)
        {
            char character = text[index];
            switch (submode)
            {
                case Pdf417TextSubmode.Alpha:
                    if (IsAlpha(character))
                    {
                        buffer[valueCount] = character == ' ' ? 26 : character - 'A';
                        valueCount = valueCount + 1;
                        index = index + 1;
                    }
                    else if (IsLower(character))
                    {
                        buffer[valueCount] = 27;
                        valueCount = valueCount + 1;
                        submode = Pdf417TextSubmode.Lower;
                    }
                    else if (FindMixedValue(character) >= 0)
                    {
                        buffer[valueCount] = 28;
                        valueCount = valueCount + 1;
                        submode = Pdf417TextSubmode.Mixed;
                    }
                    else
                    {
                        buffer[valueCount] = 29;
                        buffer[valueCount + 1] = FindPunctuationValue(character);
                        valueCount = valueCount + 2;
                        index = index + 1;
                    }

                    break;
                case Pdf417TextSubmode.Lower:
                    if (IsLower(character))
                    {
                        buffer[valueCount] = character == ' ' ? 26 : character - 'a';
                        valueCount = valueCount + 1;
                        index = index + 1;
                    }
                    else if (IsAlpha(character))
                    {
                        buffer[valueCount] = 27;
                        buffer[valueCount + 1] = character == ' ' ? 26 : character - 'A';
                        valueCount = valueCount + 2;
                        index = index + 1;
                    }
                    else if (FindMixedValue(character) >= 0)
                    {
                        buffer[valueCount] = 28;
                        valueCount = valueCount + 1;
                        submode = Pdf417TextSubmode.Mixed;
                    }
                    else
                    {
                        buffer[valueCount] = 29;
                        buffer[valueCount + 1] = FindPunctuationValue(character);
                        valueCount = valueCount + 2;
                        index = index + 1;
                    }

                    break;
                case Pdf417TextSubmode.Mixed:
                    int mixedValue = FindMixedValue(character);
                    if (mixedValue >= 0 || character == ' ')
                    {
                        buffer[valueCount] = character == ' ' ? 26 : mixedValue;
                        valueCount = valueCount + 1;
                        index = index + 1;
                    }
                    else if (IsAlpha(character))
                    {
                        buffer[valueCount] = 28;
                        valueCount = valueCount + 1;
                        submode = Pdf417TextSubmode.Alpha;
                    }
                    else if (IsLower(character))
                    {
                        buffer[valueCount] = 27;
                        valueCount = valueCount + 1;
                        submode = Pdf417TextSubmode.Lower;
                    }
                    else if (index + 1 < text.Length
                        && FindPunctuationValue(text[index + 1]) >= 0)
                    {
                        buffer[valueCount] = 25;
                        valueCount = valueCount + 1;
                        submode = Pdf417TextSubmode.Punctuation;
                    }
                    else
                    {
                        buffer[valueCount] = 29;
                        buffer[valueCount + 1] = FindPunctuationValue(character);
                        valueCount = valueCount + 2;
                        index = index + 1;
                    }

                    break;
                default:
                    int punctuationValue = FindPunctuationValue(character);
                    if (punctuationValue >= 0)
                    {
                        buffer[valueCount] = punctuationValue;
                        valueCount = valueCount + 1;
                        index = index + 1;
                    }
                    else
                    {
                        buffer[valueCount] = 29;
                        valueCount = valueCount + 1;
                        submode = Pdf417TextSubmode.Alpha;
                    }

                    break;
            }
        }

        int[] values = CopyPrefix(buffer, valueCount);
        Pdf417TextEncodingResult result = new Pdf417TextEncodingResult();
        result.Values = values;
        result.Codewords = PackValues(values);
        result.InitialSubmode = initialSubmode;
        result.FinalSubmode = submode;
        return result;
    }

    internal static Pdf417TextDecodingResult DecodeCodewords(
        int[] codewords,
        Pdf417TextSubmode initialSubmode)
    {
        if (codewords == null)
        {
            throw new BarcodeDecodingException("PDF417 text codewords must not be null.");
        }

        ValidateSubmodeForDecoding(initialSubmode);
        int[] values = new int[CheckedMath.Multiply(codewords.Length, 2)];
        int index = 0;
        while (index < codewords.Length)
        {
            int codeword = codewords[index];
            if (codeword < 0 || codeword >= TextLatchCodeword)
            {
                throw new BarcodeDecodingException("PDF417 text-compaction codewords must be between 0 and 899.");
            }

            values[index * 2] = codeword / ValueBase;
            values[(index * 2) + 1] = codeword % ValueBase;
            index = index + 1;
        }

        return DecodeValues(values, initialSubmode);
    }

    internal static Pdf417TextDecodingResult DecodeValues(
        int[] values,
        Pdf417TextSubmode initialSubmode)
    {
        if (values == null)
        {
            throw new BarcodeDecodingException("PDF417 text submode values must not be null.");
        }

        ValidateSubmodeForDecoding(initialSubmode);
        char[] characters = new char[values.Length];
        int characterCount = 0;
        Pdf417TextSubmode submode = initialSubmode;
        Pdf417TextSubmode priorToShift = initialSubmode;
        bool alphaShift = false;
        bool punctuationShift = false;
        int index = 0;
        while (index < values.Length)
        {
            int value = values[index];
            if (value < 0 || value >= ValueBase)
            {
                throw new BarcodeDecodingException("PDF417 text submode values must be between 0 and 29.");
            }

            char character = (char)0;
            if (alphaShift)
            {
                submode = priorToShift;
                alphaShift = false;
                if (value < 26)
                {
                    character = (char)('A' + value);
                }
                else if (value == 26)
                {
                    character = ' ';
                }
            }
            else if (punctuationShift)
            {
                submode = priorToShift;
                punctuationShift = false;
                if (value < 29)
                {
                    character = PunctuationCharacters[value];
                }
            }
            else
            {
                switch (submode)
                {
                    case Pdf417TextSubmode.Alpha:
                        if (value < 26)
                        {
                            character = (char)('A' + value);
                        }
                        else if (value == 26)
                        {
                            character = ' ';
                        }
                        else if (value == 27)
                        {
                            submode = Pdf417TextSubmode.Lower;
                        }
                        else if (value == 28)
                        {
                            submode = Pdf417TextSubmode.Mixed;
                        }
                        else
                        {
                            priorToShift = submode;
                            punctuationShift = true;
                        }

                        break;
                    case Pdf417TextSubmode.Lower:
                        if (value < 26)
                        {
                            character = (char)('a' + value);
                        }
                        else if (value == 26)
                        {
                            character = ' ';
                        }
                        else if (value == 27)
                        {
                            priorToShift = submode;
                            alphaShift = true;
                        }
                        else if (value == 28)
                        {
                            submode = Pdf417TextSubmode.Mixed;
                        }
                        else
                        {
                            priorToShift = submode;
                            punctuationShift = true;
                        }

                        break;
                    case Pdf417TextSubmode.Mixed:
                        if (value < 25)
                        {
                            character = MixedCharacters[value];
                        }
                        else if (value == 25)
                        {
                            submode = Pdf417TextSubmode.Punctuation;
                        }
                        else if (value == 26)
                        {
                            character = ' ';
                        }
                        else if (value == 27)
                        {
                            submode = Pdf417TextSubmode.Lower;
                        }
                        else if (value == 28)
                        {
                            submode = Pdf417TextSubmode.Alpha;
                        }
                        else
                        {
                            priorToShift = submode;
                            punctuationShift = true;
                        }

                        break;
                    default:
                        if (value < 29)
                        {
                            character = PunctuationCharacters[value];
                        }
                        else
                        {
                            submode = Pdf417TextSubmode.Alpha;
                        }

                        break;
                }
            }

            if (character != (char)0)
            {
                characters[characterCount] = character;
                characterCount = characterCount + 1;
            }

            index = index + 1;
        }

        Pdf417TextDecodingResult result = new Pdf417TextDecodingResult();
        result.Text = new string(characters, 0, characterCount);
        result.Values = CopyPrefix(values, values.Length);
        result.InitialSubmode = initialSubmode;
        result.FinalSubmode = submode;
        return result;
    }

    static int[] PackValues(int[] values)
    {
        int[] codewords = new int[(values.Length + 1) / 2];
        int valueIndex = 0;
        int codewordIndex = 0;
        while (valueIndex < values.Length)
        {
            int second = valueIndex + 1 < values.Length
                ? values[valueIndex + 1]
                : PaddingValue;
            codewords[codewordIndex] = (values[valueIndex] * ValueBase) + second;
            valueIndex = valueIndex + 2;
            codewordIndex = codewordIndex + 1;
        }

        return codewords;
    }

    static void ValidateText(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character != '\t'
                && character != '\n'
                && character != '\r'
                && (character < ' ' || character > '~'))
            {
                throw new BarcodeEncodingException("PDF417 text compaction supports printable ASCII, tab, line feed, and carriage return only.");
            }

            index = index + 1;
        }
    }

    static bool IsAlpha(char character)
    {
        return character == ' ' || (character >= 'A' && character <= 'Z');
    }

    static bool IsLower(char character)
    {
        return character == ' ' || (character >= 'a' && character <= 'z');
    }

    static int FindMixedValue(char character)
    {
        int index = 0;
        while (index < MixedCharacters.Length)
        {
            if (MixedCharacters[index] == character)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static int FindPunctuationValue(char character)
    {
        int index = 0;
        while (index < PunctuationCharacters.Length)
        {
            if (PunctuationCharacters[index] == character)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static int[] CopyPrefix(int[] source, int count)
    {
        int[] copy = new int[count];
        int index = 0;
        while (index < count)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static void ValidateSubmode(Pdf417TextSubmode submode)
    {
        if (submode != Pdf417TextSubmode.Alpha
            && submode != Pdf417TextSubmode.Lower
            && submode != Pdf417TextSubmode.Mixed
            && submode != Pdf417TextSubmode.Punctuation)
        {
            throw new BarcodeEncodingException("PDF417 initial text submode is invalid.");
        }
    }

    static void ValidateSubmodeForDecoding(Pdf417TextSubmode submode)
    {
        if (submode != Pdf417TextSubmode.Alpha
            && submode != Pdf417TextSubmode.Lower
            && submode != Pdf417TextSubmode.Mixed
            && submode != Pdf417TextSubmode.Punctuation)
        {
            throw new BarcodeDecodingException("PDF417 initial text submode is invalid.");
        }
    }
}
