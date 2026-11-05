using System;
using System.Text;

namespace htmlgui;

internal static class HtmlSecurityPolicy
{
    internal static string NormalizeResourceUrl(string? value)
    {
        return NormalizeUrl(value, false);
    }

    internal static string NormalizeHyperlinkUrl(string? value)
    {
        return NormalizeUrl(value, true);
    }

    internal static bool IsSafeResourceUrl(string? value)
    {
        return IsSafeUrl(value, false);
    }

    internal static bool IsSafeHyperlinkUrl(string? value)
    {
        return IsSafeUrl(value, true);
    }

    internal static string NormalizeResourceRelationship(string? value, bool isModule)
    {
        string relationship = value ?? string.Empty;
        if (isModule && relationship.Length == 0)
        {
            return "script";
        }

        if (!IsAsciiToken(relationship))
        {
            throw new ArgumentException("Resource relationships must be one non-empty ASCII token.", "value");
        }

        string normalized = ToLowerAsciiText(relationship);
        if (isModule && normalized != "script")
        {
            throw new ArgumentException("Module resources must use the script relationship.", "value");
        }

        return normalized;
    }

    internal static string NormalizeContentSecurityPolicyNonce(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return string.Empty;
        }

        if (value.Length > 256)
        {
            throw new ArgumentException("Content Security Policy nonces cannot exceed 256 characters.", "value");
        }

        int index = 0;
        bool foundPadding = false;
        int paddingCount = 0;
        int dataCharacterCount = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool isLetter = character >= 'a' && character <= 'z' || character >= 'A' && character <= 'Z';
            bool isDigit = character >= '0' && character <= '9';
            if (character == '=')
            {
                foundPadding = true;
                paddingCount++;
                if (paddingCount > 2)
                {
                    throw new ArgumentException("Content Security Policy nonce padding is malformed.", "value");
                }
            }
            else if (!isLetter && !isDigit && character != '+' && character != '/' && character != '-' && character != '_')
            {
                throw new ArgumentException("Content Security Policy nonces must use base64 or base64url characters.", "value");
            }
            else if (foundPadding)
            {
                throw new ArgumentException("Content Security Policy nonce padding must be trailing.", "value");
            }
            else
            {
                dataCharacterCount++;
            }

            index++;
        }

        if (dataCharacterCount == 0)
        {
            throw new ArgumentException("Content Security Policy nonces require base64 data.", "value");
        }

        return value;
    }

    internal static string ValidateRawContentTrust(UiRawContentPolicy? policy, string? value)
    {
        if (policy == null || !policy.IsTrusted || !IsTrustPolicyIdentifier(policy.PolicyId))
        {
            throw new ArgumentException("Raw content requires an enabled, named trust policy.", "policy");
        }

        HtmlEscaper.NormalizeHtmlUnicode(value ?? string.Empty, HtmlInvalidCharacterPolicy.Reject);
        return policy.PolicyId;
    }

    internal static bool IsSafeRawContentUnicode(string? value)
    {
        if (value == null)
        {
            return true;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    return false;
                }

                char lowSurrogate = value[index + 1];
                int scalar = (character - '\ud800') * 1024 + lowSurrogate - '\udc00' + 65536;
                if (HtmlEscaper.IsUnicodeNoncharacter(scalar))
                {
                    return false;
                }

                index = index + 2;
            }
            else if (char.IsLowSurrogate(character) || character == '\0' || HtmlEscaper.IsUnicodeNoncharacter(character))
            {
                return false;
            }
            else
            {
                index++;
            }
        }

        return true;
    }

    internal static string ProtectScriptClosingTokens(string? value)
    {
        string normalized = HtmlEscaper.NormalizeHtmlUnicode(value, HtmlInvalidCharacterPolicy.Reject);
        return ProtectClosingToken(normalized, "\\x3C");
    }

    internal static string ProtectJsonClosingTokens(string? value)
    {
        string normalized = HtmlEscaper.NormalizeHtmlUnicode(value, HtmlInvalidCharacterPolicy.Reject);
        return ProtectClosingToken(normalized, "\\u003C");
    }

    internal static void ValidateResourceMimeType(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return;
        }

        int slashCount = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool isLetter = character >= 'a' && character <= 'z' || character >= 'A' && character <= 'Z';
            bool isDigit = character >= '0' && character <= '9';
            if (character == '/')
            {
                slashCount++;
            }
            else if (!isLetter && !isDigit && character != '!' && character != '#' && character != '$' && character != '&' && character != '^' && character != '_' && character != '.' && character != '+' && character != '-')
            {
                throw new ArgumentException("Resource MIME types must use a type/subtype token.", "value");
            }

            index++;
        }

        if (slashCount != 1 || value[0] == '/' || value[value.Length - 1] == '/')
        {
            throw new ArgumentException("Resource MIME types must use a type/subtype token.", "value");
        }
    }

    internal static void ValidateResourceIntegrity(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return;
        }

        string[] tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            throw new ArgumentException("Resource integrity metadata is malformed.", "value");
        }

        int tokenIndex = 0;
        while (tokenIndex < tokens.Length)
        {
            string token = tokens[tokenIndex];
            bool hasAlgorithm = token.StartsWith("sha256-", StringComparison.Ordinal) || token.StartsWith("sha384-", StringComparison.Ordinal) || token.StartsWith("sha512-", StringComparison.Ordinal);
            int separatorIndex = token.IndexOf('-');
            if (!hasAlgorithm || separatorIndex < 0 || separatorIndex == token.Length - 1)
            {
                throw new ArgumentException("Resource integrity metadata must use SHA-256, SHA-384, or SHA-512 digests.", "value");
            }

            int characterIndex = separatorIndex + 1;
            bool foundPadding = false;
            int paddingCount = 0;
            int digestCharacterCount = 0;
            while (characterIndex < token.Length)
            {
                char character = token[characterIndex];
                bool isLetter = character >= 'a' && character <= 'z' || character >= 'A' && character <= 'Z';
                bool isDigit = character >= '0' && character <= '9';
                if (character == '=')
                {
                    foundPadding = true;
                    paddingCount++;
                    if (paddingCount > 2)
                    {
                        throw new ArgumentException("Resource integrity digest padding is malformed.", "value");
                    }
                }
                else if (!isLetter && !isDigit && character != '+' && character != '/' && character != '-' && character != '_')
                {
                    throw new ArgumentException("Resource integrity digests contain an invalid character.", "value");
                }
                else if (foundPadding)
                {
                    throw new ArgumentException("Resource integrity digest padding must be trailing.", "value");
                }
                else
                {
                    digestCharacterCount++;
                }

                characterIndex++;
            }

            if (digestCharacterCount == 0)
            {
                throw new ArgumentException("Resource integrity metadata requires digest data.", "value");
            }

            tokenIndex++;
        }
    }

    internal static void ValidateCrossOrigin(string? value)
    {
        if (value != null && value.Length > 0 && value != "anonymous" && value != "use-credentials")
        {
            throw new ArgumentException("The cross-origin mode is not supported.", "value");
        }
    }

    internal static void ValidateReferrerPolicy(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return;
        }

        switch (value)
        {
            case "no-referrer":
            case "no-referrer-when-downgrade":
            case "origin":
            case "origin-when-cross-origin":
            case "same-origin":
            case "strict-origin":
            case "strict-origin-when-cross-origin":
            case "unsafe-url":
                return;
            default:
                throw new ArgumentException("The referrer policy is not supported.", "value");
        }
    }

    internal static bool IsSafeAdditionalResourceAttribute(string? name, bool isScript)
    {
        if (name == "blocking" || name == "fetchpriority" || name == "title")
        {
            return true;
        }

        if (isScript)
        {
            return name == "async" || name == "defer" || name == "nomodule";
        }

        return name == "as" || name == "disabled";
    }

    internal static string NormalizeUrl(string? value, bool allowContactSchemes)
    {
        if (!IsSafeUrl(value, allowContactSchemes))
        {
            throw new ArgumentException("The URL is empty, malformed, or uses an unsafe scheme.", "value");
        }

        string checkedValue = value ?? string.Empty;
        int colonIndex = FindSchemeColon(checkedValue);
        if (colonIndex < 0)
        {
            return checkedValue;
        }

        string scheme = ToLowerAsciiText(checkedValue.Substring(0, colonIndex));
        return scheme + checkedValue.Substring(colonIndex);
    }

    internal static bool IsSafeUrl(string? value, bool allowContactSchemes)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    return false;
                }

                char lowSurrogate = value[index + 1];
                int scalar = (character - '\ud800') * 1024 + lowSurrogate - '\udc00' + 65536;
                if (HtmlEscaper.IsUnicodeNoncharacter(scalar))
                {
                    return false;
                }

                index++;
            }
            else if (char.IsLowSurrogate(character) || HtmlEscaper.IsUnicodeNoncharacter(character) || char.IsWhiteSpace(character) || char.IsControl(character) || character == '\\')
            {
                return false;
            }

            if (character == '%' && (index + 2 >= value.Length || !IsHexDigit(value[index + 1]) || !IsHexDigit(value[index + 2])))
            {
                return false;
            }

            index++;
        }

        if (value.StartsWith("//", StringComparison.Ordinal) && (value.Length == 2 || value[2] == '/' || value[2] == '?' || value[2] == '#'))
        {
            return false;
        }

        int colonIndex = FindSchemeColon(value);
        if (colonIndex < 0)
        {
            return true;
        }

        string scheme = ToLowerAsciiText(value.Substring(0, colonIndex));
        if (scheme == "http" || scheme == "https")
        {
            return HasHttpAuthority(value, colonIndex);
        }

        if (scheme == "blob")
        {
            string nested = value.Substring(colonIndex + 1);
            int nestedColonIndex = FindSchemeColon(nested);
            if (nestedColonIndex < 0)
            {
                return false;
            }

            string nestedScheme = ToLowerAsciiText(nested.Substring(0, nestedColonIndex));
            return (nestedScheme == "http" || nestedScheme == "https") && IsSafeUrl(nested, false);
        }

        if (scheme == "about")
        {
            string target = value.Substring(colonIndex + 1);
            return target == "blank" || target.StartsWith("blank?", StringComparison.Ordinal) || target.StartsWith("blank#", StringComparison.Ordinal);
        }

        if (allowContactSchemes && (scheme == "mailto" || scheme == "tel"))
        {
            return colonIndex + 1 < value.Length;
        }

        return false;
    }

    internal static int FindSchemeColon(string value)
    {
        int colonIndex = value.IndexOf(':');
        if (colonIndex <= 0)
        {
            return -1;
        }

        int slashIndex = value.IndexOf('/');
        int queryIndex = value.IndexOf('?');
        int fragmentIndex = value.IndexOf('#');
        if (slashIndex >= 0 && slashIndex < colonIndex || queryIndex >= 0 && queryIndex < colonIndex || fragmentIndex >= 0 && fragmentIndex < colonIndex)
        {
            return -1;
        }

        int index = 0;
        while (index < colonIndex)
        {
            char character = value[index];
            bool isLetter = character >= 'a' && character <= 'z' || character >= 'A' && character <= 'Z';
            bool isDigit = character >= '0' && character <= '9';
            if (index == 0 && !isLetter || index > 0 && !isLetter && !isDigit && character != '+' && character != '-' && character != '.')
            {
                return colonIndex;
            }

            index++;
        }

        return colonIndex;
    }

    internal static bool HasHttpAuthority(string value, int colonIndex)
    {
        int authorityIndex = colonIndex + 3;
        if (colonIndex + 2 >= value.Length || value[colonIndex + 1] != '/' || value[colonIndex + 2] != '/' || authorityIndex >= value.Length)
        {
            return false;
        }

        char first = value[authorityIndex];
        return first != '/' && first != '?' && first != '#';
    }

    internal static bool IsTrustPolicyIdentifier(string? value)
    {
        if (value == null || value.Length == 0 || value.Length > 128 || value.StartsWith("$renderer:", StringComparison.Ordinal))
        {
            return false;
        }

        return IsAsciiToken(value);
    }

    internal static bool IsAsciiToken(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool isLetter = character >= 'a' && character <= 'z' || character >= 'A' && character <= 'Z';
            bool isDigit = character >= '0' && character <= '9';
            if (!isLetter && !isDigit && character != '-' && character != '_' && character != '.' && character != ':')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static bool IsHexDigit(char character)
    {
        return character >= '0' && character <= '9' || character >= 'a' && character <= 'f' || character >= 'A' && character <= 'F';
    }

    internal static string ProtectClosingToken(string value, string replacement)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            if (HtmlEscaper.IsRawTextEndTag(value, index, HtmlElements.Script))
            {
                result.Append(replacement);
                index++;
            }
            else
            {
                result.Append(value[index]);
                index++;
            }
        }

        return result.ToString();
    }

    internal static string ToLowerAsciiText(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            result.Append(HtmlEscaper.ToLowerAscii(value[index]));
            index++;
        }

        return result.ToString();
    }
}
