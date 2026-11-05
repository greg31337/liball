namespace currencycodes;

internal static class DecimalAmountRules
{
    internal static int GetEffectiveScale(decimal amount)
    {
        int storedScale = GetStoredScale(amount);
        for (int scale = 0; scale < storedScale; scale++)
        {
            if (IsExactlyRepresentableAtScale(amount, scale))
            {
                return scale;
            }
        }

        return storedScale;
    }

    internal static bool HasAllowedScale(decimal amount, int allowedDigits)
    {
        if (allowedDigits < 0 || allowedDigits > 28)
        {
            return false;
        }

        int storedScale = GetStoredScale(amount);
        if (storedScale <= allowedDigits)
        {
            return true;
        }

        return IsExactlyRepresentableAtScale(amount, allowedDigits);
    }

    internal static bool IsMultipleOfIncrement(
        decimal amount,
        decimal factor,
        int increment)
    {
        if (factor <= 0m || increment < 0)
        {
            return false;
        }

        if (increment == 0)
        {
            return true;
        }

        decimal incrementValue = increment / factor;
        if (incrementValue == 0m)
        {
            return false;
        }

        return amount % incrementValue == 0m;
    }

    static int GetStoredScale(decimal amount)
    {
        int[] bits = decimal.GetBits(amount);
        return (bits[3] >> 16) & 255;
    }

    static bool IsExactlyRepresentableAtScale(decimal amount, int scale)
    {
        decimal scaledAmount = amount;
        for (int index = 0; index < scale; index++)
        {
            scaledAmount *= 10m;
        }

        return scaledAmount == decimal.Truncate(scaledAmount);
    }
}
