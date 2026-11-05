using System;

namespace html;

internal enum HtmlCharacterReferenceContext
{
    Data = 0,
    Attribute = 1
}

internal enum HtmlCharacterReferenceStatus
{
    NeedMoreInput = 0,
    NoMatch = 1,
    Match = 2
}

internal class HtmlCharacterReferenceDiagnosticData
{
    internal HtmlDiagnosticCode code;
    internal string message;
    internal int characterLength;

    internal HtmlCharacterReferenceDiagnosticData(HtmlDiagnosticCode code, string message, int characterLength)
    {
        this.code = code;
        this.message = message;
        this.characterLength = characterLength;
    }

    internal HtmlDiagnosticCode GetCode()
    {
        return this.code;
    }

    internal string GetMessage()
    {
        return this.message;
    }

    internal int GetCharacterLength()
    {
        return this.characterLength;
    }
}

internal class HtmlCharacterReferenceResult
{
    internal HtmlCharacterReferenceStatus status;
    internal int consumedCharacters;
    internal int firstCodePoint;
    internal int secondCodePoint;
    internal HtmlCharacterReferenceDiagnosticData[] diagnostics;

    internal HtmlCharacterReferenceResult(HtmlCharacterReferenceStatus status, int consumedCharacters,
        int firstCodePoint, int secondCodePoint, HtmlCharacterReferenceDiagnosticData[] diagnostics)
    {
        if (diagnostics == null)
        {
            throw new ArgumentNullException("diagnostics");
        }

        this.status = status;
        this.consumedCharacters = consumedCharacters;
        this.firstCodePoint = firstCodePoint;
        this.secondCodePoint = secondCodePoint;
        this.diagnostics = diagnostics;
    }

    internal HtmlCharacterReferenceStatus GetStatus()
    {
        return this.status;
    }

    internal bool IsMatch()
    {
        return this.status == HtmlCharacterReferenceStatus.Match;
    }

    internal int GetConsumedCharacters()
    {
        return this.consumedCharacters;
    }

    internal int GetFirstCodePoint()
    {
        return this.firstCodePoint;
    }

    internal bool HasSecondCodePoint()
    {
        return this.secondCodePoint >= 0;
    }

    internal int GetSecondCodePoint()
    {
        return this.secondCodePoint;
    }

    internal string GetReplacementString()
    {
        if (!this.IsMatch())
        {
            return "";
        }

        string result = UnicodeTools.ScalarToString(this.firstCodePoint);
        if (this.secondCodePoint >= 0)
        {
            result = result + UnicodeTools.ScalarToString(this.secondCodePoint);
        }

        return result;
    }

    internal int GetDiagnosticCount()
    {
        return this.diagnostics.Length;
    }

    internal HtmlDiagnosticCode GetDiagnosticCode(int index)
    {
        return this.GetDiagnostic(index).GetCode();
    }

    internal string GetDiagnosticMessage(int index)
    {
        return this.GetDiagnostic(index).GetMessage();
    }

    internal int GetDiagnosticCharacterLength(int index)
    {
        return this.GetDiagnostic(index).GetCharacterLength();
    }

    internal void AddDiagnostics(HtmlDiagnosticCollector collector, HtmlInput input, int ampersandOffset)
    {
        if (collector == null)
        {
            throw new ArgumentNullException("collector");
        }

        if (input == null)
        {
            throw new ArgumentNullException("input");
        }

        if (ampersandOffset < 0 || ampersandOffset > input.GetLength())
        {
            throw new ArgumentOutOfRangeException("ampersandOffset", ampersandOffset,
                "The ampersand offset is outside the input.");
        }

        int index = 0;
        while (index < this.diagnostics.Length)
        {
            HtmlCharacterReferenceDiagnosticData diagnostic = this.diagnostics[index];
            int end = ampersandOffset + diagnostic.GetCharacterLength();
            if (end > input.GetLength())
            {
                throw new ArgumentException("The input does not contain the complete diagnostic span.", "input");
            }

            collector.AddDetails(diagnostic.GetCode(), HtmlDiagnosticSeverity.Warning, diagnostic.GetMessage(),
                input.GetSourceSpan(ampersandOffset, end));
            index = index + 1;
        }
    }

    internal HtmlCharacterReferenceDiagnosticData GetDiagnostic(int index)
    {
        if (index < 0 || index >= this.diagnostics.Length)
        {
            throw new ArgumentOutOfRangeException("index", index,
                "The character-reference diagnostic index is outside the result.");
        }

        return this.diagnostics[index];
    }
}

internal static class HtmlCharacterReferenceDecoder
{
    internal const int MaximumScalar = 1114111;
    internal const int OverflowScalar = 1114112;

    internal static HtmlCharacterReferenceResult Decode(string input, bool inputComplete,
        HtmlCharacterReferenceContext context)
    {
        if (input == null)
        {
            throw new ArgumentNullException("input");
        }

        return Decode(input.AsSpan(), inputComplete, context, '\0');
    }

    internal static HtmlCharacterReferenceResult Decode(ReadOnlySpan<char> input, bool inputComplete,
        HtmlCharacterReferenceContext context)
    {
        return Decode(input, inputComplete, context, '\0');
    }

    internal static HtmlCharacterReferenceResult Decode(ReadOnlySpan<char> input, bool inputComplete,
        HtmlCharacterReferenceContext context, char additionalAllowedCharacter)
    {
        ValidateContext(context);
        if (input.Length == 0)
        {
            return inputComplete ? CreateNoMatch() : CreateNeedMoreInput();
        }

        char first = input[0];
        if (Ascii.IsHtmlWhitespace(first) || first == '<' || first == '&'
            || additionalAllowedCharacter != '\0' && first == additionalAllowedCharacter)
        {
            return CreateNoMatch();
        }

        if (first == '#')
        {
            return DecodeNumeric(input, inputComplete);
        }

        if (Ascii.IsAlphanumeric(first))
        {
            return DecodeNamed(input, inputComplete, context);
        }

        return CreateNoMatch();
    }

    internal static HtmlCharacterReferenceResult DecodeNamed(ReadOnlySpan<char> input, bool inputComplete,
        HtmlCharacterReferenceContext context)
    {
        int nameEnd = 0;
        while (nameEnd < input.Length && Ascii.IsAlphanumeric(input[nameEnd]))
        {
            nameEnd = nameEnd + 1;
        }

        if (nameEnd == input.Length && !inputComplete)
        {
            return CreateNeedMoreInput();
        }

        int examinedEnd = nameEnd;
        if (nameEnd < input.Length && input[nameEnd] == ';')
        {
            examinedEnd = nameEnd + 1;
        }

        HtmlNamedCharacterReferenceMatch match = HtmlNamedCharacterReferences.FindLongest(input.Slice(0, examinedEnd));
        if (!match.IsMatch())
        {
            if (examinedEnd > nameEnd)
            {
                return CreateNoMatchWithInvalid(1 + examinedEnd,
                    "An unknown named character reference was encountered.");
            }

            return CreateNoMatch();
        }

        int consumed = match.GetConsumedCharacters();
        if (!match.HasSemicolon() && context == HtmlCharacterReferenceContext.Attribute
            && consumed < input.Length && (input[consumed] == '=' || Ascii.IsAlphanumeric(input[consumed])))
        {
            return CreateNoMatch();
        }

        if (!match.HasSemicolon())
        {
            HtmlCharacterReferenceDiagnosticData diagnostic = new HtmlCharacterReferenceDiagnosticData(
                HtmlDiagnosticCode.CharacterReferenceMissingSemicolon,
                "A named character reference was not terminated by a semicolon.", 1 + consumed);
            return CreateMatch(consumed, match.GetFirstCodePoint(), match.GetSecondCodePoint(),
                new HtmlCharacterReferenceDiagnosticData[] { diagnostic });
        }

        return CreateMatch(consumed, match.GetFirstCodePoint(), match.GetSecondCodePoint(),
            Array.Empty<HtmlCharacterReferenceDiagnosticData>());
    }

    internal static HtmlCharacterReferenceResult DecodeNumeric(ReadOnlySpan<char> input, bool inputComplete)
    {
        int radix = 10;
        int digitStart = 1;
        if (digitStart == input.Length)
        {
            return inputComplete
                ? CreateNoMatchWithInvalid(2, "A numeric character reference contained no digits.")
                : CreateNeedMoreInput();
        }

        if (input[digitStart] == 'x' || input[digitStart] == 'X')
        {
            radix = 16;
            digitStart = digitStart + 1;
            if (digitStart == input.Length)
            {
                return inputComplete
                    ? CreateNoMatchWithInvalid(1 + digitStart,
                        "A numeric character reference contained no hexadecimal digits.")
                    : CreateNeedMoreInput();
            }
        }

        int position = digitStart;
        int number = 0;
        while (position < input.Length)
        {
            int digit = GetDigit(input[position], radix);
            if (digit < 0)
            {
                break;
            }

            if (number <= MaximumScalar)
            {
                if (number > (MaximumScalar - digit) / radix)
                {
                    number = OverflowScalar;
                }
                else
                {
                    number = number * radix + digit;
                }
            }

            position = position + 1;
        }

        if (position == digitStart)
        {
            if (position == input.Length && !inputComplete)
            {
                return CreateNeedMoreInput();
            }

            int diagnosticLength = 1 + position;
            if (position < input.Length)
            {
                diagnosticLength = diagnosticLength + 1;
            }

            return CreateNoMatchWithInvalid(diagnosticLength,
                radix == 16
                    ? "A numeric character reference contained no hexadecimal digits."
                    : "A numeric character reference contained no digits.");
        }

        if (position == input.Length && !inputComplete)
        {
            return CreateNeedMoreInput();
        }

        bool hasSemicolon = position < input.Length && input[position] == ';';
        int consumed = hasSemicolon ? position + 1 : position;
        HtmlCharacterReferenceDiagnosticData? missing = null;
        if (!hasSemicolon)
        {
            missing = new HtmlCharacterReferenceDiagnosticData(
                HtmlDiagnosticCode.CharacterReferenceMissingSemicolon,
                "A numeric character reference was not terminated by a semicolon.", 1 + consumed);
        }

        HtmlCharacterReferenceDiagnosticData? invalid = null;
        int scalar = number;
        if (number == 0)
        {
            scalar = UnicodeTools.ReplacementCharacter;
            invalid = CreateInvalidNumericDiagnostic(consumed,
                "A null numeric character reference was replaced with U+FFFD.");
        }
        else if (number > MaximumScalar)
        {
            scalar = UnicodeTools.ReplacementCharacter;
            invalid = CreateInvalidNumericDiagnostic(consumed,
                "An out-of-range numeric character reference was replaced with U+FFFD.");
        }
        else if (UnicodeTools.IsSurrogateCodePoint(number))
        {
            scalar = UnicodeTools.ReplacementCharacter;
            invalid = CreateInvalidNumericDiagnostic(consumed,
                "A surrogate numeric character reference was replaced with U+FFFD.");
        }
        else if (UnicodeTools.IsNoncharacter(number))
        {
            invalid = CreateInvalidNumericDiagnostic(consumed,
                "A numeric character reference resolved to a Unicode noncharacter.");
        }
        else if (IsDisallowedControl(number))
        {
            scalar = GetWindows1252Replacement(number);
            invalid = CreateInvalidNumericDiagnostic(consumed,
                "A numeric character reference resolved to a control character.");
        }

        return CreateNumericMatch(consumed, scalar, missing, invalid);
    }

    internal static HtmlCharacterReferenceResult CreateNumericMatch(int consumed, int scalar,
        HtmlCharacterReferenceDiagnosticData? missing, HtmlCharacterReferenceDiagnosticData? invalid)
    {
        if (missing != null && invalid != null)
        {
            return CreateMatch(consumed, scalar, -1,
                new HtmlCharacterReferenceDiagnosticData[] { missing, invalid });
        }

        if (missing != null)
        {
            return CreateMatch(consumed, scalar, -1,
                new HtmlCharacterReferenceDiagnosticData[] { missing });
        }

        if (invalid != null)
        {
            return CreateMatch(consumed, scalar, -1,
                new HtmlCharacterReferenceDiagnosticData[] { invalid });
        }

        return CreateMatch(consumed, scalar, -1, Array.Empty<HtmlCharacterReferenceDiagnosticData>());
    }

    internal static HtmlCharacterReferenceDiagnosticData CreateInvalidNumericDiagnostic(int consumed, string message)
    {
        return new HtmlCharacterReferenceDiagnosticData(
            HtmlDiagnosticCode.CharacterReferenceInvalid, message, 1 + consumed);
    }

    internal static int GetDigit(char value, int radix)
    {
        if (radix == 10)
        {
            return Ascii.IsDigit(value) ? value - '0' : -1;
        }

        return Ascii.GetHexValue(value);
    }

    internal static bool IsDisallowedControl(int value)
    {
        if (!UnicodeTools.IsControl(value))
        {
            return false;
        }

        return value == 13 || value != 9 && value != 10 && value != 12 && value != 32;
    }

    internal static int GetWindows1252Replacement(int value)
    {
        switch (value)
        {
            case 0x80: return 0x20AC;
            case 0x82: return 0x201A;
            case 0x83: return 0x0192;
            case 0x84: return 0x201E;
            case 0x85: return 0x2026;
            case 0x86: return 0x2020;
            case 0x87: return 0x2021;
            case 0x88: return 0x02C6;
            case 0x89: return 0x2030;
            case 0x8A: return 0x0160;
            case 0x8B: return 0x2039;
            case 0x8C: return 0x0152;
            case 0x8E: return 0x017D;
            case 0x91: return 0x2018;
            case 0x92: return 0x2019;
            case 0x93: return 0x201C;
            case 0x94: return 0x201D;
            case 0x95: return 0x2022;
            case 0x96: return 0x2013;
            case 0x97: return 0x2014;
            case 0x98: return 0x02DC;
            case 0x99: return 0x2122;
            case 0x9A: return 0x0161;
            case 0x9B: return 0x203A;
            case 0x9C: return 0x0153;
            case 0x9E: return 0x017E;
            case 0x9F: return 0x0178;
            default: return value;
        }
    }

    internal static HtmlCharacterReferenceResult CreateNeedMoreInput()
    {
        return new HtmlCharacterReferenceResult(HtmlCharacterReferenceStatus.NeedMoreInput,
            0, -1, -1, Array.Empty<HtmlCharacterReferenceDiagnosticData>());
    }

    internal static HtmlCharacterReferenceResult CreateNoMatch()
    {
        return new HtmlCharacterReferenceResult(HtmlCharacterReferenceStatus.NoMatch,
            0, -1, -1, Array.Empty<HtmlCharacterReferenceDiagnosticData>());
    }

    internal static HtmlCharacterReferenceResult CreateNoMatchWithInvalid(int characterLength, string message)
    {
        HtmlCharacterReferenceDiagnosticData diagnostic = new HtmlCharacterReferenceDiagnosticData(
            HtmlDiagnosticCode.CharacterReferenceInvalid, message, characterLength);
        return new HtmlCharacterReferenceResult(HtmlCharacterReferenceStatus.NoMatch,
            0, -1, -1, new HtmlCharacterReferenceDiagnosticData[] { diagnostic });
    }

    internal static HtmlCharacterReferenceResult CreateMatch(int consumed, int firstCodePoint,
        int secondCodePoint, HtmlCharacterReferenceDiagnosticData[] diagnostics)
    {
        return new HtmlCharacterReferenceResult(HtmlCharacterReferenceStatus.Match,
            consumed, firstCodePoint, secondCodePoint, diagnostics);
    }

    internal static void ValidateContext(HtmlCharacterReferenceContext context)
    {
        if ((int)context < 0 || (int)context > 1)
        {
            throw new ArgumentOutOfRangeException("context", (int)context,
                "Unknown character-reference context.");
        }
    }
}
