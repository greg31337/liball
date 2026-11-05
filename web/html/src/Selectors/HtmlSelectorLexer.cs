using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace html;

internal class HtmlSelectorLexer
{
    internal string input;
    internal HtmlLineMap lineMap;
    internal int position;
    internal List<HtmlDiagnostic> diagnostics;

    internal HtmlSelectorLexer(string selector)
    {
        if (selector == null)
        {
            throw new ArgumentNullException("selector");
        }

        this.lineMap = HtmlLineMap.Create();
        this.input = Preprocess(selector, this.lineMap);
        this.position = 0;
        this.diagnostics = new List<HtmlDiagnostic>();
    }

    internal HtmlSelectorToken Read()
    {
        this.ConsumeComments();
        int start = this.position;
        if (this.IsEnd())
        {
            return this.CreateToken(HtmlSelectorTokenKind.End, "", "", 0,
                false, false, start);
        }

        char value = this.input[this.position];
        if (IsWhitespace(value))
        {
            this.position = this.position + 1;
            while (!this.IsEnd()
                && IsWhitespace(this.input[this.position]))
            {
                this.position = this.position + 1;
            }

            return this.CreateToken(HtmlSelectorTokenKind.Whitespace, "", "",
                0, false, false, start);
        }

        if (value == '\'' || value == '"')
        {
            this.position = this.position + 1;
            return this.ConsumeString(start, value);
        }

        if (value == '#')
        {
            this.position = this.position + 1;
            if (this.IsIdentCodePoint(this.Peek(0))
                || this.IsValidEscape(this.position))
            {
                bool idHash = this.StartsIdentifier(this.position);
                string name = this.ConsumeIdentifier();
                return this.CreateToken(HtmlSelectorTokenKind.Hash, name, "",
                    0, false, idHash, start);
            }

            return this.CreateToken(HtmlSelectorTokenKind.Delimiter, "#", "",
                0, false, false, start);
        }

        if (this.StartsNumber(this.position))
        {
            return this.ConsumeNumeric(start);
        }

        if (this.StartsIdentifier(this.position))
        {
            string name = this.ConsumeIdentifier();
            if (this.Peek(0) == '(')
            {
                this.position = this.position + 1;
                return this.CreateToken(HtmlSelectorTokenKind.Function, name,
                    "", 0, false, false, start);
            }

            return this.CreateToken(HtmlSelectorTokenKind.Identifier, name,
                "", 0, false, false, start);
        }

        this.position = this.position + 1;
        if (value == '\\' && this.Peek(0) == '\n')
        {
            this.AddError("A CSS escape cannot be followed by a newline.",
                start, this.position);
        }

        return this.CreateToken(HtmlSelectorTokenKind.Delimiter,
            value.ToString(), "", 0, false, false, start);
    }

    internal HtmlSelectorToken[] Tokenize()
    {
        List<HtmlSelectorToken> result = new List<HtmlSelectorToken>();
        bool complete = false;
        while (!complete)
        {
            HtmlSelectorToken token = this.Read();
            result.Add(token);
            complete = token.GetKind() == HtmlSelectorTokenKind.End;
        }

        return result.ToArray();
    }

    internal HtmlDiagnostic[] GetDiagnostics()
    {
        return this.diagnostics.ToArray();
    }

    internal int GetPosition()
    {
        return this.position;
    }

    internal HtmlSelectorToken ConsumeNumeric(int start)
    {
        HtmlSelectorNumberData number = this.ConsumeNumber();
        if (this.StartsIdentifier(this.position))
        {
            string unit = this.ConsumeIdentifier();
            return this.CreateNumericToken(HtmlSelectorTokenKind.Dimension,
                unit, number, number.IsInteger(), start);
        }

        if (this.Peek(0) == '%')
        {
            this.position = this.position + 1;
            return this.CreateNumericToken(HtmlSelectorTokenKind.Percentage,
                "", number, false, start);
        }

        return this.CreateNumericToken(HtmlSelectorTokenKind.Number, "",
            number, number.IsInteger(), start);
    }

    internal HtmlSelectorNumberData ConsumeNumber()
    {
        int start = this.position;
        bool integer = true;
        int next = this.Peek(0);
        if (next == '+' || next == '-')
        {
            this.position = this.position + 1;
        }

        while (IsDigit(this.Peek(0)))
        {
            this.position = this.position + 1;
        }

        if (this.Peek(0) == '.' && IsDigit(this.Peek(1)))
        {
            integer = false;
            this.position = this.position + 2;
            while (IsDigit(this.Peek(0)))
            {
                this.position = this.position + 1;
            }
        }

        if ((this.Peek(0) == 'e' || this.Peek(0) == 'E')
            && this.StartsExponent(this.position))
        {
            integer = false;
            this.position = this.position + 1;
            if (this.Peek(0) == '+' || this.Peek(0) == '-')
            {
                this.position = this.position + 1;
            }

            while (IsDigit(this.Peek(0)))
            {
                this.position = this.position + 1;
            }
        }

        string representation = this.input.Substring(start,
            this.position - start);
        double value = double.Parse(representation, NumberStyles.Float,
            CultureInfo.InvariantCulture);
        return new HtmlSelectorNumberData(representation, value, integer);
    }

    internal HtmlSelectorToken ConsumeString(int start, char ending)
    {
        StringBuilder value = new StringBuilder();
        while (!this.IsEnd())
        {
            char current = this.input[this.position];
            if (current == ending)
            {
                this.position = this.position + 1;
                return this.CreateToken(HtmlSelectorTokenKind.String,
                    value.ToString(), "", 0, false, false, start);
            }

            if (current == '\n')
            {
                this.AddError("A CSS string cannot contain an unescaped newline.",
                    start, this.position);
                return this.CreateToken(HtmlSelectorTokenKind.BadString,
                    value.ToString(), "", 0, false, false, start);
            }

            if (current == '\\')
            {
                this.position = this.position + 1;
                if (this.IsEnd())
                {
                    continue;
                }

                if (this.input[this.position] == '\n')
                {
                    this.position = this.position + 1;
                    continue;
                }

                this.ConsumeEscapedCodePoint(value, start);
                continue;
            }

            this.AppendCurrentCodePoint(value);
        }

        this.AddError("A CSS string was not terminated.", start,
            this.position);
        return this.CreateToken(HtmlSelectorTokenKind.String,
            value.ToString(), "", 0, false, false, start);
    }

    internal string ConsumeIdentifier()
    {
        int start = this.position;
        StringBuilder result = new StringBuilder();
        while (!this.IsEnd())
        {
            if (this.IsIdentCodePoint(this.Peek(0)))
            {
                this.AppendCurrentCodePoint(result);
                continue;
            }

            if (this.IsValidEscape(this.position))
            {
                this.position = this.position + 1;
                this.ConsumeEscapedCodePoint(result, start);
                continue;
            }

            break;
        }

        return result.ToString();
    }

    internal void ConsumeEscapedCodePoint(StringBuilder result, int tokenStart)
    {
        if (this.IsEnd())
        {
            result.Append('\uFFFD');
            this.AddError("A CSS escape ended at end of input.", tokenStart,
                this.position);
            return;
        }

        if (IsHexDigit(this.Peek(0)))
        {
            int value = 0;
            int count = 0;
            while (count < 6 && IsHexDigit(this.Peek(0)))
            {
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
            int start = this.position;
            this.position = this.position + 2;
            bool terminated = false;
            while (!this.IsEnd() && !terminated)
            {
                if (this.Peek(0) == '*' && this.Peek(1) == '/')
                {
                    this.position = this.position + 2;
                    terminated = true;
                }
                else
                {
                    this.position = this.position + 1;
                }
            }

            if (!terminated)
            {
                this.AddError("A CSS comment was not terminated.", start,
                    this.position);
            }
        }
    }

    internal bool StartsIdentifier(int start)
    {
        int first = this.PeekAt(start);
        int second = this.PeekAt(start + 1);
        if (first == '-')
        {
            return this.IsIdentStartCodePoint(second) || second == '-'
                || this.IsValidEscape(start + 1);
        }

        if (this.IsIdentStartCodePoint(first))
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

    internal bool IsIdentStartCodePoint(int value)
    {
        return value >= 128 || value == '_' || value >= 'A' && value <= 'Z'
            || value >= 'a' && value <= 'z';
    }

    internal bool IsIdentCodePoint(int value)
    {
        return this.IsIdentStartCodePoint(value) || IsDigit(value)
            || value == '-';
    }

    internal int Peek(int offset)
    {
        return this.PeekAt(this.position + offset);
    }

    internal int PeekAt(int index)
    {
        return index < 0 || index >= this.input.Length
            ? -1
            : this.input[index];
    }

    internal bool IsEnd()
    {
        return this.position == this.input.Length;
    }

    internal HtmlSelectorToken CreateToken(HtmlSelectorTokenKind kind, string value,
        string unit, double number, bool integer, bool idHash, int start)
    {
        string representation = this.input.Substring(start,
            this.position - start);
        return new HtmlSelectorToken(kind, value, unit, representation, "",
            number, integer, idHash,
            this.lineMap.GetSpan(start, this.position));
    }

    internal HtmlSelectorToken CreateNumericToken(HtmlSelectorTokenKind kind,
        string unit, HtmlSelectorNumberData number, bool integer, int start)
    {
        string representation = this.input.Substring(start,
            this.position - start);
        return new HtmlSelectorToken(kind, "", unit, representation,
            number.GetRepresentation(), number.GetValue(), integer, false,
            this.lineMap.GetSpan(start, this.position));
    }

    internal void AddError(string message, int start, int end)
    {
        this.diagnostics.Add(HtmlDiagnostic.Create(
            HtmlDiagnosticCode.SelectorInvalidSyntax,
            HtmlDiagnosticSeverity.Error, message,
            this.lineMap.GetSpan(start, end)));
    }

    internal static string Preprocess(string value, HtmlLineMap lineMap)
    {
        StringBuilder result = new StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
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
}
