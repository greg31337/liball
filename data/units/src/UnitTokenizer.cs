namespace units;

internal class UnitTokenizer
{
    internal UnitToken[] Tokenize(string text)
    {
        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }

        System.Collections.Generic.List<UnitToken> tokens = new System.Collections.Generic.List<UnitToken>();
        int index = 0;

        while (index < text.Length)
        {
            char ch = text[index];

            if (char.IsWhiteSpace(ch))
            {
                index = index + 1;
            }
            else if (ch == '[')
            {
                index = AddCanonicalIdentifier(tokens, text, index);
            }
            else if (IsIdentifierStart(ch))
            {
                index = AddIdentifier(tokens, text, index);
            }
            else if (IsIntegerStart(text, index))
            {
                index = AddInteger(tokens, text, index);
            }
            else
            {
                tokens.Add(CreateSymbolToken(ch, index));
                index = index + 1;
            }
        }

        tokens.Add(new UnitToken(UnitTokenKind.End, "", text.Length));
        return tokens.ToArray();
    }

    internal static int AddCanonicalIdentifier(System.Collections.Generic.List<UnitToken> tokens, string text, int start)
    {
        int end = start + 1;
        while (end < text.Length && text[end] != ']')
        {
            end = end + 1;
        }

        if (end >= text.Length)
        {
            tokens.Add(new UnitToken(UnitTokenKind.Unknown, text.Substring(start), start));
            return text.Length;
        }

        string encoded = text.Substring(start + 1, end - start - 1);
        string? decoded = DecodeCanonicalIdentifier(encoded);
        if (decoded == null || decoded.Length == 0)
        {
            tokens.Add(new UnitToken(UnitTokenKind.Unknown, text.Substring(start, end - start + 1), start));
        }
        else
        {
            tokens.Add(new UnitToken(UnitTokenKind.Identifier, decoded, start));
        }

        return end + 1;
    }

    internal static string? DecodeCanonicalIdentifier(string encoded)
    {
        if (encoded.Length == 0 || encoded.Length % 4 != 0)
        {
            return null;
        }

        string decoded = "";
        int index = 0;
        while (index < encoded.Length)
        {
            int first = HexValue(encoded[index]);
            int second = HexValue(encoded[index + 1]);
            int third = HexValue(encoded[index + 2]);
            int fourth = HexValue(encoded[index + 3]);
            if (first < 0 || second < 0 || third < 0 || fourth < 0)
            {
                return null;
            }

            int value = first * 4096 + second * 256 + third * 16 + fourth;
            decoded = decoded + (char)value;
            index = index + 4;
        }

        return decoded;
    }

    internal static int HexValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        return -1;
    }

    internal static int AddIdentifier(System.Collections.Generic.List<UnitToken> tokens, string text, int start)
    {
        int index = start + 1;

        while (index < text.Length && (IsIdentifierPart(text[index]) || IsIdentifierHyphen(text, index)))
        {
            index = index + 1;
        }

        tokens.Add(new UnitToken(UnitTokenKind.Identifier, text.Substring(start, index - start), start));
        return index;
    }

    internal static int AddInteger(System.Collections.Generic.List<UnitToken> tokens, string text, int start)
    {
        int index = start;

        if (text[index] == '-' || text[index] == '+')
        {
            index = index + 1;
        }

        while (index < text.Length && IsAsciiDigit(text[index]))
        {
            index = index + 1;
        }

        tokens.Add(new UnitToken(UnitTokenKind.Integer, text.Substring(start, index - start), start));
        return index;
    }

    internal static UnitToken CreateSymbolToken(char ch, int position)
    {
        if (ch == '*')
        {
            return new UnitToken(UnitTokenKind.Star, "*", position);
        }

        if (ch == '/')
        {
            return new UnitToken(UnitTokenKind.Slash, "/", position);
        }

        if (ch == '^')
        {
            return new UnitToken(UnitTokenKind.Caret, "^", position);
        }

        if (ch == '(')
        {
            return new UnitToken(UnitTokenKind.OpenParenthesis, "(", position);
        }

        if (ch == ')')
        {
            return new UnitToken(UnitTokenKind.CloseParenthesis, ")", position);
        }

        return new UnitToken(UnitTokenKind.Unknown, ch.ToString(), position);
    }

    internal static bool IsIdentifierStart(char ch)
    {
        return char.IsLetter(ch) || ch == '_' || ch == '%';
    }

    internal static bool IsIdentifierHyphen(string text, int index)
    {
        return text[index] == '-' && index + 1 < text.Length && char.IsLetter(text[index + 1]);
    }

    internal static bool IsIdentifierPart(char ch)
    {
        return char.IsLetter(ch) || ch == '_' || ch == '%';
    }

    internal static bool IsIntegerStart(string text, int index)
    {
        if (IsAsciiDigit(text[index]))
        {
            return true;
        }

        return (text[index] == '-' || text[index] == '+') && index + 1 < text.Length && IsAsciiDigit(text[index + 1]);
    }

    internal static bool IsAsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }
}
