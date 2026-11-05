namespace currencycodes;

internal static class CurrencyAmountValidationRules
{
    internal static AmountValidationResult ValidateCurrencyAmount(
        CurrencyDataStore store,
        decimal amount,
        string? code,
        AmountUsage usage)
    {
        CurrencyAmountRuleSelector.ValidateAmountUsage(usage);
        CurrencyLookupResult lookup = store.FindCurrencyAlphabeticLookup(
            code,
            CurrencySet.ActiveOnly);
        if (lookup.Error == LookupError.MalformedInput)
        {
            return CreateInvalidAmountCurrencyResult(
                amount,
                "The currency alphabetic code is malformed.",
                null);
        }

        if (lookup.Error == LookupError.Unassigned)
        {
            return CreateInvalidAmountCurrencyResult(
                amount,
                "Currency alphabetic code '"
                    + lookup.NormalizedQuery
                    + "' is not assigned.",
                null);
        }

        if (lookup.Error == LookupError.OutOfScope)
        {
            CurrencyLookupResult allLookup = store.FindCurrencyAlphabeticLookup(
                lookup.NormalizedQuery,
                CurrencySet.All);
            CurrencyInfo? historicalCurrency = allLookup.Match == null
                ? null
                : CurrencySnapshotFactory.CopyCurrencyInfo(allLookup.Match);
            return CreateInvalidAmountCurrencyResult(
                amount,
                "Currency alphabetic code '"
                    + lookup.NormalizedQuery
                    + "' is not active.",
                historicalCurrency);
        }

        CurrencyInfo? lookupMatch = lookup.Match;
        if (lookupMatch == null)
        {
            return CreateInvalidAmountCurrencyResult(
                amount,
                "The currency lookup did not produce a match.",
                null);
        }

        CurrencyInfo currency = CurrencySnapshotFactory.CopyCurrencyInfo(lookupMatch);
        CurrencyFormatMetadata metadata = store.GetCurrencyFormatMetadataSnapshot(
            lookup.NormalizedQuery);
        CurrencyAmountRule rule = CurrencyAmountRuleSelector.SelectCurrencyAmountRule(
            metadata,
            usage);
        if (!rule.IsAvailable)
        {
            return CreateUnavailableAmountMetadataResult(amount, currency, usage);
        }

        if (!DecimalAmountRules.HasAllowedScale(amount, rule.Digits))
        {
            return CreateInvalidAmountScaleResult(amount, currency, rule);
        }

        if (!DecimalAmountRules.IsMultipleOfIncrement(amount, rule.Factor, rule.Increment))
        {
            return CreateInvalidAmountIncrementResult(amount, currency, rule);
        }

        return CreateValidAmountResult(amount, currency, rule);
    }

    static AmountValidationResult CreateInvalidAmountCurrencyResult(
        decimal amount,
        string reason,
        CurrencyInfo? currency)
    {
        return new AmountValidationResult
        {
            IsValid = false,
            Error = AmountValidationError.InvalidCurrency,
            Reason = reason,
            Currency = currency,
            Amount = amount,
            HasRequiredRule = false,
            RequiredDigits = 0,
            RequiredIncrement = 0,
            HasRoundedCandidate = false,
            RoundedCandidate = 0m
        };
    }

    static AmountValidationResult CreateUnavailableAmountMetadataResult(
        decimal amount,
        CurrencyInfo currency,
        AmountUsage usage)
    {
        string sourceName = "Cash";
        if (usage == AmountUsage.IsoMinorUnit)
        {
            sourceName = "ISO minor-unit";
        }
        else if (usage == AmountUsage.StandardFormat)
        {
            sourceName = "Standard-format";
        }

        return new AmountValidationResult
        {
            IsValid = false,
            Error = AmountValidationError.MetadataUnavailable,
            Reason = sourceName
                + " metadata is unavailable for currency '"
                + currency.AlphabeticCode
                + "'.",
            Currency = currency,
            Amount = amount,
            HasRequiredRule = false,
            RequiredDigits = 0,
            RequiredIncrement = 0,
            HasRoundedCandidate = false,
            RoundedCandidate = 0m
        };
    }

    static AmountValidationResult CreateInvalidAmountScaleResult(
        decimal amount,
        CurrencyInfo currency,
        CurrencyAmountRule rule)
    {
        return new AmountValidationResult
        {
            IsValid = false,
            Error = AmountValidationError.InvalidScale,
            Reason = "The amount has more fractional digits than the selected rule permits.",
            Currency = currency,
            Amount = amount,
            HasRequiredRule = true,
            RequiredDigits = rule.Digits,
            RequiredIncrement = rule.Increment,
            HasRoundedCandidate = false,
            RoundedCandidate = 0m
        };
    }

    static AmountValidationResult CreateInvalidAmountIncrementResult(
        decimal amount,
        CurrencyInfo currency,
        CurrencyAmountRule rule)
    {
        return new AmountValidationResult
        {
            IsValid = false,
            Error = AmountValidationError.InvalidIncrement,
            Reason = "The amount is not a multiple of the selected rounding increment.",
            Currency = currency,
            Amount = amount,
            HasRequiredRule = true,
            RequiredDigits = rule.Digits,
            RequiredIncrement = rule.Increment,
            HasRoundedCandidate = false,
            RoundedCandidate = 0m
        };
    }

    static AmountValidationResult CreateValidAmountResult(
        decimal amount,
        CurrencyInfo currency,
        CurrencyAmountRule rule)
    {
        return new AmountValidationResult
        {
            IsValid = true,
            Error = AmountValidationError.None,
            Reason = string.Empty,
            Currency = currency,
            Amount = amount,
            HasRequiredRule = true,
            RequiredDigits = rule.Digits,
            RequiredIncrement = rule.Increment,
            HasRoundedCandidate = false,
            RoundedCandidate = 0m
        };
    }
}
