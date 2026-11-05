namespace currencycodes;

internal class CurrencyRow
{
    internal string AlphabeticCode;
    internal bool HasNumericCode;
    internal string NumericCode;
    internal int NumericCodeValue;
    internal string OfficialName;
    internal bool HasIsoMinorUnit;
    internal int IsoMinorUnitDigits;
    internal CurrencyStatus Status;
    internal CurrencyKind Kind;
    internal bool IdentityFromIso;
    internal bool HistoricalStatusFromIso;
    internal bool FundClassificationFromIso;
    internal bool EnrichedFromCldr;
    internal PartialDate IntroductionDate;
    internal PartialDate WithdrawalDate;

    internal CurrencyRow(
        string alphabeticCode,
        bool hasNumericCode,
        string numericCode,
        int numericCodeValue,
        string officialName,
        bool hasIsoMinorUnit,
        int isoMinorUnitDigits,
        CurrencyStatus status,
        CurrencyKind kind,
        bool identityFromIso,
        bool historicalStatusFromIso,
        bool fundClassificationFromIso,
        bool enrichedFromCldr,
        PartialDate introductionDate,
        PartialDate withdrawalDate)
    {
        AlphabeticCode = alphabeticCode;
        HasNumericCode = hasNumericCode;
        NumericCode = numericCode;
        NumericCodeValue = numericCodeValue;
        OfficialName = officialName;
        HasIsoMinorUnit = hasIsoMinorUnit;
        IsoMinorUnitDigits = isoMinorUnitDigits;
        Status = status;
        Kind = kind;
        IdentityFromIso = identityFromIso;
        HistoricalStatusFromIso = historicalStatusFromIso;
        FundClassificationFromIso = fundClassificationFromIso;
        EnrichedFromCldr = enrichedFromCldr;
        IntroductionDate = introductionDate;
        WithdrawalDate = withdrawalDate;
    }
}
