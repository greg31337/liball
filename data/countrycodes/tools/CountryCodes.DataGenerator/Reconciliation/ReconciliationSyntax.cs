using System;
using System.Collections.Generic;
using System.Globalization;
using CountryCodes.DataGenerator.Staging;

namespace CountryCodes.DataGenerator.Reconciliation;

internal static class ReconciliationSyntax
{
    internal static bool IsAlpha2(string value)
    {
        return IsUpperLetters(value, 2);
    }

    internal static bool IsAlpha3(string value)
    {
        return IsUpperLetters(value, 3);
    }

    internal static bool IsAlpha4(string value)
    {
        return IsUpperLetters(value, 4);
    }

    internal static bool IsNumeric3(string value, bool allowAggregateRange)
    {
        if (value.Length != 3 ||
            !IsAsciiDigit(value[0]) ||
            !IsAsciiDigit(value[1]) ||
            !IsAsciiDigit(value[2]))
        {
            return false;
        }

        int numeric = ((value[0] - '0') * 100) + ((value[1] - '0') * 10) + (value[2] - '0');
        return numeric > 0 && (allowAggregateRange || numeric < 900);
    }

    internal static bool IsSubdivision(string value)
    {
        if (value.Length < 4 || value.Length > 6 || value[2] != '-' || !IsAlpha2Prefix(value))
        {
            return false;
        }

        int suffixLength = value.Length - 3;
        if (suffixLength < 1 || suffixLength > 3)
        {
            return false;
        }

        for (int index = 3; index < value.Length; index += 1)
        {
            if (!IsAsciiUpper(value[index]) && !IsAsciiDigit(value[index]))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsCanonicalDate(string value)
    {
        if (value.Length == 4)
        {
            return DateTime.TryParseExact(
                value,
                "yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime _);
        }

        return value.Length == 10 && DateTime.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime _);
    }

    internal static bool IsBcp47Tag(
        string value,
        IReadOnlyDictionary<string, bool> registeredLanguages)
    {
        if (value.Length == 0 || value.Length > 255)
        {
            return false;
        }

        string[] subtags = value.Split('-');
        if (subtags.Length == 0 || subtags[0].Length < 2 || subtags[0].Length > 8)
        {
            return false;
        }

        string language = IdentifierStagingNormalizer.LowerAscii(subtags[0]);
        if (!IsAsciiLetters(subtags[0]) || !registeredLanguages.ContainsKey(language))
        {
            return false;
        }

        Dictionary<string, bool> singletons = new Dictionary<string, bool>(StringComparer.Ordinal);
        bool needsExtensionValue = false;
        for (int index = 1; index < subtags.Length; index += 1)
        {
            string subtag = subtags[index];
            if (subtag.Length == 0 || subtag.Length > 8 || !IsAsciiAlphaNumeric(subtag))
            {
                return false;
            }

            if (subtag.Length == 1)
            {
                string singleton = IdentifierStagingNormalizer.LowerAscii(subtag);
                if (singletons.ContainsKey(singleton) || needsExtensionValue)
                {
                    return false;
                }

                singletons.Add(singleton, true);
                needsExtensionValue = true;
            }
            else
            {
                needsExtensionValue = false;
            }
        }

        return !needsExtensionValue;
    }

    private static bool IsUpperLetters(string value, int length)
    {
        if (value.Length != length)
        {
            return false;
        }

        for (int index = 0; index < value.Length; index += 1)
        {
            if (!IsAsciiUpper(value[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAlpha2Prefix(string value)
    {
        return IsAsciiUpper(value[0]) && IsAsciiUpper(value[1]);
    }

    private static bool IsAsciiLetters(string value)
    {
        for (int index = 0; index < value.Length; index += 1)
        {
            char character = value[index];
            if (!IsAsciiUpper(character) && (character < 'a' || character > 'z'))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAsciiAlphaNumeric(string value)
    {
        for (int index = 0; index < value.Length; index += 1)
        {
            char character = value[index];
            if (!IsAsciiUpper(character) &&
                (character < 'a' || character > 'z') &&
                !IsAsciiDigit(character))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAsciiUpper(char value)
    {
        return value >= 'A' && value <= 'Z';
    }

    private static bool IsAsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }
}
