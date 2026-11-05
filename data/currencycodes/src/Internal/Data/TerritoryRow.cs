namespace currencycodes;

internal class TerritoryRow
{
    internal string TerritoryKey;
    internal string CanonicalName;
    internal bool HasAlpha2Code;
    internal string Alpha2Code;
    internal bool HasAlpha3Code;
    internal string Alpha3Code;
    internal bool HasNumericCode;
    internal string NumericCode;
    internal int NumericCodeValue;
    internal TerritoryKind Kind;

    internal TerritoryRow(
        string territoryKey,
        string canonicalName,
        bool hasAlpha2Code,
        string alpha2Code,
        bool hasAlpha3Code,
        string alpha3Code,
        bool hasNumericCode,
        string numericCode,
        int numericCodeValue,
        TerritoryKind kind)
    {
        TerritoryKey = territoryKey;
        CanonicalName = canonicalName;
        HasAlpha2Code = hasAlpha2Code;
        Alpha2Code = alpha2Code;
        HasAlpha3Code = hasAlpha3Code;
        Alpha3Code = alpha3Code;
        HasNumericCode = hasNumericCode;
        NumericCode = numericCode;
        NumericCodeValue = numericCodeValue;
        Kind = kind;
    }
}
