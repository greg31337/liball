namespace currencycodes;

internal static class CurrencyAmountRuleSelector
{
    internal static void ValidateAmountUsage(AmountUsage usage)
    {
        if (usage != AmountUsage.IsoMinorUnit
            && usage != AmountUsage.StandardFormat
            && usage != AmountUsage.Cash)
        {
            throw new System.ArgumentException("The amount usage is invalid.");
        }
    }

    internal static CurrencyAmountRule SelectCurrencyAmountRule(
        CurrencyFormatMetadata metadata,
        AmountUsage usage)
    {
        ValidateAmountUsage(usage);
        if (usage == AmountUsage.IsoMinorUnit)
        {
            return new CurrencyAmountRule
            {
                IsAvailable = metadata.HasIsoMinorUnit,
                Digits = metadata.IsoMinorUnitDigits,
                Increment = 0,
                Factor = metadata.IsoMinorUnitFactor
            };
        }

        if (usage == AmountUsage.StandardFormat)
        {
            return new CurrencyAmountRule
            {
                IsAvailable = metadata.HasStandardFormat,
                Digits = metadata.StandardFormatDigits,
                Increment = metadata.StandardRoundingIncrement,
                Factor = metadata.StandardFormatFactor
            };
        }

        return new CurrencyAmountRule
        {
            IsAvailable = metadata.HasCash,
            Digits = metadata.CashDigits,
            Increment = metadata.CashRoundingIncrement,
            Factor = metadata.CashFactor
        };
    }

    internal static bool HasRepresentableCurrencyAmountScale(CurrencyAmountRule rule)
    {
        if (!rule.IsAvailable || rule.Digits < 0 || rule.Digits > 28)
        {
            return false;
        }

        decimal expectedFactor = 1m;
        for (int index = 0; index < rule.Digits; index++)
        {
            expectedFactor *= 10m;
        }

        return rule.Factor == expectedFactor;
    }
}
