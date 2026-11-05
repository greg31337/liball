namespace currencycodes;

internal static class CurrencyAmountRoundingRules
{
    internal static decimal RoundCurrencyAmount(
        CurrencyDataStore store,
        decimal amount,
        string? code,
        AmountUsage usage,
        System.MidpointRounding midpointRounding)
    {
        CurrencyAmountRuleSelector.ValidateAmountUsage(usage);
        ValidateMidpointRounding(midpointRounding);
        CurrencyInfo currency = store.GetRequiredCurrencyAlphabeticSnapshot(
            code,
            CurrencySet.ActiveOnly);
        CurrencyFormatMetadata metadata = store.GetCurrencyFormatMetadataSnapshot(
            currency.AlphabeticCode);
        CurrencyAmountRule rule = CurrencyAmountRuleSelector.SelectCurrencyAmountRule(
            metadata,
            usage);
        if (!rule.IsAvailable)
        {
            throw new InvalidCurrencyAmountException(
                "The selected amount metadata is unavailable for currency '"
                    + currency.AlphabeticCode
                    + "'.");
        }

        if (rule.Factor <= 0m || rule.Increment < 0)
        {
            throw new InvalidCurrencyAmountException(
                "The selected amount rule is invalid for currency '"
                    + currency.AlphabeticCode
                    + "'.");
        }

        decimal incrementValue = 1m / rule.Factor;
        if (rule.Increment > 0)
        {
            incrementValue = rule.Increment / rule.Factor;
        }

        if (incrementValue <= 0m)
        {
            throw new InvalidCurrencyAmountException(
                "The selected amount rule is invalid for currency '"
                    + currency.AlphabeticCode
                    + "'.");
        }

        return RoundToIncrement(amount, incrementValue, midpointRounding);
    }

    internal static decimal RoundToIncrement(
        decimal amount,
        decimal incrementValue,
        System.MidpointRounding midpointRounding)
    {
        DecimalRoundingResult result = GetRoundingResult(
            amount,
            incrementValue,
            midpointRounding);
        if (!result.Success)
        {
            throw new System.OverflowException("The rounded amount is outside the decimal range.");
        }

        return result.RoundedAmount;
    }

    internal static DecimalRoundingResult GetRoundingResult(
        decimal amount,
        decimal incrementValue,
        System.MidpointRounding midpointRounding)
    {
        ValidateMidpointRounding(midpointRounding);
        if (incrementValue <= 0m)
        {
            throw new InvalidCurrencyAmountException("The rounding increment must be positive.");
        }

        decimal remainder = amount % incrementValue;
        if (remainder == 0m)
        {
            return CreateSuccessfulRoundingResult(amount);
        }

        decimal towardZero = amount - remainder;
        if (midpointRounding == System.MidpointRounding.ToZero)
        {
            return CreateSuccessfulRoundingResult(towardZero);
        }

        if (midpointRounding == System.MidpointRounding.ToNegativeInfinity)
        {
            if (amount < 0m)
            {
                return SubtractRoundingIncrement(towardZero, incrementValue);
            }

            return CreateSuccessfulRoundingResult(towardZero);
        }

        if (midpointRounding == System.MidpointRounding.ToPositiveInfinity)
        {
            if (amount > 0m)
            {
                return AddRoundingIncrement(towardZero, incrementValue);
            }

            return CreateSuccessfulRoundingResult(towardZero);
        }

        decimal absoluteRemainder = remainder < 0m
            ? -remainder
            : remainder;
        decimal distanceToAwayFromZero = incrementValue - absoluteRemainder;
        if (absoluteRemainder < distanceToAwayFromZero)
        {
            return CreateSuccessfulRoundingResult(towardZero);
        }

        if (absoluteRemainder == distanceToAwayFromZero
            && midpointRounding == System.MidpointRounding.ToEven)
        {
            decimal incrementMultiple = towardZero / incrementValue;
            if (incrementMultiple % 2m == 0m)
            {
                return CreateSuccessfulRoundingResult(towardZero);
            }
        }

        if (amount < 0m)
        {
            return SubtractRoundingIncrement(towardZero, incrementValue);
        }

        return AddRoundingIncrement(towardZero, incrementValue);
    }

    internal static void ValidateMidpointRounding(System.MidpointRounding midpointRounding)
    {
        if (midpointRounding != System.MidpointRounding.ToEven
            && midpointRounding != System.MidpointRounding.AwayFromZero
            && midpointRounding != System.MidpointRounding.ToZero
            && midpointRounding != System.MidpointRounding.ToNegativeInfinity
            && midpointRounding != System.MidpointRounding.ToPositiveInfinity)
        {
            throw new System.ArgumentException("The midpoint rounding mode is invalid.");
        }
    }

    static DecimalRoundingResult AddRoundingIncrement(
        decimal amount,
        decimal incrementValue)
    {
        if (amount > decimal.MaxValue - incrementValue)
        {
            return CreateFailedRoundingResult();
        }

        return CreateSuccessfulRoundingResult(amount + incrementValue);
    }

    static DecimalRoundingResult SubtractRoundingIncrement(
        decimal amount,
        decimal incrementValue)
    {
        if (amount < decimal.MinValue + incrementValue)
        {
            return CreateFailedRoundingResult();
        }

        return CreateSuccessfulRoundingResult(amount - incrementValue);
    }

    static DecimalRoundingResult CreateSuccessfulRoundingResult(decimal roundedAmount)
    {
        return new DecimalRoundingResult
        {
            Success = true,
            RoundedAmount = roundedAmount
        };
    }

    static DecimalRoundingResult CreateFailedRoundingResult()
    {
        return new DecimalRoundingResult
        {
            Success = false,
            RoundedAmount = 0m
        };
    }
}
