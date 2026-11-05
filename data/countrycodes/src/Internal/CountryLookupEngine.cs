using System.Collections.Generic;

namespace countrycodes;

internal static class CountryLookupEngine
{
    internal static CountryLookupResult FindAlpha2Text(string? value)
    {
        if (value == null)
        {
            return CreateInvalidResult();
        }

        return FindAlpha2Characters(value);
    }

    internal static CountryLookupResult FindAlpha2Characters(
        global::System.ReadOnlySpan<char> value)
    {
        char[] canonical = new char[CountryCodeConstants.Alpha2Length];
        if (!AsciiCodeUtility.TryNormalizeAlpha2(value, canonical))
        {
            return CreateInvalidResult();
        }

        GeneratedCountryRow? row = CountryIndex.FindCountryByAlpha2(new string(canonical));
        return CreateCountryResult(row);
    }

    internal static CountryLookupResult FindAlpha3Text(string? value)
    {
        if (value == null)
        {
            return CreateInvalidResult();
        }

        return FindAlpha3Characters(value);
    }

    internal static CountryLookupResult FindAlpha3Characters(
        global::System.ReadOnlySpan<char> value)
    {
        char[] canonical = new char[CountryCodeConstants.Alpha3Length];
        if (!AsciiCodeUtility.TryNormalizeAlpha3(value, canonical))
        {
            return CreateInvalidResult();
        }

        GeneratedCountryRow? row = CountryIndex.FindCountryByAlpha3(new string(canonical));
        return CreateCountryResult(row);
    }

    internal static CountryLookupResult FindNumericText(string? value)
    {
        if (value == null || !AsciiCodeUtility.IsNumeric3(value))
        {
            return CreateInvalidResult();
        }

        GeneratedCountryRow? row = CountryIndex.FindCountryByNumericText(value);
        return CreateCountryResult(row);
    }

    internal static CountryLookupResult FindNumericValue(int value)
    {
        char[] canonical = new char[CountryCodeConstants.Numeric3Length];
        if (!AsciiCodeUtility.TryFormatNumeric3(value, canonical))
        {
            return CreateInvalidResult();
        }

        GeneratedCountryRow? row = CountryIndex.FindCountryByNumericText(new string(canonical));
        return CreateCountryResult(row);
    }

    internal static CountryLookupResult FindNameText(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return CreateInvalidResult();
        }

        string? searchKey = UnicodeNameUtility.CreateMatchKey(
            value,
            true,
            true,
            true,
            true);
        if (searchKey == null || searchKey.Length == 0)
        {
            return CreateInvalidResult();
        }

        GeneratedCountryNameRow[] nameRows =
            NameIndex.FindCurrentCountryNamesBySearchKey(searchKey);
        List<GeneratedCountryRow> candidates = new List<GeneratedCountryRow>();
        int index = 0;
        while (index < nameRows.Length)
        {
            GeneratedCountryNameRow nameRow = nameRows[index];
            if (IsLookupNameKind(nameRow.NameKind) &&
                global::System.String.Equals(
                    nameRow.DisplayName,
                    value,
                    global::System.StringComparison.OrdinalIgnoreCase))
            {
                GeneratedCountryRow? countryRow =
                    CountryIndex.FindCountryByAlpha2(nameRow.TargetCode);
                if (countryRow != null)
                {
                    AddCountryInOrder(candidates, countryRow);
                }
            }

            index += 1;
        }

        return CreateCandidateResult(candidates.ToArray());
    }

    internal static CountryLookupResult FindAnyText(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return CreateInvalidResult();
        }

        if (value.Length == CountryCodeConstants.Alpha2Length &&
            HasOnlyAsciiAlphanumeric(value))
        {
            return PreferCodeThenName(FindAlpha2Text(value), value);
        }

        if (value.Length == CountryCodeConstants.Alpha3Length &&
            HasOnlyAsciiAlphanumeric(value))
        {
            if (AsciiCodeUtility.IsAlpha3(value))
            {
                return PreferCodeThenName(FindAlpha3Text(value), value);
            }

            if (AsciiCodeUtility.IsNumeric3(value))
            {
                return PreferCodeThenName(FindNumericText(value), value);
            }

            return CreateInvalidResult();
        }

        if (HasOnlyAsciiDigits(value))
        {
            return CreateInvalidResult();
        }

        return FindNameText(value);
    }

    internal static CountryLookupResult PreferCodeThenName(
        CountryLookupResult codeResult,
        string value)
    {
        if (codeResult.Status != LookupStatus.NotFound)
        {
            return codeResult;
        }

        CountryLookupResult nameResult = FindNameText(value);
        if (nameResult.Status == LookupStatus.Found ||
            nameResult.Status == LookupStatus.Ambiguous)
        {
            return nameResult;
        }

        return codeResult;
    }

    internal static CountryLookupResult CreateCountryResult(GeneratedCountryRow? row)
    {
        if (row == null)
        {
            return CreateNotFoundResult();
        }

        Country country = CountryMapper.MapCountry(row);
        return new CountryLookupResult(
            LookupStatus.Found,
            country,
            new Country[0]);
    }

    internal static CountryLookupResult CreateCandidateResult(GeneratedCountryRow[] rows)
    {
        if (rows.Length == 0)
        {
            return CreateNotFoundResult();
        }

        if (rows.Length == 1)
        {
            return CreateCountryResult(rows[0]);
        }

        Country[] mapped = CountryMapper.MapCountries(rows);
        return new CountryLookupResult(LookupStatus.Ambiguous, null, mapped);
    }

    internal static CountryLookupResult CreateInvalidResult()
    {
        return new CountryLookupResult(
            LookupStatus.InvalidInput,
            null,
            new Country[0]);
    }

    internal static CountryLookupResult CreateNotFoundResult()
    {
        return new CountryLookupResult(
            LookupStatus.NotFound,
            null,
            new Country[0]);
    }

    internal static bool IsLookupNameKind(GeneratedCountryNameKind kind)
    {
        return kind == GeneratedCountryNameKind.IsoShort ||
               kind == GeneratedCountryNameKind.IsoOfficial ||
               kind == GeneratedCountryNameKind.Common ||
               kind == GeneratedCountryNameKind.Alias ||
               kind == GeneratedCountryNameKind.Deprecated;
    }

    internal static void AddCountryInOrder(
        List<GeneratedCountryRow> rows,
        GeneratedCountryRow value)
    {
        int index = 0;
        while (index < rows.Count)
        {
            int comparison = AsciiCodeUtility.CompareOrdinal(rows[index].Alpha2, value.Alpha2);
            if (comparison == 0)
            {
                return;
            }

            if (comparison > 0)
            {
                rows.Insert(index, value);
                return;
            }

            index += 1;
        }

        rows.Add(value);
    }

    internal static bool HasOnlyAsciiAlphanumeric(string value)
    {
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

    internal static bool HasOnlyAsciiDigits(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!AsciiCodeUtility.IsAsciiDigit(value[index]))
            {
                return false;
            }

            index += 1;
        }

        return value.Length > 0;
    }
}
