using System.Collections.Generic;

namespace countrycodes;

internal static class LocalizedNameCatalogEngine
{
    internal static string[] GetSupportedLocales()
    {
        List<string> locales = new List<string>();
        GeneratedCountryNameRow[] countryNames = RuntimeCatalog.GetState().CountryNames;
        int index = 0;
        while (index < countryNames.Length)
        {
            if (countryNames[index].TargetKind == GeneratedCountryNameTargetKind.Country &&
                countryNames[index].NameKind == GeneratedCountryNameKind.Localized)
            {
                AddLocaleInOrder(locales, countryNames[index].Locale);
            }

            index += 1;
        }

        GeneratedSubdivisionNameRow[] subdivisionNames =
            RuntimeCatalog.GetState().SubdivisionNames;
        index = 0;
        while (index < subdivisionNames.Length)
        {
            if (subdivisionNames[index].NameKind ==
                    GeneratedSubdivisionNameKind.Canonical ||
                subdivisionNames[index].NameKind ==
                    GeneratedSubdivisionNameKind.Localized)
            {
                AddLocaleInOrder(locales, subdivisionNames[index].Locale);
            }

            index += 1;
        }

        GeneratedGeographicRegionNameRow[] regionNames =
            RuntimeCatalog.GetState().RegionNames;
        index = 0;
        while (index < regionNames.Length)
        {
            AddLocaleInOrder(locales, regionNames[index].Locale);
            index += 1;
        }

        return locales.ToArray();
    }

    internal static bool IsLocaleAvailable(string? locale)
    {
        if (!LocaleTagUtility.IsCanonicalLocaleTag(locale))
        {
            return false;
        }

        string[] supported = GetSupportedLocales();
        int index = 0;
        while (index < supported.Length)
        {
            if (AsciiCodeUtility.EqualsOrdinal(supported[index], locale))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static string? GetCountryName(
        string? countryAlpha2,
        string? locale,
        bool useFallback)
    {
        if (countryAlpha2 == null)
        {
            return null;
        }

        char[] canonical = new char[CountryCodeConstants.Alpha2Length];
        if (!AsciiCodeUtility.TryNormalizeAlpha2(countryAlpha2, canonical))
        {
            return null;
        }

        string canonicalCode = new string(canonical);
        if (CountryIndex.FindCountryByAlpha2(canonicalCode) == null)
        {
            return null;
        }

        string[] locales = GetLookupLocales(locale, useFallback);
        GeneratedCountryNameRow[] names = NameIndex.GetNamesForCountry(canonicalCode);
        int index = 0;
        while (index < locales.Length)
        {
            string? name = FindCountryName(names, locales[index]);
            if (name != null)
            {
                return name;
            }

            index += 1;
        }

        return null;
    }

    internal static string? GetSubdivisionName(
        string? subdivisionCode,
        string? locale,
        bool useFallback)
    {
        if (subdivisionCode == null)
        {
            return null;
        }

        int maximumLength = CountryCodeConstants.SubdivisionCountryCodeLength +
                            1 +
                            CountryCodeConstants.SubdivisionMaximumSuffixLength;
        char[] canonical = new char[maximumLength];
        if (!AsciiCodeUtility.TryNormalizeSubdivisionCode(
                subdivisionCode,
                canonical,
                false))
        {
            return null;
        }

        string canonicalCode = new string(canonical, 0, subdivisionCode.Length);
        if (SubdivisionIndex.FindSubdivisionByCode(canonicalCode) == null)
        {
            return null;
        }

        string[] locales = GetLookupLocales(locale, useFallback);
        GeneratedSubdivisionNameRow[] names =
            NameIndex.GetNamesForSubdivision(canonicalCode);
        int index = 0;
        while (index < locales.Length)
        {
            string? name = FindSubdivisionName(names, locales[index]);
            if (name != null)
            {
                return name;
            }

            index += 1;
        }

        return null;
    }

    internal static string? GetRegionName(
        string? m49Code,
        string? locale,
        bool useFallback)
    {
        GeneratedGeographicRegionRow? region =
            GeographicRegionCatalogEngine.FindRegionRow(m49Code);
        if (region == null)
        {
            return null;
        }

        string[] locales = GetLookupLocales(locale, useFallback);
        GeneratedGeographicRegionNameRow[] names =
            NameIndex.GetNamesForRegion(region.M49Code);
        int localeIndex = 0;
        while (localeIndex < locales.Length)
        {
            int nameIndex = 0;
            while (nameIndex < names.Length)
            {
                if (AsciiCodeUtility.EqualsOrdinal(
                        names[nameIndex].Locale,
                        locales[localeIndex]))
                {
                    return names[nameIndex].Name;
                }

                nameIndex += 1;
            }

            localeIndex += 1;
        }

        return null;
    }

    internal static string[] GetFallbackChain(string? locale)
    {
        return LocaleTagUtility.GetFallbackChain(locale);
    }

    internal static string[] GetLookupLocales(string? locale, bool useFallback)
    {
        string? normalized = LocaleTagUtility.NormalizeLocaleTag(locale);
        if (normalized == null)
        {
            return new string[0];
        }

        if (useFallback)
        {
            return LocaleTagUtility.GetFallbackChain(normalized);
        }

        return new string[] { normalized };
    }

    internal static string? FindCountryName(
        GeneratedCountryNameRow[] names,
        string locale)
    {
        string? first = null;
        int index = 0;
        while (index < names.Length)
        {
            GeneratedCountryNameRow name = names[index];
            if (name.NameKind == GeneratedCountryNameKind.Localized &&
                AsciiCodeUtility.EqualsOrdinal(name.Locale, locale))
            {
                if (AsciiCodeUtility.EqualsOrdinal(name.SourceId, "cldr-48-2"))
                {
                    return name.DisplayName;
                }

                if (first == null)
                {
                    first = name.DisplayName;
                }
            }

            index += 1;
        }

        return first;
    }

    internal static string? FindSubdivisionName(
        GeneratedSubdivisionNameRow[] names,
        string locale)
    {
        string? canonical = null;
        int index = 0;
        while (index < names.Length)
        {
            GeneratedSubdivisionNameRow name = names[index];
            if (AsciiCodeUtility.EqualsOrdinal(name.Locale, locale))
            {
                if (name.NameKind == GeneratedSubdivisionNameKind.Localized)
                {
                    return name.DisplayName;
                }

                if (name.NameKind == GeneratedSubdivisionNameKind.Canonical &&
                    canonical == null)
                {
                    canonical = name.DisplayName;
                }
            }

            index += 1;
        }

        return canonical;
    }

    internal static void AddLocaleInOrder(List<string> locales, string value)
    {
        int index = 0;
        while (index < locales.Count)
        {
            int comparison = AsciiCodeUtility.CompareOrdinal(locales[index], value);
            if (comparison == 0)
            {
                return;
            }

            if (comparison > 0)
            {
                locales.Insert(index, value);
                return;
            }

            index += 1;
        }

        locales.Add(value);
    }
}
