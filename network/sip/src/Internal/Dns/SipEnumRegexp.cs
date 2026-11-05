namespace sip;

internal static class SipEnumRegexp
{
    const int MaximumExpressionLength = 1024;
    const int MaximumPatternLength = 256;

    internal static SipEnumRegexpResult Apply(string? expression, string? input,
        int maximumOutputLength)
    {
        if (expression == null || input == null || expression.Length < 4
            || expression.Length > MaximumExpressionLength || maximumOutputLength < 0)
        {
            return CreateError("The ENUM regular expression inputs are invalid.");
        }
        int delimiter = expression[0];
        if (IsAlphaNumeric(delimiter) || delimiter == '\\')
        {
            return CreateError("The ENUM regular expression delimiter is invalid.");
        }
        string[] fields = SplitExpression(expression, delimiter);
        if (fields.Length != 3 || fields[0].Length < 2
            || fields[0].Length > MaximumPatternLength
            || fields[0][0] != '^' || fields[0][fields[0].Length - 1] != '$'
            || fields[2].Length > 1
            || fields[2].Length == 1 && fields[2][0] != 'i' && fields[2][0] != 'I')
        {
            return CreateError("The ENUM regular expression is outside the supported bounded form.");
        }

        string pattern = fields[0].Substring(1, fields[0].Length - 2);
        string? capture = null;
        bool matched;
        int groupStart = pattern.IndexOf('(');
        if (groupStart >= 0)
        {
            int groupEnd = pattern.IndexOf(')', groupStart + 1);
            if (groupEnd < 0 || pattern.IndexOf('(', groupStart + 1) >= 0
                || pattern.IndexOf(')', groupEnd + 1) >= 0)
            {
                return CreateError("The ENUM regular expression has unsupported capture groups.");
            }
            string group = pattern.Substring(groupStart + 1, groupEnd - groupStart - 1);
            if (group != ".*" && group != "[0-9]+")
            {
                return CreateError("The ENUM capture group is not a bounded supported form.");
            }
            string? prefix = DecodeLiteral(pattern.Substring(0, groupStart), delimiter);
            string? suffix = DecodeLiteral(pattern.Substring(groupEnd + 1), delimiter);
            if (prefix == null || suffix == null)
            {
                return CreateError("The ENUM regular expression contains unsupported operators.");
            }
            bool ignoreCase = fields[2].Length == 1;
            matched = StartsWith(input, prefix, ignoreCase) && EndsWith(input, suffix, ignoreCase)
                && input.Length >= prefix.Length + suffix.Length;
            if (matched)
            {
                capture = input.Substring(prefix.Length,
                    input.Length - prefix.Length - suffix.Length);
                if (group == "[0-9]+" && !IsDigits(capture))
                {
                    matched = false;
                }
            }
        }
        else if (pattern == ".*")
        {
            matched = true;
        }
        else
        {
            string? literal = DecodeLiteral(pattern, delimiter);
            if (literal == null)
            {
                return CreateError("The ENUM regular expression contains unsupported operators.");
            }
            matched = fields[2].Length == 1
                ? SipAscii.EqualsIgnoreCase(literal, input) : literal == input;
        }
        if (!matched)
        {
            return CreateError("The ENUM regular expression does not match the number.");
        }
        return ExpandReplacement(fields[1], capture, delimiter, maximumOutputLength);
    }

    static string[] SplitExpression(string expression, int delimiter)
    {
        System.Collections.Generic.List<string> fields = new System.Collections.Generic.List<string>();
        System.Text.StringBuilder current = new System.Text.StringBuilder();
        int index = 1;
        while (index < expression.Length)
        {
            int character = expression[index];
            if (character == '\\' && index + 1 < expression.Length)
            {
                current.Append((char)character);
                current.Append(expression[index + 1]);
                index = index + 2;
            }
            else if (character == delimiter && fields.Count < 2)
            {
                fields.Add(current.ToString());
                current.Clear();
                index = index + 1;
            }
            else if (character == delimiter && fields.Count == 2)
            {
                return new string[0];
            }
            else
            {
                current.Append((char)character);
                index = index + 1;
            }
        }
        fields.Add(current.ToString());
        return fields.ToArray();
    }

    static string? DecodeLiteral(string pattern, int delimiter)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < pattern.Length)
        {
            int character = pattern[index];
            if (character == '\\')
            {
                if (index + 1 >= pattern.Length)
                {
                    return null;
                }
                builder.Append(pattern[index + 1]);
                index = index + 2;
            }
            else
            {
                if (character == '.' || character == '*' || character == '+'
                    || character == '?' || character == '[' || character == ']'
                    || character == '{' || character == '}' || character == '|'
                    || character == '(' || character == ')' || character == delimiter)
                {
                    return null;
                }
                builder.Append((char)character);
                index = index + 1;
            }
        }
        return builder.ToString();
    }

    static SipEnumRegexpResult ExpandReplacement(string replacement, string? capture,
        int delimiter, int maximumOutputLength)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < replacement.Length)
        {
            int character = replacement[index];
            if (character == '\\')
            {
                if (index + 1 >= replacement.Length)
                {
                    return CreateError("The ENUM replacement ends with an escape.");
                }
                int escaped = replacement[index + 1];
                if (escaped == '1')
                {
                    if (capture == null)
                    {
                        return CreateError("The ENUM replacement references a missing capture.");
                    }
                    builder.Append(capture);
                }
                else
                {
                    builder.Append((char)escaped);
                }
                index = index + 2;
            }
            else if (character == '$' && index + 1 < replacement.Length
                && replacement[index + 1] == '1')
            {
                if (capture == null)
                {
                    return CreateError("The ENUM replacement references a missing capture.");
                }
                builder.Append(capture);
                index = index + 2;
            }
            else
            {
                if (character == delimiter)
                {
                    return CreateError("The ENUM replacement contains an unescaped delimiter.");
                }
                builder.Append((char)character);
                index = index + 1;
            }
            if (builder.Length > maximumOutputLength)
            {
                return CreateError("The ENUM replacement exceeds the configured URI limit.");
            }
        }
        SipEnumRegexpResult result = new SipEnumRegexpResult();
        result.Success = true;
        result.Output = builder.ToString();
        return result;
    }

    static bool StartsWith(string value, string prefix, bool ignoreCase)
    {
        if (value.Length < prefix.Length)
        {
            return false;
        }
        string beginning = value.Substring(0, prefix.Length);
        return ignoreCase ? SipAscii.EqualsIgnoreCase(beginning, prefix) : beginning == prefix;
    }

    static bool EndsWith(string value, string suffix, bool ignoreCase)
    {
        if (value.Length < suffix.Length)
        {
            return false;
        }
        string ending = value.Substring(value.Length - suffix.Length);
        return ignoreCase ? SipAscii.EqualsIgnoreCase(ending, suffix) : ending == suffix;
    }

    static bool IsDigits(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool IsAlphaNumeric(int character)
    {
        return character >= '0' && character <= '9'
            || character >= 'A' && character <= 'Z'
            || character >= 'a' && character <= 'z';
    }

    static SipEnumRegexpResult CreateError(string message)
    {
        SipEnumRegexpResult result = new SipEnumRegexpResult();
        result.ErrorMessage = message;
        return result;
    }
}
