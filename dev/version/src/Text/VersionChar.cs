namespace version;

internal static class VersionChar
{
    internal const char EndOfInput = '\0';

    internal static bool IsAscii(char value)
    {
        return value <= '\x7f';
    }

    internal static bool IsDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal static bool IsLetter(char value)
    {
        return IsUppercaseLetter(value) || IsLowercaseLetter(value);
    }

    internal static bool IsUppercaseLetter(char value)
    {
        return value >= 'A' && value <= 'Z';
    }

    internal static bool IsLowercaseLetter(char value)
    {
        return value >= 'a' && value <= 'z';
    }

    internal static bool IsAlphaNumeric(char value)
    {
        return IsLetter(value) || IsDigit(value);
    }

    internal static bool IsHyphen(char value)
    {
        return value == '-';
    }

    internal static bool IsDot(char value)
    {
        return value == '.';
    }

    internal static bool IsPlus(char value)
    {
        return value == '+';
    }

    internal static bool IsUnderscore(char value)
    {
        return value == '_';
    }

    internal static bool IsStar(char value)
    {
        return value == '*';
    }

    internal static bool IsX(char value)
    {
        return value == 'x' || value == 'X';
    }

    internal static bool IsV(char value)
    {
        return value == 'v' || value == 'V';
    }

    internal static bool IsOpenBracket(char value)
    {
        return value == '[';
    }

    internal static bool IsCloseBracket(char value)
    {
        return value == ']';
    }

    internal static bool IsOpenParenthesis(char value)
    {
        return value == '(';
    }

    internal static bool IsCloseParenthesis(char value)
    {
        return value == ')';
    }

    internal static bool IsComma(char value)
    {
        return value == ',';
    }

    internal static bool IsPipe(char value)
    {
        return value == '|';
    }

    internal static bool IsComparisonOperator(char value)
    {
        return value == '<' || value == '>' || value == '=' || value == '!';
    }

    internal static bool IsWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\r' || value == '\n';
    }

    internal static bool IsSeparator(char value)
    {
        return IsDot(value) || IsHyphen(value) || IsPlus(value) || IsUnderscore(value);
    }

    internal static bool IsRangeDelimiter(char value)
    {
        return IsOpenBracket(value)
            || IsCloseBracket(value)
            || IsOpenParenthesis(value)
            || IsCloseParenthesis(value)
            || IsComma(value);
    }
}
