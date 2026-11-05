namespace currencycodes;

internal class CurrencyFormatRow
{
    internal string CurrencyCode;
    internal bool HasIsoMinorUnit;
    internal int IsoMinorUnitDigits;
    internal decimal IsoMinorUnitFactor;
    internal bool HasStandardFormat;
    internal int StandardFormatDigits;
    internal int StandardRoundingIncrement;
    internal decimal StandardFormatFactor;
    internal bool HasCash;
    internal int CashDigits;
    internal int CashRoundingIncrement;
    internal decimal CashFactor;
    internal string SourceVersion;

    internal CurrencyFormatRow(
        string currencyCode,
        bool hasIsoMinorUnit,
        int isoMinorUnitDigits,
        decimal isoMinorUnitFactor,
        bool hasStandardFormat,
        int standardFormatDigits,
        int standardRoundingIncrement,
        decimal standardFormatFactor,
        bool hasCash,
        int cashDigits,
        int cashRoundingIncrement,
        decimal cashFactor,
        string sourceVersion)
    {
        CurrencyCode = currencyCode;
        HasIsoMinorUnit = hasIsoMinorUnit;
        IsoMinorUnitDigits = isoMinorUnitDigits;
        IsoMinorUnitFactor = isoMinorUnitFactor;
        HasStandardFormat = hasStandardFormat;
        StandardFormatDigits = standardFormatDigits;
        StandardRoundingIncrement = standardRoundingIncrement;
        StandardFormatFactor = standardFormatFactor;
        HasCash = hasCash;
        CashDigits = cashDigits;
        CashRoundingIncrement = cashRoundingIncrement;
        CashFactor = cashFactor;
        SourceVersion = sourceVersion;
    }
}
