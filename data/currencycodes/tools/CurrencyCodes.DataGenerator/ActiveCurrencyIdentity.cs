namespace CurrencyCodes.DataGenerator;

internal sealed class ActiveCurrencyIdentity
{
    internal ActiveCurrencyIdentity(
        string alphabeticCode,
        string? isoNumericCodeText,
        string officialName,
        bool isFund,
        bool hasIsoMinorUnit,
        int isoMinorUnitDigits,
        bool hasCldrNumericCode,
        string? cldrSourceNumericCodeText,
        string? cldrNumericCodeText,
        int cldrNumericCodeValue,
        int cldrStandardDigits,
        int cldrStandardRounding,
        int cldrCashDigits,
        int cldrCashRounding,
        bool cldrStandardDigitsExplicit,
        bool cldrStandardRoundingExplicit,
        bool cldrCashDigitsExplicit,
        bool cldrCashRoundingExplicit,
        bool identityConflict,
        bool identityConflictReviewed)
    {
        AlphabeticCode = alphabeticCode;
        IsoNumericCodeText = isoNumericCodeText;
        OfficialName = officialName;
        IsFund = isFund;
        HasIsoMinorUnit = hasIsoMinorUnit;
        IsoMinorUnitDigits = isoMinorUnitDigits;
        HasCldrNumericCode = hasCldrNumericCode;
        CldrSourceNumericCodeText = cldrSourceNumericCodeText;
        CldrNumericCodeText = cldrNumericCodeText;
        CldrNumericCodeValue = cldrNumericCodeValue;
        CldrStandardDigits = cldrStandardDigits;
        CldrStandardRounding = cldrStandardRounding;
        CldrCashDigits = cldrCashDigits;
        CldrCashRounding = cldrCashRounding;
        CldrStandardDigitsExplicit = cldrStandardDigitsExplicit;
        CldrStandardRoundingExplicit = cldrStandardRoundingExplicit;
        CldrCashDigitsExplicit = cldrCashDigitsExplicit;
        CldrCashRoundingExplicit = cldrCashRoundingExplicit;
        IdentityConflict = identityConflict;
        IdentityConflictReviewed = identityConflictReviewed;
    }

    internal string AlphabeticCode { get; }

    internal string? IsoNumericCodeText { get; }

    internal string OfficialName { get; }

    internal bool IsFund { get; }

    internal bool HasIsoMinorUnit { get; }

    internal int IsoMinorUnitDigits { get; }

    internal bool HasCldrNumericCode { get; }

    internal string? CldrSourceNumericCodeText { get; }

    internal string? CldrNumericCodeText { get; }

    internal int CldrNumericCodeValue { get; }

    internal int CldrStandardDigits { get; }

    internal int CldrStandardRounding { get; }

    internal int CldrCashDigits { get; }

    internal int CldrCashRounding { get; }

    internal bool CldrStandardDigitsExplicit { get; }

    internal bool CldrStandardRoundingExplicit { get; }

    internal bool CldrCashDigitsExplicit { get; }

    internal bool CldrCashRoundingExplicit { get; }

    internal bool IdentityConflict { get; }

    internal bool IdentityConflictReviewed { get; }
}
