using System;

namespace html;

internal static class HtmlSanitizerPolicyNames
{
    internal const string HtmlNamespaceUri = "http://www.w3.org/1999/xhtml";

    internal static string GetHtmlNamespaceUri()
    {
        return HtmlNamespaceUri;
    }

    internal static string ValidateNamespaceUri(string value,
        string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        return value;
    }

    internal static string NormalizeElementLocalName(string namespaceUri,
        string value, string parameterName)
    {
        ValidateNamespaceUri(namespaceUri, "namespaceUri");
        ValidateMarkupName(value, parameterName);
        return namespaceUri == HtmlNamespaceUri ? FoldAscii(value) : value;
    }

    internal static string NormalizeAttributeLocalName(string namespaceUri,
        string value, string parameterName)
    {
        ValidateNamespaceUri(namespaceUri, "namespaceUri");
        ValidateMarkupName(value, parameterName);
        if (namespaceUri.Length > 0 && value.IndexOf(':') >= 0)
        {
            throw new ArgumentException(
                "A namespaced attribute local name cannot contain a colon.",
                parameterName);
        }

        return namespaceUri.Length == 0 ? FoldAscii(value) : value;
    }

    internal static string NormalizePrefix(string value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (value.Length == 0)
        {
            return "";
        }

        ValidateMarkupName(value, parameterName);
        if (value.IndexOf(':') >= 0)
        {
            throw new ArgumentException(
                "An attribute prefix cannot contain a colon.",
                parameterName);
        }

        return value;
    }

    internal static string NormalizeMarkupName(string value,
        string parameterName)
    {
        ValidateMarkupName(value, parameterName);
        return FoldAscii(value);
    }

    internal static bool IsSafeElementLocalName(string namespaceUri,
        string value)
    {
        return namespaceUri != null && IsSafeMarkupName(value);
    }

    internal static bool IsSafeAttributeLocalName(string namespaceUri,
        string value)
    {
        return namespaceUri != null && IsSafeMarkupName(value)
            && (namespaceUri.Length == 0 || value.IndexOf(':') < 0);
    }

    internal static string NormalizeScheme(string value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (value.Length == 0 || !Ascii.IsAlpha(value[0]))
        {
            throw new ArgumentException(
                "A URL scheme must begin with an ASCII letter.",
                parameterName);
        }

        int index = 1;
        while (index < value.Length)
        {
            char current = value[index];
            if (!Ascii.IsAlphanumeric(current) && current != '+'
                && current != '-' && current != '.')
            {
                throw new ArgumentException(
                    "A URL scheme contains an invalid character.",
                    parameterName);
            }

            index = index + 1;
        }

        return FoldAscii(value);
    }

    internal static string NormalizeHost(string value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("A URL host cannot be empty.",
                parameterName);
        }

        int colon = value.IndexOf(':');
        if (colon >= 0 && (value[0] != '['
            || value[value.Length - 1] != ']'))
        {
            throw new ArgumentException(
                "A URL host must not include a port.", parameterName);
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current <= 32 || current == 127 || current == '/'
                || current == '\\' || current == '?' || current == '#'
                || current == '@')
            {
                throw new ArgumentException(
                    "A URL host contains an invalid character.",
                    parameterName);
            }

            index = index + 1;
        }

        return FoldAscii(value);
    }

    internal static string NormalizeCssProperty(string value,
        string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("A CSS property cannot be empty.",
                parameterName);
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            bool allowed = Ascii.IsAlphanumeric(current) || current == '-';
            if (!allowed || index == 0 && Ascii.IsDigit(current))
            {
                throw new ArgumentException(
                    "A CSS property contains an invalid character.",
                    parameterName);
            }

            index = index + 1;
        }

        return FoldAscii(value);
    }

    internal static void ValidateClobberPrefix(string value,
        string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (value.Length == 0)
        {
            throw new ArgumentException(
                "A DOM-clobbering prefix cannot be empty.", parameterName);
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current <= 32 || current == 127
                || UnicodeTools.IsLowSurrogate(current))
            {
                throw new ArgumentException(
                    "A DOM-clobbering prefix contains an invalid character.",
                    parameterName);
            }

            if (UnicodeTools.IsHighSurrogate(current))
            {
                if (index + 1 >= value.Length
                    || !UnicodeTools.IsLowSurrogate(value[index + 1]))
                {
                    throw new ArgumentException(
                        "A DOM-clobbering prefix contains invalid Unicode.",
                        parameterName);
                }

                index = index + 1;
            }

            index = index + 1;
        }
    }

    internal static void ValidateAction(HtmlBlockedElementAction value,
        string parameterName)
    {
        if ((int)value < (int)HtmlBlockedElementAction.DropSubtree
            || (int)value > (int)HtmlBlockedElementAction.EscapeAsText)
        {
            throw new ArgumentOutOfRangeException(parameterName, (int)value,
                "Unknown blocked-element action.");
        }
    }

    internal static void ValidateAttributeAction(
        HtmlSanitizerAttributeAction value, string parameterName)
    {
        if ((int)value < (int)HtmlSanitizerAttributeAction.Keep
            || (int)value > (int)HtmlSanitizerAttributeAction.Prefix)
        {
            throw new ArgumentOutOfRangeException(parameterName, (int)value,
                "Unknown sanitizer attribute action.");
        }
    }

    internal static void ValidateAttributeValidation(
        HtmlSanitizerAttributeValidation value, string parameterName)
    {
        if ((int)value < (int)HtmlSanitizerAttributeValidation.NonEmpty
            || (int)value > (int)HtmlSanitizerAttributeValidation.LanguageTag)
        {
            throw new ArgumentOutOfRangeException(parameterName, (int)value,
                "Unknown sanitizer attribute validation.");
        }
    }

    internal static void ValidateAttributeNormalization(
        HtmlSanitizerAttributeNormalization value, string parameterName)
    {
        if ((int)value < (int)HtmlSanitizerAttributeNormalization.Trim
            || (int)value
                > (int)HtmlSanitizerAttributeNormalization.SpaceSeparatedTokens)
        {
            throw new ArgumentOutOfRangeException(parameterName, (int)value,
                "Unknown sanitizer attribute normalization.");
        }
    }

    internal static void ValidateMarkupName(string value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("A markup name cannot be empty.",
                parameterName);
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            bool allowed;
            if (index == 0)
            {
                allowed = Ascii.IsAlpha(current) || current == '_'
                    || current == ':' || current >= 128;
            }
            else
            {
                allowed = Ascii.IsAlphanumeric(current) || current == '_'
                    || current == ':' || current == '-' || current == '.'
                    || current >= 128;
            }

            if (!allowed || UnicodeTools.IsLowSurrogate(current))
            {
                throw new ArgumentException(
                    "A markup name contains an invalid character.",
                    parameterName);
            }

            if (UnicodeTools.IsHighSurrogate(current))
            {
                if (index + 1 >= value.Length
                    || !UnicodeTools.IsLowSurrogate(value[index + 1]))
                {
                    throw new ArgumentException(
                        "A markup name contains invalid Unicode.",
                        parameterName);
                }

                index = index + 1;
            }

            index = index + 1;
        }
    }

    internal static bool IsSafeMarkupName(string value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            bool allowed;
            if (index == 0)
            {
                allowed = Ascii.IsAlpha(current) || current == '_'
                    || current == ':' || current >= 128;
            }
            else
            {
                allowed = Ascii.IsAlphanumeric(current) || current == '_'
                    || current == ':' || current == '-' || current == '.'
                    || current >= 128;
            }

            if (!allowed || UnicodeTools.IsLowSurrogate(current))
            {
                return false;
            }

            if (UnicodeTools.IsHighSurrogate(current))
            {
                if (index + 1 >= value.Length
                    || !UnicodeTools.IsLowSurrogate(value[index + 1]))
                {
                    return false;
                }

                index = index + 1;
            }

            index = index + 1;
        }

        return true;
    }

    internal static string FoldAscii(string value)
    {
        char[] result = new char[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            result[index] = Ascii.ToLower(value[index]);
            index = index + 1;
        }

        return new string(result);
    }
}
