using System.Text;

namespace odf;

internal static class OdfMediaTypeRules
{
    internal static OdfMediaTypeValue ParseMediaType(string? value)
    {
        OdfMediaTypeValue result = new OdfMediaTypeValue();
        if (value == null)
        {
            return result;
        }
        result.LexicalValue = value;
        if (value.Length == 0)
        {
            return result;
        }

        int index = 0;
        int typeStart = index;
        while (index < value.Length && value[index] != '/')
        {
            if (!IsRestrictedNameCharacter(value[index]))
            {
                return result;
            }
            index = index + 1;
        }
        if (index == value.Length || !IsRestrictedName(value, typeStart, index))
        {
            return result;
        }
        result.Type = value.Substring(typeStart, index - typeStart).ToLowerInvariant();
        index = index + 1;

        int subtypeStart = index;
        while (index < value.Length && value[index] != ';' && !IsWhitespace(value[index]))
        {
            if (!IsRestrictedNameCharacter(value[index]))
            {
                return result;
            }
            index = index + 1;
        }
        if (!IsRestrictedName(value, subtypeStart, index))
        {
            return result;
        }
        result.Subtype = value.Substring(subtypeStart, index - subtypeStart).ToLowerInvariant();
        index = SkipWhitespace(value, index);

        while (index < value.Length)
        {
            if (value[index] != ';')
            {
                return result;
            }
            index = SkipWhitespace(value, index + 1);
            int nameStart = index;
            while (index < value.Length && value[index] != '=' && !IsWhitespace(value[index]))
            {
                if (!IsTokenCharacter(value[index]))
                {
                    return result;
                }
                index = index + 1;
            }
            if (index == nameStart || index - nameStart > 127)
            {
                return result;
            }
            string name = value.Substring(nameStart, index - nameStart).ToLowerInvariant();
            if (HasParameter(result, name))
            {
                return result;
            }
            index = SkipWhitespace(value, index);
            if (index == value.Length || value[index] != '=')
            {
                return result;
            }
            index = SkipWhitespace(value, index + 1);
            if (index == value.Length)
            {
                return result;
            }

            string parameterValue;
            if (value[index] == '"')
            {
                index = index + 1;
                StringBuilder decoded = new StringBuilder();
                bool closed = false;
                while (index < value.Length)
                {
                    char character = value[index];
                    if (character == '"')
                    {
                        closed = true;
                        index = index + 1;
                        break;
                    }
                    if (character == '\\')
                    {
                        index = index + 1;
                        if (index == value.Length || !IsQuotedCharacter(value[index]))
                        {
                            return result;
                        }
                        decoded.Append(value[index]);
                        index = index + 1;
                        continue;
                    }
                    if (!IsQuotedCharacter(character))
                    {
                        return result;
                    }
                    decoded.Append(character);
                    index = index + 1;
                }
                if (!closed)
                {
                    return result;
                }
                parameterValue = decoded.ToString();
            }
            else
            {
                int parameterStart = index;
                while (index < value.Length && value[index] != ';' && !IsWhitespace(value[index]))
                {
                    if (!IsTokenCharacter(value[index]))
                    {
                        return result;
                    }
                    index = index + 1;
                }
                if (index == parameterStart)
                {
                    return result;
                }
                parameterValue = value.Substring(parameterStart, index - parameterStart);
            }

            result.ParameterNames.Add(name);
            result.ParameterValues.Add(parameterValue);
            index = SkipWhitespace(value, index);
        }

        result.IsValid = true;
        return result;
    }

    internal static string FormatMediaType(OdfMediaTypeValue? value)
    {
        if (value == null || !value.IsValid)
        {
            return "";
        }
        StringBuilder result = new StringBuilder();
        result.Append(value.Type.ToLowerInvariant());
        result.Append('/');
        result.Append(value.Subtype.ToLowerInvariant());
        int index = 0;
        while (index < value.ParameterNames.Count && index < value.ParameterValues.Count)
        {
            result.Append("; ");
            result.Append(value.ParameterNames[index].ToLowerInvariant());
            result.Append('=');
            string parameterValue = value.ParameterValues[index];
            if (IsToken(parameterValue))
            {
                result.Append(parameterValue);
            }
            else
            {
                result.Append('"');
                int characterIndex = 0;
                while (characterIndex < parameterValue.Length)
                {
                    char character = parameterValue[characterIndex];
                    if (character == '"' || character == '\\')
                    {
                        result.Append('\\');
                    }
                    result.Append(character);
                    characterIndex = characterIndex + 1;
                }
                result.Append('"');
            }
            index = index + 1;
        }
        return result.ToString();
    }

    static bool IsRestrictedName(string value, int start, int end)
    {
        int length = end - start;
        if (length < 1 || length > 127)
        {
            return false;
        }
        return IsAsciiAlphaNumeric(value[start]) && IsAsciiAlphaNumeric(value[end - 1]);
    }

    static bool IsRestrictedNameCharacter(char value)
    {
        if (IsAsciiAlphaNumeric(value))
        {
            return true;
        }
        return value == '!' || value == '#' || value == '$' || value == '&' || value == '-' || value == '^' || value == '_' || value == '.' || value == '+';
    }

    static bool IsToken(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            if (!IsTokenCharacter(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool IsTokenCharacter(char value)
    {
        if (value <= 0x20 || value >= 0x7F)
        {
            return false;
        }
        return value != '(' && value != ')' && value != '<' && value != '>' && value != '@' && value != ',' && value != ';' && value != ':' && value != '\\' && value != '"' && value != '/' && value != '[' && value != ']' && value != '?' && value != '=';
    }

    static bool IsQuotedCharacter(char value)
    {
        return value == '\t' || value >= 0x20 && value < 0x7F;
    }

    static bool IsAsciiAlphaNumeric(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || value >= '0' && value <= '9';
    }

    static bool IsWhitespace(char value)
    {
        return value == ' ' || value == '\t';
    }

    static int SkipWhitespace(string value, int index)
    {
        while (index < value.Length && IsWhitespace(value[index]))
        {
            index = index + 1;
        }
        return index;
    }

    static bool HasParameter(OdfMediaTypeValue value, string name)
    {
        int index = 0;
        while (index < value.ParameterNames.Count)
        {
            if (value.ParameterNames[index].Equals(name, global::System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }
}
