namespace currencycodes;

internal static class CurrencyAmountTextRules
{
    internal static AmountTextResult FormatInvariantCurrencyAmount(
        CurrencyDataStore store,
        decimal amount,
        string? code,
        AmountUsage usage,
        System.MidpointRounding midpointRounding)
    {
        CurrencyAmountRuleSelector.ValidateAmountUsage(usage);
        CurrencyAmountRoundingRules.ValidateMidpointRounding(midpointRounding);
        AmountValidationResult validation = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            0m,
            code,
            usage);
        if (!validation.IsValid)
        {
            return CreateAmountTextValidationFailure(validation, amount);
        }

        CurrencyInfo? validationCurrency = validation.Currency;
        if (validationCurrency == null)
        {
            return new AmountTextResult
            {
                Success = false,
                Error = AmountValidationError.InvalidCurrency,
                Reason = "The currency lookup did not produce a match.",
                Currency = null,
                Amount = amount,
                InvariantText = string.Empty
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
            return new AmountTextResult
            {
                Success = false,
                Error = AmountValidationError.MetadataUnavailable,
                Reason = "The selected amount metadata cannot represent an invariant decimal scale for currency '"
                    + currency.AlphabeticCode
                    + "'.",
                Currency = currency,
                Amount = amount,
                InvariantText = string.Empty
            };
        }

        decimal incrementValue = 1m / rule.Factor;
        if (rule.Increment > 0)
        {
            incrementValue = rule.Increment / rule.Factor;
        }

        DecimalRoundingResult rounding = CurrencyAmountRoundingRules.GetRoundingResult(
            amount,
            incrementValue,
            midpointRounding);
        if (!rounding.Success)
        {
            return new AmountTextResult
            {
                Success = false,
                Error = AmountValidationError.OutOfRange,
                Reason = "The rounded amount is outside the decimal range.",
                Currency = currency,
                Amount = amount,
                InvariantText = string.Empty
            };
        }

        string format = "F" + rule.Digits.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        string invariantAmount = rounding.RoundedAmount.ToString(
            format,
            System.Globalization.CultureInfo.InvariantCulture);
        return new AmountTextResult
        {
            Success = true,
            Error = AmountValidationError.None,
            Reason = string.Empty,
            Currency = currency,
            Amount = rounding.RoundedAmount,
            InvariantText = currency.AlphabeticCode + " " + invariantAmount
        };
    }

    internal static AmountTextResult ParseInvariantCurrencyAmount(
        CurrencyDataStore store,
        string? text,
        AmountUsage usage)
    {
        CurrencyAmountRuleSelector.ValidateAmountUsage(usage);
        if (text == null || text.Length < 5 || text[3] != ' ')
        {
            return CreateInvalidAmountTextFormatResult(
                AmountValidationError.InvalidFormat,
                "The invariant currency amount text has an invalid format.",
                null,
                0m);
        }

        string currencyText = text.Substring(0, 3);
        AmountValidationResult currencyValidation =
            CurrencyAmountValidationRules.ValidateCurrencyAmount(
                store,
                0m,
                currencyText,
                usage);
        if (!currencyValidation.IsValid)
        {
            return CreateAmountTextValidationFailure(currencyValidation, 0m);
        }

        CurrencyInfo? validationCurrency = currencyValidation.Currency;
        if (validationCurrency == null)
        {
            return new AmountTextResult
            {
                Success = false,
                Error = AmountValidationError.InvalidCurrency,
                Reason = "The currency lookup did not produce a match.",
                Currency = null,
                Amount = 0m,
                InvariantText = string.Empty
            };
        }

        CurrencyInfo currency = CurrencySnapshotFactory.CopyCurrencyInfo(validationCurrency);
        string amountText = text.Substring(4);
        DecimalParsingResult parsing = ParseInvariantDecimalAmount(amountText);
        if (!parsing.Success)
        {
            if (parsing.IsOutOfRange)
            {
                return CreateInvalidAmountTextFormatResult(
                    AmountValidationError.OutOfRange,
                    "The invariant amount is outside the decimal range.",
                    currency,
                    0m);
            }

            return CreateInvalidAmountTextFormatResult(
                AmountValidationError.InvalidFormat,
                "The invariant currency amount text has an invalid format.",
                currency,
                0m);
        }

        AmountValidationResult amountValidation =
            CurrencyAmountValidationRules.ValidateCurrencyAmount(
                store,
                parsing.Amount,
                currency.AlphabeticCode,
                usage);
        if (!amountValidation.IsValid)
        {
            return CreateAmountTextValidationFailure(amountValidation, parsing.Amount);
        }

        CurrencyInfo? amountCurrency = amountValidation.Currency;
        CurrencyInfo resultCurrency = amountCurrency == null
            ? currency
            : CurrencySnapshotFactory.CopyCurrencyInfo(amountCurrency);
        return new AmountTextResult
        {
            Success = true,
            Error = AmountValidationError.None,
            Reason = string.Empty,
            Currency = resultCurrency,
            Amount = parsing.Amount,
            InvariantText = resultCurrency.AlphabeticCode + " " + amountText
        };
    }

    static DecimalParsingResult ParseInvariantDecimalAmount(string text)
    {
        if (text.Length == 0)
        {
            return CreateInvalidDecimalParsingResult(false);
        }

        int firstDigitIndex = 0;
        bool isNegative = false;
        if (text[0] == '-')
        {
            isNegative = true;
            firstDigitIndex = 1;
        }

        if (firstDigitIndex == text.Length)
        {
            return CreateInvalidDecimalParsingResult(false);
        }

        bool hasDecimalPoint = false;
        int decimalPointIndex = 0;
        for (int index = firstDigitIndex; index < text.Length; index++)
        {
            char character = text[index];
            if (character >= '0' && character <= '9')
            {
                continue;
            }

            if (character == '.'
                && !hasDecimalPoint
                && index > firstDigitIndex
                && index < text.Length - 1)
            {
                hasDecimalPoint = true;
                decimalPointIndex = index;
                continue;
            }

            return CreateInvalidDecimalParsingResult(false);
        }

        int scale = hasDecimalPoint
            ? text.Length - decimalPointIndex - 1
            : 0;
        int parseEnd = text.Length;
        while (scale > 0 && text[parseEnd - 1] == '0')
        {
            scale--;
            parseEnd--;
        }

        if (scale > 28)
        {
            return CreateInvalidDecimalParsingResult(true);
        }

        decimal coefficient = 0m;
        for (int index = firstDigitIndex; index < parseEnd; index++)
        {
            if (hasDecimalPoint && index == decimalPointIndex)
            {
                continue;
            }

            int digit = text[index] - '0';
            decimal maximumBeforeDigit = (decimal.MaxValue - digit) / 10m;
            if (coefficient > maximumBeforeDigit)
            {
                return CreateInvalidDecimalParsingResult(true);
            }

            coefficient = coefficient * 10m + digit;
        }

        decimal factor = 1m;
        for (int index = 0; index < scale; index++)
        {
            factor *= 10m;
        }

        decimal amount = coefficient / factor;
        if (isNegative)
        {
            amount = -amount;
        }

        return new DecimalParsingResult
        {
            Success = true,
            IsOutOfRange = false,
            Amount = amount
        };
    }

    static AmountTextResult CreateAmountTextValidationFailure(
        AmountValidationResult validation,
        decimal amount)
    {
        CurrencyInfo? currency = validation.Currency == null
            ? null
            : CurrencySnapshotFactory.CopyCurrencyInfo(validation.Currency);
        return new AmountTextResult
        {
            Success = false,
            Error = validation.Error,
            Reason = validation.Reason,
            Currency = currency,
            Amount = amount,
            InvariantText = string.Empty
        };
    }

    static AmountTextResult CreateInvalidAmountTextFormatResult(
        AmountValidationError error,
        string reason,
        CurrencyInfo? currency,
        decimal amount)
    {
        return new AmountTextResult
        {
            Success = false,
            Error = error,
            Reason = reason,
            Currency = currency,
            Amount = amount,
            InvariantText = string.Empty
        };
    }

    static DecimalParsingResult CreateInvalidDecimalParsingResult(bool isOutOfRange)
    {
        return new DecimalParsingResult
        {
            Success = false,
            IsOutOfRange = isOutOfRange,
            Amount = 0m
        };
    }
}
