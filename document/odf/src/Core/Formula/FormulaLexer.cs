using System.Collections.Generic;

namespace odf;

internal static class FormulaLexer
{
    internal static OdfFormulaToken[] Tokenize(string formula)
    {
        if (formula == null)
        {
            throw new OdfFormulaException("A formula is required for tokenization.");
        }
        global::System.ReadOnlySpan<char> input =
            global::System.MemoryExtensions.AsSpan(formula);
        List<OdfFormulaToken> tokens = new List<OdfFormulaToken>();
        int index = 0;
        if (StartsWithFormulaPrefix(input))
        {
            int end = ReadFormulaPrefixEnd(input);
            AddToken(
                tokens,
                input,
                OdfFormulaTokenKind.Prefix,
                0,
                end);
            index = end;
        }
        while (index < input.Length)
        {
            int start = index;
            char current = input[index];
            OdfFormulaTokenKind kind = OdfFormulaTokenKind.Unknown;
            if (global::System.Char.IsWhiteSpace(current))
            {
                index = ReadWhitespaceEnd(input, index);
                kind = OdfFormulaTokenKind.Whitespace;
            }
            else if (global::System.Char.IsDigit(current)
                || (current == '.'
                    && index + 1 < input.Length
                    && global::System.Char.IsDigit(input[index + 1])))
            {
                index = ReadNumberEnd(input, index);
                if (index < input.Length && input[index] == '%')
                {
                    index = index + 1;
                    kind = OdfFormulaTokenKind.Percentage;
                }
                else
                {
                    kind = OdfFormulaTokenKind.Number;
                }
            }
            else if (current == '"')
            {
                index = ReadStringEnd(input, index);
                kind = index - start >= 2
                    && index <= input.Length
                    && input[index - 1] == '"'
                    ? OdfFormulaTokenKind.String
                    : OdfFormulaTokenKind.Unknown;
            }
            else if (current == '[')
            {
                index = ReadReferenceEnd(input, index);
                kind = ClassifyReference(input, start, index);
            }
            else if (current == '#')
            {
                index = ReadErrorEnd(input, index);
                kind = IsValidError(input, start, index)
                    ? OdfFormulaTokenKind.Error
                    : OdfFormulaTokenKind.Unknown;
            }
            else if (current == '(')
            {
                index = index + 1;
                kind = OdfFormulaTokenKind.LeftParenthesis;
            }
            else if (current == ')')
            {
                index = index + 1;
                kind = OdfFormulaTokenKind.RightParenthesis;
            }
            else if (current == '{')
            {
                index = index + 1;
                kind = OdfFormulaTokenKind.LeftBrace;
            }
            else if (current == '}')
            {
                index = index + 1;
                kind = OdfFormulaTokenKind.RightBrace;
            }
            else if (current == ';')
            {
                index = index + 1;
                kind = OdfFormulaTokenKind.Separator;
            }
            else if (current == '|')
            {
                index = index + 1;
                kind = OdfFormulaTokenKind.ArrayRowSeparator;
            }
            else if (current == '\'')
            {
                index = ReadQuotedNameEnd(input, index);
                kind = ClassifyQuotedName(input, start, index);
            }
            else if (current == '$'
                && index + 1 < input.Length
                && input[index + 1] == '$')
            {
                index = ReadExplicitNamedEnd(input, index);
                kind = index > start + 2
                    ? OdfFormulaTokenKind.NamedExpression
                    : OdfFormulaTokenKind.Unknown;
            }
            else if (IsNameStart(current))
            {
                index = ReadNameEnd(input, index);
                kind = ClassifyName(input, start, index);
            }
            else if (IsOperatorStart(current))
            {
                index = ReadOperatorEnd(input, index);
                kind = OdfFormulaTokenKind.Operator;
            }
            else
            {
                index = index + 1;
            }
            AddToken(tokens, input, kind, start, index);
        }
        AddToken(
            tokens,
            input,
            OdfFormulaTokenKind.End,
            input.Length,
            input.Length);
        return tokens.ToArray();
    }

    static bool StartsWithFormulaPrefix(
        global::System.ReadOnlySpan<char> input)
    {
        if (input.Length == 0)
        {
            return false;
        }
        if (input[0] == '=')
        {
            return true;
        }
        int index = 0;
        if (!IsNameStart(input[index]))
        {
            return false;
        }
        index = ReadNameEnd(input, index);
        return index + 1 < input.Length
            && input[index] == ':'
            && input[index + 1] == '=';
    }

    static int ReadFormulaPrefixEnd(
        global::System.ReadOnlySpan<char> input)
    {
        if (input[0] == '=')
        {
            return input.Length > 1 && input[1] == '=' ? 2 : 1;
        }
        int index = ReadNameEnd(input, 0);
        int end = index + 2;
        if (end < input.Length && input[end] == '=')
        {
            end = end + 1;
        }
        return end;
    }

    static int ReadWhitespaceEnd(
        global::System.ReadOnlySpan<char> input,
        int index)
    {
        while (index < input.Length
            && global::System.Char.IsWhiteSpace(input[index]))
        {
            index = index + 1;
        }
        return index;
    }

    static int ReadNumberEnd(
        global::System.ReadOnlySpan<char> input,
        int index)
    {
        if (input[index] == '.')
        {
            index = index + 1;
        }
        while (index < input.Length
            && global::System.Char.IsDigit(input[index]))
        {
            index = index + 1;
        }
        if (index < input.Length && input[index] == '.')
        {
            int fraction = index + 1;
            if (fraction < input.Length
                && global::System.Char.IsDigit(input[fraction]))
            {
                index = fraction + 1;
                while (index < input.Length
                    && global::System.Char.IsDigit(input[index]))
                {
                    index = index + 1;
                }
            }
        }
        if (index < input.Length
            && (input[index] == 'E' || input[index] == 'e'))
        {
            int exponent = index + 1;
            if (exponent < input.Length
                && (input[exponent] == '+' || input[exponent] == '-'))
            {
                exponent = exponent + 1;
            }
            int digits = exponent;
            while (exponent < input.Length
                && global::System.Char.IsDigit(input[exponent]))
            {
                exponent = exponent + 1;
            }
            if (exponent > digits)
            {
                index = exponent;
            }
        }
        return index;
    }

    static int ReadStringEnd(
        global::System.ReadOnlySpan<char> input,
        int index)
    {
        index = index + 1;
        while (index < input.Length)
        {
            if (input[index] == '"')
            {
                if (index + 1 < input.Length && input[index + 1] == '"')
                {
                    index = index + 2;
                }
                else
                {
                    return index + 1;
                }
            }
            else
            {
                index = index + 1;
            }
        }
        return input.Length;
    }

    static int ReadReferenceEnd(
        global::System.ReadOnlySpan<char> input,
        int index)
    {
        bool quoted = false;
        index = index + 1;
        while (index < input.Length)
        {
            char current = input[index];
            if (current == '\'')
            {
                if (quoted
                    && index + 1 < input.Length
                    && input[index + 1] == '\'')
                {
                    index = index + 2;
                }
                else
                {
                    quoted = !quoted;
                    index = index + 1;
                }
            }
            else if (current == ']' && !quoted)
            {
                return index + 1;
            }
            else
            {
                index = index + 1;
            }
        }
        return input.Length;
    }

    static OdfFormulaTokenKind ClassifyReference(
        global::System.ReadOnlySpan<char> input,
        int start,
        int end)
    {
        if (end <= start + 2
            || end > input.Length
            || input[end - 1] != ']')
        {
            return OdfFormulaTokenKind.Unknown;
        }
        if (ReferenceHasExternalSource(input, start + 1, end - 1))
        {
            return OdfFormulaTokenKind.ExternalReference;
        }
        if (ReferenceHasRangeColon(input, start + 1, end - 1))
        {
            return OdfFormulaTokenKind.RangeReference;
        }
        return OdfFormulaTokenKind.CellReference;
    }

    static bool ReferenceHasExternalSource(
        global::System.ReadOnlySpan<char> input,
        int start,
        int end)
    {
        bool quoted = false;
        int index = start;
        while (index < end)
        {
            if (input[index] == '\'')
            {
                if (quoted && index + 1 < end && input[index + 1] == '\'')
                {
                    index = index + 2;
                }
                else
                {
                    quoted = !quoted;
                    index = index + 1;
                    if (!quoted && index < end && input[index] == '#')
                    {
                        return true;
                    }
                }
            }
            else
            {
                index = index + 1;
            }
        }
        return false;
    }

    static bool ReferenceHasRangeColon(
        global::System.ReadOnlySpan<char> input,
        int start,
        int end)
    {
        bool quoted = false;
        int index = start;
        while (index < end)
        {
            if (input[index] == '\'')
            {
                if (quoted && index + 1 < end && input[index + 1] == '\'')
                {
                    index = index + 2;
                }
                else
                {
                    quoted = !quoted;
                    index = index + 1;
                }
            }
            else
            {
                if (input[index] == ':' && !quoted)
                {
                    return true;
                }
                index = index + 1;
            }
        }
        return false;
    }

    static int ReadErrorEnd(
        global::System.ReadOnlySpan<char> input,
        int index)
    {
        index = index + 1;
        while (index < input.Length)
        {
            char current = input[index];
            if ((current >= 'A' && current <= 'Z')
                || (current >= '0' && current <= '9')
                || current == '/'
                || current == '!'
                || current == '?')
            {
                index = index + 1;
            }
            else
            {
                return index;
            }
        }
        return index;
    }

    static bool IsValidError(
        global::System.ReadOnlySpan<char> input,
        int start,
        int end)
    {
        if (end <= start + 2)
        {
            return false;
        }
        int index = start + 1;
        while (index < end
            && ((input[index] >= 'A' && input[index] <= 'Z')
                || (input[index] >= '0' && input[index] <= '9')))
        {
            index = index + 1;
        }
        if (index == start + 1 || index >= end)
        {
            return false;
        }
        if (input[index] == '!' || input[index] == '?')
        {
            return index + 1 == end;
        }
        if (input[index] != '/' || index + 1 >= end)
        {
            return false;
        }
        index = index + 1;
        if (input[index] >= 'A' && input[index] <= 'Z')
        {
            return index + 1 == end;
        }
        return input[index] >= '0'
            && input[index] <= '9'
            && index + 1 < end
            && (input[index + 1] == '!' || input[index + 1] == '?')
            && index + 2 == end;
    }

    static int ReadQuotedNameEnd(
        global::System.ReadOnlySpan<char> input,
        int index)
    {
        index = ReadSingleQuotedEnd(input, index);
        if (index < input.Length && input[index] == '#')
        {
            index = index + 1;
            if (index + 1 < input.Length
                && input[index] == '$'
                && input[index + 1] == '$')
            {
                index = ReadExplicitNamedEnd(input, index);
            }
            else if (index < input.Length && IsNameStart(input[index]))
            {
                index = ReadNameEnd(input, index);
            }
            return index;
        }
        if (index < input.Length && input[index] == '.')
        {
            index = index + 1;
            if (index + 1 < input.Length
                && input[index] == '$'
                && input[index + 1] == '$')
            {
                return ReadExplicitNamedEnd(input, index);
            }
            if (index < input.Length && IsNameStart(input[index]))
            {
                return ReadNameEnd(input, index);
            }
        }
        return index;
    }

    static int ReadSingleQuotedEnd(
        global::System.ReadOnlySpan<char> input,
        int index)
    {
        index = index + 1;
        while (index < input.Length)
        {
            if (input[index] == '\'')
            {
                if (index + 1 < input.Length && input[index + 1] == '\'')
                {
                    index = index + 2;
                }
                else
                {
                    return index + 1;
                }
            }
            else
            {
                index = index + 1;
            }
        }
        return index;
    }

    static OdfFormulaTokenKind ClassifyQuotedName(
        global::System.ReadOnlySpan<char> input,
        int start,
        int end)
    {
        if (end <= start + 1 || input[end - 1] == '\'')
        {
            return OdfFormulaTokenKind.Identifier;
        }
        int index = start + 1;
        while (index < end)
        {
            if (input[index] == '\'')
            {
                if (index + 1 < end && input[index + 1] == '\'')
                {
                    index = index + 2;
                }
                else
                {
                    index = index + 1;
                    return index < end
                        ? (input[index] == '#'
                            ? OdfFormulaTokenKind.ExternalReference
                            : OdfFormulaTokenKind.NamedExpression)
                        : OdfFormulaTokenKind.Identifier;
                }
            }
            else
            {
                index = index + 1;
            }
        }
        return OdfFormulaTokenKind.Unknown;
    }

    static int ReadExplicitNamedEnd(
        global::System.ReadOnlySpan<char> input,
        int index)
    {
        index = index + 2;
        if (index < input.Length && input[index] == '\'')
        {
            return ReadSingleQuotedEnd(input, index);
        }
        if (index < input.Length && IsNameStart(input[index]))
        {
            return ReadNameEnd(input, index);
        }
        return index;
    }

    static int ReadNameEnd(
        global::System.ReadOnlySpan<char> input,
        int index)
    {
        index = index + 1;
        while (index < input.Length && IsNamePart(input[index]))
        {
            index = index + 1;
        }
        return index;
    }

    static OdfFormulaTokenKind ClassifyName(
        global::System.ReadOnlySpan<char> input,
        int start,
        int end)
    {
        int next = end;
        while (next < input.Length
            && global::System.Char.IsWhiteSpace(input[next]))
        {
            next = next + 1;
        }
        if (next < input.Length && input[next] == '(')
        {
            return OdfFormulaTokenKind.Function;
        }
        if (TextEqualsIgnoreCase(input, start, end, "TRUE")
            || TextEqualsIgnoreCase(input, start, end, "FALSE"))
        {
            return OdfFormulaTokenKind.Boolean;
        }
        return OdfFormulaTokenKind.NamedExpression;
    }

    static bool TextEqualsIgnoreCase(
        global::System.ReadOnlySpan<char> input,
        int start,
        int end,
        string expected)
    {
        if (end - start != expected.Length)
        {
            return false;
        }
        int index = 0;
        while (index < expected.Length)
        {
            if (global::System.Char.ToUpperInvariant(input[start + index])
                != expected[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool IsNameStart(char value)
    {
        return global::System.Char.IsLetter(value);
    }

    static bool IsNamePart(char value)
    {
        return global::System.Char.IsLetterOrDigit(value)
            || value == '_'
            || value == '.'
            || IsCombiningCharacter(value);
    }

    static bool IsCombiningCharacter(char value)
    {
        global::System.Globalization.UnicodeCategory category =
            global::System.Globalization.CharUnicodeInfo.GetUnicodeCategory(
                value);
        return category
                == global::System.Globalization.UnicodeCategory.NonSpacingMark
            || category
                == global::System.Globalization.UnicodeCategory.SpacingCombiningMark
            || category
                == global::System.Globalization.UnicodeCategory.EnclosingMark;
    }

    static bool IsOperatorStart(char value)
    {
        return value == '+'
            || value == '-'
            || value == '*'
            || value == '/'
            || value == '^'
            || value == '&'
            || value == '='
            || value == '<'
            || value == '>'
            || value == '%'
            || value == '!'
            || value == '~'
            || value == ':';
    }

    static int ReadOperatorEnd(
        global::System.ReadOnlySpan<char> input,
        int index)
    {
        char current = input[index];
        if (index + 1 < input.Length)
        {
            char next = input[index + 1];
            if ((current == '<' && (next == '>' || next == '='))
                || (current == '>' && next == '=')
                || (current == '!' && next == '!'))
            {
                return index + 2;
            }
        }
        return index + 1;
    }

    static void AddToken(
        List<OdfFormulaToken> tokens,
        global::System.ReadOnlySpan<char> input,
        OdfFormulaTokenKind kind,
        int start,
        int end)
    {
        OdfFormulaToken token = new OdfFormulaToken();
        token.Kind = kind;
        token.Start = start;
        token.Length = end - start;
        token.Text = input.Slice(start, token.Length).ToString();
        tokens.Add(token);
    }
}
