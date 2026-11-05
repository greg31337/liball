namespace countrycodes;

internal static class RequiredCountryLookup
{
    internal static Country GetByAlpha2(string? value)
    {
        return TranslateResult(
            CountryLookupEngine.FindAlpha2Text(value),
            value,
            CountryCodeKind.Alpha2,
            ClassifyAlphaFailure(value, CountryCodeConstants.Alpha2Length));
    }

    internal static Country GetByAlpha3(string? value)
    {
        return TranslateResult(
            CountryLookupEngine.FindAlpha3Text(value),
            value,
            CountryCodeKind.Alpha3,
            ClassifyAlphaFailure(value, CountryCodeConstants.Alpha3Length));
    }

    internal static Country GetByNumericText(string? value)
    {
        return TranslateResult(
            CountryLookupEngine.FindNumericText(value),
            value,
            CountryCodeKind.Numeric3,
            ClassifyNumericFailure(value));
    }

    internal static Country GetByNumericValue(int value)
    {
        string input = FormatNumericValue(value);
        return TranslateResult(
            CountryLookupEngine.FindNumericValue(value),
            input,
            CountryCodeKind.Numeric3,
            ValidationFailureKind.InvalidLength);
    }

    internal static Country GetByName(string? value)
    {
        return TranslateResult(
            CountryLookupEngine.FindNameText(value),
            value,
            CountryCodeKind.Unknown,
            ClassifyNameFailure(value));
    }

    internal static Country GetByAnyIdentifier(string? value)
    {
        return TranslateResult(
            CountryLookupEngine.FindAnyText(value),
            value,
            DetectIdentifierKind(value),
            ClassifyAnyFailure(value));
    }

    internal static Country TranslateResult(
        CountryLookupResult result,
        string? input,
        CountryCodeKind kind,
        ValidationFailureKind failureKind)
    {
        if (result.Status == LookupStatus.Found && result.Country != null)
        {
            return result.Country;
        }

        if (result.Status == LookupStatus.NotFound)
        {
            string notFoundInput = input == null ? string.Empty : input;
            throw new CountryNotFoundException(notFoundInput, kind);
        }

        if (result.Status == LookupStatus.Ambiguous)
        {
            string ambiguousInput = input == null ? string.Empty : input;
            throw new AmbiguousCountryException(
                ambiguousInput,
                GetCandidateCodes(result.Candidates));
        }

        throw new InvalidCountryCodeException(input, kind, failureKind);
    }

    internal static ValidationFailureKind ClassifyAlphaFailure(
        string? value,
        int expectedLength)
    {
        if (value == null || value.Length == 0)
        {
            return ValidationFailureKind.NullOrEmpty;
        }

        if (value.Length != expectedLength)
        {
            return ValidationFailureKind.InvalidLength;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (!AsciiCodeUtility.IsAsciiLetter(value[index]))
            {
                return ValidationFailureKind.InvalidCharacter;
            }

            index += 1;
        }

        return ValidationFailureKind.InvalidShape;
    }

    internal static ValidationFailureKind ClassifyNumericFailure(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return ValidationFailureKind.NullOrEmpty;
        }

        if (value.Length != CountryCodeConstants.Numeric3Length)
        {
            return ValidationFailureKind.InvalidLength;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (!AsciiCodeUtility.IsAsciiDigit(value[index]))
            {
                return ValidationFailureKind.InvalidCharacter;
            }

            index += 1;
        }

        return ValidationFailureKind.InvalidShape;
    }

    internal static ValidationFailureKind ClassifyNameFailure(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return ValidationFailureKind.NullOrEmpty;
        }

        if (!UnicodeNameUtility.IsWellFormedUtf16(value))
        {
            return ValidationFailureKind.InvalidCharacter;
        }

        return ValidationFailureKind.InvalidShape;
    }

    internal static ValidationFailureKind ClassifyAnyFailure(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return ValidationFailureKind.NullOrEmpty;
        }

        if (!UnicodeNameUtility.IsWellFormedUtf16(value))
        {
            return ValidationFailureKind.InvalidCharacter;
        }

        if (CountryLookupEngine.HasOnlyAsciiDigits(value))
        {
            return ValidationFailureKind.InvalidLength;
        }

        return ValidationFailureKind.InvalidShape;
    }

    internal static CountryCodeKind DetectIdentifierKind(string? value)
    {
        if (value == null)
        {
            return CountryCodeKind.Unknown;
        }

        if (AsciiCodeUtility.IsAlpha2(value))
        {
            return CountryCodeKind.Alpha2;
        }

        if (AsciiCodeUtility.IsAlpha3(value))
        {
            return CountryCodeKind.Alpha3;
        }

        if (AsciiCodeUtility.IsNumeric3(value))
        {
            return CountryCodeKind.Numeric3;
        }

        return CountryCodeKind.Unknown;
    }

    internal static string FormatNumericValue(int value)
    {
        char[] canonical = new char[CountryCodeConstants.Numeric3Length];
        if (AsciiCodeUtility.TryFormatNumeric3(value, canonical))
        {
            return new string(canonical);
        }

        return value.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string[] GetCandidateCodes(Country[] candidates)
    {
        string[] codes = new string[candidates.Length];
        int index = 0;
        while (index < candidates.Length)
        {
            codes[index] = candidates[index].Alpha2;
            index += 1;
        }

        return codes;
    }
}
