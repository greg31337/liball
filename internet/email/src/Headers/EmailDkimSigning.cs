using System;
using System.Collections.Generic;

namespace email;
internal static class EmailDkimSigningValidation
{
    internal static string RequireDomain(string value, string parameterName)
    {
        if (value == null) throw new ArgumentNullException(parameterName);
        if (value.Length == 0 || value.Length > 255) throw new ArgumentException("A DKIM domain or selector must be a non-empty ASCII domain name.", parameterName);
        int labelLength = 0;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '.')
            {
                if (labelLength == 0 || value[index - 1] == '-') throw new ArgumentException("A DKIM domain or selector contains an invalid label.", parameterName);
                labelLength = 0;
            }
            else
            {
                if (current > 127 || !IsLetterOrDigit(current) && current != '-') throw new ArgumentException("A DKIM domain or selector must use ASCII letters, digits, hyphens, and dots.", parameterName);
                if (labelLength == 0 && current == '-') throw new ArgumentException("A DKIM domain or selector label cannot begin with a hyphen.", parameterName);
                labelLength++;
                if (labelLength > 63) throw new ArgumentException("A DKIM domain or selector label cannot exceed 63 characters.", parameterName);
            }

            index++;
        }

        if (labelLength == 0 || value[value.Length - 1] == '-') throw new ArgumentException("A DKIM domain or selector contains an invalid final label.", parameterName);
        return value;
    }

    internal static string RequireHeaderName(string value, string parameterName)
    {
        if (value == null) throw new ArgumentNullException(parameterName);
        if (value.Length == 0) throw new ArgumentException("A signed header name cannot be empty.", parameterName);
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current < 33 || current > 126 || current == 58) throw new ArgumentException("A signed header name must contain printable ASCII field-name characters.", parameterName);
            index++;
        }

        return value;
    }

    internal static EmailDkimCanonicalizationKind RequireCanonicalization(EmailDkimCanonicalizationKind value, string parameterName)
    {
        if (value != EmailDkimCanonicalizationKind.Simple && value != EmailDkimCanonicalizationKind.Relaxed) throw new ArgumentOutOfRangeException(parameterName);
        return value;
    }

    internal static long RequireTime(long value, string parameterName)
    {
        if (value < 0 || value > 999999999999) throw new ArgumentOutOfRangeException(parameterName);
        return value;
    }

    static bool IsLetterOrDigit(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || value >= '0' && value <= '9';
    }
}
