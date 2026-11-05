using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace html;

internal static class CssTokenizer
{
    internal static CssTokenizeResult Tokenize(string css)
    {
        return Tokenize(css, HtmlLimits.CreateDefault());
    }

    internal static CssTokenizeResult Tokenize(string css, HtmlLimits limits)
    {
        return Tokenize(css, limits, CancellationToken.None);
    }

    internal static CssTokenizeResult Tokenize(string css, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        if (css == null)
        {
            throw new ArgumentNullException("css");
        }

        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        limits.Validate();
        cancellationToken.ThrowIfCancellationRequested();
        if (css.Length > limits.GetMaximumInputCharacters())
        {
            return new CssTokenizeResult(new CssToken[0], false, true, 0);
        }

        CssTokenizerState state = new CssTokenizerState(css,
            limits.GetMaximumCssTokens(), cancellationToken);
        return state.Run();
    }
}

internal class CssTokenizerState
{
    internal string input = "";
    internal HtmlLineMap lineMap = HtmlLineMap.Create();
    internal int position;
    internal int maximumTokens;
    internal int errorCount;
    internal CancellationToken cancellationToken;

    internal CssTokenizerState(string css, int maximumTokens,
        CancellationToken cancellationTokenValue)
    {
        this.Initialize(css, maximumTokens, cancellationTokenValue);
    }

    internal void Initialize(string css, int maximumTokens,
        CancellationToken cancellationTokenValue)
    {
        if (css == null)
        {
            throw new ArgumentNullException("css");
        }

        this.lineMap = HtmlLineMap.Create();
        this.cancellationToken = cancellationTokenValue;
        this.input = Preprocess(css, this.lineMap, cancellationTokenValue);
        this.position = 0;
        this.maximumTokens = maximumTokens;
        this.errorCount = 0;
    }

    internal CssTokenizeResult Run()
    {
        List<CssToken> tokens = new List<CssToken>();
        bool complete = false;
        bool truncated = false;
        while (!complete)
        {
            this.CheckCancellation();
            this.ConsumeComments();
            if (this.IsEnd())
            {
                tokens.Add(this.CreateToken(CssTokenKind.End, "", "", 0,
                    CssNumericType.Integer, CssHashType.Unrestricted,
                    this.position));
                complete = true;
                continue;
            }

            if (tokens.Count >= this.maximumTokens)
            {
                truncated = true;
                break;
            }

            tokens.Add(this.ReadToken());
        }

        return new CssTokenizeResult(tokens.ToArray(), !truncated, truncated,
            this.errorCount);
    }

    internal CssToken ReadToken()
    {
        int start = this.position;
        char current = this.input[this.position];
        if (IsWhitespace(current))
        {
            this.position = this.position + 1;
            while (IsWhitespace(this.Peek(0)))
            {
                this.CheckCancellation();
                this.position = this.position + 1;
            }

            return this.CreateToken(CssTokenKind.Whitespace, "", "", 0,
                CssNumericType.Integer, CssHashType.Unrestricted, start);
        }

        if (current == '\'' || current == '"')
        {
            this.position = this.position + 1;
            return this.ConsumeString(start, current);
        }

        if (current == '#')
        {
            this.position = this.position + 1;
            if (this.IsIdentifierCodePoint(this.Peek(0))
                || this.IsValidEscape(this.position))
            {
                CssHashType hashType = this.StartsIdentifier(this.position)
                    ? CssHashType.Id : CssHashType.Unrestricted;
                string name = this.ConsumeIdentifier();
                return this.CreateToken(CssTokenKind.Hash, name, "", 0,
                    CssNumericType.Integer, hashType, start);
            }

            return this.CreateToken(CssTokenKind.Delimiter, "#", "", 0,
                CssNumericType.Integer, CssHashType.Unrestricted, start);
        }

        if (current == '@' && this.StartsIdentifier(this.position + 1))
        {
            this.position = this.position + 1;
            string name = this.ConsumeIdentifier();
            return this.CreateToken(CssTokenKind.AtKeyword, name, "", 0,
                CssNumericType.Integer, CssHashType.Unrestricted, start);
        }

        if (this.StartsWith("<!--"))
        {
            this.position = this.position + 4;
            return this.CreateToken(CssTokenKind.Cdo, "", "", 0,
                CssNumericType.Integer, CssHashType.Unrestricted, start);
        }

        if (this.StartsWith("-->"))
        {
            this.position = this.position + 3;
            return this.CreateToken(CssTokenKind.Cdc, "", "", 0,
                CssNumericType.Integer, CssHashType.Unrestricted, start);
        }

        if (this.StartsNumber(this.position))
        {
            return this.ConsumeNumeric(start);
        }

        if (this.StartsIdentifier(this.position))
        {
            return this.ConsumeIdentLike(start);
        }

        this.position = this.position + 1;
        if (current == '\\' && this.Peek(0) == '\n')
        {
            this.AddError();
        }

        CssTokenKind kind = CssTokenKind.Delimiter;
        if (current == ':')
        {
            kind = CssTokenKind.Colon;
        }
        else if (current == ';')
        {
            kind = CssTokenKind.Semicolon;
        }
        else if (current == ',')
        {
            kind = CssTokenKind.Comma;
        }
        else if (current == '[')
        {
            kind = CssTokenKind.OpenSquare;
        }
        else if (current == ']')
        {
            kind = CssTokenKind.CloseSquare;
        }
        else if (current == '(')
        {
            kind = CssTokenKind.OpenParenthesis;
        }
        else if (current == ')')
        {
            kind = CssTokenKind.CloseParenthesis;
        }
        else if (current == '{')
        {
            kind = CssTokenKind.OpenCurly;
        }
        else if (current == '}')
        {
            kind = CssTokenKind.CloseCurly;
        }

        return this.CreateToken(kind,
            kind == CssTokenKind.Delimiter ? current.ToString() : "", "", 0,
            CssNumericType.Integer, CssHashType.Unrestricted, start);
    }

    internal CssToken ConsumeIdentLike(int start)
    {
        string name = this.ConsumeIdentifier();
        if (this.Peek(0) != '(')
        {
            return this.CreateToken(CssTokenKind.Identifier, name, "", 0,
                CssNumericType.Integer, CssHashType.Unrestricted, start);
        }

        this.position = this.position + 1;
        if (string.Equals(name, "url", StringComparison.OrdinalIgnoreCase))
        {
            while (IsWhitespace(this.Peek(0))
                && IsWhitespace(this.Peek(1)))
            {
                this.CheckCancellation();
                this.position = this.position + 1;
            }

            if (this.UrlStartsWithString())
            {
                return this.CreateToken(CssTokenKind.Function, name, "", 0,
                    CssNumericType.Integer, CssHashType.Unrestricted, start);
            }

            return this.ConsumeUrl(start);
        }

        return this.CreateToken(CssTokenKind.Function, name, "", 0,
            CssNumericType.Integer, CssHashType.Unrestricted, start);
    }

    internal bool UrlStartsWithString()
    {
        int current = this.Peek(0);
        return current == '\'' || current == '"'
            || IsWhitespace(current)
                && (this.Peek(1) == '\'' || this.Peek(1) == '"');
    }

    internal CssToken ConsumeUrl(int start)
    {
        while (IsWhitespace(this.Peek(0)))
        {
            this.CheckCancellation();
            this.position = this.position + 1;
        }

        StringBuilder value = new StringBuilder();
        while (!this.IsEnd())
        {
            this.CheckCancellation();
            int current = this.Peek(0);
            if (current == ')')
            {
                this.position = this.position + 1;
                return this.CreateToken(CssTokenKind.Url, value.ToString(),
                    "", 0, CssNumericType.Integer,
                    CssHashType.Unrestricted, start);
            }

            if (IsWhitespace(current))
            {
                while (IsWhitespace(this.Peek(0)))
                {
                    this.CheckCancellation();
                    this.position = this.position + 1;
                }

                if (this.Peek(0) == ')')
                {
                    this.position = this.position + 1;
                    return this.CreateToken(CssTokenKind.Url,
                        value.ToString(), "", 0, CssNumericType.Integer,
                        CssHashType.Unrestricted, start);
                }

                if (this.IsEnd())
                {
                    this.AddError();
                    return this.CreateToken(CssTokenKind.Url,
                        value.ToString(), "", 0, CssNumericType.Integer,
                        CssHashType.Unrestricted, start);
                }

                return this.ConsumeBadUrl(start);
            }

            if (current == '"' || current == '\'' || current == '('
                || IsNonPrintable(current))
            {
                return this.ConsumeBadUrl(start);
            }

            if (current == '\\')
            {
                if (!this.IsValidEscape(this.position))
                {
                    return this.ConsumeBadUrl(start);
                }

                this.position = this.position + 1;
                this.ConsumeEscapedCodePoint(value);
                continue;
            }

            this.AppendCurrentCodePoint(value);
        }

        this.AddError();
        return this.CreateToken(CssTokenKind.Url, value.ToString(), "", 0,
            CssNumericType.Integer, CssHashType.Unrestricted, start);
    }

    internal CssToken ConsumeBadUrl(int start)
    {
        this.AddError();
        while (!this.IsEnd())
        {
            this.CheckCancellation();
            if (this.Peek(0) == ')')
            {
                this.position = this.position + 1;
                break;
            }

            if (this.IsValidEscape(this.position))
            {
                StringBuilder discarded = new StringBuilder();
                this.position = this.position + 1;
                this.ConsumeEscapedCodePoint(discarded);
            }
            else
            {
                this.position = this.position + 1;
            }
        }

        return this.CreateToken(CssTokenKind.BadUrl, "", "", 0,
            CssNumericType.Integer, CssHashType.Unrestricted, start);
    }

    internal CssToken ConsumeString(int start, char ending)
    {
        StringBuilder value = new StringBuilder();
        while (!this.IsEnd())
        {
            this.CheckCancellation();
            int current = this.Peek(0);
            if (current == ending)
            {
                this.position = this.position + 1;
                return this.CreateToken(CssTokenKind.String,
                    value.ToString(), "", 0, CssNumericType.Integer,
                    CssHashType.Unrestricted, start);
            }

            if (current == '\n')
            {
                this.AddError();
                return this.CreateToken(CssTokenKind.BadString,
                    value.ToString(), "", 0, CssNumericType.Integer,
                    CssHashType.Unrestricted, start);
            }

            if (current == '\\')
            {
                this.position = this.position + 1;
                if (this.IsEnd())
                {
                    continue;
                }

                if (this.Peek(0) == '\n')
                {
                    this.position = this.position + 1;
                    continue;
                }

                this.ConsumeEscapedCodePoint(value);
                continue;
            }

            this.AppendCurrentCodePoint(value);
        }

        this.AddError();
        return this.CreateToken(CssTokenKind.String, value.ToString(), "", 0,
            CssNumericType.Integer, CssHashType.Unrestricted, start);
    }

    internal CssToken ConsumeNumeric(int start)
    {
        CssNumberData number = this.ConsumeNumber();
        if (this.StartsIdentifier(this.position))
        {
            string unit = this.ConsumeIdentifier();
            return this.CreateNumericToken(CssTokenKind.Dimension, unit,
                number, start);
        }

        if (this.Peek(0) == '%')
        {
            this.position = this.position + 1;
            return this.CreateNumericToken(CssTokenKind.Percentage, "",
                number, start);
        }

        return this.CreateNumericToken(CssTokenKind.Number, "", number,
            start);
    }

    internal CssNumberData ConsumeNumber()
    {
        int start = this.position;
        CssNumericType type = CssNumericType.Integer;
        int current = this.Peek(0);
        if (current == '+' || current == '-')
        {
            this.position = this.position + 1;
        }

        while (IsDigit(this.Peek(0)))
        {
            this.CheckCancellation();
            this.position = this.position + 1;
        }

        if (this.Peek(0) == '.' && IsDigit(this.Peek(1)))
        {
            type = CssNumericType.Number;
            this.position = this.position + 2;
            while (IsDigit(this.Peek(0)))
            {
                this.CheckCancellation();
                this.position = this.position + 1;
            }
        }

        if ((this.Peek(0) == 'e' || this.Peek(0) == 'E')
            && this.StartsExponent(this.position))
        {
            type = CssNumericType.Number;
            this.position = this.position + 1;
            if (this.Peek(0) == '+' || this.Peek(0) == '-')
            {
                this.position = this.position + 1;
            }

            while (IsDigit(this.Peek(0)))
            {
                this.CheckCancellation();
                this.position = this.position + 1;
            }
        }

        string representation = this.input.Substring(start,
            this.position - start);
        double value = double.Parse(representation, NumberStyles.Float,
            CultureInfo.InvariantCulture);
        return new CssNumberData(representation, value, type);
    }

    internal string ConsumeIdentifier()
    {
        StringBuilder result = new StringBuilder();
        while (!this.IsEnd())
        {
            this.CheckCancellation();
            if (this.IsIdentifierCodePoint(this.Peek(0)))
            {
                this.AppendCurrentCodePoint(result);
                continue;
            }

            if (this.IsValidEscape(this.position))
            {
                this.position = this.position + 1;
                if (this.IsEnd())
                {
                    this.AddError();
                }

                this.ConsumeEscapedCodePoint(result);
                continue;
            }

            break;
        }

        return result.ToString();
    }

    internal void ConsumeEscapedCodePoint(StringBuilder result)
    {
        if (this.IsEnd())
        {
            result.Append('\uFFFD');
            return;
        }

        if (IsHexDigit(this.Peek(0)))
        {
            int value = 0;
            int count = 0;
            while (count < 6 && IsHexDigit(this.Peek(0)))
            {
                this.CheckCancellation();
                value = value * 16 + GetHexValue(this.Peek(0));
                this.position = this.position + 1;
                count = count + 1;
            }

            if (IsWhitespace(this.Peek(0)))
            {
                this.position = this.position + 1;
            }

            if (value == 0 || value > 1114111
                || value >= 55296 && value <= 57343)
            {
                result.Append('\uFFFD');
            }
            else
            {
                result.Append(char.ConvertFromUtf32(value));
            }

            return;
        }

        this.AppendCurrentCodePoint(result);
    }

    internal void AppendCurrentCodePoint(StringBuilder result)
    {
        char current = this.input[this.position];
        result.Append(current);
        this.position = this.position + 1;
        if (char.IsHighSurrogate(current) && !this.IsEnd()
            && char.IsLowSurrogate(this.input[this.position]))
        {
            result.Append(this.input[this.position]);
            this.position = this.position + 1;
        }
    }

    internal void ConsumeComments()
    {
        while (this.Peek(0) == '/' && this.Peek(1) == '*')
        {
            this.CheckCancellation();
            this.position = this.position + 2;
            bool terminated = false;
            while (!this.IsEnd())
            {
                this.CheckCancellation();
                if (this.Peek(0) == '*' && this.Peek(1) == '/')
                {
                    this.position = this.position + 2;
                    terminated = true;
                    break;
                }

                this.position = this.position + 1;
            }

            if (!terminated)
            {
                this.AddError();
            }
        }
    }

    internal bool StartsIdentifier(int start)
    {
        int first = this.PeekAt(start);
        int second = this.PeekAt(start + 1);
        if (first == '-')
        {
            return this.IsIdentifierStartCodePoint(second) || second == '-'
                || this.IsValidEscape(start + 1);
        }

        if (this.IsIdentifierStartCodePoint(first))
        {
            return true;
        }

        return first == '\\' && this.IsValidEscape(start);
    }

    internal bool StartsNumber(int start)
    {
        int first = this.PeekAt(start);
        int second = this.PeekAt(start + 1);
        int third = this.PeekAt(start + 2);
        if (first == '+' || first == '-')
        {
            return IsDigit(second) || second == '.' && IsDigit(third);
        }

        if (first == '.')
        {
            return IsDigit(second);
        }

        return IsDigit(first);
    }

    internal bool StartsExponent(int start)
    {
        int second = this.PeekAt(start + 1);
        if (IsDigit(second))
        {
            return true;
        }

        return (second == '+' || second == '-')
            && IsDigit(this.PeekAt(start + 2));
    }

    internal bool IsValidEscape(int start)
    {
        return this.PeekAt(start) == '\\'
            && this.PeekAt(start + 1) != '\n';
    }

    internal bool IsIdentifierStartCodePoint(int value)
    {
        return value >= 128 || value == '_' || value >= 'A' && value <= 'Z'
            || value >= 'a' && value <= 'z';
    }

    internal bool IsIdentifierCodePoint(int value)
    {
        return this.IsIdentifierStartCodePoint(value) || IsDigit(value)
            || value == '-';
    }

    internal bool StartsWith(string value)
    {
        if (this.position + value.Length > this.input.Length)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (this.input[this.position + index] != value[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal int Peek(int offset)
    {
        return this.PeekAt(this.position + offset);
    }

    internal int PeekAt(int index)
    {
        return index < 0 || index >= this.input.Length
            ? -1 : this.input[index];
    }

    internal bool IsEnd()
    {
        return this.position >= this.input.Length;
    }

    internal CssToken CreateToken(CssTokenKind kind, string value, string unit,
        double number, CssNumericType numericType, CssHashType hashType,
        int start)
    {
        string representation = this.input.Substring(start,
            this.position - start);
        return new CssToken(kind, value, unit, representation, "", number,
            numericType, hashType,
            this.lineMap.GetSpan(start, this.position));
    }

    internal CssToken CreateNumericToken(CssTokenKind kind, string unit,
        CssNumberData number, int start)
    {
        string representation = this.input.Substring(start,
            this.position - start);
        return new CssToken(kind, "", unit, representation,
            number.GetRepresentation(), number.GetValue(),
            number.GetNumericType(), CssHashType.Unrestricted,
            this.lineMap.GetSpan(start, this.position));
    }

    internal void AddError()
    {
        if (this.errorCount < int.MaxValue)
        {
            this.errorCount = this.errorCount + 1;
        }
    }

    internal static string Preprocess(string value, HtmlLineMap lineMap)
    {
        return Preprocess(value, lineMap, CancellationToken.None);
    }

    internal static string Preprocess(string value, HtmlLineMap lineMap,
        CancellationToken cancellationToken)
    {
        StringBuilder result = new StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            char current = value[index];
            if (current == '\r')
            {
                int next = index + 1;
                if (next < value.Length && value[next] == '\n')
                {
                    next = next + 1;
                }

                result.Append('\n');
                lineMap.AppendWithCharacterOffsets('\n', index, next);
                index = next;
            }
            else if (current == '\f')
            {
                result.Append('\n');
                lineMap.AppendWithCharacterOffsets('\n', index, index + 1);
                index = index + 1;
            }
            else if (char.IsHighSurrogate(current)
                && index + 1 < value.Length
                && char.IsLowSurrogate(value[index + 1]))
            {
                result.Append(current);
                lineMap.AppendWithCharacterOffsets(current, index, index + 1);
                result.Append(value[index + 1]);
                lineMap.AppendWithCharacterOffsets(value[index + 1], index + 1, index + 2);
                index = index + 2;
            }
            else if (current == '\0' || char.IsLowSurrogate(current)
                || char.IsHighSurrogate(current))
            {
                result.Append('\uFFFD');
                lineMap.AppendWithCharacterOffsets('\uFFFD', index, index + 1);
                index = index + 1;
            }
            else
            {
                result.Append(current);
                lineMap.AppendWithCharacterOffsets(current, index, index + 1);
                index = index + 1;
            }
        }

        return result.ToString();
    }

    internal void CheckCancellation()
    {
        this.cancellationToken.ThrowIfCancellationRequested();
    }

    internal static bool IsWhitespace(int value)
    {
        return value == '\n' || value == '\t' || value == ' ';
    }

    internal static bool IsDigit(int value)
    {
        return value >= '0' && value <= '9';
    }

    internal static bool IsHexDigit(int value)
    {
        return IsDigit(value) || value >= 'A' && value <= 'F'
            || value >= 'a' && value <= 'f';
    }

    internal static int GetHexValue(int value)
    {
        if (IsDigit(value))
        {
            return value - '0';
        }

        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        return value - 'a' + 10;
    }

    internal static bool IsNonPrintable(int value)
    {
        return value >= 0 && value <= 8 || value == 11
            || value >= 14 && value <= 31 || value == 127;
    }
}
