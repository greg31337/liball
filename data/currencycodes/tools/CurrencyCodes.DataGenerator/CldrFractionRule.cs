namespace CurrencyCodes.DataGenerator;

internal sealed class CldrFractionRule
{
    internal CldrFractionRule(
        string currencyCode,
        int digits,
        int rounding,
        int cashDigits,
        int cashRounding,
        bool digitsExplicit,
        bool roundingExplicit,
        bool cashDigitsExplicit,
        bool cashRoundingExplicit)
    {
        CurrencyCode = currencyCode;
        Digits = digits;
        Rounding = rounding;
        CashDigits = cashDigits;
        CashRounding = cashRounding;
        DigitsExplicit = digitsExplicit;
        RoundingExplicit = roundingExplicit;
        CashDigitsExplicit = cashDigitsExplicit;
        CashRoundingExplicit = cashRoundingExplicit;
    }

    internal string CurrencyCode { get; }

    internal int Digits { get; }

    internal int Rounding { get; }

    internal int CashDigits { get; }

    internal int CashRounding { get; }

    internal bool DigitsExplicit { get; }

    internal bool RoundingExplicit { get; }

    internal bool CashDigitsExplicit { get; }

    internal bool CashRoundingExplicit { get; }
}
