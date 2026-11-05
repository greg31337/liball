using System.Text;

namespace odf;

internal static class OdfLanguageRules
{
    internal static bool IsLanguage(string? value)
    {
        return value != null && value.Length >= 2 && value.Length <= 8 && IsAsciiLetters(value);
    }

    internal static bool IsCountry(string? value)
    {
        if (value == null)
        {
            return false;
        }
        if (value.Length == 2)
        {
            return IsAsciiLetters(value);
        }
        return value.Length == 3 && IsAsciiDigits(value);
    }

    internal static bool IsScript(string? value)
    {
        return value != null && value.Length == 4 && IsAsciiLetters(value);
    }

    internal static bool IsLanguageTag(string? value)
    {
        if (value == null || value.Length == 0 || value.Length > 255)
        {
            return false;
        }
        if (IsGrandfathered(value))
        {
            return true;
        }

        string[] parts = value.Split('-');
        int partIndex = 0;
        while (partIndex < parts.Length)
        {
            if (parts[partIndex].Length == 0 || parts[partIndex].Length > 8 || !IsAsciiAlphaNumeric(parts[partIndex]))
            {
                return false;
            }
            partIndex = partIndex + 1;
        }

        if (parts[0].Equals("x", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return parts.Length > 1;
        }
        if (!IsLanguage(parts[0]))
        {
            return false;
        }

        int index = 1;
        if (parts[0].Length <= 3)
        {
            int extlangCount = 0;
            while (index < parts.Length && extlangCount < 3 && parts[index].Length == 3 && IsAsciiLetters(parts[index]))
            {
                index = index + 1;
                extlangCount = extlangCount + 1;
            }
        }
        if (index < parts.Length && IsScript(parts[index]))
        {
            index = index + 1;
        }
        if (index < parts.Length && IsCountry(parts[index]))
        {
            index = index + 1;
        }
        int variantStart = index;
        while (index < parts.Length && IsVariant(parts[index]))
        {
            if (HasEarlierPart(parts, variantStart, index))
            {
                return false;
            }
            index = index + 1;
        }

        string extensionSingletons = "";
        while (index < parts.Length && IsExtensionSingleton(parts[index]))
        {
            string singleton = parts[index].ToLowerInvariant();
            if (extensionSingletons.IndexOf(singleton, global::System.StringComparison.Ordinal) >= 0)
            {
                return false;
            }
            extensionSingletons = extensionSingletons + singleton;
            index = index + 1;
            int extensionStart = index;
            while (index < parts.Length && parts[index].Length >= 2 && parts[index].Length <= 8 && IsAsciiAlphaNumeric(parts[index]))
            {
                index = index + 1;
            }
            if (index == extensionStart)
            {
                return false;
            }
        }

        if (index < parts.Length && parts[index].Equals("x", global::System.StringComparison.OrdinalIgnoreCase))
        {
            index = index + 1;
            if (index == parts.Length)
            {
                return false;
            }
            while (index < parts.Length)
            {
                if (parts[index].Length < 1 || parts[index].Length > 8 || !IsAsciiAlphaNumeric(parts[index]))
                {
                    return false;
                }
                index = index + 1;
            }
        }
        return index == parts.Length;
    }

    internal static string NormalizeLanguageTag(string? value)
    {
        if (value == null)
        {
            return "";
        }
        if (!IsLanguageTag(value))
        {
            return value;
        }
        if (IsGrandfathered(value))
        {
            return value.ToLowerInvariant();
        }

        string[] parts = value.Split('-');
        StringBuilder result = new StringBuilder();
        bool extensionContent = false;
        int index = 0;
        while (index < parts.Length)
        {
            if (index > 0)
            {
                result.Append('-');
            }
            string part = parts[index];
            if (index == 0)
            {
                result.Append(part.ToLowerInvariant());
            }
            else if (part.Length == 1)
            {
                result.Append(part.ToLowerInvariant());
                extensionContent = true;
            }
            else if (!extensionContent && IsScript(part))
            {
                result.Append(part.Substring(0, 1).ToUpperInvariant());
                result.Append(part.Substring(1).ToLowerInvariant());
            }
            else if (!extensionContent && part.Length == 2 && IsAsciiLetters(part))
            {
                result.Append(part.ToUpperInvariant());
            }
            else
            {
                result.Append(part.ToLowerInvariant());
            }
            index = index + 1;
        }
        return result.ToString();
    }

    internal static string NormalizeCountry(string? value)
    {
        if (value == null)
        {
            return "";
        }
        if (!IsCountry(value))
        {
            return value;
        }
        return value.ToUpperInvariant();
    }

    internal static string NormalizeScript(string? value)
    {
        if (value == null)
        {
            return "";
        }
        if (!IsScript(value))
        {
            return value;
        }
        return value.Substring(0, 1).ToUpperInvariant() + value.Substring(1).ToLowerInvariant();
    }

    static bool IsVariant(string value)
    {
        if (value.Length >= 5 && value.Length <= 8)
        {
            return IsAsciiAlphaNumeric(value);
        }
        if (value.Length == 4 && value[0] >= '0' && value[0] <= '9')
        {
            return IsAsciiAlphaNumeric(value);
        }
        return false;
    }

    static bool IsExtensionSingleton(string value)
    {
        if (value.Length != 1 || !IsAsciiAlphaNumeric(value))
        {
            return false;
        }
        return !value.Equals("x", global::System.StringComparison.OrdinalIgnoreCase);
    }

    static bool IsAsciiLetters(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool upper = character >= 'A' && character <= 'Z';
            bool lower = character >= 'a' && character <= 'z';
            if (!upper && !lower)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool IsAsciiDigits(string value)
    {
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

    static bool IsAsciiAlphaNumeric(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool letter = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z';
            bool digit = character >= '0' && character <= '9';
            if (!letter && !digit)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool IsGrandfathered(string value)
    {
        return value.Equals("en-GB-oed", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("i-ami", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("i-bnn", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("i-default", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("i-enochian", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("i-hak", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("i-klingon", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("i-lux", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("i-mingo", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("i-navajo", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("i-pwn", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("i-tao", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("i-tay", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("i-tsu", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("sgn-BE-FR", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("sgn-BE-NL", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("sgn-CH-DE", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("art-lojban", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("cel-gaulish", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("no-bok", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("no-nyn", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("zh-guoyu", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("zh-hakka", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("zh-min", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("zh-min-nan", global::System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("zh-xiang", global::System.StringComparison.OrdinalIgnoreCase);
    }

    static bool HasEarlierPart(string[] parts, int start, int current)
    {
        int index = start;
        while (index < current)
        {
            if (parts[index].Equals(parts[current], global::System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }
}
