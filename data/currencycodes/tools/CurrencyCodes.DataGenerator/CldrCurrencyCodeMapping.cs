namespace CurrencyCodes.DataGenerator;

internal sealed class CldrCurrencyCodeMapping
{
    internal CldrCurrencyCodeMapping(
        string alphabeticCode,
        string sourceNumericCodeText,
        string numericCodeText,
        int numericCodeValue)
    {
        AlphabeticCode = alphabeticCode;
        SourceNumericCodeText = sourceNumericCodeText;
        NumericCodeText = numericCodeText;
        NumericCodeValue = numericCodeValue;
    }

    internal string AlphabeticCode { get; }

    internal string SourceNumericCodeText { get; }

    internal string NumericCodeText { get; }

    internal int NumericCodeValue { get; }
}
