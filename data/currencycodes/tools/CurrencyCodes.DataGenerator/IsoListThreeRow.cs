namespace CurrencyCodes.DataGenerator;

internal sealed class IsoListThreeRow
{
    internal IsoListThreeRow(
        string entityName,
        string officialName,
        string? fundIndicator,
        string alphabeticCode,
        string? numericCode,
        bool hasMinorUnit,
        int minorUnitDigits,
        string withdrawalText,
        IsoPartialDate withdrawalDate)
    {
        EntityName = entityName;
        OfficialName = officialName;
        FundIndicator = fundIndicator;
        AlphabeticCode = alphabeticCode;
        NumericCode = numericCode;
        HasMinorUnit = hasMinorUnit;
        MinorUnitDigits = minorUnitDigits;
        WithdrawalText = withdrawalText;
        WithdrawalDate = withdrawalDate;
    }

    internal string EntityName { get; }

    internal string OfficialName { get; }

    internal string? FundIndicator { get; }

    internal string AlphabeticCode { get; }

    internal string? NumericCode { get; }

    internal bool HasMinorUnit { get; }

    internal int MinorUnitDigits { get; }

    internal string WithdrawalText { get; }

    internal IsoPartialDate WithdrawalDate { get; }
}
