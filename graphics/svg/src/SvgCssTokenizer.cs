namespace svg;

/// <summary>Stores one CSS source location.</summary>
internal class SvgCssLocation
{
    /// <summary>The one-based line.</summary>
    public int Line;
    /// <summary>The one-based column.</summary>
    public int Column;

    /// <summary>Creates a source location.</summary>
    /// <param name="line">The line.</param>
    /// <param name="column">The column.</param>
    internal SvgCssLocation(int line, int column)
    {
        Line = line;
        Column = column;
    }
}

/// <summary>Maintains bounded CSS tokenizer state.</summary>
internal class SvgCssTokenizerState
{
    string text;
    int maximumTokens;
    int position;
    int line;
    int column;
    int parseErrorCount;
    SvgCssTokenizeError error;
    int errorOffset;
    int errorLine;
    int errorColumn;
    string message;
    System.Collections.Generic.List<SvgCssToken> tokens;

    /// <summary>Creates tokenizer state.</summary>
    /// <param name="value">The CSS source.</param>
    /// <param name="maxTokens">The token bound.</param>
    internal SvgCssTokenizerState(string value, int maxTokens)
    {
        text = value;
        maximumTokens = maxTokens;
        position = 0;
        line = 1;
        column = 1;
        parseErrorCount = 0;
        error = SvgCssTokenizeError.None;
        errorOffset = -1;
        errorLine = 0;
        errorColumn = 0;
        message = "";
        tokens = new System.Collections.Generic.List<SvgCssToken>();
    }

    /// <summary>Tokenizes the complete state source.</summary>
    /// <returns>A complete result or bounded prefix.</returns>
    internal SvgCssTokenizeResult Tokenize()
    {
        while (position < text.Length && error == SvgCssTokenizeError.None)
        {
            if (!ReserveToken()) break;
            ConsumeToken();
        }
        if (error != SvgCssTokenizeError.None)
        {
            return new SvgCssTokenizeResult(false, error, tokens, parseErrorCount, errorOffset, errorLine, errorColumn, message);
        }
        return new SvgCssTokenizeResult(true, SvgCssTokenizeError.None, tokens, parseErrorCount, -1, 0, 0, "");
    }

    void ConsumeToken()
    {
        char current = text[position];
        if (IsWhitespace(current))
        {
            ConsumeWhitespace();
            return;
        }
        if (current == '/' && Peek(1) == '*')
        {
            ConsumeComment();
            return;
        }
        if (current == '"' || current == '\'')
        {
            ConsumeString(current);
            return;
        }
        if (current == '#')
        {
            ConsumeHashOrDelimiter();
            return;
        }
        if (current == '(')
        {
            ConsumeSimple(SvgCssTokenKind.OpenParenthesis);
            return;
        }
        if (current == ')')
        {
            ConsumeSimple(SvgCssTokenKind.CloseParenthesis);
            return;
        }
        if (current == '+')
        {
            if (WouldStartNumber(position)) ConsumeNumeric();
            else ConsumeSimple(SvgCssTokenKind.Delimiter);
            return;
        }
        if (current == ',')
        {
            ConsumeSimple(SvgCssTokenKind.Comma);
            return;
        }
        if (current == '-')
        {
            if (Starts("-->", position)) ConsumeFixed(SvgCssTokenKind.Cdc, 3, "");
            else if (WouldStartNumber(position)) ConsumeNumeric();
            else if (WouldStartIdentifier(position)) ConsumeIdentLike();
            else ConsumeSimple(SvgCssTokenKind.Delimiter);
            return;
        }
        if (current == '.')
        {
            if (WouldStartNumber(position)) ConsumeNumeric();
            else ConsumeSimple(SvgCssTokenKind.Delimiter);
            return;
        }
        if (current == ':')
        {
            ConsumeSimple(SvgCssTokenKind.Colon);
            return;
        }
        if (current == ';')
        {
            ConsumeSimple(SvgCssTokenKind.Semicolon);
            return;
        }
        if (current == '<' && Starts("<!--", position))
        {
            ConsumeFixed(SvgCssTokenKind.Cdo, 4, "");
            return;
        }
        if (current == '@')
        {
            ConsumeAtKeywordOrDelimiter();
            return;
        }
        if (current == '[')
        {
            ConsumeSimple(SvgCssTokenKind.OpenSquare);
            return;
        }
        if (current == '\\')
        {
            if (IsValidEscape(position)) ConsumeIdentLike();
            else
            {
                parseErrorCount = parseErrorCount + 1;
                ConsumeSimple(SvgCssTokenKind.Delimiter);
            }
            return;
        }
        if (current == ']')
        {
            ConsumeSimple(SvgCssTokenKind.CloseSquare);
            return;
        }
        if (current == '{')
        {
            ConsumeSimple(SvgCssTokenKind.OpenCurly);
            return;
        }
        if (current == '}')
        {
            ConsumeSimple(SvgCssTokenKind.CloseCurly);
            return;
        }
        if (IsDigit(current))
        {
            ConsumeNumeric();
            return;
        }
        if ((current == 'U' || current == 'u') && WouldStartUnicodeRange(position))
        {
            ConsumeUnicodeRange();
            return;
        }
        if (IsNameStart(current))
        {
            ConsumeIdentLike();
            return;
        }
        ConsumeSimple(SvgCssTokenKind.Delimiter);
    }

    void ConsumeWhitespace()
    {
        int start = position;
        int startLine = line;
        int startColumn = column;
        while (position < text.Length && IsWhitespace(text[position])) Advance();
        Emit(SvgCssTokenKind.Whitespace, start, startLine, startColumn, " ");
    }

    void ConsumeComment()
    {
        int start = position;
        int startLine = line;
        int startColumn = column;
        AdvanceCount(2);
        int valueStart = position;
        while (position < text.Length && !Starts("*/", position)) Advance();
        int valueEnd = position;
        if (position < text.Length) AdvanceCount(2);
        else parseErrorCount = parseErrorCount + 1;
        Emit(SvgCssTokenKind.Comment, start, startLine, startColumn, text.Substring(valueStart, valueEnd - valueStart));
    }

    void ConsumeString(char ending)
    {
        int start = position;
        int startLine = line;
        int startColumn = column;
        Advance();
        System.Text.StringBuilder value = new System.Text.StringBuilder();
        while (position < text.Length)
        {
            char current = text[position];
            if (current == ending)
            {
                Advance();
                Emit(SvgCssTokenKind.String, start, startLine, startColumn, value.ToString());
                return;
            }
            if (IsNewline(current))
            {
                parseErrorCount = parseErrorCount + 1;
                Emit(SvgCssTokenKind.BadString, start, startLine, startColumn, value.ToString());
                return;
            }
            if (current == '\\')
            {
                if (position + 1 >= text.Length)
                {
                    Advance();
                    continue;
                }
                if (IsNewline(Peek(1)))
                {
                    Advance();
                    ConsumeNewline();
                    continue;
                }
                ConsumeEscape(value);
                continue;
            }
            AppendPreprocessed(value);
        }
        parseErrorCount = parseErrorCount + 1;
        Emit(SvgCssTokenKind.String, start, startLine, startColumn, value.ToString());
    }

    void ConsumeHashOrDelimiter()
    {
        int start = position;
        int startLine = line;
        int startColumn = column;
        Advance();
        if (!IsNameAt(position) && !IsValidEscape(position))
        {
            Emit(SvgCssTokenKind.Delimiter, start, startLine, startColumn, "#");
            return;
        }
        SvgCssHashType hashType = WouldStartIdentifier(position) ? SvgCssHashType.Id : SvgCssHashType.Unrestricted;
        string name = ConsumeName();
        SvgCssToken token = Emit(SvgCssTokenKind.Hash, start, startLine, startColumn, name);
        token.HashType = hashType;
    }

    void ConsumeAtKeywordOrDelimiter()
    {
        int start = position;
        int startLine = line;
        int startColumn = column;
        Advance();
        if (!WouldStartIdentifier(position))
        {
            Emit(SvgCssTokenKind.Delimiter, start, startLine, startColumn, "@");
            return;
        }
        string name = ConsumeName();
        Emit(SvgCssTokenKind.AtKeyword, start, startLine, startColumn, name);
    }

    void ConsumeIdentLike()
    {
        int start = position;
        int startLine = line;
        int startColumn = column;
        string name = ConsumeName();
        if (position >= text.Length || text[position] != '(')
        {
            Emit(SvgCssTokenKind.Ident, start, startLine, startColumn, name);
            return;
        }
        Advance();
        if (AsciiEquals(name, "url") && ShouldConsumeUrl())
        {
            ConsumeUrl(start, startLine, startColumn);
            return;
        }
        Emit(SvgCssTokenKind.Function, start, startLine, startColumn, name);
    }

    bool ShouldConsumeUrl()
    {
        while (position + 1 < text.Length && IsWhitespace(text[position]) && IsWhitespace(text[position + 1])) Advance();
        if (position < text.Length && (text[position] == '"' || text[position] == '\'')) return false;
        if (position + 1 < text.Length && IsWhitespace(text[position]) && (text[position + 1] == '"' || text[position + 1] == '\'')) return false;
        return true;
    }

    void ConsumeUrl(int start, int startLine, int startColumn)
    {
        System.Text.StringBuilder value = new System.Text.StringBuilder();
        SkipWhitespace();
        while (position < text.Length)
        {
            char current = text[position];
            if (current == ')')
            {
                Advance();
                Emit(SvgCssTokenKind.Url, start, startLine, startColumn, value.ToString());
                return;
            }
            if (IsWhitespace(current))
            {
                SkipWhitespace();
                if (position >= text.Length)
                {
                    parseErrorCount = parseErrorCount + 1;
                    Emit(SvgCssTokenKind.Url, start, startLine, startColumn, value.ToString());
                    return;
                }
                if (text[position] == ')')
                {
                    Advance();
                    Emit(SvgCssTokenKind.Url, start, startLine, startColumn, value.ToString());
                    return;
                }
                ConsumeBadUrl(start, startLine, startColumn, value.ToString());
                return;
            }
            if (current == '"' || current == '\'' || current == '(' || current != '\0' && IsNonPrintable(current))
            {
                ConsumeBadUrl(start, startLine, startColumn, value.ToString());
                return;
            }
            if (current == '\\')
            {
                if (IsValidEscape(position))
                {
                    ConsumeEscape(value);
                    continue;
                }
                ConsumeBadUrl(start, startLine, startColumn, value.ToString());
                return;
            }
            AppendPreprocessed(value);
        }
        parseErrorCount = parseErrorCount + 1;
        Emit(SvgCssTokenKind.Url, start, startLine, startColumn, value.ToString());
    }

    void ConsumeBadUrl(int start, int startLine, int startColumn, string value)
    {
        parseErrorCount = parseErrorCount + 1;
        while (position < text.Length)
        {
            if (text[position] == ')')
            {
                Advance();
                break;
            }
            if (text[position] == '\\' && IsValidEscape(position))
            {
                System.Text.StringBuilder ignored = new System.Text.StringBuilder();
                ConsumeEscape(ignored);
            }
            else Advance();
        }
        Emit(SvgCssTokenKind.BadUrl, start, startLine, startColumn, value);
    }

    void ConsumeNumeric()
    {
        int start = position;
        int startLine = line;
        int startColumn = column;
        int numberStart = position;
        SvgCssNumberType numberType = ConsumeNumberRepresentation();
        string representation = text.Substring(numberStart, position - numberStart);
        SvgNumberParseResult parsed = SvgNumberParser.ParseCssNumber(representation, representation.Length);
        double numberValue = parsed.Success ? parsed.Value : representation[0] == '-' ? -double.MaxValue : double.MaxValue;
        if (WouldStartIdentifier(position))
        {
            string unit = ConsumeName();
            SvgCssToken dimension = Emit(SvgCssTokenKind.Dimension, start, startLine, startColumn, representation);
            dimension.NumberValue = numberValue;
            dimension.NumberType = numberType;
            dimension.Unit = unit;
            return;
        }
        if (position < text.Length && text[position] == '%')
        {
            Advance();
            SvgCssToken percentage = Emit(SvgCssTokenKind.Percentage, start, startLine, startColumn, representation);
            percentage.NumberValue = numberValue;
            percentage.NumberType = numberType;
            return;
        }
        SvgCssToken number = Emit(SvgCssTokenKind.Number, start, startLine, startColumn, representation);
        number.NumberValue = numberValue;
        number.NumberType = numberType;
    }

    SvgCssNumberType ConsumeNumberRepresentation()
    {
        SvgCssNumberType type = SvgCssNumberType.Integer;
        if (position < text.Length && (text[position] == '+' || text[position] == '-')) Advance();
        while (position < text.Length && IsDigit(text[position])) Advance();
        if (position + 1 < text.Length && text[position] == '.' && IsDigit(text[position + 1]))
        {
            type = SvgCssNumberType.Number;
            AdvanceCount(2);
            while (position < text.Length && IsDigit(text[position])) Advance();
        }
        if (HasExponent(position))
        {
            type = SvgCssNumberType.Number;
            Advance();
            if (position < text.Length && (text[position] == '+' || text[position] == '-')) Advance();
            while (position < text.Length && IsDigit(text[position])) Advance();
        }
        return type;
    }

    void ConsumeUnicodeRange()
    {
        int start = position;
        int startLine = line;
        int startColumn = column;
        AdvanceCount(2);
        int rangeStart = 0;
        int digits = 0;
        while (position < text.Length && digits < 6 && HexValue(text[position]) >= 0)
        {
            rangeStart = rangeStart * 16 + HexValue(text[position]);
            digits = digits + 1;
            Advance();
        }
        int questionMarks = 0;
        while (position < text.Length && digits + questionMarks < 6 && text[position] == '?')
        {
            rangeStart = rangeStart * 16;
            questionMarks = questionMarks + 1;
            Advance();
        }
        int rangeEnd = rangeStart;
        if (questionMarks > 0)
        {
            int count = 0;
            while (count < questionMarks)
            {
                rangeEnd = rangeEnd + 15 * Power16(count);
                count = count + 1;
            }
        }
        else if (position + 1 < text.Length && text[position] == '-' && HexValue(text[position + 1]) >= 0)
        {
            Advance();
            rangeEnd = 0;
            int endDigits = 0;
            while (position < text.Length && endDigits < 6 && HexValue(text[position]) >= 0)
            {
                rangeEnd = rangeEnd * 16 + HexValue(text[position]);
                endDigits = endDigits + 1;
                Advance();
            }
        }
        SvgCssToken token = Emit(SvgCssTokenKind.UnicodeRange, start, startLine, startColumn, text.Substring(start, position - start));
        token.UnicodeRangeStart = rangeStart;
        token.UnicodeRangeEnd = rangeEnd;
    }

    string ConsumeName()
    {
        System.Text.StringBuilder value = new System.Text.StringBuilder();
        while (position < text.Length)
        {
            if (IsName(text[position]))
            {
                AppendPreprocessed(value);
                continue;
            }
            if (text[position] == '\\' && IsValidEscape(position))
            {
                ConsumeEscape(value);
                continue;
            }
            break;
        }
        return value.ToString();
    }

    void ConsumeEscape(System.Text.StringBuilder value)
    {
        Advance();
        if (position >= text.Length)
        {
            parseErrorCount = parseErrorCount + 1;
            return;
        }
        int hex = HexValue(text[position]);
        if (hex < 0)
        {
            AppendPreprocessed(value);
            return;
        }
        int codePoint = 0;
        int digits = 0;
        while (position < text.Length && digits < 6)
        {
            hex = HexValue(text[position]);
            if (hex < 0) break;
            codePoint = codePoint * 16 + hex;
            digits = digits + 1;
            Advance();
        }
        if (position < text.Length && IsWhitespace(text[position])) ConsumeOneWhitespace();
        if (codePoint == 0 || codePoint > 1114111 || codePoint >= 55296 && codePoint <= 57343)
        {
            codePoint = 65533;
            parseErrorCount = parseErrorCount + 1;
        }
        value.Append(char.ConvertFromUtf32(codePoint));
    }

    void AppendPreprocessed(System.Text.StringBuilder value)
    {
        char current = text[position];
        if (current == '\0')
        {
            value.Append('\uFFFD');
            parseErrorCount = parseErrorCount + 1;
            Advance();
            return;
        }
        if (char.IsHighSurrogate(current))
        {
            if (position + 1 < text.Length && char.IsLowSurrogate(text[position + 1]))
            {
                value.Append(current);
                Advance();
                value.Append(text[position]);
                Advance();
            }
            else
            {
                value.Append('\uFFFD');
                parseErrorCount = parseErrorCount + 1;
                Advance();
            }
            return;
        }
        if (char.IsLowSurrogate(current))
        {
            value.Append('\uFFFD');
            parseErrorCount = parseErrorCount + 1;
            Advance();
            return;
        }
        value.Append(current);
        Advance();
    }

    void ConsumeSimple(SvgCssTokenKind kind)
    {
        int start = position;
        int startLine = line;
        int startColumn = column;
        string value = text[position].ToString();
        Advance();
        Emit(kind, start, startLine, startColumn, value);
    }

    void ConsumeFixed(SvgCssTokenKind kind, int count, string value)
    {
        int start = position;
        int startLine = line;
        int startColumn = column;
        AdvanceCount(count);
        Emit(kind, start, startLine, startColumn, value);
    }

    SvgCssToken Emit(SvgCssTokenKind kind, int start, int startLine, int startColumn, string value)
    {
        SvgCssToken token = new SvgCssToken(kind, text.Substring(start, position - start), value, start, position, startLine, startColumn, line, column);
        tokens.Add(token);
        return token;
    }

    bool ReserveToken()
    {
        if (tokens.Count < maximumTokens) return true;
        error = SvgCssTokenizeError.TokenLimitExceeded;
        errorOffset = position;
        errorLine = line;
        errorColumn = column;
        message = "The configured CSS token count was exceeded.";
        return false;
    }

    bool WouldStartIdentifier(int offset)
    {
        if (offset < 0 || offset >= text.Length) return false;
        char first = CharacterAt(offset);
        if (first == '-')
        {
            if (offset + 1 >= text.Length) return false;
            char second = CharacterAt(offset + 1);
            return IsNameStart(second) || second == '-' || IsValidEscape(offset + 1);
        }
        if (IsNameStart(first)) return true;
        return first == '\\' && IsValidEscape(offset);
    }

    bool WouldStartNumber(int offset)
    {
        char first = CharacterAt(offset);
        char second = CharacterAt(offset + 1);
        char third = CharacterAt(offset + 2);
        if (first == '+' || first == '-') return IsDigit(second) || second == '.' && IsDigit(third);
        if (first == '.') return IsDigit(second);
        return IsDigit(first);
    }

    bool WouldStartUnicodeRange(int offset)
    {
        if (offset + 2 >= text.Length || text[offset + 1] != '+') return false;
        return HexValue(text[offset + 2]) >= 0 || text[offset + 2] == '?';
    }

    bool IsValidEscape(int offset)
    {
        if (offset < 0 || offset >= text.Length || text[offset] != '\\') return false;
        if (offset + 1 >= text.Length) return false;
        return !IsNewline(text[offset + 1]);
    }

    bool HasExponent(int offset)
    {
        if (offset >= text.Length || text[offset] != 'e' && text[offset] != 'E') return false;
        int next = offset + 1;
        if (next < text.Length && IsDigit(text[next])) return true;
        if (next < text.Length && (text[next] == '+' || text[next] == '-')) next = next + 1;
        return next < text.Length && IsDigit(text[next]);
    }

    bool IsNameAt(int offset)
    {
        return offset < text.Length && IsName(text[offset]);
    }

    void SkipWhitespace()
    {
        while (position < text.Length && IsWhitespace(text[position])) Advance();
    }

    void ConsumeOneWhitespace()
    {
        if (position >= text.Length) return;
        char current = text[position];
        Advance();
        if (current == '\r' && position < text.Length && text[position] == '\n') Advance();
    }

    void ConsumeNewline()
    {
        ConsumeOneWhitespace();
    }

    void AdvanceCount(int count)
    {
        int index = 0;
        while (index < count && position < text.Length)
        {
            Advance();
            index = index + 1;
        }
    }

    void Advance()
    {
        char current = text[position];
        position = position + 1;
        if (current == '\r' || current == '\f')
        {
            line = line + 1;
            column = 1;
        }
        else if (current == '\n')
        {
            if (position < 2 || text[position - 2] != '\r')
            {
                line = line + 1;
                column = 1;
            }
        }
        else column = column + 1;
    }

    char Peek(int distance)
    {
        return CharacterAt(position + distance);
    }

    char CharacterAt(int offset)
    {
        if (offset < 0 || offset >= text.Length) return '\0';
        return text[offset];
    }

    bool Starts(string value, int offset)
    {
        if (offset < 0 || offset + value.Length > text.Length) return false;
        int index = 0;
        while (index < value.Length)
        {
            if (text[offset + index] != value[index]) return false;
            index = index + 1;
        }
        return true;
    }

    static bool AsciiEquals(string left, string right)
    {
        if (left.Length != right.Length) return false;
        int index = 0;
        while (index < left.Length)
        {
            char a = left[index];
            char b = right[index];
            if (a >= 'A' && a <= 'Z') a = (char)(a + 32);
            if (b >= 'A' && b <= 'Z') b = (char)(b + 32);
            if (a != b) return false;
            index = index + 1;
        }
        return true;
    }

    static bool IsWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\n' || value == '\r' || value == '\f';
    }

    static bool IsNewline(char value)
    {
        return value == '\n' || value == '\r' || value == '\f';
    }

    static bool IsDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    static bool IsNameStart(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || value == '_' || value == '\0' || value >= 128;
    }

    static bool IsName(char value)
    {
        return IsNameStart(value) || IsDigit(value) || value == '-';
    }

    static bool IsNonPrintable(char value)
    {
        return value >= '\0' && value <= '\b' || value == '\v' || value >= '\u000e' && value <= '\u001f' || value == '\u007f';
    }

    static int HexValue(char value)
    {
        if (value >= '0' && value <= '9') return value - '0';
        if (value >= 'A' && value <= 'F') return value - 'A' + 10;
        if (value >= 'a' && value <= 'f') return value - 'a' + 10;
        return -1;
    }

    static int Power16(int exponent)
    {
        int value = 1;
        int count = 0;
        while (count < exponent)
        {
            value = value * 16;
            count = count + 1;
        }
        return value;
    }

    /// <summary>Computes a CSS source location for an offset.</summary>
    /// <param name="value">The source text.</param>
    /// <param name="offset">The UTF-16 offset.</param>
    /// <returns>The one-based source location.</returns>
    internal static SvgCssLocation GetLocation(string value, int offset)
    {
        int line = 1;
        int column = 1;
        int index = 0;
        while (index < offset && index < value.Length)
        {
            char current = value[index];
            index = index + 1;
            if (current == '\r' || current == '\f')
            {
                line = line + 1;
                column = 1;
            }
            else if (current == '\n')
            {
                if (index < 2 || value[index - 2] != '\r')
                {
                    line = line + 1;
                    column = 1;
                }
            }
            else column = column + 1;
        }
        return new SvgCssLocation(line, column);
    }
}
