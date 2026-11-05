namespace currencycodes;

internal static class CurrencyNormalizationRules
{
    internal static NormalizationResult NormalizeCurrencyAlphabeticCode(string? code)
    {
        return NormalizeFixedAlphabeticCode(
            code,
            CurrencyCodeConstants.CurrencyAlphabeticCodeLength);
    }

    internal static NormalizationResult NormalizeCurrencyNumericCode(string? code)
    {
        if (code == null)
        {
            return CreateNormalizationFailure(NormalizationError.NullInput, null);
        }

        string trimmed = AsciiCodeText.TrimWhitespace(code);
        if (trimmed.Length == 0)
        {
            return CreateNormalizationFailure(NormalizationError.EmptyInput, code);
        }

        if (trimmed.Length > CurrencyCodeConstants.CurrencyNumericCodeLength)
        {
            return CreateNormalizationFailure(NormalizationError.InvalidLength, code);
        }

        if (!AsciiCodeText.IsAsciiDigitText(trimmed))
        {
            return CreateNormalizationFailure(NormalizationError.InvalidCharacter, code);
        }

        return CreateNormalizationSuccess(code, AsciiCodeText.PadNumericCode(trimmed));
    }

    internal static NormalizationResult NormalizeTerritoryAlpha2Code(string? code)
    {
        return NormalizeFixedAlphabeticCode(
            code,
            CurrencyCodeConstants.TerritoryAlpha2CodeLength);
    }

    internal static NormalizationResult NormalizeTerritoryAlpha3Code(string? code)
    {
        return NormalizeFixedAlphabeticCode(
            code,
            CurrencyCodeConstants.TerritoryAlpha3CodeLength);
    }

    static NormalizationResult NormalizeFixedAlphabeticCode(string? code, int requiredLength)
    {
        if (code == null)
        {
            return CreateNormalizationFailure(NormalizationError.NullInput, null);
        }

        string trimmed = AsciiCodeText.TrimWhitespace(code);
        if (trimmed.Length == 0)
        {
            return CreateNormalizationFailure(NormalizationError.EmptyInput, code);
        }

        if (trimmed.Length != requiredLength)
        {
            return CreateNormalizationFailure(NormalizationError.InvalidLength, code);
        }

        if (!AsciiCodeText.IsAsciiLetterText(trimmed))
        {
            return CreateNormalizationFailure(NormalizationError.InvalidCharacter, code);
        }

        return CreateNormalizationSuccess(code, AsciiCodeText.UppercaseAscii(trimmed));
    }

    static NormalizationResult CreateNormalizationFailure(
        NormalizationError error,
        string? originalText)
    {
        return new NormalizationResult
        {
            Success = false,
            Error = error,
            OriginalText = originalText,
            NormalizedText = string.Empty
        };
    }

    static NormalizationResult CreateNormalizationSuccess(string originalText, string normalizedText)
    {
        return new NormalizationResult
        {
            Success = true,
            Error = NormalizationError.None,
            OriginalText = originalText,
            NormalizedText = normalizedText
        };
    }
}
