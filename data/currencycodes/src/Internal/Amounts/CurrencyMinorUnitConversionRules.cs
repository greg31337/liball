namespace currencycodes;

internal static class CurrencyMinorUnitConversionRules
{
    internal static MinorUnitConversionResult ConvertCurrencyMajorToMinor(
        CurrencyDataStore store,
        decimal amount,
        string? code,
        AmountUsage usage)
    {
        AmountValidationResult validation = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            amount,
            code,
            usage);
        if (!validation.IsValid)
        {
            return CreateMajorToMinorValidationFailure(validation);
        }

        CurrencyInfo? validationCurrency = validation.Currency;
        if (validationCurrency == null)
        {
            return new MinorUnitConversionResult
            {
                Success = false,
                Error = AmountValidationError.InvalidCurrency,
                Reason = "The currency lookup did not produce a match.",
                Currency = null,
                MajorAmount = amount,
                MinorUnitValue = 0
            };
        }

        CurrencyInfo currency = CurrencySnapshotFactory.CopyCurrencyInfo(validationCurrency);
        CurrencyFormatMetadata metadata = store.GetCurrencyFormatMetadataSnapshot(
            currency.AlphabeticCode);
        CurrencyAmountRule rule = CurrencyAmountRuleSelector.SelectCurrencyAmountRule(
            metadata,
            usage);
        decimal minimumMajorAmount = (decimal)long.MinValue / rule.Factor;
        decimal maximumMajorAmount = (decimal)long.MaxValue / rule.Factor;
        if (amount < minimumMajorAmount || amount > maximumMajorAmount)
        {
            return CreateMajorToMinorRangeFailure(amount, currency);
        }

        decimal scaledAmount = amount * rule.Factor;
        long minorUnitValue = (long)scaledAmount;
        return new MinorUnitConversionResult
        {
            Success = true,
            Error = AmountValidationError.None,
            Reason = string.Empty,
            Currency = currency,
            MajorAmount = amount,
            MinorUnitValue = minorUnitValue
        };
    }

    internal static MinorUnitConversionResult ConvertCurrencyMinorToMajor(
        CurrencyDataStore store,
        long minorUnitValue,
        string? code,
        AmountUsage usage)
    {
        AmountValidationResult validation = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            0m,
            code,
            usage);
        if (!validation.IsValid)
        {
            return CreateMinorToMajorValidationFailure(validation, minorUnitValue);
        }

        CurrencyInfo? validationCurrency = validation.Currency;
        if (validationCurrency == null)
        {
            return new MinorUnitConversionResult
            {
                Success = false,
                Error = AmountValidationError.InvalidCurrency,
                Reason = "The currency lookup did not produce a match.",
                Currency = null,
                MajorAmount = 0m,
                MinorUnitValue = minorUnitValue
            };
        }

        CurrencyInfo currency = CurrencySnapshotFactory.CopyCurrencyInfo(validationCurrency);
        CurrencyFormatMetadata metadata = store.GetCurrencyFormatMetadataSnapshot(
            currency.AlphabeticCode);
        CurrencyAmountRule rule = CurrencyAmountRuleSelector.SelectCurrencyAmountRule(
            metadata,
            usage);
        if (!CurrencyAmountRuleSelector.HasRepresentableCurrencyAmountScale(rule))
        {
            return new MinorUnitConversionResult
            {
                Success = false,
                Error = AmountValidationError.MetadataUnavailable,
                Reason = "The selected amount metadata cannot represent an exact decimal scale for currency '"
                    + currency.AlphabeticCode
                    + "'.",
                Currency = currency,
                MajorAmount = 0m,
                MinorUnitValue = minorUnitValue
            };
        }

        decimal majorAmount = (decimal)minorUnitValue / rule.Factor;
        return new MinorUnitConversionResult
        {
            Success = true,
            Error = AmountValidationError.None,
            Reason = string.Empty,
            Currency = currency,
            MajorAmount = majorAmount,
            MinorUnitValue = minorUnitValue
        };
    }

    static MinorUnitConversionResult CreateMajorToMinorValidationFailure(
        AmountValidationResult validation)
    {
        CurrencyInfo? currency = validation.Currency == null
            ? null
            : CurrencySnapshotFactory.CopyCurrencyInfo(validation.Currency);
        return new MinorUnitConversionResult
        {
            Success = false,
            Error = validation.Error,
            Reason = validation.Reason,
            Currency = currency,
            MajorAmount = validation.Amount,
            MinorUnitValue = 0
        };
    }

    static MinorUnitConversionResult CreateMajorToMinorRangeFailure(
        decimal amount,
        CurrencyInfo currency)
    {
        return new MinorUnitConversionResult
        {
            Success = false,
            Error = AmountValidationError.OutOfRange,
            Reason = "The major-unit amount is outside the signed minor-unit range.",
            Currency = currency,
            MajorAmount = amount,
            MinorUnitValue = 0
        };
    }

    static MinorUnitConversionResult CreateMinorToMajorValidationFailure(
        AmountValidationResult validation,
        long minorUnitValue)
    {
        CurrencyInfo? currency = validation.Currency == null
            ? null
            : CurrencySnapshotFactory.CopyCurrencyInfo(validation.Currency);
        return new MinorUnitConversionResult
        {
            Success = false,
            Error = validation.Error,
            Reason = validation.Reason,
            Currency = currency,
            MajorAmount = 0m,
            MinorUnitValue = minorUnitValue
        };
    }
}
