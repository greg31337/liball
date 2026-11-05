namespace CurrencyCodes.DataGenerator;

internal sealed class CldrTerritoryCode
{
    internal CldrTerritoryCode(
        string alpha2Code,
        string? alpha3Code,
        string? numericCodeText,
        bool hasNumericCode,
        int numericCodeValue)
    {
        Alpha2Code = alpha2Code;
        Alpha3Code = alpha3Code;
        NumericCodeText = numericCodeText;
        HasNumericCode = hasNumericCode;
        NumericCodeValue = numericCodeValue;
    }

    internal string Alpha2Code { get; }

    internal string? Alpha3Code { get; }

    internal string? NumericCodeText { get; }

    internal bool HasNumericCode { get; }

    internal int NumericCodeValue { get; }
}
