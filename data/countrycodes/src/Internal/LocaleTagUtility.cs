using System.Collections.Generic;

namespace countrycodes;

internal static class LocaleTagUtility
{
    internal static bool IsStructurallyValid(string? value)
    {
        if (value == null || value.Length == 0 || value.Length > 255)
        {
            return false;
        }

        if (FindRegisteredTag(value) != null)
        {
            return true;
        }

        string[] parts = value.Split('-');
        if (!ArePartsNonempty(parts))
        {
            return false;
        }

        if (EqualsAsciiCase(parts[0], "x"))
        {
            return IsPrivateUseTagShape(parts);
        }

        return IsRegularTagShape(parts);
    }

    internal static string? NormalizeLocaleTag(string? value)
    {
        if (value == null || value.Length == 0 || value.Length > 255)
        {
            return null;
        }

        string? preferredTag = FindPreferredValue(
            value,
            LocaleTagData.PreferredTagKeys,
            LocaleTagData.PreferredTagValues);
        if (preferredTag != null)
        {
            return CanonicalizeRegularTag(preferredTag.Split('-'));
        }

        string? registeredTag = FindRegisteredTag(value);
        if (registeredTag != null)
        {
            return registeredTag;
        }

        string[] parts = value.Split('-');
        if (!ArePartsNonempty(parts))
        {
            return null;
        }

        if (EqualsAsciiCase(parts[0], "x"))
        {
            if (!IsPrivateUseTagShape(parts))
            {
                return null;
            }

            return CanonicalizePrivateUseTag(parts);
        }

        if (!IsRegularTagShape(parts))
        {
            return null;
        }

        return CanonicalizeRegularTag(parts);
    }

    internal static bool IsCanonicalLocaleTag(string? value)
    {
        string? normalized = NormalizeLocaleTag(value);
        if (value == null || normalized == null)
        {
            return false;
        }

        return AsciiCodeUtility.EqualsOrdinal(value, normalized);
    }

    internal static string[] GetFallbackChain(string? value)
    {
        string? normalized = NormalizeLocaleTag(value);
        if (normalized == null)
        {
            return new string[0];
        }

        List<string> fallbacks = new List<string>();
        fallbacks.Add(normalized);
        string candidate = normalized;
        int separatorIndex = candidate.LastIndexOf('-');
        while (separatorIndex >= 0)
        {
            candidate = candidate.Substring(0, separatorIndex);
            int previousSeparatorIndex = candidate.LastIndexOf('-');
            if (candidate.Length == 1 ||
                (previousSeparatorIndex >= 0 &&
                 candidate.Length - previousSeparatorIndex - 1 == 1))
            {
                if (previousSeparatorIndex < 0)
                {
                    candidate = string.Empty;
                }
                else
                {
                    candidate = candidate.Substring(0, previousSeparatorIndex);
                }
            }

            if (candidate.Length > 0 && IsStructurallyValid(candidate))
            {
                fallbacks.Add(candidate);
            }

            separatorIndex = candidate.LastIndexOf('-');
        }

        return fallbacks.ToArray();
    }

    internal static bool IsRegularTagShape(string[] parts)
    {
        if (parts.Length == 0 || !IsAlphaLength(parts[0], 2, 8))
        {
            return false;
        }

        int index = 1;
        int extlangCount = 0;
        if (parts[0].Length <= 3)
        {
            while (index < parts.Length &&
                   extlangCount < 3 &&
                   IsAlphaLength(parts[index], 3, 3))
            {
                index += 1;
                extlangCount += 1;
            }
        }

        if (index < parts.Length && IsAlphaLength(parts[index], 4, 4))
        {
            index += 1;
        }

        if (index < parts.Length && IsRegion(parts[index]))
        {
            index += 1;
        }

        List<string> variants = new List<string>();
        while (index < parts.Length && IsVariant(parts[index]))
        {
            if (ContainsAsciiCase(variants, parts[index]))
            {
                return false;
            }

            variants.Add(parts[index]);
            index += 1;
        }

        List<string> singletons = new List<string>();
        while (index < parts.Length &&
               IsSingleton(parts[index]) &&
               !EqualsAsciiCase(parts[index], "x"))
        {
            if (ContainsAsciiCase(singletons, parts[index]))
            {
                return false;
            }

            singletons.Add(parts[index]);
            index += 1;
            int extensionStart = index;
            while (index < parts.Length && IsAlphanumericLength(parts[index], 2, 8))
            {
                index += 1;
            }

            if (index == extensionStart)
            {
                return false;
            }
        }

        if (index < parts.Length && EqualsAsciiCase(parts[index], "x"))
        {
            index += 1;
            int privateUseStart = index;
            while (index < parts.Length && IsAlphanumericLength(parts[index], 1, 8))
            {
                index += 1;
            }

            if (index == privateUseStart)
            {
                return false;
            }
        }

        return index == parts.Length;
    }

    internal static bool IsPrivateUseTagShape(string[] parts)
    {
        if (parts.Length < 2)
        {
            return false;
        }

        int index = 1;
        while (index < parts.Length)
        {
            if (!IsAlphanumericLength(parts[index], 1, 8))
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static bool ArePartsNonempty(string[] parts)
    {
        int index = 0;
        while (index < parts.Length)
        {
            if (parts[index].Length == 0)
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static bool IsAlphaLength(string value, int minimum, int maximum)
    {
        if (value.Length < minimum || value.Length > maximum)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (!AsciiCodeUtility.IsAsciiLetter(value[index]))
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static bool IsAlphanumericLength(string value, int minimum, int maximum)
    {
        if (value.Length < minimum || value.Length > maximum)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (!AsciiCodeUtility.IsAsciiAlphanumeric(value[index]))
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static bool IsRegion(string value)
    {
        if (IsAlphaLength(value, 2, 2))
        {
            return true;
        }

        if (value.Length != 3)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (!AsciiCodeUtility.IsAsciiDigit(value[index]))
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static bool IsVariant(string value)
    {
        if (value.Length >= 5 && value.Length <= 8)
        {
            return IsAlphanumericLength(value, 5, 8);
        }

        if (value.Length != 4 || !AsciiCodeUtility.IsAsciiDigit(value[0]))
        {
            return false;
        }

        return IsAlphanumericLength(value, 4, 4);
    }

    internal static bool IsSingleton(string value)
    {
        return value.Length == 1 && AsciiCodeUtility.IsAsciiAlphanumeric(value[0]);
    }

    internal static bool ContainsAsciiCase(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (EqualsAsciiCase(values[index], value))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static bool EqualsAsciiCase(string left, string right)
    {
        return AsciiCodeUtility.EqualsOrdinalIgnoreAsciiCase(left, right);
    }

    internal static string? FindPreferredValue(
        string value,
        string[] keys,
        string[] preferredValues)
    {
        int index = FindArrayIndex(value, keys);
        if (index < 0)
        {
            return null;
        }

        return preferredValues[index];
    }

    internal static int FindArrayIndex(string value, string[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (EqualsAsciiCase(value, values[index]))
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }

    internal static string? FindRegisteredTag(string value)
    {
        int index = FindArrayIndex(value, LocaleTagData.GrandfatheredTags);
        if (index < 0)
        {
            return null;
        }

        return LocaleTagData.GrandfatheredTags[index];
    }

    internal static string CanonicalizePrivateUseTag(string[] parts)
    {
        List<string> canonicalParts = new List<string>();
        int index = 0;
        while (index < parts.Length)
        {
            canonicalParts.Add(ToLowerAsciiText(parts[index]));
            index += 1;
        }

        return string.Join("-", canonicalParts);
    }

    internal static string CanonicalizeRegularTag(string[] parts)
    {
        List<string> canonicalParts = new List<string>();
        string originalLanguage = ToLowerAsciiText(parts[0]);
        string? preferredLanguage = FindPreferredValue(
            originalLanguage,
            LocaleTagData.PreferredLanguageKeys,
            LocaleTagData.PreferredLanguageValues);
        string canonicalLanguage = originalLanguage;
        if (preferredLanguage != null)
        {
            canonicalLanguage = ToLowerAsciiText(preferredLanguage);
        }

        int index = 1;
        int extlangEnd = index;
        if (parts[0].Length <= 3)
        {
            while (extlangEnd < parts.Length &&
                   extlangEnd - index < 3 &&
                   IsAlphaLength(parts[extlangEnd], 3, 3))
            {
                extlangEnd += 1;
            }
        }

        if (index < extlangEnd)
        {
            string firstExtlang = ToLowerAsciiText(parts[index]);
            int extlangIndex = FindArrayIndex(firstExtlang, LocaleTagData.ExtlangKeys);
            if (extlangIndex >= 0 &&
                EqualsAsciiCase(
                    originalLanguage,
                    LocaleTagData.ExtlangPrefixes[extlangIndex]))
            {
                canonicalLanguage = firstExtlang;
                index += 1;
            }
        }

        canonicalParts.Add(canonicalLanguage);
        while (index < extlangEnd)
        {
            canonicalParts.Add(ToLowerAsciiText(parts[index]));
            index += 1;
        }

        if (index < parts.Length && IsAlphaLength(parts[index], 4, 4))
        {
            canonicalParts.Add(ToTitleAsciiText(parts[index]));
            index += 1;
        }

        if (index < parts.Length && IsRegion(parts[index]))
        {
            string region = ToUpperAsciiText(parts[index]);
            string? preferredRegion = FindPreferredValue(
                region,
                LocaleTagData.PreferredRegionKeys,
                LocaleTagData.PreferredRegionValues);
            if (preferredRegion != null)
            {
                region = ToUpperAsciiText(preferredRegion);
            }

            canonicalParts.Add(region);
            index += 1;
        }

        while (index < parts.Length && IsVariant(parts[index]))
        {
            string variant = ToLowerAsciiText(parts[index]);
            string? preferredVariant = FindPreferredValue(
                variant,
                LocaleTagData.PreferredVariantKeys,
                LocaleTagData.PreferredVariantValues);
            if (preferredVariant != null)
            {
                variant = ToLowerAsciiText(preferredVariant);
            }

            canonicalParts.Add(variant);
            index += 1;
        }

        while (index < parts.Length)
        {
            canonicalParts.Add(ToLowerAsciiText(parts[index]));
            index += 1;
        }

        return string.Join("-", canonicalParts);
    }

    internal static string ToLowerAsciiText(string value)
    {
        char[] characters = value.ToCharArray();
        int index = 0;
        while (index < characters.Length)
        {
            if (characters[index] >= 'A' && characters[index] <= 'Z')
            {
                characters[index] = (char)(characters[index] + ('a' - 'A'));
            }

            index += 1;
        }

        return new string(characters);
    }

    internal static string ToUpperAsciiText(string value)
    {
        char[] characters = value.ToCharArray();
        int index = 0;
        while (index < characters.Length)
        {
            characters[index] = AsciiCodeUtility.ToUpperAscii(characters[index]);
            index += 1;
        }

        return new string(characters);
    }

    internal static string ToTitleAsciiText(string value)
    {
        char[] characters = ToLowerAsciiText(value).ToCharArray();
        characters[0] = AsciiCodeUtility.ToUpperAscii(characters[0]);
        return new string(characters);
    }
}
