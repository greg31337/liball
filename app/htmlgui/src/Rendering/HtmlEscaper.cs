using System;
using System.Text;

namespace htmlgui;

internal enum HtmlInvalidCharacterPolicy
{
    Reject = 0,
    Replace = 1
}

internal static class HtmlEscaper
{
    internal static string EscapeText(string? value, HtmlInvalidCharacterPolicy policy)
    {
        string normalized = NormalizeHtmlUnicode(value, policy);
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < normalized.Length)
        {
            char character = normalized[index];
            if (character == '&')
            {
                result.Append("&amp;");
            }
            else if (character == '<')
            {
                result.Append("&lt;");
            }
            else if (character == '>')
            {
                result.Append("&gt;");
            }
            else
            {
                result.Append(character);
            }

            index++;
        }

        return result.ToString();
    }

    internal static string EscapeAttributeValue(string? value, HtmlInvalidCharacterPolicy policy)
    {
        string normalized = NormalizeHtmlUnicode(value, policy);
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < normalized.Length)
        {
            char character = normalized[index];
            if (character == '&')
            {
                result.Append("&amp;");
            }
            else if (character == '<')
            {
                result.Append("&lt;");
            }
            else if (character == '>')
            {
                result.Append("&gt;");
            }
            else if (character == '"')
            {
                result.Append("&quot;");
            }
            else if (character == '\'')
            {
                result.Append("&#39;");
            }
            else
            {
                result.Append(character);
            }

            index++;
        }

        return result.ToString();
    }

    internal static string EscapeComment(string? value, HtmlInvalidCharacterPolicy policy)
    {
        string normalized = NormalizeHtmlUnicode(value, policy);
        StringBuilder result = new StringBuilder();
        bool previousHyphen = false;
        int index = 0;
        while (index < normalized.Length)
        {
            char character = normalized[index];
            bool unsafeCharacter = character == '-' && (index == 0 || index == normalized.Length - 1 || previousHyphen);
            if (index == 0 && character == '>')
            {
                unsafeCharacter = true;
            }

            if (unsafeCharacter)
            {
                AppendReplacementOrReject(result, policy, "HTML comments cannot begin with '>', begin or end with '-', or contain '--'.");
                previousHyphen = false;
            }
            else
            {
                result.Append(character);
                previousHyphen = character == '-';
            }

            index++;
        }

        return result.ToString();
    }

    internal static string EscapeRawText(string? value, string? elementName, HtmlInvalidCharacterPolicy policy)
    {
        if (!IsValidRawTextElementName(elementName))
        {
            throw new ArgumentException("Raw-text element names must be non-empty ASCII names.", "elementName");
        }

        string rawTextElementName = elementName ?? string.Empty;
        string normalized = NormalizeHtmlUnicode(value, policy);
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < normalized.Length)
        {
            if (IsRawTextEndTag(normalized, index, rawTextElementName))
            {
                AppendReplacementOrReject(result, policy, "Raw text cannot contain its HTML end-tag delimiter.");
                index++;
            }
            else
            {
                result.Append(normalized[index]);
                index++;
            }
        }

        return result.ToString();
    }

    internal static string NormalizeHtmlUnicode(string? value, HtmlInvalidCharacterPolicy policy)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        ValidateInvalidCharacterPolicy(policy);
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (char.IsHighSurrogate(character))
            {
                if (index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
                {
                    char lowSurrogate = value[index + 1];
                    int scalar = (character - '\ud800') * 1024 + lowSurrogate - '\udc00' + 65536;
                    if (IsUnicodeNoncharacter(scalar))
                    {
                        AppendReplacementOrReject(result, policy, "The value contains a Unicode noncharacter.");
                    }
                    else
                    {
                        result.Append(character);
                        result.Append(lowSurrogate);
                    }

                    index = index + 2;
                }
                else
                {
                    AppendReplacementOrReject(result, policy, "The value contains an unmatched high surrogate.");
                    index++;
                }
            }
            else if (char.IsLowSurrogate(character))
            {
                AppendReplacementOrReject(result, policy, "The value contains an unmatched low surrogate.");
                index++;
            }
            else if (character == '\0')
            {
                AppendReplacementOrReject(result, policy, "The value contains a null character.");
                index++;
            }
            else if (IsUnicodeNoncharacter(character))
            {
                AppendReplacementOrReject(result, policy, "The value contains a Unicode noncharacter.");
                index++;
            }
            else
            {
                result.Append(character);
                index++;
            }
        }

        return result.ToString();
    }

    internal static void ValidateInvalidCharacterPolicy(HtmlInvalidCharacterPolicy policy)
    {
        if (policy != HtmlInvalidCharacterPolicy.Reject && policy != HtmlInvalidCharacterPolicy.Replace)
        {
            throw new ArgumentException("The HTML invalid-character policy is not supported.", "policy");
        }
    }

    internal static void AppendReplacementOrReject(StringBuilder result, HtmlInvalidCharacterPolicy policy, string message)
    {
        if (policy == HtmlInvalidCharacterPolicy.Reject)
        {
            throw new ArgumentException(message, "value");
        }

        result.Append('\uFFFD');
    }

    internal static bool IsUnicodeNoncharacter(int scalar)
    {
        return scalar >= 64976 && scalar <= 65007 || scalar >= 65534 && scalar <= 1114111 && (scalar & 65535) >= 65534;
    }

    internal static bool IsValidRawTextElementName(string? elementName)
    {
        if (elementName == null || elementName.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < elementName.Length)
        {
            char character = elementName[index];
            bool isLower = character >= 'a' && character <= 'z';
            bool isUpper = character >= 'A' && character <= 'Z';
            bool isDigit = character >= '0' && character <= '9';
            if (!isLower && !isUpper && !isDigit && character != '-')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static bool IsRawTextEndTag(string value, int index, string elementName)
    {
        if (index + elementName.Length + 2 > value.Length || value[index] != '<' || value[index + 1] != '/')
        {
            return false;
        }

        int nameIndex = 0;
        while (nameIndex < elementName.Length)
        {
            char actual = ToLowerAscii(value[index + nameIndex + 2]);
            char expected = ToLowerAscii(elementName[nameIndex]);
            if (actual != expected)
            {
                return false;
            }

            nameIndex++;
        }

        int boundaryIndex = index + elementName.Length + 2;
        if (boundaryIndex >= value.Length)
        {
            return false;
        }

        char boundary = value[boundaryIndex];
        return boundary == '>' || boundary == '/' || boundary == ' ' || boundary == '\t' || boundary == '\n' || boundary == '\r' || boundary == '\f';
    }

    internal static char ToLowerAscii(char character)
    {
        if (character >= 'A' && character <= 'Z')
        {
            return (char)(character + 32);
        }

        return character;
    }
}
