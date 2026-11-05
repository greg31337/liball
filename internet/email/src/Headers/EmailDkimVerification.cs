using System;
using System.Collections.Generic;

namespace email;
internal class EmailDkimVerificationValidation
{
    internal static string RequireDomain(string value, string parameterName)
    {
        if (value == null) throw new ArgumentNullException(parameterName);
        if (!IsDomain(value)) throw new ArgumentException("A DKIM domain or selector must be a valid ASCII dot-atom.", parameterName);
        return value;
    }

    internal static bool IsDomain(string value)
    {
        if (value == null || value.Length == 0 || value.Length > 255) return false;
        int labelLength = 0;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '.')
            {
                if (labelLength == 0 || labelLength > 63 || value[index - 1] == '-') return false;
                labelLength = 0;
            }
            else
            {
                bool alphaNumeric = current >= 'a' && current <= 'z' || current >= 'A' && current <= 'Z' || current >= '0' && current <= '9';
                if (!alphaNumeric && current != '-' || labelLength == 0 && current == '-') return false;
                labelLength++;
            }

            index++;
        }

        return labelLength > 0 && labelLength <= 63 && value[value.Length - 1] != '-';
    }
}
