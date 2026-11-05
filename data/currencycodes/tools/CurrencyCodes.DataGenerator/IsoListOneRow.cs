namespace CurrencyCodes.DataGenerator;

internal sealed class IsoListOneRow
{
    internal IsoListOneRow(
        string entityName,
        string officialName,
        bool isFund,
        string? alphabeticCode,
        string? numericCode,
        bool hasMinorUnit,
        int minorUnitDigits)
    {
        EntityName = entityName;
        OfficialName = officialName;
        IsFund = isFund;
        AlphabeticCode = alphabeticCode;
        NumericCode = numericCode;
        HasMinorUnit = hasMinorUnit;
        MinorUnitDigits = minorUnitDigits;
    }

    internal string EntityName { get; }

    internal string OfficialName { get; }

    internal bool IsFund { get; }

    internal string? AlphabeticCode { get; }

    internal string? NumericCode { get; }

    internal bool HasMinorUnit { get; }

    internal int MinorUnitDigits { get; }
}
